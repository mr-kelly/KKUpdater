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
using System.Threading;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    /// </summary>
    public class UpdaterVerifyFileHandler : UpdaterHandler
    {
        private string _localUpdatePath;

        private string _patchListFileName;
        private string _manifestFileName;
        private UpdaterFlagCheckHandler _flagChecker;
        private string _appVersionFileName;
        private string _resourceVersionFileName;

        public string ResourceVersionFilePath
        {
            get { return Path.Combine(_localUpdatePath, _resourceVersionFileName); }
        }

        public string PatchListFilePath
        {
            get { return Path.Combine(_localUpdatePath, _patchListFileName); }
        }

        public string ManifestFilePath
        {
            get { return Path.Combine(_localUpdatePath, _manifestFileName); }
        }

        public UpdaterVerifyFileHandler(string localUpdatePath, UpdaterFlagCheckHandler flagChecker = null,
            string patchListFileName = ".patch_list", string manifestFileName = ".manifest",
            string resourceVersionFileName = ".resource_version")
        {
            _localUpdatePath = localUpdatePath;
            _patchListFileName = patchListFileName;
            _manifestFileName = manifestFileName;
            _flagChecker = flagChecker;
            _resourceVersionFileName = resourceVersionFileName;
        }

        protected internal override void Start()
        {
            ThreadPool.QueueUserWorkItem(Threader);
        }

        private void Threader(object state)
        {
            var err = CheckAppValid();
            if (err != null)
            {
                // 非法文件阵列，清理
                ClearAllPatchResources();
            }

            var result = VerifyPatchResources();

            if (result && _flagChecker != null)
            {
                // success ,write version file
                var dirPath = Path.GetDirectoryName(_flagChecker.AppVersionFilePath);
                if (dirPath != null && !Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);
                File.WriteAllText(_flagChecker.AppVersionFilePath, _flagChecker.VersionString);
            }

            Finish();
        }

        /// <summary>
        ///     检查客户端有效性，文件是否齐全
        ///     0. .manifest, .resource_version或.patch_list不存在
        ///     1. 版本文件与apk内的版本号不一致
        ///     2. 验证不通过
        /// </summary>
        /// <returns></returns>
        private Exception CheckAppValid()
        {
            string notFoundFile = null;
            if (Directory.Exists(_localUpdatePath))
            {
                if (!File.Exists(ManifestFilePath)) // .manifest
                {
                    notFoundFile = ManifestFilePath;
                    goto Exit0;
                }
                if (!File.Exists(PatchListFilePath)) // .patch_list
                {
                    notFoundFile = PatchListFilePath;
                    goto Exit0;
                }
                if (!File.Exists(ResourceVersionFilePath))
                {
                    notFoundFile = ResourceVersionFilePath;
                    goto Exit0;
                }
            }

            return null;

            Exit0:
            return
                new Exception(
                    string.Format(
                        "Verify patch resources failed ! not found `{0}`, maybe cheat by user! Force delete all `patch_resources`",
                        notFoundFile));
        }

        /// <summary>
        ///     针对.patch_list和.manifest，对所有更新过的补丁资源，进行检验
        /// </summary>
        /// <returns></returns>
        private bool VerifyPatchResources()
        {
            PatchListFile patchList = null;
            List<string> patchListRemove = null; // 将被从patchList移除的字符串缓存

            if (Directory.Exists(_localUpdatePath) &&
                File.Exists(PatchListFilePath) && File.Exists(ManifestFilePath))
            {
                patchList = new PatchListFile(PatchListFilePath);
                patchListRemove = new List<string>();
                var manifestData = new ManifestFile(ManifestFilePath);

                //                var time = Time.time;
                foreach (var relativePath in patchList.Datas)
                {
                    var fileName = Path.GetFileName(relativePath);
                    if (fileName.StartsWith(".")) // 忽略.manifest, .resource_version
                        continue;

                    // 校验
                    var fullPath = Path.Combine(this._localUpdatePath, relativePath);
                    if (!manifestData.Datas.ContainsKey(relativePath) && File.Exists(fullPath))
                    {
                        // 多出来的文件，清理掉
                        AppendLog("Warn useless `{0}` in .manifest file", relativePath);
                        File.Delete(fullPath);
                        patchListRemove.Add(relativePath);
                        // patch_list存在，但不存在具体文件，证明之前资源包被删掉了
                    }
                    else
                    {
                        if (!File.Exists(fullPath))
                        {
                            OnError(this, "Error not found file `{0}`", fullPath);
                            goto Exit0;
                        }
                        var manifestMd5 = manifestData.Datas[relativePath].MD5.ToLower();
                        var realMd5 = Md5Helper.Md5File(fullPath).ToLower();

                        if (manifestMd5 != realMd5)
                        {
                            OnError(this, "Error when md5 verify, need: {0}, but: {1}", realMd5, manifestMd5);
                            goto Exit0;
                        }
                        //                        if (Time.time - time > 0.1f) // 帧速过慢了，回停一下
                        //                        {
                        //                            time = Time.time;
                        //                            yield return null;
                        //                        }
                    }
                }

                // 搜寻patch_resources目录,确保里面的文件都在.patch_List里!
                var patchResourcesDirPath = _localUpdatePath;
                foreach (var filePath in Directory.GetFiles(patchResourcesDirPath, "*", SearchOption.AllDirectories))
                {
                    var fileName = Path.GetFileName(filePath);
                    if (fileName.StartsWith(".")) continue; // 忽略隐藏文件

                    var cleanPath =
                        filePath.Substring(patchResourcesDirPath.Length + 1,
                            filePath.Length - patchResourcesDirPath.Length - 1).Replace("\\", "/"); // + - 1为了砍掉/
                    if (!patchList.Datas.Contains(cleanPath))
                    {
                        OnError(this, "patch_resources file: {0} not in .patch_list", filePath);
                        goto Exit0;
                    }
                }
            }
            // else 不用处理了， CheckAppValid会做检查

            CleanPatchList(patchList, patchListRemove);
            //            verifyResult[0] = true;
            //            yield break;
            return true;

            Exit0:
            // 验证失败，将清理下载资源。重新走更新流程，会重新下载资源
            ClearAllPatchResources();
            CleanPatchList(patchList, patchListRemove);
            //            verifyResult[0] = false;
            return false;
        }

        /// <summary>
        ///     patch_list列表有改动？ 进行移除并保存
        /// </summary>
        /// <param name="patchList"></param>
        /// <param name="patchListRemove"></param>
        private void CleanPatchList(PatchListFile patchList, List<string> patchListRemove)
        {
            if (patchList == null || patchListRemove == null) return;

            if (patchListRemove.Count > 0)
            {
                for (var i = 0; i < patchListRemove.Count; i++)
                {
                    patchList.Datas.Remove(patchListRemove[i]);
                }
                patchListRemove.Clear();
                patchList.Save();
            }
        }

        public static Action ClearAllUpdateFilesHook;

        public static bool ClearAllUpdateFiles(string localUpdatePath, UpdaterHandler handler = null)
        {
            //if (Directory.Exists(KEnv.PatchResourcesDirPath))
            //            {
            // 可以断定是重新安装了客户端，删掉patch_resources
            //                var waitDelete = true;
            //                string deleteDirError = null;
            //                ThreadPool.QueueUserWorkItem((_) =>
            //                {
            try
            {
                if (Directory.Exists(localUpdatePath))
                    Directory.Delete(localUpdatePath, true);

                if (ClearAllUpdateFilesHook != null)
                    ClearAllUpdateFilesHook();
            }
            catch (Exception e)
            {
                OnError(handler, e.Message);
                return false;
            }
            //                    finally
            //                    {
            //                        waitDelete = false;
            //                    }
            //                });
            //                while (waitDelete)
            //                    yield return null;
            //                if (!string.IsNullOrEmpty(deleteDirError))
            //                {
            //                    KLog.Err(deleteDirError);
            //                }
            // 清理过后，进行刷新热更新脚本列表
            //                Reload();
            //            }
            return true;
        }

        /// <summary>
        ///     清理所有的更新资源脚本，采用线程池确保性能够快
        /// </summary>
        /// <returns></returns>
        public bool ClearAllPatchResources()
        {
            return ClearAllUpdateFiles(_localUpdatePath, this);
        }
    }
}