using loginlogout.Areas.Identity.Data;
using loginlogout.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace loginlogout.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminProfileController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return View(new AdminProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(AdminProfileViewModel profile)
        {
            if (!ModelState.IsValid)
            {
                await PopulateProfilePageAsync(profile);
                return View("Index", profile);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = profile.FullName.Trim();
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddIdentityErrors(updateResult);
                await PopulateProfilePageAsync(profile);
                return View("Index", profile);
            }

            var phoneNumber = string.IsNullOrWhiteSpace(profile.PhoneNumber)
                ? null
                : profile.PhoneNumber.Trim();
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, phoneNumber);
            if (!phoneResult.Succeeded)
            {
                AddIdentityErrors(phoneResult);
                await PopulateProfilePageAsync(profile);
                return View("Index", profile);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["profileupdated"] = "Your admin profile was updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(AdminPasswordViewModel password)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await PopulateProfilePageAsync(new AdminProfileViewModel
                {
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email ?? string.Empty
                });
                return View("Index", new AdminProfileViewModel
                {
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email ?? string.Empty
                });
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                password.CurrentPassword,
                password.NewPassword);

            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                await PopulateProfilePageAsync(new AdminProfileViewModel
                {
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email ?? string.Empty
                });
                return View("Index", new AdminProfileViewModel
                {
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email ?? string.Empty
                });
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["passwordupdated"] = "Your password was changed successfully.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateProfilePageAsync(AdminProfileViewModel profile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return;
            }

            profile.Email = user.Email ?? string.Empty;
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
