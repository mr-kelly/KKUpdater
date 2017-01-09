#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

using System;
using System.IO;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    /// </summary>
    public class UpdaterCheckVersionHandler : UpdaterHandler
    {
        private IUpdaterRequestVersionHandler _requestVersionHandler;

        private string _localUpdaterPath;
        private string _versionFileName;

        /// <summary>
        ///     Local Version Number
        /// </summary>
        public int LocalVersion { get; private set; }

        /// <summary>
        ///     Remote requested version number
        /// </summary>
        public int RemoteVersion { get; private set; }


        public UpdaterCheckVersionHandler(IUpdaterRequestVersionHandler requestVersionHandler, string localUpdatePath,
            string versionFileName)
        {
            _requestVersionHandler = requestVersionHandler;
            _localUpdaterPath = localUpdatePath;
            _versionFileName = versionFileName;
        }

        protected internal override void Start()
        {
            // get local version
            try
            {
                LocalVersion = LocalVersionFile.GetLocalVersion(_localUpdaterPath, _versionFileName, 0);
            }
            catch (Exception e)
            {
                // parse resources_version file failed, clear all
                OnError(this, e.Message);
                UpdaterVerifyFileHandler.ClearAllUpdateFiles(_localUpdaterPath, this);
            }

            RemoteVersion = _requestVersionHandler.GetVersion();

            Finish();
        }

        protected internal override UpdaterHandler OnTransition(UpdaterHandler[] all, UpdaterHandler next)
        {
            if (LocalVersion == RemoteVersion)
                return null; // over, no next any more
            if (LocalVersion > RemoteVersion)
            {
                AppendLog("Local version greater than remote version! clear local all!");

                // clear all, re update all
                UpdaterVerifyFileHandler.ClearAllUpdateFiles(_localUpdaterPath, this);
                // IMPOSSIBLE！ local version cannot greater than Remote version! this must be a revert operation
                LocalVersion = 0;
            }

            if (RemoteVersion == 0) // no update! over now!
                return null;

            AppendLog("Local version {0}, Remote Version {1}, do update!", LocalVersion, RemoteVersion);
            // default, next go
            return next;
        }
    }
}