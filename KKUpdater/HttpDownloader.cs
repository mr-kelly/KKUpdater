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
using System.Text;
using System.IO;
using System.Threading;

namespace KKUpdater
{
    /// <summary>
    ///     Cache + Break point downloader
    /// </summary>
    public class HttpDownloader
    {
        /// <summary>
        ///     Reqeuster's buffer bytes, bigger, faster
        /// </summary>
        private const int BufferSize = 10240;

        private string _saveFullPath;

        public string SaveFullPath
        {
            get { return _saveFullPath; }
        }

        public string Url { get; private set; }

        //CWWWLoader WWWLoader;

        /// <summary>
        ///     How many seconds will connect time out
        /// </summary>
        private float TimeOutDefine;


        public HttpRequester Requester { get; private set; }

        public delegate void HttpDownloaderCallback(HttpDownloader downloader);

        private HttpDownloaderCallback _stepCallback;
        private HttpDownloaderCallback _finishCallback;

        private bool _isFinished;

        public bool IsFinished
        {
            get { return _isFinished; }

            private set
            {
                _isFinished = value;
                if (_finishCallback != null)
                    _finishCallback(this);
            }
        }

        public Exception Error { get; private set; }

        public bool IsError
        {
            get { return Error != null; }
        }
        private bool _useContinue; // 是否断点续传
        private bool _useCache;
        private int _expireDays = 1; // 过期时间, 默认1天

        //public WWW Www { get { return WWWLoader.Www; } }
        public double Progress
        {
            get
            {
                if (IsFinished) return 1;
                if (TotalSize <= 0) return 0;
                return DownloadedSize / (double)TotalSize;
            }
        }

        //public float Speed { get { return WWWLoader.LoadSpeed; } } // 速度

        /// <summary>
        /// 下载的整个大小，在获取到Response后，会设置这个值 
        /// </summary>
        public long TotalSize
        {
            get { return Requester != null ? Requester.TotalSize : 0; }

        }
        /// <summary>
        /// 下载了多少？
        /// </summary>
        public long DownloadedSize
        {
            get { return Requester != null ? Requester.RequestedSize : 0; }
        }
        private HttpRequester.BeforeReadAsyncHookDelegate _requesterAsyncHook;

        private HttpDownloader()
        {
        }

        public HttpDownloader(string fullUrl, string saveFullPath, bool useContinue = false, bool useCache = false,
            int expireDays = 1, int timeout = 5)
        {
            Ctor(fullUrl, saveFullPath, useContinue, useCache, expireDays, timeout);
        }

        public void SetFinishCallback(HttpDownloaderCallback finishCallback)
        {
            _finishCallback = finishCallback;
        }

        public void SetRequsterAsyncHook(HttpRequester.BeforeReadAsyncHookDelegate asyncHook)
        {
            _requesterAsyncHook = asyncHook;

        }

        /// <summary>
        ///     Set every step of download size changed,
        ///     attension, callback in the thread pool
        /// </summary>
        /// <param name="stepCallback"></param>
        public HttpDownloader SetStepCallback(HttpDownloaderCallback stepCallback)
        {
            _stepCallback = stepCallback;
            return this;
        }


        /// <summary>
        /// </summary>
        /// <param name="fullUrl"></param>
        /// <param name="saveFullPath">完整的保存路径！</param>
        /// <param name="useContinue">是否断点续传</param>
        /// <param name="useCache">如果存在则不下载了！</param>
        /// <param name="expireDays"></param>
        /// <param name="timeout"></param>
        public static HttpDownloader Load(string fullUrl, string saveFullPath, bool useContinue = false,
            bool useCache = false, int expireDays = 1, int timeout = 5)
        {
            var downloader = new HttpDownloader();
            downloader.Ctor(fullUrl, saveFullPath, useContinue, useCache, expireDays, timeout);

            return downloader;
        }

        private void Ctor(string fullUrl, string saveFullPath, bool useContinue, bool useCache = false,
            int expireDays = 1, int timeout = 10)
        {
            Url = fullUrl;
            _saveFullPath = Path.GetFullPath(saveFullPath);
            _useCache = useCache;
            _useContinue = useContinue;
            _expireDays = expireDays;
            TimeOutDefine = timeout; // 默认10秒延遲
        }

        public static HttpDownloader Load(string fullUrl, string saveFullPath, int expireDays, int timeout = 5)
        {
            return Load(fullUrl, saveFullPath, expireDays != 0, true, expireDays, timeout);
        }

        private void StartDownload()
        {
            if (_useCache && File.Exists(_saveFullPath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(_saveFullPath);
                Log("缓存文件: {0}, 最后修改时间: {1}", _saveFullPath, lastWriteTime);
                var deltaDays = (DateTime.Now - lastWriteTime).TotalDays;
                // 文件未过期
                if (deltaDays < _expireDays)
                {
                    Log("缓存文件未过期 {0}", _saveFullPath);
                    IsFinished = true;
                    //                    yield break;
                    return;
                }
            }

            string dir = Path.GetDirectoryName(_saveFullPath);
            if (dir == null)
                throw new NullReferenceException("Cannot get directory name: " + _saveFullPath);

            if (!Directory.Exists(dir) && !string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            ThreadableResumeDownload(Url);
        }

        private void Log(string msg, params object[] args)
        {
            Console.WriteLine(msg, args);
        }

        public byte[] GetDatas()
        {
            if (!IsFinished || IsError)
                throw new Exception("GetDatas: Not finished yet, or check error!");

            return System.IO.File.ReadAllBytes(_saveFullPath);
        }

        public string TmpDownloadPath
        {
            get { return _saveFullPath + ".download"; }
        }

        private void ThreadableResumeDownload(string url)
        {
            if (_isFinished)
            {
                Error = new Exception("Before ThreadableResumeDownload Error!");
                return;
            }

            System.IO.FileStream downloadFileStream;
            //打开上次下载的文件或新建文件 
            long lStartPos = 0;

            if (_useContinue && System.IO.File.Exists(TmpDownloadPath))
            {
                downloadFileStream = System.IO.File.OpenWrite(TmpDownloadPath);
                lStartPos = downloadFileStream.Length;
                downloadFileStream.Seek(lStartPos, System.IO.SeekOrigin.Current); //移动文件流中的当前指针 

                Log("Resume.... from {0}", lStartPos);
                //CDebug.LogConsole_MultiThread("Resume.... from {0}", lStartPos);
            }
            else
            {
                downloadFileStream = new System.IO.FileStream(TmpDownloadPath, System.IO.FileMode.OpenOrCreate);
                lStartPos = 0;
            }

            Requester = new HttpRequester(Url, downloadFileStream, BufferSize);
            if (lStartPos > 0)
                Requester.SetRequestRange(lStartPos);

            Requester.BeforeReadAsyncHook = _requesterAsyncHook;
            Requester.SetFinishCallback(OnRequestFinish);

            Requester.SetStreamCallback((req) =>
            {
                if (_stepCallback != null)
                    _stepCallback(this);
            });
            Requester.Start();
        }

        private void OnRequestFinish(HttpRequester req)
        {
            try
            {
                req.Dispose(); // 释放写入的Stream...进行移动操作
                Error = req.Error;

                if (IsError)
                {
                    Log("下载过程中出现错误:" + Error.ToString());
                    // 如果非断点续传模式，错误删掉临时文件
                    if (!_useContinue)
                    {
                        if (File.Exists(TmpDownloadPath))
                            File.Delete(TmpDownloadPath); // delete temporary file
                    }
                }
                else
                {
                    if (File.Exists(_saveFullPath))
                        File.Delete(_saveFullPath);
                    File.Move(TmpDownloadPath, _saveFullPath);
                }
            }
            catch (Exception e)
            {
                Error = e;
            }


            IsFinished = true;
        }

        /// <summary>
        ///     Do start the download thread
        /// </summary>
        public void Start()
        {
            StartDownload();
        }

        /// <summary>
        /// Cancel downloader, now only can stop the stream read process
        /// </summary>
        public void Cancel()
        {
            if (Requester != null)
            {
                Requester.Cancel();
            }
            Error = new CancelException();
            IsFinished = true;
        }
    }
}