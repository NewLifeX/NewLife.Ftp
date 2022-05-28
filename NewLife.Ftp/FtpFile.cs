using System.Text.RegularExpressions;

namespace NewLife.Ftp;

/// <summary>
/// Ftp文件
/// </summary>
public class FtpFile
{
    #region 属性
    /// <summary>路径</summary>
    public String Path { get; private set; }

    /// <summary>文件名</summary>
    public String FileName { get; private set; }

    /// <summary>全名</summary>
    public String FullName
    {
        get
        {
            var str = Path;
            if (!String.IsNullOrEmpty(str) && !str.EndsWith("/")) str += "/";
            return str + FileName;
        }
    }

    /// <summary>是否目录</summary>
    public Boolean IsDirectory { get; private set; }

    /// <summary>大小</summary>
    public Int64 Size { get; private set; }

    /// <summary>权限</summary>
    public String Permission { get; private set; }

    /// <summary>时间</summary>
    public DateTime FileDateTime { get; private set; }

    /// <summary>Ftp客户端</summary>
    public FtpClient Client => Parent?.Client;

    /// <summary>父目录</summary>
    public FtpDirectory Parent { get; set; }
    #endregion

    #region 构造函数
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="m"></param>
    /// <param name="path"></param>
    private FtpFile(Match m, String path)
    {
        //Match m = GetMatchingRegex(line);
        //if (m == null) throw new Exception("不能分析行：" + line);

        Path = path;
        FileName = m.Groups["name"].Value;
        Permission = m.Groups["permission"].Value;

        if (!Int64.TryParse(m.Groups["size"].Value, out var size)) size = 0;
        Size = size;

        var _dir = m.Groups["dir"].Value;
        IsDirectory = !String.IsNullOrEmpty(_dir) & _dir != "-";

        var d = DateTime.MinValue;
        if (!DateTime.TryParse(m.Groups["timestamp"].Value, out d))
        {
            var str = m.Groups["timestamp"].Value;
            if (str.Length > 6)
            {
                str = str.Insert(6, "20");
                if (!DateTime.TryParse(str, out d)) d = DateTime.MinValue;
            }
            else
                d = DateTime.MinValue;
        }
        FileDateTime = d;
    }

    /// <summary>
    /// 建立对象
    /// </summary>
    /// <param name="line"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public static FtpFile Create(String line, String path)
    {
        var m = GetMatchingRegex(line);
        //if (m == null) throw new Exception("不能分析行：" + line);
        if (m == null) return null;

        var ftp = new FtpFile(m, path);

        return ftp;
    }
    #endregion

    #region 方法
    private static readonly String[] _ParseFormats = new String[] {
        "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)",
        "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{4})\\s+(?<name>.+)",
        "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\d+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)",
        "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})\\s+\\d+\\s+\\w+\\s+\\w+\\s+(?<size>\\d+)\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{1,2}:\\d{2})\\s+(?<name>.+)",
        "(?<dir>[\\-d])(?<permission>([\\-r][\\-w][\\-xs]){3})(\\s+)(?<size>(\\d+))(\\s+)(?<ctbit>(\\w+\\s\\w+))(\\s+)(?<size2>(\\d+))\\s+(?<timestamp>\\w+\\s+\\d+\\s+\\d{2}:\\d{2})\\s+(?<name>.+)",
        "(?<timestamp>\\d{2}\\-\\d{2}\\-\\d{2}\\s+\\d{2}:\\d{2}[Aa|Pp][mM])\\s+(?<dir>\\<\\w+\\>){0,1}(?<size>\\d+){0,1}\\s+(?<name>.+)" };

    private static Match GetMatchingRegex(String line)
    {
        Regex rx;
        Match m;
        for (var i = 0; i <= _ParseFormats.Length - 1; i++)
        {
            rx = new Regex(_ParseFormats[i], RegexOptions.Compiled);
            m = rx.Match(line);
            if (m.Success)
            {
                return m;
            }
        }
        return null;
    }

    /// <summary>
    /// 已重载。
    /// </summary>
    /// <returns></returns>
    public override String ToString() => FileName;
    #endregion

    #region 操作
    /// <summary>
    /// 下载该文件
    /// </summary>
    /// <param name="localfile"></param>
    /// <param name="mode"></param>
    public void Download(String localfile, FtpTransportMode mode) => Client.DownloadFile(FullName, localfile, mode);

    /// <summary>
    /// 删除
    /// </summary>
    public void Delete()
    {
        Client.DeleteFile(FullName);

        if (Parent != null)
        {
            if (Parent.Files.ContainsKey(FileName)) Parent.Files.Remove(FileName);
        }
    }
    #endregion
}