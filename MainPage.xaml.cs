using Followshows.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Security.Credentials;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using System.Runtime.Serialization.Json;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Followshows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private API api;

        //Mark as watched
        private Grid item;
        private Episode ep;

        private int selectedPivot;

        public MainPage()
        {
            this.InitializeComponent();

            CommandBar bar = new CommandBar();
            AppBarButton logou = new AppBarButton(){ Icon=  new SymbolIcon(Symbol.Cancel), Label="Log out"};
            logou.Click += logout;
            AppBarButton refr= new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:Assets/appbar.refresh.png") }, Label = "Refresh" };
            refr.Click += refresh;
            bar.PrimaryCommands.Add(logou);
            bar.PrimaryCommands.Add(refr);
            bar.ClosedDisplayMode =  AppBarClosedDisplayMode.Minimal;

            BottomAppBar = bar;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            selectedPivot = 0;

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
            ////Loading
            if(e.NavigationParameter != null)
            {
                api = (API)e.NavigationParameter;
            }
            else
            {
                if(api == null)
                {
                    throw new Exception("There is no api defined");
                }
            }

            TvShow show = new TvShow();
            api.passed = show;
            if (api.hasInternet())
            {
                ////Load Queue
                List<Episode> queue = await api.getQueue();
                if (queue != null)
                {
                    lijst.ItemsSource = queue;
                }

                //Load Tracker
                List<TvShow> track = await api.getTracker();
                if (track != null)
                {
                    tracker.ItemsSource = track;
                }
            }
            else
            {
                api.getNetwork().PropertyChanged += NetworkStatus_Changed;
            }
           

            //lijst.SelectionMode = ListViewSelectionMode.Multiple;


        }

        async void NetworkStatus_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.refresh(null,null);
            });

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

        #region MarkAsWatched


        private void Item_Tapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            item = sender as Grid;
            ep = item.DataContext as Episode;

            Image ima = item.FindName("ima") as Image;
            if (ima.Opacity > 0.5)
            {
                if (!ep.Aired)
                {
                    Helper.message("This episode hasn't aired yet. You cannot mark it as watched", "EPISODE NOT AIRED");
                    return;
                }

                DoubleAnimation ani = new DoubleAnimation();
                Storyboard.SetTarget(ani, ima);

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

                DoubleAnimation ani = new DoubleAnimation();
                Storyboard.SetTarget(ani, ima);

                Storyboard board = new Storyboard();
                Storyboard.SetTargetProperty(ani, "Opacity");
                ani.To = 0.9;
                board.Duration = new Duration(TimeSpan.FromSeconds(1));
                board.Children.Add(ani);

                board.Begin();

                ep.Seen = false;

                item.DataContext = null;
                item.DataContext = ep;

                api.markNotAsWatched(ep);
            }



        }

        void board_Completed(object sender, object e)
        {

            api.markAsWatched(ep);
            
            ep.Seen = true;

            item.DataContext = null;
            item.DataContext = ep;
        }

        #endregion

        private void Logout(object sender, TappedRoutedEventArgs e)
        {
            PasswordVault vault = new PasswordVault();
            vault.Remove(vault.FindAllByResource("email")[0]);
            api.refresh();
            Frame rootFrame = Window.Current.Content as Frame;
            if (!rootFrame.Navigate(typeof(MainPage), api))
            {
                throw new Exception("Failed to create initial page");
            }
        }

        private void logout(object sender, RoutedEventArgs e)
        {
            PasswordVault vault = new PasswordVault();
            vault.Remove(vault.FindAllByResource("email")[0]);
            api.refresh();
            Frame rootFrame = Window.Current.Content as Frame;
            if (!rootFrame.Navigate(typeof(LandingPage), api))
            {
                throw new Exception("Failed to create initial page");
            }
        }

        private void pivotItem_Changed(object sender, SelectionChangedEventArgs e)
        {
            Pivot page = sender as Pivot;
            selectedPivot = page.SelectedIndex;
            var hi = e;
        }

        public async void refresh(object sender, RoutedEventArgs e)
        {
            switch (selectedPivot)
            {
                case 0:
                    List<Episode> queue = await api.getQueue();
                    if (queue != null)
                    {
                        lijst.ItemsSource = queue;
                    }
                    break;
                case 1:
                    List<TvShow> track = await api.getTracker();
                    if (track != null)
                    {
                        tracker.ItemsSource = track;
                    }
                    break;
            }

        }

        private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Image im = sender as Image;
            ImageSource source = im.Source;
            Episode twee = im.DataContext as Episode;
            if (twee.Image.UriSource.ToString().Contains("360"))
            {
                twee.Image.UriSource = new Uri(twee.Image.UriSource.ToString().Replace("360", "180").Replace("207", "104"));
            }
            else
            {
                Uri a = new Uri("ms-appx:Assets/basicQueueItem.bmp");
                im.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(a);
                im.Opacity = 1;
                im.Stretch = Stretch.Fill;
            }

        }

        private void trackerItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Grid gr = sender as Grid;
            api.passed = gr.DataContext;
            Frame rootFrame = Window.Current.Content as Frame;
            if (!rootFrame.Navigate(typeof(ShowPage), api))
            {
                throw new Exception("Failed to create initial page");
            }
        }
    }
}
