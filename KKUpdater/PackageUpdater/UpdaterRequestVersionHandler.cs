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
using System.Text;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     You can custom requst version method,  like use Unity's WWW
    /// </summary>
    public interface IUpdaterRequestVersionHandler
    {
        int GetVersion();
    }

    /// <summary>
    /// </summary>
    public class UpdaterRequestVersionHandler : UpdaterHandler, IUpdaterRequestVersionHandler
    {
        /// <summary>
        ///     result of requested
        /// </summary>
        protected int Result;

        private readonly string _urlPrefix;
        private readonly string _packageNamePrefix;
        private readonly string _versionFileName;
        private HttpRequester _req;

        /// <summary>
        ///     When this field not null, return the remote version directly
        /// </summary>
        public static int? DebugVersionResult = null;

        private bool _addRandomQueryString;

        /// <summary>
        ///     the request full url
        /// </summary>
        public string Url { get; private set; }

        public override double Progress
        {
            get { return _req != null ? _req.Progress : 0d; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="urlPrefix"></param>
        /// <param name="packageNamePrefix"></param>
        /// <param name="versionFileName"></param>
        /// <param name="addRandomQueryString">append query string to avoid cdn cache! but this will give more pressure to CDN server</param>
        public UpdaterRequestVersionHandler(string urlPrefix, string packageNamePrefix, string versionFileName, bool addRandomQueryString = true)
        {
            _urlPrefix = urlPrefix;
            _packageNamePrefix = packageNamePrefix;
            _versionFileName = versionFileName;
            _addRandomQueryString = addRandomQueryString;
        }

        /// <summary>
        /// Build the full url with version file name
        /// </summary>
        /// <param name="versionFileName"></param>
        /// <returns></returns>
        protected string BuildUrl(string versionFileName)
        {
            var url = new StringBuilder();
            url.Append(_urlPrefix);
            if (url[url.Length - 1] != '/')
                url.Append('/');
            url.Append(_packageNamePrefix);
            url.AppendFormat("{0}.txt", versionFileName);

            if (_addRandomQueryString)
            {
                var randTick = DateTime.UtcNow.Ticks;
                url.AppendFormat("?t={0}", randTick);
            }

            return url.ToString();
        }

        protected internal override void Start()
        {
            if (DebugVersionResult != null)
            {
                Result = DebugVersionResult.Value;
                Finish();
                return;
            }

            var url = BuildUrl(_versionFileName);
            Url = url;
            _req = new HttpRequester(Url);
            _req.SetFinishCallback(OnFninshedRequest);
            _req.Start();
        }

        /// <summary>
        ///     After finish Http request the file version file
        /// </summary>
        /// <param name="req"></param>
        protected void OnFninshedRequest(HttpRequester req)
        {
            if (req.Error != null)
                OnError(this, req.Error.Message);
            else
            {
                var bytes = req.DataBytes;
                if (bytes != null && bytes.Length > 0)
                {
                    Result = int.Parse(Encoding.UTF8.GetString(req.DataBytes));
                }
                else
                {
                    OnError(this, "Error bytes!!! {0}", bytes);
                }
            }


            // release
            req.Dispose();

            Finish();
        }

        /// <summary>
        ///     Get result, the requested remote version
        /// </summary>
        /// <returns></returns>
        public int GetVersion()
        {
            if (!IsFinished || IsError) throw new Exception("Error, cannot GetVersion()");

            return Result;
        }
    }
}