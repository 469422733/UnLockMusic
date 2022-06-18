using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace UnLockMusic
{
    /// <summary>
    /// 模拟浏览器操作
    /// </summary>
    public class clsHttpClient
    {
        private HttpClient m_htpClient;
        private HttpResponseMessage m_htpResponse;
        public int m_intStatusCode;    //提示码
        public string m_strStatusMsg;  //提示信息

        public clsHttpClient()
        {
            m_htpClient = new HttpClient();
            m_intStatusCode = 0;
            m_strStatusMsg = "";
        }

        /// <summary>
        /// Get方式
        /// </summary>
        /// <param name="url"></param>
        /// <param name="date">数据，格式为"a=1&b=2&c=3"</param>
        /// <returns></returns>
        public string GetWeb(string url, string date = "")
        {
            string strResult = "";

            if (url == "")
            {
                return strResult;
            }

            if (date != "")
                url = url + "?" + date;
            strResult = pGetWeb(url);
            return strResult;
        }
            
        /// <summary>
        /// Post方式，若date为空则无法调用
        /// </summary>
        /// <param name="url"></param>
        /// <param name="date">数据，格式为"a=1&b=2&c=3"</param>
        /// <returns></returns>
        public string PostWeb(string url, string date)
        {
            string strResult = "";
            List<KeyValuePair<String, String>> paramList = new List<KeyValuePair<string, string>>();

            if (url == "" || date == "")
            {
                return strResult;
            }

            paramList = pGetParamList(date);
            strResult = pPostWeb(url, paramList);
            return strResult;
        }

        public void AddHeaders(string key,string value)
        {
            m_htpClient.DefaultRequestHeaders.Add(key, value);
            //m_htpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3904.108 Safari/537.36");
            //m_htpClient.DefaultRequestHeaders.Add("csrf", "");
            //m_htpClient.DefaultRequestHeaders.Add("Referer", "http://www.kuwo.cn/search/list?key=鞠婧祎");
        }

        public string GetHeaders(string key)
        {
            return m_htpResponse.Headers.GetValues(key).FirstOrDefault<string>();
            //return m_htpResponse.Headers.GetValues(key).FirstOrDefault<string>();
        }

        //内部Get 和 Post 方法
        private string pGetWeb(string url)
        {
            string strResult = "";

            m_htpResponse = m_htpClient.GetAsync(new Uri(url)).Result;
            strResult = m_htpResponse.Content.ReadAsStringAsync().Result;

            return strResult;
        }

        private string pPostWeb(string url, List<KeyValuePair<String, String>> paramList)
        {
            string strResult = "";
            m_htpResponse = m_htpClient.PostAsync(new Uri(url), new FormUrlEncodedContent(paramList)).Result;
            strResult = m_htpResponse.Content.ReadAsStringAsync().Result;

            return strResult;
        }

        /// <summary>
        /// 获取参数列表
        /// </summary>
        /// <param name="date">参数，格式为"a=1&b=2&c=3"</param>
        /// <returns></returns>
        private List<KeyValuePair<string, string>> pGetParamList(string date)
        {
            List<KeyValuePair<String, String>> paramList = new List<KeyValuePair<String, String>>();

            if (date == "")
                return paramList;

            date = date + "&";
            string strKey = "";
            string strValue = "";
            int intFirst = 0;
            int intSecond = 0;

            do
            {
                intFirst = date.IndexOf("=");
                intSecond = date.IndexOf("&");
                strKey = date.Substring(0, intFirst);
                strValue = date.Substring(intFirst + 1, intSecond - intFirst - 1);
                paramList.Add(new KeyValuePair<string, string>(strKey, strValue));

                date = date.Substring(intSecond + 1);
            } while (date != "");

            return paramList;
        }


    }
}
