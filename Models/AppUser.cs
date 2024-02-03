using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace Identity.Models
{
    //AppUser là class dùng để để định là user trong database
    public class AppUser : IdentityUser
    {
        public Country Country { get; set; }

        public int Age { get; set; }

        [Required]
        public string Salary { get; set; }
    }
}
