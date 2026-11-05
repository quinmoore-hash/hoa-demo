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

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

builder.Services.AddHttpClient<HoaApiClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["HoaApi:BaseUrl"] ?? "http://localhost:5080"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

    // Dev-only demo staff login, read from appsettings.Development.json. Not a real credential.
    var demo = app.Configuration.GetSection("DemoStaffUser");
    if (app.Environment.IsDevelopment() && demo["Email"] is { Length: > 0 } email && demo["Password"] is { Length: > 0 } password)
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (await users.FindByEmailAsync(email) is null)
        {
            await users.CreateAsync(new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true }, password);
        }
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
