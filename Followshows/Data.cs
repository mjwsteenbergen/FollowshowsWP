using Followshows;
using Followshows.almostApi;
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

public class Season
{
    public List<Episode> season;
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

public class SearchResult
{
    public string id { get; set; }
    public string label { get; set; }
    public string value { get; set; }
    public string type { get; set; }
    public string category { get; set; }
    public string image { get; set; }
    public bool poster { get; set; }
    public bool followed { get; set; }    
}

public class Command
{
    public Episode episode { get; set; }
    public bool watched { get; set; }
}
