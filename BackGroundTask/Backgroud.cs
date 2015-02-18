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
                    
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("lastQueueEpisode",CreationCollisionOption.OpenIfExists);

                    string previous = await Windows.Storage.FileIO.ReadTextAsync(fil);

                    Queue q = new Queue();
                    ObservableCollection<Episode> ep = q.getQueue();
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


                    await Windows.Storage.FileIO.WriteTextAsync(fil, ep[0].EpisodeName);
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
