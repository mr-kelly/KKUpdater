#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

using System.Text;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     校验下载回来的资源包zip, 下载MD5文件
    /// </summary>
    public class UpdaterVerifyPackageHandler : UpdaterHandler
    {
        private string _md5FileUrl;
        private IUpdaterDownloadPackageHandler _downloader;

        public string Md5String { get; private set; }

        public UpdaterVerifyPackageHandler(IUpdaterDownloadPackageHandler download)
        {
            _downloader = download;
        }

        protected internal override void Start()
        {
            _md5FileUrl = _downloader.GetFullUrl() + ".md5";
            var request = new HttpRequester(_md5FileUrl);
            request.SetFinishCallback(OnFinishRequest);
            request.Start();
        }

        private void OnFinishRequest(HttpRequester req)
        {
            if (req.Error != null)
                OnError(this, req.Error.Message);

            if (req.DataBytes == null)
            {
                OnError(this, "Null request data: {0}", req.Url);
            }
            else
            {
                Md5String = Encoding.UTF8.GetString(req.DataBytes);

                var fileMd5 = Md5Helper.Md5File(_downloader.GetSavePath());
                if (fileMd5.ToLower() != Md5String.ToLower())
                {
                    OnError(this, "Md5 verify failed! Need: {0}, But: {1}, File: {2}", fileMd5, Md5String,
                        _downloader.GetSavePath());

                }
            }

            req.Dispose();
            Finish();
        }
    }
}