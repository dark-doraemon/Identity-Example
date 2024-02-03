using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Contracts;
using Microsoft.AspNetCore.Identity;
using Identity.Models;
namespace Identity.Controllers
{
    public class EmailController : Controller
    {
        private UserManager<AppUser> userManager;
        public EmailController(UserManager<AppUser> usrMgr)
        {
            userManager = usrMgr;
        }
       

        //người người dùng nhấp vào url xác nhận thì nó sẽ xử lý trong hàm này
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
                return View("Error");
            
            //kiểm tra token và user có đúng không 
            var result = await userManager.ConfirmEmailAsync(user, token);
            //nếu Succeeded thì cột EmailConfirmed của user trong database sẽ true 


            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }
    }
}
