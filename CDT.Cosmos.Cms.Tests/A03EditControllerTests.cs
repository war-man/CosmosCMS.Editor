using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Tests
{
    [TestClass]
    public class A03EditControllerTests
    {
        //private static List<int> _articleIds;
        //private static List<int> _logIds;
        //private static List<int> _redirectIds;
        //private static IdentityUser _testUser;
        private static ApplicationDbContext _dbContext;
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
            _dbContext = StaticUtilities.GetApplicationDbContext();
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _dbContext.Dispose();
        }

        [TestMethod]
        public async Task A01_CreateAndSavePage()
        {
            ArticleViewModel model;
            ArticleViewModel savedModel;

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

            var articleTest1 = await _dbContext.Articles.FirstOrDefaultAsync(w => w.Title == model.Title);

            Assert.IsNotNull(articleTest1);

            model.Title = "New Page";
            model.Content = LoremIpsum.WhyLoremIpsum;

            using var reader = File.OpenText(@"..\..\..\JavaScript1.js");

            var js = await reader.ReadToEndAsync();
            model.HeaderJavaScript = js;
            model.FooterJavaScript = js;

            var encodedArticleViewModel = model;


            var articleTest2 = await _dbContext.Articles.FirstOrDefaultAsync(w => w.Title == "New Page");

            Assert.IsNull(articleTest2);

            using (var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo)))
            {
                var jsonResult = (JsonResult) await controller.SaveHtml(encodedArticleViewModel);
                var jsonData = (SaveResultJsonModel) jsonResult.Value;
                savedModel = jsonData.Model;
            }

            //var articleTest3 = await _dbContext.Articles.FirstOrDefaultAsync(w => w.Title == model.Title);

            //Assert.IsNull(articleTest3);

            Assert.AreEqual(savedModel.Title, "New Page");
            Assert.AreEqual(1,
                savedModel
                    .VersionNumber); // Original model.VersionNumber should be 1, and the saved model should have an encremented id.
            Assert.AreEqual(savedModel.ArticleNumber, model.ArticleNumber);
            Assert.AreNotEqual(savedModel.UrlPath, model.UrlPath);
            Assert.AreEqual("new_page", savedModel.UrlPath);

            Assert.AreEqual(savedModel.Content, LoremIpsum.WhyLoremIpsum);
            var articles = await _dbContext.Articles.ToListAsync();
            Assert.AreEqual(14, articles.Count);
        }

        [TestMethod]
        public async Task A02_GetHomePage_Success()
        {
            var articles = await _dbContext.Articles.ToListAsync();
            Assert.AreEqual(14, articles.Count);
            using var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            using var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo));
            articles = await _dbContext.Articles.ToListAsync();
            Assert.AreEqual(14, articles.Count);
            var home = (ViewResult) await homeController.Index("", "");

            var homeModel = (ArticleViewModel) home.Model;

            var homePage = await controller.Edit(homeModel.Id);

            Assert.IsNotNull(homePage);
            Assert.IsInstanceOfType(homePage, typeof(ViewResult));
        }

        [TestMethod]
        public async Task A03_EditPage_Success()
        {
            using var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            using var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            var article = await _dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            var page = (ViewResult) await homeController.Index(article.UrlPath, "");

            var pageModel = (ArticleViewModel) page.Model;

            var editPage = (ViewResult) await controller.Edit(pageModel.Id);

            Assert.IsNotNull(editPage);
            Assert.IsInstanceOfType(editPage, typeof(ViewResult));

            var editModel = (ArticleViewModel) editPage.Model;

            var jsonResult = (JsonResult) await controller.SaveHtml(editModel);
            Assert.IsInstanceOfType(jsonResult, typeof(JsonResult));

            var testPull = (ViewResult) await controller.Edit(pageModel.Id);
            var newModel = (ArticleViewModel) testPull.Model;

            var testArticle = await _dbContext.Articles.FirstOrDefaultAsync(a => a.Id == editModel.Id);

            Assert.IsTrue(newModel.UrlPath == testArticle.UrlPath);
            Assert.IsTrue(newModel.Title == testArticle.Title);
            Assert.IsTrue(newModel.Content == testArticle.Content);
            Assert.IsTrue(newModel.HeaderJavaScript == testArticle.HeaderJavaScript);
            Assert.IsTrue(newModel.FooterJavaScript == testArticle.FooterJavaScript);
        }

        [TestMethod]
        public async Task A04_EditCode_Success()
        {
            using var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            using var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            var article = await _dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

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

        [TestMethod]
        public async Task A05_EditCode_FailValidation()
        {
            using var homeController =
                StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            using var controller =
                StaticUtilities.GetEditorController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            var article = await _dbContext.Articles.Where(p => p.Published.HasValue).OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

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