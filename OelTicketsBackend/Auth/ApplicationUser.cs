using Microsoft.AspNetCore.Identity;

namespace OelTicketsBackend.Auth;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}