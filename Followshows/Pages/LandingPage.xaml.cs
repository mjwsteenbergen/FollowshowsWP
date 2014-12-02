using Followshows.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Followshows
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LandingPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        private API api;
        private List<BitmapImage> list;

        private bool loggingin;

        public LandingPage()
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
            StatusBar statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            // Hide the status bar
            await statusBar.HideAsync();
            api = e.NavigationParameter as API;

            loggingin = true;

            startTimer();

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

        private void keyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.ToString() == "Enter")
                FocusManager.TryMoveFocus(FocusNavigationDirection.Next);
        }

        private void password_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key.ToString() == "Enter")
                TryLoginOrRegister();
        }

        private void LoginAndRegisterButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            TryLoginOrRegister();
        }

        private async void RegisterButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Register.Visibility == Windows.UI.Xaml.Visibility.Collapsed)
            {
                Register.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                Frame rootFrame = Window.Current.Content as Frame;
                StatusBar bar = StatusBar.GetForCurrentView();
                await bar.ProgressIndicator.ShowAsync();
                await bar.ShowAsync();
                bar.ProgressIndicator.Text = "Trying to register...";

                bool allok = true;
                if (firstname.Text == null)
                {
                    firstname.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (lastname.Text == null)
                {
                    lastname.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (email.Text == null || !Regex.IsMatch(email.Text, "[^@]+@[^@]+.[a-zA-Z]{2,6}"))
                {
                    email.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (password.Password == null || password.Password.Length < 6)
                {
                    password.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (timezone.SelectedItem == null)
                {
                    timezone.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (allok)
                {
                    if (await api.RegisterWithEmail(firstname.Text, lastname.Text, email.Text, password.Password, (timezone.SelectedItem as string)))
                    {
                        Windows.Security.Credentials.PasswordVault vault = new Windows.Security.Credentials.PasswordVault();
                        vault.Add(new Windows.Security.Credentials.PasswordCredential("email", email.Text, password.Password));

                        if (!rootFrame.Navigate(typeof(MainPage), api))
                        {
                            throw new Exception("Failed to create initial page");
                        }
                    }
                }
            }
        }

        private async void TryLoginOrRegister()
        {
            StatusBar bar = StatusBar.GetForCurrentView();
            await bar.ProgressIndicator.ShowAsync();
            await bar.ShowAsync();

            Frame rootFrame = Window.Current.Content as Frame;
                bar.ProgressIndicator.Text = "Trying to log in...";


            if (email.Text == null || password.Password == null)
            {
                await new MessageDialog("Your password was incorrect. Please try again", "Incorrect password").ShowAsync();
                return;
            }
            if (!(await api.LoginWithEmail(email.Text, password.Password)))
            {
                await new MessageDialog("Your password was incorrect. Please try again", "Incorrect password").ShowAsync();
            }
            else
            {
                Windows.Security.Credentials.PasswordVault vault = new Windows.Security.Credentials.PasswordVault();
                vault.Add(new Windows.Security.Credentials.PasswordCredential("email", email.Text, password.Password));
                if (!rootFrame.Navigate(typeof(MainPage), api))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            await bar.HideAsync();
        }

        private void SwitchRegister(object sender, TappedRoutedEventArgs e)
        {
            
        }


        #region focus

        private void gotFocus(object sender, RoutedEventArgs e)
        {
            (sender as Control).Foreground = new SolidColorBrush(Colors.Black);
        }

        private void lostFocus(object sender, RoutedEventArgs e)
        {  
            (sender as Control).Foreground = new SolidColorBrush(Colors.White);
            (sender as Control).BorderBrush = new SolidColorBrush(Colors.White);
        }

        #endregion

        private void buttonTapped(object sender, TappedRoutedEventArgs e)
        {
            Pivo.SelectedIndex = (sender as StackPanel).Name == "zero" ? 0 : 2;
        }

        private async void startTimer()
        {
            List<string> wallpaper = new List<string>(){
                    "http://www.pagepulp.com/wp-content/gotseas2.jpg",
                    "http://i.fokzine.net/upload/14/09/140905_725_arrow_ver3_xlg.jpg",
                    "http://img2.wikia.nocookie.net/__cb20141026203433/agentsofshield/images/1/17/Agents_of_S.H.I.E.L.D._Season_1_Poster.jpg",
                    "http://fc05.deviantart.net/fs70/i/2010/180/5/1/Doctor_Who_Season_5_Poster_by_JKop360.jpg",
                    "http://www.xnds.de/wp-content/uploads/2014/06/The-Blacklist-poster.jpg",
                    "http://www.moviesonline.ca/wp-content/uploads/2010/09/TWD_1-SHEET_WEB.jpg"
                        };

            list = new List<BitmapImage>();
            List<BitmapImage>  list2 = new List<BitmapImage>();

            foreach(string item in wallpaper)
            {
                BitmapImage image = new BitmapImage();
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.ImageOpened += image_ImageOpened;
                image.UriSource = new Uri(item);
                list2.Add(image);
            }
            //bak.ItemsSource = list;
            
            while(true){
                await Task.Delay(5000);

                if (list.Count < wallpaper.Count)
                {
                    foreach (BitmapImage image in list2)
                    {
                        Tester.Source = image;
                    }
                }
                
                if(list.Count == 0)
                {
                    continue;
                }

                //bak.ScrollIntoView(bak.Items[new Random().Next(0, 3)],ScrollIntoViewAlignment.Leading);
                Background.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                Background.Source = list[new Random().Next(0, wallpaper.Count)];
                Background.Visibility = Windows.UI.Xaml.Visibility.Visible;


                

                //HttpClient http = new HttpClient();
                //Stream resp = await http.GetStreamAsync(wallpaper[1]);
                

                //var ras = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                //await resp.CopyToAsync(ras.AsStreamForWrite());
                //bi.SetSource(ras);

                
                //bi.ImageOpened += (object sener, RoutedEventArgs f) =>
                //{
                //    LayoutRoot.Background = new ImageBrush() { ImageSource = sener as BitmapImage, Opacity = 0.4 };
                //};
                //bi.UriSource = new Uri(wallpaper[1]);
                //Background.Source = bi;
            }
            
        }

        void image_ImageOpened(object sender, RoutedEventArgs e)
        {
            list.Add(sender as BitmapImage);
        }

        private void PivotChanged(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            Pivot piv = sender as Pivot;
            if (piv.SelectedIndex == 2)
            {
                api.RegisterWithFacebook();
            }
        }

        
    }
}
