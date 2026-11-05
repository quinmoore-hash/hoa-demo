using System.Globalization;
using HoaDemo.Web.Components;
using HoaDemo.Web.Components.Account;
using HoaDemo.Web.Data;
using HoaDemo.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Money/dates are always rendered as en-US regardless of the host culture.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Internal app: every page requires sign-in unless it opts out with [AllowAnonymous] (the Account pages do).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=hoa-web.db";
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var smtp = builder.Configuration.GetSection("Smtp");
if (!string.IsNullOrWhiteSpace(smtp["Host"]))
{
    builder.Services.Configure<SmtpOptions>(smtp);
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
}

builder.Services.AddHttpClient<HoaApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["HoaApi:BaseUrl"] ?? "http://localhost:5080"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

    // Bootstrap staff login from the "StaffUser" config section, in any environment. Development ships a demo
    // account in appsettings.Development.json; elsewhere set StaffUser__Email / StaffUser__Password.
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("StaffUser");
    var staff = app.Configuration.GetSection("StaffUser");
    if (staff["Email"] is { Length: > 0 } email && staff["Password"] is { Length: > 0 } password)
    {
        if (await users.FindByEmailAsync(email) is null)
        {
            var result = await users.CreateAsync(new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true }, password);
            if (result.Succeeded)
            {
                logger.LogInformation("Created staff user {Email}.", email);
            }
            else
            {
                logger.LogError("Could not create staff user {Email}: {Errors}", email, string.Join(" ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    else if (!await users.Users.AnyAsync())
    {
        logger.LogWarning("No staff users exist. Set StaffUser:Email and StaffUser:Password to create the first login.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Static files must be served before authorization so the fallback policy doesn't block CSS/JS on the login page.
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// blazor.web.js is served by a framework endpoint; exempt it from the fallback policy so the anonymous login page can load it.
var components = app.MapGroup("");
((IEndpointConventionBuilder)components).Add(endpoint =>
{
    if (endpoint is RouteEndpointBuilder { RoutePattern.RawText: var path } && path?.Contains("blazor.web.js", StringComparison.OrdinalIgnoreCase) == true)
    {
        endpoint.Metadata.Add(new Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute());
    }
});
components.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();

public partial class Program { }
