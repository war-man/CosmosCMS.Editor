using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CDT.Cosmos.Cms.Common.Services;
using CDT.Cosmos.Cms.Models;
using CDT.Cosmos.Cms.Services;
using Kendo.Mvc.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Common.Tests
{
    [TestClass]
    public class A02AzureBlobServiceTests
    {
        private const string FileRoot = "files";
        //private const string ImageRoot = "images";

        private static readonly List<FolderFilePair> FolderPaths = new List<FolderFilePair>
        {
            new FolderFilePair
            {
                FolderName = "folder1",
                Folder = $"{FileRoot}/folder1",
                FileName = "folder.stubxx"
            },
            new FolderFilePair
            {
                FolderName = "folder2",
                Folder = $"{FileRoot}/folder2",
                FileName = "folder.stubxx"
            },
            new FolderFilePair
            {
                FolderName = "subfolderA",
                Folder = $"{FileRoot}/folder2/subfolderA",
                FileName = "folder.stubxx"
            },
            new FolderFilePair
            {
                FolderName = "folder3",
                Folder = $"{FileRoot}/folder3",
                FileName = "folder.stubxx"
            }
        };

        private static IOptions<AzureBlobServiceConfig> GetBlobConfig()
        {
            return Options.Create(new AzureBlobServiceConfig
            {
                ConnectionString =
                    "DefaultEndpointsProtocol=https;AccountName=cosmoscmsunittests;AccountKey=8l4SksCfz3vfOvPdu02VrYO0nfBDIzSKW4qGZ/Q5695kZSB4GO9LCsZakJnhnb0yr3k1Ab9EEJo6L5/W2TMn5g==;EndpointSuffix=core.windows.net",
                BlobServicePublicUrl = "https://cosmoscmsunittests.z22.web.core.windows.net/"
            });
        }

        private static BlobServiceClient GetBlobServiceClient()
        {
            return new BlobServiceClient(GetBlobConfig().Value.ConnectionString);
        }

        private static BlobContainerClient GetBlobContainerClient()
        {
            var client = GetBlobServiceClient();
            return client.GetBlobContainerClient("$web");
        }


        private static AzureBlobService GetBlobService()
        {
            var logger = new Logger<AzureBlobService>(new NullLoggerFactory());
            var service = new AzureBlobService(GetBlobConfig(), logger);
            return service;
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var service = GetBlobService();
            var blobs = service.GetFiles("", null).Result;
            foreach (var blob in blobs)
            {
                var task = service.DeleteBlob(blob.Name);
                task.Wait();
            }
        }

        [TestMethod]
        public async Task A01_CreateFolders()
        {
            var service = GetBlobService();

            var result1 = await service.CreateFolder(FolderPaths[0].Folder);
            var result2 = await service.CreateFolder(FolderPaths[1].Folder);

            var result3 = await service.CreateFolder(FolderPaths[2].Folder);

            // Check it out.
            var client = GetBlobServiceClient();

            var containerClient = GetBlobContainerClient();
            var blobs = containerClient.GetBlobsAsync().AsPages();

            var blobList = new List<BlobItem>();

            await foreach (var page in blobs)
            foreach (var item in page.Values)
                blobList.Add(item);


            Assert.AreEqual(FileBrowserEntryType.Directory, result1.EntryType);
            Assert.AreEqual(FileBrowserEntryType.Directory, result2.EntryType);
            Assert.AreEqual(FileBrowserEntryType.Directory, result3.EntryType);
            Assert.AreEqual(FolderPaths[0].FolderName, result1.Name);
            Assert.AreEqual(FolderPaths[1].FolderName, result2.Name);
            Assert.AreEqual(FolderPaths[2].FolderName, result3.Name);

            Assert.AreEqual(3, blobList.Count);

            var fullPaths = FolderPaths.Select(s => $"{s.Folder}/{s.FileName}").ToList();
            var blobPaths = blobList.Select(s => $"{s.Name}").ToList();

            foreach (var blobPath in blobPaths) Assert.IsTrue(fullPaths.Contains(blobPath));
        }

        [TestMethod]
        public async Task A02_UploadFiles()
        {
            var blobService = GetBlobService();
            //var client = GetBlobServiceClient();
            var testFile0 = StaticUtilities.GetFormFile("helloworld0.txt");
            var testFile1 = StaticUtilities.GetFormFile("helloworld1.txt");
            var testFile2 = StaticUtilities.GetFormFile("helloworld2.txt");

            var fileBrowserEntry0 = await blobService.UploadFile<FileBrowserEntry>(FolderPaths[0].Folder, testFile0);
            var fileBrowserEntry1 = await blobService.UploadFile<BlobClient>(FolderPaths[1].Folder, testFile1);
            var fileBrowserEntry2 = await blobService.UploadFile<FileBrowserEntry>(FolderPaths[2].Folder, testFile2);

            // Interrogate file 0 results;
            Assert.AreEqual(FileBrowserEntryType.File, fileBrowserEntry0.EntryType);
            Assert.AreEqual("helloworld0.txt", fileBrowserEntry0.Name);
            Assert.AreEqual(testFile0.Length, fileBrowserEntry0.Size);

            // Interrogate file 1 results;
            Assert.AreEqual($"{FolderPaths[1].Folder}/helloworld1.txt", fileBrowserEntry1.Name);
            Assert.AreEqual(testFile1.Length, fileBrowserEntry1.GetProperties().Value.ContentLength);

            // Interrogate file 2 results;
            Assert.AreEqual(FileBrowserEntryType.File, fileBrowserEntry2.EntryType);
            Assert.AreEqual("helloworld2.txt", fileBrowserEntry2.Name);
            Assert.AreEqual(testFile2.Length, fileBrowserEntry2.Size);
        }

        [TestMethod]
        public async Task A03_ReadFileEntries()
        {
            var blobService = GetBlobService();
            var result1 = await blobService.Read("files/folder1", FileBrowserEntryType.Directory,
                AllowedFileExtensions.GetFilterForBlobs(AllowedFileExtensions.ExtensionCollectionType.FileUploads));

            Assert.AreEqual(1, result1.Count);
            var result2 = await blobService.Read(FolderPaths[0].Folder, FileBrowserEntryType.Directory,
                AllowedFileExtensions.GetFilterForBlobs(AllowedFileExtensions.ExtensionCollectionType.FileUploads));
            Assert.AreEqual(1, result2.Count);
            var result3 = await blobService.Read(FolderPaths[1].Folder + "/", FileBrowserEntryType.Directory,
                AllowedFileExtensions.GetFilterForBlobs(AllowedFileExtensions.ExtensionCollectionType.FileUploads));
            Assert.AreEqual(2, result3.Count);
            var result4 = await blobService.Read(FolderPaths[2].Folder, FileBrowserEntryType.Directory,
                AllowedFileExtensions.GetFilterForBlobs(AllowedFileExtensions.ExtensionCollectionType.FileUploads));
            Assert.AreEqual(1, result4.Count);
            var result5 = await blobService.Read("files/folder1", FileBrowserEntryType.Directory,
                AllowedFileExtensions.GetFilterForBlobs(AllowedFileExtensions.ExtensionCollectionType.ImageUploads));
            Assert.AreEqual(0, result5.Count);
        }

        [TestMethod]
        public async Task A04_CopyBlobs()
        {
            var blobService = GetBlobService();
            var copyResult = await blobService.CopyBlob(FolderPaths[1].Folder + "/helloworld1.txt",
                FolderPaths[3].Folder + "/copied-helloworld1.txt");

            var result1 = await blobService.Read(FolderPaths[1].Folder, FileBrowserEntryType.Directory,
                AllowedFileExtensions.GetFilterForBlobs(AllowedFileExtensions.ExtensionCollectionType.FileUploads));
            var result3 = await blobService.Read(FolderPaths[3].Folder, FileBrowserEntryType.Directory,
                AllowedFileExtensions.GetFilterForBlobs(AllowedFileExtensions.ExtensionCollectionType.FileUploads));

            Assert.IsTrue(await copyResult.ExistsAsync());
            Assert.AreEqual(1, result3.Count);
        }

        [TestMethod]
        public async Task A05_DeleteFoldersAndBlobs()
        {
            var blobService = GetBlobService();
            var file1 = new FileBrowserEntry
            {
                EntryType = FileBrowserEntryType.File,
                Name = "helloworld1.txt",
                Size = 0
            };
            var result1 = await blobService.Destroy(FolderPaths[1].Folder, file1);
        }
    }

    public class FolderFilePair
    {
        public string Folder { get; set; }
        public string FileName { get; set; }
        public string FolderName { get; set; }
    }
}