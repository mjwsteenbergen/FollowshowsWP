using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Followshows.almostApi
{
    public class Episode : INotifyPropertyChanged
    {
        public string EpisodeName { get; set; }
        public DateTime airtime { get; set; }
        public DateTime airdate { get; set; }

        public string summary { get; set; }
        public string network { get; set; }

        public string ShowName { get; set; }


        public BitmapImage Image { get; set; }

        public string EpisodePos { get; set; }

        public int ISeason { get; set; }
        public int IEpisode { get; set; }

        public string id { get; set; }
        public string url { get; set; }

        public Visibility redo { get; set; }
        public double Height { get; set; }
        public double Opacity { get; set; }

        public bool Aired { get; set; }

        private bool seen;
        public bool Seen
        {
            get
            { return seen; }
            set
            {
                if (value == true)
                {
                    seen = true;
                    Opacity = 0.2;
                    redo = Visibility.Visible;
                }
                else
                {
                    seen = false;
                    Opacity = 0.9;
                    redo = Visibility.Collapsed;
                }
            }
        }
        public Episode(bool AiredOnTV, bool SeenSomewhere)
        {
            Aired = AiredOnTV;
            Seen = SeenSomewhere;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public async Task markAsWatched()
        {
            if (!Aired)
            {
                Helper.message("This episode hasn't aired yet. You cannot mark it as watched", "EPISODE NOT AIRED");
                throw new Exception("IT DID NOT AIR");
            }
            Response resp = await (new Response("http://followshows.com/api/markEpisodeAsWatched?episodeId=" + id, null)).call();

        }

        public async Task markNotAsWatched()
        {
            Response resp = await (new Response("http://followshows.com/api/markEpisodeAsNotWatched?episodeId=" + id, null)).call();
        }
    }


}
