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
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Email;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using SharedCode;

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

        //Items in datatemplate
        private GridView queue;
        private ListView tracker;
        private GridView cale;

        List<Episode> q;

        public MainPage()
        {
            this.InitializeComponent();

            initiateCommandBar();
            
            
            

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            //http://followshowswp.uservoice.com/forums/255100-general
        }

        private void initiateCommandBar()
        {
            CommandBar bar = new CommandBar();
            AppBarButton logou = new AppBarButton() { Icon = new SymbolIcon(Symbol.Cancel), Label = "Log out" };
            logou.Click += logout;
            AppBarButton refr = new AppBarButton() { Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:Assets/Buttons/appbar.refresh.png") }, Label = "Refresh" };
            refr.Click += refresh;
            AppBarButton search = new AppBarButton() { Icon = new SymbolIcon(Symbol.Find), Label = "Search" };
            search.Click += search_Click;

            AppBarButton ideas = new AppBarButton() { Label = "Suggest a feature" };
            ideas.Click += openForum;
            AppBarButton bugs = new AppBarButton() { Label = "Report a bug" };
            ideas.Click += openForum;
            AppBarButton contact = new AppBarButton() { Label = "Contact Developer" };
            contact.Click += sendEmail;

            bar.PrimaryCommands.Add(refr);
            bar.PrimaryCommands.Add(search);
            bar.PrimaryCommands.Add(logou);

            bar.SecondaryCommands.Add(ideas);
            bar.SecondaryCommands.Add(bugs);
            bar.SecondaryCommands.Add(contact);

            bar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;

            BottomAppBar = bar;
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
            StatusBar statusBar = StatusBar.GetForCurrentView();
            // Hide the status bar
            await statusBar.HideAsync();

            ////Loading
            api = API.getAPI();

            //As a precaution set a show as the passed object
            TvShow show = new TvShow(false);
            api.passed = show;

            if (api.hasInternet())
            {
                    LoadLists();
            }
            else
            {


                List<Episode> list = await api.recoverQueue();
                List<TvShow> listTV = await api.recoverTracker();
                List<Episode> cal = await api.recoverCalendar();
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    if (list.Count > 0)
                    {
                        queue.ItemsSource = list;
                    }

                    if (listTV.Count > 0)
                    {
                        tracker.ItemsSource = listTV;
                    }

                    if (cal.Count > 0)
                    {
                        if (cal != null)
                        {
                            var result =
                                from ep in cal
                                group ep by ep.airdate
                                    into grp
                                    orderby grp.Key
                                    select grp;
                            calendar.Source = result;
                        }
                    }


                    //q = list;
                });


                //Wait for when we do have internet
                api.getNetwork().PropertyChanged += NetworkStatus_Changed;
            }


            Tile.setTile(0);


        }

        private async void LoadLists()
        {

            StatusBar bar = StatusBar.GetForCurrentView();
            await bar.ProgressIndicator.ShowAsync();
            bar.ProgressIndicator.Text = "Getting Queue";
            await bar.ShowAsync();

            //Execute commands before loading
            await api.executeCommands();

            //Load Queue
            List<Episode> queueList = await (new Queue()).getQueue();
            if (queueList != null)
            {
                queue.ItemsSource = queueList;
            }

            bar.ProgressIndicator.Text = "Getting Calendar";

            List<Episode> cal = await api.getCalendar();
            if (cal != null)
            {
                var result =
                    from ep in cal
                    group ep by ep.airdate
                        into grp
                        orderby grp.Key
                        select grp;
                calendar.Source = result;
            }


            bar.ProgressIndicator.Text = "Getting Tracker";

            //Load Tracker
            List<TvShow> track = await api.getTracker();
            if (track != null)
            {
                tracker.ItemsSource = track;
            }

            

            bar.ProgressIndicator.Text = "Done";
            await bar.HideAsync();

            api.store();
        }

        async void NetworkStatus_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //Run inside of UI thread (otherwise won't work)
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                LoadLists();
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


        private async void Item_Tapped(object sender, DoubleTappedRoutedEventArgs e)
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

                if (api.hasInternet())
                {
                    await ep.markAsWatched();
                }
                else
                {
                    api.addCommand(new Command() { episode = ep, watched = true });
                }


                ep.Seen = true;

                ep.OnPropertyChanged("redo");
  

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

                ep.OnPropertyChanged("redo");
                ep.OnPropertyChanged("Opacity");

                if (api.hasInternet())
                {
                    await ep.markNotAsWatched();
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
            ep.OnPropertyChanged("Opacity");
        }

        #endregion

        private void logout(object sender, RoutedEventArgs e)
        {
            PasswordVault vault = new PasswordVault();
            try
            {
                vault.Remove(vault.FindAllByResource("email")[0]);
            }
            catch (Exception)
            { }

            try
            {
                vault.Remove(vault.FindAllByResource("facebook")[0]);
            }
            catch (Exception)
            { }
            
            api.refresh();
            Frame rootFrame = Window.Current.Content as Frame;
            if (!rootFrame.Navigate(typeof(LandingPage), api))
            {
                throw new Exception("Failed to create initial page");
            }
        }

        public void refresh(object sender, RoutedEventArgs e)
        {
            if (!api.hasInternet())
            {
                return;
            }
            LoadLists();
        }

        private async void openForum(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("http://followshowswp.uservoice.com/forums/255100-general"));
        }

        private async void sendEmail(object sender, RoutedEventArgs e)
        {
            var emailMessage = new Windows.ApplicationModel.Email.EmailMessage();

            Contact c = new Contact();
            c.Emails.Add(new ContactEmail() { Address = "followshows@nntn.nl" });

            var email = c.Emails.FirstOrDefault<ContactEmail>();
            if (email != null)
            {
                var emailRecipient = new EmailRecipient(email.Address);
                emailMessage.To.Add(emailRecipient);
            }

            await EmailManager.ShowComposeNewEmailAsync(emailMessage);

        }

        void search_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (!rootFrame.Navigate(typeof(Search), api))
            {
                throw new Exception("Failed to create initial page");
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
            if (!api.hasInternet())
            {
                Helper.message("This feature is currently unavailable");
                return;
            }

            Grid gr = sender as Grid;
            api.passed = gr.DataContext;
            Frame rootFrame = Window.Current.Content as Frame;
            
            if (!rootFrame.Navigate(typeof(ShowPage), api))
            {
                throw new Exception("Failed to create initial page");
            }
        }

        private void Register(object sender, RoutedEventArgs e)
        {
            Control c = sender as Control;

            switch (c.Name)
            {
                case "queue":
                    queue = sender as GridView;
                    if (q != null)
                    {
                        queue.ItemsSource = q;
                    }
                    break;
                case "tracker":
                    tracker = sender as ListView;
                    break;
                case "cale":
                    cale = sender as GridView;
                    break;
            }
        }
    }
}
