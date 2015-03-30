using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SharedCode
{
    public class Memory
    {
        public static async void store(Tracker originalTracker)
        {
            ObservableCollection<TvShow> tracker = originalTracker.tracker;
            if (tracker != null && tracker.Count != 0)
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.CreateFileAsync("tracker.txt", CreationCollisionOption.ReplaceExisting);

                await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(tracker));
            }
        }

        public static async void store(Queue queue)
        {
            try
            {
                if (queue != null && queue.Count != 0)
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("queue.txt", CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(queue));
                }
            }
            catch (Exception e)
            {
                Memory.writeErrorToFile("Memory.Store", e).RunSynchronously();
            }
        }

        public static async void storeOffline(ObservableCollection<Episode> queue)
        {
            try
            {
                if (queue != null && queue.Count != 0)
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("queueOffline.txt", CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(queue));
                }
            }
            catch (Exception e)
            {
                Memory.writeErrorToFile("Memory.Queue.StoreOffline", e).RunSynchronously();
            }
        }

        public static async void store(List<Episode> calendar)
        {
            if (calendar != null && calendar.Count != 0)
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.CreateFileAsync("calendar.txt", CreationCollisionOption.ReplaceExisting);

                await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(calendar));
            }
        }

        public static async Task storeMostRecentQueueEpisode(Queue q)
        {

            foreach(Episode ep in q)
            {
                if(!ep.Seen)
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("lastQueueEpisode", CreationCollisionOption.ReplaceExisting);
                    await Windows.Storage.FileIO.WriteTextAsync(fil, ep.EpisodeName);
                    return;
                }
            }
            
        }

        public static async Task<string> getMostRecentQueueEpisode()
        {
            StorageFolder temp = ApplicationData.Current.LocalFolder;
            StorageFile fil = await temp.CreateFileAsync("lastQueueEpisode", CreationCollisionOption.OpenIfExists);

            return await Windows.Storage.FileIO.ReadTextAsync(fil);
        }

        public static async Task<ObservableCollection<Episode>> recoverQueue()
        {
            ObservableCollection<Episode> queue = new ObservableCollection<Episode>();


            StorageFolder temp = ApplicationData.Current.LocalFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil              =  await temp.CreateFileAsync("queue.txt",        CreationCollisionOption.OpenIfExists);
                StorageFile offlineQueueFile =  await temp.CreateFileAsync("queueOffline.txt", CreationCollisionOption.OpenIfExists);

                string text        = await Windows.Storage.FileIO.ReadTextAsync(fil);
                string offlineQuee = await Windows.Storage.FileIO.ReadTextAsync(offlineQueueFile);

                ObservableCollection<Episode> OriginalQueue = JsonConvert.DeserializeObject<ObservableCollection<Episode>>(text.ToString());
                ObservableCollection<Episode> offlineQueue = JsonConvert.DeserializeObject<ObservableCollection<Episode>>(offlineQuee.ToString());

                if (offlineQueue != null)
                {
                    queue = offlineQueue;
                    foreach (Episode e in OriginalQueue)
                    {
                        queue.Add(e);
                    }
                }
                else
                {
                    if (OriginalQueue != null)
                    {
                        queue = OriginalQueue;
                    }
                }

                

                List<Command> comList = await (API.getAPI()).getCommands();

                foreach (Command com in comList)
                {
                    foreach (Episode ep in queue)
                    {
                        if (com.episode.EpisodePos == ep.EpisodePos && com.episode.ShowName == ep.ShowName)
                        {
                            ep.Seen = com.watched;
                        }
                    }
                }
            }
            return queue;
        }

        public static async Task<List<TvShow>> recoverTracker()
        {
            List<TvShow> tracker = new List<TvShow>();

            StorageFolder temp = ApplicationData.Current.LocalFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.CreateFileAsync("tracker.txt",CreationCollisionOption.OpenIfExists);

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);
                tracker = new List<TvShow>();

                tracker = JsonConvert.DeserializeObject<List<TvShow>>(text.ToString());
            }

            return tracker;
        }

        public static async Task<List<Episode>> recoverCalendar()
        {
            List<Episode> calendar = new List<Episode>();

            StorageFolder temp = ApplicationData.Current.LocalFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.CreateFileAsync("calendar.txt",CreationCollisionOption.OpenIfExists);
                calendar = new List<Episode>();

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);

                calendar = JsonConvert.DeserializeObject<List<Episode>>(text.ToString());

            }

            return calendar;
        }


        public static StorageFolder sdFolder;
        public static StorageFile fil;
        public static bool debug = false;


        public static async Task setSdFolder()
        {
            if(debug)
            {

            }
            sdFolder = (await Windows.Storage.KnownFolders.RemovableDevices.GetFoldersAsync() as IReadOnlyList<StorageFolder>).FirstOrDefault();
            if (sdFolder == null) return;
            fil = await (await sdFolder.CreateFolderAsync("Followshows", CreationCollisionOption.OpenIfExists)).CreateFileAsync("FollowshowsCrash.txt", CreationCollisionOption.OpenIfExists);
        }

        public static async Task writeErrorToFile(object sender, Exception e)
        {
            string s = "Type: " + e.GetType().Name + "\n"
                    + "Message: " + e.Message + "\n" 
                    + "====  Full Stacktrace: ====\n" 
                    + e.StackTrace + "\n" 
                    + "==== End ====";
            await logToFile(sender, s);
            
        }

        public static async Task logToFile(object sender, String message)
        {
            if (sdFolder == null) return;
            try {
                await Windows.Storage.FileIO.AppendTextAsync(fil, "\n"
                    + "== " + DateTime.Now.ToString() + " ==\n"
                    + "Location: " + sender.ToString() + "\n"
                    + "== Message ==" + "\n" + message);
            } catch {
                
            }
             
        }

        public static void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            writeErrorToFile(sender, e.Exception).RunSynchronously();
        }
    }
}
