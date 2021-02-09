using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Controllers;
using CDT.Cosmos.Cms.Models;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Tests
{
    [TestClass]
    public class A04TemplatesControllerTests
    {
        //private const string AdminRoleName = "Administrators";
        private const string EditorRoleName = "Editors";

        private static ApplicationDbContext _dbContext;
        //private const string AuthorRoleName = "Authors";

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            _dbContext = StaticUtilities.GetApplicationDbContext();

            _dbContext.Templates.RemoveRange(_dbContext.Templates.ToList());
            _dbContext.SaveChanges();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _dbContext.Dispose();
        }

        private TemplatesController GetTemplatesController()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logger = new Logger<TemplatesController>(new NullLoggerFactory());
            var controller = new TemplatesController(
                logger,
                _dbContext,
                siteOptions,
                GetUserManager(),
                null,
                Options.Create(StaticUtilities.GetRedisContextConfig()))
            {
                ControllerContext = {HttpContext = GetMockContext().Result}
            };
            return controller;
        }

        private UserManager<IdentityUser> GetUserManager()
        {
            var userStore = new UserStore<IdentityUser>(_dbContext);
            var userManager = new UserManager<IdentityUser>(userStore, null, new PasswordHasher<IdentityUser>(), null,
                null, null, null, null, null);
            return userManager;
        }

        private async Task<ClaimsPrincipal> GetFoo()
        {
            var userManager = GetUserManager();
            var user = await userManager.FindByEmailAsync("foo@foo.com");
            if (user == null)
            {
                await userManager.CreateAsync(new IdentityUser("foo@foo.com")
                {
                    Email = "foo@foo.com",
                    Id = Guid.NewGuid().ToString(),
                    EmailConfirmed = true
                });
                user = await userManager.FindByEmailAsync("foo@foo.com");
                await userManager.AddToRoleAsync(user, EditorRoleName);
            }

            var claims = await userManager.GetClaimsAsync(user);

            claims.Add(new Claim(ClaimTypes.Name, user.UserName));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Basic"));

            return principal;
        }

        private async Task<HttpContext> GetMockContext()
        {
            var user = await GetFoo();

            return new DefaultHttpContext
            {
                FormOptions = new FormOptions(),
                Items = new Dictionary<object, object>(),
                RequestAborted = default,
                RequestServices = null,
                ServiceScopeFactory = null,
                Session = null,
                TraceIdentifier = null,
                User = user
            };
        }

        [TestMethod]
        public async Task A01_CreateTemplate()
        {
            using var controller = GetTemplatesController();
            //var dataSourceRequest = new DataSourceRequest
            //{
            //    Aggregates = null,
            //    Filters = null,
            //    GroupPaging = false,
            //    Groups = null,
            //    IncludeSubGroupCount = false,
            //    Page = 0,
            //    PageSize = 0,
            //    Skip = 0,
            //    Sorts = null,
            //    Take = 0
            //};

            var templates = new List<Template>
            {
                new Template {Id = 0, Title = "Template 1", Description = LoremIpsum.SubSection1},
                new Template {Id = 0, Title = "Template 2", Description = LoremIpsum.SubSection2},
                new Template {Id = 0, Title = "Template 3", Description = LoremIpsum.SubSection3}
            };

            _dbContext.Templates.AddRange(templates);
            await _dbContext.SaveChangesAsync();

            var redirectToActionResult = (RedirectToActionResult) await controller.Create();

            Assert.IsInstanceOfType(redirectToActionResult, typeof(RedirectToActionResult));

            var entities = await _dbContext.Templates.ToListAsync();

            Assert.AreEqual(4, entities.Count);
        }

        [TestMethod]
        public async Task A02_ReadAndUpdateTemplate()
        {
            using var controller = GetTemplatesController();
            var dataSourceRequest = new DataSourceRequest
            {
                Aggregates = null,
                Filters = null,
                GroupPaging = false,
                Groups = null,
                IncludeSubGroupCount = false,
                Page = 0,
                PageSize = 0,
                Skip = 0,
                Sorts = null,
                Take = 0
            };

            var jsonResult = (JsonResult) await controller.Templates_Read(dataSourceRequest);
            Assert.IsInstanceOfType(jsonResult, typeof(JsonResult));
            var newModel = (DataSourceResult) jsonResult.Value;
            var templates = ((IEnumerable<TemplateIndexViewModel>) newModel.Data).ToList();

            for (var i = 0; i < templates.Count; i++)
            {
                var t1 = templates[i];
                t1.Title = t1.Title + "-" + i;
                t1.Description = t1.Description + "-" + i;
            }

            await controller.Templates_Update(dataSourceRequest, templates);

            var entities = await _dbContext.Templates.ToListAsync();

            Assert.AreEqual(4, entities.Count);

            for (var i = 0; i < entities.Count; i++)
            {
                var t1 = templates[i];
                var t2 = entities[i];

                Assert.AreNotEqual(t1.Description, t2.Description);
                Assert.AreNotEqual(t1.Title, t2.Title);
            }
        }

        [TestMethod]
        public async Task A03_EditTemplateContent()
        {
            var entities = await _dbContext.Templates.ToListAsync();

            using var templatesController = GetTemplatesController();
            //var dataSourceRequest = new DataSourceRequest
            //{
            //    Aggregates = null,
            //    Filters = null,
            //    GroupPaging = false,
            //    Groups = null,
            //    IncludeSubGroupCount = false,
            //    Page = 0,
            //    PageSize = 0,
            //    Skip = 0,
            //    Sorts = null,
            //    Take = 0
            //};

            for (var i = 0; i < entities.Count; i++)
            {
                var id = entities[i].Id;
                var viewResult = (ViewResult) await templatesController.EditCode(id);
                var model = (TemplateCodeEditorViewModel) viewResult.Model;
                switch (i)
                {
                    case 0:
                        model.Content = LoremIpsum.SubSection5;
                        break;
                    case 1:
                        model.Content = LoremIpsum.SubSection6;
                        break;
                    case 2:
                        model.Content = LoremIpsum.SubSection7;
                        break;
                }

                var viewResult2 = (ViewResult) await templatesController.EditCode(model.Id);
                var model2 = (TemplateCodeEditorViewModel) viewResult2.Model;

                var template = await _dbContext.Templates.FindAsync(id);

                //Assert.AreEqual("standard1", model2.TemplateName);

                //Assert.AreEqual(model.Content, template.Content);
                Assert.AreEqual(model2.Content, template.Content);
            }
        }
    }
}