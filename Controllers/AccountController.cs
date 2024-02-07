using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Identity.Controllers
{
    [Authorize]//bắt muộc mọi action phải đăng nhập mới được sử dụng trừ các action nào có AllowAnonymous attribute
    public class AccountController : Controller
    {

        private UserManager<AppUser> userManager;
        private SignInManager<AppUser> signInManager;

        public AccountController(UserManager<AppUser> userMgr, SignInManager<AppUser> signinMgr)
        {
            userManager = userMgr;
            signInManager = signinMgr;
        }


        #region login
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = "/Account/Login")
        {
            Login login = new Login();
            login.ReturnUrl = returnUrl;
            return View(login);
        }

        [Authorize]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(Login login)
        {
            //nếu không có lỗi
            if (ModelState.IsValid)
            {
                //tìm kiếm người dùng bằng email
                AppUser appUser = await userManager.FindByEmailAsync(login.Email);

                //nếu tìm thấy user
                if (appUser != null)
                {
                    await signInManager.SignOutAsync(); //đăng xuất hết tất cả các login trược đó

                    //sử dụng hàm PasswordSignInAsync để đăng nhập
                    //isPersistent : có nên duy trì cookie khi đóng browser không và ta sẽ dùng nó làm tính năng remember me(true : vẫn lưu cookie khi đóng trình duyêt)
                    //lockoutOnFailure : có nên khóa tài khoản người dùng khi không đăng nhập thành công không
                    Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(appUser,
                        login.Password, login.Remember, true);

                    //nếu là tài khoản xác thực 2FA thì cho dù đúng mật khẩu cũng không succeded
                    if (result.Succeeded)
                        return Redirect(login.ReturnUrl ?? "/");

                    //kiểm tra tài khoản có bị khóa không 
                    if (result.IsLockedOut)
                    {
                        ModelState.AddModelError("", "Your account is locked out. Kindly wait for 1 minutes and try again");
                    }
                    else
                    {
                        //kiểm tra người dùng đã xác thực tài khoản chưa
                        if (await userManager.IsEmailConfirmedAsync(appUser) == false)
                        {
                            ModelState.AddModelError(nameof(login.Email), "Email is unconfirmed, please confirm it first");
                        }

                        //kiểm tra có xác thực 2FA không(chỉ trả về true nếu đúng mật khẩu và có bật 2FA)
                        else if (result.RequiresTwoFactor)
                        {
                            //chuyển tới action LoginTwoStep
                            return RedirectToAction("LoginTwoStep", new { appUser.Email, login.ReturnUrl });
                        }
                      
                    }
                }
                ModelState.AddModelError(nameof(login.Email), "Login Failed: Invalid Email or password");
            }
            return View(login);
        }
        #endregion



        [AllowAnonymous]
        public async Task<IActionResult> LoginTwoStep(string email, string returnUrl)
        {
            var user = await userManager.FindByEmailAsync(email);

            //sử dụng hàm GenerateTwoFactorTokenAsync của class UserManager<T> để tạo ra token 
            var token = await userManager.GenerateTwoFactorTokenAsync(user, "Email");
            //VD : nó sẽ tạo ra 1 token là 259618 và chương trình sẽ gữi token này tới email của mình và phải nhập token này để xác thực 2FA

            EmailHelper emailHelper = new EmailHelper();

            //gửi token tới email của user;
            bool emailResponse = emailHelper.SendEmailTwoFactorCode(user.Email, token);


            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> LoginTwoStep(TwoFactor twoFactor)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            //khi người dùng nhập token vào thì nó sẽ validate bằng TwoFactorSignInAsync
            //

            var result = await signInManager.TwoFactorSignInAsync("Email", twoFactor.TwoFactorCode, false, false);
            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Invalid Login Attempt");
                return View();
            }
        }

        #region logout
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        #endregion


        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            string redirectUrl = base.Url.Action("GoogleResponse", "Account");//đường link sẽ được redirect tới

            //sử dụng ConfigureExternalAuthenticationProperties để cấu hình redirect URL
            //chẳng hạn property này chứ thông tin URL sẽ chuyển hướng khi đã xác thực thông tin cần thiết
            var properties = signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);


            //redirect user to Google OAuth URL với properties
            //do sử sụng scheme google nên nó sẽ biết trả về Google OAuth URL 
            return new ChallengeResult("Google", properties);
        }


        //sau khi xác thực user , google will redirect them to GoogleResponse action
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            //.GetExternalLoginInfoAsync method lấy chi tiết của user account và trả về dữ liệu có kiểu là ExternalLoginInfo
            ExternalLoginInfo info = await signInManager.GetExternalLoginInfoAsync();

            //ExternalLoginInfo định nghĩa 1 princial có thuộc tính ClaimsPrincipal 
            //nó chứa các claim cho user bới google

            //nếu info == null thì trả về login action
            if (info == null)
                return RedirectToAction(nameof(Login));

            //lấy name và email của user
            string[] userInfo = { info.Principal.FindFirst(ClaimTypes.Name).Value, info.Principal.FindFirst(ClaimTypes.Email).Value };

            //ExternalLoginSignInAsync method dùng để đăng nhập vào chương trình (giống PasswordSignInAsync nhưng ở đây là dùng google để login)
            var result = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);


            //nếu đăng nhập thành công
            if (result.Succeeded)
            {
                //return View(userInfo);
                return RedirectToAction("Index", "Home");

            }

            //ngược lại thì không có user trong database được đại diện bằng google user
            //vì vậy thì ta tạo ra 1 user mới và liên kết với Google credentials 
            else
            {
                //tạo 1 user mới với thông in của google
                AppUser user = new AppUser
                {
                    Email = info.Principal.FindFirst(ClaimTypes.Email).Value,
                    UserName = info.Principal.FindFirst(ClaimTypes.Email).Value,
                    Salary = "0"
                };

                //tạo user 
                IdentityResult identResult = await userManager.CreateAsync(user);

                //nếu tạo thành công 
                if (identResult.Succeeded)
                {
                    //thêm external UserLoginInfo vào user
                    identResult = await userManager.AddLoginAsync(user, info);

                    //nếu add thành công
                    if (identResult.Succeeded)
                    {
                        //đăng nhập cho user
                        await signInManager.SignInAsync(user, false);
                        //return View(userInfo);
                        return RedirectToAction("Index", "Home");
                    }

                }
                return AccessDenied();
            }
        }

        //hàm ForgotPassword dùng để hiển thị giao diện quên mật khẩu (nhập email cần reset password)
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }


        //hàm ForgotPassword (post) dùng đề gửi link reset khi người dùng nhập email
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            //tìm kiếm người dùng theo email 
            var user = await userManager.FindByEmailAsync(email);

            if (user == null)//nếu không tìm thấy user
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));

            }

            //tạo password reset token dự trên user bằng hàm GeneratePasswordResetTokenAsync
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            //tạo ra link reset password 
            var link = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            //gửi link reset password tới email của user
            EmailHelper emailHelper = new EmailHelper();
            bool emailResponse = emailHelper.SendEmailPasswordReset(user.Email, link);

            if (emailResponse)
                return RedirectToAction("ForgotPasswordConfirmation");
            else
            {
                // log email failed 
            }
            return View(email);
        }

        //hàm này dùng để hiện thì thông báo khi người dùng rằng chương trình đã gửi link reset password vào email
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }



        //Hàm ResetPassword (nhập mật khẩu với và xác nhận lại mật khẩu)
        //hàm này sẽ được gọi khi người dùng click vào link reset
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            //lấy token và emaail gán vào view rồi từ view gủi token và email vào hàm post 
            var model = new ResetPassword { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPassword resetPassword)
        {
            if (!ModelState.IsValid)
                return View(resetPassword);

            var user = await userManager.FindByEmailAsync(resetPassword.Email);
            if (user == null)
                RedirectToAction("ResetPasswordConfirmation");

            //reset password nhưng phải kiểm tra token có đúng không
            var resetPassResult = await userManager.ResetPasswordAsync(user, resetPassword.Token, resetPassword.Password);
            if (!resetPassResult.Succeeded)
            {
                foreach (var error in resetPassResult.Errors)
                    ModelState.AddModelError(error.Code, error.Description);
                return View();
            }


            return View("ResetPasswordConfirmation");
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
