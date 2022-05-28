using System;
using System.IO;
using System.Net;
using NewLife.Ftp;

namespace Test
{
    class Program
    {
        static void Main(String[] args)
        {
            Console.WriteLine("Hello World!");

            try
            {
                foo();
            }
            catch (Exception ex)
            {
            }

            Console.WriteLine("ok");
            Console.ReadLine();
        }

        static void foo()
        {

            //FileInfo fileInf = new FileInfo("D:\\123.txt");
            //string uri = "ftp://101.226.244.93:8221/" + fileInf.Name;
            //FtpWebRequest reqFTP;

            //reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://101.226.244.93:8221/" + fileInf.Name));// 根据uri创建FtpWebRequest对象
            //reqFTP.Credentials = new NetworkCredential("company7003", "LKXLNC9S3DCS");    // ftp用户名和密码
            //reqFTP.KeepAlive = false;    // 默认为true，连接不会被关闭， 在一个命令之后被执行
            //reqFTP.Method = WebRequestMethods.Ftp.UploadFile;    // 指定执行什么命令
            //reqFTP.UseBinary = true;   // 指定数据传输类型
            //reqFTP.ContentLength = fileInf.Length;    // 上传文件时通知服务器文件的大小


            //int contentLen;
            //FileStream fileStream = fileInf.OpenRead(); // 打开一个文件读取内容到fileStream中
            //var buffer = new byte[fileStream.Length];
            //contentLen = fileStream.Read(buffer, 0, buffer.Length); //从fileStream读取数据到buffer中

            //Stream requestStream = reqFTP.GetRequestStream();
            //// 流内容没有结束
            //while (contentLen != 0)
            //{
            //    requestStream.Write(buffer, 0, contentLen);// 把内容从buffer 写入 requestStream中，完成上传。
            //    contentLen = fileStream.Read(buffer, 0, buffer.Length);
            //}

            //// 关闭两个流
            //requestStream.Close();
            ////uploadResponse = (FtpWebResponse)reqFTP.GetResponse();
            //fileStream.Close();



            var fc = new FtpClient("101.226.244.93:8221", "company7003", "LKXLNC9S3DCS");
            fc.UseBinary = true;

            var x = fc.UploadFile("D:\\1231.txt", "1231234.txt", FtpTransportMode.OverWrite);
        }
    }
}