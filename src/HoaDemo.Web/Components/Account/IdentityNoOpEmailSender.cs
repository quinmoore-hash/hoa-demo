using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using HoaDemo.Web.Data;

namespace HoaDemo.Web.Components.Account;

// Registered only when no "Smtp:Host" is configured; account pages check for it and hide email-dependent flows.
internal sealed class IdentityNoOpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailSender emailSender = new NoOpEmailSender();

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        emailSender.SendEmailAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");
 
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");
 
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
}
