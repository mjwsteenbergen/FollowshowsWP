using SharedCode;
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
using System.ComponentModel;
using Windows.Phone.UI.Input;

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
            api = API.getAPI();

            fuProgressRing.Background = new SolidColorBrush();

            startTimer();
            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

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

        private void LoginButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (Register.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                Register.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                TryLoginOrRegister();
            }
           
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
                else
                {
                    await new MessageDialog("Something went wrong while registering. Please check your input").ShowAsync();
                    await bar.HideAsync();
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

        private async void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (Pivo.SelectedIndex != 1)
            {
                Pivo.IsLocked = false;
                Pivo.SelectedIndex = 1;
                e.Handled = true;
                await Task.Delay(500);
                Pivo.IsLocked = true;
                Pivo.SelectedIndex = 1;
            }
        }

        private async void buttonTapped(object sender, TappedRoutedEventArgs e)
        {
            Pivo.IsLocked = false;
            Pivo.SelectedIndex = (sender as Button).Name == "zero" ? 0 : 2;
            await Task.Delay(500);
            Pivo.IsLocked = true;
            Pivo.SelectedIndex = (sender as Button).Name == "zero" ? 0 : 2;
        }

        private void layoutUpdate(object sender, object e)
        {
            Pivo.IsLocked = true;
            Pivo.SelectionChanged -= layoutUpdate;
        }

        private async void startTimer()
        {
            List<string> wallpaper = new List<string>(){
                    "http://www.pagepulp.com/wp-content/gotseas2.jpg",
                    "http://i.fokzine.net/upload/14/09/140905_725_arrow_ver3_xlg.jpg",
                    "http://img2.wikia.nocookie.net/__cb20141026203433/agentsofshield/images/1/17/Agents_of_S.H.I.E.L.D._Season_1_Poster.jpg",
                    "http://d2spmqy4pos7su.cloudfront.net/Doctor_Who/Doctor_Who_357x500.jpg",
                    "http://www.xnds.de/wp-content/uploads/2014/06/The-Blacklist-poster.jpg",
                    "http://www.moviesonline.ca/wp-content/uploads/2010/09/TWD_1-SHEET_WEB.jpg",
                    "http://ia.media-imdb.com/images/M/MV5BMTQ5MzY5ODE5M15BMl5BanBnXkFtZTgwNzU4OTM1MjE@._V1_SX214_AL_.jpg", //Flash
                    "http://ia.media-imdb.com/images/M/MV5BMTU5MTczNTkxNl5BMl5BanBnXkFtZTgwNTM5NDc1MTE@._V1_SX214_AL_.jpg", //The 100
                    "http://ia.media-imdb.com/images/M/MV5BMjA1ODMzNDM5Ml5BMl5BanBnXkFtZTgwNDU0NjQ5MTE@._V1_SY317_CR0,0,214,317_AL_.jpg", //Orange is the new Black
                    "http://ia.media-imdb.com/images/M/MV5BMjMzNTU3MDY3OF5BMl5BanBnXkFtZTgwMjY1Nzg3MTE@._V1_SY317_CR104,0,214,317_AL_.jpg", //Gotham
                    "http://d2spmqy4pos7su.cloudfront.net/Modern_Family/Modern_Family_357x500.jpg",
                    "http://d2spmqy4pos7su.cloudfront.net/Elementary/Elementary_357x500.jpg",
                    "http://d2spmqy4pos7su.cloudfront.net/Grimm/Grimm_357x500.jpg",
                    "http://d2spmqy4pos7su.cloudfront.net/person-of-interest/person-of-interest_357x500.jpg",
                    "http://d2spmqy4pos7su.cloudfront.net/Better-Call-Saul/Better-Call-Saul_357x500.jpg",
                    "http://d2spmqy4pos7su.cloudfront.net/The_Big_Bang_Theory/The_Big_Bang_Theory_357x500.jpg"
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
            
            BitmapImage current = null;

            while(true){

                await Task.Delay(20);

                if (list.Count < wallpaper.Count)
                {
                    foreach (BitmapImage image in list2)
                    {
                        Tester.Source = image;
                    }
                }

                ImageBack.Source = current;
                ImageFron.Opacity = 1.0;
                
                if(list.Count == 0)
                {
                    continue;
                }

                List<BitmapImage> shuffledList = new List<BitmapImage>();
                foreach (BitmapImage im in list)
                {
                    int i = (int)((new Random().NextDouble() * shuffledList.Count));
                    shuffledList.Insert(i, im);
                }

                list = shuffledList;

                if (current == shuffledList[0] && shuffledList.Count > 2)
                {
                    current = shuffledList[1];
                }
                else
                {
                    current = shuffledList[0];
                }

                

                ImageFron.Source = current;
                fadeIn.Begin();

                await Task.Delay(6000);
              
            }
            
        }

        void image_ImageOpened(object sender, RoutedEventArgs e)
        {
            list.Add(sender as BitmapImage);
        }

        private void PivotChanged(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            
        }

        private void selChange(object sender, SelectionChangedEventArgs e)
        {
            Pivot piv = sender as Pivot;
            if (piv.SelectedIndex == 2)
            {
                api.RegisterWithFacebook();
            }
        }

        
    }
}
