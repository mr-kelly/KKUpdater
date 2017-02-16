#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using KKUpdater;
using KKUpdater.FilePuller;
using KKUpdater.PackageUpdater;

namespace UpdaterTests
{
    [TestFixture()]
    public class Test
    {
        [SetUp]
        public void Init()
        {
            //			var dllDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            //			Directory.SetCurrentDirectory(dllDir);
            Console.WriteLine("Current working dir: {0}", Path.GetFullPath("./"));
        }

        [Test]
        public void TestFilePuller()
        {
            // misc test
            //            Assert.True(typeof (object).IsInstanceOfType(new string('a', 1)));

            // puller
            var puller = new FilePuller("https://www.baidu.com/favicon.ico", "puller_download/favicon.ico",
                ".puller_metas");
            puller.Start();
            while (!puller.IsFinished)
            {
                Thread.Sleep(1);
            }
            Assert.AreEqual(null, puller.Error);
            Console.WriteLine("Has Downloaded? : {0}", puller.HasDownloaded);
            Assert.True(File.Exists("puller_download/favicon.ico"));
            Assert.True(Directory.Exists(".puller_metas"));
            Assert.True(Directory.GetFiles(".puller_metas").Length > 0);
        }

        [Test()]
        public void TestRequest()
        {
            var url = "http://baidu.com";

            var requester = new HttpRequester(url);
            requester.Start();
            var lastProgress = -1d;
            while (!requester.IsFinished)
            {
                if (!lastProgress.Equals(requester.Progress))
                {
                    Console.WriteLine("Progress: {0}", requester.Progress);
                    lastProgress = requester.Progress;
                }
                Thread.Sleep(1);
            }

            var str = Encoding.UTF8.GetString(requester.DataBytes);
            Console.WriteLine(str);
            Assert.Null(requester.Error);
            Assert.AreNotEqual(null, requester.DataStream);
            Assert.True(requester.DataStream.Length > 0);
        }

        [Test]
        public void TestRequestHead()
        {
            using (var req = new HttpRequester("http://www.baidu.com"))
            {
                req.SetRequestMethod("HEAD");
                req.Start();
                while (!req.IsFinished)
                {
                    Thread.Sleep(1);
                }
                Assert.True(req.Response.Headers.Count > 0);

                Assert.True(req.Response.LastModified.Year > 1970);
            }
        }

        [Test()]
        public void TestDownloader()
        {
            var zipUrl = "http://www.baidu.com";
//            var zipUrl = "http://dzp79b220mp4w.cloudfront.net/g1-resources-package/trunk/g1.resources.trunk.1.4.0.0.zip"; // very slow, 弱网络
            var downloader = new HttpDownloader(zipUrl, "test_download.html");
            downloader.SetStepCallback(
                (ab) =>
                {
                    Console.WriteLine(string.Format("Downloader progress: {0}", downloader.Progress));
                });
            downloader.Start();
            var lastProgress = 0d;
            while (!downloader.IsFinished)
            {
                var progress = downloader.Progress;
                if (!lastProgress.Equals(progress))
                {
                    //		        Assert.True(progress > 0);
                    //		        Assert.True(progress < 1);
//                    Console.WriteLine("Progress: {0}", progress);
                    lastProgress = progress;
                }
                Thread.Sleep(1);
            }

            Assert.False(downloader.IsError);
            Assert.True(File.Exists("test_download.html"));
        }

        [Test]
        public void TestFileListPuller()
        {
            var listPuller = new FileListPuller("http://sjres.icantw.com/g1-resources-package/pullfiles_debug/trunk", ".list.txt", Path.GetFullPath("./listPuller"), Path.GetFullPath("./.listPullerMeta"), true);

            listPuller.Start();

            while (!listPuller.IsFinished)
            {
                Console.WriteLine("FileListPuller Progress: {0}", listPuller.Progress);
                Thread.Sleep(1);
            }

            Assert.AreEqual(true, listPuller.IsFinished);
            Assert.AreEqual(null, listPuller.Error);
            Assert.True(File.Exists("listPuller/.list.txt"));

        }

        [Test]
        public void TestRequsterAsyncHook()
        {
            
            var zipUrl = "http://sjres.icantw.com/g1-resources-package/trunk/g1.resources.trunk.1.3.0.3.zip";
            var req = new HttpRequester(zipUrl);
            req.BeforeReadAsyncHook = (req11, doRead, doStop) =>
            {
                Assert.Greater(req.TotalSize, 0);
                doStop();
            };
            req.Start();
            while (!req.IsFinished)
                Thread.Sleep(1);

            Assert.True(req.Error is CancelException);

            var req2 = new HttpRequester(zipUrl);
            req2.BeforeReadAsyncHook = (req22, doRead, doStop) =>
            {
                doRead(); // do nothing, begin read
            };
            req2.Start();
            while (!req2.IsFinished)
                Thread.Sleep(1);

            Assert.False(req2.IsError);
        }

        /// <summary>
        /// 测试断点续传
        /// </summary>
        [Test]
        public void TestContinue()
        {
            var zipUrl = "http://sjres.icantw.com/g1-resources-package/trunk/g1.resources.trunk.1.3.0.3.zip";
//            var zipUrl = "http://dzp79b220mp4w.cloudfront.net/g1-resources-package/trunk/g1.resources.trunk.1.4.0.0.zip"; // very slow, 弱网络
            var savePath = "test_continue.zip";

            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            var downloader = new HttpDownloader(zipUrl, savePath, true);
            downloader.Start();

            Thread.Sleep(2000);
            downloader.Cancel();
            Console.WriteLine("Canceled! Now : {0}B/{1}B, {2}%", downloader.DownloadedSize, downloader.TotalSize,
                downloader.Progress*100d);


            Assert.True(File.Exists(savePath + ".download"));
            Assert.True(!File.Exists(savePath));


            Console.WriteLine("Start continue downloader...");
            downloader = new HttpDownloader(zipUrl, savePath, true);
            downloader.Start();

            while (!downloader.IsFinished)
            {
                Console.WriteLine("Continue! Now : {0}B/{1}B, {2}%", downloader.DownloadedSize, downloader.TotalSize,
                    downloader.Progress*100d);
                Thread.Sleep(50);
            }

            Console.WriteLine("Finish! Now : {0}B/{1}B, {2}%", downloader.DownloadedSize, downloader.TotalSize,
                downloader.Progress*100d);
            Assert.True(!File.Exists(savePath + ".download"));
            Assert.True(File.Exists(savePath));
        }

        [Test]
        public void TestTGAll()
        {
            // force request version result
            //            UpdaterRequestVersionHandler.DebugVersionResult = 2;

            var isDebug = true;
            var urlPrefix = "http://sjres.icantw.com/g1-resources-package/trunk";
            var namePrefix = "g1.resources.trunk.1.2.0";
            var upHandlers = UpdaterHelper.CreateAllUpdater("./TestUpdate", urlPrefix, namePrefix,
                ".resource_version", isDebug);

            UpdaterDecompressHandler.OnDecompressedEvent += OnDecompressEvent;

            PatchListFile.PatchListCustomWriter = (bytes) =>
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] ^= 1;
                }

                return bytes;
            };

            PatchListFile.PatchListCustomReader = (patchListPath) =>
            {
                var bytes = File.ReadAllBytes(patchListPath);
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] ^= 1;
                }
                var stream = new MemoryStream(bytes);
                return new StreamReader(stream);
            };

            var updater = new Updater(upHandlers);

            updater.OnTransitionEvent +=
                (current, next) =>
                {
                    Console.WriteLine("----------------\n[OnTransitionEvent:{0}]\n{1}\n----------------",
                        current.GetType(), current.GetLogs());
                };

            var thread = updater.StartThread();

            while (!thread.IsDone)
            {
                Console.WriteLine("{0} : {1}%", thread.CurrentHandler, thread.Progress * 100d);
                if (thread.CurrentHandler is UpdaterDownloadPackageHandler)
                {
                    var downloader = thread.CurrentHandler as UpdaterDownloadPackageHandler;
                    Console.WriteLine("TotalSize: " + downloader.TotalDownloadSize);
                    Console.WriteLine("DownloadSize: " + downloader.DownloadedSize);
                }
                Thread.Sleep(1);
            }
            // Check if RequestVersionHandler error, ignore
            Console.WriteLine(thread.Progress);
            if (thread.IsError)
            {
                Console.WriteLine("Error: {0}, Handler: {1}", thread.Error, thread.ErrorHandler);
            }
            Assert.False(thread.IsError);


            var checkVersionHandler = upHandlers[4] as UpdaterCheckVersionHandler;
            if (isDebug && checkVersionHandler.IsFinished &&
                checkVersionHandler.LocalVersion != checkVersionHandler.RemoteVersion)
            {
                // 证明需要更新

                // downloadHandler
                var downloader = upHandlers[5] as UpdaterDownloadPackageHandler;
                var zipPath = downloader.GetSavePath();
                Assert.True(File.Exists(zipPath));
            }
        }

        /// <summary>
        ///     After decompress, before verify
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void OnDecompressEvent(UpdaterDecompressHandler arg1, List<string> arg2)
        {
            foreach (var f in arg2)
            {
                Console.WriteLine("Decompressed: {0}", f);
            }
        }

        [Test]
        public void TestSaltMd5()
        {
            var salt = "!*@&(!&$!";
            var md5 = Md5Helper.Md5String("abcdefg", salt);

            Console.WriteLine(md5);
            Assert.IsNotNullOrEmpty(md5);
            Assert.AreEqual(32, md5.Length);


            var md5File = Md5Helper.Md5File("nunit.framework.dll", salt);
            Console.WriteLine(md5File);
            Assert.IsNotNullOrEmpty(md5File);
            Assert.AreEqual(32, md5File.Length);

            var md5Nosalt = Md5Helper.Md5String("abcdefg");
            Assert.AreNotEqual(md5, md5Nosalt);

            var md5FileNoSalt = Md5Helper.Md5File("nunit.framework.dll");
            Assert.AreNotEqual(md5File, md5FileNoSalt);
        }
    }
}