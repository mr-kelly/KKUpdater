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
using System.Linq;
using System.Text;

namespace KKUpdater.PackageUpdater
{
    /// <summary>
    ///     patch_resources目录有.patch_list，记录所有更新过的文件，用于比较
    /// </summary>
    public class StringListFile
    {
        private HashSet<string> m_List = new HashSet<string>();
        private readonly string _path;

        /// <summary>
        ///     Filter the bytes
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public delegate byte[] CustomWriter(byte[] bytes);

        private CustomWriter _customWriter;

        /// <summary>
        ///     Custom read the strings
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public delegate TextReader CustomReader(string path);

        private CustomReader _customReader;

        /// <summary>
        /// </summary>
        /// <param name="path"></param>
        /// <param name="customReader">custom stream reader</param>
        /// <param name="customWriter"></param>
        public StringListFile(string path, CustomReader customReader = null, CustomWriter customWriter = null)
        {
            _path = path;
            _customReader = customReader;
            _customWriter = customWriter;

            if (File.Exists(path))
            {
                TextReader reader;
                if (_customReader != null)
                {
                    reader = _customReader(path);
                }
                else
                {
                    reader = new StreamReader(path);
                }

                using (reader)
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        m_List.Add(line);
                    }
                }
            }
        }

        public HashSet<string> Datas
        {
            get { return m_List; }
        }

        public bool Add(string filePath)
        {
            return m_List.Add(filePath);
        }

        public void Save()
        {
            var content = string.Join("\n", m_List.ToArray());
            var bytes = Encoding.UTF8.GetBytes(content);
            if (_customWriter != null)
                bytes = _customWriter(bytes);

            File.WriteAllBytes(_path, bytes);
        }
    }
}