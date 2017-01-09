#region https://github.com/mr-kelly

// KKUpdater - Robust Resources Package Downloader
// 
// A private module of KSFramework<https://github.com/mr-kelly/KSFramework>
//  
// Author: chenpeilin / mr-kelly
// Email: 23110388@qq.com
// Website: https://github.com/mr-kelly

#endregion

using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace KKUpdater
{
    /// <summary>
    /// </summary>
    public static class Md5Helper
    {
        private static readonly MD5 Md5Hash = MD5.Create();

        /// <summary>
        ///     Get salt string bytes
        /// </summary>
        /// <param name="salt"></param>
        /// <returns></returns>
        private static byte[] GetSaltBytes(string salt)
        {
            if (string.IsNullOrEmpty(salt))
                return null;
            return Encoding.UTF8.GetBytes(salt);
        }

        /// <summary>
        ///     get md5 from string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Md5String(string str, string salt = null)
        {
            using (var stream = new MemoryStream())
            {
                var strBytes = Encoding.UTF8.GetBytes(str);
                stream.Write(strBytes, 0, strBytes.Length);

                var saltBytes = GetSaltBytes(salt);
                if (saltBytes != null)
                    stream.Write(saltBytes, 0, salt.Length);

                stream.Position = 0;
                byte[] hash = Md5Hash.ComputeHash(stream);
                return _ToHexDigest(hash);
            }
        }

        /// <summary>
        ///     get md5 of a file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string Md5File(string fileName, string salt = null)
        {
            using (var fs = System.IO.File.OpenRead(fileName))
            {
                var saltBytes = GetSaltBytes(salt);
                if (saltBytes != null)
                {
                    using (var saltStream = new MemoryStream(saltBytes))
                    {
                        saltStream.Position = 0;
                        using (var mergedStream = new MergedStream(fs, saltStream))
                        {
                            return _ToHexDigest(Md5Hash.ComputeHash(mergedStream));
                        }
                    }
                }
                else
                {
                    return _ToHexDigest(Md5Hash.ComputeHash(fs));
                }
            }
        }

        private static string _ToHexDigest(byte[] hash)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));

            return sb.ToString();
        }
    }
}