using Microsoft.AspNetCore.Identity;

namespace MinimalVilla.Models.Domain
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }
        public string? UserSecret { get; set; }
    }
}