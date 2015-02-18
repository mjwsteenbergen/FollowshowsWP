using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace SharedCode
{
    public class Tracker
    {
        public ObservableCollection<TvShow> tracker = new ObservableCollection<TvShow>();
        API api;

        public Tracker()
        {
            api = API.getAPI();
        }

        public ObservableCollection<TvShow> getTracker()
        {
            return tracker;
        }

        public async Task<ObservableCollection<TvShow>> load()
        {
            if (! await api.hasInternet())
                return tracker;

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

                    try
                    {
                        BitmapImage bimp = new BitmapImage() { UriSource = new Uri(HTML.getAttribute(title.ChildNodes, "src")) };
                        bimp.CreateOptions = BitmapCreateOptions.None;
                        show.Image = bimp;
                    }
                    catch { }
                    show.showUrl = HTML.getAttribute(title.ChildNodes, "href").Replace("/show/", "");
                    show.stillToWatch = HTML.getChild(tvshow.ChildNodes, "class", "towatch").InnerHtml;
                    string perc = HTML.getChild(tvshow.ChildNodes, "role", "progressbar").InnerText.Replace("%", "");
                    show.percentageWatched = float.Parse(perc) / 100 * 150;
                    tracker.Add(show);
                }
                catch (Exception e)
                {
                    Memory.writeErrorToFile(this, e);
                }
            }
            return tracker;
        }
    }
}
