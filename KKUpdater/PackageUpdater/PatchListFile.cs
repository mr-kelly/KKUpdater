#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

namespace KKUpdater.PackageUpdater
{
    public class PatchListFile : StringListFile
    {
        public static CustomReader PatchListCustomReader;
        public static CustomWriter PatchListCustomWriter;

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        public PatchListFile(string path)
            : base(path, PatchListCustomReader, PatchListCustomWriter)
        {
        }
    }
}