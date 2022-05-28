namespace NewLife.Ftp;

/// <summary>
/// Ftp目录
/// </summary>
public class FtpDirectory : List<FtpFile>
{
    #region 构造函数
    ///// <summary>
    ///// 
    ///// </summary>
    //public FtpDirectory() { }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="client"></param>
    /// <param name="line"></param>
    /// <param name="path"></param>
    public FtpDirectory(FtpClient client, String line, String path)
    {
        if (String.IsNullOrEmpty(path)) path = "/";

        Path = path;
        Client = client;

        Load(line);

        if (client != null) client.Directories[path] = this;
    }

    private void Load(String line)
    {
        if (String.IsNullOrEmpty(line)) return;
        line = line.Replace("\n", null);

        var lines = line.Split(new Char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
        Clear();
        foreach (var item in lines)
        {
            //FtpFile f = new FtpFile(item, Path);
            var f = FtpFile.Create(item, Path);
            if (f == null) continue;

            f.Parent = this;

            if (!f.IsDirectory && Client != null) Client.Files[FtpPath.Combine(Path, f.FileName)] = f;

            Add(f);
        }
    }

    private void Reload()
    {
        Files = null;
        Directories = null;
        Load(Client.ListDirectoryDetailsString(Path));
    }

    /// <summary>
    /// 已重载。
    /// </summary>
    /// <returns></returns>
    public override String ToString() => Path;

    /// <summary>路径</summary>
    public String Path { get; set; }

    /// <summary>Ftp客户端</summary>
    public FtpClient Client { get; set; }
    #endregion

    #region 扩展属性
    private readonly List<String> hasLoad = new List<String>();

    private Dictionary<String, FtpFile> _Files;
    /// <summary>该目录下的文件</summary>
    public Dictionary<String, FtpFile> Files
    {
        get
        {
            if (!hasLoad.Contains("Files"))
            {
                _Files = new Dictionary<String, FtpFile>();
                foreach (var item in this)
                {
                    if (!item.IsDirectory) _Files.Add(item.FileName, item);
                }

                hasLoad.Add("Files");
            }
            return _Files;
        }
        set
        {
            _Files = value;
            if (value == null && hasLoad.Contains("Files")) hasLoad.Remove("Files");
        }
    }

    private Dictionary<String, FtpDirectory> _Directories;
    /// <summary>该目录下的子目录</summary>
    public Dictionary<String, FtpDirectory> Directories
    {
        get
        {
            if (!hasLoad.Contains("Directories"))
            {
                _Directories = new Dictionary<String, FtpDirectory>();
                foreach (var item in this)
                {
                    if (item.IsDirectory)
                    {
                        var d = Client.ListDirectoryDetails(item.FullName);
                        d.Parent = this;
                        _Directories.Add(item.FileName, d);
                    }
                }

                hasLoad.Add("Directories");
            }
            return _Directories;
        }
        set
        {
            _Directories = value;
            if (value == null && hasLoad.Contains("Directories")) hasLoad.Remove("Directories");
        }
    }

    /// <summary>父目录</summary>
    public FtpDirectory Parent { get; set; }
    #endregion

    #region 方法
    /// <summary>
    /// 是否包含指定的文件或者目录
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Boolean Contains(String name) => Exists(delegate (FtpFile item) { return String.Equals(item.FileName, name, StringComparison.OrdinalIgnoreCase); });

    /// <summary>
    /// 建立子目录
    /// </summary>
    /// <param name="name"></param>
    public void CreateDirectory(String name)
    {
        if (Contains(name)) throw new Exception("目录[" + name + "]已存在！");

        Client.MakeDirectory(FtpPath.Combine(Path, name));

        ////清缓存
        //Directories = null;
        //Load(Client.ListDirectoryDetailsString(Path));

        var d = Client.ListDirectoryDetails(FtpPath.Combine(Path, name));
        d.Parent = this;
        Directories.Add(name, d);
    }

    /// <summary>
    /// 上传
    /// </summary>
    /// <param name="localfile"></param>
    /// <param name="remotefile"></param>
    /// <param name="mode">模式</param>
    /// <returns></returns>
    public Int64 UploadFile(String localfile, String remotefile, FtpTransportMode mode)
    {
        if (String.IsNullOrEmpty(localfile)) throw new ArgumentNullException("localfile", "没有指定本地文件名。");

        var fi = new FileInfo(localfile);
        if (!fi.Exists) throw new Exception("本地文件不存在。");

        if (String.IsNullOrEmpty(remotefile)) remotefile = fi.Name;

        var size = Client.UploadFile(localfile, FtpPath.Combine(Path, remotefile), mode);

        Reload();

        return size;
    }

    /// <summary>
    /// 上传
    /// </summary>
    /// <param name="localfile"></param>
    /// <param name="mode">模式</param>
    /// <returns></returns>
    public Int64 UploadFile(String localfile, FtpTransportMode mode) => UploadFile(localfile, null, mode);
    #endregion
}