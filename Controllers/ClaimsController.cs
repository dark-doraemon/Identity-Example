using Identity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Identity.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private UserManager<AppUser> userManager;
        private IAuthorizationService authService;

        public ClaimsController(UserManager<AppUser> userMgr,IAuthorizationService auth)
        {
            userManager = userMgr;
            authService = auth; 
        }



        public IActionResult Index()
        {
            return View(base.User?.Claims);
        }


        #region create Claim for user
        public ViewResult Create() => View();

        [HttpPost]
        [ActionName("Create")]
        public async Task<IActionResult> Create_Post(string claimType, string claimValue)
        {
            //firstly, get current logged in user from class UserManger by GetUserAsync method
            AppUser user = await userManager.GetUserAsync(HttpContext.User);

            //create a Claim object 
            Claim claim = new Claim(claimType, claimValue, ClaimValueTypes.String);

            //add new claim have just been created for specified user by AddClaimAyncMethod of UserManager class
            IdentityResult result = await userManager.AddClaimAsync(user, claim);

            if (result.Succeeded)
                return RedirectToAction("Index");
            else
                Errors(result);
            return View();
        }

        #endregion


        #region delete claim from user
        [HttpPost]
        public async Task<IActionResult> Delete(string claimValues)
        {

            //firstly, getting user we have just logged in 
            AppUser user = await userManager.GetUserAsync(HttpContext.User);

            //Splitting a string, which are seperated with ";" to a string array 
            string[] claimValuesArray = claimValues.Split(";");

            //string[0] = claim.Type
            //string[1] = claim.Value
            //string[2] = claim.Issuer
            string claimType = claimValuesArray[0], claimValue = claimValuesArray[1], claimIssuer = claimValuesArray[2];


            //Find the claim by linq
            Claim claim = User.Claims.Where(x => x.Type == claimType && x.Value == claimValue && x.Issuer == claimIssuer).FirstOrDefault();


            //remove claim from specified user
            IdentityResult result = await userManager.RemoveClaimAsync(user, claim);

            if (result.Succeeded)
                return RedirectToAction("Index");
            else
                Errors(result);

            return View("Index");
        }
        #endregion


        //chỉ user nào đáp ưng được policy AspManager mới được sử dụng action này
        [Authorize(Policy = "AspManager")]
        public IActionResult Project()
        {
            return View("Index", base.User?.Claims);
        }



        [Authorize(Policy = "AllowTom")]
        public ViewResult TomFiles() => View("Index", User?.Claims);


        //thay vì sử sụng Authorize chũng ta sẽ xác thực trong controller
        //bằng sự trợ giúp của IAuthorizationService 
        public async Task<IActionResult> PrivateAccess(string title)
        {
            string[] allowedUsers = { "tom", "alice" };// tạo 1 mảng chỉ các user thỏa policy

            //hàm AuthorizeAsync kiểm user có thỏa các yếu cầu của policy không
            AuthorizationResult authorized = await authService.AuthorizeAsync(User, allowedUsers, "PrivateAccess");

            if (authorized.Succeeded)
                return View("Index", User?.Claims);
            else
                return new ChallengeResult(); //ChallengeResult yêu cầu người dùng đăng nhập
        }

        void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }
    }
}
