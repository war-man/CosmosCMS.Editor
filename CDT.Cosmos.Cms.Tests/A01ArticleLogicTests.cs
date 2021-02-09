using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace CDT.Cosmos.Cms.Tests
{
    [TestClass]
    public class A01ArticleLogicTests
    {
        private static IdentityUser _testUser;
        private static ApplicationDbContext _dbContext;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            _dbContext = StaticUtilities.GetApplicationDbContext();
            _testUser = StaticUtilities.GetIdentityUser(TestUsers.Foo).Result;

            _dbContext.ArticleLogs.RemoveRange(_dbContext.ArticleLogs.ToList());
            _dbContext.Articles.RemoveRange(_dbContext.Articles.ToList());

            _dbContext.SaveChanges();
        }

        [ClassCleanup]
        public static void CleanupClass()
        {
            //_dbContext.Articles.RemoveRange(_dbContext.Articles.ToList());
            //_dbContext.Users.RemoveRange(_dbContext.Users.ToList());
            //_dbContext.SaveChanges();
            _dbContext.Dispose();
        }

        /// <summary>
        ///     Test the creation of root page, and, version it.
        /// </summary>
        [TestMethod]
        public async Task A_Create_And_Save_Root()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });

            var logic = new ArticleLogic(_dbContext, siteOptions.Value);

            var rootModel = await logic.Create($"New Title {Guid.NewGuid().ToString()}");

            Assert.AreEqual(0, rootModel.Id);
            Assert.AreEqual(0, rootModel.VersionNumber);

            //rootModel.Title = Guid.NewGuid().ToString();
            rootModel.Content = Guid.NewGuid().ToString();
            rootModel.Published = DateTime.Now;

            //
            // CREATE THE HOME (ROOT) PAGE.
            //
            var homePage = await logic.UpdateOrInsert(rootModel, _testUser.Id);

            //var articles = await _dbContext.Articles.ToListAsync();

            Assert.IsNotNull(homePage);
            Assert.IsInstanceOfType(homePage, typeof(ArticleViewModel));
            Assert.IsTrue(0 < homePage.Id);
            Assert.AreEqual(1, homePage.VersionNumber);
            //Assert.AreEqual("root", homePage.UrlPath);
            Assert.IsTrue(homePage.Published.HasValue);

            //
            // Check other properties
            //
            Assert.AreEqual(rootModel.Title, homePage.Title);
            Assert.AreEqual(rootModel.Content, homePage.Content);

            //
            // Check Logs
            //
            var logs = await _dbContext.ArticleLogs.Where(l => l.ArticleId == homePage.Id).ToListAsync();

            Assert.AreEqual(4, logs.Count);
        }

        [TestMethod]
        public async Task B_Get_Home_Page()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);

            //var article = await _dbContext.Articles.FirstOrDefaultAsync(w => w.UrlPath == "ROOT");

            //
            // All four should find the home page.
            //
            var test1 = await logic.GetByUrl(null);
            var test2 = await logic.GetByUrl("");
            var test3 = await logic.GetByUrl("/");
            var test4 = await logic.GetByUrl("   ");

            Assert.IsNotNull(test1);
            Assert.IsNotNull(test2);
            Assert.IsNotNull(test3);
            Assert.IsNotNull(test4);

            Assert.AreEqual(test1.Id, test2.Id);
            Assert.AreEqual(test1.Id, test3.Id);
            Assert.AreEqual(test1.Id, test4.Id);
        }

        [TestMethod]
        public async Task C_Add_Versions_Some_Published()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);

            //var article = await _dbContext.Articles.FirstOrDefaultAsync(w => w.UrlPath == "ROOT");

            //var articles = await _dbContext.Articles.ToListAsync();

            var version1 = await logic.GetByUrl("");

            // Make changes to version 1
            version1.VersionNumber = 0; // Trigger new version
            version1.Published = null; // Not published

            // Save work, now should have new version number
            var version2 = await logic.UpdateOrInsert(version1, _testUser.Id);
            //Assert.AreEqual(2, version2.VersionNumber);
            Assert.IsFalse(version2.Published.HasValue);

            // Make a third version, still unpublished, now change to published
            version2.VersionNumber = 0;
            var version3 = await logic.UpdateOrInsert(version2, _testUser.Id);
            Assert.AreEqual(3, version3.VersionNumber);
            Assert.IsFalse(version3.Published.HasValue);

            // Fourth version. Third one is published, but new versions are UNPUBLISHED by default.
            version3.Published = DateTime.Now;
            version3.VersionNumber = 0;
            var version4 = await logic.UpdateOrInsert(version3, _testUser.Id);
            Assert.AreEqual(4, version4.VersionNumber);
            Assert.IsFalse(version4.Published.HasValue);

            // Now set to un published for version 5
            version4.Published = null;
            version4.VersionNumber = 0;


            // Make a third version, still unpublished, now change to published
            var version5 = await logic.UpdateOrInsert(version4, _testUser.Id);
            Assert.AreEqual(5, version5.VersionNumber);
            Assert.IsFalse(version5.Published.HasValue);

            var versions = await _dbContext.Articles
                .Where(a => a.ArticleNumber == version5.ArticleNumber).ToListAsync();
            Assert.AreEqual(5, versions.Count);
        }

        [TestMethod]
        public async Task C_Get_Last_Published_Version()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);
            var article = await logic.GetByUrl("");
            var article1 = article;
            var lastPublishedArticle = await _dbContext.Articles
                .Where(p => p.Published != null && p.ArticleNumber == article1.ArticleNumber)
                .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
            //var lastArticlePeriod = await _dbContext.Articles.Where(p => p.ArticleNumber == article.ArticleNumber).OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();


            Assert.AreEqual(lastPublishedArticle.ArticleNumber, article.ArticleNumber);
            Assert.AreEqual(lastPublishedArticle.VersionNumber,
                article.VersionNumber); // VERSION 4 is NOT PUBLISHED!! .

            var articleNumber = article.ArticleNumber;
            var versionNumber = article.VersionNumber;

            // Now get the very last version, unpublished or not.
            article = await logic.GetByUrl("", "en-US", false);
            Assert.AreEqual(articleNumber, article.ArticleNumber);
            Assert.AreNotEqual(versionNumber, article.VersionNumber);
        }

        [TestMethod]
        public async Task D_Create_New_Article_Versions_TestRedirect_Publish()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });

            var logic = new ArticleLogic(_dbContext, siteOptions.Value);

            // Create and save version 1
            var version1 = await logic.UpdateOrInsert(await logic.Create("This is a second page" + Guid.NewGuid()),
                _testUser.Id);
            var oldPath = version1.UrlPath;
            Assert.AreEqual(1, version1.VersionNumber);
            Assert.AreEqual(2, version1.ArticleNumber);

            // Make a change to version 1, and save as an update (don't create a new version)
            version1.Content = Guid.NewGuid() + "WOW";
            var ver1OriginalContent = version1.Content;

            var version1UpdateA = await logic.UpdateOrInsert(version1, _testUser.Id);
            Assert.AreEqual(version1.Content, version1UpdateA.Content);
            Assert.AreEqual(1, version1UpdateA.VersionNumber);

            // Make a change to version 1, and save as an update (don't create a new version)
            version1UpdateA.Content = Guid.NewGuid() + "NOW";
            var version1UpdateB = await logic.UpdateOrInsert(version1UpdateA, _testUser.Id);
            Assert.AreEqual(version1UpdateA.Content, version1UpdateB.Content);
            Assert.AreNotEqual(version1.Content, version1UpdateB.Content);
            Assert.AreEqual(1, version1UpdateB.VersionNumber);

            // Make a new version (version 2)
            version1UpdateB.Published = null;
            version1UpdateB.VersionNumber = 0;
            var version2 = await logic.UpdateOrInsert(version1UpdateB, _testUser.Id);
            Assert.AreEqual(2, version2.VersionNumber);
            Assert.AreEqual(version1UpdateB.Content, version2.Content);

            version2.Title = "New Version " + Guid.NewGuid(); // Change the title 
            version2.VersionNumber = 0; // Make a version 3
            version2.Published = DateTime.Now; // New version published.


            var titleChangeVersion3 = await logic.UpdateOrInsert(version2, _testUser.Id);
            Assert.AreEqual(3, titleChangeVersion3.VersionNumber);
            // Even though version 2 is published, new versions should NEVER be published.
            Assert.IsFalse(titleChangeVersion3.Published.HasValue);
            Assert.AreEqual(titleChangeVersion3.Title, version2.Title);
            Assert.AreNotEqual(version1.Title, version2.Title);

            titleChangeVersion3.VersionNumber = 0; // Make version 4
            var version4 = await logic.UpdateOrInsert(titleChangeVersion3, _testUser.Id);

            Assert.AreEqual(4, version4.VersionNumber);

            //
            // Get all four versions
            //
            var testVersion1 = _dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 1);
            var testVersion2 = _dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 2);
            var testVersion3 = _dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 3);
            var testVersion4 = _dbContext.Articles.Include(i => i.ArticleLogs)
                .FirstOrDefault(f => f.ArticleNumber == 2 && f.VersionNumber == 4);

            Assert.IsNotNull(testVersion1);
            Assert.IsNotNull(testVersion2);
            Assert.IsNotNull(testVersion3);
            Assert.IsNotNull(testVersion4);

            // Test Version 1
            // Changing title, changes it for all versions of the same Article Number
            Assert.IsTrue(testVersion1.Title.Contains("New Version"));
            Assert.IsFalse(testVersion1.Published.HasValue);
            Assert.AreEqual(1, testVersion1.VersionNumber);
            Assert.AreEqual(2, testVersion1.ArticleNumber);
            Assert.AreNotEqual(ver1OriginalContent, testVersion1.Content); // Last change made.
            // Test Version 1 Logs
            Assert.IsTrue(testVersion1.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("New article",
                                                                StringComparison.CurrentCultureIgnoreCase)));
            Assert.IsTrue(testVersion1.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("New version",
                                                                StringComparison.CurrentCultureIgnoreCase)));

            Assert.AreEqual(2, testVersion1.ArticleLogs.Count(a => a.IdentityUserId == _testUser.Id &&
                                                                   a.ActivityNotes == "Edit existing"));
            Assert.AreEqual(4, testVersion1.ArticleLogs.Count);

            // Test Version 2
            //Assert.AreEqual("New Version", testVersion2.Title);
            Assert.IsFalse(testVersion2.Published.HasValue);
            Assert.AreEqual(2, testVersion2.VersionNumber);
            Assert.AreEqual(2, testVersion2.ArticleNumber);
            //// Test Version 2 Logs
            Assert.IsTrue(testVersion2.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes == "New version"));

            Assert.AreEqual(1, testVersion2.ArticleLogs.Count);

            // Test Version 3
            Assert.IsFalse(testVersion3.Published.HasValue);
            Assert.AreEqual(3, testVersion3.VersionNumber);
            Assert.AreEqual(2, testVersion3.ArticleNumber);
            // Test Version 3 Logs
            Assert.IsTrue(testVersion3.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("Redirect")));
            Assert.IsTrue(testVersion3.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes == "New version"));
            Assert.IsTrue(testVersion3.ArticleLogs.Any(a => a.IdentityUserId == _testUser.Id &&
                                                            a.ActivityNotes.Contains("Redirect")));
            Assert.AreEqual(2, testVersion3.ArticleLogs.Count);

            //
            // Test redirect
            //
            //var alist = await _dbContext.Articles.Where(w => w.StatusCode == 3).ToListAsync();
            var redirectResult = await logic.GetByUrl(version2.UrlPath, "en-US");
            Assert.AreEqual(StatusCodeEnum.Redirect, redirectResult.StatusCode);
            Assert.AreEqual(redirectResult.Content, titleChangeVersion3.UrlPath);

            //var redirect = await _dbContext.ArticleRedirects.FirstOrDefaultAsync(
            //    f => f.ArticleId == testVersion3.Id
            //);
            //Assert.AreEqual(oldPath, redirect.OldUrlPath);

            // Test version 4
            // Title for all version
            Assert.AreEqual(testVersion1.Title, testVersion4.Title);
            Assert.IsFalse(testVersion4.Published.HasValue);
            Assert.AreEqual(4, testVersion4.VersionNumber);
            Assert.AreEqual(2, testVersion4.ArticleNumber);
            Assert.IsTrue(testVersion4.ArticleLogs.Any());
            Assert.IsTrue(testVersion4.ArticleLogs.Any());
            Assert.IsTrue(testVersion4.ArticleLogs.Count > 0);

            //
            // There should be 4 versions
            //
            Assert.AreEqual(4, _dbContext.Articles.Where(a => a.ArticleNumber == version1.ArticleNumber).Count());
        }

        [TestMethod]
        public async Task E_SetStatus()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);
            var entity = await _dbContext.Articles.FirstOrDefaultAsync(f => f.Title.Contains("New Version"));

            var article = await logic.GetByUrl(entity.UrlPath, "en-US", false);

            var results1 = await logic.SetStatus(article.ArticleNumber, StatusCodeEnum.Deleted,
                _testUser.Id);

            Assert.AreEqual(_dbContext.Articles.Where(a => a.ArticleNumber == article.ArticleNumber).Count(), results1);

            // Since this is deleted, this should not be able to be retrieved through any of these options.

            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US"));
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US", false));
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US", false,
                false)); // The article is deleted, can't even retrieve it here
            // Now set to inactive, this article should now be found.
            var results1b = await logic.SetStatus(article.ArticleNumber, StatusCodeEnum.Inactive,
                _testUser.Id);
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US"));
            Assert.IsNull(await logic.GetByUrl(entity.UrlPath, "en-US", false));
            Assert.IsNotNull(await logic.GetByUrl(entity.UrlPath, "en-US", false,
                false)); // The article is deleted, can't even retrieve it here


            var results2 = await logic.SetStatus(article.ArticleNumber, StatusCodeEnum.Inactive,
                _testUser.Id);

            Assert.AreEqual(_dbContext.Articles.Where(a => a.ArticleNumber == article.ArticleNumber).Count(), results2);

            // Now this should be visible
            var result3 = await logic.GetByUrl(entity.UrlPath, "en-US", false, false);

            Assert.IsNotNull(result3);
        }

        [TestMethod]
        public async Task F_CheckTitle()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);
            var entity = await _dbContext.Articles.FirstOrDefaultAsync(f => f.ArticleNumber == 1);
            var article = await logic.GetByUrl(entity.UrlPath, "en-US", false, false);
            Assert.IsFalse(await logic.ValidateTitle(entity.Title, 0));
            Assert.IsTrue(await logic.ValidateTitle(article.Title, article.ArticleNumber));
        }

        [TestMethod]
        public async Task G_FindByUrlTest()
        {
            //Public_Safety_Power_Shutoff_(PSPS)
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);
            var article = await logic.Create("Public Safety Power Shutoff (PSPS)" + Guid.NewGuid());
            article.ArticleNumber = 0;
            article.Published = DateTime.Now.AddDays(-1);
            article.Content = "Hellow world!";
            var saved = await logic.UpdateOrInsert(article, _testUser.Id);

            var find = await logic.GetByUrl(saved.UrlPath, "en-US");
            Assert.IsNotNull(find);
        }

        [TestMethod]
        public async Task H_Test_ScheduledPublishing()
        {
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);
            var article = await logic.GetByUrl("", "en-US");

            var lastPublishedArticle = await _dbContext.Articles
                .Where(p => p.Published != null && p.ArticleNumber == article.ArticleNumber)
                .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
            //var lastArticlePeriod = await _dbContext.Articles.Where(p => p.ArticleNumber == article.ArticleNumber).OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();

            Assert.AreEqual(lastPublishedArticle.ArticleNumber, article.ArticleNumber);
            Assert.AreEqual(lastPublishedArticle.VersionNumber,
                article.VersionNumber); // VERSION 4 is NOT PUBLISHED!! .

            var articleNumber = article.ArticleNumber;
            var versionNumber = article.VersionNumber;

            // Now get the very last version, unpublished or not.
            article = await logic.GetByUrl("", "en-US", false);
            Assert.AreEqual(articleNumber, article.ArticleNumber);
            Assert.AreNotEqual(versionNumber, article.VersionNumber);

            // Update the last version, publish for 20 seconds from now...
            article.Published = DateTime.UtcNow.AddSeconds(20);
            var newArticleViewModel = await logic.UpdateOrInsert(article, _testUser.Id);

            // We should still be getting the original article.
            var oldArticleViewModel = await logic.GetByUrl("", "en-US");

            // Wait 20 seconds for the new article to become published...
            Thread.Sleep(20000);

            // Now get the article, with same URL, this should be different.
            var publishedArticle = await logic.GetByUrl("", "en-US");

            // This should be the same article ID.
            Assert.AreEqual(lastPublishedArticle.Id, oldArticleViewModel.Id);

            // This should be the new article ID
            Assert.AreEqual(article.Id, publishedArticle.Id);
        }

        [TestMethod]
        public async Task I_Get_Translations_Home_Page()
        {
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", "CA-Response-Portal-bfc617b86937.json");

            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = true,
                AllowSetup = true
            });
            var logic = new ArticleLogic(_dbContext, siteOptions.Value);

            //var article = await _dbContext.Articles.FirstOrDefaultAsync(w => w.UrlPath == "ROOT");

            var article = await logic.Create("Rosetta Stone");

            article.Content =
                "The other night 'bout two o'clock, or maybe it was three,\r\nAn elephant with shining tusks came chasing after me.\r\nHis trunk was wavin' in the air an'  spoutin' jets of steam\r\nAn' he was out to eat me up, but still I didn't scream\r\nOr let him see that I was scared - a better thought I had,\r\nI just escaped from where I was and crawled in bed with dad.\r\n\r\nSource: https://www.familyfriendpoems.com/poem/being-brave-at-night-by-edgar-albert-guest";

            article.Published = DateTime.Now.AddDays(-1);

            var result = await logic.UpdateOrInsert(article, _testUser.Id);

            //
            // All four should find the home page.
            //
            //var test1 = await logic.GetByUrl(result.UrlPath, "en");
            //var test2 = await logic.GetByUrl(result.UrlPath, "es");
            var test3 = await logic.GetByUrl(result.UrlPath, "vi");
            //var test4 = await logic.GetByUrl(result.UrlPath, "fr");

            //Assert.IsNotNull(test1);
            //Assert.IsNotNull(test2);
            Assert.IsNotNull(test3);
            //Assert.IsNotNull(test4);

            //Assert.AreEqual(test1.Id, test2.Id);
            Assert.AreEqual(result.Id, test3.Id);

            Assert.AreEqual("đá Rosetta", test3.Title);
            //Assert.AreEqual(test1.Id, test4.Id);


        }

        [TestMethod]
        public async Task J_TestRedisCacheFunctions()
        {
            var redisContextConfig = StaticUtilities.GetRedisContextConfig();
            var distributedCache = StaticUtilities.GetRedisDistributedCache();
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = false,
                AllowSetup = false
            });

            var logic = new ArticleLogic(_dbContext, distributedCache, siteOptions.Value, StaticUtilities.GetLogger<ArticleLogic>(), Options.Create(redisContextConfig));

            var article = await logic.GetByUrl("root", "en");

            var key = RedisCacheService.GetPageCacheKey(redisContextConfig.CacheId, "en",
                RedisCacheService.CacheOptions.Database, "root");
            var cache = await distributedCache.GetAsync(key);
            Assert.IsNotNull(cache);

        }

        [TestMethod]
        public async Task K_TestRedisCachePurge()
        {
            var redisContextConfig = StaticUtilities.GetRedisContextConfig();
            var distributedCache = StaticUtilities.GetRedisDistributedCache();
            var siteOptions = Options.Create(new SiteCustomizationsConfig
            {
                ReadWriteMode = false,
                AllowSetup = false
            });

            var logic = new ArticleLogic(_dbContext, distributedCache, siteOptions.Value, StaticUtilities.GetLogger<ArticleLogic>(), Options.Create(redisContextConfig));

            // Get list of articles...
            var articles = await logic.GetArticleList(_dbContext.Articles.AsQueryable());

            foreach (var article in articles)
            {
                var item = logic.GetByUrl(article.UrlPath);
            }

            var results = await logic.FlushRedis("");

            Assert.IsNotNull(results);

            Assert.IsTrue(results.Keys.Count >= 2);
        }
    }
}