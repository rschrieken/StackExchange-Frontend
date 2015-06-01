using CsQuery;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StackExchange.Frontend
{
    public class ClientStack
    {
        private CookieContainer cookies = new CookieContainer();
        private 

        string _pwd;
        string _user;

        public ClientStack(string user, string pasword)
        {
            _user = user;
            _pwd = pasword;
        }

        public void Login()
        {
            SEOpenIDLogin(_user, _pwd);
        }

        public void LoginSite(string site)
        {
            site = site.Replace("http://", "https://");
            var index = Get(site);
            var login = Get(site + "/users/login?returnurl="+site);
            var html = CQ.Create(login);
            // If we find no fkey or session in html.
            string fkey = GetInputValue(html, "fkey");
            // give it a try.
            if (!String.IsNullOrEmpty(fkey))
            {
                // We found an fkey, use it to login via openid.
                var data = "email=" + Uri.EscapeDataString(_user) +
                           "&password=" + Uri.EscapeDataString(_pwd) +
                           "&fkey=" + fkey;

                var res = Post(site + "/users/login", data);
                // TODO, error checking 
            }

        }

        /// <summary>
        /// Visit flag-summary for a user
        /// </summary>
        /// <param name="user">the network user</param>
        /// <returns>all flags on the first page</returns>
        public List<Flag> GetFlags(network_user user)
        {
            var flags = new List<Flag>();
            // get the flag-summary page and CQ 
            var flagSummary = CQ.Create(
                Get(String.Format(
                        "{0}/users/flag-summary/{1}", 
                        user.site_url, 
                        user.user_id)));
            // itererate over each flaggesd post
            flagSummary
                .Find("#mainbar div.flagged-post")
                .Each(ele => {
                    // handle a single post
                    var flagdata = new Flag();
                    
                    var flagpost = CQ.Create(ele);
                    // the flagged post
                    var post = flagpost.Find("div.answer-link a");
                    flagdata.Url = user.site_url + post.Attr("href");
                    flagdata.Title = post.Text();

                    // the OP (and date)
                    var op = flagpost.Find("div.post-user-info span").Attr("title");
                    DateTime creationdate;
                    DateTime.TryParse(op, out creationdate);
                    flagdata.CreationDate = creationdate;

                    // spam flags look different
                    // our own flag data
                    var me = flagpost.Find("div.cbt");
                    var time = me.Find("span.relativetime").Attr("title");
                    DateTime flagdate;
                    DateTime.TryParse(time, out flagdate);
                    flagdata.FlagDate = flagdate;

                    // in the collection
                    flags.Add(flagdata);
                });

            return flags;
        }

        /// <summary>
        /// Call the StackApi https://api.stackexchange.com/docs/associated-users
        /// </summary>
        /// <param name="accountid">your account id</param>
        /// <returns>network-users</returns>
        public List<network_user> GetAssociatedAccounts(int accountid)
        {
            var users = new List<network_user>();
            using( var wc = new EnhancedWebclient() )
            {
                var serializer = new DataContractJsonSerializer(typeof(Wrapper));

                using (var ms = new MemoryStream(
                    wc.DownloadData(
                        String.Format("https://api.stackexchange.com/2.2/users/{0}/associated?pagesize=100&filter=!6PboxJ(eCNnSz", accountid))))
                {
                    var wrapper = (Wrapper) serializer.ReadObject(ms);
                    foreach(var user in wrapper.items)
                    {
                        users.Add(user);
                    }
                }
            }
            return users;
        }

        /// <summary>
        /// GET from the url whatever is returned as a string.
        /// This method uses/fills the shared CookieContainer.
        /// </summary>
        public string Get(string url)
        {
            using (var sr = new StreamReader(GetAsStream(url)))
            {
                return sr.ReadToEnd();
            }
        }

        // trickery to wait
        static ManualResetEvent  wait = new ManualResetEvent(false);

        /// <summary>
        /// ge the raw stream from the http response
        /// </summary>
        /// <param name="url">the url to get </param>
        /// <returns>the stream</returns>
        private Stream GetAsStream(string url)
        {
            wait.WaitOne(200); // prevent being throttled

            var req = HttpWebRequest.CreateHttp(url);
            req.Method = "GET";
            req.AllowAutoRedirect = true;
            req.CookieContainer = cookies;
            var resp = (HttpWebResponse)req.GetResponse();
            return resp.GetResponseStream();
        }

        /// <summary>
        /// POST to the url the urlencoded data and return the contents as a string.
        /// This method uses/fills the shared CookieContainer.
        /// </summary>
        private string Post(string url, string data)
        {
            using (var resp = PostStream(url, data))
            {
                using (var sr = new StreamReader(resp))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Posts and returns the HttpWebResponse
        /// </summary>
        /// <param name="url"></param>
        /// <param name="contenttype"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private HttpWebResponse PostRequest(string url, string contenttype, string data)
        {
            var req = HttpWebRequest.CreateHttp(url);
            req.Method = "POST";
            req.CookieContainer = cookies;
            if (!String.IsNullOrEmpty(data))
            {
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = data.Length;
                using (var sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(data);
                    sw.Flush();
                }
            }
            else
            {
                req.ContentType = contenttype;
                req.ContentLength = 0;
            }
            var resp = (HttpWebResponse)req.GetResponse();
            return resp;
        }


        /// <summary>
        /// Posts and returns the stream
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private Stream PostStream(string url, string data)
        {
            var resp = PostRequest(url, null, data);
            return resp.GetResponseStream();
        }



        /// <summary>
        /// Perform an login for the SE openID provider.
        /// Notice that when you run this from the BOT
        /// you are already logged-in,
        /// we only get the Cookies.
        /// </summary>
        private void SEOpenIDLogin(string email, string password)
        {
            // Do a Get to retrieve the cookies.
            var start = Get("https://openid.stackexchange.com/account/login");

            var html = CQ.Create(start);
            // If we find no fkey or session in html.
            string fkey = GetInputValue(html, "fkey");
            

            // give it a try.
            if (!String.IsNullOrEmpty(fkey))
            {
                // We found an fkey, use it to login via openid.
                var data = "email=" + Uri.EscapeDataString(email) +
                           "&password=" + Uri.EscapeDataString(password) +
                           "&fkey=" + fkey;

                var res = Post("https://openid.stackexchange.com/account/login/submit", data);

                var result = CQ.Create(res);
                var error = result.Find("div.error");
                if (error.Length >0)
                {
                    throw new Exception(error.Find("p").Text());
                }
                var user = result.Find("div.user-info");
            }
        }

        private static string GetInputValue(CQ input, string elementName)
        {
            var fkeyE = input["input"].FirstOrDefault(e => e.Attributes["name"] == elementName);

            return fkeyE == null ? null : fkeyE.Attributes["value"];
        }
    }
}
