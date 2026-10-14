using loginlogout.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Cookie expire time
    options.SlidingExpiration = true; // Har request par expiry renew ho
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

// Add Identity services
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Add controllers and views
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseForwardedHeaders();

// Database Migration & Admin Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Roles create karein
    var roles = new[] { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the {role} role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }

    // Default Fallback values lagadi hain taake config missing hone par bhi admin ban jaye
    var adminEmail = builder.Configuration["Admin:Email"] ?? "masadkhatri05@gmail.com";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin@123";

    if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
    {
        logger.LogWarning("Admin account was not seeded. Configure Admin:Email and Admin:Password to create it.");
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
                EmailConfirmed = true,
                FullName = builder.Configuration["Admin:FullName"] ?? "Store Administrator"
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create the configured admin account: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign the Admin role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
            }
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// Routing
app.MapControllerRoute(name: "default", pattern: "{controller=Admin}/{action=Dashboard}/{id?}");

app.Run();