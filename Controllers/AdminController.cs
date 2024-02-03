using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Identity.Controllers
{
    public class AdminController : Controller
    {
        //UserMangaer là class trong Microsoft.AspNetCore.Identity namesapce
        //class này đùng để quản lý người dùng trong database
        //nói chung là dùng để thêm, sửa, xóa,.... user trong database 
        //và user ở đây được class AppUser đại diện
        private UserManager<AppUser> userManager;

        //dùng để lấy mật khẩu bị hash
        private IPasswordHasher<AppUser> passwordHasher;

        //validate password và user
        private IPasswordValidator<AppUser> passwordValidator;
        private IUserValidator<AppUser> userValidator;

        public AdminController(UserManager<AppUser> usrMgr,
            IPasswordHasher<AppUser> passwordHash,
            IPasswordValidator<AppUser> passwordValidator,
            IUserValidator<AppUser> userValid)
        {
            this.userManager = usrMgr;
            this.passwordHasher = passwordHash;
            this.passwordValidator = passwordValidator;
            this.userValidator = userValid;
        }

        #region read users
        public IActionResult Index()
        {
            return View(userManager.Users);
        }
        #endregion

        #region create user
        public ViewResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(User user)
        {
            //nếu mà không có lỗi thì create user
            if (base.ModelState.IsValid)
            {
                //tạo ra AppUser thông tin được cung cấp bởi User class object
                AppUser appUser = new AppUser
                {
                    UserName = user.Name,
                    Email = user.Email,
                    Country = user.Country,
                    Age = user.Age,
                    Salary = user.Salary,

                    TwoFactorEnabled = true,//bật xác thực 2 yếu tố cho user
                };
                //tại vì class UserManage dùng class AppUser làm đại diện cho user, nên add user ta phải add AppUser class object
                //Lưu ý khi gọi hàm CreateAsync thì CreateAsync cũng gọi hàm ValidateAsync của PasswordValidator
                //Mà ValidateAsync được override theo ý của ta 
                IdentityResult result = await userManager.CreateAsync(appUser, user.Password);

                if (result.Succeeded) // nếu mà tạo thành công thì trả về action index
                {
                    //nếu tạo thành công thì gửi email về cho user để xác nhận 

                    //tạo token bằng GenerateEmailConfirmationTokenAsync để tạo đường link
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(appUser);

                    //tạo ra 1 url xác nhận để gửi cho user
                    var confirmationLink = Url.Action("ConfirmEmail", "Email", new { token, email = user.Email }, Request.Scheme);
                    

                    //gửi email tới user
                    EmailHelper emailHelper = new EmailHelper();

                    bool emailResponse = emailHelper.SendEmail(user.Email, confirmationLink);


                    if (emailResponse)
                        return RedirectToAction("Index");
                    else
                    {
                        // log email failed 
                    }
                }

                else // nếu không thành công thì add các lỗi vào Modal State
                {
                    //thuộc tính Errors của UserMangar chứa tất cả các lỗi xảy ra khi thực hiện các Identity operation(create,delelte,...)
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Code + " " + error.Description);
                    }
                }
            };

            //nếu có lỗi thì trả vè view Create
            return View(user);
        }

        #endregion user

        #region update
        public async Task<IActionResult> Update(string id)
        {
            AppUser user = await userManager.FindByIdAsync(id);
            //nếu tìm thấy user thì trả view update
            if (user != null)
                return View(user);
            else //nếu không tìm thấy thì trả về index
                return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Update(string id, string email, string password, int age, string country, string salary)
        {
            //đầu tiên lấy thông tin của user by id
            AppUser user = await userManager.FindByIdAsync(id);

            //nếu tìm thấy user thì update thông tin 
            if (user != null)
            {
                IdentityResult validEmail = null;
                if (!string.IsNullOrEmpty(email)) //validate email
                {
                    user.Email = email;
                    validEmail = await userValidator.ValidateAsync(userManager, user);
                    if (!validEmail.Succeeded)
                    {
                        Errors(validEmail);
                    }
                }
                else
                    ModelState.AddModelError("", "Email cannot be empty");


                IdentityResult validPass = null;
                if (!string.IsNullOrEmpty(password)) //validate password
                {
                    validPass = await passwordValidator.ValidateAsync(userManager, user, password);

                    //nếu mà validate password thành công thì hashpassword
                    if (validPass.Succeeded)
                    {
                        user.PasswordHash = passwordHasher.HashPassword(user, password);
                    }
                    else
                    {
                        Errors(validPass);
                    }

                }
                else
                {
                    ModelState.AddModelError("", "Password cannot be empty");
                }
                user.Age = age;
                user.TwoFactorEnabled = true;
                user.EmailConfirmed = true;
                Country myCountry;
                Enum.TryParse(country, out myCountry);
                user.Country = myCountry;

                if (!string.IsNullOrEmpty(salary))
                {
                    user.Salary = salary;
                }
                else
                {
                    ModelState.AddModelError("", "Salary cannot be empty");
                }

                if (validEmail != null && validPass != null && validEmail.Succeeded && validPass.Succeeded && !string.IsNullOrEmpty(salary))
                {
                    IdentityResult result = await userManager.UpdateAsync(user);
                    //nếu update thành công thì return về index
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Index");
                    }
                    else //nếu thất bại thì add lỗi vào model state thông qua hàm Errors(hàm tự code)
                    {
                        Errors(result);
                    }
                }
            }

            else
            {
                base.ModelState.AddModelError("", "User Not Found");
            }
            return View(user);
        }


        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);

            }
        }
        #endregion

        #region delete

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            //tìm kiếm user theo id 
            AppUser user = await userManager.FindByIdAsync(id);

            //nếu có user
            if (user != null)
            {
                IdentityResult result = await userManager.DeleteAsync(user);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    Errors(result);
            }
            else
            {
                ModelState.AddModelError("", "User Not Found");
            }


            //return View("Index", userManager.Users);
            return RedirectToAction("Index");
        }
        #endregion
    }
}
