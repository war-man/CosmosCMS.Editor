using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CDT.Cosmos.Cms.Common.Data;
using CDT.Cosmos.Cms.Models;
using Kendo.Mvc.UI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CDT.Cosmos.Cms.Tests
{
    [TestClass]
    public class A07FileManagerTests
    {
        private static ApplicationDbContext _dbContext;
        private static int _teamId;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            _dbContext = StaticUtilities.GetApplicationDbContext();

            var blobService = StaticUtilities.GetAzureBlobService();

            var blobs = blobService.GetFiles("", null).Result;
            foreach (var blobItem in blobs)
                if (!blobItem.Deleted)
                {
                    var task = blobService.DeleteBlob(blobItem.Name);
                    task.Wait();
                }

            _teamId = _dbContext.Teams.Select(s => s.Id).FirstOrDefaultAsync().Result;
        }

        [ClassCleanup]
        public static void Cleanup()
        {
            _dbContext.Dispose();
        }

        [TestMethod]
        public async Task A00_01_Root_TestPathConversion()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));

            var absolutePath = controller.GetAbsolutePath(null,"");
            Assert.AreEqual("pub", absolutePath);

            var relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "/");
            Assert.AreEqual("pub", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "\\");
            Assert.AreEqual("pub", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("", relativePath);
        }

        [TestMethod]
        public async Task A00_02_Folder_TestPathConversion()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));

            var absolutePath = controller.GetAbsolutePath(null, "New Folder 1");
            Assert.AreEqual("pub/New Folder 1", absolutePath);

            var relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "/New Folder 1");
            Assert.AreEqual("pub/New Folder 1", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "/New Folder 1/");
            Assert.AreEqual("pub/New Folder 1", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "//New Folder 1");
            Assert.AreEqual("pub/New Folder 1", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);
        }

        [TestMethod]
        public async Task A00_03_TeamFolder_TestPathConversion()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));

            var absolutePath = controller.GetAbsolutePath(_teamId, "New Folder 1");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1", absolutePath);

            var relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);


            absolutePath = controller.GetAbsolutePath(_teamId, "/New Folder 1");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1", absolutePath);

            relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);


            absolutePath = controller.GetAbsolutePath(_teamId, "/New Folder 1/");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1", absolutePath);

            relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);


            absolutePath = controller.GetAbsolutePath(_teamId, "//New Folder 1");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1", absolutePath);

            relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1", relativePath);
        }

        [TestMethod]
        public async Task A00_04_Folder_TestSubPathConversion()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));

            var absolutePath = controller.GetAbsolutePath(null, "New Folder 1/Sub Path 2");
            Assert.AreEqual("pub/New Folder 1/Sub Path 2", absolutePath);

            var relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);


            absolutePath = controller.GetAbsolutePath( null,"/New Folder 1/Sub Path 2");
            Assert.AreEqual("pub/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "/New Folder 1/Sub Path 2/");
            Assert.AreEqual("pub/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "//New Folder 1/Sub Path 2");
            Assert.AreEqual("pub/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(null, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);


            absolutePath = controller.GetAbsolutePath(null, "//New Folder 1//Sub Path 2");
            Assert.AreEqual("pub/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(null, "pub/New Folder 1//Sub Path 2");
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);
        }

        [TestMethod]
        public async Task A00_05_TeamFolder_TestSubPathConversion()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));

            var absolutePath = controller.GetAbsolutePath(_teamId, "New Folder 1/Sub Path 2");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1/Sub Path 2", absolutePath);

            var relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);


            absolutePath = controller.GetAbsolutePath(_teamId, "/New Folder 1/Sub Path 2");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);


            absolutePath = controller.GetAbsolutePath(_teamId, "/New Folder 1/Sub Path 2/");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);


            absolutePath = controller.GetAbsolutePath(_teamId, "//New Folder 1/Sub Path 2");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(_teamId, absolutePath);
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);

            absolutePath = controller.GetAbsolutePath(_teamId, "//New Folder 1//Sub Path 2");
            Assert.AreEqual($"pub/teams/{_teamId}/New Folder 1/Sub Path 2", absolutePath);

            relativePath = controller.GetRelativePath(_teamId, $"pub/teams/{_teamId}/New Folder 1//Sub Path 2");
            Assert.AreEqual("New Folder 1/Sub Path 2", relativePath);
        }

        [TestMethod]
        public async Task A01_ReadRootFolder()
        {
            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                // Make sure all the root folders exist.
                await controller.EnsureRootFoldersExist();

                var result = await controller.Read("");
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 1);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("/");
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 1);
            }
        }

        [TestMethod]
        public async Task A02_CreateFolders()
        {
            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                await controller.EnsureRootFoldersExist();
                var result = controller.Create("", new FileManagerEntry
                {
                    Name = "New Folder 1",
                    Path = "",
                    IsDirectory = true
                }, null);
                var jsonResult = (JsonResult) result;
                var model = (FileManagerEntry) jsonResult.Value;
                Assert.AreEqual("new-folder-1", model.Name);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = controller.Create("", new FileManagerEntry
                {
                    Name = "New Folder 2",
                    Path = "",
                    IsDirectory = true
                }, null);
                var jsonResult = (JsonResult) result;
                var model = (FileManagerEntry) jsonResult.Value;
                Assert.AreEqual("new-folder-2", model.Name);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = controller.Create("", new FileManagerEntry
                {
                    Name = "New Folder 2/New Sub Folder 1",
                    Path = "",
                    IsDirectory = true
                }, null);
                var jsonResult = (JsonResult) result;
                var model = (FileManagerEntry) jsonResult.Value;
                Assert.AreEqual("new-folder-2/new-sub-folder-1", model.Path);
                Assert.AreEqual("new-sub-folder-1", model.Name);
            }
        }

        [TestMethod]
        public async Task A03_ReadFolders()
        {
            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("/");
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 3);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("new-folder-2");
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 1);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("new-folder-2/");
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 1);
            }
        }

        [TestMethod]
        public async Task A04_UploadFiles()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var path = "New-Folder-2";

            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            foreach (var fileName in fileNames)
            {
                var guid = Guid.NewGuid();
                var file = new[] {StaticUtilities.GetFormFile(fileName)};
                var metadata =
                    $"{{\"chunkIndex\":0,\"contentType\":\"text/plain\",\"fileName\":\"{fileName}\",\"relativePath\":\"{fileName}\",\"totalFileSize\":{file.Length},\"totalChunks\":1,\"uploadUid\":\"{guid}\"}}";

                var result = (JsonResult) await controller.Upload(file, metadata, path);

                var model = (FileUploadResult) result.Value;

                Assert.IsTrue(model.uploaded);
                Assert.AreEqual(guid.ToString(), model.fileUid);

                var blobClient = blobService.GetBlobClient($"pub/new-folder-2/{fileName}");

                Assert.IsTrue(await blobClient.ExistsAsync());
            }
        }

        [TestMethod]
        public async Task A05_RenameFolderWithFiles()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var oldFolder = "New-Folder-2";
            var newFolder = "New-Folder-3a";
            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            var fileManagerEntry = new FileManagerEntry
            {
                Created = default,
                CreatedUtc = default,
                Extension = null,
                HasDirectories = false,
                IsDirectory = true,
                Modified = default,
                ModifiedUtc = default,
                Name = newFolder,
                Path = oldFolder,
                Size = 0
            };

            var result = (JsonResult) await controller.Update(fileManagerEntry, null);

            var model = (FileManagerEntry) result.Value;
            Assert.AreEqual(newFolder.ToLower(), model.Path);
            Assert.AreEqual(newFolder.ToLower(), model.Name);
            Assert.AreEqual("", model.Extension);
            Assert.AreEqual(true, model.IsDirectory);

            foreach (var fileName in fileNames)
            {
                var blobClient = blobService.GetBlobClient($"pub/{newFolder.ToLower()}/{fileName}");
                Assert.IsTrue(await blobClient.ExistsAsync());
            }
        }

        [TestMethod]
        public async Task A06_RenameFiles()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));

            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};

            var blobService = StaticUtilities.GetAzureBlobService();


            foreach (var fileName in fileNames)
            {
                var fileManagerEntry = new FileManagerEntry
                {
                    Created = default,
                    CreatedUtc = default,
                    Extension = null,
                    HasDirectories = false,
                    IsDirectory = false,
                    Modified = default,
                    ModifiedUtc = default,
                    Name = $"t-{fileName}",
                    Path = $"New-Folder-3a/{fileName}",
                    Size = 0
                };

                var result = (JsonResult) await controller.Update(fileManagerEntry, null);

                var model = (FileManagerEntry) result.Value;
                Assert.AreEqual($"new-folder-3a/t-{fileName.ToLower()}", model.Path);
                Assert.AreEqual(Path.GetFileNameWithoutExtension($"t-{fileName}"), model.Name);

                var blobClient = blobService.GetBlobClient($"pub/new-folder-3a/t-{fileName.ToLower()}");
                Assert.IsTrue(await blobClient.ExistsAsync());
            }
        }

        [TestMethod]
        public async Task B01_TeamFolder_ReadRootFolder()
        {
            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                // Make sure all the root folders exist.
                await controller.EnsureRootFoldersExist();

                var result = await controller.Read("", _teamId);
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.AreEqual(0, model?.Count());
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("/", _teamId);
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.AreEqual(0, model?.Count());
            }
        }

        [TestMethod]
        public async Task B02_TeamFolder_CreateFolders()
        {
            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                await controller.EnsureRootFoldersExist();
                var result = controller.Create("", new FileManagerEntry
                {
                    Name = "New Folder 1",
                    Path = "",
                    IsDirectory = true
                }, _teamId);
                var jsonResult = (JsonResult) result;
                var model = (FileManagerEntry) jsonResult.Value;
                Assert.AreEqual("new-folder-1", model.Name);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = controller.Create("", new FileManagerEntry
                {
                    Name = "New Folder 2",
                    Path = "",
                    IsDirectory = true
                }, _teamId);
                var jsonResult = (JsonResult) result;
                var model = (FileManagerEntry) jsonResult.Value;
                Assert.AreEqual("new-folder-2", model.Name);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = controller.Create("", new FileManagerEntry
                {
                    Name = "New Folder 2/New Sub Folder 1",
                    Path = "",
                    IsDirectory = true
                }, _teamId);
                var jsonResult = (JsonResult) result;
                var model = (FileManagerEntry) jsonResult.Value;
                Assert.AreEqual("new-folder-2/new-sub-folder-1", model.Path);
                Assert.AreEqual("new-sub-folder-1", model.Name);
            }
        }

        [TestMethod]
        public async Task B03_TeamFolder_ReadFolders()
        {
            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("/", _teamId);
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 2);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("new-folder-2", _teamId);
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 1);
            }

            using (var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
            {
                var result = await controller.Read("new-folder-2/", _teamId);
                var jsonResult = (JsonResult) result;
                var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
                Assert.IsTrue(model?.Count() == 1);
            }
        }

        [TestMethod]
        public async Task B04_TeamFolder_UploadFiles()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var path = "New-Folder-2";

            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            foreach (var fileName in fileNames)
            {
                var guid = Guid.NewGuid();
                var file = new[] {StaticUtilities.GetFormFile(fileName)};
                var metadata =
                    $"{{\"chunkIndex\":0,\"contentType\":\"text/plain\",\"fileName\":\"{fileName}\",\"relativePath\":\"{fileName}\",\"totalFileSize\":{file.Length},\"totalChunks\":1,\"uploadUid\":\"{guid}\"}}";

                var result = (JsonResult) await controller.Upload(file, metadata, path, _teamId);

                var model = (FileUploadResult) result.Value;

                Assert.IsTrue(model.uploaded);
                Assert.AreEqual(guid.ToString(), model.fileUid);

                var blobClient = blobService.GetBlobClient($"pub/teams/{_teamId}/new-folder-2/{fileName.ToLower()}");

                Assert.IsTrue(await blobClient.ExistsAsync());
            }
        }

        [TestMethod]
        public async Task B05_TeamFolder_RenameFolderWithFiles()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var oldFolder = "New-Folder-2";
            var newFolder = "New-Folder-3a";
            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            var fileManagerEntry = new FileManagerEntry
            {
                Created = default,
                CreatedUtc = default,
                Extension = null,
                HasDirectories = false,
                IsDirectory = true,
                Modified = default,
                ModifiedUtc = default,
                Name = newFolder,
                Path = oldFolder,
                Size = 0
            };

            var result = (JsonResult) await controller.Update(fileManagerEntry, _teamId);

            var model = (FileManagerEntry) result.Value;
            Assert.AreEqual(newFolder.ToLower(), model.Path);
            Assert.AreEqual(newFolder.ToLower(), model.Name);

            foreach (var fileName in fileNames)
            {
                var blobClient =
                    blobService.GetBlobClient($"pub/teams/{_teamId}/{newFolder.ToLower()}/{fileName.ToLower()}");
                Assert.IsTrue(await blobClient.ExistsAsync());
            }
        }

        [TestMethod]
        public async Task B06_TeamFolder_RenameFiles()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));

            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};

            var blobService = StaticUtilities.GetAzureBlobService();


            foreach (var fileName in fileNames)
            {
                var fileManagerEntry = new FileManagerEntry
                {
                    Created = default,
                    CreatedUtc = default,
                    Extension = null,
                    HasDirectories = false,
                    IsDirectory = false,
                    Modified = default,
                    ModifiedUtc = default,
                    Name = $"t-{fileName}",
                    Path = $"New-Folder-3a/{fileName}",
                    Size = 0
                };

                var result = (JsonResult) await controller.Update(fileManagerEntry, _teamId);

                var model = (FileManagerEntry) result.Value;
                Assert.AreEqual($"new-folder-3a/t-{fileName.ToLower()}", model.Path);
                Assert.AreEqual(Path.GetFileNameWithoutExtension($"t-{fileName}"), model.Name);

                var blobClient = blobService.GetBlobClient($"pub/new-folder-3a/t-{fileName}");
                Assert.IsTrue(await blobClient.ExistsAsync());
            }
        }

        [TestMethod]
        public async Task C01_ReReadRootFolder()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
            var result = await controller.Read("/");
            var jsonResult = (JsonResult) result;
            var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
            Assert.IsTrue(model?.Count() == 3);
        }

        [TestMethod]
        public async Task C02_ReReadTeamRootFolder()
        {
            using var controller =
                StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
            var result = await controller.Read("/", _teamId);
            var jsonResult = (JsonResult) result;
            var model = (IEnumerable<FileManagerEntry>) jsonResult.Value;
            Assert.IsTrue(model?.Count() == 2);
        }

        [TestMethod]
        public async Task D01_DeleteFiles_Fail()
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

            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var testFolder = "New-Folder-3a";

            // THESE FILES DO NOT EXIST
            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            foreach (var fileName in fileNames)
            {
                var fileManagerEntry = new FileManagerEntry
                {
                    Created = default,
                    CreatedUtc = default,
                    Extension = null,
                    HasDirectories = false,
                    IsDirectory = false,
                    Modified = default,
                    ModifiedUtc = default,
                    Name = fileName,
                    Path = $"{testFolder}/{fileName}",
                    Size = 0
                };

                using var controller =
                    StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
                await Assert.ThrowsExceptionAsync<Exception>(() => controller.Destroy(fileManagerEntry, null));
            }
        }

        [TestMethod]
        public async Task D02_DeleteFilesInTeamFolder_Fail()
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
            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var testFolder = "New-Folder-3a";

            // THESE FILES DO NOT EXIST
            var fileNames = new[] {"helloworld1.txt", "helloworld2.txt", "helloworld3.txt", "helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            foreach (var fileName in fileNames)
            {
                var fileManagerEntry = new FileManagerEntry
                {
                    Created = default,
                    CreatedUtc = default,
                    Extension = null,
                    HasDirectories = false,
                    IsDirectory = false,
                    Modified = default,
                    ModifiedUtc = default,
                    Name = fileName,
                    Path = $"{testFolder}/{fileName}",
                    Size = 0
                };

                using var controller =
                    StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com"));
                await Assert.ThrowsExceptionAsync<Exception>(() => controller.Destroy(fileManagerEntry, _teamId));
            }
        }

        [TestMethod]
        public async Task D03_DeleteFiles_Success()
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

            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var testFolder = "New-Folder-3a";

            var fileNames = new[] {"t-helloworld1.txt", "t-helloworld2.txt", "t-helloworld3.txt", "t-helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            foreach (var fileName in fileNames)
            {
                var fileManagerEntry = new FileManagerEntry
                {
                    Created = default,
                    CreatedUtc = default,
                    Extension = null,
                    HasDirectories = false,
                    IsDirectory = false,
                    Modified = default,
                    ModifiedUtc = default,
                    Name = fileName,
                    Path = $"{testFolder}/{fileName}",
                    Size = 0
                };
                using (var controller =
                    StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
                {
                    await controller.Destroy(fileManagerEntry, null);
                }

                var blobClient = blobService.GetBlobClient($"pub/{testFolder}/{fileName}");

                Assert.IsFalse(await blobClient.ExistsAsync());
            }
        }

        [TestMethod]
        public async Task D04_DeleteFilesInTeamFolder_Success()
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
            // {"chunkIndex":0,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // {"chunkIndex":428,"contentType":"image/jpeg","fileName":"stbart.jpg","relativePath":"stbart.jpg","totalFileSize":491463,"totalChunks":447,"uploadUid":"5d6e5675-7ffe-428c-bae0-7afc487bbdf3"}
            // Folder: New-Folder-2/

            var testFolder = "New-Folder-3a";

            var fileNames = new[] {"t-helloworld1.txt", "t-helloworld2.txt", "t-helloworld3.txt", "t-helloworld4.txt"};
            var blobService = StaticUtilities.GetAzureBlobService();

            foreach (var fileName in fileNames)
            {
                var fileManagerEntry = new FileManagerEntry
                {
                    Created = default,
                    CreatedUtc = default,
                    Extension = null,
                    HasDirectories = false,
                    IsDirectory = false,
                    Modified = default,
                    ModifiedUtc = default,
                    Name = fileName,
                    Path = $"{testFolder}/{fileName}",
                    Size = 0
                };

                using (var controller =
                    StaticUtilities.GetFileManagerController(await StaticUtilities.GetPrincipal("foo@foo.com")))
                {
                    await controller.Destroy(fileManagerEntry, _teamId);
                }

                var blobClient = blobService.GetBlobClient($"pub/teams/{_teamId}/{testFolder}/{fileName}");

                Assert.IsFalse(await blobClient.ExistsAsync());
            }
        }
    }
}