using System.ComponentModel.DataAnnotations;

namespace Identity.Models
{

    //class này dùng để chứa thông tin khi người dùng nhập vào form reset password
    public class ResetPassword
    {
        [Required]
        public string Password { get; set; }
 
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
 
        public string Email { get; set; }
        public string Token { get; set; }
    }
}
