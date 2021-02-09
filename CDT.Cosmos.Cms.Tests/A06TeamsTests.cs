using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Models;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Tests
{
    [TestClass]
    public class A06TeamsControllerTests
    {
        private static ApplicationDbContext _dbContext;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            _dbContext = StaticUtilities.GetApplicationDbContext();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _dbContext.Dispose();
        }

        [TestMethod]
        public async Task A01_CreateTeam()
        {
            //
            // Arrange the tests
            //
            var user1 = await StaticUtilities.GetIdentityUser(TestUsers.Teamfoo1);
            var user2 = await StaticUtilities.GetIdentityUser(TestUsers.Teamfoo2);

            var userManager = StaticUtilities.GetUserManager();
            Assert.IsTrue((await userManager.AddToRoleAsync(user1, "Team Members")).Succeeded);
            Assert.IsTrue((await userManager.AddToRoleAsync(user2, "Team Members")).Succeeded);

            using var controller = StaticUtilities.GetTeamsController();

            var teamViewModel = new[]
            {
                new TeamViewModel
                {
                    Id = 0,
                    TeamName = "Team 1",
                    TeamDescription = "Test team 1"
                }
            };

            var result = await controller.Teams_Create(new DataSourceRequest
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
            }, teamViewModel);

            Assert.IsInstanceOfType(result, typeof(JsonResult));

            var modelObject = ((JsonResult) result).Value;

            Assert.IsInstanceOfType(modelObject, typeof(DataSourceResult));

            var model = (DataSourceResult) modelObject;

            var modelView = (List<TeamViewModel>) model.Data;

            Assert.AreEqual(1, modelView.Count);

            Assert.AreEqual("Team 1", modelView.FirstOrDefault().TeamName);

            var team = await _dbContext.Teams.Include(i => i.Members).Include(a => a.Articles)
                .Where(a => a.TeamName == "Team 1").FirstOrDefaultAsync();

            Assert.IsNotNull(team);
            Assert.AreEqual(0, team.Members.Count);
            Assert.AreEqual(0, team.Articles.Count);
        }

        /// <summary>
        ///     User 1 is an author, 2 an editor.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task A02_AddUsersToTeam()
        {
            //
            // Arrange the tests
            //
            var user1 = await StaticUtilities.GetIdentityUser(TestUsers.Teamfoo1);
            var user2 = await StaticUtilities.GetIdentityUser(TestUsers.Teamfoo2);

            // Done arranging things.

            //
            // Setup the test, where user 1 and 2 are members of the global role "Team Members"
            //

            var team = await _dbContext.Teams.FirstOrDefaultAsync();

            var inputModel = new List<TeamMemberViewModel>();

            inputModel.Add(new TeamMemberViewModel
            {
                Id = 0,
                TeamRole = new TeamRoleLookupItem(TeamRoleEnum.Author),
                Team = new TeamViewModel
                {
                    Id = team.Id,
                    TeamDescription = team.TeamDescription,
                    TeamName = team.TeamName
                },
                Member = new TeamMemberLookupItem(user1)
            });

            inputModel.Add(new TeamMemberViewModel
            {
                Id = 0,
                TeamRole = new TeamRoleLookupItem(TeamRoleEnum.Editor),
                Team = new TeamViewModel
                {
                    Id = team.Id,
                    TeamDescription = team.TeamDescription,
                    TeamName = team.TeamName
                },
                Member = new TeamMemberLookupItem(user2)
            });

            using var controller = StaticUtilities.GetTeamsController();

            var result = await controller.TeamMembers_Create(new DataSourceRequest(), inputModel, team.Id);
            team = await _dbContext.Teams.Include(i => i.Members)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync();

            Assert.IsTrue(team.Members.Count == 2);
            Assert.IsTrue(team.Members.Any(a => a.User.Email == TestUsers.Teamfoo1));
            Assert.IsTrue(team.Members.Any(a => a.User.Email == TestUsers.Teamfoo2));

            Assert.IsInstanceOfType(result, typeof(JsonResult));

            var model = ((DataSourceResult) ((JsonResult) result).Value).Data;

            Assert.IsInstanceOfType(model, typeof(List<TeamMemberViewModel>));

            team = await _dbContext.Teams.Include(i => i.Members).Where(t => t.Id == team.Id).FirstOrDefaultAsync();

            Assert.AreEqual(2, team.Members.Count);
        }

        [TestMethod]
        public async Task A03_TestTeamUserNoArticlesToAccess()
        {
            var user1 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo1);
            var user2 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo2);

            var team = await _dbContext.Teams.FirstOrDefaultAsync();


            using (var controller = StaticUtilities.GetEditorController(user1))
            {
                var result = (JsonResult) await controller.Get_Articles(new DataSourceRequest());
                var data = (List<ArticleListItem>) ((DataSourceResult) result.Value).Data;

                Assert.AreEqual(0, data.Count);
            }

            using (var controller = StaticUtilities.GetEditorController(user2))
            {
                var result = (JsonResult) await controller.Get_Articles(new DataSourceRequest());
                var data = (List<ArticleListItem>) ((DataSourceResult) result.Value).Data;

                Assert.AreEqual(0, data.Count);
            }
        }

        [TestMethod]
        public async Task A04_TestTeamUser1CreateArticle()
        {
            var user1 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo1);
            //var user2 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo2);
            var team = await _dbContext.Teams.FirstOrDefaultAsync();


            using var controller = StaticUtilities.GetEditorController(user1);

            var redirect = await controller.Create(new CreatePageViewModel
            {
                Id = 0,
                TeamId = team.Id,
                Templates = new List<SelectListItem>(),
                TemplateId = null,
                Title = "Hello World Team 1 User 1"
            });

            Assert.IsInstanceOfType(redirect, typeof(RedirectToActionResult));

            team = await _dbContext.Teams.Include(i => i.Articles).Where(t => t.Id == team.Id).FirstOrDefaultAsync();

            Assert.AreEqual(1, team.Articles.Count);
        }

        [TestMethod]
        public async Task A05_TestTeamUsersAccessArticle()
        {
            var user1 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo1);
            var user2 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo2);
            var team = await _dbContext.Teams.Include(a => a.Articles).Include(m => m.Members).FirstOrDefaultAsync();

            Assert.IsTrue(team.Articles.Count == 1);
            Assert.IsTrue(team.Members.Count == 2);
            Assert.IsTrue(team.Members.Any(a => a.User.Email == user1.Identity.Name));

            using (var controller = StaticUtilities.GetEditorController(user1))
            {
                var result = (JsonResult) await controller.Get_Articles(new DataSourceRequest());
                var data = (List<ArticleListItem>) ((DataSourceResult) result.Value).Data;

                Assert.AreEqual(1, data.Count);
            }

            using (var controller = StaticUtilities.GetEditorController(user2))
            {
                var result = (JsonResult) await controller.Get_Articles(new DataSourceRequest());
                var data = (List<ArticleListItem>) ((DataSourceResult) result.Value).Data;

                Assert.AreEqual(1, data.Count);
            }

            // Reinitialize the principals and make sure they are added to the Team Members role.
            user1 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo1);
            user2 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo2);

            Assert.IsTrue(user1.IsInRole("Team Members"));
            Assert.IsTrue(user2.IsInRole("Team Members"));
        }

        [TestMethod]
        public async Task A06_TestTeamUsersEditArticle()
        {
            var user1 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo1);
            var user2 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo2);

            Assert.IsTrue(user1.IsInRole("Team Members"));
            Assert.IsTrue(user2.IsInRole("Team Members"));

            using (var controller = StaticUtilities.GetEditorController(user1))
            {
                //
                // Revalidate that user 1 can retrieve the article just created.
                //
                var result = (JsonResult) await controller.Get_Articles(new DataSourceRequest());
                var data = (List<ArticleListItem>) ((DataSourceResult) result.Value).Data;

                Assert.AreEqual(1, data.Count);

                //
                // Pull out the article to edit, this should fail because the article is already published
                // and user1 is a team author.
                //
                var targetArticle = data.FirstOrDefault();
                Assert.IsTrue(targetArticle.LastPublished.HasValue);

                var actionResult = await controller.Edit(targetArticle.Id);
                Assert.IsInstanceOfType(actionResult, typeof(UnauthorizedResult));
            }
        }

        [TestMethod]
        public async Task A07_EditorSucceed()
        {
            // Team editor
            var user2 = await StaticUtilities.GetPrincipal(TestUsers.Teamfoo2);

            //
            // Editor saves an article with a publish date set. Success
            //
            using (var controller = StaticUtilities.GetEditorController(user2))
            {
                var articles = (JsonResult) await controller.Get_Articles(new DataSourceRequest());
                var data = (List<ArticleListItem>) ((DataSourceResult) articles.Value).Data;

                Assert.AreEqual(1, data.Count);

                var article = data.FirstOrDefault();
                var viewResult = (ViewResult) await controller.Edit(article.Id);
                var model = (ArticleViewModel) viewResult.Model;
                model.Published = DateTime.UtcNow;

                var jsonResult = (JsonResult) await controller.SaveHtml(model);
                var jsonModel = (SaveResultJsonModel) jsonResult.Value;

                Assert.IsTrue(jsonModel.ValidationState == ModelValidationState.Valid);
                Assert.IsTrue(jsonModel.Model.Published.HasValue);
            }
        }
    }
}