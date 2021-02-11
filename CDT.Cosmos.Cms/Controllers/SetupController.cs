using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Services;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CDT.Cosmos.Cms.Controllers
{
    public class SetupController : BaseController
    {
        private readonly AzureBlobService _blobService;
        //private readonly EmailSender _emailSender;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SetupController(ILogger<SetupController> logger,
            ApplicationDbContext dbContext,
            RoleManager<IdentityRole> roleManager,
            UserManager<IdentityUser> userManager,
            IOptions<SiteCustomizationsConfig> options,
            IDistributedCache distributedCache,
            ArticleLogic articleLogic,
            IOptions<RedisContextConfig> redisOptions,
            AzureBlobService blobService) :
            base(options, dbContext, logger, userManager, articleLogic, distributedCache, redisOptions)
        {
            _roleManager = roleManager;
            //_emailSender = (EmailSender) emailSender;
            _blobService = blobService;
        }

        public async Task<IActionResult> Index()
        {
            if (SiteOptions.Value.ReadWriteMode && SiteOptions.Value.AllowSetup)
                try
                {
                    //
                    // Double check that there are NO administrators defined yet.
                    //
                    var admins = await UserManager.GetUsersInRoleAsync("Administrators");
                    if (admins != null && admins.Count > 0)
                        // Site is already setup, don't run again.
                        return RedirectToAction("Index", "Home");

                    var iLib = new IconLibrary(DbContext);
                    await iLib.Ensure_FontIconLibraryLoaded();

                    if (!await _roleManager.RoleExistsAsync("Editors"))
                        await _roleManager.CreateAsync(new IdentityRole("Editors"));

                    if (!await _roleManager.RoleExistsAsync("Authors"))
                        await _roleManager.CreateAsync(new IdentityRole("Authors"));

                    if (!await _roleManager.RoleExistsAsync("Reviewers"))
                        await _roleManager.CreateAsync(new IdentityRole("Reviewers"));

                    if (!await _roleManager.RoleExistsAsync("Team Members"))
                        await _roleManager.CreateAsync(new IdentityRole("Team Members"));

                    //
                    // Add current user as the first administrator.
                    //

                    if (!await _roleManager.RoleExistsAsync("Administrators"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Administrators"));
                        var user = await UserManager.GetUserAsync(User);
                        var result = await UserManager.AddToRoleAsync(user, "Administrators");
                        if (!result.Succeeded)
                        {
                            foreach (var identityError in result.Errors) Logger.LogError(identityError.Description);
                            throw new Exception($"Could not add user '{User.Identity?.Name}' as administrator.");
                        }
                    }

                    //
                    // If we can do all this setup, then we are definitely connected to the database.
                    //

                    return RedirectToAction("FinishSetup");
                }
                catch (Exception e)
                {
                    Logger.LogError(e, e.Message);
                }

            return Unauthorized();
        }

        public IActionResult FinishSetup()
        {
            if (SiteOptions.Value.ReadWriteMode && SiteOptions.Value.AllowSetup) return View();

            return Unauthorized();
        }

        [HttpGet]
        [Authorize(Roles = "Administrators")]
        public IActionResult TrainingReset()
        {
            if (SiteOptions.Value.ReadWriteMode && SiteOptions.Value.AllowReset) return View();

            return Unauthorized();
        }

        [HttpPost]
        [Authorize(Roles = "Administrators")]
        public async Task<IActionResult> TrainingReset(bool reset)
        {
            if (SiteOptions.Value.ReadWriteMode && SiteOptions.Value.AllowReset)
            {
                if (!reset) return View();

                DbContext.ArticleLogs.RemoveRange(DbContext.ArticleLogs.ToList());
                DbContext.Articles.RemoveRange(DbContext.Articles.ToList());
                DbContext.Users.RemoveRange(DbContext.Users.ToList());
                DbContext.Roles.RemoveRange(DbContext.Roles.ToList());

                await DbContext.SaveChangesAsync();

                await _blobService.Destroy("", new FileBrowserEntry
                {
                    EntryType = FileBrowserEntryType.Directory,
                    Name = "",
                    Size = 0
                });

                return RedirectToAction("ResetComplete");
            }

            return Unauthorized();
        }

        [HttpGet]
        public IActionResult ResetComplete()
        {
            if (SiteOptions.Value.ReadWriteMode && SiteOptions.Value.AllowReset) return View();

            return Unauthorized();
        }
    }
}