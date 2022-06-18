using System;
using System.IO;
using System.Net;

namespace UnLockMusic
{
    /// <summary>
    /// 来自于：https://www.cnblogs.com/xiandedanteng/p/7078902.html
    /// </summary>
    class clsHttpDownloadFile
    {
        /// <summary>
        /// Http方式下载文件，必定新建文件，如果后缀不是3个字母（如.mp3）必定出错。该方法暂时废弃。
        /// </summary>
        /// <param name="url">http地址</param>
        /// <param name="document">文件夹</param>
        /// <param name="localfile">文件名</param>
        /// <returns></returns>
        public bool DownloadEx(string url, string document, string localfile)
        {
            bool flag = false;
            long startPosition = 0; // 上次下载的文件起始位置
            FileStream writeStream; // 写入本地文件流对象
            int i = 0;

            localfile = (document == "" ? "" : document + "\\") + localfile;//如果有传入文件夹名，则加入文件夹名
            if (document != "")
                if (!Directory.Exists(document))   //如果不存在就创建 Music 文件夹  
                    Directory.CreateDirectory(document);

            while (File.Exists(localfile))
            {
                i++;
                if (i == 1)
                    localfile = localfile.Insert(localfile.Length - 4, "(" + i + ")");//
                else
                    localfile = localfile.Replace("(" + (i - 1) + ")", "(" + i + ")");
            }
            writeStream = new FileStream(localfile, FileMode.Create);// 文件不保存创建一个文件
            startPosition = 0;

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(url);// 打开网络连接

                if (startPosition > 0)
                {
                    myRequest.AddRange((int)startPosition);// 设置Range值,与上面的writeStream.Seek用意相同,是为了定义远程文件读取位置
                }


                Stream readStream = myRequest.GetResponse().GetResponseStream();// 向服务器请求,获得服务器的回应数据流


                byte[] btArray = new byte[512];// 定义一个字节数据,用来向readStream读取内容和向writeStream写入内容
                int contentSize = readStream.Read(btArray, 0, btArray.Length);// 向远程文件读第一次

                while (contentSize > 0)// 如果读取长度大于零则继续读
                {
                    writeStream.Write(btArray, 0, contentSize);// 写入本地文件
                    contentSize = readStream.Read(btArray, 0, btArray.Length);// 继续向远程文件读取
                }

                //关闭流
                writeStream.Close();
                readStream.Close();

                flag = true;        //返回true下载成功
            }
            catch (Exception)
            {
                writeStream.Close();
                flag = false;       //返回false下载失败
            }

            return flag;
        }
        /// <summary>
        /// Http方式下载文件
        /// </summary>
        /// <param name="url">http地址</param>
        /// <param name="localfile">文件名</param>
        /// <returns></returns>
        public bool Download(string url, string localfile)
        {
            bool flag = false;
            long startPosition = 0; // 上次下载的文件起始位置
            FileStream writeStream; // 写入本地文件流对象

            // 判断要下载的文件夹是否存在
            if (File.Exists(localfile))
            {
                writeStream = File.OpenWrite(localfile);             // 存在则打开要下载的文件
                startPosition = writeStream.Length;                  // 获取已经下载的长度
                writeStream.Seek(startPosition, SeekOrigin.Current); // 本地文件写入位置定位
            }
            else
            {
                writeStream = new FileStream(localfile, FileMode.Create);// 文件不保存创建一个文件
                startPosition = 0;
            }

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)HttpWebRequest.Create(url);// 打开网络连接

                if (startPosition > 0)
                {
                    myRequest.AddRange((int)startPosition);// 设置Range值,与上面的writeStream.Seek用意相同,是为了定义远程文件读取位置
                }

                Stream readStream = myRequest.GetResponse().GetResponseStream();// 向服务器请求,获得服务器的回应数据流

                byte[] btArray = new byte[512];// 定义一个字节数据,用来向readStream读取内容和向writeStream写入内容
                int contentSize = readStream.Read(btArray, 0, btArray.Length);// 向远程文件读第一次

                while (contentSize > 0)// 如果读取长度大于零则继续读
                {
                    writeStream.Write(btArray, 0, contentSize);// 写入本地文件
                    contentSize = readStream.Read(btArray, 0, btArray.Length);// 继续向远程文件读取
                }

                //关闭流
                writeStream.Close();
                readStream.Close();

                flag = true;        //返回true下载成功
            }
            catch (Exception)
            {
                writeStream.Close();
                flag = false;       //返回false下载失败
            }

            return flag;
        }

        /// <summary>
        /// http下载文件
        /// </summary>
        /// <param name="url">下载文件地址</param>
        /// <returns></returns>
        public static bool HttpDownload(string url, string localfile)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(url, localfile);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
