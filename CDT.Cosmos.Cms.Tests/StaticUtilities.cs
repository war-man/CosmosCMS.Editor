using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Controllers;
using CDT.Cosmos.Cms.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CDT.Cosmos.Cms.Tests
{
    public static class TestUsers
    {
        public const string Foo = "foo@foo.com";
        public const string Teamfoo1 = "teamfoo1@foo.com";
        public const string Teamfoo2 = "teamfoo2@foo.com";
    }

    public static class StaticUtilities
    {
        //private const string EditorRoleName = "Editors";
        private static IConfiguration _configuration;

        internal static IConfiguration GetConfig()
        {
            if (_configuration != null) return _configuration;

            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonFile(jsonConfig, true)
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true);  // User secrects override all - put here

            var config = builder.Build();

            // From either local secrets or app config, get connection info for Azure Vault.
            var tenantId = config["TenantId"];
            if (string.IsNullOrEmpty(tenantId)) tenantId = Environment.GetEnvironmentVariable("AzureVaultTenantId");
            var clientId = config["ClientId"];
            if (string.IsNullOrEmpty(clientId)) clientId = Environment.GetEnvironmentVariable("AzureVaultClientId");
            var key = config["Key"];
            if (string.IsNullOrEmpty(key)) key = Environment.GetEnvironmentVariable("AzureVaultKey");
            var vaultUrl = config["VaultUrl"];
            if (string.IsNullOrEmpty(vaultUrl)) vaultUrl = Environment.GetEnvironmentVariable("AzureVaultUrl");

            builder.AddAzureKeyVault(vaultUrl, clientId, key);
            _configuration = builder.Build();

            return _configuration;
        }

        public static Guid GetCacheId()
        {
            return new Guid("4dc3249c-64a9-4731-babc-fe5b2b1a7af7");
        }

        public static ApplicationDbContext GetApplicationDbContext()
        {
            var config = (ConfigurationRoot) GetConfig();
            var providerList = config.Providers.ToList();
            var localSecrets =  providerList[1];

            if (localSecrets == null || !localSecrets.TryGet("ConnectionStrings:DefaultConnection", out string connectionString))
            {
                connectionString = GetConfig().GetConnectionString("DefaultConnection");
            }

            var builder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Use this to test against SQL Server
            builder.UseSqlServer(connectionString, providerOptions => providerOptions.EnableRetryOnFailure());
            
            // Use this to test using an in-memory SQLITE database
            //builder.UseSqlite(CreateInMemoryDatabase());
            
            var context = new ApplicationDbContext(builder.Options);

            //context.Database.EnsureDeleted();
            //context.Database.EnsureCreated();

            //context.Database.Migrate();

            return context;
        }

        //private static DbConnection CreateInMemoryDatabase()
        //{
        //    var connection = new SqliteConnection("Filename=:memory:");

        //    connection.Open();

        //    return connection;
        //}

        public static ILogger<T> GetLogger<T>()
        {
            return new Logger<T>(new NullLoggerFactory());
        }

        public static IOptions<AzureBlobServiceConfig> GetAzureBlobServiceOptions()
        {
            var connectionString = GetConfig().GetConnectionString("DefaultBlobConnection");
            var blobUrl = GetConfig()["BlobServicePublicUrl"];

            return Options.Create(new AzureBlobServiceConfig
            {
                BlobServicePublicUrl = blobUrl,
                ConnectionString = connectionString
            });
        }

        public static AzureBlobService GetAzureBlobService()
        {
            var blobOptions = GetAzureBlobServiceOptions();
            var service = new AzureBlobService(blobOptions, GetLogger<AzureBlobService>());
            return service;
        }

        public static IFormFile GetFormFile(string fileName)
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes($"This is a file for {fileName}."));
            return new FormFile(stream, 0, stream.Length, fileName, fileName);
        }

        public static UserManager<IdentityUser> GetUserManager()
        {
            var userStore = new UserStore<IdentityUser>(GetApplicationDbContext());
            var userManager = new UserManager<IdentityUser>(userStore, null, new PasswordHasher<IdentityUser>(), null,
                null, null, null, null, null);
            return userManager;
        }

        public static RoleManager<IdentityRole> GetRoleManager()
        {
            var userStore = new RoleStore<IdentityRole>(GetApplicationDbContext());
            var userManager = new RoleManager<IdentityRole>(userStore, null, null, null, null);
            return userManager;
        }

        public static IEmailSender GetEmailSender()
        {
            var sendGridKey = GetConfig()["SendGridKey"];
            var emailFrom = GetConfig()["EmailFrom"];

            var authConfig = new AuthMessageSenderOptions
            {
                EmailFrom = emailFrom,
                SendGridKey = sendGridKey
            };
            var emailSender = new EmailSender(Options.Create(authConfig));
            return emailSender;
        }

        public static async Task<IdentityUser> GetIdentityUser(string emailAddress)
        {
            using var userManager = GetUserManager();
            var user = await userManager.FindByEmailAsync(emailAddress);
            if (user == null)
            {
                await userManager.CreateAsync(new IdentityUser(emailAddress)
                {
                    Email = emailAddress,
                    Id = Guid.NewGuid().ToString(),
                    EmailConfirmed = true
                });
                user = await userManager.FindByEmailAsync(emailAddress);
            }

            return user;
        }

        public static async Task<ClaimsPrincipal> GetPrincipal(string emailAddress)
        {
            var user = await GetIdentityUser(emailAddress);
            using var userManager = GetUserManager();
            var claims = await userManager.GetClaimsAsync(user);

            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));

            return principal;
        }

        public static HttpContext GetMockContext(ClaimsPrincipal user)
        {
            return new DefaultHttpContext
            {
                FormOptions = new FormOptions(),
                Items = new Dictionary<object, object>(),
                RequestAborted = default,
                RequestServices = null!,
                ServiceScopeFactory = null!,
                Session = null!,
                TraceIdentifier = null!,
                User = user
            };
        }

        public static HomeController GetHomeController(ClaimsPrincipal user)
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logger = new Logger<HomeController>(new NullLoggerFactory());
            //var blobOptions = Options.Create(new AzureBlobServiceConfig());
            var controller = new HomeController(logger,
                GetApplicationDbContext(),
                siteOptions,
                Options.Create(GetRedisContextConfig()),
                GetArticleLogic()
                )
            {
                ControllerContext = {HttpContext = GetMockContext(user)}
            };
            return controller;
        }

        public static ArticleLogic GetArticleLogic(bool readWriteModeOn = true, bool allSetupOn = true)
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = readWriteModeOn,
                AllowSetup = allSetupOn
            });
            var distributedCache = StaticUtilities.GetRedisDistributedCache();
            return new ArticleLogic(
                GetApplicationDbContext(),
                distributedCache,
                GetLogger<ArticleLogic>(),
                siteOptions,
                Options.Create(GetRedisContextConfig()),
                new TranslationServices(GetGoogleOptions()));
        }

        public static IOptions<GoogleCloudAuthConfig> GetGoogleOptions()
        {
            var googleAuthConfigSection = GetConfig().GetSection("GoogleCloudAuthConfig");
            var options = googleAuthConfigSection.Get<GoogleCloudAuthConfig>();
            return Options.Create(options);
        }

        public static EditorController GetEditorController(ClaimsPrincipal user)
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logger = new Logger<EditorController>(new NullLoggerFactory());

            var controller = new EditorController(
                logger,
                GetApplicationDbContext(),
                GetUserManager(),
                siteOptions,
                GetRedisDistributedCache(), 
                null,
                null,
                GetArticleLogic(),
                Options.Create(GetRedisContextConfig()),
                GetBlobOptions())
            {
                ControllerContext = {HttpContext = GetMockContext(user)}
            };
            return controller;
        }

        public static SetupController GetSetupController()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logger = new Logger<SetupController>(new NullLoggerFactory());
            //var blobOptions = Options.Create(new AzureBlobServiceConfig());
            var claimsPrincipal = GetPrincipal(TestUsers.Foo).Result;
            var redisConfig = Options.Create(GetRedisContextConfig());

            var controller = new SetupController(logger,
                GetApplicationDbContext(),
                GetRoleManager(),
                GetUserManager(),
                siteOptions,
                null,
                GetArticleLogic(),
                redisConfig,
                GetAzureBlobService())
            {
                ControllerContext = {HttpContext = GetMockContext(claimsPrincipal)}
            };
            return controller;
        }

        public static TeamsController GetTeamsController()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logger = new Logger<TeamsController>(new NullLoggerFactory());
            //var blobOptions = Options.Create(new AzureBlobServiceConfig());
            var claimsPrincipal = GetPrincipal(TestUsers.Foo).Result;

            var controller = new TeamsController(siteOptions, GetApplicationDbContext(), logger, GetUserManager(), GetArticleLogic())
            {
                ControllerContext = {HttpContext = GetMockContext(claimsPrincipal)}
            };
            return controller;
        }

        public static RedisCache GetRedisDistributedCache()
        {
            var redisCache = new RedisCache(GetRedisCacheOptions());
            return redisCache;
        }
        

        public static IOptions<AzureBlobServiceConfig> GetBlobOptions()
        {
            var section = GetConfig().GetSection("AzureBlobServiceConfig");

            return Options.Create(new AzureBlobServiceConfig
            {
                ConnectionString = section["ConnectionString"],
                BlobServicePublicUrl = section["BlobServicePublicUrl"]
            });
        }

        public static IOptions<RedisCacheOptions> GetRedisCacheOptions()
        {
            var contextConfig = GetRedisContextConfig();
            var redisConfigurationOptions = new ConfigurationOptions
            {
                Password = contextConfig.Password,
                Ssl = true,
                SslProtocols = SslProtocols.Tls12,
                AbortOnConnectFail = false
            };
            redisConfigurationOptions.EndPoints.Add(contextConfig.Host, 6380);
            redisConfigurationOptions.ConnectTimeout = 2000;
            redisConfigurationOptions.ConnectRetry = 3;
            var redisOptions = new RedisCacheOptions
            {
                ConfigurationOptions = redisConfigurationOptions,
                InstanceName = null
            };
            return Options.Create(redisOptions);
        }

        public static RedisContextConfig GetRedisContextConfig()
        {
            var section = GetConfig().GetSection("RedisContextConfig");

            return new RedisContextConfig
            {
                AbortConnect = false,
                CacheDuration = 120, // Default is 60
                CacheId = GetCacheId(),
                Host = section["Host"],
                Password = section["Password"],
                Port = 6380,
                Ssl = true
            };
        }

        public static FileManagerController GetFileManagerController(ClaimsPrincipal user)
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logger = new Logger<FileManagerController>(new NullLoggerFactory());

            var controller = new FileManagerController(
                logger,
                GetApplicationDbContext(),
                GetUserManager(),
                siteOptions,
                GetRedisDistributedCache(),
                GetArticleLogic(),
                GetAzureBlobServiceOptions(),
                Options.Create(GetRedisContextConfig()),
                GetAzureBlobService())
            {
                ControllerContext = {HttpContext = GetMockContext(user)}
            };
            return controller;
        }
    }
}