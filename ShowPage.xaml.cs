using Followshows.Common;
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
        private int currentPivot = 0;
        private CommandBar bar;

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
            NTW.DataContext = new Episode(false,true) { redo = Windows.UI.Xaml.Visibility.Collapsed };

            api = (API)e.NavigationParameter;
            Show = await api.getShow(api.passed as TvShow);

                //Create a new commandbar and add buttons
            bar = new CommandBar();
            AppBarButton follow = new AppBarButton() { Icon = new SymbolIcon(Symbol.Favorite), Label = "Follow" };
            follow.Click += follow_Click;
            follow.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if(!Show.following)
            {
                follow.Visibility = Windows.UI.Xaml.Visibility.Visible;
                amIFollowing.Text = "Not Yet...";
            }
            
            AppBarButton unfollow = new AppBarButton() { Icon = new SymbolIcon(Symbol.UnFavorite), Label = "Unfollow" };
            unfollow.Click += unfollow_Click;
            unfollow.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if(Show.following)
            {
                unfollow.Visibility = Windows.UI.Xaml.Visibility.Visible;
                amIFollowing.Text = "Yes you are";
            }

            AppBarButton seen = new AppBarButton() { Icon = new SymbolIcon(Symbol.Accept), Label = "Mark all as seen" };
            seen.Click += markAsSeen_Click;
            seen.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            bar.PrimaryCommands.Add(follow);
            bar.PrimaryCommands.Add(unfollow);
            bar.PrimaryCommands.Add(seen);

            BottomAppBar = bar;

            Image.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(Show.Image.UriSource.ToString().Replace("80", "357").Replace("112", "500")));
            this.DataContext = Show;

            NTWtitle.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            List<Episode> wl = await api.getWatchList();
            if(wl != null)
            {
                foreach(Episode ep in wl)
                {
                    if(ep.SeriesName == Show.Name)
                    {
                        NTW.DataContext = ep;
                        NTWtitle.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                    }
                }
            
            }
            if(Show.numberOfSeasons > 0)
            {
                season = new List<Episode>[Show.numberOfSeasons+1];
                //For every show create a new pivot with a gridview with the episode-itemtemplate;
                for(int i=Show.numberOfSeasons; i>0; i--)
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

            if (bar == null)
                return;
            if (currentPivot > 0)
            {
                //If we are on the first pivot, show only the mark all as seen button
                foreach( AppBarButton button in bar.PrimaryCommands)
                {
                    if (button.Label == "Mark all as seen")
                        button.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    else
                        button.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                //If we are following the show, show unfollow button. else show Follow button.
                foreach (AppBarButton button in bar.PrimaryCommands)
                {
                    if (button.Label == "Mark all as seen")
                        button.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    else
                    {
                        if (Show.following && button.Label == "Unfollow")
                            button.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        if (!Show.following && button.Label == "Follow")
                            button.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                }
            }
        }

        private async void markAsSeen_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog dia = new MessageDialog("Mark season as watched?","Are you sure?");
            dia.Commands.Add(new UICommand("Ok"));
            dia.Commands.Add(new UICommand("Cancel"));
            IUICommand res = await dia.ShowAsync();

            //if ok pressed
            if (res.Label == "Ok")
            {
                PivotItem pivoi = Pivot.Items[currentPivot] as PivotItem;
                string seasonnr = pivoi.Header.ToString().Replace("Season ", "");
                
                foreach(Episode epi in season[Int32.Parse(seasonnr)])
                {
                    if(epi.Aired)
                    {
                        epi.Seen = true;
                        epi.OnPropertyChanged("redo");
                        epi.OnPropertyChanged("Opacity");
                    }
                }
                api.markSeasonAsWatched(seasonnr, Show);
            }
        }

        private void unfollow_Click(object sender, RoutedEventArgs e)
        {
            api.unfollowShow(Show.showUrl);
            if (bar == null)
                return;
            foreach (AppBarButton button in bar.PrimaryCommands)
            {
                if (button.Label == "Follow")
                    button.Visibility = Windows.UI.Xaml.Visibility.Visible;
                else
                    button.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }

        }

        private void follow_Click(object sender, RoutedEventArgs e)
        {
            api.followShow(Show.showUrl);
            if (bar == null)
                return;
            foreach (AppBarButton button in bar.PrimaryCommands)
            {
                if (button.Label == "Unfollow")
                    button.Visibility = Windows.UI.Xaml.Visibility.Visible;
                else
                    button.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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
                Uri a = new Uri("ms-appx:Assets/basicQueueItem.bmp");
                im.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(a);
                im.Opacity = 1;
                im.Stretch = Stretch.Fill;
            }
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

            Image ima = item.FindName("ima") as Image;
            if (ima.Opacity == 1)
            {
                if (!ep.Aired)
                {
                    Helper.message("This episode hasn't aired yet. You cannot mark it as watched", "EPISODE NOT AIRED");
                    return;
                }

                //Show fade-out animation
                DoubleAnimation ani = new DoubleAnimation();
                Storyboard.SetTarget(ani, ima);

                Storyboard board = new Storyboard();
                board.Completed += board_Completed;
                Storyboard.SetTargetProperty(ani, "Opacity");
                ani.To = 0.2;
                board.Duration = new Duration(TimeSpan.FromSeconds(1));
                board.Children.Add(ani);

                ep.Seen = true;

                board.Begin();
                
            }
            else
            {
                item = sender as Grid;
                ep = item.DataContext as Episode;
                
                //Show animation
                DoubleAnimation ani = new DoubleAnimation();
                Storyboard.SetTarget(ani, ima);

                Storyboard board = new Storyboard();
                Storyboard.SetTargetProperty(ani, "Opacity");
                ani.To = 1;
                board.Duration = new Duration(TimeSpan.FromSeconds(1));
                board.Children.Add(ani);

                board.Begin();

                ep.Seen = false;

                //Refresh data-context
                item.DataContext = null;
                item.DataContext = ep;


                api.markNotAsWatched(ep);
            }
            

            
        }

        void board_Completed(object sender, object e)
        {
            ep.redo = Visibility.Visible;

            api.markAsWatched(ep);

            //Refresh datacontext
            item.DataContext = null;
            item.DataContext = ep;
        }

        #endregion
    }
}
