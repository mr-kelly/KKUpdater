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
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     flag file is a mark of all updater operation.
    ///     When flag changed, all update files should be clean!
    ///     Check flag file, if false, clear all local update path
    /// </summary>
    public class UpdaterFlagCheckHandler : UpdaterHandler
    {
        private string _localUpdatePath;
        public string VersionString { get; private set; }
        private string _appVersionFileName;

        public bool CheckResult { get; private set; }
        public bool ExistsFlagFile { get; private set; }
        /// <summary>
        ///     存放在持久化目录，有一个版本号标记，用来判断是否安装了新的客户端
        /// </summary>
        public string AppVersionFilePath
        {
            get { return Path.Combine(_localUpdatePath, _appVersionFileName); }
        }

        public UpdaterFlagCheckHandler(string localUpdatePath, string versionString,
            string versionFileName = ".updater_flag")
        {
            _localUpdatePath = localUpdatePath;
            VersionString = versionString;
            _appVersionFileName = versionFileName;
        }

        protected internal override void Start()
        {
            var verFilePath = Path.Combine(_localUpdatePath, _appVersionFileName);
            ExistsFlagFile = File.Exists(verFilePath);
            if (
                !ExistsFlagFile ||
                (File.Exists(verFilePath) && (File.ReadAllText(verFilePath) != VersionString))
                )
            {
                CheckResult = false;
                ThreadPool.QueueUserWorkItem(Threader, null);
            }
            else
            {
                CheckResult = true;
                Finish();
            }
        }

        private void Threader(object state)
        {
            if (Directory.Exists(_localUpdatePath))
            {
                AppendLog("Flag check not valid!");
                UpdaterVerifyFileHandler.ClearAllUpdateFiles(_localUpdatePath);
            } // else do nothing

            Finish();
        }
    }
}