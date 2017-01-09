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
using System.Collections;
using System.Threading;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     Resource File Auto Updater
    ///     A state machine, dispatch jobs with UpdaterHandler
    /// </summary>
    public class Updater
    {
        /// <summary>
        ///     Whether finish all, or error
        /// </summary>
        public bool IsDone { get; private set; }

        /// <summary>
        ///     Callback on done
        /// </summary>
        public event Action<Updater> FinishCallback;

        /// <summary>
        ///     Transition event delegate
        /// </summary>
        /// <param name="current"></param>
        /// <param name="next"></param>
        public delegate void OnTransitionEventDelegate(UpdaterHandler current, UpdaterHandler next);

        /// <summary>
        ///     Trigger on updater handler transition
        /// </summary>
        public event OnTransitionEventDelegate OnTransitionEvent;

        /// <summary>
        ///     Error string
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        ///     Which Handler error?
        /// </summary>
        public UpdaterHandler ErrorHandler { get; private set; }

        /// <summary>
        /// 默认一个handler最多60秒
        /// </summary>
        public double UpdaterHandlerTimeout = 60;


        /// <summary>
        /// 满载的handler，最多给超时5秒
        /// </summary>
        public double UpdaterHandlerFullProgressTimeout = 5;

        /// <summary>
        ///     Check if error
        /// </summary>
        public bool IsError
        {
            get { return Error != null; }
        }

        public double Progress
        {
            get
            {
                if (IsDone) return 1d;

                double p = 0;
                for (var i = 0; i < _handlers.Length; i++)
                {
                    var handler = _handlers[i];
                    if (handler != null)
                        p += handler.Progress;
                }
                return p / _handlers.Length;
            }
        }

        /// <summary>
        ///     Handlers all here
        /// </summary>
        private UpdaterHandler[] _handlers;

        public UpdaterHandler CurrentHandler { get; private set; }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="urlPrefix"></param>
        /// <param name="packageNamePrefix"></param>
        /// <param name="isDebug"></param>
        /// <param name="zipExtension"></param>
        /// <param name="md5Extension"></param>
        /// <param name="versionFileExtension"></param>
        public Updater(UpdaterHandler[] handlers)
        {
            _handlers = handlers;
        }

        /// <summary>
        ///     Return a enumerator, you can use it for thread or coroutine
        /// </summary>
        /// <returns></returns>
        public IEnumerator StartEnumerator()
        {
            for (var i = 0; i < _handlers.Length; i++)
            {
                var handler = _handlers[i];

                CurrentHandler = handler;
                handler.Start();

                var currentHandlerStartTime = DateTime.Now;
                DateTime? fullProgressStarTime = null;

                var otherError = false;
                while (!handler.IsFinished)
                {
                    if ((DateTime.Now - currentHandlerStartTime).TotalSeconds > UpdaterHandlerTimeout)
                    {
                        Error =
                            new TimeoutException(string.Format("UpdaterHandler {0} Timeout over {1}s", handler,
                                UpdaterHandlerTimeout));
                        otherError = true;
                        break;
                    }

                    if (handler.Progress >= 1)
                    {
                        if (fullProgressStarTime == null)
                            fullProgressStarTime = DateTime.Now;

                        if ((DateTime.Now - fullProgressStarTime.Value).TotalSeconds > UpdaterHandlerFullProgressTimeout)
                        {
                            Error =
                                new TimeoutException(
                                    string.Format("UpdaterHandler {0} Progress 100% but timeout over {1}s", handler,
                                        UpdaterHandlerFullProgressTimeout));
                            otherError = true;
                            break;
                        }
                    }

                    yield return null;
                }

                if (otherError)
                {
                    ErrorHandler = handler;
                    break;
                }

                if (handler.IsError)
                {
                    Error = handler.Error;
                    ErrorHandler = handler;
                    break;
                }

                // next transition
                if ((i + 1) < _handlers.Length)
                {
                    var next = _handlers[i + 1];

                    if (OnTransitionEvent != null)
                        OnTransitionEvent(CurrentHandler, next);

                    if (handler.OnTransition(_handlers, next) == null)
                    {
                        break; // if null, break next
                    }
                }
            }

            CurrentHandler = null;
            IsDone = true;
            if (FinishCallback != null)
            {
                FinishCallback(this);
            }
        }



        /// <summary>
        ///     Must call this start to start the updater
        /// </summary>
        public Updater StartThread()
        {
            ThreadPool.QueueUserWorkItem(_StartThread);
            return this;
        }

        /// <summary>
        ///     Thread function
        /// </summary>
        /// <param name="state"></param>
        private void _StartThread(object state)
        {
            for (var i = 0; i < _handlers.Length; i++)
            {
                var handler = _handlers[i];
                CurrentHandler = handler;
                handler.Start();

                var currentHandlerStartTime = DateTime.Now;
                DateTime? fullProgressStarTime = null;

                var otherError = false;
                while (!handler.IsFinished)
                {
                    if ((DateTime.Now - currentHandlerStartTime).TotalSeconds > UpdaterHandlerTimeout)
                    {
                        Error =
                            new TimeoutException(string.Format("UpdaterHandler {0} Timeout over {1}s", handler,
                                UpdaterHandlerTimeout));
                        otherError = true;
                        break;
                    }

                    if (handler.Progress >= 1)
                    {
                        if (fullProgressStarTime == null)
                            fullProgressStarTime = DateTime.Now;

                        if ((DateTime.Now - fullProgressStarTime.Value).TotalSeconds > UpdaterHandlerFullProgressTimeout)
                        {
                            Error =
                                new TimeoutException(
                                    string.Format("UpdaterHandler {0} Progress 100% but timeout over {1}s", handler,
                                        UpdaterHandlerFullProgressTimeout));
                            otherError = true;
                            break;
                        }
                    }

                    Thread.Sleep(1);
                }

                if (otherError)
                {
                    ErrorHandler = handler;
                    break;
                }

                if (handler.IsError)
                {
                    Error = handler.Error;
                    ErrorHandler = handler;
                    break;
                }

                // next transition
                if ((i + 1) < _handlers.Length)
                {
                    var next = _handlers[i + 1];

                    if (OnTransitionEvent != null)
                        OnTransitionEvent(CurrentHandler, next);

                    if (handler.OnTransition(_handlers, next) == null)
                    {
                        break; // if null, break next
                    }
                }
            }
            CurrentHandler = null;

            IsDone = true;
            if (FinishCallback != null)
            {
                FinishCallback(this);
            }
        }
    }
}