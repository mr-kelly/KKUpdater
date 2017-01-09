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
using ICSharpCode.SharpZipLib.Zip;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     Decompress the downloaded resource package
    /// </summary>
    public class UpdaterDecompressHandler : UpdaterHandler
    {
        private UpdaterDownloadPackageHandler _downloader;
        public string LocalUpdatePath { get; private set; }
        public string ManifestFileName { get; private set; }
        public string ResourceVersionFileName { get; private set; }

        public long TotalDecompressSize { get; private set; }
        public long DecompressCurrentSize { get; private set; }

        private readonly List<string> _decompressedList = new List<string>();
        public string DeletedFileName { get; private set; }


        /// <summary>
        /// 是否完成后删除zip包
        /// </summary>
        public bool IsDeleteZip
        {
            get { return _isDeleteZip; }
            set { _isDeleteZip = value; }
        }
        private bool _isDeleteZip = true;

        /// <summary>
        ///     after decompress, before verify, event, parse the decompressed list
        /// </summary>
        public static Action<UpdaterDecompressHandler, List<string>> OnDecompressedEvent;

        /// <summary>
        ///     差异文件列表文件
        /// </summary>
        private string _patchListFileName;

        private bool _isDebug;

        public string ResVersionPath
        {
            get { return Path.Combine(LocalUpdatePath, ResourceVersionFileName); }
        }

        /// <summary>
        ///     差异的文件路径
        /// </summary>
        public string PatchListPath
        {
            get { return Path.Combine(LocalUpdatePath, _patchListFileName); }
        }

        /// <summary>
        ///     .manifest 文件
        /// </summary>
        public string ManifestPath
        {
            get { return Path.Combine(LocalUpdatePath, ManifestFileName); }
        }

        /// <summary>
        ///     删除记录文件路径
        /// </summary>
        public string DeletedFilePath
        {
            get { return Path.Combine(LocalUpdatePath, DeletedFileName); }
        }

        public UpdaterDecompressHandler(bool isDebug, string localUpdatePath, UpdaterDownloadPackageHandler downloader,
            string resourceVersionFileName = ".resource_version", string manifestFileName = ".manifest",
            string deletedFileName = ".deleted", string patchListFileName = ".patch_list")
        {
            _isDebug = isDebug;

            LocalUpdatePath = localUpdatePath;
            _downloader = downloader;
            ManifestFileName = manifestFileName;
            DeletedFileName = deletedFileName;
            ResourceVersionFileName = resourceVersionFileName;
            _patchListFileName = patchListFileName;
        }

        public override double Progress
        {
            get
            {
                if (TotalDecompressSize == 0) return 0;
                return DecompressCurrentSize/(double) TotalDecompressSize;
            }
        }

        protected internal override void Start()
        {
            var zipPath = _downloader.GetSavePath();

            // 获取压缩文件数量
            using (var zipFile = new ZipFile(zipPath))
            {
                TotalDecompressSize = zipFile.Count;
                DecompressCurrentSize = 0;
            }
            using (var s = new ZipInputStream(File.OpenRead(zipPath)))
            {
                // 进度条重新来过
                //_progressBarCount = 0;
                //_progressBarCountLastSecond = 0;  // 重来
                //_progressBarTotalCount = (int)zipJson.Size; // 重新计算进度条

                ZipEntry theEntry;
                //var timeCount = Time.realtimeSinceStartup;

                try
                {
                    while ((theEntry = s.GetNextEntry()) != null)
                    {
                        if (theEntry.IsDirectory) continue;

                        var cleanPath = theEntry.Name.Replace("\\", "/");
                        string directorName = Path.Combine(LocalUpdatePath, Path.GetDirectoryName(cleanPath));

                        var fileName = Path.GetFileName(cleanPath);
                        // 其它解压
                        string fullFileName = Path.Combine(directorName, fileName);
                        _decompressedList.Add(cleanPath);

                        if (!Directory.Exists(directorName))
                        {
                            Directory.CreateDirectory(directorName);
                        }
                        if (!String.IsNullOrEmpty(fullFileName))
                        {
                            using (FileStream streamWriter = File.Create(fullFileName))
                            {
                                byte[] data = new byte[s.Length];
                                s.Read(data, 0, data.Length);
                                streamWriter.Write(data, 0, data.Length);

                                DecompressCurrentSize++;
                                AppendLog("解压文件: {0}, 解压的大小: {1}KB", cleanPath,
                                    data.Length/1024f);
                                // 忽略下面的resharper dispose 提示。。。，因为协程里没这问题
                                //decompressLogs.Add(cleanName); // 记录解压的文件，用于断电续传  
                            }
                        }
                    }

                    if (OnDecompressedEvent != null)
                        OnDecompressedEvent(this, _decompressedList);

                    var patchList = new PatchListFile(PatchListPath);

                    if (File.Exists(DeletedFilePath))
                    {
                        var deleteFile = new DeletedListFile(DeletedFilePath);

                        foreach (var path in deleteFile.Datas)
                        {
                            var fullPath = Path.Combine(LocalUpdatePath, path);
                            if (File.Exists(fullPath))
                            {
                                File.Delete(fullPath);
                            }
                            else
                            {
                                AppendLog("Not exist file in .deleted file list: {0}", deleteFile);
                            }
                            // remove from patchList
                            patchList.Datas.Remove(path);
                        }

                        // delete the .deleted file
                        File.Delete(DeletedFilePath);
                    }

                    // 解压的文件进行验证
                    // 进行解压的文件校验
                    var manifestTable = new ManifestFile(ManifestPath);
                    foreach (var checkPath in _decompressedList)
                    {
                        // .manifest, .delete文件不进行验证
                        if (checkPath == ManifestFileName) continue;
                        if (checkPath == DeletedFileName) continue;

                        var checkFullPath = Path.Combine(LocalUpdatePath, checkPath);
                        ManifestFile.Manifest checkMani;
                        if (!manifestTable.Datas.TryGetValue(checkPath, out checkMani))
                        {
                            throw new Exception(string.Format("Error when check file: {0}, no manifest data", checkPath));
                        }

                        if (!File.Exists(checkFullPath))
                        {
                            throw new Exception(string.Format("Error when check file: {0} not exist", checkFullPath));
                        }

                        var expectMd5 = checkMani;
                        var fileMd5 = Md5Helper.Md5File(checkFullPath);
                        if (expectMd5.MD5.ToLower() != fileMd5.ToLower())
                        {
                            throw new Exception(string.Format("Error verify on {0}, expect:{1}, but:{2}", checkFullPath,
                                expectMd5.MD5, fileMd5));
                        }
                    }

                    // 读取、创建.patch_list
                    foreach (var file in _decompressedList)
                    {
                        patchList.Add(file);
                    }
                    patchList.Save();

                    LocalVersionFile.Write(LocalUpdatePath, ResourceVersionFileName, _downloader.RemoteVersion);
                }
                catch (Exception e)
                {
                    OnError(this, e.Message);
                }
            }

            if (IsDeleteZip)
            {
                // 解压完成，对压缩包进行删除
                if (File.Exists(zipPath))
                    File.Delete(zipPath);
            }

            Finish();
        }

    }
}