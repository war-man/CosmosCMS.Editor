using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Tests
{
    [TestClass]
    public class A00EnvironmentTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            var _dbContext = StaticUtilities.GetApplicationDbContext();

            //
            // Wipe clean the database before starting.
            //
            _dbContext.TeamMembers.RemoveRange(_dbContext.TeamMembers.ToList());
            _dbContext.Teams.RemoveRange(_dbContext.Teams.ToList());
            _dbContext.ArticleLogs.RemoveRange(_dbContext.ArticleLogs.ToList());
            _dbContext.Articles.RemoveRange(_dbContext.Articles.ToList());
            _dbContext.Users.RemoveRange(_dbContext.Users.ToList());
            _dbContext.Roles.RemoveRange(_dbContext.Roles.ToList());
            _dbContext.SaveChanges();
        }

        [TestMethod]
        public async Task A00_TestApplicationDbContext()
        {
            var dbContext = StaticUtilities.GetApplicationDbContext();
            Assert.IsNotNull(dbContext);
            Assert.IsTrue(await dbContext.Database.CanConnectAsync());
        }

        [TestMethod]
        public async Task A01_RunSetup()
        {
            using (var setupController = StaticUtilities.GetSetupController())
            {
                var result = await setupController.Index();

                Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
                Assert.AreEqual("FinishSetup", ((RedirectToActionResult) result).ActionName);
            }

            using (var dbContext = StaticUtilities.GetApplicationDbContext())
            {
                var roles = await dbContext.Roles.ToListAsync();
                Assert.AreEqual(5, roles.Count);
            }

            using (var roleManager = StaticUtilities.GetRoleManager())
            {
                Assert.IsTrue(await roleManager.RoleExistsAsync("Editors"));
                Assert.IsTrue(await roleManager.RoleExistsAsync("Authors"));
                Assert.IsTrue(await roleManager.RoleExistsAsync("Reviewers"));
                Assert.IsTrue(await roleManager.RoleExistsAsync("Administrators"));
                Assert.IsTrue(await roleManager.RoleExistsAsync("Team Members"));
            }


            var foo = await StaticUtilities.GetIdentityUser(TestUsers.Foo);

            using var userManager = StaticUtilities.GetUserManager();
            Assert.IsTrue(await userManager.IsInRoleAsync(foo, "Administrators"));
        }
    }
}