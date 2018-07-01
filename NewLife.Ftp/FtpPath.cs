using System;

namespace NewLife.Ftp
{
    /// <summary>
    /// Ftp路径
    /// </summary>
    public static class FtpPath
    {
        /// <summary>
        /// 合并路径
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static String Combine(String path1, String path2)
        {
            if (String.IsNullOrEmpty(path1))
                path1 = "/";
            else if (!path1.StartsWith("/"))
                path1 = "/" + path1;

            if (String.IsNullOrEmpty(path2)) return path1;

            if (path2.StartsWith("/")) path2 = path2.Substring(1);

            if (String.IsNullOrEmpty(path2)) return path1;

            if (path1.EndsWith("/"))
                return path1 + path2;
            else
                return path1 + "/" + path2;
        }
    }
}
