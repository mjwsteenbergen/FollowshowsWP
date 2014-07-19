using Followshows.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
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

        private bool loggingin;


        private TextBox email;
        private TextBox firstName;
        private TextBox lastName;
        private ComboBox timeZone;
        private PasswordBox password;
        private TextBlock noAc;
        private TextBlock passBlock;
        private TextBlock header;
        private Button logreg;

        private Grid LoginForm;
        private Grid Email;
        private Grid FirstName;
        private Grid LastName;
        private Grid TimeZone;

        private string firstname;
        private string lastname;
        private string emailad;
        private string option;
        private string timezone;

        //Facebook
        WebView webv_facebook;

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

        private async void TryLoginOrRegister()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (loggingin)
            {
                if (emailad == null || password.Password == null)
                {
                    await new MessageDialog("Your password was incorrect. Please try again", "Incorrect password").ShowAsync();
                    return;
                }
                if (!(await api.LoginWithEmail(emailad, password.Password)))
                {
                    await new MessageDialog("Your password was incorrect. Please try again", "Incorrect password").ShowAsync();

                }
                else
                {
                    Windows.Security.Credentials.PasswordVault vault = new Windows.Security.Credentials.PasswordVault();
                    vault.Add(new Windows.Security.Credentials.PasswordCredential("email", emailad, password.Password));
                    if (!rootFrame.Navigate(typeof(MainPage), api))
                    {
                        throw new Exception("Failed to create initial page");
                    }
                }
            }
            else
            {
                bool allok = true;
                if (firstname == null)
                {
                    firstName.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (lastname == null)
                {
                    lastName.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (emailad == null || !Regex.IsMatch(emailad, "[^@]+@[^@]+.[a-zA-Z]{2,6}"))
                {
                    email.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (password.Password == null || password.Password.Length < 6)
                {
                    password.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (timezone == null)
                {
                    timeZone.BorderBrush = new SolidColorBrush() { Color = Windows.UI.Colors.Red };
                    allok = false;
                }
                if (allok)
                {
                    if (await api.RegisterWithEmail(firstname, lastname, emailad, password.Password, timezone))
                    {
                        Windows.Security.Credentials.PasswordVault vault = new Windows.Security.Credentials.PasswordVault();
                        vault.Add(new Windows.Security.Credentials.PasswordCredential("email", emailad, password.Password));

                        if (!rootFrame.Navigate(typeof(MainPage), api))
                        {
                            throw new Exception("Failed to create initial page");
                        }
                    }
                    else
                    {
                        noAc.Text = "Something went wrong :(";
                    }
                }
            }



        }

        private void SwitchRegister(object sender, TappedRoutedEventArgs e)
        {
            if (loggingin)
            {
                FirstName.Visibility = Visibility.Visible;
                LastName.Visibility = Visibility.Visible;
                TimeZone.Visibility = Visibility.Visible;
                LoginForm.Margin = new Thickness(0, 80, 0, 0);
                loggingin = false;
                noAc.Text = "Got an account?";
                logreg.Content = "Register";
                passBlock.Text = "Password (more than 6 characters)";
                header.Text = "Register";
            }
            else
            {
                FirstName.Visibility = Visibility.Collapsed;
                LastName.Visibility = Visibility.Collapsed;
                TimeZone.Visibility = Visibility.Collapsed;
                LoginForm.Margin = new Thickness(0, 0, 0, 0);
                loggingin = true;
                noAc.Text = "Don't have an account?";
                logreg.Content = "Login";
                passBlock.Text = "Password";
                header.Text = "Login";
            }
        }


        #region register Items

        #region loading and identifying controls

        private void password_Loaded(object sender, RoutedEventArgs e)
        {
            password = sender as PasswordBox;
        }

        private void loaded(object sender, RoutedEventArgs e)
        {

            Control control = sender as Control;
            if (control == null)
            {
                webv_facebook = sender as WebView;
                api.LoginWithFacebook(webv_facebook);
                return;
            }
            switch (control.Name)
            {
                case ("firstname"):
                    firstName = sender as TextBox;
                    break;
                case ("lastname"):
                    lastName = sender as TextBox;
                    break;
                case ("email"):
                    email = sender as TextBox;
                    break;
            }
        }

        private void gridLoad(object sender, RoutedEventArgs e)
        {
            Grid gr = sender as Grid;
            switch (gr.Name)
            {
                case ("LoginForm"):
                    LoginForm = gr;
                    break;
                case ("FirstName"):
                    FirstName = gr;
                    break;
                case ("LastName"):
                    LastName = gr;
                    break;
                case ("TimeZone"):
                    TimeZone = gr;
                    break;
                case ("Email"):
                    Email = gr;
                    break;
            }
        }

        private void Button_Loaded(object sender, RoutedEventArgs e)
        {
            logreg = sender as Button;
        }

        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock block = sender as TextBlock;
            switch (block.Name)
            {
                case "passBlock":
                    passBlock = block;
                    break;
                case "header":
                    header = block;
                    break;
                case "noAc":
                    noAc = block;
                    break;
            }
        }


        #endregion

        #region changed

        private void firstName_Changed(object sender, TextChangedEventArgs e)
        {
            firstName = sender as TextBox;
            firstname = firstName.Text;
            firstName.BorderBrush = null;
        }

        private void lastName_Changed(object sender, TextChangedEventArgs e)
        {
            lastName = sender as TextBox;
            lastname = firstName.Text;
            lastName.BorderBrush = BorderBrush;
        }

        void email_Changed(object sender, TextChangedEventArgs e)
        {
            email = sender as TextBox;
            emailad = email.Text;
            email.BorderBrush = BorderBrush;
        }

        private void TimeZoneChanged(object sender, SelectionChangedEventArgs e)
        {
            timeZone = sender as ComboBox;
            timezone = timeZone.SelectedItem.ToString();
            timeZone.BorderBrush = new SolidColorBrush(Windows.UI.Colors.White);
        }

        #endregion

        private void timezoneLoaded(object sender, RoutedEventArgs e)
        {
            timeZone = sender as ComboBox;
        }

        #endregion

        private void frameNav(WebView sender, WebViewNavigationStartingEventArgs args)
        {

            //http://followshows.com/choose
        }

        private void webv_facebook_FrameNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {

        }

        














    }
}
