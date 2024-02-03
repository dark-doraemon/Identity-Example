﻿using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
                    Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync(appUser, login.Password, login.Remember, false);

                    //nếu là tài khoản xác thực 2FA thì cho dù đúng mật khẩu cũng không succeded
                    if (result.Succeeded)
                        return Redirect(login.ReturnUrl ?? "/");
                    
                    //kiểm tra người dùng đã xác thực tài khoản chưa
                    if(await userManager.IsEmailConfirmedAsync(appUser) == false)
                    {
                        ModelState.AddModelError(nameof(login.Email), "Email is unconfirmed, please confirm it first");
                    }

                    //kiểm tra có xác thực 2FA không
                    else if (result.RequiresTwoFactor)
                    {
                        //chuyển tới action LoginTwoStep
                        return RedirectToAction("LoginTwoStep", new { appUser.Email, login.ReturnUrl  });
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
    }
}