using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Tests
{
    [TestClass]
    public class A05HomeControllerTests
    {
        private static ApplicationDbContext _dbContext;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            _dbContext = StaticUtilities.GetApplicationDbContext();

            _dbContext.ArticleLogs.RemoveRange(_dbContext.ArticleLogs.ToList());
            _dbContext.Articles.RemoveRange(_dbContext.Articles.ToList());
            _dbContext.Layouts.RemoveRange(_dbContext.Layouts.ToList());
            _dbContext.Users.RemoveRange(_dbContext.Users.ToList());
            _dbContext.Roles.RemoveRange(_dbContext.Roles.ToList());
            _dbContext.SaveChanges();
        }


        [TestMethod]
        public async Task A01_RedirectToSetup()
        {
            var controller = StaticUtilities.GetHomeController(await StaticUtilities.GetPrincipal(TestUsers.Foo));

            var redirectToSetup = await controller.Index("", "");

            Assert.IsInstanceOfType(redirectToSetup, typeof(RedirectToActionResult));

            Assert.AreEqual("Setup", ((RedirectToActionResult) redirectToSetup).ControllerName);
            Assert.AreEqual("Index", ((RedirectToActionResult) redirectToSetup).ActionName);
        }

        [TestMethod]
        public async Task A02_RunSetup()
        {
            var setupController = StaticUtilities.GetSetupController();

            var indexResult = await setupController.Index();

            Assert.IsInstanceOfType(indexResult, typeof(RedirectToActionResult));
        }
    }
}