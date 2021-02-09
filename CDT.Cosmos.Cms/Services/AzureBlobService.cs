using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Controllers;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CDT.Cosmos.Cms.Services
{
    public class AzureBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<AzureBlobService> _logger;

        public AzureBlobService(IOptions<AzureBlobServiceConfig> options, ILogger<AzureBlobService> logger)
        {
            _logger = logger;
            _blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        }

        /// <summary>
        ///     Copies a single blob and returns it's <see cref="BlobClient" />.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns>The destination or new <see cref="BlobClient" />.</returns>
        /// <remarks>
        ///     Tip: After operation, check the returned blob object to see if it exists.
        /// </remarks>
        public async Task<BlobClient> CopyBlob(string source, string destination)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("$web");
            var sourceBlob = containerClient.GetBlobClient(source);
            if (await sourceBlob.ExistsAsync())
            {
                var lease = sourceBlob.GetBlobLeaseClient();
                await lease.AcquireAsync(TimeSpan.FromSeconds(-1));

                // Get a BlobClient representing the destination blob with a unique name.
                var destBlob = containerClient.GetBlobClient(destination);

                try
                {
                    // Start the copy operation.
                    var c = await destBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                    await c.WaitForCompletionAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message, e);
                }

                await lease.ReleaseAsync();

                return destBlob;
            }

            return null;
        }

        public async Task<List<BlobItem>> GetFiles(string folderName, string[] filters)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("$web");
            await containerClient.CreateIfNotExistsAsync();
            var blobList = new List<BlobItem>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                if (!blobItem.Name.StartsWith(folderName)) continue;
                var extension = Path.GetExtension(blobItem.Name);
                if (filters != null && !string.IsNullOrEmpty(extension) && filters.Contains(extension.ToLower()))
                    blobList.Add(blobItem);
                else
                    blobList.Add(blobItem);
            }

            return blobList;
        }

        /// <summary>
        ///     Gets a client for a blob.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public BlobClient GetBlobClient(string target)
        {
            target = target?.TrimStart('/');
            var containerClient = _blobServiceClient.GetBlobContainerClient("$web");
            var task = containerClient.CreateIfNotExistsAsync();
            task.Wait();
            return containerClient.GetBlobClient(target);
        }

        /// <summary>
        ///     Encodes the file name for blob storage, and prepends the name with the folder path.
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetBlobName(string folderName, string fileName)
        {
            return $"{folderName.TrimEnd('/')}/{fileName}";
        }

        /// <summary>
        ///     Gets an append blob client, used for chunk uploads.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public AppendBlobClient GetAppendBlobClient(string target)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("$web");
            var task = containerClient.CreateIfNotExistsAsync();
            task.Wait();
            return containerClient.GetAppendBlobClient(target);
        }

        /// <summary>
        ///     Deletes a file (blob).
        /// </summary>
        /// <param name="folderName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<bool> DeleteBlob(string folderName, string fileName)
        {
            return await DeleteBlob(GetBlobName(folderName, fileName));
        }

        /// <summary>
        ///     Deletes a file (blob).
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<bool> DeleteBlob(string target)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("$web");
            return await containerClient.DeleteBlobIfExistsAsync(target);
        }

        /// <summary>
        ///     Gets a list of blobs or prefixes (virtual folders) at a given path.
        /// </summary>
        /// <param name="path">Search Path</param>
        /// <param name="segmentSize"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>IMPORTANT:  If root, use empty string.</para>
        ///     <list type="bullet">
        ///         <item>
        ///             Do not lead with '/' in the prefix (path).
        ///         </item>
        ///         <item>
        ///             Use format: file/folder1/ (note slash at end).
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <example>
        ///     <code>Search("files/", null)</code>
        /// </example>
        public async Task<List<BlobHierarchyItem>> Search(string path, int? segmentSize)
        {
            // See for helpful info: https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list?tabs=dotnet
            string continuationToken = null;
            var container = _blobServiceClient.GetBlobContainerClient("$web");
            try
            {
                var hierachicalItems = new List<BlobHierarchyItem>();

                // Call the listing operation and enumerate the result segment.
                // When the continuation token is empty, the last segment has been returned and
                // execution can exit the loop.
                do
                {
                    //path = path.Trim('/');
                    if (!path.EndsWith("/")) path = $"{path}/";

                    // See: https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list?tabs=dotnet
                    var resultSegment = container.GetBlobsByHierarchyAsync(prefix: path, delimiter: "/")
                        .AsPages(continuationToken, segmentSize);

                    await foreach (var blobPage in resultSegment)
                    {
                        // A hierarchical listing may return both virtual directories and blobs.
                        hierachicalItems.AddRange(blobPage.Values);

                        // Get the continuation token and loop until it is empty.
                        continuationToken = blobPage.ContinuationToken;
                    }
                } while (continuationToken != "");

                return hierachicalItems;
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        #region FILE AND IMAGE BROWSER COMMON METHODS

        /// <summary>
        ///     Creates a folder if it does not yet exists.
        /// </summary>
        /// <param name="path">Full path to folder to create</param>
        /// <returns></returns>
        /// <exception cref="Exception">Folder creation failure.</exception>
        public async Task<FileBrowserEntry> CreateFolder(string path)
        {
            //
            // Blob storage does not have a folder object, just blobs with paths.
            // Therefore, to create an illusion of a folder, we have to create a blob
            // that will be in the folder.  For example:
            // 
            // To create folder /pictures 
            //
            // You have to pub a blob here /pictures/folder.subxx
            //
            // To remove a folder, you simply remove all blobs below /pictures
            //
            await using var stream =
                new MemoryStream(Encoding.ASCII.GetBytes($"This is a folder stub file for {path}."));
            var stub = new FormFile(stream, 0, stream.Length, "folder.stubxx", "folder.stubxx");

            // Make sure the path doesn't already exist.
            var files = await GetFiles(path, null);

            // Create or return existing here.
            FileBrowserEntry browserEntry;
            if (files != null && files.Count > 0)
                browserEntry = new FileBrowserEntry
                {
                    EntryType = FileBrowserEntryType.Directory,
                    Name = path,
                    Size = 0
                };
            else
                browserEntry = await UploadFile<FileBrowserEntry>(path, stub);

            return browserEntry;
        }

        /// <summary>
        ///     Finds a hierarchical of blobs by "Name" using a "starts with" filter.
        /// </summary>
        /// <param name="startsWith"></param>
        /// <param name="segmentSize">Default is 5000</param>
        /// <param name="container">Optional</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>Starts with parameter uses absolute pathing, not relative to the website.</para>
        ///     <para>
        ///         This function is taken from
        ///         <a href="https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blobs-list?tabs=dotnet">this example</a>.
        ///     </para>
        /// </remarks>
        public async Task<List<BlobItem>> ListBlobsHierarchicalListing(string startsWith, int? segmentSize,
            BlobContainerClient container = null)
        {
            var blobItemList = new List<BlobItem>();

            // If the container is given, we are likely reusing one in a search.
            // Otherwise if null, get the default container.
            container ??= _blobServiceClient.GetBlobContainerClient("$web");

            // Call the listing operation and return pages of the specified size.
            var resultSegment = container.GetBlobsByHierarchyAsync(prefix: startsWith, delimiter: "/")
                .AsPages(default, segmentSize);

            // Enumerate the blobs returned for each page.
            await foreach (var blobPage in resultSegment)
                // A hierarchical listing may return both virtual directories and blobs.
            foreach (var blobHierarchyItem in blobPage.Values)
                if (blobHierarchyItem.IsPrefix)
                {
                    // Write out the prefix of the virtual directory.
                    Console.WriteLine("Virtual directory prefix: {0}", blobHierarchyItem.Prefix);

                    // Call recursively with the prefix to traverse the virtual directory.
                    blobItemList.AddRange(
                        await ListBlobsHierarchicalListing(blobHierarchyItem.Prefix, null, container));
                }
                else
                {
                    // Write out the name of the blob.
                    blobItemList.Add(blobHierarchyItem.Blob);
                }

            // Return the blob list here.
            return blobItemList;
        }


        /// <summary>
        ///     Reads the files found in the given path for <see cref="Controllers.FileBrowserController.Read" /> or
        ///     <see cref="Controllers.ImageBrowserController.Read" />.
        /// </summary>
        /// <param name="path">Full path to folder to read</param>
        /// <param name="entryType"></param>
        /// <param name="extensionFilter"></param>
        /// <returns></returns>
        /// <example>
        ///     <code>
        /// AzureBlobService("/pictures", <see cref="FileBrowserEntryType.Directory" />, new [] { ".txt", ".doc", ".pdf" });
        /// AzureBlobService("/pictures/MyPicture.png", <see cref="FileBrowserEntryType.File" />, new [] { ".txt", ".doc", ".pdf" });
        /// </code>
        /// </example>
        public async Task<List<FileBrowserEntry>> Read(string path, FileBrowserEntryType entryType,
            List<string> extensionFilter)
        {
            if (entryType == FileBrowserEntryType.Directory && !path.EndsWith("/")) path = path + "/";

            var blobs = await Search(path, null);
            var files = blobs.Where(w =>
                    w.Blob == null || !w.Blob.Name.EndsWith("folder.stubxx"))
                .Select(s => new FileBrowserEntry
                {
                    EntryType = s.IsPrefix ? FileBrowserEntryType.Directory : FileBrowserEntryType.File,
                    Name = s.IsPrefix
                        ? s.Prefix.TrimEnd('/').Split("/").LastOrDefault()
                        : s.Blob.Name.Split("/")
                            .LastOrDefault(),
                    Size = s.IsPrefix ? 0 : s.Blob.Properties.ContentLength ?? 0
                }).ToList();

            if (extensionFilter == null || !extensionFilter.Any()) return files.ToList();
            //var blobs = _blobService.GetFiles("images", Filter.Replace("*", "").Split(",")).Result;

            // EXAMPLE Json
            // [{"Name":"1.png","Size":5998,"EntryType":0},{"Name":"2.png","Size":5405,"EntryType":0},{"Name":"3.png","Size":7428,"EntryType":0},{"Name":"4.png","Size":6280,"EntryType":0},{"Name":"5.png","Size":6878,"EntryType":0},{"Name":"6.png","Size":6176,"EntryType":0},{"Name":"7.png","Size":6375,"EntryType":0},{"Name":"8.png","Size":6967,"EntryType":0},{"Name":"9.png","Size":7075,"EntryType":0}]
            var model = files.Where(a =>
                a.EntryType == FileBrowserEntryType.Directory ||
                extensionFilter.Contains(Path.GetExtension(a.Name.ToLower()))).ToList();

            return model;
        }

        /// <summary>
        ///     Uploads file and returns either type <see cref="FileBrowserEntry" /> or <see cref="BlobClient" />.
        /// </summary>
        /// <typeparam name="T"><see cref="FileBrowserEntry" /> | <see cref="BlobClient" /></typeparam>
        /// <param name="folderName"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<T> UploadFile<T>(string folderName, IFormFile file)
        {
            var blobClient = GetBlobClient(GetBlobName(folderName, file.FileName));
            await blobClient.DeleteIfExistsAsync();
            await using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, true);
            if (!await blobClient.ExistsAsync()) throw new Exception($"Could not upload file. ({file.Name})");
            if (typeof(T) == typeof(FileBrowserEntry))
            {
                var name = blobClient.Name.Split("/").LastOrDefault();

                if (name == "folder.stubxx")
                    return (T) (object) new FileBrowserEntry
                    {
                        EntryType = FileBrowserEntryType.Directory,
                        Name = folderName.Split("/").LastOrDefault(),
                        Size = 0
                    };

                return (T) (object) new FileBrowserEntry
                {
                    EntryType = FileBrowserEntryType.File,
                    Name = name,
                    Size = file.Length
                };
            }

            if (typeof(T) == typeof(BlobClient)) return (T) (object) blobClient;

            throw new Exception($"Unsupported type: {typeof(T)}");
        }

        /// <summary>
        ///     Destroys a directory or a file for <see cref="Controllers.FileBrowserController.Destroy" /> and
        ///     <see cref="Controllers.ImageBrowserController.Destroy" />.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="entry"></param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>Note:</para>
        ///     <list type="bullet">
        ///         <item>
        ///             When destroying a directory, path parameter must be the full path to the directory.
        ///             In this case, <see cref="FileBrowserEntry.Name" /> is ignored.
        ///         </item>
        ///         <item>
        ///             When destroying a file, the path parameter leads to the file, then <see cref="FileBrowserEntry.Name" /> is
        ///             used.
        ///         </item>
        ///     </list>
        ///     Internally uses <see cref="DeleteBlob(string,string)" /> to do its work.
        /// </remarks>
        public async Task<bool> Destroy(string path, FileBrowserEntry entry)
        {
            if (entry.EntryType == FileBrowserEntryType.Directory)
            {
                //
                // Check to see if folder is empty
                //
                var results = new Dictionary<string, bool>();

                var blobs = await GetFiles(path, null);
                foreach (var blobItem in blobs)
                {
                    var result = await DeleteBlob(blobItem.Name);
                    results.Add(blobItem.Name, result);
                }

                // Can't delete, the folder still has blobs in it.
                if (results.Any(r => r.Value == false))
                {
                    var unsuccessful = string.Join(",", results.Where(a => a.Value == false).Select(s => s.Key));
                    throw new Exception($"Could not delete: {unsuccessful.TrimEnd(',')}");
                }

                return true;
            }

            return await DeleteBlob(path, entry.Name);
        }

        #endregion
    }
}