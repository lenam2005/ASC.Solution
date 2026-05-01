using ASC.DataAccess;
using ASC.Web.Configuration;
using ASC.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using ASC.Business.Interfaces;
using ASC.Business;

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

        // Đăng ký Identity + Google + Services
        public static IServiceCollection AddMyDependencyGroup(this IServiceCollection services, IConfiguration config)
        {
            // DbContext inject cho repository nếu cần
            services.AddScoped<DbContext, ApplicationDbContext>();

            // Identity
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            // Google Auth
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId = config["Google:Identity:ClientId"];
                    options.ClientSecret = config["Google:Identity:ClientSecret"];
                    options.SignInScheme = IdentityConstants.ExternalScheme;
                });
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = config.GetSection("CacheSettings:CacheConnectionString").Value;
                options.InstanceName = config.GetSection("CacheSettings:CacheInstance").Value;
            });

            // Application services
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            // Seed + UnitOfWork
            services.AddScoped<IIdentitySeed, IdentitySeed>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Add MasterDataOperations
            services.AddScoped<IMasterDataOperations, MasterDataOperations>();
            services.AddScoped<IServiceRequestOperations, ServiceRequestOperations>();

            // AutoMapper
            services.AddAutoMapper(typeof(ApplicationDbContext));

            // Session + Cache
            services.AddSession();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<INavigationCacheOperations, NavigationCacheOperations>();

            // MVC + Razor Pages
            services.AddRazorPages();
            //services.AddControllersWithViews();
            // MVC + Razor Pages
            services.AddRazorPages();

            services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddDatabaseDeveloperPageExceptionFilter();

            return services;
        }
    }
}