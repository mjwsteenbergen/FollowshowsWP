using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using Facebook;
using Windows.Security.Authentication.Web;
using Windows.UI.Xaml.Controls;
using Windows.Security.Credentials;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Newtonsoft.Json;

namespace Followshows
{
    public class API
    {
        HttpClient client;
        public string lastPage { get; set; }
        public Object passed;
        public bool loggedIn;
        public bool gotInternet;

        public bool debug = false;

        private NetworkChanged network;

        private List<Episode> queue;
        private List<TvShow> tracker;
        private List<Episode> watchList;

        #region BASIC

        public API(HttpClient http, string lastVis)
        {
            lastPage = lastVis;
            client = http;
            loggedIn = false;
            gotInternet = false;

            //debug = true;

            network = new NetworkChanged();

            Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged += NetworkStatusChanged;
        }

        public static API createWebsite()
        {
            HttpClient http = new HttpClient();
            API web = new API(http, "None");
            Frame rootFrame = Window.Current.Content as Frame;

            return web;
        }

        public async Task<bool> login()
        {
            Frame rootFrame = Window.Current.Content as Frame;

            PasswordVault vault = new PasswordVault();
            PasswordCredential cred = null;
            try
            {
                if (vault.FindAllByResource("email").ToString() != null)
                {
                    cred = vault.FindAllByResource("email")[0];
                    cred.RetrievePassword();
                }
            }
            catch (Exception)
            { 
                return false;
            }
            return await this.LoginWithEmail(cred.UserName.ToString(), cred.Password.ToString());
        }

        public void refresh()
        {
            client = new HttpClient();
            loggedIn = false;
        }

        private async Task<Response> getResponse(string url, HttpContent cont, bool post)
        {
            Response res = new Response();
            Uri uri = new Uri(url);

            var connectionP = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            if (connectionP == null)
            {
                res.hasInternet = false;
                Helper.message("I am sorry to tell you, but I cannot connect to the internet :(", "No Internet");
                return res;
            }
            res.hasInternet = true;

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
                response.EnsureSuccessStatusCode();
                res.page = Regex.Replace(((await response.Content.ReadAsStringAsync()).Replace("\n", "").Replace("\\\"", "").Replace("\t", "")), " {2,}", "");
                res.content = response.Content;
                if (res.page.Contains("Forgot your password?"))
                {
                    if(! await login())
                    {
                        Frame rootFrame = Window.Current.Content as Frame;
                        if (!rootFrame.Navigate(typeof(LandingPage), this))
                        {
                            throw new Exception("Failed to create initial page");
                        }
                    }
                    if (post)
                    {
                        response = await client.PostAsync(uri, cont);
                    }
                    else
                    {
                        response = await client.GetAsync(uri);
                    }
                }
                return res;
            }
            catch (Exception)
            {
                return res;
            }

        }

        private async Task<Response> getResponse(string url, HttpContent cont)
        {
            return await getResponse(url, cont, true);
        }

        public bool hasInternet()
        {
            if (gotInternet)
                return true;
            else
            {
                var connectionP = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                if (connectionP == null)
                    return false;

                this.gotInternet = true;
            }
            return true;
        }

        #endregion

        #region HTMLSTUFF

        /// <summary>
        /// Returns the first child of the node
        /// </summary>
        /// <param name="node">An HTML node</param>
        /// <returns></returns>
        private HtmlNode getChild(HtmlNode node)
        {
            return getChild(node, 0);
        }

        /// <summary>
        /// Returns the i'th child
        /// </summary>
        /// <param name="node">HTML Node</param>
        /// <param name="i">th child</param>
        /// <returns></returns>
        private HtmlNode getChild(HtmlNode node, int i)
        {
            if (node != null && i != null)
            {
                HtmlNode[] res = node.DescendantNodes().ToArray<HtmlNode>();
                if (res.Length > i - 1)
                    return node.DescendantNodes().ToArray<HtmlNode>()[i];
            }
            return null;
        }

        /// <summary>
        /// Returns a node in the immediate decendants of the node which holds the attribute-value pair
        /// </summary>
        /// <param name="node">an HTML Node</param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HtmlNode getChild(HtmlNode node, string attribute, string value)
        {
            if (node == null || attribute == null || value == null)
                return null;
            foreach (HtmlNode child in node.DescendantNodes())
            {
                if (child.Attributes[attribute] != null)
                {
                    if (child.Attributes[attribute].Value == value)
                        return child;
                }
            }
            throw new InvalidDataException();
            //return null;
        }

        /// <summary>
        /// Returns the node if anywhere in the decendants holds a attribute-value pair
        /// </summary>
        /// <param name="col"></param>
        /// <param name="attribute"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public HtmlNode getChild(IEnumerable<HtmlNode> col, string attribute, string value)
        {
            if (col == null || attribute == null || value == null) return null;
            foreach (HtmlNode child in col)
            {
                if (child.Attributes[attribute] != null)
                {
                    if (child.Attributes[attribute].Value == value)
                        return child;
                }
                HtmlNode node = getChild(child.DescendantNodes(), attribute, value);
                if (node != null)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// Returns a node with the attribute, if it is somewhere in the decendants tree
        /// </summary>
        /// <param name="col"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private string getAttribute(IEnumerable<HtmlNode> col, string attribute)
        {
            if (col == null || attribute == null) return null;
            foreach (HtmlNode child in col)
            {
                if (child.Attributes[attribute] != null)
                {
                    return child.Attributes[attribute].Value;
                }
                string node = getAttribute(child.DescendantNodes(), attribute);
                if (node != null)
                    return node;
            }
            return null;
        }

        #endregion

        public async Task<bool> RegisterWithEmail(string firstname, string lastname, string email, string password, string timezone)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("firstName", firstname);
            dict.Add("lastName", password);
            dict.Add("email", email);
            dict.Add("password", password);
            dict.Add("timezone", new Regex("(.)+ - ").Replace(timezone, "").ToString());
            HttpContent content = new FormUrlEncodedContent(dict);

            Response resp = await getResponse("http://followshows.com/signup/save", content);
            if (resp.hasInternet)
            {
                lastPage = resp.page;
                return lastPage.Contains("Last step! Follow some TV shows.");
            }
            return false;
        }

        public async Task<Boolean> LoginWithEmail(string username, string password)
        {
            //Login Data    
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("j_username", username);
            dict.Add("j_password", password);
            HttpContent content = new FormUrlEncodedContent(dict);

            //Login
            Response resp = await getResponse("http://followshows.com/login/j_spring_security_check", content);
            if (resp.hasInternet && resp.page != null)
            {
                lastPage = resp.page;

                //Check if actually loggedIn
                if (!lastPage.Contains("Wrong email or password."))
                {
                    loggedIn = true;
                    return true;
                }
            }
            return false;
        }

        public async Task<string> LoginWithFacebook2(string username, string password)
        {
            HttpResponseMessage res2 = await client.SendAsync(new HttpRequestMessage(System.Net.Http.HttpMethod.Head, "http://followshows.com/"));

            //Login Data    
            Dictionary<string, string> dict = new Dictionary<string, string>();

            //Stupid data
            dict.Add("api_key", "287824544623545");
            dict.Add("display", "page");
            dict.Add("enable_profile_selector", "");
            dict.Add("legacy_return", "1");
            dict.Add("profile_selector_ids", "");
            dict.Add("skip_api_login", "1");
            dict.Add("signed_next", "1");
            dict.Add("trynum", "1");
            dict.Add("timezone", "-120");
            dict.Add("lgnrnd", "060437_4CU5");
            dict.Add("lgnjs", "1402232680");
            dict.Add("persistent", "1");
            dict.Add("default_persistent", "1");
            dict.Add("login", "Aanmelden");


            dict.Add("email", "martijn.j.w.steenbergen@planet.nl");
            dict.Add("pass", "w8opmij");
            HttpContent content = new FormUrlEncodedContent(dict);

            //
            //Login
            //lastPage = (string)(await getResponse("https://www.facebook.com/login.php?skip_api_login=1&api_key=287824544623545&signed_next=1&next=https%3A%2F%2Fwww.facebook.com%2Fv1.0%2Fdialog%2Foauth%3Fredirect_uri%3Dhttp%253A%252F%252Ffollowshows.com%252Ffacebook%252Flogin%26scope%3Demail%252Cpublish_actions%26client_id%3D287824544623545%26ret%3Dlogin&cancel_uri=http%3A%2F%2Ffollowshows.com%2Ffacebook%2Flogin%3Ferror%3Daccess_denied%26error_code%3D200%26error_description%3DPermissions%2Berror%26error_reason%3Duser_denied%23_%3D_&display=page", content))[1];
            //lastPage = (string)(await getResponse("https://www.facebook.com/dialog/oauth?client_id=287824544623545&redirect_uri=http://followshows.com/facebook/login&scope=email,publish_actions", null))[1];
            return lastPage;
        }

        public async void LoginWithFacebook()
        {
            FacebookClient _fb = new FacebookClient();
            var redirectUrl = "https://www.facebook.com/connect/login_success.html";
            try
            {
                //fb.AppId = facebookAppId;
                var loginUrl = _fb.GetLoginUrl(new
                {
                    client_id = "547279985377700",
                    redirect_uri = redirectUrl,
                    scope = "user_about_me,read_stream,publish_stream",
                    display = "popup",
                    response_type = "token"
                });

                var endUri = new Uri(redirectUrl);
                Windows.Foundation.Collections.ValueSet set = new Windows.Foundation.Collections.ValueSet();


                await Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, loginUrl, endUri);
                });
                //    WebAuthenticationOptions.None, loginUrl, endUri);
                //if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.Success)
                //{
                //    var callbackUri = new Uri(WebAuthenticationResult.ResponseData.ToString());
                //    var facebookOAuthResult = _fb.ParseOAuthCallbackUrl(callbackUri);
                //    var accessToken = facebookOAuthResult.AccessToken;
                //    if (String.IsNullOrEmpty(accessToken))
                //    {
                //        // User is not logged in, they may have canceled the login
                //    }
                //    else
                //    {
                //        // User is logged in and token was returned
                //        //LoginSucceded(accessToken);
                //    }

                //}
                //else if (WebAuthenticationResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
                //{
                //    throw new InvalidOperationException("HTTP Error returned by AuthenticateAsync() : " + WebAuthenticationResult.ResponseErrorDetail.ToString());
                //}
                //else
                //{
                //    // The user canceled the authentication
                //}
            }
            catch (Exception ex)
            {
                //
                // Bad Parameter, SSL/TLS Errors and Network Unavailable errors are to be handled here.
                //
                throw ex;
            }


        }

        public async Task<List<Episode>> getQueue()
        {
            if (!hasInternet() && queue != null)
                return queue;
            queue = new List<Episode>();

            Response resp = await getResponse("http://followshows.com/api/queue?from=0", null);
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resp.page);

            

            foreach (HtmlNode Episode in doc.DocumentNode.ChildNodes)
            {
                if (Episode.Name != "li") { continue; }
                Episode ep = new Episode(true, false);

                foreach (HtmlNode item in Episode.DescendantNodes())
                {
                    if (item.Attributes["class"] != null)
                    {
                        switch (item.Attributes["class"].Value)
                        {
                            case "column_date hidden-xs hidden-sm":
                                foreach (HtmlNode item2 in item.DescendantNodes())
                                {
                                    if (item2.Attributes["title"] != null)
                                        ep.date = item2.Attributes["title"].Value;
                                    if (item2.Attributes["class"] != null)//&& item2.Attributes["class"].Value == "network")
                                        ep.network = item2.InnerHtml;
                                }
                                break;
                            case "column_poster":
                                foreach (HtmlNode node in item.DescendantNodes().ToArray<HtmlNode>()[0].DescendantNodes())
                                {
                                    if (node.Name == "#text") continue;
                                    if (node != null && node.Attributes["src"] != null)
                                    {
                                        BitmapImage bitmapImage = new BitmapImage();
                                        bitmapImage.UriSource = new Uri(node.Attributes["src"].Value.Replace("180", "360").Replace("104", "207"));
                                        //ep.Image = node.Attributes["src"].Value;
                                        ep.Image = bitmapImage;
                                    }

                                }
                                break;
                            case "column_infos":
                                helpItem(item, ep);
                                break;
                        }
                    }
                }
                queue.Add(ep);
            }
            //store();
            return queue;
        }

        public async Task<List<TvShow>> getTracker()
        {
            if (!hasInternet() && tracker != null)
                return tracker;
            tracker = new List<TvShow>();

            Response resp = await getResponse("http://followshows.com/viewStyleTracker?viewStyle=expanded", null);
            if (!resp.hasInternet || resp.page == null)
            {
                return tracker;
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resp.page);

            HtmlNode tbody = doc.GetElementbyId("tracker");
            HtmlNode head = getChild(tbody);

            foreach (HtmlNode tvshow in head.ChildNodes)
            {
                try
                {
                    //bool bol = tvshow.GetAttributeValue("class", true);
                    //tvshow.Element("class");
                    //HtmlNodeCollection col =  tvshow.ChildNodes;
                    TvShow show = new TvShow(true);
                    HtmlNode title = getChild(tvshow);
                    show.Name = WebUtility.HtmlDecode(title.InnerText);
                    show.Image = new BitmapImage() { UriSource = new Uri(getAttribute(title.DescendantNodes(), "src")) };
                    //show.ImageUrl = getAttribute(title.DescendantNodes(), "src");
                    show.showUrl = getAttribute(title.DescendantNodes(), "href").Replace("/show/","");
                    show.stillToWatch = getChild(tvshow.DescendantNodes(), "class", "towatch").InnerHtml;
                    string perc = getChild(tvshow, "class", "progress").InnerText.Replace("%", "");
                    show.percentageWatched = float.Parse(perc) / 100 * 150;
                    tracker.Add(show);
                }
                catch (Exception)
                {

                }

                //if debug enabled break to save time 
                if (debug && tracker.Count == 5)
                    break;
            }
            return tracker;
        }

        public void helpItem(HtmlNode item, Episode ep)
        {
            ep.SeriesName = item.DescendantNodes().ToArray<HtmlNode>()[0].InnerText;
            foreach (HtmlNode titleofzo in item.DescendantNodes())
                if (titleofzo.Attributes["class"] != null)
                {
                    switch (titleofzo.Attributes["class"].Value)
                    {
                        case "title":
                            ep.SeriesName = titleofzo.DescendantNodes().ToArray<HtmlNode>()[0].InnerHtml;
                            break;
                        case "infos":
                            string[] build = titleofzo.DescendantNodes().ToArray<HtmlNode>()[0].InnerHtml.Split(new char[] { ' ' });
                            ep.ISeason = build[1];
                            ep.IEpisode = build[3].Replace(":", "");
                            ep.EpisodePos = "Season " + build[1] + " Episode " + build[3].Replace(":", "");
                            string name = "";
                            for (int i = 4; i < build.Length; i++)
                            {
                                name += build[i] + " ";
                            }
                            ep.EpisodeName = name;
                            foreach (HtmlNode searchforSum in titleofzo.DescendantNodes())
                            {
                                if (searchforSum.Attributes["class"] != null && searchforSum.Attributes["class"].Value == "summary hidden-xs")
                                {
                                    ep.summary = searchforSum.InnerText.Replace(@"...&nbsp;(more)", "");
                                }
                            }
                            break;
                        case "btn btn-default watch-episode-button":
                            ep.id = titleofzo.Attributes["episodeid"].Value;
                            break;

                    }
                }
        }

        public async Task<List<Episode>> getWatchList()
        {
            watchList = new List<Episode>();
            
            Response resp = await getResponse("http://followshows.com/home/watchlist", null, false);
            HtmlDocument doc = new HtmlDocument();
            if (resp.page == null)
                return watchList;
            doc.LoadHtml(resp.page);

            

            foreach (HtmlNode episode in getChild(doc.DocumentNode.DescendantNodes(), "class", "videos-grid videos-grid-home clearfix episodes-popover").ChildNodes)
            {
                Episode res = new Episode(true, false);
                res.SeriesName = getChild(episode.DescendantNodes(), "class", "title").InnerText;
                res.id = getAttribute(episode.ChildNodes, "episodeId");
                HtmlNode Ename = getChild(episode.DescendantNodes(), "class", "subtitle");
                res.EpisodeName = getChild(Ename).InnerText;
                res.Image = new BitmapImage(new Uri(getAttribute(episode.DescendantNodes(), "style").Replace("background-image: url(", "").Replace(");", "")));
                string[] build = getChild(episode.DescendantNodes(), "class", "description").InnerText.Split(new char[] { ' ' });
                res.ISeason = build[1];
                res.IEpisode = build[3].Replace(".", "");
                res.EpisodePos = "Season " + res.ISeason + " Episode " + res.IEpisode;
                watchList.Add(res);
            }
            foreach (HtmlNode episode in getChild(doc.DocumentNode.DescendantNodes(), "class", "videos-grid-home-more clearfix episodes-popover").ChildNodes)
            {
                Episode res = new Episode(true, false);
                res.SeriesName = getChild(episode.DescendantNodes(), "class", "title").InnerText;
                res.id = getAttribute(episode.ChildNodes, "episodeId");
                HtmlNode Ename = getChild(episode.DescendantNodes(), "class", "subtitle");
                res.EpisodeName = getChild(Ename).InnerText;
                res.Image = new BitmapImage(new Uri(getAttribute(episode.DescendantNodes(), "style").Replace("background-image: url(", "").Replace(");", "")));
                string[] build = getChild(episode.DescendantNodes(), "class", "description").InnerText.Split(new char[] { ' ' });
                res.ISeason = build[1];
                res.IEpisode = build[3].Replace(".", "");
                res.EpisodePos = "Season " + res.ISeason + " Episode " + res.IEpisode;
                watchList.Add(res);
            }

            return watchList;
        }

        public async Task<TvShow> getShow(TvShow show)
        {
            Response resp = await getResponse("http://followshows.com/show/" + show.showUrl, null, false);
            HtmlDocument doc = new HtmlDocument();
            if (resp.page == null)
                return show;

            doc.LoadHtml(resp.page);

            HtmlNode summaryid = doc.GetElementbyId("summary");

            string extendedPart = getChild(summaryid.DescendantNodes(), "class", "details").InnerText;
            show.SummaryExtended = getChild(summaryid.DescendantNodes(), "class", "summary-text").InnerText.Replace("...&nbsp;(more)", "");

            show.Summary = show.SummaryExtended.Replace(extendedPart, "") + "...";



            HtmlNode showSummary = doc.GetElementbyId("content-about");
            show.Genre = getChild(showSummary.DescendantNodes(), "class", "genres").InnerText.Replace("GENRES:", "");
            show.Airs = getChild(showSummary.DescendantNodes(), "class", "infos col-xs-12 col-sm-6").InnerText.Replace("AIRS:", "");

            HtmlNode forFollowandName = getChild(showSummary.DescendantNodes(), "class", "summary");
            show.Followers = getChild(forFollowandName, 2).InnerText.Replace(" followers","");
            show.Name = getChild(forFollowandName, 0).InnerText;

            HtmlNode season = doc.GetElementbyId("season-filter");
            if (season != null)
            {
                show.numberOfSeasons = season.ChildNodes.ToArray<HtmlNode>().Length;
            }

            HtmlNode actors = doc.GetElementbyId("actors");
            if (actors != null)
            {
                show.Actors = actors.InnerText;
            }
            else
            {
                show.Actors = "None";
            }
            

            return show;
        }

        public async Task<List<Episode>> getSeason(TvShow show, int seasonNr)
        {
            List<Episode> season = new List<Episode>();

            Response resp = await getResponse("http://followshows.com/api/show/" + show.showUrl + "/season/" + seasonNr, null, false);
            HtmlDocument doc = new HtmlDocument();
            if (resp.page == null)
                return season;
            doc.LoadHtml(resp.page);

            

            foreach (HtmlNode episode in getChild(doc.DocumentNode.ChildNodes, "class", "clearfix").ChildNodes)
            {
                Episode ep = new Episode(false, false);
                ep.SeriesName = show.Name;

                HtmlNode name = getChild(episode.DescendantNodes(), "class", "episode-link");
                if (name != null)
                {
                    ep.Aired = true;
                    ep.EpisodeName = name.InnerText;
                    ep.Image = new BitmapImage(new Uri(getAttribute(getChild(episode.DescendantNodes(), "class", "poster").DescendantNodes(), "src").Replace("130x75", "360x207")));

                    string[] build = getChild(episode.DescendantNodes(), "class", "episode-label").InnerText.Split(new char[] { ' ' });
                    ep.ISeason = build[1];
                    ep.IEpisode = build[3].Split(new char[] { ',' })[0];
                    ep.EpisodePos = "Season " + ep.ISeason + " Episode " + ep.IEpisode;
                    ep.id = getAttribute(episode.DescendantNodes(), "episodeid");
                    if (!episode.InnerText.Contains("Mark as watched"))
                    {
                        ep.Seen = true;
                    }

                }
                else
                {
                    ep.Aired = false;
                    ep.EpisodeName = getChild(episode).InnerText;
                    ep.Image = new BitmapImage(new Uri("ms-appx:Assets/basicQueueItem.bmp"));
                    string[] build = episode.InnerText.Replace(ep.EpisodeName, "").Replace(",", " ").Split(new char[] { ' ' });
                    ep.ISeason = build[2];
                    ep.IEpisode = build[4];
                    ep.EpisodePos = "Season " + ep.ISeason + " Episode " + ep.IEpisode;
                }


                season.Add(ep);
            }

            season.Reverse();
            return season;
        }

        public async Task<List<TvShow>> searchTvShow(string searchTerm)
        {
            List<TvShow> res = new List<TvShow>();
            List<TvShow> res2 = new List<TvShow>();
           
            if (searchTerm == null || searchTerm == "")
            {
                passed = res2;
                return res;
            }

            Response resp = await getResponse("http://followshows.com/ajax/header/search?term=" + searchTerm, null, false);
            if (resp.page == null)
                return res;
            List<SearchResult> response = JsonConvert.DeserializeObject<List<SearchResult>>(resp.page);
            foreach(SearchResult result in response)
            {
                if (result.type == "show")
                {
                    TvShow show = new TvShow(result.followed);
                    if (result.poster)
                    {
                        show.Image = new BitmapImage(new Uri(result.image.Replace("30x42", "357x500")));
                    }
                    show.Name = result.value;
                    show.showUrl = result.id;
                    res.Add(show);
                }
                else
                {
                    TvShow show = new TvShow(false);
                    if (result.poster)
                    {
                        show.Image = new BitmapImage(new Uri(result.image.Replace("30x42", "357x500")));
                    }
                    show.Name = result.value;
                    show.showUrl = result.id;
                    res2.Add(show);
                }
            }
            passed = res2;
            return res;
        }

        public async void followShow(string showUrl)
        {
            if (showUrl == null) return;
            Response resp = await getResponse("http://followshows.com/api/followShow?show=" + showUrl, null);
            if (resp.hasInternet)
            {
                lastPage = resp.page;
            }
            if (resp.page == null || resp.page.Contains("DMCA Policy"))
            {
                Helper.message("Uhm... Something went wrong.", "Sorry");
            }
        }

        public async void unfollowShow(string showUrl)
        {
            if (showUrl == null) return;
            Response resp = await getResponse("http://followshows.com/api/unfollowShow?show=" + showUrl, null);
            if (resp.hasInternet)
            {
                lastPage = resp.page;
            }
            if (resp.page == null || resp.page.Contains("DMCA Policy"))
            {
                Helper.message("Uhm... Something went wrong.", "Sorry");
            }
        }

        public async void markSeasonAsWatched(string seasonnr, TvShow show)
        {
            if (seasonnr == null|| show.showUrl == null) return;
            Response resp = await getResponse("http://followshows.com/api/markSeasonAsWatched?show=" + show.showUrl  + "&season=" + seasonnr, null);
            if (resp.hasInternet)
            {
                lastPage = resp.page;
            }
            if (resp.page == null || resp.page.Contains("DMCA Policy"))
            {
                Helper.message("Uhm... Something went wrong.","Sorry");
            }
        }

        public async void markAsWatched(Episode ep)
        {
            if (ep.id == null) return;
            if (!ep.Aired)
            {
                Helper.message("This episode hasn't aired yet. You cannot mark it as watched", "EPISODE NOT AIRED");
                throw new Exception("IT DID NOT AIR");
            }
            Response resp = await getResponse("http://followshows.com/api/markEpisodeAsWatched?episodeId=" + ep.id, null);
            if (resp.hasInternet)
            {
                lastPage = resp.page;
            }
        }

        public async void markNotAsWatched(Episode ep)
        {
            if (ep.id == null) return;
            Response resp = await getResponse("http://followshows.com/api/markEpisodeAsNotWatched?episodeId=" + ep.id, null);
            if (resp.hasInternet)
            {
                lastPage = resp.page;
            }
        }



        public NetworkChanged getNetwork()
        {
            return network;
        }

        private async void NetworkStatusChanged(object sender)
        {
            if (!loggedIn)
            {
                PasswordVault vault = new PasswordVault();
                if (vault.FindAllByResource("email").ToString() != null)
                {
                    PasswordCredential cred = vault.FindAllByResource("email")[0];
                    cred.RetrievePassword();
                    if ((await LoginWithEmail(cred.UserName.ToString(), cred.Password.ToString())) != true)
                    {
                        Frame rootFrame = Window.Current.Content as Frame;
                        if (!rootFrame.Navigate(typeof(LandingPage), this))
                        {
                            throw new Exception("Failed to create initial page");
                        }
                    }

                    network.OnPropertyChanged("network");
                }
            }
        }



        //public async void store()
        //{
        //    if (queue != null)
        //    {
        //        StorageFolder temp = ApplicationData.Current.TemporaryFolder;
        //        StorageFile fil = await temp.CreateFileAsync("queu", CreationCollisionOption.ReplaceExisting);

        //        //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<Episode>));
        //        //IRandomAccessStream str = await fil.OpenAsync(FileAccessMode.ReadWrite);
        //        //ser.WriteObject(str.AsStreamForWrite(), queue);
        //    }

        //}

        //public async Task<List<Episode>> recoverQueue()
        //{
        //    StorageFolder temp = ApplicationData.Current.TemporaryFolder;
        //    IReadOnlyList<StorageFile> fill = await temp.GetFilesAsync();
        //    StorageFile fil = null;
        //    if (fill != null)
        //    {
        //        foreach (StorageFile fold in fill)
        //        {
        //            if (fold.Name == "queu")
        //            {
        //                fil = fold;
        //                break;
        //            }
        //        }
        //    }
        //    DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<Episode>));
        //    IRandomAccessStream str = await fil.OpenAsync(FileAccessMode.Read);
        //    return (List<Episode>)ser.ReadObject(str.AsStreamForRead());

        //}
    }

}

public class FalseLoginException : Exception
{
    public FalseLoginException() { }
}

public class Helper
{
    public async static void message(string message, string title)
    {
        try
        {
            await new MessageDialog(message, title).ShowAsync();
        }
        catch
        {

        }

    }

    public async static void message(string message)
    {
        await new MessageDialog(message).ShowAsync();
    }

}