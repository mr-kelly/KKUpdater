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

namespace KKUpdater.FilePuller
{
    /// <summary>
    ///     Auto http check the file modified time, mark the last file meta info.
    ///     When file has not modified, do nothing. when file modifed, do something
    /// </summary>
    public class FilePuller
    {
		/// <summary>
		/// File save where
		/// </summary>
		/// <value>The save path.</value>
		public string SavePath { get; private set;}

        private string _url;

        /// <summary>
        ///     Here are all the url's last modified time records
        /// </summary>
        private string _pullerMetaFolderPath;

        private HttpRequester _headerRequest;
        private HttpDownloader _downloader;
        private DateTime _currentLastModifiedUtc;

        /// <summary>
        ///     Meta file is the MD5 of the download last modified time record
        /// </summary>
        private string _metaFilePath;

		public delegate void FinishCallbackDelegate(FilePuller puller);
		private FinishCallbackDelegate _finishedCallback;
		private bool _isFinished;
        /// <summary>
        /// Add query string to avoid cdn cache
        /// </summary>
        private bool _addTickQueryString;

        public bool IsFinished
		{
			get
			{
				return _isFinished;
			}
			set
			{
				_isFinished = value;
				if (_finishedCallback != null)
				{
					_finishedCallback(this);
				}
			}
		}

		/// <summary>
		/// Gets the error.
		/// </summary>
		/// <value>The error.</value>
        public Exception Error { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:KKUpdater.FilePuller.FilePuller"/> is error.
		/// </summary>
		/// <value><c>true</c> if is error; otherwise, <c>false</c>.</value>
		public bool IsError
		{
			get
			{
				return Error != null;
			}
		}

        /// <summary>
        ///     If the url has changed and downloaded
        /// </summary>
        public bool HasDownloaded { get; private set; }

        /// <summary>
        /// the progress of the puller
        /// </summary>
        public double Progress
        {
            get
            {
                if (_isFinished)
                    return 1;
                return _downloader != null ? _downloader.Progress : 0;
            }
        }

        public FilePuller(string url, string savePath, string pullerMetaFolderPath, bool addTickQueryString = false)
        {
            _url = url;
            SavePath = savePath;
            _pullerMetaFolderPath = pullerMetaFolderPath;
            _addTickQueryString = addTickQueryString;
            if (_addTickQueryString)
            {
                _url += string.Format("?t={0}", DateTime.Now.Ticks.ToString());
            }
        }

        public void Start()
        {
            if (!Directory.Exists(_pullerMetaFolderPath))
            {
                Directory.CreateDirectory(_pullerMetaFolderPath);
            }

            _headerRequest = new HttpRequester(_url);
            _headerRequest.SetRequestMethod("HEAD");
            _headerRequest.SetFinishCallback(OnHeaderRequestFinish);
            _headerRequest.Start();
        }

		/// <summary>
		/// Sets the finish callback.
		/// </summary>
		/// <param name="callback">Callback.</param>
		public void SetFinishCallback(FinishCallbackDelegate callback)
		{
			_finishedCallback = callback;
		}

        private void OnHeaderRequestFinish(HttpRequester req)
        {
            if (req.IsError)
            {
                Error = req.Error;
                IsFinished = true;
                return;
            }

            using (req)
            {
                var lastModified = req.Response.LastModified;
                // md5(savepath) to save the last modified time
                var metaKey = SavePath;
                _metaFilePath = Path.Combine(_pullerMetaFolderPath, Md5Helper.Md5String(metaKey));

                // no handle for UTC or time zone
                DateTime metaFileDateTime = DateTime.MinValue;
                if (File.Exists(_metaFilePath))
                {
                    long metaFileTicks;
                    var metaFileTicksStr = File.ReadAllText(_metaFilePath);
                    if (!long.TryParse(metaFileTicksStr, out metaFileTicks))
                    {
                        Error = new Exception("meta file ticks parse error");
                        IsFinished = true;
                        return;
                    }

                    metaFileDateTime = new DateTime(metaFileTicks);
                }

                _currentLastModifiedUtc = lastModified.ToUniversalTime();
                HasDownloaded = _currentLastModifiedUtc != metaFileDateTime || !File.Exists(SavePath);

                if (HasDownloaded)
                {
                    // do download
                    _downloader = new HttpDownloader(_url, SavePath);
                    _downloader.SetFinishCallback(OnDownloadFinish);
                    _downloader.Start();
                }
                else
                {
                    // no modified, ignore!
                    IsFinished = true;
                }
            }
        }


        private void OnDownloadFinish(HttpDownloader downloader)
        {
            if (downloader.IsError)
            {
                Error = downloader.Error;
                IsFinished = true;
                return;
            }

            IsFinished = true;
            // finish all, write record
            File.WriteAllText(_metaFilePath, _currentLastModifiedUtc.Ticks.ToString());
        }
    }
}