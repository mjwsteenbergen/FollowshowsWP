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
using SharedCode;

namespace SharedCode
{
    public class API
    {
        HttpClient client;
        private HttpCookieManager cookieMonster;
        public Object passed;
        public bool loggedIn;
        public bool gotInternet;

        public bool debug = false;

        private NetworkChanged network;

        private List<Episode> queue;
        private List<TvShow> tracker;
        private List<Episode> watchList;
        private List<Episode> calendar;



        #region BASIC

        private static API api;
        public static API getAPI()
        {
            if (api == null)
            {
                api = new API();
            }
            return api;
        }

        public HttpClient getClient()
        {
            
            return client;
        }

        public API()
        {
            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            filter.AutomaticDecompression = true;
            HttpClient http = new HttpClient(filter);
            cookieMonster = filter.CookieManager;

            client = http;
            loggedIn = false;
            gotInternet = false;

            //debug = true;

            network = new NetworkChanged();

            Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged += NetworkStatusChanged;
        }

        public async void refresh()
        {
            client = new HttpClient();

            queue = new List<Episode>();
            calendar = new List<Episode>();
            tracker = new List<TvShow>();

            loggedIn = false;

            try
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.CreateFileAsync("cookie", CreationCollisionOption.ReplaceExisting);
            }
            catch
            { }
        }

        #endregion

        

        

        public async Task<bool> login()
        {
            PasswordVault vault = new PasswordVault();
            PasswordCredential cred = null;
            try
            {
                if (vault.FindAllByResource("email").ToString() != null)
                {
                    cred = vault.FindAllByResource("email")[0];
                    cred.RetrievePassword();
                    return (await LoginWithEmail(cred.UserName.ToString(), cred.Password.ToString()));
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

        public async Task<Boolean> hasInternet()
        {
            var connectionP = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            if (connectionP == null)
            {
                gotInternet = false;
                return false;
            }    

            if(gotInternet == true)
            {
                return true;
            }

            Response res =  await (new Response("google.com")).call();

            gotInternet = true;

            return !res.noInternet;
        }

        public async Task<bool> RegisterWithEmail(string firstname, string lastname, string email, string password, string timezone)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("firstName", firstname);
            dict.Add("lastName", password);
            dict.Add("email", email);
            dict.Add("password", password);
            dict.Add("timezone", new Regex("(.)+ - ").Replace(timezone, "").ToString());
            IHttpContent content = new HttpFormUrlEncodedContent(dict);

            Response resp = await (new Response("http://followshows.com/signup/save", content)).call();
            if (!resp.somethingWentWrong)
            {
                return resp.page.Contains("Last step! Follow some TV shows.");
            }
            return false;
        }

        public async Task<bool> LoginWithEmail(string username, string password)
        {
            //Login Data    
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("j_username", username);
            dict.Add("j_password", password);
            IHttpContent content = new HttpFormUrlEncodedContent(dict);

            //Login
            Response resp = await (new Response("http://followshows.com/login/j_spring_security_check", content)).call();
            if (!(resp.pageCouldNotBeFound && resp.noInternet))
            {
                //Check if actually loggedIn
                if (!resp.page.Contains("Wrong email or password."))
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
                Response resp = await (new Response("http://followshows.com/")).call();

                if (resp.somethingWentWrong)
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
            WebAuthenticationBroker.AuthenticateAndContinue(new Uri("https://followshows.com/facebook/authenticate"), new Uri("http://followshows.com/facebook/login"));
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

                Response resp = await (new Response(output, null)).call();

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

        

        

        public async Task<List<Episode>> getWatchList()
        {
            watchList = new List<Episode>();

            Response resp = await (new Response("http://followshows.com/home/watchlist", false)).call();
            HtmlDocument doc = new HtmlDocument();
            if (resp.page == null)
                return watchList;
            doc.LoadHtml(resp.page);

            foreach (HtmlNode episode in HTML.getChild(doc.DocumentNode.ChildNodes, "class", "videos-grid videos-grid-home clearfix episodes-popover").ChildNodes)
            {
                watchList.Add(Episode.getWatchListEpisode(episode));
            }
            foreach (HtmlNode episode in HTML.getChild(doc.DocumentNode.ChildNodes, "class", "videos-grid-home-more clearfix episodes-popover").ChildNodes)
            {
                watchList.Add(Episode.getWatchListEpisode(episode));
            }

            return watchList;
        }

        public async Task<List<Episode>> getCalendar()
        {
            calendar = new List<Episode>();

            string url = "http://followshows.com/api/calendar?date="
                + (DateTime.UtcNow.Date.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString()
                + "&days=14";

            Response resp = await (new Response(url)).call();

            HtmlDocument doc = new HtmlDocument();
            if (resp.page == null)
                return calendar;
            doc.LoadHtml(resp.page);

            HtmlNode table = HTML.getChild(doc.DocumentNode, "class", "calendar");
            HtmlNode tableRow = HTML.getChild(table, 1);

            bool check = false;
            foreach (HtmlNode day in tableRow.ChildNodes)
            {
                if (!HTML.getAttribute(day, "class").Contains("today") && check == false)
                    continue;
                check = true;

                HtmlNode ul = HTML.getChild(day);
                if (ul != null)
                {
                    foreach (HtmlNode li in ul.ChildNodes)
                    {
                        calendar.Add(Episode.getCalendarEpisode(li));
                    }
                }

            }

            return calendar;
        }

        public async Task<List<Episode>> getSeason(TvShow show, int seasonNr)
        {
            List<Episode> season = new List<Episode>();

            Response resp = await (new Response("http://followshows.com/api/show/" + show.showUrl + "/season/" + seasonNr, false)).call();

            if (resp.somethingWentWrong)
                return season;

            foreach (HtmlNode episode in HTML.getChild(resp.firstNode.ChildNodes, "class", "clearfix").ChildNodes)
            {
                Episode ep = new Episode(false, false);
                ep.ShowName = show.Name;

                HtmlNode name = HTML.getChild(episode.ChildNodes, "class", "episode-link");
                if (name != null)
                {
                    ep.Aired = true;
                    ep.EpisodeName = name.InnerText;
                    ep.Image = new BitmapImage(new Uri(HTML.getAttribute(HTML.getChild(episode.ChildNodes, "class", "poster").ChildNodes, "src").Replace("130x75", "360x207")));

                    string[] build = HTML.getChild(episode.ChildNodes, "class", "episode-label").InnerText.Split(new char[] { ' ' });
                    ep.ISeason = int.Parse(build[1]);
                    ep.IEpisode = int.Parse(build[3].Split(new char[] { ',' })[0]);
                    ep.EpisodePos = "S" + ep.ISeason + "E" + ep.IEpisode;
                    ep.id = HTML.getAttribute(episode.ChildNodes, "episodeid");
                    if (!episode.InnerText.Contains("Mark as watched"))
                    {
                        ep.Seen = true;
                    }

                }
                else
                {
                    ep.Aired = false;
                    ep.Image = new BitmapImage(new Uri("ms-appx:Assets/basicQueueItem.png"));
                    string[] build = episode.InnerText.Split(new char[] { ',' });
                    ep.EpisodeName = build[0];
                    string[] seasonThing = build[1].Split(new char[] { ' ' });
                    ep.ISeason = int.Parse(seasonThing[1]);
                    ep.IEpisode = int.Parse(seasonThing[3]);
                    ep.EpisodePos = "S" + ep.ISeason + "E" + ep.IEpisode;
                }


                season.Add(ep);
            }

            return season;
        }

        public async Task<List<TvShow>> searchTvShow(string searchTerm)
        {
            List<TvShow> showList = new List<TvShow>();
            List<TvShow> userList = new List<TvShow>();

            if (searchTerm == null || searchTerm == "")
            {
                passed = userList;
                return showList;
            }

            Response resp = await (new Response("http://followshows.com/ajax/header/search?term=" + searchTerm, null, false)).call();
            if (resp.page == null)
                return showList;
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
                    showList.Add(show);
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
                    userList.Add(show);
                }
            }
            passed = userList;
            return showList;
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
                    }

                    network.OnPropertyChanged("network");
                }
            }
        }

        #region Offline Storage

        

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
                        await com.episode.markAsWatched();
                    }
                    else
                    {
                        await com.episode.markNotAsWatched();
                    }
                }
                try
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.GetFileAsync("commands");
                    await fil.DeleteAsync();
                }
                catch (Exception e)
                {
                    Memory.writeErrorToFile(this, e).RunSynchronously();
                }
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