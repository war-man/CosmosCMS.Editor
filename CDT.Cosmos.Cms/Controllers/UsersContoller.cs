using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Controllers
{
    // See: https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/areas?view=aspnetcore-3.1
    [Authorize(Roles = "Administrators")]
    public class UsersController : BaseController
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UsersController> _logger;
        private readonly IOptions<SiteCustomizationsConfig> _options;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public UsersController(UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<UsersController> logger,
            IOptions<SiteCustomizationsConfig> options, ApplicationDbContext dbContext,
            IDistributedCache distributedCache,
            IOptions<RedisContextConfig> redisOptions) :
            base(options, dbContext, logger, userManager, distributedCache, redisOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _options = options;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            if (_options.Value.ReadWriteMode)
            {
                // Make sure the roles exist for users.
                // var oldRoleList = (await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(id))).ToList();
                if (_options.Value.AllowSetup)
                {
                    var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                    if (allRoles.Count == 0)
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Authors"));
                        await _roleManager.CreateAsync(new IdentityRole("Editors"));
                        await _roleManager.CreateAsync(new IdentityRole("Reviewers"));
                        await _roleManager.CreateAsync(new IdentityRole("Administrators"));
                    }

                    if (!User.IsInRole("Administrators"))
                    {
                        var user = await _userManager.FindByEmailAsync(User.Identity.Name);
                        var result = await _userManager.AddToRoleAsync(user, "Administrators");
                        if (!result.Succeeded)
                        {
                            var builder = new StringBuilder();
                            foreach (var identityError in result.Errors)
                                builder.AppendLine(
                                    $"Error code: {identityError.Code}. Description: {identityError.Description}");

                            throw new Exception(builder.ToString());
                        }
                    }
                }

                var roles = await _dbContext.Roles.ToListAsync();
                var usersAndRoles = await _dbContext.UserRoles.ToListAsync();
                var users = await _dbContext.Users.ToListAsync();
                var userLogins = await _dbContext.UserLogins.ToListAsync();

                var queryUsersAndRoles = (from ur in usersAndRoles
                    join r in roles on ur.RoleId equals r.Id
                    select new
                    {
                        ur.UserId,
                        RoleName = r.Name
                    }).ToList();

                var model = new List<UsersIndexViewModel>();

                foreach (var user in users)
                {
                    var role = queryUsersAndRoles.FirstOrDefault(f => f.UserId == user.Id);
                    var login = userLogins.FirstOrDefault(f => f.UserId == user.Id);

                    model.Add(new UsersIndexViewModel
                    {
                        UserId = user.Id,
                        EmailConfirmed = user.EmailConfirmed,
                        EmailAddress = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Role = role == null ? "No Role" : role.RoleName,
                        LoginProvider = login == null ? "Local Acct." : login.ProviderDisplayName
                    });
                }

                return View(model.ToList());
            }

            return Unauthorized();
        }

        public async Task<IActionResult> RoleMembership(string id)
        {
            if (_options.Value.ReadWriteMode)
            {
                var user = await _userManager.FindByIdAsync(id);

                if (user == null)
                    return NotFound();

                ViewData["saved"] = null;

                var roleList = (await _userManager.GetRolesAsync(await _userManager.FindByIdAsync(id))).ToList();

                var model = new UserRolesViewModel
                {
                    UserEmailAddress = user.Email,
                    UserId = user.Id
                };

                if (roleList.Any(a => a.Contains("Administrator")))
                    model.Administrator = true;
                else if (roleList.Any(a => a.Contains("Editors")))
                    model.Editor = true;
                else if (roleList.Any(a => a.Contains("Authors")))
                    model.Author = true;
                else if (roleList.Any(a => a.Contains("Reviewers")))
                    model.Reviewer = true;
                else if (roleList.Any(a => a.Contains("Team Members")))
                    model.TeamMember = true;
                else
                    model.NoRole = true;

                return View(model);
            }

            return Unauthorized();
        }

        [HttpPost]
        public async Task<IActionResult> RoleMembership(UserRolesViewModel model)
        {
            if (_options.Value.ReadWriteMode)
            {
                if (model == null)
                    return RedirectToAction("Index");

                if (ModelState.IsValid)
                {
                    ViewData["saved"] = null;

                    var user = await _userManager.FindByIdAsync(model.UserId);

                    var roleList = await _userManager.GetRolesAsync(user);

                    if (await _userManager.IsInRoleAsync(user, "Administrators"))
                    {
                        var admins = await _userManager.GetUsersInRoleAsync("Administrators");
                        if (admins.Count == 1)
                        {
                            ModelState.AddModelError("UserRole",
                                "Cannot change permissions of the last administrator.");
                            return View(model);
                        }
                    }

                    var result = await _userManager.RemoveFromRolesAsync(user, roleList);

                    if (result.Succeeded)
                    {
                        switch (model.UserRole)
                        {
                            case "Administrator":
                                result = await _userManager.AddToRoleAsync(user, "Administrators");
                                break;
                            case "Editor":
                                result = await _userManager.AddToRoleAsync(user, "Editors");
                                break;
                            case "Author":
                                result = await _userManager.AddToRoleAsync(user, "Authors");
                                break;
                            case "Reviewer":
                                result = await _userManager.AddToRoleAsync(user, "Reviewers");
                                break;
                            case "TeamMember":
                                // Team member role is a late addition, existing sites may not have this yet.
                                if (!await _roleManager.RoleExistsAsync("Team Members"))
                                    await _roleManager.CreateAsync(new IdentityRole("Team Members"));
                                result = await _userManager.AddToRoleAsync(user, "Team Members");
                                break;
                            case "RemoveAccount":
                                result = await _userManager.DeleteAsync(user);
                                if (result.Succeeded)
                                    return RedirectToAction("Index");
                                break;
                        }

                        if (result.Succeeded)
                        {
                            roleList = await _userManager.GetRolesAsync(user);

                            model = new UserRolesViewModel
                            {
                                UserEmailAddress = user.Email,
                                UserId = user.Id
                            };

                            if (roleList.Any(a => a.Contains("Administrator")))
                                model.Administrator = true;
                            else if (roleList.Any(a => a.Contains("Editors")))
                                model.Editor = true;
                            else if (roleList.Any(a => a.Contains("Authors")))
                                model.Author = true;
                            else if (roleList.Any(a => a.Contains("Reviewers")))
                                model.Reviewer = true;
                            else if (roleList.Any(a => a.Contains("Team Members")))
                                model.TeamMember = true;
                            else
                                model.NoRole = true;
                            ViewData["saved"] = true;

                            model.UserRole = string.Empty;
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                                ModelState.AddModelError("UserEmailAddress",
                                    $"Code: {error.Code}. Description: {error.Description}");
                        }
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                            ModelState.AddModelError("UserEmailAddress",
                                $"Code: {error.Code}. Description: {error.Description}");
                    }
                }

                return View(model);
            }

            return Unauthorized();
        }

        public async Task<IActionResult> Roles()
        {
            if (_options.Value.ReadWriteMode) return View(await _roleManager.Roles.OrderBy(o => o.Name).ToListAsync());

            return Unauthorized();
        }

        [AllowAnonymous]
        public new async Task<IActionResult> SignOut()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}