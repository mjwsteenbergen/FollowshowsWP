using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Credentials;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using BackGroundTask;
using SharedCode;
using System.Threading.Tasks;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Followshows
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                //this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            try
            {
                await registerBackgroundTask();
            }
            catch(Exception)
            { }

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;


                API ap = API.getAPI();

                if(ap.hasLoginCreds()) {
                    if (await ap.login())
                    {
                        if (!rootFrame.Navigate(typeof(MainPage), ap))
                        {
                            throw new Exception("Failed to create initial page");
                        }
                    }
                    else
                    {
                        var connectionP = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                        if (connectionP == null)
                        {
                            Helper.message("You don't have internet. Some features won't be enabled");

                            if (!rootFrame.Navigate(typeof(MainPage), ap))
                            {
                                throw new Exception("Failed to create initial page");
                            }
                        }
                        else
                        {
                            if (!rootFrame.Navigate(typeof(LandingPage)))
                            {
                                throw new Exception("Failed to create initial page");
                            }
                        }
                    }
                }
                else
                {
                    if (!rootFrame.Navigate(typeof(LandingPage)))
                    {
                        throw new Exception("Failed to create initial page");
                    }
                }
                
                
            }

            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        //This method is called when returning from the authentication broker when logging in with facebook
        protected override async void OnActivated(IActivatedEventArgs args)
        {
            API api = API.getAPI();

            if (args is WebAuthenticationBrokerContinuationEventArgs)
            {

                Frame rootFrame = Window.Current.Content as Frame;

                // Do not repeat app initialization when the Window already has content,
                // just ensure that the window is active
                if (rootFrame == null)
                {
                    // Create a Frame to act as the navigation context and navigate to the first page
                    rootFrame = new Frame();

                    // TODO: change this value to a cache size that is appropriate for your application
                    rootFrame.CacheSize = 1;

                    // Place the frame in the current Window
                    Window.Current.Content = rootFrame;
                }

                if (rootFrame.Content == null)
                {
                    if (!rootFrame.Navigate(typeof(LandingPage), api))
                    {
                        throw new Exception("Failed to create initial page");
                    }
                }


                //Store the cookies
                await api.RegisterWithFacebook2((WebAuthenticationBrokerContinuationEventArgs)args);

                //Login
                if (await api.login())
                {
                    if (!rootFrame.Navigate(typeof(MainPage), api))
                    {
                        throw new Exception("Failed to create initial page");
                    }
                }

                Window.Current.Activate();
            }
        }

        public async Task registerBackgroundTask()
        {
            await BackgroundExecutionManager.RequestAccessAsync();

            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = "BackGroundTask";

            SystemTrigger trig = new SystemTrigger(SystemTriggerType.InternetAvailable, false);
            builder.SetTrigger(trig);
            builder.AddCondition(new SystemCondition(SystemConditionType.FreeNetworkAvailable));

            builder.TaskEntryPoint = typeof(BackGroundTask.Backgroud).FullName;

            BackgroundTaskRegistration register = builder.Register();

        }
    }
}