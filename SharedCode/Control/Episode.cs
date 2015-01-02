using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace SharedCode
{
    public class Episode : INotifyPropertyChanged
    {
        public string EpisodeName { get; set; }
        public DateTime airtime { get; set; }
        public DateTime airdate { get; set; }

        public string summary { get; set; }
        public string network { get; set; }

        public string ShowName { get; set; }

        public String imageUrl { get; set; }
        public BitmapImage Image { get; set; }

        public string EpisodePos { get; set; }

        public int ISeason { get; set; }
        public int IEpisode { get; set; }

        public string id { get; set; }
        public string url { get; set; }

        public Visibility redo { get; set; }
        public double Height { get; set; }
        public double Opacity { get; set; }

        public bool Aired { get; set; }

        public bool New { get; set; }

        private bool seen;
        public bool Seen
        {
            get
            { return seen; }
            set
            {
                if (value == true)
                {
                    seen = true;
                    Opacity = 0.2;
                    redo = Visibility.Visible;
                }
                else
                {
                    seen = false;
                    Opacity = 0.9;
                    redo = Visibility.Collapsed;
                }
            }
        }
        public Episode(bool AiredOnTV, bool SeenSomewhere)
        {
            Aired = AiredOnTV;
            Seen = SeenSomewhere;
            New = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public async Task markAsWatched()
        {
            if (!Aired)
            {
                Helper.message("This episode hasn't aired yet. You cannot mark it as watched", "EPISODE NOT AIRED");
                throw new Exception("IT DID NOT AIR");
            }
            Response resp = await (new Response("http://followshows.com/api/markEpisodeAsWatched?episodeId=" + id, null)).call();

        }

        public async Task markNotAsWatched()
        {
            Response resp = await (new Response("http://followshows.com/api/markEpisodeAsNotWatched?episodeId=" + id, null)).call();
        }



        /// <summary>
        /// Converts Html to a Episode object
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Episode getQueueEpisode(HtmlNode node)
        {
            Episode res = new Episode(true, false);
            if (HTML.getChild(node.ChildNodes, "class", "label label-warning") != null)
            {
                res.New = true;
            }

            HtmlNode netDate =  HTML.getChild(node);
            res.network = HTML.getAttribute(netDate.ChildNodes, "network");
            
            HtmlNode posterNode = HTML.getChild(node, "class", "column_poster");

            res.imageUrl = HTML.getAttribute(posterNode.ChildNodes, "src").Replace("180", "360").Replace("104", "207");
            try
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.UriSource = new Uri(res.imageUrl);
                bitmapImage.CreateOptions = BitmapCreateOptions.None;
                res.Image = bitmapImage;
            }
            catch { }

            HtmlNode rest = HTML.getChild(node.ChildNodes, "class", "column_infos");

            HtmlNode one = HTML.getChild(rest, "class", "title ");
            HtmlNode two = HTML.getChild(one);


            res.url = HTML.getAttribute(two, "href");
            res.ShowName = HTML.getChild(rest, "class", "title ").InnerText;

            HtmlNode locDateSum = HTML.getChild(rest, "class", "infos");

            string entire = HTML.getChild(locDateSum).InnerText;

            string[] split = entire.Split(':');
            string[] seasonEps = split[0].Replace("Season ", "").Replace("Episode ", "").Split(' ');
            res.ISeason = int.Parse(seasonEps[0]);
            res.IEpisode = int.Parse(seasonEps[1]);
            res.EpisodePos = "S" + res.ISeason + "E" + res.IEpisode;

            res.EpisodeName = split[1].Remove(0,1);

            HtmlNode dateNode = HTML.getChild(locDateSum.ChildNodes, "class", "visible-xs visible-sm xs-infos");
            split = dateNode.InnerText.Split(' ');
            try
            {
                res.airdate = DateTime.Parse(split[0]);
            }
            catch { }
            

            res.summary = HTML.getChild(locDateSum, 2).InnerText;

            res.id = HTML.getAttribute(node.ChildNodes, "episodeid");

            return res;
        }

        /// <summary>
        /// Converts Html to a Episode object
        /// DONT LOOK AT ME, I AM UGLY
        /// </summary>
        /// <param name="li"></param>
        /// <returns></returns>
        public static Episode getCalendarEpisode(HtmlNode li) {

            HtmlNode a = HTML.getChild(li);

            Episode ep = new Episode(false, false);
            ep.ShowName = HTML.getAttribute(a, "data-show");
            ep.network = HTML.getChild(a.ChildNodes, "class", "network").InnerText;
            ep.Image = new BitmapImage(new Uri(Regex.Match(HTML.getAttribute(HTML.getChild(a, "class", "poster"), "style"), @"(?<=\().+(?!\))").ToString().Replace(@");", "")));


            //Season Position and episode url
            HtmlDocument data = new HtmlDocument();
            data.LoadHtml(HTML.getAttribute(a, "data-content"));
            ep.EpisodeName = HTML.getChild(HTML.getChild(data.DocumentNode)).InnerText;
            string air = HTML.getChild(data.DocumentNode, 1).InnerText.Replace("Airs on ", "").Replace(", on " + ep.network + " at ", " ");
            try
            {
                ep.airtime = DateTime.Parse(air);
                ep.airdate = DateTime.Parse(air).Date;
            }
            catch (Exception) { }
            
            ep.url = HTML.getAttribute(HTML.getChild(HTML.getChild(data.DocumentNode, 2)), "href");

            string[] build = HTML.getChild(HTML.getChild(data.DocumentNode, 2)).InnerText.Split(' ');
            ep.ISeason = int.Parse(build[1]);
            ep.IEpisode = int.Parse(build[3]);
            ep.EpisodePos = "S" + build[1] + "E" + build[3].Replace(":", "");

            return ep;

        }

        public static Episode getWatchListEpisode(HtmlNode episode)
        {
            Episode res = new Episode(true, false);
            res.ShowName = HTML.getChild(episode.ChildNodes, "class", "title").InnerText;
            res.id = HTML.getAttribute(episode.ChildNodes, "episodeId");
            HtmlNode Ename = HTML.getChild(episode.ChildNodes, "class", "subtitle");
            res.EpisodeName = HTML.getChild(Ename).InnerText;
            res.Image = new BitmapImage(new Uri(HTML.getAttribute(episode.ChildNodes, "style").Replace("background-image: url(", "").Replace(");", "")));
            string[] build = HTML.getChild(episode.ChildNodes, "class", "description").InnerText.Split(new char[] { ' ' });
            res.ISeason = int.Parse(build[1]);
            res.IEpisode = int.Parse(build[3].Replace(".", ""));
            res.EpisodePos = "Season " + res.ISeason + " Episode " + res.IEpisode;
            
            return res;
        }
    }


}
