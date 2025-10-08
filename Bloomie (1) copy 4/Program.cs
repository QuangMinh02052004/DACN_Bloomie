using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Bloomie.Data;
using Bloomie.Services.Implementations;
using Bloomie.Services.Interfaces;
using Bloomie.Models.Entities;
using Bloomie.Areas.Admin.Models;
using Bloomie.Middleware;
using Bloomie.Models.Momo;
using OfficeOpenXml;
using Python.Runtime;
using Bloomie.Hubs;
// THÊM CÁC USING DIRECTIVES CÒN THIẾU
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

// Connect MomoAPI
builder.Services.Configure<MomoOptionModel>(builder.Configuration.GetSection("MomoAPI"));
builder.Services.AddScoped<IMomoService, MomoService>();

// Cấu hình logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// Cấu hình Email Service
builder.Services.AddTransient<IEmailService, EmailService>();

// Cấu hình Session và Cache
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cấu hình Controllers và Views
builder.Services.AddControllersWithViews();

// CẤU HÌNH DATA PROTECTION
var keysDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys");
Directory.CreateDirectory(keysDirectory);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("BloomieApp")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// CẤU HÌNH ANTIFORGERY
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = ".Bloomie.CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Cấu hình Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(60);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
});

// CẤU HÌNH APPLICATION COOKIE
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".Bloomie.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// CẤU HÌNH XÁC THỰC - ĐÃ SỬA GOOGLE OAUTH
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["GoogleKeys:ClientId"];
    options.ClientSecret = builder.Configuration["GoogleKeys:ClientSecret"];
    // Sử dụng callback URL đơn giản
    options.CallbackPath = "/signin-google";
    // Thêm các scope cần thiết
    options.Scope.Add("profile");
    options.Scope.Add("email");
    // Lưu access token và refresh token
    options.SaveTokens = true;
    // Cấu hình authorization endpoint
    options.AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    options.TokenEndpoint = "https://oauth2.googleapis.com/token";
    options.UserInformationEndpoint = "https://www.googleapis.com/oauth2/v2/userinfo";
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.AppId = builder.Configuration["FacebookKeys:AppId"];
    facebookOptions.AppSecret = builder.Configuration["FacebookKeys:AppSecret"];
    facebookOptions.CallbackPath = "/signin-facebook";
})
.AddTwitter(twitterOptions =>
{
    twitterOptions.ConsumerKey = builder.Configuration["TwitterKeys:ClientId"];
    twitterOptions.ConsumerSecret = builder.Configuration["TwitterKeys:ClientSecret"];
    twitterOptions.CallbackPath = "/signin-twitter";
});

// Các services khác
builder.Services.AddHttpClient("GHN", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["GHN:BaseUrl"]);
    client.DefaultRequestHeaders.Add("Token", builder.Configuration["GHN:ApiToken"]);
});

builder.Services.AddScoped<IGHNService, GHNService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
builder.Services.AddScoped<IVnPayService, VnPayService>();

var app = builder.Build();

// MIDDLEWARE XỬ LÝ ANTIFORGERY ERRORS
app.Use(async (context, next) =>
{
    var antiforgery = context.RequestServices.GetService<IAntiforgery>();
    if (antiforgery != null)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (Exception ex)
        {
            var logger = context.RequestServices.GetService<ILogger<Program>>();
            logger?.LogWarning(ex, "Antiforgery validation failed");
            context.Response.Cookies.Delete(".Bloomie.CSRF");
        }
    }
    await next();
});

// Tạo roles và admin account
using (var scope = app.Services.CreateScope())
{
    // Clean up old keys
    var keysDir = Path.Combine(Directory.GetCurrentDirectory(), "DataProtectionKeys");
    if (Directory.Exists(keysDir))
    {
        var oldKeys = Directory.GetFiles(keysDir, "*.xml");
        foreach (var keyFile in oldKeys)
        {
            try { File.Delete(keyFile); } catch { }
        }
    }

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // Tạo roles
    string[] roles = new[] { "Admin", "User", "Manager", "Staff" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Tạo admin account
    string adminEmail = "admin@bloomie.com";
    string adminPassword = "Admin@123";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            FullName = "Administrator",
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// ENDPOINT RESET AUTH
app.MapGet("/reset-auth", async (HttpContext context) =>
{
    context.Response.Cookies.Delete(".Bloomie.Auth");
    context.Response.Cookies.Delete(".AspNetCore.Identity.Application");
    context.Response.Cookies.Delete(".AspNetCore.Antiforgery");
    context.Response.Cookies.Delete(".Bloomie.CSRF");

    var signInManager = context.RequestServices.GetService<SignInManager<ApplicationUser>>();
    if (signInManager != null)
    {
        await signInManager.SignOutAsync();
    }

    return Results.Redirect("/Account/Login?reset=success");
});

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.UseUserAccessLogging();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<NotificationHub>("/notificationHub");
    endpoints.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();