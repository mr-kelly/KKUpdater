#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

using System.Collections.Generic;
using System.IO;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     资源包中的.manifest文件，资源Manifest读表
    /// </summary>
    public class ManifestFile
    {
        public class Manifest
        {
            public string File;
            public string MD5;
            public ulong Size;
        }

        public Dictionary<string, Manifest> Datas;

        public delegate TextReader CustomReaderDelegate(string path);

        public static CustomReaderDelegate CustomReader;

        public ManifestFile(string path)
        {
            Datas = new Dictionary<string, Manifest>();

            TextReader reader;
            if (File.Exists(path))
            {
                if (CustomReader != null)
                {
                    reader = CustomReader(path);
                }
                else
                {
                    reader = new StreamReader(path);
                }

                using (reader)
                {
                    int lineCount = -1;

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineCount++;
                        if (lineCount == 0) continue; // ignore header

                        ProcessLine(line);
                    }
                }
            }
        }

        /// <summary>
        ///     process line string
        /// </summary>
        /// <param name="line"></param>
        private void ProcessLine(string line)
        {
            var itms = line.Split('\t');

            var mani = new Manifest();
            mani.File = itms[0];
            mani.MD5 = itms[1];
            ulong.TryParse(itms[2], out mani.Size);

            Datas.Add(mani.File, mani);
        }
    }
}