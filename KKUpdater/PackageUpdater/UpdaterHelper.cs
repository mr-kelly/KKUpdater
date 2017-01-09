#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     Predefine some updater state group
    /// </summary>
    public class UpdaterHelper
    {
        /// <summary>
        ///     Standard, all updater
        /// </summary>
        /// <returns></returns>
        public static UpdaterHandler[] CreateAllUpdater(string localUpdatePath, string urlPrefix,
            string packageNamePrefix, string versionFileName = ".resource_version", bool isDebug = true)
        {
            var stop = new UpdaterIdleHandler();
            var flagChecker = new UpdaterFlagCheckHandler(localUpdatePath, "test.foo.bar.1.0.0");
            var verifyFiles = new UpdaterVerifyFileHandler(localUpdatePath, flagChecker);
            var requestVersion = new UpdaterRequestVersionHandler(urlPrefix, packageNamePrefix, versionFileName, true);
            var checkVersion = new UpdaterCheckVersionHandler(requestVersion, localUpdatePath, versionFileName);
            var download = new UpdaterDownloadPackageHandler(checkVersion, localUpdatePath + "/../", urlPrefix,
                packageNamePrefix);
            var verifyPackage = new UpdaterVerifyPackageHandler(download);
            var decompress = new UpdaterDecompressHandler(isDebug, localUpdatePath, download);
            decompress.IsDeleteZip = false;
            return new UpdaterHandler[]
            {
                stop,
                flagChecker,
                isDebug ? (UpdaterHandler) stop : verifyFiles, // debug mode can ignore this step
                requestVersion,
                checkVersion,
                download,
                verifyPackage,
                decompress,
                verifyFiles,
            };
        }
    }
}