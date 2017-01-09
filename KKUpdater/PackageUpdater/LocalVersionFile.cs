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

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    /// </summary>
    public class LocalVersionFile
    {
        /// <summary>
        ///     Custom read local version method's delegate
        /// </summary>
        /// <param name="versionFilePath"></param>
        /// <returns></returns>
        public delegate int CustomReadLocalVersionDelegate(string versionFilePath);

        /// <summary>
        ///     Custom read local version method
        /// </summary>
        /// <param name="versionFilePath"></param>
        /// <returns></returns>
        public static CustomReadLocalVersionDelegate CustomReadLocalVersion;

        public delegate byte[] CustomWriteResVersionDelegate(int versionNumber);

        /// <summary>
        ///     Custom the write behaivour of
        /// </summary>
        public static CustomWriteResVersionDelegate CustomWriteResVersion;

        /// <summary>
        ///     Write the version file to update path
        /// </summary>
        /// <param name="localUpdaterPath"></param>
        /// <param name="versionFileName"></param>
        /// <param name="version"></param>
        public static void Write(string localUpdaterPath, string versionFileName, int version)
        {
            // 记录资源版本号
            byte[] writeBytes;
            if (CustomWriteResVersion != null)
            {
                writeBytes = CustomWriteResVersion(version);
            }
            else
            {
                // 默认存字符串UTF-8
                var verString = version.ToString();
                writeBytes = Encoding.UTF8.GetBytes(verString);
            }

            var resVersionPath = Path.Combine(localUpdaterPath, versionFileName);
            File.WriteAllBytes(resVersionPath, writeBytes);
        }

        /// <summary>
        /// </summary>
        /// <param name="localUpdaterPath"></param>
        /// <param name="versionFileName"></param>
        /// <param name="defaultVersion"></param>
        /// <returns></returns>
        public static int GetLocalVersion(string localUpdaterPath, string versionFileName, int? defaultVersion = null)
        {
            var versionFile = Path.Combine(localUpdaterPath, versionFileName);
            if (File.Exists(versionFile))
            {
                int version;
                if (CustomReadLocalVersion != null)
                {
                    version = CustomReadLocalVersion(versionFile);
                }
                else
                {
                    // default, read all
                    var vStr = File.ReadAllText(versionFile).Trim();
                    if (!Int32.TryParse(vStr, out version))
                    {
                        throw new Exception(String.Format("[GetLocalVersion]Failed to parse version file : {0}",
                            versionFile));
                    }
                }

                return version;
            }
            else
            {
                if (defaultVersion != null)
                {
                    return defaultVersion.Value;
                }
                else
                {
                    throw new Exception(String.Format("[GetLocalVersion]Not exist version file : {0}", versionFile));
                }
            }
        }
    }
}