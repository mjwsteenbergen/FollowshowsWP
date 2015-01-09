using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedCode
{
    public class Queue
    {

        List<Episode> queue;
        API api = API.getAPI();
        int count = 10;

        public Queue()
        {

        }

        public async Task<List<Episode>> getQueue()
        {
            if (!api.hasInternet() && queue != null)
                return queue;
            queue = new List<Episode>();

            Response resp = await (new Response("http://followshows.com/api/queue?from=0")).call();


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
        

    }
}
