using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace SharedCode
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

        private string url;
        protected IHttpContent cont;
        protected bool post;

        public Response(string url)
        {
            this.url = url;
            cont = null;
            post = true;
        }

        public Response(string stringurl, IHttpContent content)
        {
            url = stringurl;
            cont = content;
            post = true;

        }

        public Response(string url, IHttpContent cont, bool post)
        {
            this.url = url;
            this.cont = cont;
            this.post = post;
        }

        public Response(string url, bool post)
        {
            this.url = url;
            this.post = post;
        }

        public async Task<Response> call()
        {
            await doActualResponse(url, cont, post);

            return this;
        }

        public async Task doActualResponse(string url, IHttpContent cont, bool post)
        {
            client = API.getAPI().getClient();
            notLoggedIn = true;
            pageCouldNotBeFound = true;
            somethingWentWrong = true;

            if (testInternet())
            {
                return;
            }
            if (await getResponse(url, cont, post))
            {
                return;
            }
            if (page.Contains("Forgot your password?"))
            {
                return;
            }
            notLoggedIn = false;
            somethingWentWrong = false;
        }

        public bool testInternet()
        {
            var connectionP = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            if (connectionP == null)
            {
                noInternet = true;
                return true;
            }
            noInternet = false;
            return false;
        }

        public async Task<bool> getResponse(string url, IHttpContent cont, bool post)
        {
            try
            {
                Uri uri = new Uri(url);

                HttpClient client = new HttpClient();

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
                    return true;
                }
                page = Regex.Replace(((await response.Content.ReadAsStringAsync()).Replace("\n", "").Replace("\\\"", "").Replace("\t", "")), " {2,}", "");                
                pageCouldNotBeFound = false;


                try
                {
                    htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(page);
                firstNode = htmldoc.DocumentNode;
                }
                catch(Exception)
                {}

                

                content = response;
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
