using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CDT.Cosmos.Cms.Common.Controllers;
using CDT.Cosmos.Cms.Common.Models;
using CDT.Cosmos.Cms.Common.Services;
using Google.Cloud.Translate.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Z.EntityFramework.Plus;

namespace CDT.Cosmos.Cms.Common.Data.Logic
{
    /// <summary>
    ///     Main logic behind getting and maintaining web site articles.
    /// </summary>
    /// <remarks>An article is the "content" behind a web page.</remarks>
    public class ArticleLogic
    {
        private readonly SiteCustomizationsConfig _config;
        private readonly ApplicationDbContext _dbContext;
        private readonly IDistributedCache _distributedCache;
        //private readonly IOptions<GoogleCloudAuthConfig> _googleCloudAuthConfig;

        //private readonly IOptions<AzureBlobServiceConfig> _blobConfig;
        private readonly IOptions<RedisContextConfig> _redisOptions;
        private readonly TranslationServices _translationServices;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="distributedCache"></param>
        /// <param name="config"></param>
        /// <param name="redisOptions"></param>
        /// <param name="translationServices"></param>
        public ArticleLogic(ApplicationDbContext dbContext,
            IDistributedCache distributedCache,
            IOptions<SiteCustomizationsConfig> config,
            IOptions<RedisContextConfig> redisOptions,
            TranslationServices translationServices)
        {
            _dbContext = dbContext;
            _distributedCache = distributedCache;
            _config = config.Value;
            _redisOptions = redisOptions;
            _translationServices = translationServices;
        }

        #region CREATE METHODS

        /// <summary>
        ///     Creates a new article, does NOT save it to the database before returning a copy for editing.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="templateId"></param>
        /// <returns>Unsaved article ready to edit and save</returns>
        /// <remarks>
        ///     <para>
        ///         Creates a new article, unsaved, ready to edit.  Uses <see cref="GetDefaultLayout" /> to get the layout,
        ///         and builds the <see cref="ArticleViewModel" /> using method
        ///         <seealso cref="BuildArticleViewModel(Article, string)" />.
        ///     </para>
        ///     <para>
        ///         If a template ID is given, the contents of this article is loaded with content from the <see cref="Template" />
        ///         .
        ///     </para>
        /// </remarks>
        public async Task<ArticleViewModel> Create(string title, int? templateId = null)
        {
            //var layout = await GetDefaultLayout(false);
            var layout = await _dbContext.Layouts.FirstOrDefaultAsync(f => f.IsDefault);
            if (layout != null)
                _dbContext.Entry(layout).State = EntityState.Detached; // Prevents layout from being updated.

            var defaultTemplate = string.Empty;

            if (templateId.HasValue)
            {
                var template = await _dbContext.Templates.FindAsync(templateId.Value);

                defaultTemplate = template?.Content;

            }

            if (string.IsNullOrEmpty(defaultTemplate))
            {
                defaultTemplate = "<div class=\"container m-y-lg\">" +
                                  "<main class=\"main-primary\">" +
                                  "<div class=\"row\">" +
                                  "<div class=\"col-md-12\"><h1>Why Lorem Ipsum</h1><p>" +
                                  LoremIpsum.WhyLoremIpsum + "</p></div>" +
                                  "</div>" +
                                  "<div class=\"row\">" +
                                  "<div class=\"col-md-6\"><h2>Column 1</h2><p>" + LoremIpsum.SubSection1 + "</p></div>" +
                                  "<div class=\"col-md-6\"><h2>Column 2</h2><p>" + LoremIpsum.SubSection2 + "</p></div>" +
                                  "</div>" +
                                  "</main>" +
                                  "</div>";
            }

            var article = new Article
            {
                ArticleNumber = 0,
                VersionNumber = 0,
                Title = title,
                Content = defaultTemplate,
                Updated = DateTime.Now,
                UrlPath = HttpUtility.UrlEncode(title.Replace(" ", "_")),
                ArticleLogs = new List<ArticleLog>(),
                LayoutId = layout?.Id
            };

            return await BuildArticleViewModel(article, "en-US");
        }

        #endregion

        #region VALLIDATION

        /// <summary>
        ///     Validate that the title is not already taken by another article.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        public async Task<bool> ValidateTitle(string title, int articleNumber)
        {
            if (title.ToLower() == "pub") return false;
            var article = await _dbContext.Articles.FirstOrDefaultAsync(a =>
                    a.Title.ToLower() == title.Trim().ToLower() && // Is the title used already
                    a.StatusCode != (int)StatusCodeEnum.Deleted // and the page is active (active or is inactive)
            );

            if (article == null) return true;

            return article.ArticleNumber == articleNumber;
        }

        #endregion

        #region GET METHODS

        /// <summary>
        ///     Gets an article by ID (row Key), or creates a new (unsaved) article if id is null.
        /// </summary>
        /// <param name="id">Row Id (or identity) number.  If null returns a new article.</param>
        /// <param name="controllerName"></param>
        /// <remarks>
        ///     <para>
        ///         For new articles, uses <see cref="Create" /> and the method
        ///         <see cref="BuildArticleViewModel(Article, string)" /> to
        ///         generate the <see cref="ArticleViewModel" /> .
        ///     </para>
        ///     <para>
        ///         Retrieves <see cref="Article" /> and builds an <see cref="ArticleViewModel" /> using the method
        ///         <see cref="BuildArticleViewModel(Article, string)" />,
        ///         or in the case of a template, uses method <see cref="BuildTemplateViewModel" />.
        ///     </para>
        /// </remarks>
        /// <returns>
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="BuildArticleViewModel(Article, string)" /> or <see cref="BuildTemplateViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(int? id, EnumControllerName controllerName)
        {
            if (controllerName == EnumControllerName.Template)
            {
                if (id == null)
                    throw new Exception("Template ID:null not found.");

                var idNo = id.Value;
                var template = await _dbContext.Templates.FindAsync(idNo);

                if (template == null) throw new Exception($"Template ID:{id} not found.");
                return BuildTemplateViewModel(template);
            }

            if (id == null)
            {
                var count = await _dbContext.Articles.CountAsync();
                return await Create("Page " + count);
            }

            var article = await _dbContext.Articles
                .Include(l => l.Layout)
                .FirstOrDefaultAsync(a => a.Id == id && a.StatusCode != 2);

            if (controllerName == EnumControllerName.Edit)
                article.Content = article.Content.Replace(" crx=\"", " contenteditable=\"",
                    StringComparison.CurrentCultureIgnoreCase);

            if (article == null) throw new Exception($"Article ID:{id} not found.");
            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Gets a template represented as an <see cref="ArticleViewModel" />.
        /// </summary>
        /// <param name="template"></param>
        /// <returns>ArticleViewModel</returns>
        private ArticleViewModel BuildTemplateViewModel(Template template)
        {
            return new ArticleViewModel
            {
                Id = template.Id,
                ArticleNumber = template.Id,
                UrlPath = HttpUtility.UrlEncode(template.Title.Trim().Replace(" ", "_")),
                VersionNumber = 1,
                Published = DateTime.Now,
                Title = template.Title,
                Content = template.Content,
                Updated = DateTime.Now.ToUniversalTime(),
                HeaderJavaScript = string.Empty,
                FooterJavaScript = string.Empty,
                ReadWriteMode = _config.ReadWriteMode
            };
        }

        /// <summary>
        ///     Gets a copy of the article ready for edit.
        /// </summary>
        /// <param name="articleNumber">Article Number</param>
        /// <param name="versionNumber">Version to edit</param>
        /// <returns>
        ///     <see cref="ArticleViewModel" />
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see
        ///         <see cref="BuildArticleViewModel(Article, string)" />
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> Get(int articleNumber, int versionNumber)
        {
            var article = await _dbContext.Articles.Include(l => l.Layout)
                .FirstOrDefaultAsync(
                    a => a.ArticleNumber == articleNumber &&
                         a.VersionNumber == versionNumber &&
                         a.StatusCode != 2);

            if (article == null)
                throw new Exception($"Article number:{articleNumber}, Version:{versionNumber}, not found.");

            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Gets the current *published* version of an article.  Gets the home page if ID is null.
        /// </summary>
        /// <param name="urlPath">URL Encoded path</param>
        /// <param name="lang">Language to return content as.</param>
        /// <param name="publishedOnly">Only retrieve latest published version</param>
        /// <param name="onlyActive">Only retrieve active status</param>
        /// <returns>
        ///     <see cref="ArticleViewModel" />
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         Returns <see cref="ArticleViewModel" />. For more details on what is returned, see <see cref="GetArticle" />
        ///         and <see cref="BuildArticleViewModel" />.
        ///     </para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </remarks>
        public async Task<ArticleViewModel> GetByUrl(string urlPath, string lang = "en-US", bool publishedOnly = true,
            bool onlyActive = true)
        {
            urlPath = urlPath?.Trim().ToLower();
            if (string.IsNullOrEmpty(urlPath) || urlPath.Trim() == "/")
                urlPath = "root";

            if (_config.ReadWriteMode) return await GetArticle(urlPath, publishedOnly, onlyActive, lang);

            if (_distributedCache != null && _config.ReadWriteMode != true)
            {
                var cacheKey = RedisCacheService.GetPageCacheKey(_redisOptions.Value.CacheId, lang,
                    RedisCacheService.CacheOptions.Database, urlPath);

                var bytes = await _distributedCache.GetAsync(cacheKey);
                if (bytes == null)
                {
                    var model = await GetArticle(urlPath, publishedOnly, onlyActive, lang);
                    if (model != null)
                    {
                        var cacheBytes = Serialize(model);
                        var cacheOptions = new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow =
                                TimeSpan.FromSeconds(_redisOptions.Value.CacheDuration)
                        };
                        await _distributedCache.SetAsync(cacheKey, cacheBytes, cacheOptions);
                    }

                    return model;
                }

                return Deserialize<ArticleViewModel>(bytes);
            }

            return await GetArticle(urlPath, publishedOnly, onlyActive, lang);
        }

        /// <summary>
        ///     Private method used return an article view model.
        /// </summary>
        /// <param name="urlPath"></param>
        /// <param name="publishedOnly"></param>
        /// <param name="onlyActive"></param>
        /// <param name="lang">Language to translate the en-US into.</param>
        /// <returns>
        ///     <para>Returns an <see cref="ArticleViewModel" /> created with <see cref="BuildArticleViewModel" />.</para>
        ///     <para>NOTE: Cannot access articles that have been deleted.</para>
        /// </returns>
        private async Task<ArticleViewModel> GetArticle(string urlPath, bool publishedOnly, bool onlyActive,
            string lang)
        {
            urlPath = urlPath?.TrimStart('/');
            Article article;
            // Get time zone info
            //var pst = TimeZoneUtility.ConvertUtcDateTimeToPst(DateTime.UtcNow);

            var activeStatusCodes =
                onlyActive ? new[] { 0, 3 } : new[] { 0, 1, 3 }; // i.e. StatusCode.Active (DEFAULT) and StatusCode.Redirect

            if (publishedOnly)
                article = await _dbContext.Articles.Include(a => a.Layout)
                    .Where(a => a.UrlPath.ToLower() == urlPath &&
                                a.Published != null &&
                                a.Published <= DateTime.UtcNow &&
                                activeStatusCodes.Contains(a.StatusCode))
                    .OrderByDescending(o => o.VersionNumber).FirstOrDefaultAsync();
            else
                article = await _dbContext.Articles.Include(a => a.Layout)
                    .Where(a => a.UrlPath.ToLower() == urlPath &&
                                activeStatusCodes.Contains(a.StatusCode))
                    .OrderBy(o => o.VersionNumber)
                    .LastOrDefaultAsync();

            if (article == null) return null;

            return await BuildArticleViewModel(article, lang);
        }

        /// <summary>
        ///     This method puts an article into trash, and, all its versions.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>This method puts an article into trash. Use <see cref="RetrieveFromTrash" /> to restore an article. </para>
        ///     <para>WARNING: Make sure the menu MenuController.Index does not reference deleted files.</para>
        /// </remarks>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public async Task TrashArticle(int articleNumber)
        {
            var doomed = await _dbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (doomed == null) throw new KeyNotFoundException($"Article number {articleNumber} not found.");

            if (doomed.Any(a => a.UrlPath.ToLower() == "root"))
                throw new NotSupportedException(
                    "Cannot trash the home page.  Replace home page with another, then send to trash.");
            foreach (var article in doomed) article.StatusCode = (int)StatusCodeEnum.Deleted;

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        ///     Retrieves and article and all its versions from trash.
        /// </summary>
        /// <param name="articleNumber"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         Please be aware of the following:
        ///     </para>
        ///     <list type="bullet">
        ///         <item><see cref="Article.StatusCode" /> is set to <see cref="StatusCodeEnum.Active" />.</item>
        ///         <item><see cref="Article.Title" /> will be altered if a live article exists with the same title.</item>
        ///         <item>
        ///             If the title changed, the <see cref="Article.UrlPath" /> will be updated using
        ///             <see cref="HandleUrlEncodeTitle" />.
        ///         </item>
        ///         <item>The article and all its versions are set to unpublished (<see cref="Article.Published" /> set to null).</item>
        ///     </list>
        /// </remarks>
        public async Task RetrieveFromTrash(int articleNumber)
        {
            var redeemed = await _dbContext.Articles.Where(w => w.ArticleNumber == articleNumber).ToListAsync();

            if (redeemed == null) throw new KeyNotFoundException($"Article number {articleNumber} not found.");

            var title = redeemed.FirstOrDefault()?.Title.ToLower();

            // Avoid restoring an article that has a title that collides with a live article.
            if (await _dbContext.Articles.AnyAsync(a =>
                a.Title.ToLower() == title && a.ArticleNumber != articleNumber &&
                a.StatusCode == (int)StatusCodeEnum.Deleted))
            {
                var newTitle = title + " (" + await _dbContext.Articles.CountAsync() + ")";
                var url = HandleUrlEncodeTitle(newTitle);
                foreach (var article in redeemed)
                {
                    article.Title = newTitle;
                    article.UrlPath = url;
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }
            else
            {
                foreach (var article in redeemed)
                {
                    article.StatusCode = (int)StatusCodeEnum.Active;
                    article.Published = null;
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        #endregion

        #region SAVE ARTICLE METHODS

        /// <summary>
        ///     Updates an existing article, or inserts a new one.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="userId"></param>
        /// <param name="teamId"></param>
        /// <remarks>
        ///     <para>
        ///         If the article number is '0', a new article is inserted.  If a version number is '0', then
        ///         a new version is created. Recreates <see cref="ArticleViewModel" /> using method
        ///         <see cref="BuildArticleViewModel(Article, string)" />.
        ///     </para>
        ///     <list type="bullet">
        ///         <item>
        ///         </item>
        ///         <item>
        ///             Actions taken here by users are logged using <see cref="HandleLogEntry" />.
        ///         </item>
        ///         <item>
        ///             Title changes (and redirects) are handled by adding a new article with redirect info.
        ///         </item>
        ///         <item>
        ///             Publishing and cache management is handled by <see cref="HandlePublishing" />
        ///         </item>
        ///         <item>
        ///             The <see cref="ArticleViewModel" /> that is returned, is rebuilt using <see cref="BuildArticleViewModel" />
        ///             .
        ///         </item>
        ///         <item>
        ///             Flushes REDIS cache for both data and the page using <see cref="FlushRedis" />.
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <returns></returns>
        public async Task<ArticleViewModel> UpdateOrInsert(ArticleViewModel model, string userId, int? teamId = null)
        {
            Article article;

            if (!await _dbContext.Users.AnyAsync(a => a.Id == userId))
                throw new Exception($"User ID: {userId} not found!");

            //
            //  Validate that title is not already taken.
            //
            if (!await ValidateTitle(model.Title, model.ArticleNumber))
                throw new Exception($"Title '{model.Title}' already taken");

            bool isRoot = await _dbContext.Articles.AnyAsync(a => a.ArticleNumber == model.ArticleNumber && a.UrlPath == "root");

            //
            // Is this a new article?
            //
            if (model.ArticleNumber == 0)
            {
                //
                // If the article number is 0, then this is a new article.
                // The save action will give this a new unique article number.
                //

                // If no other articles exist, then make this the new root or home page.
                isRoot = (await _dbContext.Articles.CountAsync()) == 0;

                article = new Article
                {
                    ArticleNumber = await GetNextArticleNumber(),
                    VersionNumber = 1,
                    UrlPath = isRoot ? "root" : HandleUrlEncodeTitle(model.Title.Trim()),
                    ArticleLogs = new List<ArticleLog>(),
                    Updated = DateTime.UtcNow,
                    RoleList = model.RoleList
                };

                // Force publishing of a NEW home page.
                model.Published = isRoot ? DateTime.UtcNow : model.Published;

                var articleCount = await _dbContext.Articles.CountAsync();

                _dbContext.Articles.Add(article); // Set in an "add" state.
                HandleLogEntry(article, $"New article {articleCount}", userId);
                HandleLogEntry(article, "New version 1", userId);

                if (article.Published.HasValue || isRoot)
                    HandleLogEntry(article, "Publish", userId);

                // If this is part of a team, add that here.
                if (teamId != null)
                {
                    var team = await _dbContext.Teams.Include(i => i.Articles)
                        .FirstOrDefaultAsync(f => f.Id == teamId.Value);

                    team.Articles.Add(article);
                }

                //
                // Get rid of any old redirects
                //
                var oldRedirects = _dbContext
                    .Articles
                    .Where(w =>
                        w.StatusCode == (int)StatusCodeEnum.Redirect &&
                        w.UrlPath == article.UrlPath
                    );

                _dbContext.Articles.RemoveRange(oldRedirects);
            }
            else
            {
                //
                // Validate that this article already exists.
                //
                if (!await _dbContext.Articles.AnyAsync(a => a.ArticleNumber == model.ArticleNumber))
                    throw new Exception($"Article number: {model.ArticleNumber} not found!");

                //
                // Retrieve the article that we will be using.
                // This will either be used to create a new version (detached then added as new),
                // or updated in place.
                //
                article = await _dbContext.Articles.Include(i => i.ArticleLogs)
                    .FirstOrDefaultAsync(a => a.Id == model.Id);

                //
                // We are adding a new version.
                // DETACH and put into an ADD state.
                //
                if (model.VersionNumber == 0)
                {
                    article = new Article
                    {
                        ArticleNumber = model.ArticleNumber, // This stays the same
                        VersionNumber = await GetNextVersionNumber(model.ArticleNumber),
                        UrlPath = model.UrlPath,
                        HeaderJavaScript = article.HeaderJavaScript,
                        FooterJavaScript = article.FooterJavaScript,
                        ArticleLogs = new List<ArticleLog>(),
                        LayoutId = article.LayoutId,
                        Title = article.Title, // Keep this from previous version, will handle title change below.
                        Updated = DateTime.UtcNow
                    };

                    // Force the model into an unpublished state
                    model.Published = null;

                    await _dbContext.Articles.AddAsync(article); // Put this entry in an add state

                    HandleLogEntry(article, "New version", userId);
                }
                else
                {
                    HandleLogEntry(article, "Edit existing", userId);
                }

                //
                // Is the title changing? If so handle redirect if this is NOT the root.
                //
                if (!isRoot && !string.Equals(article.Title, model.Title, StringComparison.CurrentCultureIgnoreCase))
                {
                    // make all those articles with the old path inactive, so they don't conflict with URLs
                    var oldArticles = _dbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                        .ToListAsync().Result;

                    article.UrlPath = HandleUrlEncodeTitle(model.Title);

                    // Add redirect here
                    await _dbContext.Articles.AddAsync(new Article
                    {
                        Id = 0,
                        LayoutId = null,
                        ArticleNumber = 0,
                        StatusCode = (int)StatusCodeEnum.Redirect,
                        UrlPath = model.UrlPath, // Old URL
                        VersionNumber = 0,
                        Published =
                            TimeZoneUtility.ConvertUtcDateTimeToPst(
                                DateTime.UtcNow.AddDays(-1)), // Make sure this sticks!
                        Title = "Redirect",
                        Content = article.UrlPath, // New URL
                        Updated = DateTime.UtcNow,
                        HeaderJavaScript = null,
                        FooterJavaScript = null,
                        Layout = null,
                        ArticleLogs = null,
                        MenuItems = null,
                        FontIconId = null,
                        FontIcon = null
                    });


                    HandleLogEntry(article, $"Redirect {model.UrlPath} to {article.UrlPath}", userId);

                    //
                    // Update the path 
                    //
                    article.UrlPath = HandleUrlEncodeTitle(model.Title);

                    // We have to change the title and paths for all versions now.
                    foreach (var oldArticle in oldArticles)
                    {
                        // We have to change the title and paths for all versions now.
                        oldArticle.UrlPath = article.UrlPath;

                        oldArticle.Title = model.Title;
                        oldArticle.Updated = DateTime.UtcNow;
                    }

                    _dbContext.Articles.UpdateRange(oldArticles);
                }

                //
                // Is the role list changing?
                //
                if (!string.Equals(article.RoleList, model.RoleList, StringComparison.CurrentCultureIgnoreCase))
                {
                    // get all prior article versions, changing security now.
                    var oldArticles = _dbContext.Articles.Where(w => w.ArticleNumber == article.ArticleNumber)
                        .ToListAsync().Result;

                    HandleLogEntry(article, $"Changing role access from '{article.RoleList}' to '{model.RoleList}'.", userId);

                    //
                    // We have to change the title and paths for all versions now.
                    //
                    foreach (var oldArticle in oldArticles) oldArticle.RoleList = model.RoleList;
                }
            }

            //
            // Detect if the article is being published, and add log entry
            //
            HandlePublishing(model, article, userId);

            article.Title = model.Title.Trim();

            // When we save to the database, remove content editable attribute.
            article.Content = model.Content.Replace(" contenteditable=\"", " crx=\"",
                StringComparison.CurrentCultureIgnoreCase);

            //
            // Make sure everything server-side is saved in UTC time.
            //
            article.Published = model.Published?.ToUniversalTime();
            article.Updated = DateTime.UtcNow;

            article.HeaderJavaScript = model.HeaderJavaScript;
            article.FooterJavaScript = model.FooterJavaScript;

            article.RoleList = model.RoleList;

            // Save changes to database.
            await _dbContext.SaveChangesAsync();

            // Now, prior to sending model back, re-enable the content editable attribute.
            article.Content = model.Content.Replace(" crx=\"", " contenteditable=\"",
                StringComparison.CurrentCultureIgnoreCase);

            //await FlushRedis(article.UrlPath);
            return await BuildArticleViewModel(article, "en-US");
        }

        /// <summary>
        ///     Updates the date/time stamp for all published articles to current UTC time.
        /// </summary>
        /// <returns>Number of articles updated with new date/time</returns>
        /// <remarks>This action is used only for "publishing" entire websites.</remarks>
        public async Task<int> UpdateDateTimeStamps()
        {
            var articleIds = (await PrivateGetArticleList(_dbContext.Articles.AsQueryable(), false))?.Select(s => s.Id)
                .ToList();
            if (articleIds == null || articleIds.Any() == false) return 0;

            // DateTime.Now uses DateTime.UtcNow internally and then applies localization.
            // In short, use ToUniversalTime() if you already have DateTime.Now and
            // to convert it to UTC, use DateTime.UtcNow if you just want to retrieve the
            // current time in UTC.
            var now = DateTime.UtcNow;
            var count = await _dbContext.Articles.Where(a => articleIds.Contains(a.Id)).UpdateAsync(u => new Article
            {
                Updated = now
            });
            return count;
        }

        /// <summary>
        ///     Changes the status of an article by marking all versions with that status.
        /// </summary>
        /// <param name="articleNumber">Article to set status for</param>
        /// <param cref="StatusCodeEnum" name="code"></param>
        /// <param name="userId"></param>
        /// <exception cref="Exception">User ID or article number not found.</exception>
        /// <returns>Returns the number of versions for the given article where status was set</returns>
        public async Task<int> SetStatus(int articleNumber, StatusCodeEnum code, string userId)
        {
            if (!await _dbContext.Users.AnyAsync(a => a.Id == userId))
                throw new Exception($"User ID: {userId} not found!");

            var versions = await _dbContext.Articles.Where(a => a.ArticleNumber == articleNumber).ToListAsync();
            if (!versions.Any()) throw new Exception($"Article number: {articleNumber} not found!");

            foreach (var version in versions)
            {
                version.StatusCode = (int)code;
                version.ArticleLogs ??= new List<ArticleLog>();

                var statusText = code switch
                {
                    StatusCodeEnum.Deleted => "deleted",
                    StatusCodeEnum.Active => "active",
                    _ => "inactive"
                };

                version.ArticleLogs.Add(new ArticleLog
                {
                    ActivityNotes = $"Status changed to '{statusText}'.",
                    IdentityUserId = userId,
                    DateTimeStamp = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync();
            return versions.Count;
        }

        #endregion

        #region LISTS

        /// <summary>
        ///     Gets the latest versions of articles (published or not).
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        ///     <para>Note: Cannot list articles that are trashed.</para>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleList(string userId, bool showDefaultSort = true)
        {
            var articles = _dbContext.Articles.Include(i => i.Team)
                .Where(t => t.Team.Members.Any(m => m.UserId == userId));

            //= __dbContext.Teams.Include(i => i.Articles)
            //.Where(t => t.Members.Any(a => a.UserId == userId)).SelectMany(s => s.Articles);

            return await PrivateGetArticleList(articles, showDefaultSort);
        }

        /// <summary>
        ///     Gets the latest versions of articles (published or not).
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        ///     <para>Note: Cannot list articles that are trashed.</para>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleList(IQueryable<Article> query = null, bool showDefaultSort = true)
        {
            return await PrivateGetArticleList(query ?? _dbContext.Articles.AsQueryable(), showDefaultSort);
        }

        /// <summary>
        ///     Gets the latest versions of articles (published or not) for a specific team.
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        ///     <para>Note: Cannot list articles that are trashed.</para>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleList(int teamId)
        {
            var articles = _dbContext.Teams.Include(i => i.Articles)
                .Where(t => t.Id == teamId).SelectMany(s => s.Articles);

            return await PrivateGetArticleList(articles);
        }

        /// <summary>
        ///     Gets the latest versions of articles that are in the trash.
        /// </summary>
        /// <returns>Gets article number, version number, last data published (if applicable)</returns>
        /// <remarks>
        /// </remarks>
        public async Task<List<ArticleListItem>> GetArticleTrashList()
        {
            var data = await
                (from x in _dbContext.Articles
                 where x.StatusCode == (int)StatusCodeEnum.Deleted
                 group x by x.ArticleNumber
                    into g
                 select new
                 {
                     ArticleNumber = g.Key,
                     VersionNumber = g.Max(i => i.VersionNumber),
                     LastPublished = g.Max(m => m.Published),
                     Status = g.Max(f => f.StatusCode)
                 }).ToListAsync();

            var model = new List<ArticleListItem>();

            foreach (var item in data)
            {
                var art = await _dbContext.Articles.FirstOrDefaultAsync(
                    f => f.ArticleNumber == item.ArticleNumber && f.VersionNumber == item.VersionNumber
                );
                model.Add(new ArticleListItem
                {
                    ArticleNumber = art.ArticleNumber,
                    Id = art.Id,
                    LastPublished = item.LastPublished?.ToUniversalTime(),
                    Title = art.Title,
                    Updated = art.Updated.ToUniversalTime(),
                    VersionNumber = art.VersionNumber,
                    Status = art.StatusCode == 0 ? "Active" : "Inactive",
                    UrlPath = art.UrlPath
                });
            }

            return model;
        }

        #endregion

        #region PRIVATE METHODS

        private async Task<List<ArticleListItem>> PrivateGetArticleList(IQueryable<Article> articles, bool showDefaultSort = true)
        {

            var data = await
                (from x in articles
                 where x.StatusCode != (int)StatusCodeEnum.Deleted
                 group x by x.ArticleNumber
                    into g
                 select new
                 {
                     ArticleNumber = g.Key,
                     IsPublished = g.Max(i => i.Published)
                 }).ToListAsync();

            var model = new List<ArticleListItem>();

            foreach (var item in data)
            {
                Article art;

                if (item.IsPublished.HasValue)
                {
                    // If published, get the last published article, search by article number and published date and time.
                    art = await _dbContext.Articles.Include(i => i.Team).Where(
                    f => f.ArticleNumber == item.ArticleNumber && f.Published.HasValue).OrderBy(o => o.Id).LastOrDefaultAsync();
                }
                else
                {
                    // If not published, get the last entity ID for the article.
                    art = await _dbContext.Articles.Include(i => i.Team).Where(
                    f => f.ArticleNumber == item.ArticleNumber).OrderBy(o => o.Id).LastOrDefaultAsync();
                }

                var entity = new ArticleListItem
                {
                    ArticleNumber = art.ArticleNumber,
                    Id = art.Id,
                    IsDefault = art.UrlPath == "root",
                    LastPublished = art.Published.HasValue ? DateTime.SpecifyKind(art.Published.Value, DateTimeKind.Utc) : (DateTime?)null,
                    Title = art.Title,
                    Updated = DateTime.SpecifyKind(art.Updated, DateTimeKind.Utc),
                    VersionNumber = art.VersionNumber,
                    Status = art.StatusCode == 0 ? "Active" : "Inactive",
                    UrlPath = art.UrlPath,
                    TeamName = art.Team == null ? "" : art.Team.TeamName
                };
                model.Add(entity);
            }

            if (showDefaultSort)
            {
                return model.OrderByDescending(o => o.IsDefault).ThenBy(t => t.Title).ToList();
            }

            return model.ToList();
        }

        /// <summary>
        ///     This method creates an <see cref="ArticleViewModel" /> ready for display and edit.
        /// </summary>
        /// <param name="article"></param>
        /// <param name="lang"></param>
        /// <returns>
        ///     <para>Returns <see cref="ArticleViewModel" /> that includes:</para>
        ///     <list type="bullet">
        ///         <item>
        ///             Current ArticleVersionInfo
        ///         </item>
        ///         <item>
        ///             If the site is in authoring or publishing mode (<see cref="ArticleViewModel.ReadWriteMode" />)
        ///         </item>
        ///     </list>
        /// </returns>
        private async Task<ArticleViewModel> BuildArticleViewModel(Article article, string lang)
        {
            if (article.Layout == null)
            {
                var defaultLayout = await GetDefaultLayout(lang);
                article.LayoutId = defaultLayout.Id;
                article.Layout = defaultLayout.GetLayout();
            }

            var languageName = "US English";

            if (_translationServices != null && !lang.Equals("en", StringComparison.CurrentCultureIgnoreCase) &&
                !lang.Equals("en-us", StringComparison.CurrentCultureIgnoreCase))
            {
                var result = await _translationServices.GetTranslation(lang, "en-us", new[] { article.Title, article.Content });

                languageName =
                    (await GetSupportedLanguages(lang))?.Languages.FirstOrDefault(f => f.LanguageCode == lang)
                    ?.DisplayName ?? lang;

                article.Title = result.Translations[0].TranslatedText;

                article.Content = result.Translations[1].TranslatedText;
            }

            var cacheKey = _redisOptions == null
                ? "nocache"
                : RedisCacheService.GetPageCacheKey(_redisOptions.Value.CacheId, lang,
                    RedisCacheService.CacheOptions.Html, article.UrlPath);

            return new ArticleViewModel
            {
                ArticleNumber = article.ArticleNumber,
                LanguageCode = lang,
                LanguageName = languageName,
                CacheKey = cacheKey,
                CacheDuration = _redisOptions?.Value.CacheDuration ?? 60,
                Content = article.Content,
                StatusCode = (StatusCodeEnum)article.StatusCode,
                Id = article.Id,
                Published = article.Published.HasValue ? DateTime.SpecifyKind(article.Published.Value, DateTimeKind.Utc) : (DateTime?)null,
                Title = article.Title,
                UrlPath = article.UrlPath,
                Updated = DateTime.SpecifyKind(article.Updated, DateTimeKind.Utc),
                VersionNumber = article.VersionNumber,
                HeaderJavaScript = article.HeaderJavaScript,
                FooterJavaScript = article.FooterJavaScript,
                Layout = await BuildDefaultLayout(lang),
                ReadWriteMode = _config.ReadWriteMode,
                RoleList = article.RoleList
            };
        }

        /// <summary>
        ///     This is the private, internal method used by <see cref="BuildMenu" /> to create menu content.
        /// </summary>
        /// <returns>
        ///     Returns a menu content following the pattern seen here in
        ///     <a
        ///         href="https://github.com/Office-of-Digital-Innovation/California-State-Template-NET-core/blob/master/Pages/Shared/header/_Navigation.cshtml">
        ///         GitHub
        ///     </a>
        ///     .
        /// </returns>
        private async Task<string> InternalBuildMenu()
        {
            var items = await _dbContext.MenuItems.Include(c => c.ChildItems).Where(m => m.ParentId == null)
                .OrderBy(o => o.SortOrder)
                .ToListAsync();

            if (!items.Any()) return string.Empty;

            var builder = new StringBuilder();

            //builder.Append("<ul id =\"nav_list\" class=\"top-level-nav\">");

            foreach (var menuItem in items)
                if (menuItem.ChildItems.Count > 0)
                {
                    builder.Append("<li class=\"nav-item\">");
                    builder.Append(
                        $"<a class=\"nav-link\" href=\"{menuItem.Url}\" ><span class=\"{menuItem.IconCode}\"></span> {menuItem.MenuText}</a>");
                    //if (menuItem.ChildItems.Any())
                    //{ 

                    //    builder.Append("<div class=\"sub-nav\"><ul class=\"sub-nav\">");

                    //    foreach (var ddItem in menuItem.ChildItems)
                    //    {
                    //        builder.Append($"<li class=\"unit1\"><a class=\"second-level-link\" href=\"{ddItem.Url}\"><span class=\"{ddItem.IconCode}\"></span> {ddItem.MenuText}</a></li>");
                    //    }
                    //    builder.Append("</ul></div>");
                    //}
                    builder.Append("</li>");
                }
                else
                {
                    builder.Append("<li class=\"nav-item\">");
                    builder.Append(
                        $"<a class=\"nav-link\" href=\"{menuItem.Url}\" ><span class=\"{menuItem.IconCode}\"></span> {menuItem.MenuText}</a>");
                    builder.Append("</li>");
                }

            //builder.Append("</ul>");

            return builder.ToString();
        }

        /// <summary>
        ///     Returns the menu from <see cref="IDistributedCache" />, or creates a new menu if cache is null or not available.
        /// </summary>
        /// <returns>Menu content as <see cref="string" />.</returns>
        /// <remarks>
        ///     If the menu can't be pulled from <see cref="IDistributedCache" />, this method creates a fresh copy of
        ///     content using <see cref="InternalBuildMenu" />.
        /// </remarks>
        public async Task<string> BuildMenu(string lang)
        {
            if (_distributedCache == null || _config.ReadWriteMode) return await InternalBuildMenu();

            var key = RedisCacheService.GetPageCacheKey(_redisOptions.Value.CacheId, lang,
                RedisCacheService.CacheOptions.Menu, "menu");
            var menu = await _distributedCache.GetStringAsync(key);

            if (menu != null) return menu;

            menu = await InternalBuildMenu();
            await _distributedCache.SetStringAsync(key, menu, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(150)
            });

            return menu;

        }

        private async Task<int> GetNextVersionNumber(int articleNumber)
        {
            return await _dbContext.Articles.Where(a => a.ArticleNumber == articleNumber)
                .MaxAsync(m => m.VersionNumber) + 1;
        }

        private async Task<int> GetNextArticleNumber()
        {
            if (await _dbContext.Articles.AnyAsync())
                return await _dbContext.Articles.MaxAsync(m => m.ArticleNumber) + 1;

            return 1;
        }


        /// <summary>
        ///     Get the list of languages supported for translation by Google.
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public async Task<SupportedLanguages> GetSupportedLanguages(string lang)
        {
            if (_translationServices == null) return new SupportedLanguages();

            if (_config.ReadWriteMode || _distributedCache == null)
            {
                var languages = await _translationServices.GetSupportedLanguages(lang);
                return languages;
            }

            var cachKey = RedisCacheService.GetPageCacheKey(_redisOptions.Value.CacheId, lang,
                RedisCacheService.CacheOptions.GoogleLanguages,
                "GoogleLang");

            var bytes = await _distributedCache.GetAsync(cachKey);

            //
            // If the list was found in Redis, return it now straight away.
            //
            if (bytes != null)
                return Deserialize<SupportedLanguages>(bytes);

            //
            // Otherwise, get the list, and stash in Redis
            //
            var model = await _translationServices.GetSupportedLanguages(lang);
            var cacheBytes = Serialize(model);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20)
            };
            await _distributedCache.SetAsync(cachKey, cacheBytes, cacheOptions);

            return model;
        }

        /// <summary>
        ///     Provides a standard method for turning a title into a URL Encoded path.
        /// </summary>
        /// <param name="title">Title to be converted into a URL.</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>This is accomplished using <see cref="HttpUtility.UrlEncode(string)" />.</para>
        ///     <para>Blanks are turned into underscores (i.e. "_").</para>
        ///     <para>All strings are normalized to lower case.</para>
        /// </remarks>
        public string HandleUrlEncodeTitle(string title)
        {
            return HttpUtility.UrlEncode(title.Trim().Replace(" ", "_").ToLower());
        }

        private void HandlePublishing(ArticleViewModel model, Article article, string userId)
        {
            //
            // Detect if the article is being published, and add log entry
            //
            if (article.Published != model.Published)
                HandleLogEntry(article, model.Published.HasValue ? "Publish" : "Un-publish", userId);
        }

        private void HandleLogEntry(Article article, string note, string userId)
        {
            article.ArticleLogs ??= new List<ArticleLog>();
            article.ArticleLogs.Add(new ArticleLog
            {
                ArticleId = article.Id,
                IdentityUserId = userId,
                ActivityNotes = note,
                DateTimeStamp = DateTime.UtcNow
            });
        }

        /// <summary>
        ///     Makes an article the new home page.
        /// </summary>
        /// <param name="id">Article Id (row key)</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     The old home page has its URL changed from "root" to its normal path.  Also writes to the log
        ///     using <see cref="HandleLogEntry" />. Also flushes REDIS cache for the home page.
        /// </remarks>
        public async Task NewHomePage(int id, string userId)
        {
            //
            // Can't make a deleted file the new home page.
            //
            var newHome = await _dbContext.Articles
                .Where(w => w.Id == id && w.StatusCode != (int)StatusCodeEnum.Deleted).ToListAsync();
            if (newHome == null) throw new Exception($"Article Id {id} not found.");
            var utcDateTimeNow = DateTime.UtcNow;
            if (newHome.All(a => a.Published != null && a.Published.Value <= utcDateTimeNow)) throw new Exception("Article has not been published yet.");

            var currentHome = await _dbContext.Articles.Where(w => w.UrlPath.ToLower() == "root").ToListAsync();

            var newUrl = HandleUrlEncodeTitle(currentHome.FirstOrDefault()?.Title);

            foreach (var article in currentHome) article.UrlPath = newUrl;

            await _dbContext.SaveChangesAsync();

            foreach (var article in newHome) article.UrlPath = "root";

            await _dbContext.SaveChangesAsync();

            var newHomeArticle = newHome.OrderByDescending(o => o.Id).FirstOrDefault(w => w.Published != null);

            if (newHomeArticle != null)
                HandleLogEntry(newHomeArticle, $"Article {newHomeArticle.ArticleNumber} is now the new home page.",
                    userId);

            //
            // Flush REDIS for the home page.
            //
            await FlushRedis("root");
        }

        /// <summary>
        ///     Gets the default layout, including navigation menu.
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="includeMenu"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         Inserts a Bootstrap style nav bar where this '&lt;!--{COSMOS-UL-NAV}--&gt;' is placed in the
        ///         <see cref="LayoutViewModel.HtmlHeader" />
        ///     </para>
        /// </remarks>
        public async Task<LayoutViewModel> GetDefaultLayout(string lang, bool includeMenu = true)
        {
            if (_distributedCache == null || _config.ReadWriteMode)
                return await BuildDefaultLayout(lang, includeMenu);

            var cacheKey = RedisCacheService.GetPageCacheKey(_redisOptions.Value.CacheId, lang,
                RedisCacheService.CacheOptions.Database, "defMenu");

            var bytes = await _distributedCache.GetAsync(cacheKey);

            if (bytes != null)
                return Deserialize<LayoutViewModel>(bytes);

            var model = await BuildDefaultLayout(lang, includeMenu);

            if (model != null)
            {
                var cacheBytes = Serialize(model);
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_redisOptions.Value.CacheDuration)
                };
                await _distributedCache.SetAsync(
                    cacheKey,
                    cacheBytes
                    , cacheOptions);
            }

            return model;

        }

        /// <summary>
        ///     Builds a default layout, including navigation menu.
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="includeMenu"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         Inserts a <see href="https://getbootstrap.com/docs/4.0/components/navbar/">Bootstrap style nav bar</see>
        ///         where this '&lt;!--{COSMOS-UL-NAV}--&gt;' is placed in the HtmlHeader
        ///     </para>
        /// </remarks>
        private async Task<LayoutViewModel> BuildDefaultLayout(string lang, bool includeMenu = true)
        {
            LayoutViewModel layoutViewModel;
            var layout = await _dbContext.Layouts.FirstOrDefaultAsync(a => a.IsDefault) ??
                         await _dbContext.Layouts.FirstOrDefaultAsync();

            //
            // If no layout exists, creates a new default one.
            //
            if (layout == null)
            {
                layoutViewModel = new LayoutViewModel();
                layout = layoutViewModel.GetLayout();
                await _dbContext.Layouts.AddAsync(layout);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                layoutViewModel = new LayoutViewModel(layout);
            }

            if (!includeMenu) return layoutViewModel;

            //
            // Only add a menu if one is defined.  It will be null if no menu exists.
            //
            var menuHtml = await BuildMenu(lang);
            if (string.IsNullOrEmpty(menuHtml)) return new LayoutViewModel(layout);


            // Make sure no changes are tracked with the layout.
            _dbContext.Entry(layout).State = EntityState.Detached;

            layout.HtmlHeader = layout.HtmlHeader?.Replace("<!--{COSMOS-UL-NAV}-->",
                "<!--{COSMOS-UL-NAV}-->" +
                $"<ul class=\"navbar-nav mr-auto\">{menuHtml}</ul>");

            return new LayoutViewModel(layout);
        }

        #endregion

        #region DISTRIBUTED CACHE FUNCTIONS

        /// <summary>
        ///     Flushes REDIS cache for both data and page
        /// </summary>
        /// <param name="urlPath"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Uses <see cref="IDistributedCache.RemoveAsync" /> to remove both data and page caches.
        /// </remarks>
        public async Task<FlushRedisResultViewModel> FlushRedis(string urlPath)
        {
            var path = urlPath.ToLower();
            var model = new FlushRedisResultViewModel
            {
                UrlPath = path
            };
            if (_distributedCache == null) return model;
            //var urls = (await GetArticleList()).Select(s => s.UrlPath).ToList();

            var redisService = new RedisCacheService(_redisOptions);

            var keys = redisService.GetKeys();
            model.CacheConnected = true;

            foreach (var key in keys)
            {
                var k = key.ToString();
                if (string.IsNullOrEmpty(path))
                {
                    await _distributedCache.RemoveAsync(k);
                    model.Keys.Add(k);
                }
                else
                {
                    if (k.Contains(path, StringComparison.CurrentCultureIgnoreCase))
                    {
                        await _distributedCache.RemoveAsync(k);
                        model.Keys.Add(k);
                    }
                }
            }

            return model;
        }

        /// <summary>
        ///     Serializes an object using <see cref="Newtonsoft.Json.JsonConvert.SerializeObject(object)" />
        ///     and <see cref="System.Text.Encoding.UTF32" />.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static byte[] Serialize(object obj)
        {
            if (obj == null) return null;
            return Encoding.UTF32.GetBytes(JsonConvert.SerializeObject(obj));
        }

        /// <summary>
        ///     Deserializes an object using <see cref="Newtonsoft.Json.JsonConvert.DeserializeObject(string)" />
        ///     and <see cref="System.Text.Encoding.UTF32" />.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static T Deserialize<T>(byte[] bytes)
        {
            var data = Encoding.UTF32.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(data);
        }

        #endregion
    }
}