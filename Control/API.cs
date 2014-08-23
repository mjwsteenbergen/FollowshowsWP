using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.IO;
using Windows.Security.Authentication.Web;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.Security.Credentials;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Followshows
{
    public class API
    {
        HttpClient client;
        private HttpCookieManager cookieMonster;
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
            if (http == null)
            {
                throw new Exception("No client is provided");
            }

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
            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            filter.AutomaticDecompression = true;
            HttpClient http = new HttpClient(filter);
            API web = new API(http, "None");
            web.cookieMonster = filter.CookieManager;
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
                    return await this.LoginWithEmail(cred.UserName.ToString(), cred.Password.ToString());
                }
            }
            catch (Exception)
            { }
            try
            {
                if (vault.FindAllByResource("facebook").ToString() != null)
                {
                    cred = vault.FindAllByResource("facebook")[0];
                    cred.RetrievePassword();
                    return await LoginWithFacebook();
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;

        }

        public void refresh()
        {
            client = new HttpClient();
            loggedIn = false;
        }

        private async Task<Response> getResponse(string url, IHttpContent cont, bool post)
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
                res.content = response;
                if (res.page.Contains("Forgot your password?"))
                {
                    if (!await login())
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

        private async Task<Response> getResponse(string url, IHttpContent cont)
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
            IHttpContent content = new HttpFormUrlEncodedContent(dict);

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
            IHttpContent content = new HttpFormUrlEncodedContent(dict);

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

        public async Task<bool> LoginWithFacebook()
        {
            //Get all the cookies from storage
            StorageFolder temp = ApplicationData.Current.LocalFolder;

            //Check if it might be there
            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.GetFileAsync("cookie");

                //Convert
                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);
                List<HttpCookie> cooklist = JsonConvert.DeserializeObject<List<HttpCookie>>(text.ToString());

                //Apply them to the httpclient, so we can log in.
                foreach (HttpCookie cook in cooklist)
                {
                    cookieMonster.SetCookie(cook);
                }

                //Check if it works
                Response resp = await getResponse("http://followshows.com/", null);

                if (resp.page == null || !resp.content.IsSuccessStatusCode || resp.page.Contains("Wrong email or password.") || resp.page.Contains("Already have an account? Log in now"))
                {
                    return false;
                }

                //Pure hapiness
                return true;

            }

            return false;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// Returns in:
        /// <see cref="App.xaml.cs/OnActivated"/>
        public void RegisterWithFacebook()
        {
            // Activate the broker to authenticate 
            WebAuthenticationBroker.AuthenticateAndContinue(new Uri("https://www.facebook.com/login.php?skip_api_login=1&api_key=287824544623545&signed_next=1&next=https%3A%2F%2Fwww.facebook.com%2Fv1.0%2Fdialog%2Foauth%3Fredirect_uri%3Dhttp%253A%252F%252Ffollowshows.com%252Ffacebook%252Flogin%26scope%3Demail%252Cpublish_actions%26client_id%3D287824544623545%26ret%3Dlogin&cancel_uri=http%3A%2F%2Ffollowshows.com%2Ffacebook%2Flogin%3Ferror%3Daccess_denied%26error_code%3D200%26error_description%3DPermissions%2Berror%26error_reason%3Duser_denied%23_%3D_&display=page"), new Uri("http://followshows.com/facebook/login"));
        }

        /// <summary>
        /// Storing all the cookies for future use;
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<bool> RegisterWithFacebook2(Windows.ApplicationModel.Activation.WebAuthenticationBrokerContinuationEventArgs args)
        {
            WebAuthenticationResult result = args.WebAuthenticationResult;

            if (result.ResponseStatus == WebAuthenticationStatus.Success)
            {
                StatusBar bar = StatusBar.GetForCurrentView();
                await bar.ProgressIndicator.ShowAsync();
                await bar.ShowAsync();
                bar.ProgressIndicator.Text = "Retrieving logindata";

                string output = result.ResponseData.ToString();

                Response resp = await getResponse(output, null);

                HttpCookieCollection col = cookieMonster.GetCookies(new Uri("http://followshows.com/"));

                List<HttpCookie> cooklist = new List<HttpCookie>();
                foreach (HttpCookie cok in col)
                {
                    cooklist.Add(cok);
                }

                StorageFolder local = ApplicationData.Current.LocalFolder;
                StorageFile fil = await local.CreateFileAsync("cookie", CreationCollisionOption.ReplaceExisting);

                PasswordVault vault = new PasswordVault();
                vault.Add(new PasswordCredential("facebook", "Nobody ever reads", "this shit"));

                bar.ProgressIndicator.Text = "Storing logindata";

                await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(cooklist));

                bar.ProgressIndicator.Text = "Logging in";

                return true;
            }

            return false;

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
                                        bitmapImage.CreateOptions = BitmapCreateOptions.None;
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

            if (head == null)
            {
                return tracker;
            }

            foreach (HtmlNode tvshow in head.ChildNodes)
            {
                try
                {
                    //bool bol = tvshow.GetAttributeValue("class", true);
                    //tvshow.Element("class");
                    //HtmlNodeCollection col =  tvshow.ChildNodes;
                    TvShow show = new TvShow(true);
                    HtmlNode title = getChild(tvshow);
                    show.Name = System.Net.WebUtility.HtmlDecode(title.InnerText);
                    show.Image = new BitmapImage() { UriSource = new Uri(getAttribute(title.DescendantNodes(), "src")) };
                    //show.ImageUrl = getAttribute(title.DescendantNodes(), "src");
                    show.showUrl = getAttribute(title.DescendantNodes(), "href").Replace("/show/", "");
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
                            ep.EpisodePos = "S" + build[1] + "E" + build[3].Replace(":", "");
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
            show.Followers = getChild(forFollowandName, 2).InnerText.Replace(" followers", "");
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
            foreach (SearchResult result in response)
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
            if (seasonnr == null || show.showUrl == null) return;
            Response resp = await getResponse("http://followshows.com/api/markSeasonAsWatched?show=" + show.showUrl + "&season=" + seasonnr, null);
            if (resp.hasInternet)
            {
                lastPage = resp.page;
            }
            if (resp.page == null || resp.page.Contains("DMCA Policy"))
            {
                Helper.message("Uhm... Something went wrong.", "Sorry");
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

        #region Offline Storage

        public async Task store()
        {
            if (queue != null && queue.Count != 0)
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.CreateFileAsync("queue.txt", CreationCollisionOption.ReplaceExisting);

                await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(queue));
            }
            if (tracker != null && tracker.Count != 0)
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.CreateFileAsync("tracker.txt", CreationCollisionOption.ReplaceExisting);

                await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(tracker));
            }

        }

        public async Task<List<Episode>> recoverQueue()
        {
            queue = new List<Episode>();

            StorageFolder temp = ApplicationData.Current.LocalFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.GetFileAsync("queue.txt");

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);

                text.ToString();

                queue = JsonConvert.DeserializeObject<List<Episode>>(text.ToString());

                List<Command> comList = await getCommands();

                foreach (Command com in comList)
                {
                    foreach (Episode ep in queue)
                    {
                        if (com.episode.EpisodePos == ep.EpisodePos && com.episode.SeriesName == ep.SeriesName)
                        {
                            ep.Seen = com.watched;
                        }
                    }
                }
            }
            return queue;
        }

        public async Task<List<TvShow>> recoverTracker()
        {
            tracker = new List<TvShow>();

            StorageFolder temp = ApplicationData.Current.TemporaryFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.GetFileAsync("tracker.txt");

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);

                text.ToString();

                tracker = JsonConvert.DeserializeObject<List<TvShow>>(text.ToString());

            }

            return tracker;
        }

        public async void addCommand(Command com)
        {
            StorageFolder temp = ApplicationData.Current.LocalFolder;
            StorageFile fil = null;
            string text = null;
            List<Command> comList = new List<Command>();

            try
            {
                fil = await temp.GetFileAsync("commands");
                text = await Windows.Storage.FileIO.ReadTextAsync(fil);
                comList = JsonConvert.DeserializeObject<List<Command>>(text.ToString());
            }
            catch { }

            if (fil == null)
            {
                fil = await temp.CreateFileAsync("commands");
            }


            comList.Add(com);

            await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(comList));


        }

        public async Task<List<Command>> getCommands()
        {
            StorageFolder temp = ApplicationData.Current.LocalFolder;
            List<Command> res = new List<Command>();
            StorageFile fil;

            try
            {
                fil = await temp.GetFileAsync("commands");
                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);
                res = JsonConvert.DeserializeObject<List<Command>>(text.ToString());

                return res;
            }
            catch
            {
                return res;
            }
        }

        public async Task executeCommands()
        {
            List<Command> commands = await getCommands();
            if (commands.Count > 0)
            {
                foreach (Command com in commands)
                {
                    if (com.watched)
                    {
                        markAsWatched(com.episode);
                    }
                    else
                    {
                        markNotAsWatched(com.episode);
                    }
                }
                try
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.GetFileAsync("commands");
                    await fil.DeleteAsync();
                }
                catch (Exception)
                { }
            }

        }

        #endregion

        public bool hasLoginCreds()
        {
            PasswordVault vault = new PasswordVault();
            PasswordCredential cred = null;
            try
            {
                if (vault.FindAllByResource("email").ToString() != null)
                {
                    cred = vault.FindAllByResource("email")[0];
                    cred.RetrievePassword();
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }
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