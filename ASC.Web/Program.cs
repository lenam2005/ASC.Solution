using ASC.Web.Configuration;
using ASC.Web.Data;
using ASC.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services
    .AddConfig(builder.Configuration)
    .AddMyDependencyGroup();

var app = builder.Build();

// Seed dữ liệu roles/users từ appsettings.json
using (var scope = app.Services.CreateScope())
{
    var storageSeed = scope.ServiceProvider.GetRequiredService<IIdentitySeed>();

    await storageSeed.Seed(
        scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
        scope.ServiceProvider.GetRequiredService<IOptions<ApplicationSettings>>()
    );
}
//CreateNavigationCache
using (var scope = app.Services.CreateScope())
{
    var navigationCacheOperations = scope.ServiceProvider.GetRequiredService<INavigationCacheOperations>();
    await navigationCacheOperations.CreateNavigationCacheAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Route cho Area
app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();


//using ASC.Web.Configuration;
//using ASC.Web.Data;
//using ASC.Web.Services;
//using ASC.DataAccess;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
//    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(connectionString));

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

//builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//// Cấu hình Identity có Role
//builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
//{
//    options.User.RequireUniqueEmail = true;
//    options.SignIn.RequireConfirmedAccount = false; // hoặc true nếu bài yêu cầu xác nhận email
//})
//.AddEntityFrameworkStores<ApplicationDbContext>()
//.AddDefaultTokenProviders();

//builder.Services.AddOptions();
//builder.Services.Configure<ApplicationSettings>(
//    builder.Configuration.GetSection("AppSettings"));
//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession();

//builder.Services.AddControllersWithViews();
//builder.Services.AddRazorPages();

//// Add application services
//builder.Services.AddTransient<IEmailSender, AuthMessageSender>();
//builder.Services.AddTransient<ISmsSender, AuthMessageSender>();

//// Đăng ký DbContext cho repository nếu cần
//builder.Services.AddScoped<DbContext, ApplicationDbContext>();

//// Đăng ký seed và UnitOfWork
//builder.Services.AddSingleton<IIdentitySeed, IdentitySeed>();
////builder.Services.AddScoped<IIdentitySeed, IdentitySeed>();
//builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//var app = builder.Build();

//// Seed dữ liệu roles/users từ appsettings.json
//using (var scope = app.Services.CreateScope())
//{
//    var storageSeed = scope.ServiceProvider.GetRequiredService<IIdentitySeed>();

//    await storageSeed.Seed(
//        scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>(),
//        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
//        scope.ServiceProvider.GetRequiredService<IOptions<ApplicationSettings>>()
//    );
//}

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseMigrationsEndPoint();
//}
//else
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();
//app.UseSession();

//// PHẢI có Authentication trước Authorization
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapRazorPages();

//app.Run();