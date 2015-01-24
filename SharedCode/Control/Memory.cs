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

        public static async void StoreTracker(Tracker track, EventArgs e)
        {
            List<TvShow> tracker = track.tracker;
            if (tracker != null && tracker.Count != 0)
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.CreateFileAsync("tracker.txt", CreationCollisionOption.ReplaceExisting);

                await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(tracker));
            }
        }

        public static async void store(Queue q)
        {
            try
            {
                ObservableCollection<Episode> queue = q.getQueue();
                if (queue != null && queue.Count != 0)
                {
                    StorageFolder temp = ApplicationData.Current.LocalFolder;
                    StorageFile fil = await temp.CreateFileAsync("queue.txt", CreationCollisionOption.ReplaceExisting);

                    await Windows.Storage.FileIO.WriteTextAsync(fil, JsonConvert.SerializeObject(queue));
                }
            }
            catch (Exception)
            { }
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

        public static async Task<List<Episode>> recoverQueue()
        {
            List<Episode> queue = new List<Episode>();

            StorageFolder temp = ApplicationData.Current.LocalFolder;

            IReadOnlyList<IStorageItem> tempItems = await temp.GetItemsAsync();
            if (tempItems.Count > 0)
            {
                StorageFile fil = await temp.GetFileAsync("queue.txt");

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);

                text.ToString();

                queue = JsonConvert.DeserializeObject<List<Episode>>(text.ToString());

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
                StorageFile fil = await temp.GetFileAsync("tracker.txt");

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
                StorageFile fil = await temp.GetFileAsync("calendar.txt");
                calendar = new List<Episode>();

                string text = await Windows.Storage.FileIO.ReadTextAsync(fil);

                calendar = JsonConvert.DeserializeObject<List<Episode>>(text.ToString());

            }

            return calendar;
        }
    }
}
