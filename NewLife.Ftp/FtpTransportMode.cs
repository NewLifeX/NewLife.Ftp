namespace NewLife.Ftp;

/// <summary>
/// Ftp传输模式
/// </summary>
public enum FtpTransportMode
{
    /// <summary>
    /// 默认
    /// </summary>
    None = 0,

    /// <summary>
    /// 覆盖
    /// </summary>
    OverWrite = 1,

    /// <summary>
    /// 断点续传
    /// </summary>
    Append = 2
}