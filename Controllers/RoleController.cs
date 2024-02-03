using Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Identity.Controllers
{
    public class RoleController : Controller
    {
        //sử dụng class IdentityRole làm đại diện cho role
        private RoleManager<IdentityRole> roleManager;
        private UserManager<AppUser> userManager;
        public RoleController(RoleManager<IdentityRole> roleMgr, UserManager<AppUser> userMrg)
        {
            roleManager = roleMgr;
            userManager = userMrg;
        }
        private void Errors(IdentityResult result)
        {
            foreach (IdentityError error in result.Errors)
                ModelState.AddModelError("", error.Description);
        }

        #region Read role
        public ViewResult Index()
        {
            //thuộc tính Roles của RoleManager cũng cấp tất cẩ các roles
            //mỗi có có kiểu dữ liệu là IdentityRole
            
            return View(roleManager.Roles);
        }
        #endregion

        #region Create Role
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create([Required] string name)
        {
            if (ModelState.IsValid)
            {
                IdentityResult result = await roleManager.CreateAsync(new IdentityRole(name));
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    Errors(result);
            }
            return View();
        }

        #endregion


        #region delete role
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            IdentityRole role = await roleManager.FindByIdAsync(id);
            if (role != null)
            {
                IdentityResult result = await roleManager.DeleteAsync(role);
                if (result.Succeeded)
                    return RedirectToAction("Index");
                else
                    Errors(result);
            }
            else
                ModelState.AddModelError("", "No role found");

            return View("Index", roleManager.Roles);
        }

        #endregion

        #region update role
        public async Task<IActionResult> Update(string id)
        {
            IdentityRole role = await roleManager.FindByIdAsync(id);
            List<AppUser> members = new List<AppUser>();
            List<AppUser> nonMembers = new List<AppUser>();
            foreach (AppUser user in userManager.Users)
            {
                var list = await userManager.IsInRoleAsync(user, role.Name) ? members : nonMembers;
                list.Add(user);
            }
            return View(new RoleEdit
            {
                Role = role,
                Members = members,
                NonMembers = nonMembers
            });
        }


        [HttpPost]
        public async Task<IActionResult> Update(RoleModification model)
        {
            IdentityResult result;
            if (ModelState.IsValid)
            {

                //duyệt danh sách các user được cấp quyển
                foreach (string userId in model.AddIds ?? new string[] { })
                {
                    //tìm kiếm user theo id
                    AppUser user = await userManager.FindByIdAsync(userId);

                    //nếu tìm thấy user thì cấp quyền đó cho user
                    if (user != null)
                    {
                        result = await userManager.AddToRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }

                //duyệt danh sách các user bị xóa quyển
                foreach (string userId in model.DeleteIds ?? new string[] { })
                {
                    //tìm kiếm user theo id
                    AppUser user = await userManager.FindByIdAsync(userId);

                    //nếu tìm thấy user thì xóa quyền đó của user
                    if (user != null)
                    {
                        result = await userManager.RemoveFromRoleAsync(user, model.RoleName);
                        if (!result.Succeeded)
                            Errors(result);
                    }
                }
            }

            if (ModelState.IsValid)
                return RedirectToAction(nameof(Index));
            else
                return await Update(model.RoleId);
        }

        #endregion
    }
}
