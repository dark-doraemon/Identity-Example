using Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Identity.IdentityPolicy
{
    public class CustomPasswordPolicy : PasswordValidator<AppUser>
    {

        //overide ValidateAsync của PasswordValidator<AppUser>
        public override async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user, string password)
        {
            //validate password theo password rules trong file program.cs
            IdentityResult result = await base.ValidateAsync(manager, user, password);

            //nếu không có lỗi thì tạo ra 1 List<IdentityError> mới 
            List<IdentityError> errors = result.Succeeded ? new List<IdentityError>() : result.Errors.ToList();

            //tiếp theo check password theo ý của mình
            
            //kiểm tra password có chứa username không (dùng hàm Contains để kiềm tra) nếu có thì add lỗi
            if (password.ToLower().Contains(user.UserName.ToLower()))
            {
                errors.Add(new IdentityError
                {
                    Description = "Password cannot contain username"
                });
            }

            //nếu password có chứa chuỗi 123 thì add lỗi
            if (password.Contains("123"))
            {
                errors.Add(new IdentityError
                {
                    Description = "Password cannot contain 123 numeric sequence"
                });
            }

            //nếu sô lượng lỗi == 0 thì trả về thành công, ngược lại thì trả về 1 mảng các lỗi
            return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
        }
    }
}
