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
using System.Text;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     custom downloader
    /// </summary>
    public interface IUpdaterDownloadPackageHandler
    {
        string GetFullUrl();
        string GetSavePath();
    }

    /// <summary>
    /// </summary>
    public class UpdaterDownloadPackageHandler : UpdaterHandler, IUpdaterDownloadPackageHandler
    {
        private UpdaterCheckVersionHandler _checkVersionHandler;
        private string _urlPrefix;
        private string _packageNamePrefix;
        private string _packageExtension;
        private string _downloadToPath;
        private string _savePath;
        private string _fullUrl;
        private HttpDownloader _down;
        private HttpRequester.BeforeReadAsyncHookDelegate _requesterAsyncHook = null;

        public int RemoteVersion
        {
            get { return _checkVersionHandler.RemoteVersion; }
        }

        public override double Progress
        {
            get { return _down != null ? _down.Progress : 0d; }
        }

        /// <summary>
        ///     How many bytes all download
        /// </summary>
        public long TotalDownloadSize
        {
            get { return _down != null ? _down.TotalSize : 0; }
        }

        /// <summary>
        ///     How many bytes has downloaded
        /// </summary>
        public long DownloadedSize
        {
            get { return _down != null ? _down.DownloadedSize : 0; }
        }

        /// <summary>
        /// </summary>
        /// <param name="checkVersion"></param>
        /// <param name="downloadToPath">ע�����ص���ʱĿ¼����Ҫ�Ͳ���Ŀ¼һһ�£�</param>
        /// <param name="urlPrefix"></param>
        /// <param name="packageNamePrefix"></param>
        /// <param name="packageExtension"></param>
        public UpdaterDownloadPackageHandler(UpdaterCheckVersionHandler checkVersion, string downloadToPath,
            string urlPrefix, string packageNamePrefix, string packageExtension = ".zip")
        {
			_downloadToPath = Path.GetFullPath(downloadToPath);
            _checkVersionHandler = checkVersion;
            _urlPrefix = urlPrefix;
            _packageNamePrefix = packageNamePrefix;
            _packageExtension = packageExtension;
        }

        protected internal override void Start()
        {
            // construct the url
            var url = new StringBuilder();
            url.Append(_urlPrefix);
            if (url[url.Length - 1] != '/')
                url.Append('/');

            // zip file name
            var fileName = string.Format("{0}.{1}-{2}{3}", _packageNamePrefix, _checkVersionHandler.LocalVersion,
                _checkVersionHandler.RemoteVersion,
                _packageExtension);
            url.AppendFormat(fileName);

            // ���̵�ַ
            var savePath = Path.Combine(_downloadToPath, fileName);
            _savePath = savePath;
            _fullUrl = url.ToString();

            _down = new HttpDownloader(_fullUrl, _savePath, true);
            _down.SetRequsterAsyncHook(_requesterAsyncHook);
            _down.SetFinishCallback((_) =>
            {
                if (_.Error != null)
                    OnError(this, string.Format("{0} : {1}", _fullUrl, _.Error.Message));

                Finish();
            });

            _down.Start();
        }


        /// <summary>
        /// set downloader's requester async hook
        /// </summary>
        /// <param name="asyncHook"></param>
        public void SetRequsterAsyncHook(HttpRequester.BeforeReadAsyncHookDelegate asyncHook)
        {
            _requesterAsyncHook = asyncHook;
        }

        /// <summary>
        ///     Get download zip url
        /// </summary>
        /// <returns></returns>
        public string GetFullUrl()
        {
            return _fullUrl;
        }

        /// <summary>
        ///     Get Downloaded files saved path
        /// </summary>
        /// <returns></returns>
        public string GetSavePath()
        {
            if (!IsFinished) throw new Exception("Not finished yet");
            return _savePath;
        }
    }
}