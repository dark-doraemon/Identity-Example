using Identity.Models;
using Microsoft.AspNetCore.Identity;

namespace Identity.IdentityPolicy
{
    public class CustomUsernameEmailPolicy : UserValidator<AppUser>
    {
        //khi kiểm tra user name thì chương trình thì mặc định sẽ gọi hàm ValidateAsync của UserValidator
        //nhưng ta đã override ValidateAsync nên nó sẽ gọi vào hàm này và nó sẽ validate theo ý của chúng ta
        public async override Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
        {
            //đầu tiên validate theo các user rules trong file program.cs
            IdentityResult result = await base.ValidateAsync(manager, user);

            List<IdentityError> errors = result.Succeeded ? new List<IdentityError>() : result.Errors.ToList();

            if (user.UserName == "google")
            {
                errors.Add(new IdentityError
                {
                    Description = "Google cannot be used as a user name"
                });
            }


            if (!user.Email.ToLower().EndsWith("@yahoo.com"))
            {
                errors.Add(new IdentityError
                {
                    Description = "Only yahoo.com email addresses are allowed"
                });
            }

            return errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed(errors.ToArray());
        }
    }
}
