using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace NewLife.Ftp
{
    /// <summary>
    /// Ftp客户端
    /// </summary>
    public class FtpClient
    {
        #region 属性
        /// <summary>主机名</summary>
        public String Hostname { get; set; }

        /// <summary>端口</summary>
        public Int32 Port { get; set; }

        /// <summary>验证</summary>
        public ICredentials Credentials { get; set; }

        /// <summary>使用默认认证</summary>
        public Boolean UseDefaultCredentials
        {
            get { return Credentials is NetworkCredential; }
            set { Credentials = value ? CredentialCache.DefaultCredentials : null; }
        }

        /// <summary>保持连接</summary>
        public Boolean KeepAlive { get; set; }

        /// <summary>获取或设置一个 Boolean 值，该值指定文件传输的数据类型。</summary>
        public Boolean UseBinary { get; set; }

        /// <summary>获取或设置客户端应用程序的数据传输过程的行为。（默认：true）</summary>
        public Boolean UsePassive { get; set; } = true;

        /// <summary>本地请求对象</summary>
        public FtpWebRequest LocalRequest { get; set; }

        private FtpDirectory _Root;
        /// <summary>根</summary>
        public FtpDirectory Root
        {
            get
            {
                if (_Root == null) _Root = ListDirectoryDetails(null);
                return _Root;
            }
            set { _Root = value; }
        }

        /// <summary>文件</summary>
        public Dictionary<String, FtpFile> Files { get; set; } = new Dictionary<String, FtpFile>();

        /// <summary>目录</summary>
        public Dictionary<String, FtpDirectory> Directories { get; set; } = new Dictionary<String, FtpDirectory>();

        /// <summary>用户数据</summary>
        public Object UserState { get; set; }
        #endregion

        #region 事件
        /// <summary>
        /// 下载文件事件
        /// </summary>
        public event EventHandler<LoadFileEventArgs> OnDownloadFile;

        /// <summary>
        /// 下载文件完成事件
        /// </summary>
        public event EventHandler<LoadFileEventArgs> OnDownloadFileFinished;

        /// <summary>
        /// 上传文件事件
        /// </summary>
        public event EventHandler<LoadFileEventArgs> OnUploadFile;

        /// <summary>
        /// 上传文件事件完成
        /// </summary>
        public event EventHandler<LoadFileEventArgs> OnUploadFileFinished;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造一个Ftp客户端实例
        /// </summary>
        public FtpClient() { }

        /// <summary>
        /// 构造一个Ftp客户端实例
        /// </summary>
        /// <param name="hostname"></param>
        public FtpClient(String hostname) : this(hostname, null, null) { }

        /// <summary>
        /// 构造一个Ftp客户端实例
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        public FtpClient(String hostname, String user, String pass) : this(hostname, user, pass, null) { }

        /// <summary>
        /// 构造一个Ftp客户端实例
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <param name="domain"></param>
        public FtpClient(String hostname, String user, String pass, String domain)
        {
            Hostname = hostname;
            if (!String.IsNullOrEmpty(user))
            {
                if (String.IsNullOrEmpty(domain))
                    Credentials = new NetworkCredential(user, pass);
                else
                    Credentials = new NetworkCredential(user, pass, domain);
            }
        }
        #endregion

        #region 列表
        /// <summary>
        /// 列出目录
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public List<String> ListDirectory(String directory)
        {
            if (String.IsNullOrEmpty(directory)) directory = "/";

            var request = GetFtpRequest(directory);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            var str = GetStringResponse(request);
            str = str.Replace("\r\n", "\r").TrimEnd('\r');
            var result = new List<String>();
            result.AddRange(str.Split('\r'));
            return result;
        }

        /// <summary>
        /// 列出目录明细
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public FtpDirectory ListDirectoryDetails(String directory)
        {
            return new FtpDirectory(this, ListDirectoryDetailsString(directory), directory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public String ListDirectoryDetailsString(String directory)
        {
            if (String.IsNullOrEmpty(directory)) directory = "/";

            var request = GetFtpRequest(directory);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

            var str = GetStringResponse(request);
            str = str.Replace("\r\n", "\r").TrimEnd('\r');
            return str;
        }
        #endregion

        #region 上传
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="localfile">本地文件</param>
        /// <param name="remotefile">远程文件</param>
        /// <param name="mode">模式</param>
        /// <returns></returns>
        public Int64 UploadFile(String localfile, String remotefile, FtpTransportMode mode)
        {
            #region 准备工作
            if (String.IsNullOrEmpty(localfile)) throw new ArgumentNullException("localfile", "没有指定本地文件名。");
            if (String.IsNullOrEmpty(remotefile)) throw new ArgumentNullException("remotefile", "没有指定远程文件名。");

            if (localfile[1] != Path.VolumeSeparatorChar)
                localfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localfile);

            var fi = new FileInfo(localfile);
            if (!fi.Exists) throw new Exception("本地文件不存在。");

            //if (String.IsNullOrEmpty(remotefile)) remotefile = fi.Name;

            var e = new LoadFileEventArgs();
            if (OnUploadFile != null)
            {
                e.Cancel = false;
                e.Src = localfile;
                e.SrcSize = fi.Length;
                e.Des = remotefile;

                OnUploadFile(this, e);

                if (e.Cancel) return 0;
            }
            #endregion

            try
            {
                #region 上传
                var ftp = GetFtpRequest(remotefile);
                ftp.Method = WebRequestMethods.Ftp.UploadFile;
                ftp.UseBinary = true;

                var buffer = new Byte[2048];
                Int64 size = 0;
                using (var fs = fi.OpenRead())
                {
                    try
                    {

                        //如果采用断点续传，则从断点处开始
                        if (mode == FtpTransportMode.Append)
                        {
                            //取远程文件大小
                            var size2 = GetFileSize(remotefile);
                            if (fs.Length <= size2) return size2;

                            //设置断点
                            ftp.ContentOffset = size2;
                            fs.Position = size2;
                        }

                        using (var rs = ftp.GetRequestStream())
                        {
                            Int32 dataRead;
                            do
                            {
                                dataRead = fs.Read(buffer, 0, buffer.Length);
                                size += dataRead;
                                rs.Write(buffer, 0, dataRead);
                            } while (dataRead >= buffer.Length);
                            rs.Close();
                        }
                    }
                    finally
                    {
                        fs.Close();
                    }
                }
                e.DesSize = size;
                return size;
                #endregion
            }
            finally
            {
                OnUploadFileFinished?.Invoke(this, e);
            }
        }

        /// <summary>
        /// 上传目录
        /// </summary>
        /// <param name="localpath"></param>
        /// <param name="dir"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Int64 UploadDirectory(String localpath, FtpDirectory dir, FtpTransportMode mode)
        {
            if (String.IsNullOrEmpty(localpath)) throw new ArgumentNullException("localpath");
            if (dir == null) throw new ArgumentNullException("dir");

            if (localpath[1] != Path.VolumeSeparatorChar)
                localpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localpath);

            var di = new DirectoryInfo(localpath);
            Int64 size = 0;

            //遍历文件
            var files = di.GetFiles();
            if (files != null && files.Length > 0)
            {
                foreach (var item in files)
                {
                    //检查远端文件是否存在
                    if (dir.Contains(item.Name))
                    {
                        if (mode == FtpTransportMode.None) continue;

                        //删除
                        if (mode == FtpTransportMode.OverWrite) dir.Files[item.Name].Delete();
                    }

                    //上传文件
                    //UploadFile(item.FullName, f.FullName);
                    size += dir.UploadFile(item.FullName, mode);
                }
            }

            //遍历目录
            var dis = di.GetDirectories();
            if (dis != null && dis.Length > 0)
            {
                foreach (var item in dis)
                {
                    //检查远端文件夹是否存在
                    if (!dir.Contains(item.Name)) dir.CreateDirectory(item.Name);

                    //递归
                    size += UploadDirectory(item.FullName, dir.Directories[item.Name], mode);
                }
            }
            return size;
        }
        #endregion

        #region 下载
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="remotefile">远程文件</param>
        /// <param name="localfile">本地文件</param>
        /// <param name="mode">端点续传模式</param>
        /// <returns></returns>
        public Int64 DownloadFile(String remotefile, String localfile, FtpTransportMode mode)
        {
            #region 准备工作
            if (String.IsNullOrEmpty(remotefile)) return 0;
            if (String.IsNullOrEmpty(localfile))
            {
                var str = remotefile;
                if (str.Contains("/")) str = str.Substring(str.LastIndexOf("/") + 1);
                localfile = str;
            }
            if (localfile[1] != Path.VolumeSeparatorChar)
                localfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localfile);

            if (File.Exists(localfile))
            {
                if (mode == FtpTransportMode.OverWrite)
                    File.Delete(localfile);
                else if (mode == FtpTransportMode.None)
                    return 0;
                //throw new Exception("目标文件已存在！");
            }
            #endregion

            var e = new LoadFileEventArgs();
            if (OnDownloadFile != null)
            {
                e.Cancel = false;
                e.Src = remotefile;
                e.Des = localfile;
                var fi = new FileInfo(localfile);
                if (fi != null && fi.Exists) e.DesSize = fi.Length;

                OnDownloadFile(this, e);

                if (e.Cancel) return 0;
            }

            try
            {
                #region 下载
                var ftp = GetFtpRequest(remotefile);
                ftp.Method = WebRequestMethods.Ftp.DownloadFile;
                ftp.UseBinary = true;

                if (!Directory.Exists(Path.GetDirectoryName(localfile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(localfile));
                var fs = new FileStream(localfile, FileMode.OpenOrCreate, FileAccess.Write);
                //如果采用断点续传，则从断点处开始
                if (mode == FtpTransportMode.Append)
                {
                    //取远程文件大小
                    var size = GetFileSize(remotefile);
                    if (fs.Length >= size) return 0;

                    //设置断点
                    ftp.ContentOffset = fs.Length;
                }

                var buffer = new Byte[2048];
                Int64 count = 0;
                using (var response = GetResponse(ftp))
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        try
                        {
                            var read = 0;
                            do
                            {
                                read = responseStream.Read(buffer, 0, buffer.Length);
                                count += read;
                                fs.Write(buffer, 0, read);
                            } while (read != 0);
                        }
                        finally
                        {
                            fs.Close();
                            responseStream.Close();
                        }
                    }

                    response.Close();
                }
                e.SrcSize = count;
                return count;
                #endregion
            }
            finally
            {
                if (OnDownloadFileFinished != null)
                {
                    var fi = new FileInfo(localfile);
                    if (fi != null && fi.Exists) e.DesSize = fi.Length;

                    OnDownloadFileFinished(this, e);
                }
            }
        }

        /// <summary>
        /// 下载目录
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="localpath"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        public Int64 DownloadDirectory(FtpDirectory dir, String localpath, FtpTransportMode mode)
        {
            if (dir == null) throw new ArgumentNullException("dir");
            if (dir.Count < 1) return 0;
            if (String.IsNullOrEmpty(localpath)) throw new ArgumentNullException("localpath");

            if (localpath[1] != Path.VolumeSeparatorChar)
                localpath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, localpath);

            Int64 count = 0;
            //遍历
            foreach (var item in dir)
            {
                if (item.IsDirectory)
                {
                    //递归
                    var d = ListDirectoryDetails(item.FullName);
                    count += DownloadDirectory(d, Path.Combine(localpath, item.FileName), mode);
                }
                else
                {
                    //下载文件
                    count += DownloadFile(item.FullName, Path.Combine(localpath, item.FileName), mode);
                }
            }
            return count;
        }
        #endregion

        #region 删除
        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="file"></param>
        public void DeleteFile(String file)
        {
            if (String.IsNullOrEmpty(file)) throw new ArgumentNullException("file");

            var ftp = GetFtpRequest(file);
            ftp.Method = WebRequestMethods.Ftp.DeleteFile;

            GetStringResponse(ftp);
        }
        #endregion

        #region 取文件大小
        /// <summary>
        /// 取文件大小
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Int64 GetFileSize(String file)
        {
            if (String.IsNullOrEmpty(file)) throw new ArgumentNullException("file");

            var ftp = GetFtpRequest(file);
            ftp.Method = WebRequestMethods.Ftp.GetFileSize;

            try
            {
                var response = GetResponse(ftp);
                var size = response.ContentLength;
                response.Close();
                return size;
            }
            catch { return 0; }
        }
        #endregion

        #region 重命名文件
        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldfile"></param>
        /// <param name="newfile"></param>
        public void Rename(String oldfile, String newfile)
        {
            if (String.IsNullOrEmpty(oldfile)) throw new ArgumentNullException("oldfile");
            if (String.IsNullOrEmpty(newfile)) throw new ArgumentNullException("newfile");

            if (!newfile.StartsWith("/")) newfile = "/" + newfile;

            var ftp = GetFtpRequest(oldfile);
            ftp.Method = WebRequestMethods.Ftp.Rename;
            ftp.RenameTo = newfile;

            GetStringResponse(ftp);
        }
        #endregion

        #region 建立目录
        /// <summary>
        /// 建立目录
        /// </summary>
        /// <param name="path"></param>
        public void MakeDirectory(String path)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            if (!path.EndsWith("/")) path += "/";

            var ftp = GetFtpRequest(path);
            ftp.Method = WebRequestMethods.Ftp.MakeDirectory;

            GetStringResponse(ftp);

            //Root = null;
        }
        #endregion

        #region 删除目录
        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="path"></param>
        public void RemoveDirectory(String path)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            var ftp = GetFtpRequest(path);
            ftp.Method = WebRequestMethods.Ftp.RemoveDirectory;

            GetStringResponse(ftp);

            //Root = null;
        }
        #endregion

        #region 基础功能
        /// <summary>
        /// 取得Ftp请求对象
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private FtpWebRequest GetFtpRequest(String path)
        {
            return GetFtpRequest(new Uri(AdjustDir(path)));
        }

        /// <summary>
        /// 取得Ftp请求对象
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        protected virtual FtpWebRequest GetFtpRequest(Uri address)
        {
            if (KeepAlive && LocalRequest != null) return LocalRequest;

            var request = (FtpWebRequest)FtpWebRequest.Create(address);
            request.KeepAlive = KeepAlive;
            if (Credentials != null) request.Credentials = Credentials;
            request.UseBinary = UseBinary;
            request.UsePassive = UsePassive;

            if (KeepAlive) LocalRequest = request;

            return request;
        }

        /// <summary>
        /// 取响应
        /// </summary>
        /// <param name="ftp"></param>
        /// <returns></returns>
        protected virtual FtpWebResponse GetResponse(FtpWebRequest ftp)
        {
            try
            {
                var response = (FtpWebResponse)ftp.GetResponse();
                return response;
            }
            catch (WebException ex)
            {
                if (ex.Response is FtpWebResponse) throw new FtpWebResponseException(ex);
                throw;
            }
        }

        /// <summary>
        /// 取得响应字符串
        /// </summary>
        /// <param name="ftp"></param>
        /// <returns></returns>
        protected virtual String GetStringResponse(FtpWebRequest ftp)
        {
            var result = "";
            using (var response = GetResponse(ftp))
            {
                var size = response.ContentLength;
                using (var datastream = response.GetResponseStream())
                {
                    using (var sr = new StreamReader(datastream, Encoding.Default))
                    {
                        result = sr.ReadToEnd();
                        sr.Close();
                    }

                    datastream.Close();
                }

                response.Close();
            }

            return result;
        }

        /// <summary>
        /// 取得响应大小
        /// </summary>
        /// <param name="ftp"></param>
        /// <returns></returns>
        private Int64 GetSize(FtpWebRequest ftp)
        {
            Int64 size;
            using (var response = GetResponse(ftp))
            {
                size = response.ContentLength;
                response.Close();
            }

            return size;
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 调整目录
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private String AdjustDir(String path)
        {
            if (String.IsNullOrEmpty(path)) path = "/";
            if (!path.StartsWith("/")) path = "/" + path;
            return "ftp://" + Hostname + path;
        }
        #endregion
    }
}