using ASC.DataAccess;
using ASC.Web.Configuration;
using ASC.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ASC.Web.Services
{
    public static class DependencyInjection
    {
        // Cấu hình DbContext, AppSettings, Session
        public static IServiceCollection AddConfig(this IServiceCollection services, IConfiguration config)
        {
            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddOptions();
            services.Configure<ApplicationSettings>(config.GetSection("AppSettings"));

            services.AddDistributedMemoryCache();
            services.AddSession();

            return services;
        }

        // Đăng ký Identity, services, MVC
        public static IServiceCollection AddMyDependencyGroup(this IServiceCollection services)
        {
            // DbContext inject cho repository nếu cần
            services.AddScoped<DbContext, ApplicationDbContext>();

            // Identity có Role
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Application services
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            // Seed + UnitOfWork
            services.AddScoped<IIdentitySeed, IdentitySeed>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Adđ Cache ,Session
            services.AddSession();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddDistributedMemoryCache(); //
            services.AddSingleton<INavigationCacheOperations, NavigationCacheOperations>();

            // MVC + Razor Pages
            services.AddRazorPages();
            services.AddControllersWithViews();
            services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }
    }
}