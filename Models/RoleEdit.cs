using Microsoft.AspNetCore.Identity;

namespace Identity.Models
{
    //this class is used to represent the Role and details of Users who are in the role
    public class RoleEdit
    {
        public IdentityRole Role { get; set; }
        public IEnumerable<AppUser> Members { get; set; }
        public IEnumerable<AppUser> NonMembers { get; set; }
    }
}
