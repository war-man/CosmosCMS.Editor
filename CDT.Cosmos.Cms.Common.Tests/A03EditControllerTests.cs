using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Common.Tests
{
    /// <summary>
    /// This is a series of tests that exercise the <see cref="Cms.Controllers.EditorController"/>.
    /// </summary>
    [TestClass]
    public class A03EditControllerTests
    {
        //private static List<int> _articleIds;
        //private static List<int> _logIds;
        //private static List<int> _redirectIds;
        //private static IdentityUser _testUser;
        //private static ApplicationDbContext _dbContext;
        //private static IOptions<SiteCustomizationsConfig> _siteOptions;

        //private const string AdminRoleName = "Administrators";
        //private const string EditorRoleName = "Editors";
        //private const string AuthorRoleName = "Authors";

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            using var dbContext = StaticUtilities.GetApplicationDbContext();
            dbContext.ArticleLogs.RemoveRange(dbContext.ArticleLogs.ToList());
            dbContext.Articles.RemoveRange(dbContext.Articles.ToList());

            dbContext.SaveChanges();
        }
        
        /// <summary>
        /// Test the ability to create the home page
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task A01A_CreateHomePage()
        {
            //
            // Test creating and saving a page
            //
            ArticleViewModel model;
            //ArticleViewModel savedModel;

            using (var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo)))
            {
                // Step 1. Create a blank, unsaved CreatePageViewModel
                var initialViewResult = (ViewResult) await controller.Create();
                var createModel = (CreatePageViewModel) initialViewResult.Model;

                // Step 2. Save the new page with a unique title. This redirects to the edit function.
                createModel.Title = Guid.NewGuid().ToString();
                var redirectResult = (RedirectToActionResult) await controller.Create(createModel);

                // Edit function returns a model, ready to edit.  It is saved in the database.
                var viewResult = (ViewResult) await controller.Edit((int) redirectResult.RouteValues["Id"]);
                model = (ArticleViewModel) viewResult.Model;
            }

            //
            // Using EF make sure the article was created
            //
            await using var dbContext = StaticUtilities.GetApplicationDbContext();
            var articleTest1 = await dbContext.Articles.FirstOrDefaultAsync(w => w.Title == model.Title);

            //
            // The model of the new page, should be found by EF.
            //
            Assert.IsNotNull(articleTest1); // Should exist
            // titles should match
            Assert.AreEqual(model.Title, articleTest1.Title);
            // Being the first page, the URL should be "root"
            Assert.AreEqual("root", articleTest1.UrlPath);

        }

        //
        // Test the ability for the editor controller to make and save content changes
        //
        [TestMethod]
        public async Task A01B_ModifyHomePageTitle()
        {

            //
            // Test updating home page title, should not change URL
            //
            ArticleViewModel model;
            ArticleViewModel savedModel;

            //
            // Using EF get the home page
            //
            Article articleTest1;
            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                articleTest1 = await dbContext.Articles.FirstOrDefaultAsync(w => w.UrlPath == "root");
            }

            //
            // Get the home page so we can edit it
            //
            using (var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo)))
            {
               
                // Edit function returns a model, ready to edit.  It is saved in the database.
                var viewResult = (ViewResult)await controller.Edit(articleTest1.Id);
                model = (ArticleViewModel)viewResult.Model;
            }

            //
            // Change the title now
            //
            var oldTitle = model.Title;
            model.Title = "New Page";
            model.Content = LoremIpsum.WhyLoremIpsum;

            //
            // Add some javascript to the header and footer
            //
            using var reader = File.OpenText(@"..\..\..\JavaScript1.js");

            var js = await reader.ReadToEndAsync();
            model.HeaderJavaScript = js;
            model.FooterJavaScript = js;
            
            //
            // Now save the title and javascript block changes.
            //
            using (var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo)))
            {
                var jsonResult = (JsonResult)await controller.SaveHtml(model);
                var jsonData = (SaveResultJsonModel)jsonResult.Value;
                savedModel = jsonData.Model;
            }

            //
            // Use EF to make sure the changes were saved
            //
            Article articleTest2;
            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                articleTest2 = await dbContext.Articles.FirstOrDefaultAsync(w => w.Title == model.Title);
            }


            //
            // Use EF to make sure we are looking at the right article
            //
            Assert.AreEqual(articleTest2.Id, articleTest1.Id);
            Assert.AreEqual("New Page", articleTest2.Title);

            //
            // Title should now be different from the original
            //
            Assert.AreNotEqual(oldTitle, articleTest2.Title);

            //
            // Title should now be the same
            //
            Assert.AreEqual("New Page", savedModel.Title);

            //
            // Check that the content block saved fine.
            // 
            Assert.AreEqual(savedModel.Content, LoremIpsum.WhyLoremIpsum);

            //
            // Check to make sure the header javascript is saved.
            //
            Assert.AreEqual(js, savedModel.HeaderJavaScript);

            //
            // Check to make sure the footer javascript is saved
            //
            Assert.AreEqual(js, savedModel.FooterJavaScript);

            //
            // But the UrlPath should stay as "root" as this is the home page
            //
            Assert.AreEqual("root", savedModel.UrlPath);

            //
            // Check the version number, we didn't create one, so should still be version 1
            //
            Assert.AreEqual(1,
                savedModel
                    .VersionNumber); // Original model.VersionNumber should be 1, and the saved model should have an incremented id.
            
            //
            // Article number should stay the same
            //
            Assert.AreEqual(savedModel.ArticleNumber, model.ArticleNumber);

        }

        //
        // Now lets try getting the home page using URL ""
        //
        [TestMethod]
        public async Task A02_GetHomePage_Success()
        {
            List<Article> articles1;
            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                articles1 = await dbContext.Articles.ToListAsync();
            }

            Assert.IsTrue(articles1.Count > 0);
            using var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            using var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo));


            List<Article> articles2;
            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                articles2 = await dbContext.Articles.ToListAsync();
            }
            Assert.AreEqual(1, articles2.Count);

            var home = (ViewResult) await homeController.Index("", "");

            var homeModel = (ArticleViewModel) home.Model;

            var homePage = await controller.Edit(homeModel.Id);

            Assert.IsNotNull(homePage);
            Assert.IsInstanceOfType(homePage, typeof(ViewResult));
        }

        //
        // Test the ability to save a page, without changes, and test that NO changes were actually made.
        //
        [TestMethod]
        public async Task A03_EditPageSaveChanges_Success()
        {
            //
            // Using EF, get the article we are going to work with.
            //
            Article article;
            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                article = await dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
            }

            //
            // This represents the original, unaltered page.
            //
            ArticleViewModel originalArticleViewModel;

            //
            // The model we are going to edit
            //
            ArticleViewModel editModel;

            //
            // This represents the article after being saved
            //
            ArticleViewModel savedArticleViewModel;

            using (var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo)))
            {

                var page = (ViewResult)await homeController.Index(article.UrlPath, "");

                originalArticleViewModel = (ArticleViewModel)page.Model;
            }

            Article testArticle;
            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                testArticle = await dbContext.Articles.FirstOrDefaultAsync(a => a.Id == originalArticleViewModel.Id);
            }

            //
            // Use EF to make sure we are looking at the right article
            //
            Assert.AreEqual(testArticle.Id, originalArticleViewModel.Id);
            Assert.AreEqual(testArticle.Title, originalArticleViewModel.Title);

            //
            // Now save
            //
            using (var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo)))
            {
                //
                // Get the page we are going to edit
                //
                var editPage = (ViewResult)await controller.Edit(originalArticleViewModel.Id);

                Assert.IsNotNull(editPage);
                Assert.IsInstanceOfType(editPage, typeof(ViewResult));

                //
                // Pull the model out, we are going to change this.
                //
                editModel = (ArticleViewModel)editPage.Model;

                //
                // Save again, NO changes. Saving should not alter content.
                //
                var jsonResult = (JsonResult)await controller.SaveHtml(editModel);
                Assert.IsInstanceOfType(jsonResult, typeof(JsonResult));

                var testPull = (ViewResult)await controller.Edit(originalArticleViewModel.Id);
                savedArticleViewModel = (ArticleViewModel)testPull.Model;
            }

            Assert.IsTrue(savedArticleViewModel.UrlPath == testArticle.UrlPath);
            Assert.IsTrue(savedArticleViewModel.Title == testArticle.Title);
            Assert.IsTrue(savedArticleViewModel.Content == testArticle.Content);
            Assert.IsTrue(savedArticleViewModel.HeaderJavaScript == testArticle.HeaderJavaScript);
            Assert.IsTrue(savedArticleViewModel.FooterJavaScript == testArticle.FooterJavaScript);
        }

        //
        // Test the ability to edit  CODE, and save with success
        //
        [TestMethod]
        public async Task A04_EditCode_Success()
        {
            using var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            using var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            Article article;
            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                article = await dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
            }

            var page = (ViewResult) await homeController.Index(article.UrlPath, "");

            var pageModel = (ArticleViewModel) page.Model;

            var editPage = (ViewResult) await controller.EditCode(pageModel.Id);

            var codeModel = (EditCodePostModel) editPage.Model;

            var result1 = (ViewResult) await controller.EditCode(codeModel);
            var editResult1 = (EditCodePostModel) result1.Model;

            var result2 = (ViewResult) await controller.EditCode(editResult1);
            var editResult2 = (EditCodePostModel) result2.Model;

            var result3 = (ViewResult) await controller.EditCode(editResult2);
            var editResult3 = (EditCodePostModel) result3.Model;

            var result4 = (ViewResult) await controller.EditCode(editResult3);
            var editResult4 = (EditCodePostModel) result4.Model;

            var result5 = (ViewResult) await controller.EditCode(editResult4);
            var editResult5 = (EditCodePostModel) result5.Model;

            Assert.AreEqual(string.IsNullOrEmpty(pageModel.HeaderJavaScript),
                string.IsNullOrEmpty(editResult5.HeaderJavaScript));
            Assert.AreEqual(string.IsNullOrEmpty(pageModel.FooterJavaScript),
                string.IsNullOrEmpty(editResult5.FooterJavaScript));
        }

        //
        // Test what happens when HTML syntax error is injected, and tried to be saved with Edit Code method.
        //
        [TestMethod]
        public async Task A05_EditCode_FailValidation()
        {
            using var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            using var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            Article article;

            await using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                article = await dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                    .FirstOrDefaultAsync();
            }

            var page = (ViewResult) await homeController.Index(article.UrlPath, "");

            var pageModel = (ArticleViewModel) page.Model;

            var editPage = (ViewResult) await controller.EditCode(pageModel.Id);

            var codeModel = (EditCodePostModel) editPage.Model;
            codeModel.Content = "<div><div><span><h1>Wow this is messed up!";
            var result1 = (ViewResult) await controller.EditCode(codeModel);
            var editResult1 = (EditCodePostModel) result1.Model;

            Assert.IsFalse(result1.ViewData.ModelState.IsValid);
            Assert.AreEqual(1, result1.ViewData.ModelState.Keys.Count());
            Assert.AreEqual(1, result1.ViewData.ModelState.Values.Count());
            var errorList = result1.ViewData.ModelState.Values.ToList();
            Assert.AreEqual(1, errorList.Count);
            Assert.AreEqual(4, errorList[0].Errors.Count);

            // Make sure this didn't save, reload edit page, and compare.
            editPage = (ViewResult) await controller.EditCode(pageModel.Id);
            codeModel = (EditCodePostModel) editPage.Model;
            Assert.AreNotEqual("<div><div><span><h1>Wow this is messed up!", codeModel.Content);
        }
    }
}