using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace SharedCode
{
    public class TvShow : INotifyPropertyChanged
    {
        public TvShow(bool follow)
        {
            following = follow;
            numberOfSeasons = 0;
            Followers = "0";
        }

        //Basic Stuff
        public string Name { get; set; }
        public BitmapImage Image { get; set; }
        public string showUrl { get; set; }
        public bool following { get; set; }

        //Tracker
        public float percentageWatched { get; set; }
        public string stillToWatch { get; set; }

        //ShowPage
        public List<Season> Season { get; set; }
        public string Genre { get; set; }
        public string Airs { get; set; }
        public string Followers { get; set; }
        public string Summary { get; set; }
        public string SummaryExtended { get; set; }
        public string Actors { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

        public int numberOfSeasons { get; set; }

        public async Task expand()
        {
            Response resp = await (new Response("http://followshows.com/show/" + showUrl, false)).call();
            if (resp.somethingWentWrong)
            {
                return;
            }

            HtmlNode summaryid = resp.htmldoc.GetElementbyId("summary");

            string extendedPart = HTML.getChild(summaryid.ChildNodes, "class", "details").InnerText;
            SummaryExtended = HTML.getChild(summaryid.ChildNodes, "class", "summary-text").InnerText.Replace("...&nbsp;(more)", "");

            Summary = SummaryExtended.Replace(extendedPart, "") + "...";



            HtmlNode showSummary = resp.htmldoc.GetElementbyId("content-about");
            Genre = HTML.getChild(showSummary.ChildNodes, "class", "genres").InnerText.Replace("GENRES:", "");
            Airs = HTML.getChild(showSummary.ChildNodes, "class", "infos col-xs-12 col-sm-6").InnerText.Replace("AIRS:", "");

            HtmlNode forFollowandName = HTML.getChild(showSummary.ChildNodes, "class", "summary");
            Followers = HTML.getChild(forFollowandName.ChildNodes, "class", "followers").InnerText.Replace(" followers)", "").Replace("( ","");
            Name = HTML.getChild(forFollowandName, 0).InnerText;

            HtmlNode season = resp.htmldoc.GetElementbyId("season-filter");
            if (season != null)
            {
                numberOfSeasons = season.ChildNodes.ToArray<HtmlNode>().Length;
            }

            HtmlNode actors = resp.htmldoc.GetElementbyId("actors");
            if (actors != null)
            {
                Actors = actors.InnerText;
            }
            else
            {
                Actors = "None";
            }
            OnPropertyChanged("");
        }

        public async Task follow()
        {
            if (showUrl == null) return;
            Response resp = await (new Response("http://followshows.com/api/followShow?show=" + showUrl, null)).call();

            if (resp.somethingWentWrong)
            {
                Helper.message("Uhm... Something went wrong.", "Sorry");
            }
        }

        public async Task unfollow()
        {
            if (showUrl == null) return;
            Response resp = await (new Response("http://followshows.com/api/unfollowShow?show=" + showUrl, null)).call();

            if (resp.somethingWentWrong)
            {
                Helper.message("Uhm... Something went wrong.", "Sorry");
            }
        }

        public async Task markSeasonAsWatched(string seasonnr)
        {
            if (seasonnr == null || showUrl == null) return;
            Response resp = await (new Response("http://followshows.com/api/markSeasonAsWatched?show=" + showUrl + "&season=" + seasonnr, null)).call();

            if (resp.somethingWentWrong)
            {
                Helper.message("Uhm... Something went wrong.", "Sorry");
            }
        }

        //Property changed
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class ShowTvShow : TvShow
    {
        public ShowTvShow(bool follow)
            : base(follow)
        { }

        public string NameCaps
        {
            get
            {
                return Name.ToUpper();
            }

        }
    }
}
