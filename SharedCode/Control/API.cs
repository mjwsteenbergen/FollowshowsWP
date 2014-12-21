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

        public async Task<List<Episode>> getQueue()
        {
            if (!hasInternet() && queue != null)
                return queue;
            queue = new List<Episode>();

            Response resp = await (new Response("http://followshows.com/api/queue?from=0")).call();
            //TODO find out
            //HtmlDocument ht = new HtmlDocument();
            //ht.LoadHtml("<li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 21, 2014\">Nov 21</span>        <div class=\"network\">nickToons</div>        <div class=\"label label-warning\">NEW</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1693495&amp;api_user=130\" title=\"The Legend of Korra\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/The-Legend-of-Korra/The-Legend-of-Korra_S04E08_180x104.jpg\" title=\"Watch The Legend of Korra Season 4 Episode 8 - Remembrances\" alt=\"Watch The Legend of Korra Season 4 Episode 8 - Remembrances\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1693495&amp;api_user=130\" title=\"The Legend of Korra\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/The-Legend-of-Korra\">The Legend of Korra</a>        </div>        <div class=\"infos\">            <a href=\"/show/The-Legend-of-Korra/episode/S04E08\" title=\"The Legend of Korra 04x08\">Season 4 Episode 8: Remembrances</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/21/2014 on nickToons            </div>            <span class=\"summary hidden-xs\"></span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://www.nick.com/videos/clip/the-legend-of-korra-221-full-episode-jk3s.html\" title=\"Nickelodeon\">                    <img src=\"/images ick.png\" alt=\"Watch at Nickelodeon\" title=\"Watch at Nickelodeon\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/remembrances/id917393749?i=943789261&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PWZ504E?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=7aJyukHiEmc&amp;cdid=tvseason-K1__DfaeyDFY4nbYiycIKw&amp;gdid=tvepisode-mGoGn0C4nPM\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button watched\" episodeid=\"240115\">Watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 20, 2014\">Nov 20</span>        <div class=\"network\">CBS</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1693445&amp;api_user=130\" title=\"Elementary\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/Elementary/Elementary_S03E04_180x104.jpg\" title=\"Watch Elementary Season 3 Episode 4 - Bella\" alt=\"Watch Elementary Season 3 Episode 4 - Bella\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1693445&amp;api_user=130\" title=\"Elementary\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/Elementary\">Elementary</a>        </div>        <div class=\"infos\">            <a href=\"/show/Elementary/episode/S03E04\" title=\"Elementary 03x04\">Season 3 Episode 4: Bella</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/20/2014 on CBS            </div>            <span class=\"summary hidden-xs\">When a groundbreaking artificial intelligence software program is stolen, Sherlock agrees to take on the case, but enlists Joan’s assistance in solving it when he becomes more interested in disproving<span class=\"read-more\">...&nbsp;<a href=\"#\">(more)</a></span><span style=\"display: none;\" class=\"details\"> the computer’s abilities than finding the thief. Joan confronts Sherlock about his motives after she learns he has been in direct contact with her boyfriend without her knowledge.<span class=\"re-collapse\"> <a href=\"#\">(less)</a></span></span>            </span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://xfinitytv.comcast.net/watch/Elementary/8601213205877266112/361652291864/Elementary---Bella/videos\" title=\"Xfinity\">                    <img src=\"/images/xfinity.png\" alt=\"Watch at Xfinity\" title=\"Watch at Xfinity\">                </a>                <a href=\"http://www.cbs.com/shows/elementary/video/B882B544-C4CA-7085-EB5C-C5063C0CED2F/elementary-bella/\" title=\"CBS\">                    <img src=\"/images/cbs.png\" alt=\"Watch at CBS\" title=\"Watch at CBS\">                </a>                <a href=\"https://www.directv.com/tv/Elementary-eG9VYjJKM2V2d1k9/Bella-UkhPUVVYbGxUNkVsVTRzT2gyaEZ3dz09?primaryCta=streaming&amp;autoClick=ctaWatchNow\" title=\"DirecTV\">                    <img src=\"/images/directv_free.png\" alt=\"Watch at DirecTV\" title=\"Watch at DirecTV\">                </a>                <a href=\"http://uverse.com/tv/show/elementary?play=c___gBXzLweOLZ5K\" title=\"AT&amp;T U-verse\">                    <img src=\"/images/att_uverse_free.png\" alt=\"Watch at AT&amp;T U-verse\" title=\"Watch at AT&amp;T U-verse\">                </a>                <a href=\"http://www.dishanywhere.com/shows/elementary_445236/episodes/4104251/1751164/\" title=\"Dish\">                    <img src=\"/images/dish_free.png\" alt=\"Watch at Dish\" title=\"Watch at Dish\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PVNOMYQ?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=38xpt5je13E&amp;cdid=tvseason-9eHYEVNjKOsCkeclzCTODg&amp;gdid=tvepisode-6yvVulmw6LI\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/bella/id909051894?i=943693250&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"https://www.youtube.com/watch?v=6yvVulmw6LI\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"241482\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 20, 2014\">Nov 20</span>        <div class=\"network\">USA Network</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1693579&amp;api_user=130\" title=\"White Collar\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/White_Collar/White_Collar_S06E03_180x104.jpg\" title=\"Watch White Collar Season 6 Episode 3 - Uncontrolled Variables\" alt=\"Watch White Collar Season 6 Episode 3 - Uncontrolled Variables\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1693579&amp;api_user=130\" title=\"White Collar\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/White_Collar\">White Collar</a>        </div>        <div class=\"infos\">            <a href=\"/show/White_Collar/episode/S06E03\" title=\"White Collar 06x03\">Season 6 Episode 3: Uncontrolled Variables</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/20/2014 on USA Network            </div>            <span class=\"summary hidden-xs\">Neal is conflicted over scamming an innocent mark; Peter works with an audacious Interpol agent who could jeopardize their operation.</span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://xfinitytv.comcast.net/watch/White-Collar/7928405847638901112/361744963759/Uncontrolled-Variables/videos\" title=\"Xfinity\">                    <img src=\"/images/xfinity_tveverywhere.png\" alt=\"Watch at Xfinity\" title=\"Watch at Xfinity\">                </a>                <a href=\"http://www.usanetwork.com/whitecollar/videos/uncontrolled-variables\" title=\"USA\">                    <img src=\"/images/usa_tveverywhere.png\" alt=\"Watch at USA\" title=\"Watch at USA\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PGCSSOM?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/uncontrolled-variables/id931053768?i=942117831&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=cLFLPBy-boI&amp;cdid=tvseason-fhpE0Vxn4H3_FSAUDVoC4w&amp;gdid=tvepisode-1lbrRD_hRJo\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"238584\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 19, 2014\">Nov 19</span>        <div class=\"network\">CW</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1690267&amp;api_user=130\" title=\"The 100\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/The-100/The-100_S02E05_180x104.jpg\" title=\"Watch The 100 Season 2 Episode 5 - Human Trials\" alt=\"Watch The 100 Season 2 Episode 5 - Human Trials\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1690267&amp;api_user=130\" title=\"The 100\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/The-100\">The 100</a>        </div>        <div class=\"infos\">            <a href=\"/show/The-100/episode/S02E05\" title=\"The 100 02x05\">Season 2 Episode 5: Human Trials</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/19/2014 on CW            </div>            <span class=\"summary hidden-xs\">Kane leads a mission to make peace with the Grounders. Meanwhile, Jasper agrees to participate in a risky experiment, Lincoln enters a world of pain and President Dante Wallace issues a warning. Final<span class=\"read-more\">...&nbsp;<a href=\"#\">(more)</a></span><span style=\"display: none;\" class=\"details\">ly, Finn’s search for Clarke takes a violent turn.<span class=\"re-collapse\"> <a href=\"#\">(less)</a></span></span>            </span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://www.cwtv.com/shows/the-100/human-trials/?play=98c462cc-17ce-41c0-9194-fb49d701b4cb\" title=\"The CW\">                    <img src=\"/images/thecw.png\" alt=\"Watch at The CW\" title=\"Watch at The CW\">                </a>                <a href=\"http://www.hulu.com/watch/714348\" title=\"Hulu Plus\">                    <img src=\"/images/hulu_plus.png\" alt=\"Watch at Hulu Plus\" title=\"Watch at Hulu Plus\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/human-trials/id913271905?i=943337393&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=qDfC7pzsRJM&amp;cdid=tvseason-zjQZ6JuwiBIwyZvguIRM3g&amp;gdid=tvepisode-UlJMzvij5KI\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>                <a href=\"https://www.youtube.com/watch?v=UlJMzvij5KI\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PPMXSJ8?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"241428\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 19, 2014\">Nov 19</span>        <div class=\"network\">CW</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1690244&amp;api_user=130\" title=\"Arrow\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/Arrow/Arrow_S03E07_180x104.jpg\" title=\"Watch Arrow Season 3 Episode 7 - Draw Back Your Bow\" alt=\"Watch Arrow Season 3 Episode 7 - Draw Back Your Bow\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1690244&amp;api_user=130\" title=\"Arrow\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/Arrow\">Arrow</a>        </div>        <div class=\"infos\">            <a href=\"/show/Arrow/episode/S03E07\" title=\"Arrow 03x07\">Season 3 Episode 7: Draw Back Your Bow</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/19/2014 on CW            </div>            <span class=\"summary hidden-xs\">Oliver must stop an Arrow-obsessed serial killer, Carrie Cutter, who is convinced that The Arrow is her one true love and will stop at nothing to get his attention. Unfortunately, her way of getting h<span class=\"read-more\">...&nbsp;<a href=\"#\">(more)</a></span><span style=\"display: none;\" class=\"details\">is attention is to kill people. Meanwhile, Ray asks Felicity to be his date for a work dinner with important clients. Thea auditions new DJs for Verdant and meets Chase, a brash DJ with whom she immediately clashes.<span class=\"re-collapse\"> <a href=\"#\">(less)</a></span></span>            </span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://www.cwtv.com/shows/arrow/draw-back-your-bow/?play=2948afaa-bdfa-4623-9bb2-e63bee6195dd\" title=\"The CW\">                    <img src=\"/images/thecw.png\" alt=\"Watch at The CW\" title=\"Watch at The CW\">                </a>                <a href=\"http://www.hulu.com/watch/714978\" title=\"Hulu Plus\">                    <img src=\"/images/hulu_plus.png\" alt=\"Watch at Hulu Plus\" title=\"Watch at Hulu Plus\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PPMYI4M?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=r7y7TN7X_nw&amp;cdid=tvseason-I_iDDk5NToPgXCb58_Nnjw&amp;gdid=tvepisode-YhEkD9MHjUc\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>                <a href=\"https://www.youtube.com/watch?v=YhEkD9MHjUc\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/draw-back-your-bow/id909118472?i=943343289&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"229274\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 18, 2014\">Nov 18</span>        <div class=\"network\">ABC</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1673241&amp;api_user=130\" title=\"Forever (2014)\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/Forever-2014/Forever-2014_S01E09_180x104.jpg\" title=\"Watch Forever (2014) Season 1 Episode 9 - 6 A.M.\" alt=\"Watch Forever (2014) Season 1 Episode 9 - 6 A.M.\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1673241&amp;api_user=130\" title=\"Forever (2014)\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/Forever-2014\">Forever (2014)</a>        </div>        <div class=\"infos\">            <a href=\"/show/Forever-2014/episode/S01E09\" title=\"Forever (2014) 01x09\">Season 1 Episode 9: 6 A.M.</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/18/2014 on ABC            </div>            <span class=\"summary hidden-xs\">Harlems jazz community serves as the backdrop for a musical murder probe dealing with the rights to a legendary jazz hit, \"6 A.M.\" Whoever legitimately penned the chartbuster would be set for life, p<span class=\"read-more\">...&nbsp;<a href=\"#\">(more)</a></span><span style=\"display: none;\" class=\"details\">roviding a classic motive. Therefore, jazz history is on the line when Henry and Jo investigate the murder of jazz saxophonist Pepper Evans son. Simultaneously, bittersweet memories come flooding back to Henry, Jo and Pepper as they reflect on their father-child relationships. Meanwhile Henry, always a big fan of classical music, and Abe, a jazz enthusiast since he was a youngster, have a keyboard face-off as Abe tries to teach Henry to appreciate a new, more emotional kind of music<span class=\"re-collapse\"> <a href=\"#\">(less)</a></span></span>            </span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://abc.go.com/shows/forever/episode-guide/season-01/109-6-am\" title=\"ABC\">                    <img src=\"/images/abc_tveverywhere.png\" alt=\"Watch at ABC\" title=\"Watch at ABC\">                </a>                <a href=\"http://xfinitytv.comcast.net/watch/Forever/4783760702421939112/358809155507/6-A.M./videos\" title=\"Xfinity\">                    <img src=\"/images/xfinity_tveverywhere.png\" alt=\"Watch at Xfinity\" title=\"Watch at Xfinity\">                </a>                <a href=\"http://www.hulu.com/watch/714416\" title=\"Hulu Plus\">                    <img src=\"/images/hulu_plus.png\" alt=\"Watch at Hulu Plus\" title=\"Watch at Hulu Plus\">                </a>                <a href=\"https://www.youtube.com/watch?v=uc3t0Z6tzH0\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PPMZFHG?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/6-a.m./id908780464?i=941946569&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=2WP7-jyEYA4&amp;cdid=tvseason-rsUqFtffG2yUK7FFO-Mu3g&amp;gdid=tvepisode-uc3t0Z6tzH0\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"241966\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 18, 2014\">Nov 18</span>        <div class=\"network\">FOX</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1669912&amp;api_user=130\" title=\"New Girl\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net ew_Girl ew_Girl_S04E08_180x104.jpg\" title=\"Watch New Girl Season 4 Episode 8 - Teachers\" alt=\"Watch New Girl Season 4 Episode 8 - Teachers\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1669912&amp;api_user=130\" title=\"New Girl\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show ew_Girl\">New Girl</a>        </div>        <div class=\"infos\">            <a href=\"/show ew_Girl/episode/S04E08\" title=\"New Girl 04x08\">Season 4 Episode 8: Teachers</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/18/2014 on FOX            </div>            <span class=\"summary hidden-xs\">Jess, Coach and Ryan attend a teachers conference, led by education guru Brenda Brown. Back at the loft, Nick, Schmidt and Winston try to have a wild weekend while Jess is out of town.</span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://www.fox.com/watch/359119939519\" title=\"FOX\">                    <img src=\"/images/fox_tveverywhere.png\" alt=\"Watch at FOX\" title=\"Watch at FOX\">                </a>                <a href=\"http://xfinitytv.comcast.net/watch ew-Girl/7602787974345717112/359822915849/Teachers/videos\" title=\"Xfinity\">                    <img src=\"/images/xfinity_tveverywhere.png\" alt=\"Watch at Xfinity\" title=\"Watch at Xfinity\">                </a>                <a href=\"http://www.hulu.com/watch/714467\" title=\"Hulu Plus\">                    <img src=\"/images/hulu_plus.png\" alt=\"Watch at Hulu Plus\" title=\"Watch at Hulu Plus\">                </a>                <a href=\"https://www.youtube.com/watch?v=Xlm2iATuktE\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PM7G6RW?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=gYctkvdHCh8&amp;cdid=tvseason-3uEm8e4kxzfGGarw7zm1AA&amp;gdid=tvepisode-Xlm2iATuktE\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/teachers/id907588220?i=942160950&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"221439\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 17, 2014\">Nov 17</span>        <div class=\"network\">ABC</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1604280&amp;api_user=130\" title=\"Castle\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/Castle/Castle_S07E07_180x104.jpg\" title=\"Watch Castle Season 7 Episode 7 - Once Upon a Time in the West\" alt=\"Watch Castle Season 7 Episode 7 - Once Upon a Time in the West\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1604280&amp;api_user=130\" title=\"Castle\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/Castle\">Castle</a>        </div>        <div class=\"infos\">            <a href=\"/show/Castle/episode/S07E07\" title=\"Castle 07x07\">Season 7 Episode 7: Once Upon a Time in the West</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/17/2014 on ABC            </div>            <span class=\"summary hidden-xs\">When Castle and Beckett learn that a murder victim may have been poisoned at an Old West-style resort, they visit the resort posing as newlyweds to uncover the truth.</span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://xfinitytv.comcast.net/watch/Castle/5933713864774440112/358743619640/Once-Upon-a-Time-in-the-West/videos\" title=\"Xfinity\">                    <img src=\"/images/xfinity_tveverywhere.png\" alt=\"Watch at Xfinity\" title=\"Watch at Xfinity\">                </a>                <a href=\"http://watchabc.go.com/castle/SH559040/VDKA0_r3i1vgfy/once-upon-a-time-in-the-west\" title=\"ABC\">                    <img src=\"/images/abc_tveverywhere.png\" alt=\"Watch at ABC\" title=\"Watch at ABC\">                </a>                <a href=\"http://www.hulu.com/watch/714391\" title=\"Hulu Plus\">                    <img src=\"/images/hulu_plus.png\" alt=\"Watch at Hulu Plus\" title=\"Watch at Hulu Plus\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=25TCHIWRfqk&amp;cdid=tvseason-UMiCJW5hYE4heLOGS7dpXQ&amp;gdid=tvepisode-uEY90x6vUlo\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PNDQFH6?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>                <a href=\"http://click.linksynergy.com/fs-bin/click?id=eCa2jsTcHfQ&amp;subid=&amp;offerid=251672.1&amp;type=10&amp;tmpid=9417&amp;RD_PARM1=http%3A%2F%2Fwww.vudu.com%2Fmovies%2F%23%21content%2F580613\" title=\"VUDU\">                    <img src=\"/images/vudu.png\" alt=\"Watch at VUDU\" title=\"Watch at VUDU\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/once-upon-a-time-in-the-west/id911663938?i=942068026&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"https://www.youtube.com/watch?v=uEY90x6vUlo\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"231779\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 17, 2014\">Nov 17</span>        <div class=\"network\">FOX</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1606251&amp;api_user=130\" title=\"Gotham\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/Gotham/Gotham_S01E09_180x104.jpg\" title=\"Watch Gotham Season 1 Episode 9 - Harvey Dent\" alt=\"Watch Gotham Season 1 Episode 9 - Harvey Dent\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1606251&amp;api_user=130\" title=\"Gotham\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/Gotham\">Gotham</a>        </div>        <div class=\"infos\">            <a href=\"/show/Gotham/episode/S01E09\" title=\"Gotham 01x09\">Season 1 Episode 9: Harvey Dent</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/17/2014 on FOX            </div>            <span class=\"summary hidden-xs\">Trying to close the Wayne murder case, a young Harvey Dent and Gordon team up, much to Mayor James’ chagrin. Meanwhile, Penguin makes contact with Mooney’s secret weapon, Liza.</span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://www.hulu.com/watch/714601\" title=\"Hulu\">                    <img src=\"/images/hulu_free.png\" alt=\"Watch at Hulu\" title=\"Watch at Hulu\">                </a>                <a href=\"http://www.fox.com/watch/359616067864\" title=\"FOX\">                    <img src=\"/images/fox.png\" alt=\"Watch at FOX\" title=\"Watch at FOX\">                </a>                <a href=\"http://xfinitytv.comcast.net/watch/Gotham/6254733241694866112/359818307542/Harvey-Dent/videos\" title=\"Xfinity\">                    <img src=\"/images/xfinity_tveverywhere.png\" alt=\"Watch at Xfinity\" title=\"Watch at Xfinity\">                </a>                <a href=\"http://www.hulu.com/watch/714601\" title=\"Hulu Plus\">                    <img src=\"/images/hulu_plus.png\" alt=\"Watch at Hulu Plus\" title=\"Watch at Hulu Plus\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=qaagWmT4u8E&amp;cdid=tvseason-bCvCAdfPZDaKG6GneY96YA&amp;gdid=tvepisode-bemyhOvP02I\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/harvey-dent/id907275869?i=941943380&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"http://click.linksynergy.com/fs-bin/click?id=eCa2jsTcHfQ&amp;subid=&amp;offerid=251672.1&amp;type=10&amp;tmpid=9417&amp;RD_PARM1=http%3A%2F%2Fwww.vudu.com%2Fmovies%2F%23%21content%2F576327\" title=\"VUDU\">                    <img src=\"/images/vudu.png\" alt=\"Watch at VUDU\" title=\"Watch at VUDU\">                </a>                <a href=\"https://www.youtube.com/watch?v=bemyhOvP02I\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PS1JBQA?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"239991\">Mark as watched</a>    </div></li><li class=\"episode-queue episode-module row\">    <div class=\"column_date hidden-xs hidden-sm\">        <span title=\"Nov 16, 2014\">Nov 16</span>        <div class=\"network\">ABC</div>    </div>    <div class=\"column_poster\">        <a href=\"http://www.guidebox.com/watch.php?episode=1529492&amp;api_user=130\" title=\"Revenge\">            <img src=\"http://d2spmqy4pos7su.cloudfront.net/Revenge/Revenge_S04E08_180x104.jpg\" title=\"Watch Revenge Season 4 Episode 8 - Contact\" alt=\"Watch Revenge Season 4 Episode 8 - Contact\" style=\"height: 101px;\" border=\"0\" width=\"180\">        </a>        <a href=\"http://www.guidebox.com/watch.php?episode=1529492&amp;api_user=130\" title=\"Revenge\" class=\"video_overlay\" target=\"_blank\">            <img src=\"/css/images/play-video-icon.png\" border=\"0\">        </a>    </div>    <div class=\"column_infos\">        <div class=\"title \">            <a href=\"/show/Revenge\">Revenge</a>        </div>        <div class=\"infos\">            <a href=\"/show/Revenge/episode/S04E08\" title=\"Revenge 04x08\">Season 4 Episode 8: Contact</a>            <div class=\"visible-xs visible-sm xs-infos\">                11/16/2014 on ABC            </div>            <span class=\"summary hidden-xs\">Victorias future hangs in the balance as the FBI closes in and a mysterious new enemy strikes.</span>        </div>        <div class=\"links clearfix hidden-xs\">            <div class=\"videos\">                <a href=\"http://xfinitytv.comcast.net/watch/Revenge/7878683525689254112/358436931831/Contact/videos\" title=\"Xfinity\">                    <img src=\"/images/xfinity_tveverywhere.png\" alt=\"Watch at Xfinity\" title=\"Watch at Xfinity\">                </a>                <a href=\"http://watchabc.go.com/revenge/SH55126554/VDKA0_i1u9p9zh/contact\" title=\"ABC\">                    <img src=\"/images/abc_tveverywhere.png\" alt=\"Watch at ABC\" title=\"Watch at ABC\">                </a>                <a href=\"http://www.hulu.com/watch/714138\" title=\"Hulu Plus\">                    <img src=\"/images/hulu_plus.png\" alt=\"Watch at Hulu Plus\" title=\"Watch at Hulu Plus\">                </a>                <a href=\"https://www.youtube.com/watch?v=EA4N9cLaVa4\" title=\"YouTube\">                    <img src=\"/images/youtube_purchase.png\" alt=\"Watch at YouTube\" title=\"Watch at YouTube\">                </a>                <a href=\"http://click.linksynergy.com/fs-bin/click?id=eCa2jsTcHfQ&amp;subid=&amp;offerid=251672.1&amp;type=10&amp;tmpid=9417&amp;RD_PARM1=http%3A%2F%2Fwww.vudu.com%2Fmovies%2F%23%21content%2F580521\" title=\"VUDU\">                    <img src=\"/images/vudu.png\" alt=\"Watch at VUDU\" title=\"Watch at VUDU\">                </a>                <a href=\"https://itunes.apple.com/us/tv-season/contact/id911667320?i=941936612&amp;at=11l4Ma\" title=\"iTunes\">                    <img src=\"/images/itunes.png\" alt=\"Watch at iTunes\" title=\"Watch at iTunes\">                </a>                <a href=\"https://play.google.com/store/tv/show?id=cjqVr0guHrc&amp;cdid=tvseason-2SXomQqhqd4PzMhXLSDM2A&amp;gdid=tvepisode-EA4N9cLaVa4\" title=\"Google Play\">                    <img src=\"/images/google_play.png\" alt=\"Watch at Google Play\" title=\"Watch at Google Play\">                </a>                <a href=\"http://www.amazon.com/gp/product/B00PMLHNTS?tag=mytvrss-20\" title=\"Amazon\">                    <img src=\"/images/amazon_buy.png\" alt=\"Watch at Amazon\" title=\"Watch at Amazon\">                </a>            </div>        </div>        <a class=\"btn btn-default watch-episode-button\" episodeid=\"239630\">Mark as watched</a>    </div></li>");
            //resp.htmldoc = ht;
            //resp.firstNode = ht.DocumentNode;

            if (resp.somethingWentWrong)
            {
                return queue;
            }


            foreach (HtmlNode episode in resp.firstNode.ChildNodes)
            {
                if (episode.Name != "li") { continue; }

                queue.Add(Episode.getQueueEpisode(episode));
            }
            return queue;
        }

        public async Task<List<TvShow>> getTracker()
        {
            if (!hasInternet() && tracker != null)
                return tracker;
            tracker = new List<TvShow>();

            Response resp = await (new Response("http://followshows.com/viewStyleTracker?viewStyle=expanded")).call();
            if (resp.somethingWentWrong)
            {
                return tracker;
            }

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resp.page);

            HtmlNode tbody = doc.GetElementbyId("tracker");
            HtmlNode head = HTML.getChild(tbody);

            if (head == null)
            {
                return tracker;
            }

            foreach (HtmlNode tvshow in head.ChildNodes)
            {
                try
                {
                    TvShow show = new TvShow(true);
                    HtmlNode title = HTML.getChild(tvshow);
                    show.Name = System.Net.WebUtility.HtmlDecode(title.InnerText);
                    show.Image = new BitmapImage() { UriSource = new Uri(HTML.getAttribute(title.ChildNodes, "src")) };
                    show.showUrl = HTML.getAttribute(title.ChildNodes, "href").Replace("/show/", "");
                    show.stillToWatch = HTML.getChild(tvshow.ChildNodes, "class", "towatch").InnerHtml;
                    string perc = HTML.getChild(tvshow.ChildNodes, "role", "progressbar").InnerText.Replace("%", "");
                    show.percentageWatched = float.Parse(perc) / 100 * 150;
                    tracker.Add(show);
                }
                catch (Exception)
                {

                }
            }
            return tracker;
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

        public async Task store()
        {
            try
            {
                if (queue != null && queue.Count != 0)
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("queue.txt", CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(queue));
                }

                if (calendar != null && calendar.Count != 0)
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("calendar.txt", CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(calendar));
                }

                if (tracker != null && tracker.Count != 0)
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("tracker.txt", CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(tracker));
                }
            }
            catch (Exception)
            { }
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
                        if (com.episode.EpisodePos == ep.EpisodePos && com.episode.ShowName == ep.ShowName)
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
            StorageFolder temp = ApplicationData.Current.LocalFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.GetFileAsync("tracker.txt");

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);
                tracker = new List<TvShow>();

                tracker = JsonConvert.DeserializeObject<List<TvShow>>(text.ToString());
            }

            return tracker;
        }

        public async Task<List<Episode>> recoverCalendar()
        {
            StorageFolder temp = ApplicationData.Current.LocalFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.GetFileAsync("calendar.txt");
                calendar = new List<Episode>();

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);

                calendar = JsonConvert.DeserializeObject<List<Episode>>(text.ToString());

            }

            return calendar;
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