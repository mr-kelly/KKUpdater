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
    public abstract class UpdaterHandler
    {
        public delegate void FinishCallbackDelegate(UpdaterHandler handler);
        internal FinishCallbackDelegate OnFinishedCallback;
        public bool IsFinished { get; private set; }
        public Exception Error { get; private set; }

        /// <summary>
        ///     Mark all the multi thread logs
        /// </summary>
        private StringBuilder _logs;

        public virtual double Progress
        {
            get { return IsFinished ? 1d : 0d; }
        }

        public bool IsError
        {
            get { return Error != null; }
        }

        /// <summary>
        ///     Custom the behaviour of OnError by override this delegate
        /// </summary>
        public static Action<string> CustomOnError;

        protected void OnEnter()
        {
        }

        /// <summary>
        ///     Doing stuff
        /// </summary>
        protected internal abstract void Start();

        protected void OnLeave()
        {
        }

        protected void Finish()
        {
            IsFinished = true;
            if (OnFinishedCallback != null)
                OnFinishedCallback(this);
        }

        /// <summary>
        /// 设置完成回调
        /// </summary>
        /// <param name="callback"></param>
        public void SetFinishCallback(FinishCallbackDelegate callback)
        {
            OnFinishedCallback = callback;
        }

        /// <summary>
        ///     Error handler
        /// </summary>
        /// <param name="message"></param>
        protected static void OnError(UpdaterHandler handler, string message, params object[] args)
        {
            if (handler != null)
                handler.AppendLog(message, args);

            var errMsg = string.Format(message, args);
            if (handler != null)
            {
                handler.Error = new UpdaterException(errMsg);
                handler.IsFinished = true;
            }

            if (CustomOnError != null)
                CustomOnError(errMsg);
            //            else
            //                throw new UpdaterException(errMsg);
        }

        /// <summary>
        ///     Decide transition to which next handler
        /// </summary>
        /// <param name="all"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        protected internal virtual UpdaterHandler OnTransition(UpdaterHandler[] all, UpdaterHandler next)
        {
            return next;
        }

        /// <summary>
        ///     Append logs to logs container
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void AppendLog(string format, params object[] args)
        {
            if (_logs == null)
                _logs = new StringBuilder();
            _logs.AppendLine(string.Format(format, args));
        }

        /// <summary>
        ///     return string builder's logs
        /// </summary>
        /// <returns></returns>
        public string GetLogs()
        {
            if (_logs != null)
                return _logs.ToString();
            return null;
        }
    }
}