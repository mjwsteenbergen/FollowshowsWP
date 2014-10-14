using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace Followshows.Control
{
    public class Response
    {
        private HttpClient client;

        public bool noInternet { get; set; }
        public bool pageCouldNotBeFound;
        public bool notLoggedIn;
        public bool somethingWentWrong;
        public string page;
        public HttpResponseMessage content;
        public HtmlDocument htmldoc;
        public HtmlNode firstNode;

        public Response(string url)
        {
            doActualResponse(url, null, true);
        }

        public Response(string url, IHttpContent cont)
        {
            doActualResponse(url, cont, true);
        }

        public Response(string url, IHttpContent cont, bool post)
        {
            doActualResponse(url, cont, post);
        }

        public async void doActualResponse(string url, IHttpContent cont, bool post)
        {
            HttpClient client = API.getAPI().getClient();

            if (testInternet())
            {
                somethingWentWrong = true;
                return;
            }
            if (await getResponse(url, cont, post))
            {
                somethingWentWrong = true;
                return;
            }
            htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(page);
            firstNode = htmldoc.DocumentNode;
            somethingWentWrong = false;
        }

        public bool testInternet()
        {
            var connectionP = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            if (connectionP == null)
            {
                noInternet = true;
                Helper.message("I am sorry to tell you, but I cannot connect to the internet :(", "No Internet");
                return true;
            }
            noInternet = false;
            return false;
        }

        public async Task<bool> getResponse(string url, IHttpContent cont, bool post)
        {
            Uri uri = new Uri(url);

            try
            {
                HttpResponseMessage response;
                if (post)
                {
                    response = await client.PostAsync(uri, cont);
                }
                else
                {
                    response = await client.GetAsync(uri);
                }
                if (!response.IsSuccessStatusCode)
                {
                    pageCouldNotBeFound = true;
                }
                pageCouldNotBeFound = false;

                page = Regex.Replace(((await response.Content.ReadAsStringAsync()).Replace("\n", "").Replace("\\\"", "").Replace("\t", "")), " {2,}", "");
                content = response;
                if (page.Contains("Forgot your password?") || resp.page.Contains("DMCA Policy"))
                {
                    notLoggedIn = true;
                    return true;
                }
                notLoggedIn = false;
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
