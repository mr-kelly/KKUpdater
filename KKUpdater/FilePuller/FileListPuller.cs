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
using System.Text;
using System.Threading;

namespace KKUpdater.FilePuller
{
    /// <summary>
    /// Download a file list file, record all file need to pull
    /// All record must be a relative url
    /// </summary>
    public class FileListPuller
    {
        /// <summary>
        /// Whether pull files with random query string
        /// </summary>
        private bool _addTicksQueryString;
        private FilePuller _listFilePuller;

        public Action<FileListPuller> FinishCallback;

        /// <summary>
        /// Whether .list file re download which will trigger all files download check
        /// </summary>
        public bool IsListFileChanged { get; private set; }

        /// <summary>
        /// Before files download
        /// </summary>
        public Action<FileListPuller> BeforeFilesDownloadEvent;

        public bool IsFinished { get; private set; }
        public Exception Error { get; private set; }
        public bool IsError { get { return Error != null; } }
        public FilePuller ErrorPuller { get; private set; }
        public List<string> List { get; private set; }
        public List<FilePuller> PullersList { get; private set; }

        /// <summary>
        /// How many files real downloaded?
        /// </summary>
        public int DownloadedCount
        {
            get
            {
                var downloadedCount = 0;
                if (PullersList != null)
                {
                    foreach (var d in PullersList)
                    {
                        if (d != null)
                        {
                            if (d.HasDownloaded) downloadedCount++;
                        }
                    }
                }
                return downloadedCount;
            }
        }

        /// <summary>
        /// .list.txt文件临时下载，最终完成后拷贝到最后目录
        /// </summary>
        public string ListFileSavePathTmp
        {
            get { return ListFileSavePath + TmpFileExtension; }
        }


        /// <summary>
        /// Temp file 's extension
        /// </summary>
        public string TmpFileExtension { get; private set; }

        public string ListFileNameTmp
        {
            get { return ListFileName + TmpFileExtension; }
        }

        public string ListFileSavePath { get; private set; }
        public string ListFileUrl { get; private set; }

        public string SaveFolderPath { get; private set; }

        public string UrlPrefix { get; private set; }

        public string PullerMetaFolderPath { get; private set; }
        public string ListFileName { get; private set; }

        /// <summary>
        /// Get progress
        /// </summary>
        public double Progress
        {
            get
            {
                if (IsFinished)
                    return 1d;

                if (_listFilePuller == null)
                    return 0;

                var pullerCount = 1;
                var p = _listFilePuller.Progress;
                if (PullersList != null)
                {
                    pullerCount += PullersList.Count;
                    foreach (var puller in PullersList)
                    {
                        p += puller.Progress;
                    }
                }
                return p / (double)pullerCount;
            }
        }

        public FileListPuller(string urlPrefix, string listFileName, string saveFolderPath, string pullerMetaFolderPath, bool addTicksQueryString = false)
        {
            TmpFileExtension = Path.GetRandomFileName() + ".tmp";
            UrlPrefix = urlPrefix;
            SaveFolderPath = saveFolderPath;
            PullerMetaFolderPath = pullerMetaFolderPath;
            ListFileUrl = UrlCombine(urlPrefix, listFileName);
            ListFileName = listFileName;
            _addTicksQueryString = addTicksQueryString;
        }

        public void Start()
        {
            ListFileSavePath = Path.Combine(SaveFolderPath, ListFileName);

            _listFilePuller = new FilePuller(ListFileUrl, ListFileSavePathTmp, PullerMetaFolderPath, _addTicksQueryString);
            _listFilePuller.SetFinishCallback(OnListFileRequestFinish);
            _listFilePuller.Start();
        }


        /// <summary>
        /// URLs the combine.
        /// </summary>
        /// <returns>The combine.</returns>
        /// <param name="a">The alpha component.</param>
        /// <param name="b">The blue component.</param>
        string UrlCombine(string a, string b)
        {
            var url = new StringBuilder();
            url.Append(a);
            if (url[url.Length - 1] != '/')
                url.Append('/');
            url.Append(b);
            return url.ToString();
        }

        /// <summary>
        /// requester finished
        /// </summary>
        /// <param name="puller">Req.</param>
        void OnListFileRequestFinish(FilePuller puller)
        {
            if (puller.IsError)
            {
                Error = puller.Error;
                ErrorPuller = puller;
                OnFinish();
                return;
            }
            if (!puller.HasDownloaded)
            {
                // 并没有下载，则认为.list文件压根没修改，不需要重复做下载工作了~
                OnFinish();
                return;
            }

            // begin download all files, mark the list file changed flag
            IsListFileChanged = true;
            if (BeforeFilesDownloadEvent != null)
            {
                BeforeFilesDownloadEvent(this);
            }

            // read a string list of files
            List = new List<string>();
            using (var reader = new StreamReader(puller.SavePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        List.Add(line);
                    }
                }
            }

            // Start pull the list
            PullersList = new List<FilePuller>();
            foreach (var line in List)
            {
                string file;
                bool toDelete = false;
                if (line.StartsWith("-"))
                {
                    file = line.TrimStart('-').Trim();
                    toDelete = true; // 进入删除模式
                }
                else
                {
                    file = line.Trim();
                }

                var fileUrl = UrlCombine(UrlPrefix, file);
                var savePath = Path.Combine(SaveFolderPath, file);

                if (toDelete)
                {
                    if (file == "*")
                    {
                        DeleteAllOthers();
                    }
                    else
                    {
                        // just delete, don't pull
                        if (File.Exists(savePath))
                        {
                            File.Delete(savePath);
                        }
                    }
                }
                else
                {
                    var filePuller = new FilePuller(fileUrl, savePath, PullerMetaFolderPath);
                    PullersList.Add(filePuller);
                    filePuller.Start();
                }
            }

            ThreadPool.QueueUserWorkItem(ThreadCheckDownloadList, null);

        }

        /// <summary>
        /// 删除除了.list.txt文件外的所有其它文件
        /// </summary>
        protected void DeleteAllOthers()
        {
            // 删除除了.list之外的其它文件。。。
            if (Directory.Exists(SaveFolderPath))
            {
                foreach (var filepath in Directory.GetFiles(SaveFolderPath, "*", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(filepath) != ListFileName)
                    {
                        File.Delete(filepath);
                    }
                }
            }
        }

        void OnFinish()
        {
            IsFinished = true;
            if (FinishCallback != null)
                FinishCallback(this);

            if (Error != null)
            {
                // if some errors happend, delete .list.txt.tmp file
                if (File.Exists(ListFileSavePathTmp))
                    File.Delete(ListFileSavePathTmp);
            }
            else
            {
                if (File.Exists(ListFileSavePath))
                    File.Delete(ListFileSavePath);
                File.Move(ListFileSavePathTmp, ListFileSavePath);
            }

        }
        void ThreadCheckDownloadList(object state)
        {
            while (true)
            {
                if (CheckDownloadListOk())
                {
                    break;
                }
                Thread.Sleep(1);

                // TODO: timeout, tickout

            }


            // get any one puller's error as the list puller 's error
            foreach (var puller in PullersList)
            {
                if (puller.Error != null)
                {
                    Error = puller.Error;
                    ErrorPuller = puller;
                    break;
                }
            }

            OnFinish();
        }

        /// <summary>
        /// Checks the download list all puller download ok.
        /// </summary>
        /// <returns><c>true</c>, if download list ok was checked, <c>false</c> otherwise.</returns>
        bool CheckDownloadListOk()
        {
            foreach (var puller in PullersList)
            {
                if (!puller.IsFinished)
                {
                    return false;
                }
            }

            return true;

        }

    }
}
