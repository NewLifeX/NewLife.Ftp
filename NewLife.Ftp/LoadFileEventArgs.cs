using System.ComponentModel;

namespace NewLife.Ftp;

/// <summary>
/// 上传/下载文件事件参数
/// </summary>
public class LoadFileEventArgs : CancelEventArgs
{
    /// <summary>Ftp文件</summary>
    public String Src { get; set; }

    /// <summary>源文件大小</summary>
    public Int64 SrcSize { get; set; }

    /// <summary>目标文件</summary>
    public String Des { get; set; }

    /// <summary>目标文件大小</summary>
    public Int64 DesSize { get; set; }
}