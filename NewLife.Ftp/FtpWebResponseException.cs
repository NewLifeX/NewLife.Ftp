using System.Net;

namespace NewLife.Ftp;

/// <summary>
/// Ftp响应异常
/// </summary>
public class FtpWebResponseException : WebException
{
    /// <summary>响应</summary>
    public new FtpWebResponse Response { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ex"></param>
    public FtpWebResponseException(WebException ex)
        : base((ex.Response is FtpWebResponse) ? (ex.Response as FtpWebResponse).StatusDescription : null, ex)
    {
        if (ex != null)
        {
            Response = ex.Response as FtpWebResponse;
        }
    }
}