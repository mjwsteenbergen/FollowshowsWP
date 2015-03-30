using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace SharedCode
{
    public class Queue : ObservableCollection<Episode>
    {
        API api;
        int count = 0;

        bool downloading;

        public Queue()
        {
            api = API.getAPI();
            //items = new ObservableCollection<Episode>();
            downloading = false;
        }

        //public ObservableCollection<Episode> getQueue()
        //{
        //    if(items == null || items.Count == 0)
        //    {
        //        return items;
        //    }

        //    return items;
        //}

        public async Task download()
        {
            if (! await api.hasInternet() || this == null)
                return;
            while(Count != 0)
            {
                RemoveAt(0);
            }
            

            Response resp = await (new Response("http://followshows.com/api/queue?from=0")).call();

            count += 10;

            if (resp.somethingWentWrong)
            {
                return;
            }


            foreach (HtmlNode episode in resp.firstNode.ChildNodes)
            {
                if (episode.Name != "li") { continue; }

                Add(Episode.getQueueEpisode(episode));
            }

            return;
        }

        public async Task downloadMoreEpisodes()
        {
            if(downloading)
            {
                return;
            }
            downloading = true;
            count += 10;
            Response resp = await (new Response("http://followshows.com/api/queue?from=" + count.ToString())).call();

            if (resp.somethingWentWrong)
            {
                return;
            }


            foreach (HtmlNode episode in resp.firstNode.ChildNodes)
            {
                if (episode.Name != "li") { continue; }

                Add(Episode.getQueueEpisode(episode));
            }
            downloading = false;
        }
        
        //public void setQueue(ObservableCollection<Episode> newQ)
        //{
        //    items = newQ;
        //}

    }
}
