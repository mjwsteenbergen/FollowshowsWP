using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Web.Http;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using SharedCode;
using System.Collections.ObjectModel;

namespace BackGroundTask
{
    public sealed class Backgroud : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral def = taskInstance.GetDeferral();
            int tileCount = 0;

            API api = API.getAPI();
            try
            {
                if (await api.login())
                {

                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = null;
                    foreach(StorageFile fil2 in await temp.GetFilesAsync())
                    {
                        string name = fil2.DisplayName;
                        if(name == "lastQueueEpisode")
                        {
                            fil = fil2;
                        }
                    }
                    if (fil == null)
                    {
                        fil = await temp.CreateFileAsync("lastQueueEpisode.txt", CreationCollisionOption.ReplaceExisting);
                    }


                    string previous = await Windows.Storage.FileIO.ReadTextAsync(fil);



                    Queue q = new Queue();
                    ObservableCollection<Episode> ep = q.getQueue();
                    await q.downloadQueue();
                    foreach (Episode epi in ep)
                    {
                        if (epi.New)
                        {
                            tileCount++;
                        }
                    }
                    foreach (Episode epi in ep)
                    {
                        if (epi.EpisodeName == previous || previous == "")
                            break;
                        tileCount++;
                    }

                    await Windows.Storage.FileIO.WriteTextAsync(fil, ep[0].EpisodeName);

                    Tracker track = new Tracker();

                    track.trackEvent += Memory.StoreTracker;

                    Memory.store(q);
                    
                    
                }
                //bool b = await api.login();
            }
            catch(Exception e)
            {
                api.writeErrorToFile(this, e);
                return;
            }

            Tile.add(tileCount);

            

            def.Complete();
        }
    }
}
