using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnLockMusic
{
    class clsMusicOperation
    {
        private string m_strFileFormat;
        private clsHttpClient m_htpClient;

        //QQ音乐
        private string m_strGetQQMusicList = "https://c.y.qq.com/soso/fcgi-bin/client_search_cp?new_json=1&w=<<SongName>>&format=json";
        private string m_strGetQQMusicVkey = "https://u.y.qq.com/cgi-bin/musicu.fcg?&data={\"req_0\":{\"module\":\"vkey.GetVkeyServer\",\"method\":\"CgiGetVkey\",\"param\":{\"guid\":\"958563768\",\"songmid\":[\"<<mid>>\"],\"uin\":\"291116260\"}}}";
        private string m_strGetQQMusicDownloadURL = "http://ws.stream.qqmusic.qq.com/";
        //酷狗音乐
        //private static string m_strKgMid = "4c681a7352fe592273400a79644bc30c"; //静态，第一次获取 //研究发现，mid可为任意值
        private string m_strGetKgMusicList = "https://songsearch.kugou.com/song_search_v2?&keyword=<<SongName>>&platform=WebFilter";
        private static string m_strGetKgMusicDownloadURL = "https://wwwapi.kugou.com/yy/index.php?r=play/getdata&hash=<<FileHash>>&dfid=<<kgDFID>>&mid=<<KgMid>>";
        //酷我音乐
        //private string m_strGetKwMusicListOld = "http://sou.kuwo.cn/ws/NSearch?type=music&key=<<SongName>>";//旧搜索URL，返回html
        private string m_strGetKwMusicList1 = "http://www.kuwo.cn/search/list?key=<<SongName>>";//新搜索，分两步，两个url
        private string m_strGetKwMusicList2 = "http://www.kuwo.cn/api/www/search/searchMusicBykeyWord?key=<<SongName>>&pn=1&rn=30&reqId=";
        private string m_strGetKwMusicDownloadURL = "http://antiserver.kuwo.cn/anti.s?format=mp3|mp3&rid=<<MUSICID>>&type=convert_url&response=res";//旧下载接口，可下vip
        //网易云
        private string m_strGetWyyMusicList = "https://music.163.com/weapi/cloudsearch/get/web?csrf_token=";
        private string m_strGetWyyMusicDownloadURL = "https://music.163.com/weapi/song/enhance/player/url/v1?csrf_token=";
        private string m_strWyyMusicIV = "0102030405060708";
        private string m_strWyyMusicGetList = "{\"hlpretag\":\"<span class=\\\"s-fc7\\\">\",\"hlposttag\":\"</span>\",\"s\":\"<<SongName>>\",\"type\":\"1\",\"offset\":\"0\",\"total\":\"true\",\"limit\":\"10\",\"csrf_token\":\"\"}";
        private string m_strWyyMusicGetDownloadURL = "{\"ids\":\"[<<ID>>]\",\"level\":\"standard\",\"encodeType\":\"aac\",\"csrf_token\":\"\"}";
        private string m_strWyyMusicSecond = "010001";
        private string m_strWyyMusicThrid = "00e0b509f6259df8642dbc35662901477df22677ec152b5ff68ace615bb7b725152b3ab17a876aea8a5aa76d2e417629ec4ee341f56135fccf695280104e0312ecbda92557c93870114af6c9d05c4f7f0c3685b7a46bee255932575cce10b424d813cfe4875d3e82047b97ddef52741d546b8e289dc6935b3ece0462db0a22b8e7";
        private string m_strWyyMusicFourth = "0CoJUm6Qyw8W8jud"; //第4个参数，随机16位数也用这个
        private string m_strWyyMusicRSA = "bf50d0bcf56833b06d8d1219496a452a1d860fd58a14c0aafba3e770104ca77dc6856cb310ed3309039e6865081be4ddc2df52663373b20b70ac25b4d0c6ca466daef6b50174e93536e2d580c49e70649ad1936584899e85722eb83ceddfb4f56c1172fca5e60592d0e6ee3e8e02be1fe6e53f285b0389162d8e6ddc553857cd"; //对应 m_strWyyMusicFourth 进行RSA加密后

        public clsMusicOperation()
        {
            m_htpClient = new clsHttpClient();
            m_strFileFormat = "";
        }

        /// <summary>
        /// 获取音乐列表
        /// </summary>
        /// <param name="SongName">歌曲名</param>
        /// <returns></returns>
        public List<clsMusic> GetMusicList(string SongName)
        {
            List<clsMusic> lmsc = new List<clsMusic>();
            var list = Parallel(() => GetKWMusicList(SongName), () => GetKGMusicList(SongName), () => GetWYYMusicList(SongName), () => GetQQMusicList(SongName));
            foreach (var value in list)
            {
                lmsc.AddRange(value);
            }
            return lmsc;
        }
        public List<clsMusic> GetKWMusicList(string SongName)
        {
            bool bolCanDownload;
            List<clsMusic> lmsc = new List<clsMusic>();
            //string url = m_strGetMusicList + SongName;
            //----------酷我音乐-----------
            string url = m_strGetKwMusicList1.Replace("<<SongName>>", SongName);
            string strJson = m_htpClient.GetWeb(url);
            string KwCSRF = m_htpClient.GetHeaders("Set-Cookie");
            KwCSRF = KwCSRF.Substring(KwCSRF.IndexOf("kw_token=") + 9, KwCSRF.IndexOf(";") - 9);
            m_htpClient.AddHeaders("csrf", KwCSRF);
            m_htpClient.AddHeaders("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
            m_htpClient.AddHeaders("Referer", url);
            url = m_strGetKwMusicList2.Replace("<<SongName>>", SongName);
            strJson = m_htpClient.GetWeb(url);

            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(strJson);

                for (int i = 0; i < jo["data"]["list"].Count(); i++)
                {
                    bolCanDownload = true; //重置
                                           //旧接口下载，高品质而且vip可下
                                           //if (jo["data"]["lists"][i]["pay"].ToString() == "16711935") //该字段为16711935，则为收费，不可下载
                                           //    bolCanDownload = false;

                    clsMusic msc = new clsMusic(i, jo["data"]["list"][i]["name"].ToString(), "", jo["data"]["list"][i]["artist"].ToString(), jo["data"]["list"][i]["album"].ToString(), enmMusicSource.Kw, jo["data"]["list"][i]["musicrid"].ToString(), bolCanDownload);

                    lmsc.Add(msc);
                }
            }
            catch { }

            return lmsc;
            //----------酷我音乐end------------
        }

        public List<clsMusic> GetKGMusicList(string SongName)
        {
            bool bolCanDownload;
            List<clsMusic> lmsc = new List<clsMusic>();
            //string url = m_strGetMusicList + SongName;
            //-----酷狗音乐-------------
            string url = m_strGetKgMusicList.Replace("<<SongName>>", SongName);
            string strJson = m_htpClient.GetWeb(url);

            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(strJson);

                int i;
                for (i = 0; i < jo["data"]["lists"].Count(); i++)
                {
                    bolCanDownload = true; //重置
                    if (jo["data"]["lists"][i]["trans_param"]["musicpack_advance"].ToString() == "1") //该字段为1，则为收费，不可下载
                        bolCanDownload = false;

                    clsMusic msc = new clsMusic(i, jo["data"]["lists"][i]["SongName"].ToString(), "", jo["data"]["lists"][i]["SingerName"].ToString(), jo["data"]["lists"][i]["AlbumName"].ToString(), enmMusicSource.Kg, jo["data"]["lists"][i]["FileHash"].ToString(), bolCanDownload);

                    lmsc.Add(msc);
                }
            }
            catch { }
            return lmsc;
            //-----酷狗音乐end-------------
        }

        public List<clsMusic> GetWYYMusicList(string SongName)
        {
            bool bolCanDownload;
            List<clsMusic> lmsc = new List<clsMusic>();
            clsAESEncrypt aes = new clsAESEncrypt();
            //----------网易云音乐---------
            string strWyyFirst = m_strWyyMusicGetList.Replace("<<SongName>>", SongName);
            strWyyFirst = aes.AESEncrypt(strWyyFirst, m_strWyyMusicFourth, m_strWyyMusicIV);
            strWyyFirst = aes.AESEncrypt(strWyyFirst, m_strWyyMusicFourth, m_strWyyMusicIV);//两次AES加密，得到 params 参数
                                                                                            //string url = m_strGetMusicList + SongName;
            string url = m_strGetWyyMusicList;
            string strJson = m_htpClient.PostWeb(url, "params=" + strWyyFirst + "&encSecKey=" + m_strWyyMusicRSA);

            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(strJson);
                int i;
                for (i = 0; i < jo["result"]["songs"].Count(); i++)
                {
                    bolCanDownload = true; //重置
                    if (jo["result"]["songs"][i]["privilege"]["fl"].ToString() == "0") //该字段为0，则为不可下载（歌名灰色）
                        bolCanDownload = false;

                    //副标题 jo["result"]["songs"][i]["alia"].ToString() 多为[]，需要清洗，干脆不要了
                    clsMusic msc = new clsMusic(i, jo["result"]["songs"][i]["name"].ToString(), "", jo["result"]["songs"][i]["ar"][0]["name"].ToString(), jo["result"]["songs"][i]["al"]["name"].ToString(), enmMusicSource.Wyy, jo["result"]["songs"][i]["id"].ToString(), bolCanDownload);

                    lmsc.Add(msc);
                }
            }
            catch { }
            return lmsc;
            //---------网易云音乐end---------
        }


        public List<clsMusic> GetQQMusicList(string SongName)
        {
            bool bolCanDownload;
            List<clsMusic> lmsc = new List<clsMusic>();
            //string url = m_strGetMusicList + SongName;
            //------QQ音乐--------
            string url = m_strGetQQMusicList.Replace("<<SongName>>", SongName);
            string strJson = m_htpClient.GetWeb(url);

            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(strJson);

                int i;
                for (i = 0; i < jo["data"]["song"]["list"].Count(); i++)
                {
                    bolCanDownload = true; //重置
                    if (jo["data"]["song"]["list"][i]["pay"]["pay_play"].ToString() == "1") //该字段为1，则为收费，不可下载
                        bolCanDownload = false;

                    clsMusic msc = new clsMusic(i, jo["data"]["song"]["list"][i]["name"].ToString(), jo["data"]["song"]["list"][i]["lyric"].ToString(), jo["data"]["song"]["list"][i]["singer"][0]["name"].ToString(), jo["data"]["song"]["list"][i]["album"]["name"].ToString(), enmMusicSource.QQ, jo["data"]["song"]["list"][i]["mid"].ToString(), bolCanDownload);

                    lmsc.Add(msc);
                }
            }
            catch { }
            return lmsc;
        }
        /// <summary>
        /// 获取音乐下载URL
        /// </summary>
        /// <param name="date">必要的数据，如QQ音乐为mid</param>
        /// <param name="intSource">来源，默认为1-QQ音乐</param>
        /// <returns></returns>
        public string GetMusicDownloadURL(string date, enmMusicSource emsSource = enmMusicSource.QQ)
        {
            string ResultURL = "";
            string url = "";
            JObject jo;
            string strWyyFirst = "";
            clsAESEncrypt aes = new clsAESEncrypt();

            switch (emsSource)
            {
                case enmMusicSource.QQ:
                    url = m_strGetQQMusicVkey.Replace("<<mid>>", date);
                    ResultURL = m_htpClient.GetWeb(url);

                    if (ResultURL.Substring(1, 8) == "\"code\":0")
                    {
                        jo = (JObject)JsonConvert.DeserializeObject(ResultURL);
                        ResultURL = jo["req_0"]["data"]["midurlinfo"][0]["flowurl"].ToString();
                        ResultURL = m_strGetQQMusicDownloadURL + ResultURL;

                        m_strFileFormat = jo["req_0"]["data"]["midurlinfo"][0]["filename"].ToString();
                        m_strFileFormat = m_strFileFormat.Substring(m_strFileFormat.IndexOf("."));
                    }
                    break;
                case enmMusicSource.Kg:
                    url = m_strGetKgMusicDownloadURL.Replace("<<FileHash>>", date);//.Replace("<<KgMid>>", "c596eb268a2705383a10d0af021664c0");//.Replace("<<KgDFID>>","07u9ob41Vu350chwOw4ejU7b");
                    ResultURL = m_htpClient.GetWeb(url);

                    if (ResultURL.Substring(1, 10) == "\"status\":1")
                    {
                        jo = (JObject)JsonConvert.DeserializeObject(ResultURL);
                        ResultURL = jo["data"]["play_url"].ToString();

                        m_strFileFormat = ResultURL.Substring(ResultURL.Length - 4);//可能会出错，如果后缀不是3个字节的话
                    }
                    break;
                case enmMusicSource.Kw:
                    ResultURL = m_strGetKwMusicDownloadURL.Replace("<<MUSICID>>", date);

                    m_strFileFormat = ".mp3";//旧接口目前只想到mp3格式
                    break;
                case enmMusicSource.Wyy:
                    strWyyFirst = m_strWyyMusicGetDownloadURL.Replace("<<ID>>", date);
                    strWyyFirst = aes.AESEncrypt(strWyyFirst, m_strWyyMusicFourth, m_strWyyMusicIV);
                    strWyyFirst = aes.AESEncrypt(strWyyFirst, m_strWyyMusicFourth, m_strWyyMusicIV);//两次AES加密，得到 params 参数
                    url = m_strGetWyyMusicDownloadURL;
                    ResultURL = m_htpClient.PostWeb(url, "params=" + strWyyFirst + "&encSecKey=" + m_strWyyMusicRSA);

                    if (ResultURL.Substring(1, 6) == "\"data\"")
                    {
                        jo = (JObject)JsonConvert.DeserializeObject(ResultURL);
                        ResultURL = jo["data"][0]["url"].ToString();

                        m_strFileFormat = "." + jo["data"][0]["type"].ToString();
                    }
                    break;
                default:
                    break;
            }

            return ResultURL;
        }

        /// <summary>
        /// 获取文件后缀，只能在GetMusicDownloadURL()方法后调用
        /// </summary>
        public string GetFileFormat()
        {
            return m_strFileFormat;
        }

        /// <summary>
        /// 设置酷狗mid，防止被封
        /// </summary>
        /// <param name="mid"></param>
        public static void SetKgMid(string mid = "c596eb268a2705383a10d0af021664c0")
        {
            m_strGetKgMusicDownloadURL = m_strGetKgMusicDownloadURL.Replace("<<KgMid>>", mid);
        }
        /// <summary>
        /// 设置酷狗rfid，防止被封
        /// </summary>
        /// <param name="rfid"></param>
        public static void SetKgDfid(string dfid = "07u9ob41Vu350chwOw4ejU7b")
        {
            m_strGetKgMusicDownloadURL = m_strGetKgMusicDownloadURL.Replace("<<kgDFID>>", dfid);
        }

        public static T[] Parallel<T>(params Func<T>[] actions)
        {
            Task<T>[] tasks = new Task<T>[actions.Length];
            T[] rtn = new T[actions.Length];
            int i = 0;
            foreach (var func in actions)
            {
                tasks[i] = Task.Run(func);
                ++i;
            }
            i = 0;
            foreach (var task in tasks)
            {
                task.Wait();
                rtn[i] = task.Result;
                ++i;
            }
            return rtn;
        }
    }
}
