using Followshows.Common;
using SharedCode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.Popups;
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
        private CommandBar bar;
        private int currentPivot = 0;


        private bool summaryExtended = false;

        List<Episode>[] season;

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

            //Create a fake show, which isn't visible to decrease uglyness
            //NTW.DataContext = new Episode(false, true) { redo = Windows.UI.Xaml.Visibility.Collapsed };

            api = API.getAPI();
            //Show = (api.passed as ShowTVShow);
            Show = await api.getShow(api.passed as TvShow);

            //if(Show.following)
            //{
            //    followColor.Fill = ((SolidColorBrush)App.Current.Resources["PhoneAccentBrush"]);
            //}

            //Create a new commandbar and add buttons
            bar = new CommandBar();
            AppBarButton follow = new AppBarButton() { Icon = new SymbolIcon(Symbol.Favorite), Label = "Follow" };
            follow.Click += Tapped_Favorite;
            

            AppBarButton unfollow = new AppBarButton() { Icon = new SymbolIcon(Symbol.UnFavorite), Label = "Unfollow" };
            unfollow.Click += Tapped_Favorite;
            unfollow.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            

            AppBarButton seen = new AppBarButton() { Icon = new SymbolIcon(Symbol.Accept), Label = "Mark all as seen" };
            seen.Click += markAsSeen_Click;
            seen.Visibility = Windows.UI.Xaml.Visibility.Collapsed;            

            bar.PrimaryCommands.Add(seen);
            bar.PrimaryCommands.Add(follow);
            bar.PrimaryCommands.Add(unfollow);

            setFollowingAppButton();

            bar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            BottomAppBar = bar;

            Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(Show.Image.UriSource.ToString().Replace("80", "357").Replace("112", "500")));
            this.DataContext = Show;

        //    NTWtitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

        //    List<Episode> wl = await api.getWatchList();
        //    if(wl != null)
        //    {
        //        foreach(Episode ep in wl)
        //        {
        //            if(ep.SeriesName == Show.Name)
        //            {
        //                NTW.DataContext = ep;
        //                NTWtitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
        //                break;
        //            }
        //        }
            
        //    }
            if (Show.numberOfSeasons > 0)
            {
                season = new List<Episode>[Show.numberOfSeasons + 1];
                //For every show create a new pivot with a gridview with the episode-itemtemplate;
                for (int i = Show.numberOfSeasons; i > 0; i--)
                {
                    PivotItem item = new PivotItem();
                    item.Header = "Season " + i;
                    List<Episode> episodelist = await api.getSeason(Show, i);
                    season[i] = episodelist;
                    GridView view = new GridView();

                    view.ItemTemplate = Resources["seasy"] as DataTemplate;
                    view.ItemsSource = episodelist;
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

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivo = sender as Pivot;
            currentPivot = pivo.SelectedIndex;

            //if (bar == null)
            //    return;
            if (currentPivot == 0)
            {
                pivo.Margin = new Thickness(0, -40, 0, 0);
                if (BottomAppBar == null)
                    return;
                
                //If we are on the first pivot, show only the mark all as seen button
                (bar.PrimaryCommands[0] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                setFollowingAppButton();
                BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            }
            else
            {
                pivo.Margin = new Thickness(0, 0, 0, 0);
                (bar.PrimaryCommands[0] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Visible;
                (bar.PrimaryCommands[1] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                (bar.PrimaryCommands[2] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        private void setFollowingAppButton()
        {
            (bar.PrimaryCommands[1] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (!Show.following)
            {
                (bar.PrimaryCommands[1] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Visible;
                //amIFollowing.Text = "Not Yet...";
            }

            (bar.PrimaryCommands[2] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (Show.following)
            {
                (bar.PrimaryCommands[2] as AppBarButton).Visibility = Windows.UI.Xaml.Visibility.Visible;
                //amIFollowing.Text = "Yes you are";
            }
        }

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            ScrollViewer view = sender as ScrollViewer;
            if (e.FinalView.VerticalOffset > 20)
            {
                view.Padding = new Thickness(0,0,0,0.5);
                BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                view.Padding = new Thickness(0, 0, 0, 0);
                BottomAppBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        private async void markAsSeen_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog dia = new MessageDialog("Mark season as watched?", "Are you sure?");
            dia.Commands.Add(new UICommand("Ok"));
            dia.Commands.Add(new UICommand("Cancel"));
            IUICommand res = await dia.ShowAsync();

            //if ok pressed
            if (res.Label == "Ok")
            {
                PivotItem pivoi = Pivot.Items[currentPivot] as PivotItem;
                string seasonnr = pivoi.Header.ToString().Replace("Season ", "");

                foreach (Episode epi in season[Int32.Parse(seasonnr)])
                {
                    if (epi.Aired)
                    {
                        epi.Seen = true;
                        epi.OnPropertyChanged("redo");
                        epi.OnPropertyChanged("Opacity");
                    }
                }
                api.markSeasonAsWatched(seasonnr, Show);
            }
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
            //If the image failed to load, return load the smaller resolution image.
            Image.Source = Show.Image;
            

            Image im = sender as Image;
            ImageSource source = im.Source;
            Episode twee = im.DataContext as Episode;
            string Imsource = twee.Image.UriSource.ToString();
            if (Imsource.Contains("360") || Imsource.Contains("357"))
            {
                twee.Image.UriSource = new Uri(Imsource.Replace("360x207", "130x75").Replace("357x500","30x42"));
            }
            else
            {
                Uri a = new Uri("ms-appx:///Assets/basicQueueItem.bmp");
                im.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(a);
                im.Opacity = 1;
                im.Stretch = Stretch.Fill;
            }
        }

        private void Tapped_ShowFullText(object sender, TappedRoutedEventArgs e)
        {
            if (summaryExtended)
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

                DoubleAnimation ani = new DoubleAnimation();
                Storyboard.SetTarget(ani, ima);

                Storyboard board = new Storyboard();
                Storyboard.SetTargetProperty(ani, "Opacity");
                ani.To = 0.9;
                board.Duration = new Duration(TimeSpan.FromSeconds(1));
                board.Children.Add(ani);

                board.Begin();

                ep.Seen = false;

                ep.OnPropertyChanged("redo");
                ep.OnPropertyChanged("Opacity");

                if (api.hasInternet())
                {
                    api.markNotAsWatched(ep);
                }
                else
                {
                    Command com = new Command();
                    com.episode = ep;
                    com.watched = false;

                    api.addCommand(com);

                }


            }



        }

        void board_Completed(object sender, object e)
        {
            if (api.hasInternet())
            {
                api.markAsWatched(ep);
            }
            else
            {
                api.addCommand(new Command() { episode = ep, watched = true });
            }


            ep.Seen = true;

            ep.OnPropertyChanged("redo");
            ep.OnPropertyChanged("Opacity");
        }

        #endregion

        private void Tapped_Favorite(object sender, RoutedEventArgs e)
        {
            if (Show.following)
            {
                api.unfollowShow(Show.showUrl);
                Show.following = false;
                //followColor.Fill = ((SolidColorBrush)App.Current.Resources["AppBarBackgroundThemeBrush"]);

            }
            else
            {
                api.followShow(Show.showUrl);
                Show.following = true;
                //followColor.Fill = ((SolidColorBrush)App.Current.Resources["PhoneAccentBrush"]);
            }
            setFollowingAppButton();
        }

        
    }
}
