using Microsoft.AspNetCore.Identity;

namespace OutsourceTracker.Authentication;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string FullName { get; set; }

    public string AlphaCode { get; set; }

    public string WorkdayId { get; set; }
}
