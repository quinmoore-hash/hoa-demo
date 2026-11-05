extern alias Web;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ApplicationUser = Web::HoaDemo.Web.Data.ApplicationUser;

namespace HoaDemo.Tests.Web
{
    public class EmailSenderTests
    {
        [Fact]
        public async Task Without_smtp_the_forgot_password_page_says_email_is_unavailable()
        {
            using var factory = new HoaWebFactory("Production");
            var html = await factory.CreateClient().GetStringAsync("/Account/ForgotPassword");

            Assert.Contains("Password reset email isn't set up", html);
            Assert.DoesNotContain("forgot-password", html);
        }

        [Fact]
        public async Task With_smtp_the_forgot_password_form_is_shown()
        {
            using var smtp = new FakeSmtpServer();
            using var factory = new HoaWebFactory("Production", SmtpSettings(smtp));
            var html = await factory.CreateClient().GetStringAsync("/Account/ForgotPassword");

            Assert.DoesNotContain("isn't set up", html);
            Assert.Contains("forgot-password", html);
        }

        [Fact]
        public async Task With_smtp_reset_links_are_delivered()
        {
            using var smtp = new FakeSmtpServer();
            using var factory = new HoaWebFactory("Production", SmtpSettings(smtp));
            var sender = factory.Services.GetRequiredService<IEmailSender<ApplicationUser>>();

            await sender.SendPasswordResetLinkAsync(new ApplicationUser(), "owner@example.com", "https://hoa.test/Account/ResetPassword/abc123");
            // Undo quoted-printable soft line breaks so the link can be matched.
            var transcript = (await smtp.Transcript).Replace("=" + Environment.NewLine, "");

            Assert.Contains("MAIL FROM:<noreply@example.com>", transcript);
            Assert.Contains("RCPT TO:<owner@example.com>", transcript);
            Assert.Contains("Subject: Reset your password", transcript);
            Assert.Contains("https://hoa.test/Account/ResetPassword/abc123", transcript);
        }

        private static Dictionary<string, string> SmtpSettings(FakeSmtpServer smtp) => new Dictionary<string, string>
        {
            ["Smtp:Host"] = "127.0.0.1",
            ["Smtp:Port"] = smtp.Port.ToString(),
            ["Smtp:From"] = "noreply@example.com",
        };
    }
}
