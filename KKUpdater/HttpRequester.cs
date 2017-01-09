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
using System.Net;
using System.IO;
using System.Threading;

namespace KKUpdater
{
    /// <summary>
    ///     Http Request Helper class
    ///     Ref to Microsoft official example:
    ///     https://msdn.microsoft.com/en-us/library/system.net.httpwebrequest.begingetresponse.aspx
    /// </summary>
    public class HttpRequester : IDisposable
    {
        static HttpRequester()
        {
            // support for https/SSL
            System.Net.ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };

            ServicePointManager.MaxServicePointIdleTime = 100000;
            ServicePointManager.DefaultConnectionLimit = 10;
        }

        public delegate void HttpRequesterCallback(HttpRequester req);

        /// <summary>
        ///     When finish give it a callback
        /// </summary>
        private HttpRequesterCallback _onFinishCallback;

        /// <summary>
        ///     Every read stream, callback
        /// </summary>
        private HttpRequesterCallback _onStreamCallback;


        /// <summary>
        /// Async hook
        /// </summary>
        /// <param name="continueRead"></param>
        /// <param name="doStop"></param>
        public delegate void BeforeReadAsyncHookDelegate(HttpRequester req, Action continueRead, Action doStop);

        /// <summary>
        /// Before read the http stream, you can hook the `before read`
        /// Example: prompt the window, let user determine whether or not continue the download?
        /// </summary>
        public BeforeReadAsyncHookDelegate BeforeReadAsyncHook;

        /// <summary>
        ///     bytes array temp buffer, will affect the download speed
        /// </summary>
        private int BufferSize;

        /// <summary>
        ///     Timeout of server response
        /// </summary>
        private const int DefaultTimeout = 4 * 1000; // 4s

        // This class stores the State of the request.
        public byte[] BufferRead;
        public HttpWebRequest Request { get; private set; }
        public HttpWebResponse Response { get; private set; }
        public Stream ResponseStream;

        /// <summary>
        ///     Request method , default GET
        /// </summary>
        private string _requestMethod = "GET";

        /// <summary>
        ///     Request URL
        /// </summary>
        public string Url { get; private set; }

        public bool IsFinished
        {
            get { return _allDoneLock.WaitOne(0); }
            private set
            {
                if (value)
                {
                    _allDoneLock.Set();
                    if (_onFinishCallback != null)
                        _onFinishCallback(this);
                }
                else
                {
                    _allDoneLock.Reset(); // dont set false
                }
            }
        }

        public Exception Error { get; private set; }

        /// <summary>
        ///     multi thread lock
        /// </summary>
        private readonly ManualResetEvent _allDoneLock = new ManualResetEvent(false);

        private long _requestRange;

        /// <summary>
        ///     How many bytes has read?
        /// </summary>
        public long RequestedSize { get; private set; }

        /// <summary>
        ///     Total size of this request
        /// </summary>
        public long TotalSize { get; private set; }

        /// <summary>
        ///     Progress
        /// </summary>
        public double Progress
        {
            get
            {
                if (TotalSize <= 0) return 0;
                return RequestedSize / (double)TotalSize;
            }
        }

        /// <summary>
        ///     get the result data stream;  all request write to this stream
        /// </summary>
        public Stream DataStream { get; private set; }

        /// <summary>
        ///     Convert the result data stream to bytes
        /// </summary>
        public byte[] DataBytes
        {
            get
            {
                if (DataStream == null)
                    return null;

                DataStream.Seek(0, SeekOrigin.Begin); // reset position
                int len = (int)DataStream.Length;
                var bytes = new byte[DataStream.Length];
                int pos = 0;
                int readSize;
                while ((readSize = DataStream.Read(bytes, pos, len - pos)) > 0)
                {
                    pos += readSize;
                }

                return bytes;
            }
        }

        public bool IsError
        {
            get { return Error != null; }
        }

        public HttpRequester(string url, Stream dataStream, int bufferSize = 1024)
        {
            Url = url;
            BufferSize = bufferSize; // default buffer size 1024
            DataStream = dataStream;
            TotalSize = long.MaxValue;
            RequestedSize = 0;

            BufferRead = new byte[BufferSize];
            Request = null;
            ResponseStream = null;
        }

        public HttpRequester(string url, int bufferSize = 1024)
            : this(url, new MemoryStream(), bufferSize)
        {
        }

        /// <summary>
        ///     Set http request method, like GET, POST,　HEAD
        /// </summary>
        /// <param name="method"></param>
        public void SetRequestMethod(string method)
        {
            _requestMethod = method;
        }

        /// <summary>
        ///     Every stream read step will callback here
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public HttpRequester SetStreamCallback(HttpRequesterCallback callback)
        {
            _onStreamCallback = callback;
            return this;
        }

        /// <summary>
        ///     When finish all download, callback
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public HttpRequester SetFinishCallback(HttpRequesterCallback callback)
        {
            _onFinishCallback = callback;
            return this;
        }

        /// <summary>
        ///     Abort the request if the timer fires.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="timedOut"></param>
        private void ResponseTimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                HttpWebRequest request = state as HttpWebRequest;
                if (request != null)
                {
                    request.Abort();
                }
            }
        }

        private void ReadTimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                if (ResponseStream != null)
                {
                    ResponseStream.Close();
                }
            }
        }
        public void Start()
        {
            try
            {
                // Create a HttpWebrequest object to the desired URL. 
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(Url);

                if (_requestRange > 0)
                {
                    myHttpWebRequest.AddRange((int)_requestRange); //设置Range值
                    RequestedSize = _requestRange;
                }

                /**
        * If you are behind a firewall and you do not have your browser proxy setup
        * you need to use the following proxy creation code.

          // Create a proxy object.
          WebProxy myProxy = new WebProxy();

          // Associate a new Uri object to the _wProxy object, using the proxy address
          // selected by the user.
          myProxy.Address = new Uri("http://myproxy");


          // Finally, initialize the Web request object proxy property with the _wProxy
          // object.
          myHttpWebRequest.Proxy=myProxy;
        ***/

                // Create an instance of the RequestState and assign the previous myHttpWebRequest
                // object to its request field.  
                Request = myHttpWebRequest;
                Request.Method = _requestMethod;
                Request.UserAgent = "github.com/mr-kelly";
                // Start the asynchronous request.
                IAsyncResult result =
                    (IAsyncResult)myHttpWebRequest.BeginGetResponse(new AsyncCallback(RespCallback), null);

                // this line implements the timeout, if there is a timeout, the callback fires and the request becomes aborted
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(ResponseTimeoutCallback),
                    myHttpWebRequest, DefaultTimeout, true);

                // The response came in the allowed time. The work processing will happen in the 
                // callback function.
                //            AllDoneLock.WaitOne();

                // Release the HttpWebResponse resource.
                //            myRequestState.response.Close();
            }
            catch (Exception e)
            {
                if (!IsFinished)
                {
                    Error = e;
                    IsFinished = true;
                }
            }
        }
        private void RespCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                // State of request is asynchronous.
                HttpWebRequest myHttpWebRequest = Request;
                //                var res = myHttpWebRequest.GetResponse();
                var res = myHttpWebRequest.EndGetResponse(asynchronousResult);
                Response = (HttpWebResponse)res;
                TotalSize = Response.ContentLength + RequestedSize; // add the part of 

                // Read the response into a Stream object.
                Stream responseStream = Response.GetResponseStream();
                ResponseStream = responseStream;

                Action doRead = () =>
                {
                    // Begin the Reading of the contents of the HTML page and print it to the console.
                    IAsyncResult readResult = responseStream.BeginRead(BufferRead, 0, BufferSize,
                        new AsyncCallback(ReadCallBack), null);
                    ThreadPool.RegisterWaitForSingleObject(readResult.AsyncWaitHandle, new WaitOrTimerCallback(ReadTimeoutCallback),
                        null, DefaultTimeout, true);
                };
                Action doStop = () =>
                {
                    Cancel();
                };

                if (BeforeReadAsyncHook != null)
                {
                    BeforeReadAsyncHook(this, doRead, doStop);
                }
                else
                {
                    doRead();
                }
            }
            catch (Exception e)
            {
                if (!IsFinished)
                {
                    var ex = new Exception(Url, e);
                    Error = ex;
                    IsFinished = true;
                }
            }
        }


        private void ReadCallBack(IAsyncResult asyncResult)
        {
            try
            {
                int read = ResponseStream.EndRead(asyncResult);
                RequestedSize += read;

                if (_onStreamCallback != null)
                    _onStreamCallback(this);
                if (read > 0)
                {
                    DataStream.Write(BufferRead, 0, read);

                    if (IsFinished)
                    {
                        return; // force stopped!
                    }

                    var readResult = ResponseStream.BeginRead(BufferRead, 0, BufferSize,
                        ReadCallBack, null);
                    ThreadPool.RegisterWaitForSingleObject(readResult.AsyncWaitHandle,
                        new WaitOrTimerCallback(ReadTimeoutCallback),
                        null, DefaultTimeout, true);
                    return;
                }
                else
                {
                    ResponseStream.Dispose();
                    IsFinished = true;
                }
            }
            catch (Exception e)
            {
                if (!IsFinished)
                {
                    var ex = new Exception(this.Url, e);
                    Error = ex;
                    IsFinished = true;
                }
            }
        }

        /// <summary>
        ///     Add range to HttpRequest class
        /// </summary>
        /// <param name="requestRange"></param>
        public void SetRequestRange(long requestRange)
        {
            _requestRange = requestRange;
        }

        public void Dispose()
        {
            if (!IsFinished)
            {
                Error = new Exception("Force finish by disposed!");
                IsFinished = true;
            }

            if (DataStream != null)
            {
                DataStream.Dispose();
            }

            if (Response != null)
            {
                Response.Close();
            }
        }

        /// <summary>
        /// Force stop the requester
        /// </summary>
        public void Cancel()
        {
            if (Response != null)
            {
                Response.Close();
            }
            if (ResponseStream != null)
            {
                ResponseStream.Close(); // 注意，要用close而不是disposed进行取消操作！
            }
            //            if (DataStream != null)
            //            {
            //                DataStream.Close();
            //            }

            if (!IsFinished)
            {
                Error = new CancelException();
                IsFinished = true;
            }
        }
    }
}