using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Xml.Linq;

namespace GamesSampleCloudService.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
            var uri = new Uri("https://db3.notify.windows.com/?token=AgYAAACH%2bR30TU9ZxLentivjW2gtlAKyq%2fSDPF8gHEIGdqARb8XPJMabUttNstLicKnPdkJpT5%2bIs6eBzDsdgT9R426bqXC8fD1lhAN1AVnwM65%2fUULDM%2bk5Y7sPRV86%2fqbSCjc%3d");
            //var result = Push(value as string, token);
            //PushToast(uri);
            NotifyBadge(uri, "test de badge");
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        private string GetAccessToken()
        {
            string url = "https://login.live.com/accesstoken.srf";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            string sid = HttpUtility.UrlEncode("ms-app://s-1-15-2-3979289415-2387761292-3812543000-2006087323-2767995084-3575680297-832369492");
            string secret = HttpUtility.UrlEncode("lvKlbEesvwM3Rqs004htrfNlkMz5DlcW");
            string content = "grant_type=client_credentials&client_id={0}&client_secret={1}&scope=notify.windows.com";

            string data = string.Format(content, sid, secret);

            byte[] notificationMessage = Encoding.Default.GetBytes(data);
            request.ContentLength = notificationMessage.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(notificationMessage, 0, notificationMessage.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            string result;
            using(Stream responseStream = response.GetResponseStream())
            {
                var streamReader = new StreamReader(responseStream);
                result = streamReader.ReadToEnd();
            }

            Newtonsoft.Json.Linq.JObject jObject = Newtonsoft.Json.Linq.JObject.Parse(result);
            result = (string)jObject["access_token"];
            
            return result;
        }

        public void NotifyBadge(Uri uri, string badge)
        {
            XDocument xml = XDocument.Parse(@"<badge value=""" + badge + @""" />");
            this.SendToWNS(uri, xml, NotificationType.Badge);
        }
        
        private void PushToast(Uri uri)
        {
            XDocument xml = XDocument.Parse(@"<toast launch=""" + Guid.NewGuid().ToString() + @""">
                                             <visual lang=""en-US"">
                                                 <binding template=""ToastText01"">
                                                     <text id=""1"">" + "C'est un toast de test" + @"</text>
                                                 </binding>
                                             </visual>
                                         </toast>");
            this.SendToWNS(uri, xml, NotificationType.Toast);
        }

        static string Token;

        private string SendToWNS(Uri uri, XDocument message, NotificationType notificationType)
        {
            Token = GetAccessToken();

            HttpWebRequest request = HttpWebRequest.Create(uri) as HttpWebRequest;

            request.Method = "POST";
            request.Headers.Add("X-WNS-Type", this.GetNotificationType(notificationType));
            request.Headers.Add("X-WNS-RequestForStatus", "true");
            request.ContentType = "text/xml";
            request.Headers.Add("Authorization", String.Format("Bearer {0}", Token));
     
            byte[] contentInBytes = Encoding.UTF8.GetBytes(message.ToString(SaveOptions.DisableFormatting));
     
            using (Stream requestStream = request.GetRequestStream())
                requestStream.Write(contentInBytes, 0, contentInBytes.Length);

            try
            {
                using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse())
                {
                    Debug.WriteLine(webResponse.Headers["X-WNS-DEVICECONNECTIONSTATUS"]);
                    Debug.WriteLine(webResponse.Headers["X-WNS-NOTIFICATIONSTATUS"]);
                    Debug.WriteLine(webResponse.Headers["X-WNS-MSG-ID"]);
                    Debug.WriteLine(webResponse.Headers["X-WNS-DEBUG-TRACE"]);                         
                    return webResponse.StatusCode.ToString();
                }
            }
            catch (Exception e)
            {
                return e.Message;
                // TODO gérer l'exception
            }
            
        }

        enum NotificationType
        {
            Badge,
            Tile,
            Toast,
            Raw
        }

        private string GetNotificationType(NotificationType notificationType)
        {
            switch (notificationType)
            {
                case NotificationType.Badge:
                    return "wns/badge";
                case NotificationType.Tile:
                    return "wns/tile";
                case NotificationType.Toast:
                    return "wns/toast";
                case NotificationType.Raw:
                    return "wns/raw";
            }
     
            throw new NotSupportedException("Type is not supported");
        }
                                                                                
        private HttpStatusCode Push(string pushUri, string accessToken)
        {
            //TODO a degager
            pushUri = "https://db3.notify.windows.com/?token=AgYAAADPY6A6SyeB0co6mr7VwP7e25mvvREAmd%2fbHnMZx39dKZAL9810LWLTyHnODdunval4kqRtHUsVKKwK9UGBTFVd2%2ffxtlHu1T58hTy%2fJZrCJ%2boyoKcZ9%2bvmMewyMZdet30%3d";

            var subscriptionUri = new Uri(pushUri);

            var request = (HttpWebRequest)WebRequest.Create(subscriptionUri);
            request.Method = "POST";
            request.ContentType = "text/xml";
            request.Headers = new WebHeaderCollection();
            request.Headers.Add("X-WNS-Type", "wns/badge");
            request.Headers.Add("Authorization","Bearer " + accessToken);

            string data = "<?xml version='1.0' encoding='utf-8'?><badge value=\"2\"/>";

            byte[] notificationMessage = Encoding.Default.GetBytes(data);
            request.ContentLength = notificationMessage.Length;

            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(notificationMessage, 0, notificationMessage.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();

            return response.StatusCode;
        }

    }
}