using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public string ReturnUrl { get; set; }

    public class InputModel
    {
      
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid || Input == null)
        {
            return Page();
        }

        var user = new ApplicationUser
        {
            FullName = Input.FullName.Trim(),
            UserName = Input.Email.Trim(),
            Email = Input.Email.Trim(),
            Role = "User"
        };
        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                var createRoleResult = await _roleManager.CreateAsync(new IdentityRole("User"));
                if (!createRoleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    foreach (var error in createRoleResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    return Page();
                }
            }

            var addToRoleResult = await _userManager.AddToRoleAsync(user, "User");
            if (!addToRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                foreach (var error in addToRoleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Profile", "User", new { area = "" });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}
