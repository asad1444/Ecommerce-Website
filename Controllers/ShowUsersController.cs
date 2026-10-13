using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Controller;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ShowUsersController : Controller
    {
        private readonly ApplicationDbContext context;
        private readonly UserManager<ApplicationUser> userManager;

        public ShowUsersController(ApplicationDbContext context,UserManager<ApplicationUser> userManager)
        {
            this.context = context;
            this.userManager = userManager;
        }

        public IActionResult ShowUsers()
        {

            var users = context.Users.ToList();
            return View(users);
        }
        [HttpPost]
        public IActionResult Remove(string id)
        {
            var deleteuser = context.Users.Find(id);
            if(deleteuser != null)
            {
                context.Users.Remove(deleteuser);
                TempData["deleteuser"] = "User Deleted Successfully";
                context.SaveChanges();
                return RedirectToAction("ShowUsers");
            }
            return View();
        }


    }
}
