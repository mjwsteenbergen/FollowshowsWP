using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using SharedCode;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Imaging;
using System.Collections;
using System.IO;

namespace BackGroundTask
{
    public sealed class Backgroud : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var def = taskInstance.GetDeferral();
            await Memory.setSdFolder();
            int tileCount = 0;

            API api = API.getAPI();
            try
            {
                if (await api.login())
                {

                    string previous = await Memory.getMostRecentQueueEpisode();

                    Queue q = new Queue();
                    ObservableCollection<Episode> ep = q;
                    ObservableCollection<Episode> newEpisodes = new ObservableCollection<Episode>();
                    await q.download();
                    //foreach (Episode epi in ep)
                    //{
                        //if (epi.New)
                        //{
                            //tileCount++;
                            //newEpisodes.Add(epi);
                        //}

                    //}

                    

                    foreach (Episode epi in ep)
                    {
                        if (epi.EpisodeName == previous)
                            break;
                        tileCount++;
                        newEpisodes.Add(epi);
                        await Memory.logToFile(this, "I found a new episode " + epi.EpisodeName + " replaces " + previous);
                    }


                    
                    await Memory.storeMostRecentQueueEpisode(q);
                    Memory.storeOffline(newEpisodes);
                }
            }
            catch(Exception e)
            {
                Memory.writeErrorToFile(this, e).RunSynchronously();
            }



            Tile.add(tileCount);

            def.Complete();
        }
    }
}
