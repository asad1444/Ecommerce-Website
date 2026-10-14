using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews(options =>
{
    // Agar aap saare controllers ko secure karna chahte hain toh theek hai, 
    // lekin Identity pages ko block na karein iske liye ye global filter carefully use hota hai.
    options.Filters.Add(new AuthorizeFilter());
});
builder.Services.AddRazorPages();

builder.Services.AddSession();
builder.Services.AddDistributedMemoryCache();

// Development Email sender
builder.Services.AddSingleton<IEmailSender, DummyEmailSender>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 1. Routing sabse pehle aayega
app.UseRouting();

// 2. Session routing ke baad aur Authentication se pehle
app.UseSession();

// 3. Authentication & Authorization MUST be in this order
app.UseAuthentication();
app.UseAuthorization();

// 4. Endpoints & Mappings
app.MapGet("/", context =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles and admin before the server starts handling requests.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException($"Unable to create the {role} role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    var adminEmail = builder.Configuration["Admin:Email"] ?? "masadkhatri05@gmail.com";
    var adminPassword = builder.Configuration["Admin:Password"];
    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        app.Logger.LogWarning("Admin account was not seeded. Configure Admin:Email and Admin:Password.");
    }
    else
    {
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = builder.Configuration["Admin:FullName"] ?? "Admin User",
                Role = "Admin",
                EmailConfirmed = true
            };

        var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create the configured admin account: {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }

        if (adminUser.Role != "Admin")
        {
            adminUser.Role = "Admin";
            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to update the configured admin account role: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to assign the Admin role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}

app.Run();

// DummyEmailSender class
public class DummyEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"Email to {email} with subject {subject}");
        return Task.CompletedTask;
    }
}