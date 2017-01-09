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

namespace KKUpdater.PackageUpdater
{
    public class DeletedListFile : StringListFile
    {
        public static CustomReader PatchListCustomReader;
        public static CustomWriter PatchListCustomWriter;

        public DeletedListFile(string path)
            : base(path, PatchListCustomReader, PatchListCustomWriter)
        {
        }
    }
}