using System;
using System.Security.Authentication;
using CDT.Azure.CDN;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Serialization;
using StackExchange.Redis;

namespace CDT.Cosmos.Cms
{
    /// <summary>
    /// Startup class for the website.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Configuration for the website.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Method configures services for the website.
        /// </summary>
        /// <param name="services"></param>
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var azureCdnSection = Configuration.GetSection("AzureCdnConfig");
            var siteCustomizationsSection = Configuration.GetSection("SiteCustomizations");
            var redisSection = Configuration.GetSection("RedisContextConfig");

            var siteCustomConfig = siteCustomizationsSection.Get<SiteCustomizationsConfig>();
            services.Configure<SiteCustomizationsConfig>(siteCustomizationsSection);

            // Add Redis Cache Service here
            services.AddTransient<RedisCacheService>();

            // Google Translation Services
            services.AddTransient<TranslationServices>();

            // Get CDN Configuration
            var azureCdnConfig = siteCustomizationsSection.Get<AzureCdnConfig>();
            if (azureCdnConfig != null)
                services.Configure<AzureCdnConfig>(azureCdnSection);
            else
                // Akamai premium.
                services.Configure<AkamaiContextConfig>(Configuration.GetSection("AkamaiContextConfig"));

            services.Configure<AzureBlobServiceConfig>(Configuration.GetSection("AzureBlobServiceConfig"));
            services.Configure<AuthMessageSenderOptions>(Configuration.GetSection("AuthMessageSenderOptions"));

            // For Google Translator Services
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "CA-Response-Portal-bfc617b86937.json");

            services.AddTransient<AzureBlobService>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
            services.ConfigureApplicationCookie(o =>
            {
                o.ExpireTimeSpan = TimeSpan.FromDays(5);
                o.SlidingExpiration = true;
            });


            // Add this before identity
            services.AddControllersWithViews();
            services.AddRazorPages();

            try
            {
                //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1

                var redisContext = redisSection.Get<RedisContextConfig>();

                services.Configure<RedisContextConfig>(redisSection);
                var redisOptions = new ConfigurationOptions
                {
                    Password = redisContext.Password,
                    Ssl = true,
                    SslProtocols = SslProtocols.Tls12,
                    AbortOnConnectFail = redisContext.AbortConnect
                };

                redisOptions.EndPoints.Add(redisContext.Host, 6380);
                redisOptions.ConnectTimeout = 2000;
                redisOptions.ConnectRetry = 5;
                services.AddStackExchangeRedisCache(options => { options.ConfigurationOptions = redisOptions; });
                services.AddResponseCaching(options => { options.UseCaseSensitivePaths = false; });
            }
            catch
            {
                // Nothing to do right now, let REDIS fail
                // var t = "E";
            }

            if (siteCustomConfig.UseAzureSignalR)
                // See: https://github.com/aspnet/AzureSignalR-samples/tree/master/samples/ChatRoom
                services.AddSignalR().AddAzureSignalR();
            else
                services.AddSignalR();
            // End before identity

            // See: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
            // And see: https://stackoverflow.com/questions/46320189/asp-net-core-2-unable-to-resolve-service-for-type-microsoft-entityframeworkcore
            // requires
            // using Microsoft.AspNetCore.Identity.UI.Services;
            // using WebPWrecover.Services;
            // Setup SendGrid as EmailSender: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
            // requires
            // using Microsoft.AspNetCore.Identity.UI.Services;
            // using WebPWrecover.Services;
            services.AddTransient<IEmailSender, EmailSender>();

            var authConfig = Configuration.GetSection("Authentication");
            var config = authConfig.Get<AuthenticationConfig>();
            services.Configure<AuthenticationConfig>(authConfig);

            // https://forums.asp.net/t/2130410.aspx?Roles+and+RoleManager+in+ASP+NET+Core+2
            services.AddIdentity<IdentityUser, IdentityRole>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddUserManager<UserManager<IdentityUser>>()
                .AddRoleManager<RoleManager<IdentityRole>>()
                .AddDefaultTokenProviders();

            //
            // Configure authentication providers.
            //
            if (config.Microsoft != null && !string.IsNullOrEmpty(config.Microsoft.ClientId))
                // Microsoft
                services.AddAuthentication().AddMicrosoftAccount(microsoftOptions =>
                {
                    // Microsoft https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/microsoft-logins?view=aspnetcore-3.1
                    microsoftOptions.ClientId = config.Microsoft.ClientId;
                    microsoftOptions.ClientSecret = config.Microsoft.ClientSecret;
                });

            if (config.Google != null && !string.IsNullOrEmpty(config.Google.ClientId))
                services.AddAuthentication()
                    .AddGoogle(options =>
                    {
                        // Google https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-3.1#create-a-google-api-console-project-and-client-id
                        // Dashboard https://console.developers.google.com/projectselector2/apis/dashboard?authuser=0&organizationId=0&supportedpurview=project
                        // On dashboard, on menu left, click "Credentials." It will be one of the "Oauth" settings.

                        options.ClientId = config.Google.ClientId;
                        options.ClientSecret = config.Google.ClientSecret;
                    });

            services.AddKendo();

            // Need to add this for Telerik C:\Users\eric\source\repos\response.ca.gov.v2\response.ca.gov.v2\Views\Test\Widgets.cshtml
            // https://docs.telerik.com/aspnet-core/getting-started/prerequisites/environment-support#json-serialization
            services.AddMvc()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ContractResolver =
                        new DefaultContractResolver())
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddRazorPagesOptions(options =>
                {
                    if (siteCustomConfig.ReadWriteMode)
                    {
                        // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                        //options.AllowAreas = true;
                        options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                        options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                    }
                });

            services.ConfigureApplicationCookie(options =>
            {
                if (siteCustomConfig.ReadWriteMode)
                {
                    // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                    options.LoginPath = "/Identity/Account/Login";
                    options.LogoutPath = "/Identity/Account/Logout";
                    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                }
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="lifetime"></param>
        /// <param name="cache"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime,
            IDistributedCache cache)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            try
            {
                lifetime.ApplicationStarted.Register(() =>
                {
                    var options = new DistributedCacheEntryOptions();
                    //var redisCache = (Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache)cache;
                    //cache.Set("cachedTimeUTC", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("u")), options);
                });
            }
            catch //(Exception e)
            {
                //var x = e; // Nothing to do, capture for debugging.
            }

            app.UseResponseCaching(); //https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "MyArea",
                    "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                endpoints.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();

                // This route must go last.  A page name can't conflict with any of the above.
                // This route allows page titles to become URLs.
                endpoints.MapControllerRoute("DynamicPage", "/{id?}/{lang?}", new {controller = "Home", action = "Index"});
            });
        }
    }
}