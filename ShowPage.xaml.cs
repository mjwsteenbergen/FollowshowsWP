using Followshows.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Followshows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShowPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        public TvShow Show;
        private API api;

        private bool summaryExtended = false;

        //Mark as watched
        private Grid item;
        private Episode ep;

        public ShowPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            //Loading
            api = (API)e.NavigationParameter;
            Show = await api.getShow(api.passed as TvShow);
            //Show.Image.UriSource = new Uri(Show.Image.UriSource.ToString().Replace("80", "357").Replace("112", "500"));
            //Show.Image =
            Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(Show.Image.UriSource.ToString().Replace("80", "357").Replace("112", "500")));
            this.DataContext = Show;

            NTW.DataContext = new Episode() { redo = Windows.UI.Xaml.Visibility.Collapsed };
            List<Episode> wl = await api.getWatchList();
            if(wl != null)
            {
                foreach(Episode ep in wl)
                {
                    if(ep.SeriesName == Show.Name)
                    {
                        NTW.DataContext = ep;
                        break;
                    }
                }
            
            }
            if(Show.numberOfSeasons > 0)
            {
                for(int i=Show.numberOfSeasons; i>0; i--)
                {
                    PivotItem item = new PivotItem();
                    item.Header = "Season " + i;
                    List<Episode> season = await api.getSeason(Show, i);
                    //List<Episode> season = await api.getQueue();
                    GridView view = new GridView();
                    view.ItemTemplate = Resources["seasy"] as DataTemplate;
                    view.ItemsSource = season;
                    item.Content = view;
                    item.ApplyTemplate();
                    Pivot.Items.Add(item);
                }
            }
            
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image.Source = Show.Image;
        }

        private void Tapped_ShowFullText(object sender, TappedRoutedEventArgs e)
        {
            if(summaryExtended)
            {
                Summary.Text = Show.Summary;
                summaryExtended = false;
            }
            else
            {
                Summary.Text = Show.SummaryExtended;
                summaryExtended = true;
            }
            
        }

        #region MarkAsWatched


        private void Item_Tapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            item = sender as Grid;
            ep = item.DataContext as Episode;

            Grid ima = item.FindName("ima") as Grid;
            if (ima.Opacity == 1)
            {
               

                DoubleAnimation ani = new DoubleAnimation();
                Storyboard.SetTarget(ani, item.Children.ToArray<Object>()[0] as Grid);

                Storyboard board = new Storyboard();
                board.Completed += board_Completed;
                Storyboard.SetTargetProperty(ani, "Opacity");
                ani.To = 0.2;
                ep.Opacity = 0.2;
                board.Duration = new Duration(TimeSpan.FromSeconds(1));
                board.Children.Add(ani);

                board.Begin();
                
            }
            else
            {
                item = sender as Grid;
                ep = item.DataContext as Episode;

                ep.redo = Visibility.Collapsed;

                item.DataContext = null;
                item.DataContext = ep;

                DoubleAnimation ani = new DoubleAnimation();
                Storyboard.SetTarget(ani, item.Children.ToArray<Object>()[0] as Grid);

                Storyboard board = new Storyboard();
                Storyboard.SetTargetProperty(ani, "Opacity");
                ani.To = 1;
                board.Duration = new Duration(TimeSpan.FromSeconds(1));
                board.Children.Add(ani);

                board.Begin();

                api.markNotAsWatched(ep);
            }
            

            
        }

        void board_Completed(object sender, object e)
        {
            ep.redo = Visibility.Visible;

            api.markAsWatched(ep);
            item.DataContext = null;
            item.DataContext = ep;
        }

        #endregion
    }
}
