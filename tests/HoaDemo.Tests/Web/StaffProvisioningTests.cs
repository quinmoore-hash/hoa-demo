extern alias Web;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ApplicationUser = Web::HoaDemo.Web.Data.ApplicationUser;

namespace HoaDemo.Tests.Web
{
    public class StaffProvisioningTests
    {
        [Fact]
        public async Task Production_creates_the_configured_staff_user_on_startup()
        {
            using var factory = new HoaWebFactory("Production", new Dictionary<string, string>
            {
                ["StaffUser:Email"] = "manager@example.com",
                ["StaffUser:Password"] = "Manager123!",
            });
            factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync("manager@example.com");

            Assert.NotNull(user);
            Assert.True(user!.EmailConfirmed);
            Assert.True(await users.CheckPasswordAsync(user, "Manager123!"));
        }

        [Fact]
        public void Production_without_staff_config_starts_with_no_users()
        {
            using var factory = new HoaWebFactory("Production");
            factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            Assert.False(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>().Users.Any());
        }

        [Theory]
        [InlineData("/staff")]
        [InlineData("/Account/Manage/Email")]
        public async Task Staff_and_account_pages_require_sign_in(string path)
        {
            using var factory = new HoaWebFactory("Production");
            var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("/Account/Login", response.Headers.Location!.PathAndQuery);
        }
    }
}
