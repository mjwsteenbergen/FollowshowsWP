using Followshows;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

public class Episode
{
    public string SeriesName { get; set; }
    public string date { get; set; }
    public string network { get; set; }
    public BitmapImage Image { get; set; }

    public string EpisodePos { get; set; }
    public string ISeason { get; set; }

    public string IEpisode { get; set; }

    public string EpisodeName { get; set; }

    public string summary { get; set; }

    public string id { get; set; }

    public Visibility redo { get; set; }
    public double Height { get; set; }

    public double Opacity { get; set; }

    public Episode()
    {
        redo = Visibility.Collapsed;
        Opacity = 0.9;
    }
}

public class Season
{
    public List<Episode> season;
}

public class TvShow
{
    public TvShow()
    {
        numberOfSeasons = 0;
        Followers = "0";
    }
    //Basic Stuff
    public string Name { get; set; }
    public BitmapImage Image { get; set; }
    public string showUrl { get; set; }

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
    
    public override string ToString(){
        return Name.ToString();
    }


    public int numberOfSeasons { get; set; }
}

public class Response
{
    public bool hasInternet { get; set; }
    public string page { get; set; }
    public HttpContent content { get; set; }
}

public class NetworkChanged : INotifyPropertyChanged
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
