using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;

namespace BackGroundTask 
{
    public class Episode : INotifyPropertyChanged
    {
        public string EpisodeName { get; set; }
        public DateTime airtime { get; set; }
        public DateTime airdate { get; set; }

        public string summary { get; set; }
        public string network { get; set; }

        public string ShowName { get; set; }


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
    }

    public class Season
    {
        public List<Episode> season;
    }

    public class TvShow
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

        public override sealed string ToString()
        {
            return Name.ToString();
        }

        public string NameCaps
        {
            get
            {
                return Name.ToUpper();
            }

        }

        public int numberOfSeasons { get; set; }
    }

    public sealed class ShowTvShow : TvShow
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

    public sealed class Response
    {
        public bool hasInternet { get; set; }
        public string page { get; set; }
        public HttpResponseMessage content { get; set; }
    }

    public sealed class NetworkChanged : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public sealed class SearchResult
    {
        public string id { get; set; }
        public string label { get; set; }
        public string value { get; set; }
        public string type { get; set; }
        public string image { get; set; }
        public bool poster { get; set; }
        public bool followed { get; set; }
    }

    public sealed class Command
    {
        public Episode episode { get; set; }
        public bool watched { get; set; }
    }

}

