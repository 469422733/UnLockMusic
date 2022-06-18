using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnLockMusic
{
    /// <summary>
    /// 来源，0-，1-QQ音乐，2-酷狗音乐
    /// </summary>
    enum enmMusicSource
    {
        Nothing,
        QQ,
        Kg,
        Kw,
        Wyy
    }
    /// <summary>
    /// 音乐信息
    /// </summary>
    class clsMusic
    {
        private int mscID;              //ID，实际无用，实际使用以列表ID为主
        private string mscName;         //歌名
        private string mscSubheading;   //副标题
        private string mscSinger;       //歌手
        private string mscClass;        //专辑
        private enmMusicSource mscSource;          //来源，0-，1-QQ音乐，2-酷狗音乐
        private string mscDownloadInfo; //QQ音乐为mid，酷狗音乐为FileHash
        private string mscDownloadURL;  //下载URL
        private bool mscCanDownload;    //是否可下载

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="intID">ID</param>
        /// <param name="strName">歌名</param>
        /// <param name="strSubheading">副标题</param>
        /// <param name="strSinger">歌手</param>
        /// <param name="strClass">专辑</param>
        /// <param name="intSource">来源</param>
        /// <param name="strOtherInfo">其他信息，如：QQ音乐为mid</param>
        /// <param name="bolCanDownload">是否可下载，默认true</param>
        public clsMusic(int intID, string strName, string strSubheading, string strSinger, string strClass, enmMusicSource emsSource, string strOtherInfo, bool bolCanDownload = true)
        {
            mscID = intID;
            mscName = strName;
            mscSubheading = strSubheading;
            mscSinger = strSinger;
            mscClass = strClass;
            mscSource = emsSource;
            mscCanDownload = bolCanDownload;

            switch (emsSource)
            {
                case enmMusicSource.QQ:
                case enmMusicSource.Kg:
                case enmMusicSource.Kw:
                case enmMusicSource.Wyy:
                    mscDownloadInfo = strOtherInfo;
                    mscDownloadURL = "";
                    break;
                default:
                    mscDownloadInfo = strOtherInfo;
                    mscDownloadURL = "";
                    break;
            }
        }

        /// <summary>
        /// ID，实际不使用
        /// </summary>
        public int ID
        {
            get { return mscID; }
        }

        /// <summary>
        /// 歌名
        /// </summary>
        public string Name
        {
            get { return mscName; }
        }

        /// <summary>
        /// 副标题
        /// </summary>
        public string Subheading
        {
            get { return mscSubheading; }
        }

        /// <summary>
        /// 歌手
        /// </summary>
        public string Singer
        {
            get { return mscSinger; }
        }

        /// <summary>
        /// 专辑
        /// </summary>
        public string Class
        {
            get { return mscClass; }
        }

        /// <summary>
        /// 来源
        /// </summary>
        public enmMusicSource Source
        {
            get { return mscSource; }
        }

        /// <summary>
        /// QQ音乐为mid，酷狗音乐为FileHash
        /// </summary>
        public string DownloadInfo
        {
            get { return mscDownloadInfo; }
        }

        public string DownloadURL
        {
            get { return mscDownloadURL; }
        }

        /// <summary>
        /// 是否可下载
        /// </summary>
        public bool CanDownload
        {
            get { return mscCanDownload; }
        }
    }
}
