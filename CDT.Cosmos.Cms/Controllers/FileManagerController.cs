using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Common.Data.Logic;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CDT.Cosmos.Cms.Controllers
{
    [Authorize(Roles = "Administrators, Editors, Authors, Team Members")]
    public class FileManagerController : BaseController
    {
        public FileManagerController(ILogger<FileManagerController> logger,
            ApplicationDbContext dbContext,
            UserManager<IdentityUser> userManager,
            IOptions<SiteCustomizationsConfig> options,
            IDistributedCache distributedCache,
            ArticleLogic articleLogic,
            IOptions<AzureBlobServiceConfig> blobConfig,
            IOptions<RedisContextConfig> redisOptions,
            AzureBlobService blobService,
            IWebHostEnvironment hostEnvironment) : base(options,
            dbContext,
            logger,
            userManager,
            articleLogic,
            distributedCache,
            redisOptions)
        {
            _blobService = blobService;
            _blobConfig = blobConfig;
            _options = options;
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            if (_options.Value.ReadWriteMode)
            {
                await EnsureRootFoldersExist();

                try
                {
                    var teams = await GetTeamsForUser();

                    var teamId = GetSelectedTeamId(teams);

                    if (User.IsInRole("Team Members"))
                        if (!teams.Any())
                            // User does not belong to any teams yet.
                            return Unauthorized();

                    ViewData["BlobEndpointUrl"] = GetBlobRootUrl();

                    return View(new FileManagerViewModel
                    {
                        TeamId = teamId,
                        TeamFolders = teams
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Index(FileManagerViewModel model)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    var teams = await GetTeamsForUser();

                    ViewData["BlobEndpointUrl"] = GetBlobRootUrl();

                    return View(new FileManagerViewModel
                    {
                        TeamId = model.TeamId,
                        TeamFolders = teams
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            return NotFound();
        }

        /// <summary>
        ///     Gets the list of teams for the current logged in user.
        /// </summary>
        /// <returns></returns>
        private async Task<List<SelectListItem>> GetTeamsForUser()
        {
            List<SelectListItem> teams;
            if (User.IsInRole("Team Members"))
            {
                var user = await UserManager.GetUserAsync(User);

                teams = await DbContext.Teams
                    .Where(t => t.Members.Any(a => a.UserId == user.Id))
                    .OrderBy(o => o.Id)
                    .Select(s => new SelectListItem
                    {
                        Text = "Team (" + s.Id + "): " + s.TeamName,
                        Value = s.Id.ToString()
                    })
                    .ToListAsync();
            }
            else
            {
                // Global users can see all teams.
                teams = await DbContext.Teams
                    .OrderBy(o => o.Id)
                    .Select(s => new SelectListItem
                    {
                        Text = "Team (" + s.Id + "): " + s.TeamName,
                        Value = s.Id.ToString()
                    })
                    .ToListAsync();
            }

            return teams;
        }

        private int? GetSelectedTeamId(List<SelectListItem> items)
        {
            if (items.Any())
            {
                var selected = items.FirstOrDefault(f => f.Selected);
                if (selected != null)
                    if (int.TryParse(selected.Value, out var teamId))
                        return teamId;
            }

            return null;
        }

        #region PRIVATE FIELDS

        private readonly IOptions<AzureBlobServiceConfig> _blobConfig;
        private readonly IOptions<SiteCustomizationsConfig> _options;
        private readonly ILogger<FileManagerController> _logger;
        private readonly AzureBlobService _blobService;
        private readonly IWebHostEnvironment _hostEnvironment;

        #endregion

        #region HELPER METHODS

        /// <summary>
        ///     Makes sure all root folders exist.
        /// </summary>
        /// <returns></returns>
        public async Task EnsureRootFoldersExist()
        {
            await _blobService.CreateFolder(GetAbsolutePath(null, "/"));
            await _blobService.CreateFolder(GetAbsolutePath(null, "/teams/"));
            var teamIds = await DbContext.Teams.Select(a => a.Id).ToListAsync();
            if (teamIds.Any())
                foreach (var teamId in teamIds)
                    await _blobService.CreateFolder(GetAbsolutePath("/teams/", teamId.ToString()));
        }

        /// <summary>
        ///     Encodes a URL
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <remarks>
        ///     For more information, see
        ///     <a
        ///         href="https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata#blob-names">
        ///         documentation
        ///     </a>
        ///     .
        /// </remarks>
        public string UrlEncode(string path)
        {
            var parts = ParsePath(path);
            var urlEncodedParts = new List<string>();
            foreach (var part in parts) urlEncodedParts.Add(HttpUtility.UrlEncode(part.Replace(" ", "-")));

            return TrimPathPart(string.Join('/', urlEncodedParts));
        }


        #endregion

        #region FILE MANAGER FUNCTIONS

        /// <summary>
        ///     Creates a new entry, using relative path-ing, and normalizes entry name to lower case.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="entry"></param>
        /// <param name="teamId"></param>
        /// <returns><see cref="JsonResult" />(<see cref="FileManagerEntry" />)</returns>
        /// <remarks>Uses <see cref="GetFileManagerEntry{T}" /> to build the file manager entry.</remarks>
        public virtual ActionResult Create(string target, FileManagerEntry entry, int? teamId)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    //
                    // FULL OR ABSOLUTE BLOB PATH
                    //
                    entry.Path = target.ToLower();
                    entry.Name = entry.Name.ToLower();
                    entry.Extension = entry.Extension?.ToLower();
                    target = target?.ToLower();

                    var fullPath = GetAbsolutePath(entry.Path, entry.Name);
                    fullPath = UrlEncode(fullPath);

                    var byteArray = Encoding.ASCII.GetBytes($"This is a folder stub file for {target}.");
                    using var stream = new MemoryStream(byteArray);
                    var stub = new FormFile(stream, 0, stream.Length, "folder.stubxx", "folder.stubxx");
                    var blobClient = _blobService.UploadFile<BlobClient>(fullPath, stub).Result;

                    //
                    // The blob client will have the full path to the item.  Relatively speaking
                    // the FileManager may not be starting at "/", but instead at something like "pub/".
                    // In other words, the file manager is working from a "relative" path.
                    //
                    var fileManagerEntry = GetFileManagerEntry(blobClient, true, teamId).Result;

                    //
                    // RELATIVE PATH
                    //
                    fileManagerEntry.Path = GetRelativePath(fileManagerEntry.Path);

                    //{"Name":"test","Size":0,"EntryType":1}
                    return Json(fileManagerEntry);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            return NotFound();
        }

        /// <summary>
        ///     Deletes a folder, normalizes entry to lower case.
        /// </summary>
        /// <param name="entry">Item to delete using relative path</param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public async Task<ActionResult> Destroy(FileManagerEntry entry, int? teamId)
        {
            /*
             * EXAMPLE FileManagerEntry:
             *
                Name: Screenshot 2020-11-24 204618
                Size: 22589
                Path: Documents/New Folder/Screenshot 2020-11-24 204618.jpg
                Extension: .jpg
                IsDirectory: false
                HasDirectories: false
                Created: Thu Dec 31 2020 07:05:29 GMT-0800 (Pacific Standard Time)
                CreatedUtc: Thu Dec 31 2020 07:05:29 GMT-0800 (Pacific Standard Time)
                Modified: Thu Dec 31 2020 07:05:29 GMT-0800 (Pacific Standard Time)
                ModifiedUtc: Thu Dec 31 2020 07:05:29 GMT-0800 (Pacific Standard Time)
             *
             */
            if (_options.Value.ReadWriteMode)
                try
                {
                    //
                    // CONVERT TO FULL PATH
                    //
                    var path = GetAbsolutePath(entry.Path).ToLower();

                    if (entry.IsDirectory)
                        await DeleteDirectory(path);
                    else
                        await DeleteFile(path);
                    return Json(new object[0]);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            return NotFound();
        }

        /// <summary>
        ///     Read files for a given path, retuning <see cref="AppendBlobClient" />, not <see cref="BlockBlobClient" />.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="teamId"></param>
        /// <returns>List of items found at target search, relative</returns>
        [HttpPost]
        public async Task<IActionResult> Read(string target, int? teamId = null)
        {
            target = string.IsNullOrEmpty(target) ? "" : HttpUtility.UrlDecode(target);

            if (_options.Value.ReadWriteMode)
            {

                //
                // GET FULL OR ABSOLUTE PATH
                //
                var model = await GetFiles(target, teamId);

                return Json(model);
            }

            return NotFound();
        }

        public async Task<IActionResult> ImageBrowserRead(string path)
        {
            return await FileBrowserRead(path, "i");
        }

        public async Task<IActionResult> FileBrowserRead(string path, string fileType = "f")
        {
            path = string.IsNullOrEmpty(path) ? "" : HttpUtility.UrlDecode(path);

            if (_options.Value.ReadWriteMode)
            {

                var model = await GetFiles(path, null);

                string[] fileExtensions = null;
                switch (fileType)
                {
                    case "f":
                        fileExtensions = AllowedFileExtensions.GetFilterForViews(AllowedFileExtensions.ExtensionCollectionType.FileUploads).Split(',').Select(s => s.Trim().ToLower()).ToArray();
                        break;
                    case "i":
                        fileExtensions = AllowedFileExtensions.GetFilterForViews(AllowedFileExtensions.ExtensionCollectionType.ImageUploads).Split(',').Select(s => s.Trim().ToLower()).ToArray();
                        break;
                }

                var jsonModel = new List<FileBrowserEntry>();

                foreach (var entry in model)
                {
                    if (entry.IsDirectory || fileExtensions == null)
                    {
                        jsonModel.Add(new FileBrowserEntry()
                        {
                            EntryType = entry.IsDirectory ? FileBrowserEntryType.Directory : FileBrowserEntryType.File,
                            Name = $"{entry.Name}{entry.Extension}",
                            Size = entry.Size
                        });
                    }
                    else if (fileExtensions.Contains(entry.Extension.TrimStart('.')))
                    {
                        jsonModel.Add(new FileBrowserEntry()
                        {
                            EntryType = entry.IsDirectory ? FileBrowserEntryType.Directory : FileBrowserEntryType.File,
                            Name = $"{entry.Name}{entry.Extension}",
                            Size = entry.Size
                        });
                    }
                }

                return Json(jsonModel.Select(s => new KendoFileBrowserEntry(s)).ToList());
            }

            return NotFound();
        }

        [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new [] { "path" })]
        public async Task<IActionResult> CreateThumbnail(string path)
        {
            path = string.IsNullOrEmpty(path) ? "" : HttpUtility.UrlDecode(path);

            var searchPath = GetAbsolutePath(null, path);

            var blobClient = _blobService.GetBlobClient(searchPath);

            try
            {
                using (var fileStream = await blobClient.OpenReadAsync())
                {
                    // 80 x 80
                    var desiredSize = new ImageSizeModel();

                    const string contentType = "image/png";

                    var thumbnailCreator = new ThumbnailCreator();

                    return File(thumbnailCreator.Create(fileStream, desiredSize, contentType), contentType);
                }
            }
            catch
            {
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, "images\\ImageIcon.png");
                using (var fileStream = System.IO.File.OpenRead(filePath))
                {
                    // 80 x 80
                    var desiredSize = new ImageSizeModel();

                    const string contentType = "image/png";

                    var thumbnailCreator = new ThumbnailCreator();

                    return File(thumbnailCreator.Create(fileStream, desiredSize, contentType), contentType);
                }
            }
        }

        /// <summary>
        ///     Updates the name an entry with a given entry, normalize names to lower case.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="teamId"></param>
        /// <returns>An empty <see cref="ContentResult" />.</returns>
        /// <exception cref="Exception">Forbidden</exception>
        [HttpPost]
        public async Task<ActionResult> Update(FileManagerEntry entry, int? teamId)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    // entry.Path is the full path to the thing being renamed.
                    // Example: MyFolder/OldName
                    //
                    // entry.Name is the new name
                    // Example: NewName
                    //
                    // The above example will rename MyFolder/OldName to MyFolder/NewName
                    //

                    entry.Path = entry.Path?.ToLower();
                    entry.Name = entry.Name?.ToLower();
                    entry.Extension = entry.Extension?.ToLower();

                    var source = GetAbsolutePath(entry.Path);
                    string destination;
                    if (!string.IsNullOrEmpty(source) && source.Contains("/"))
                    {
                        var pathParts = source.Split("/");
                        // For the following line, see example 3 here: https://stackoverflow.com/questions/3634099/c-sharp-string-array-replace-last-element
                        pathParts[^1] = entry.Name!;
                        destination = string.Join("/", pathParts);
                    }
                    else
                    {
                        destination = entry.Name;
                    }

                    // Encode using our own rules
                    destination = TrimPathPart(UrlEncode(destination));

                    var blobs = await _blobService.ListBlobsHierarchicalListing(source, null);

                    foreach (var blob in blobs)
                    {
                        var newName = blob.Name.Replace(source!, destination);
                        var copyResult = await _blobService.CopyBlob(blob.Name, newName);
                        if (!copyResult.Exists() || !await _blobService.DeleteBlob(blob.Name))
                            throw new Exception($"Update failed when trying to make new copy of {entry.Name}.");
                    }

                    // File manager is expecting the file name to come back without an extension.
                    entry.Name = Path.GetFileNameWithoutExtension(destination);
                    entry.Path = GetRelativePath(destination);
                    entry.Extension = entry.IsDirectory || string.IsNullOrEmpty(entry.Extension) ? "" : entry.Extension;

                    // Example: {"Name":"Wow","Size":0,"Path":"Wow","Extension":"","IsDirectory":true,"HasDirectories":false,"Created":"2020-10-30T18:14:16.0772789+00:00","CreatedUtc":"2020-10-30T18:14:16.0772789Z","Modified":"2020-10-30T18:14:16.0772789+00:00","ModifiedUtc":"2020-10-30T18:14:16.0772789Z"}
                    return Json(entry);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            return NotFound();
        }

        private async Task<List<FileManagerEntry>> GetFiles(string target, int? teamId)
        {
            var rootPath = GetAbsolutePath("");
            target = GetAbsolutePath(target);

            //var results = await InternalRead(fullPath, teamId);
            var blobObjects = await _blobService.Search(target, null);

            if (!blobObjects.Any()) return new List<FileManagerEntry>();

            var folders = blobObjects.Where(w => w.IsPrefix && w.Prefix != target + "/").ToList();
            var files = blobObjects.Where(w =>
                w.IsPrefix == false &&
                w.Blob.Name.EndsWith("folder.stubxx") == false && w.Blob.Properties.BlobType == BlobType.Append);

            // Filter by team ID
            if (teamId.HasValue)
            {
                if (target == rootPath)
                {
                    folders = folders.Where(f => f.Prefix.StartsWith($"{rootPath}/teams")).ToList();
                    files = new List<BlobHierarchyItem>();  // Team user should not see any files here.
                }
                else
                {
                    folders = folders.Where(f => f.Prefix.StartsWith($"{rootPath}/teams/{teamId.Value.ToString()}")).ToList();
                    files = files.Where(b => b.Blob.Name.StartsWith($"{rootPath}/teams/{teamId.Value.ToString()}")).ToList();
                }
            }

            var fileSystemList = new List<FileManagerEntry>();

            foreach (var folder in folders)
            {
                var folderName = folder.Prefix.TrimEnd('/').Split("/").LastOrDefault();
                
                var entry = new FileManagerEntry
                {
                    Created = DateTime.Now,
                    CreatedUtc = DateTime.UtcNow,
                    Extension = string.Empty,
                    IsDirectory = true,
                    Modified = DateTime.Now,
                    ModifiedUtc = DateTime.UtcNow,
                    Name = folderName, // Get the name of the last folder
                    Path = GetRelativePath(null, folder.Prefix),
                    Size = 0,
                    HasDirectories = false
                };
                fileSystemList.Add(entry);
            }

            foreach (var file in files)
            {
                var entry = GetFileManagerEntry(file, false, null).Result;
                fileSystemList.Add(entry);
            }

            return fileSystemList;
        }

        private async Task DeleteFile(string path)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    var blobClient = _blobService.GetBlobClient(path);
                    if (!await blobClient.DeleteIfExistsAsync())
                        throw new Exception($"Could not delete file: ({path}).");

                    return;
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            throw new UnauthorizedAccessException("Unauthorized");
        }

        private async Task DeleteDirectory(string path)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    if (path == "files" || path == "images")
                        throw new UnauthorizedAccessException($"Cannot delete folder {path}.");
                    var blobs = await _blobService.ListBlobsHierarchicalListing(path, null);

                    foreach (var blobItem in blobs)
                        if (!blobItem.Deleted)
                            await DeleteFile(blobItem.Name);

                    return;
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            throw new UnauthorizedAccessException("Unauthorized");
        }

        /// <summary>
        ///     Takes information from a <see cref="BlobClient" />, file name and path, and returns a
        ///     <see cref="FileManagerEntry" />.
        /// </summary>
        /// <param name="blobClient"></param>
        /// <param name="isDirectory"></param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     Local times are set to Pacific Standard Time.
        /// </remarks>
        private async Task<FileManagerEntry> GetFileManagerEntry<T>(T blobClient, bool isDirectory, int? teamId)
        {
            if (blobClient == null) throw new Exception("blobClient cannot be null.");
            //string name;
            DateTime createdOn, createdOnUtc, modifiedOn, modifiedOnUtc;
            long contentLength;
            string extension;
            string path;

            if (typeof(T) == typeof(BlobClient))
            {
                var obj = blobClient as BlobClient;

                // ReSharper disable once PossibleNullReferenceException
                var props = await obj.GetPropertiesAsync();

                createdOn = props.Value.CreatedOn.UtcDateTime;
                createdOnUtc = props.Value.CreatedOn.UtcDateTime;
                modifiedOn = props.Value.LastModified.UtcDateTime;
                modifiedOnUtc = props.Value.LastModified.UtcDateTime;

                contentLength = props.Value.ContentLength;
                extension = isDirectory ? "" : Path.GetExtension(obj.Name);
                path = obj.Name;
            }
            else if (typeof(T) == typeof(AppendBlobClient))
            {
                var obj = blobClient as AppendBlobClient;

                // ReSharper disable once PossibleNullReferenceException
                var props = await obj.GetPropertiesAsync();

                createdOn = props.Value.CreatedOn.UtcDateTime;
                createdOnUtc = props.Value.CreatedOn.UtcDateTime;
                modifiedOn = props.Value.LastModified.UtcDateTime;
                modifiedOnUtc = props.Value.LastModified.UtcDateTime;

                contentLength = props.Value.ContentLength;
                extension = isDirectory ? "" : Path.GetExtension(obj.Name);
                path = obj.Name;
            }
            else if (typeof(T) == typeof(BlobHierarchyItem))
            {
                var obj = blobClient as BlobHierarchyItem;

                if (obj != null && obj.IsBlob)
                {
                    var props = obj.IsBlob ? obj.Blob.Properties : null;

                    // ReSharper disable once PossibleNullReferenceException
                    createdOn = props.CreatedOn?.Date ?? DateTime.UtcNow;
                    createdOnUtc = props.CreatedOn?.UtcDateTime ?? DateTime.UtcNow;
                    modifiedOn = props.LastModified?.DateTime ?? DateTime.UtcNow;
                    modifiedOnUtc = props.LastModified?.UtcDateTime ?? DateTime.UtcNow;

                    contentLength = props.ContentLength ?? 0;
                    extension = isDirectory ? "" : Path.GetExtension(obj.Blob.Name);
                    path = obj.Blob.Name;
                }
                else
                {
                    createdOn = DateTime.UtcNow;
                    createdOnUtc = DateTime.UtcNow;
                    modifiedOn = DateTime.UtcNow;
                    modifiedOnUtc = DateTime.UtcNow;
                    contentLength = 0;
                    extension = "";

                    // ReSharper disable once PossibleNullReferenceException
                    path = obj.Prefix;
                }
            }
            else
            {
                throw new Exception("Type not supported.");
            }

            if (isDirectory) path = path?.Replace("/folder.stubxx", "");
            var fileName = Path.GetFileNameWithoutExtension(path);
            path = GetRelativePath(path);

            // CONVERT TO PREFERRED TIME ZONE
            // TODO: Make a site-wide preferred timezone setting.
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

            return new FileManagerEntry
            {
                Created = TimeZoneInfo.ConvertTimeFromUtc(createdOn, timeZone),
                CreatedUtc = createdOnUtc,
                Extension = string.IsNullOrEmpty(extension) ? string.Empty : extension.ToLower(),
                HasDirectories = false,
                IsDirectory = isDirectory,
                Modified = TimeZoneInfo.ConvertTimeFromUtc(modifiedOn, timeZone),
                ModifiedUtc = modifiedOnUtc,
                Name = fileName,
                Path = path,
                Size = contentLength
            };
        }

        #endregion

        #region UTILITY FUNCTIONS

        /// <summary>
        ///     Conversion of a file manager relative path to a full path used by the blob client..
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="pathParts">Relative path parts</param>
        /// <returns></returns>
        public string GetAbsolutePath(params string[] pathParts)
        {
            var root = TrimPathPart("pub").ToLower();

            var paths = new List<string>();

            if (!string.IsNullOrEmpty(root)) paths.Add(root);

            paths.AddRange(ParsePath(pathParts));

            return string.Join('/', paths);
        }
        
        /// <summary>
        ///     Converts the full path from a blob, to a relative one useful for the file manager.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public  string GetRelativePath(params string[] fullPath)
        {
            var rootPath = GetAbsolutePath("");

            var absolutePath = string.Join('/', ParsePath(fullPath));

            if (absolutePath.ToLower().StartsWith(rootPath.ToLower()))
            {
                if (rootPath.Length == absolutePath.Length) return "";
                return TrimPathPart(absolutePath.Substring(rootPath.Length));
            }

            return TrimPathPart(absolutePath);
        }


        /// <summary>
        ///     Gets the public URL of the blob.
        /// </summary>
        /// <param name="teamId"></param>
        /// <returns></returns>
        public  string GetBlobRootUrl()
        {
            return $"{_blobConfig.Value.BlobServicePublicUrl}{GetAbsolutePath("")}/";
        }

        /// <summary>
        ///     Parses out a path into a string array.
        /// </summary>
        /// <param name="pathParts"></param>
        /// <returns></returns>
        public  string[] ParsePath(params string[] pathParts)
        {
            if (pathParts == null) return new string[]{};

            var paths = new List<string>();

            foreach (var part in pathParts)
                if (!string.IsNullOrEmpty(part))
                {
                    var split = part.Split("/");
                    foreach (var p in split)
                        if (!string.IsNullOrEmpty(p))
                        {
                            var path = TrimPathPart(p);
                            if (!string.IsNullOrEmpty(path)) paths.Add(path);
                        }
                }

            return paths.ToArray();
        }

        /// <summary>
        ///     Trims leading and trailing slashes and white space from a path part.
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public  string TrimPathPart(string part)
        {
            if (string.IsNullOrEmpty(part))
                return "";

            return part.Trim('/').Trim('\\').Trim();
        }

        #endregion


        #region EDIT (CODE | IMAGE) FUNCTIONS

        public async Task<IActionResult> EditCode(string path)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    var extension = Path.GetExtension(path.ToLower());

                    var filter = _options.Value.AllowedFileTypes.Split(',');
                    var editorField = new EditorField
                    {
                        FieldId = "Content",
                        FieldName = Path.GetFileName(path)
                    };

                    if (!filter.Contains(extension)) return new UnsupportedMediaTypeResult();

                    switch (extension)
                    {
                        case ".js":
                            editorField.EditorMode = EditorMode.JavaScript;
                            editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                            break;
                        case ".css":
                            editorField.EditorMode = EditorMode.Css;
                            editorField.IconUrl = "/images/seti-ui/icons/css.svg";
                            break;
                        default:
                            editorField.EditorMode = EditorMode.Html;
                            editorField.IconUrl = "/images/seti-ui/icons/html.svg";
                            break;
                    }

                    //
                    // Get the blob now, so we can determine the type, or use this client as-is
                    //
                    var blob = _blobService.GetBlobClient(path);
                    var properties = blob.GetProperties();
                    await using var memoryStream = new MemoryStream();
                    //
                    // Determine the blob type
                    //
                    switch (properties.Value.BlobType)
                    {
                        case BlobType.Append:
                            var appendBlobClient = _blobService.GetAppendBlobClient(path);
                            await appendBlobClient.DownloadToAsync(memoryStream);
                            break;
                        case BlobType.Block:
                            await blob.DownloadToAsync(memoryStream);
                            break;
                    }

                    return View(new FileManagerEditCodeViewModel
                    {
                        Id = 1,
                        Path = path,
                        EditorTitle = Path.GetFileName(blob.Name),
                        EditorFields = new List<EditorField>
                        {
                            editorField
                        },
                        Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
                        EditingField = "Content",
                        CustomButtons = new List<string>()
                    });
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            return NotFound();
        }

        public async Task<IActionResult> EditImage(string path)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    var extension = Path.GetExtension(path.ToLower());

                    var filter = new[] {".png", ".jpg", ".gif", ".jpeg"};
                    if (filter.Contains(extension))
                    {
                        EditorMode mode;
                        switch (extension)
                        {
                            case ".js":
                                mode = EditorMode.JavaScript;
                                break;
                            case ".css":
                                mode = EditorMode.Css;
                                break;
                            default:
                                mode = EditorMode.Html;
                                break;
                        }

                        var blob = _blobService.GetBlobClient(path);
                        //var model = new FileManagerEditCodeViewModel();
                        await using var memoryStream = new MemoryStream();
                        await blob.DownloadToAsync(memoryStream);

                        return View(new FileManagerEditCodeViewModel
                        {
                            Id = 1,
                            Path = path,
                            EditorTitle = Path.GetFileName(blob.Name),
                            EditorFields = new List<EditorField>
                            {
                                new EditorField
                                {
                                    FieldId = "Content",
                                    FieldName = "Html Content",
                                    EditorMode = mode
                                }
                            },
                            Content = Encoding.UTF8.GetString(memoryStream.ToArray()),
                            EditingField = "Content",
                            CustomButtons = new List<string>()
                        });
                    }

                    return new UnsupportedMediaTypeResult();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> EditCode(FileManagerEditCodeViewModel model)
        {
            if (_options.Value.ReadWriteMode)
                try
                {
                    var extension = Path.GetExtension(model.Path);

                    var editorField = new EditorField
                    {
                        FieldId = "Content",
                        FieldName = Path.GetFileName(model.Path?.ToLower())
                    };

                    switch (extension)
                    {
                        case ".js":
                            model.EditorMode = EditorMode.JavaScript;
                            editorField.EditorMode = EditorMode.JavaScript;
                            editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                            break;
                        case ".css":
                            editorField.EditorMode = EditorMode.Css;
                            editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                            model.EditorMode = EditorMode.Css;
                            break;
                        default:
                            editorField.EditorMode = EditorMode.Html;
                            editorField.IconUrl = "/images/seti-ui/icons/javascript.svg";
                            model.EditorMode = EditorMode.Html;
                            break;
                    }

                    //
                    // Validate HTML here
                    if (model.EditorMode == EditorMode.Html)
                        // ReSharper disable once PossibleNullReferenceException
                        model.Content = ValidateHtml(model.Path.ToLower(), model.Content, ModelState);

                    if (ModelState.IsValid)
                    {
                        var blob = _blobService.GetBlobClient(model.Path);
                        //
                        // Get the blob now, so we can determine the type, or use this client as-is
                        //
                        var properties = await blob.GetPropertiesAsync();
                        await using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(model.Content));
                        memoryStream.Position = 0;

                        //
                        // Determine the blob type
                        //
                        switch (properties.Value.BlobType)
                        {
                            case BlobType.Append:
                                var appendBlobClient = _blobService.GetAppendBlobClient(model.Path);
                                await appendBlobClient.AppendBlockAsync(memoryStream);
                                break;
                            case BlobType.Block:
                                await blob.DeleteIfExistsAsync();
                                await blob.UploadAsync(memoryStream);
                                break;
                        }

                        model.Content = Encoding.UTF8.GetString(memoryStream.ToArray());
                    }


                    model.EditorFields = new List<EditorField> {editorField};
                    model.CustomButtons = new List<string> {"Close"};
                    model.IsValid = ModelState.IsValid;
                    return View(model);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                    throw;
                }

            return NotFound();
        }

        #endregion

        #region UPLOADER FUNCTIONS

        public ActionResult Remove(string[] fileNames, string path, int? teamId = null)
        {
            // For now, comment the following out because we are going to use the
            // file manager to remove files.

            //var fullPath = GetAbsolutePath(teamId, path).ToLower();

            //foreach (var fileName in fileNames)
            //{
            //    var filePath = $"/{TrimPathPart(fullPath)}/{fileName.ToLower()}";
            //    await DeleteFile(filePath);
            //}

            // Return an empty string to signify success
            return Content("");
        }

        /// <summary>
        ///     Used to upload files, one chunk at a time, and normalizes the blob name to lower case.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="metaData"></param>
        /// <param name="path"></param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> Upload(IEnumerable<IFormFile> files, string metaData, string path,
            int? teamId = null)
        {
            try
            {
                //
                // NOTE: If there is no "metaData," it means the file(s) are NOT being "chunked."
                // The files are being uploaded in whole
                //
                var fullPath = GetAbsolutePath(path);

                if (metaData == null) return await SaveAsync(files, fullPath);
                return await AppendBlobAsync(files, metaData, fullPath);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        /// <summary>
        ///     Appends chunks to a blob, whose name is normalized to lower case.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="metaData"></param>
        /// <param name="blobRelativePath"></param>
        /// <returns></returns>
        private async Task<JsonResult> AppendBlobAsync(IEnumerable<IFormFile> files, string metaData,
            string blobRelativePath)
        {
            try
            {
                if (string.IsNullOrEmpty(metaData)) throw new Exception("metaData cannot be null or empty.");
                //
                // NOTE: If we made it to here, it means we are receiving data in chunks
                //

                //
                // Get information about the chunk we are on.
                //
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(metaData));

                var serializer = new JsonSerializer();
                FileUploadMetaData fileMetaData;
                using (var streamReader = new StreamReader(ms))
                {
                    fileMetaData =
                        (FileUploadMetaData) serializer.Deserialize(streamReader, typeof(FileUploadMetaData));
                }

                //
                // On first chunk, check for existing blob.  If it exists we need to get rid of 
                // all traces of it in the blob storage and in the database. Also, if this is the
                // first time we are writing to this container, let's make sure it exists.
                //
                var file = files.FirstOrDefault();

                // ReSharper disable once PossibleNullReferenceException
                var pathParts = ParsePath(blobRelativePath, fileMetaData.FileName);

                var blobName = UrlEncode(TrimPathPart(string.Join('/', pathParts)).ToLower());

                var blobClient = _blobService.GetAppendBlobClient(blobName);

                if (fileMetaData.ChunkIndex == 0)
                    try
                    {
                        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
                        if (!await blobClient.ExistsAsync()) await blobClient.CreateIfNotExistsAsync();

                        BlobProperties properties = await blobClient.GetPropertiesAsync();

                        BlobHttpHeaders headers = new BlobHttpHeaders
                        {
                            // Set the MIME ContentType every time the properties 
                            // are updated or the field will be cleared
                            ContentType = GetContentType(fileMetaData),
                            ContentLanguage = "en-us",
                            // Populate remaining headers with 
                            // the pre-existing properties
                            CacheControl = properties.CacheControl,
                            ContentDisposition = properties.ContentDisposition,
                            ContentEncoding = properties.ContentEncoding,
                            ContentHash = properties.ContentHash
                        };

                        // Set the blob's properties.
                        await blobClient.SetHttpHeadersAsync(headers);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                //
                // The following taken from here: https://stackoverflow.com/questions/60304474/c-sharp-azure-appendblob-appendblock-adding-a-file-larger-than-the-4mb-limit
                //
                var appendBlobMaxAppendBlockBytes = blobClient.AppendBlobMaxAppendBlockBytes;

                // ReSharper disable once PossibleNullReferenceException
                using (var f = file.OpenReadStream())
                {
                    int bytesRead;
                    var buffer = new byte[appendBlobMaxAppendBlockBytes];
                    while ((bytesRead = f.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        //Stream stream = new MemoryStream(buffer);
                        var newArray = new Span<byte>(buffer, 0, bytesRead).ToArray();
                        await using Stream stream = new MemoryStream(newArray);
                        stream.Position = 0;

                        await blobClient.AppendBlockAsync(stream);
                    }
                }

                var fileBlob = new FileUploadResult
                {
                    uploaded = fileMetaData.TotalChunks - 1 <= fileMetaData.ChunkIndex,
                    fileUid = fileMetaData.UploadUid
                };

                return Json(fileBlob);
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        private string GetContentType(FileUploadMetaData fileMetaData)
        {
            if (!string.IsNullOrEmpty(fileMetaData.ContentType.Trim()) || string.IsNullOrEmpty(fileMetaData.FileName.Trim()))
            {
                return fileMetaData.ContentType;
            }

            var extension = Path.GetExtension(fileMetaData.FileName)?.ToLower();

            if (string.IsNullOrEmpty(extension))
            {
                return "application/octet-stream";
            }
            //svg as "image/svg+xml"(W3C: August 2011)
            switch (extension)
            {
                case ".ttf":
                    return "application/x-font-ttf"; // (IANA: March 2013)
                case ".woff":
                    return "application/font-woff"; // (IANA: January 2013)
                case ".woff2":
                    return "application/font-woff2"; // (W3C W./ E.Draft: May 2014 / March 2016)
                case ".or":
                    return "application/x-font-truetype";
                case ".otf":
                    return "application/x-font-opentype"; // (IANA: March 2013)
                case ".eot":
                    return "application/vnd.ms-fontobject"; // (IANA: December 2005)
                case ".sfnt":
                    return "application/font-sfnt"; // (IANA: March 2013)
                default:
                    return "application/octet-stream";
            }
        }

        /// <summary>
        ///     Uploads a file to a blob in a single transaction.  Blob name is normalized to lower case.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        private async Task<ActionResult> SaveAsync(IEnumerable<IFormFile> files, string relativePath)
        {
            try
            {
                // The Name of the Upload component is "files"
                if (files != null)
                    foreach (var file in files)
                    {
                        var blobName = UrlEncode(relativePath + file.FileName).ToLower();

                        var blobClient = _blobService.GetBlobClient(blobName);

                        await blobClient.DeleteIfExistsAsync();

                        await blobClient.UploadAsync(file.OpenReadStream());
                    }

                // Return an empty string to signify success
                return Content("");
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message, e);
                throw;
            }
        }

        #endregion
    }
}