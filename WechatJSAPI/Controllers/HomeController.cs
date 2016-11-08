using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace WechatJSAPI.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            return View();
        }


        public static readonly string appid =
          System.Web.Configuration.WebConfigurationManager.AppSettings["wxappid"];

        public static readonly string secret =
         System.Web.Configuration.WebConfigurationManager.AppSettings["wxsecret"];

        public static readonly bool isDedug =
        System.Web.Configuration.WebConfigurationManager.AppSettings["IsDebug"] == "true";


        public static string _ticket = "";

        public static DateTime _lastTimestamp;

        [HttpGet]
        public ActionResult Info(string url, string noncestr)
        {
            if (string.IsNullOrEmpty(_ticket) || _lastTimestamp == null || (_lastTimestamp - DateTime.Now).Milliseconds > 7200)
            {
                var resultString = HTTPHelper.GetHTMLByURL("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid="
                    + appid + "&secret=" + secret);
                dynamic resultValue = JsonConvert.DeserializeObject<dynamic>(resultString);
                if (resultValue == null || resultValue.access_token == null || resultValue.access_token.Value == null)
                {
                    return Json(new { issuccess = false, error = "获取token失败" });
                }
                var token = resultValue.access_token.Value;

                resultString = HTTPHelper.GetHTMLByURL("https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token=" + token + "&type=jsapi");
                dynamic ticketValue = JsonConvert.DeserializeObject<dynamic>(resultString);
                if (ticketValue == null || ticketValue.errcode == null || ticketValue.errcode.Value != 0 || ticketValue.ticket == null)
                    return Json(new { issuccess = false, error = "获取ticketValue失败" });
                _ticket = ticketValue.ticket.Value;
                _lastTimestamp = DateTime.Now;
                var timestamp = GetTimeStamp();
                var hexString = string.Format("jsapi_ticket={0}&noncestr={3}&timestamp={1}&url={2}",
                    _ticket, timestamp, url, noncestr);

                return Json(new
                {
                    issuccess = true,
                    sha1value = GetSHA1Value(hexString),
                    timestamp = timestamp,
                    url = url,
                    appid = appid,
                    debug = isDedug,
                    tiket = _ticket
                },JsonRequestBehavior.AllowGet);

            }
            else
            {
                var timestamp = GetTimeStamp();
                var hexString = string.Format("jsapi_ticket={0}&noncestr=1234567890123456&timestamp={1}&url={2}",
                   _ticket, timestamp, url);
                return Json(new
                {
                    issuccess = true,
                    sha1value = GetSHA1Value(hexString),
                    timestamp = timestamp,
                    url = url,
                    appid = appid,
                    debug = isDedug,
                    tiket = _ticket
                }, JsonRequestBehavior.AllowGet);
            }
        }


        private string GetSHA1Value(string sourceString)
        {
            var hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(sourceString));
            return string.Join("", hash.Select(b => b.ToString("x2")).ToArray());
        }

        private static string GetTimeStamp()
        {

            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);

            return Convert.ToInt64(ts.TotalSeconds).ToString();

        }


    }
}
