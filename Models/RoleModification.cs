using System.ComponentModel.DataAnnotations;

namespace Identity.Models
{

    //this class will help in doing the changes to a role
    public class RoleModification
    {
        [Required]
        public string RoleName { get; set; }

        public string RoleId { get; set; }

        public string[]? AddIds { get; set; } // a arrary of string contains users id to add to a certain role 

        public string[]? DeleteIds { get; set; } // a arrary of string contains users id to remove from a certain role 
    }
}
