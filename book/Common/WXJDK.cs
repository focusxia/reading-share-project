using book.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace book.Common
{
    public class WXJDK
    {
        public static string GetSha1Str(string str)
        {
            byte[] strRes = Encoding.UTF8.GetBytes(str);
            HashAlgorithm iSha = new SHA1CryptoServiceProvider();
            strRes = iSha.ComputeHash(strRes);
            var enText = new StringBuilder();
            foreach (byte iByte in strRes)
            {
                enText.AppendFormat("{0:x2}", iByte);
            }
            return enText.ToString();
        }

        public static string GetAccessToken()
        {
            string apiUrl = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", "wxb8ea569b2499e60f", "746cb59d05e296a7b86df7cd8e60c025");
            string token = null;
            if (System.Web.HttpContext.Current.Session["JsAccessToken"] == null)
            {
                string result = GetMethod(apiUrl);
                if (!string.IsNullOrEmpty(result))
                {
                    JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                    token = jo["access_token"].ToString();
                    System.Web.HttpContext.Current.Session["JsAccessToken"] = token;
                    System.Web.HttpContext.Current.Session.Timeout = 7200;
                }
            }
            else
            {
                token = System.Web.HttpContext.Current.Session["JsAccessToken"].ToString();
            }
            return token;
        }

        public static string GetJsApiTicket()
        {
            string apiUrl = string.Format("https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={0}&type=jsapi", GetAccessToken());
            string ticket = null;
            if (System.Web.HttpContext.Current.Session["JsApiTicket"] == null)
            {
                string result = GetMethod(apiUrl);
                if (!string.IsNullOrEmpty(result))
                {
                    JObject jo = (JObject)JsonConvert.DeserializeObject(result);
                    ticket = jo["ticket"].ToString();
                    System.Web.HttpContext.Current.Session["JsApiTicket"] = ticket;
                    System.Web.HttpContext.Current.Session.Timeout = 7200;
                }
            }
            else
            {
                ticket = System.Web.HttpContext.Current.Session["JsApiTicket"].ToString();
            }
            return ticket;
        }


        public static string GetMethod(string url)
        {
            //var request = (HttpWebRequest)WebRequest.Create(url);

            //request.Method = "GET";
            //request.ContentType = "application/x-www-form-urlencoded";
            //var stream = request.GetRequestStream();
            //var response = (HttpWebResponse)request.GetResponse();

            //var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            WebClient wc = new WebClient();
            var result = wc.DownloadString(url);
            return result;
        }

        public static string Sign(string jsapiTicket, string nonceStr, string timestamp, string url)
        {
            string str = string.Format("jsapi_ticket={0}&noncestr={1}&timestamp={2}&url={3}", jsapiTicket, nonceStr, timestamp, url);
            return GetSha1Str(str);
        }

        public static string DateToUnix()
        {
            DateTime timeStamp = new DateTime(1970, 1, 1);  //得到1970年的时间戳
            long a = (DateTime.UtcNow.Ticks - timeStamp.Ticks) / 10000000;
            return a.ToString();
        }

        public static WXConfig GetWXConfig(string url)
        {
            WXConfig wxConfig = new WXConfig();
            wxConfig.appId = "wxb8ea569b2499e60f";
            wxConfig.timestamp = DateToUnix();
            wxConfig.nonceStr = getNoncestr();
            wxConfig.signature = Sign(GetJsApiTicket(), wxConfig.nonceStr, wxConfig.timestamp, url);
            wxConfig.jsApiList = new List<string>();
            wxConfig.jsApiList.Add("onMenuShareTimeline");
            wxConfig.jsApiList.Add("onMenuShareAppMessage");

            return wxConfig;
        }

        public static string getNoncestr()
        {
            Random random = new Random();

            return MD5Util.GetMD5(random.Next(1000).ToString(), "GBK");
        }
    }

    public class MD5Util
    {
        public MD5Util()
        {
            //
            // TODO: 在此处添加构造函数逻辑
            //
        }

        /** 获取大写的MD5签名结果 */
        public static string GetMD5(string encypStr, string charset)
        {
            string retStr;
            MD5CryptoServiceProvider m5 = new MD5CryptoServiceProvider();

            //创建md5对象
            byte[] inputBye;
            byte[] outputBye;

            //使用GB2312编码方式把字符串转化为字节数组．
            try
            {
                inputBye = Encoding.GetEncoding(charset).GetBytes(encypStr);
            }
            catch (Exception ex)
            {
                inputBye = Encoding.GetEncoding("GB2312").GetBytes(encypStr);
            }
            outputBye = m5.ComputeHash(inputBye);

            retStr = System.BitConverter.ToString(outputBye);
            retStr = retStr.Replace("-", "").ToUpper();
            return retStr;
        }
    }
}