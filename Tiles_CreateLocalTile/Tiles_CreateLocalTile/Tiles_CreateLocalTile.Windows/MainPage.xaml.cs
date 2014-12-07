using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Tiles_CreateLocalTile.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Tiles_CreateLocalTile
{
    
    public sealed partial class MainPage : Page
    {
        private string mediumImagePath = "";
        private string wideImagePath = "";

        public MainPage()
        {
            this.InitializeComponent();
        }

        #region Tile Rendering, Saving, Setting
        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            ////////////////////////////////////////////////////////////////////
            // Render and save tiles
            if (medCanvasHolder.Visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                // Save that image to a local folder that the tile can access (see the ImageSaver class)
                medImage.Source = null;
                mediumImagePath = await ImageSaver.SaveImageToFolder((UIElement)medCanvas, 150, 150, "TileImageSquare.png");
                medImage.Source = new BitmapImage(new Uri(mediumImagePath, UriKind.Absolute));
            }
            else
            {
                wideImage.Source = null;
                wideImagePath = await ImageSaver.SaveImageToFolder((UIElement)wideCanvas, 310, 150, "TileImageWide.png");
                wideImage.Source = new BitmapImage(new Uri(wideImagePath, UriKind.Absolute));
            }
            
            //////////////////////////////////////////////////////////////////
            // Reset UI surface
            HideDrawingSpace.Begin();
            cmdBar.Visibility = Visibility.Collapsed;

        }

        // Call the method to set the tiles
        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            TileSetter.CreateTiles(mediumImagePath, wideImagePath, tileText.Text); 
        }
        #endregion

        #region UI Navigation Events
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            HideDrawingSpace.Completed += HideDrawingSpace_Completed;

        }

        void HideDrawingSpace_Completed(object sender, object e)
        {
            if (medCanvasHolder.Visibility == Visibility.Visible)
                medCanvas.Children.Clear();
            else
                wideCanvas.Children.Clear();

            if (mediumImagePath != "")
            {
                MediumBtn.Visibility = Visibility.Collapsed;
                medImageGrid.Visibility = Visibility.Visible;
            }

            if (wideImagePath != "")
            {
                WideBtn.Visibility = Visibility.Collapsed;
                wideImageGrid.Visibility = Visibility.Visible;
            }
            if (medImageGrid.Visibility == Visibility.Visible &&
                wideCanvas.Visibility == Visibility.Visible &&
                tileText.Text.Length > 0)
            {
                UpdateBtn.IsEnabled = true;
            }
        }

        private void MediumBtn_Click(object sender, RoutedEventArgs e)
        {
            medCanvasHolder.Visibility = Visibility.Visible;
            wideCanvasHolder.Visibility = Visibility.Collapsed;
            ShowDrawingSpace.Begin();
            cmdBar.Visibility = Visibility.Visible;
            cmdBar.IsOpen = true;
        }

        private void WideBtn_Click(object sender, RoutedEventArgs e)
        {
            wideCanvasHolder.Visibility = Visibility.Visible;
            medCanvasHolder.Visibility = Visibility.Collapsed;
            ShowDrawingSpace.Begin();
            cmdBar.Visibility = Visibility.Visible;
            cmdBar.IsOpen = true;
        }

        private void tileText_LostFocus(object sender, RoutedEventArgs e)
        {
            if (medImageGrid.Visibility == Visibility.Visible &&
                wideCanvas.Visibility == Visibility.Visible &&
                tileText.Text.Length > 0)
            {
                UpdateBtn.IsEnabled = true;
            }
        }
        #endregion

        #region Drawing Events

        private void AppBarButton_Click_1(object sender, RoutedEventArgs e)
        {
            HideDrawingSpace.Begin();
            cmdBar.Visibility = Visibility.Collapsed;
        }

        private SolidColorBrush drawingBrush = new SolidColorBrush(Color.FromArgb(255, 184, 0, 0));
        private Point lastPoint = new Point(0, 0);
        private double lineWidth = 8;
        private void ChangeToRed(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            redBtn.BorderThickness = new Thickness(3, 3, 3, 3);
            greenBtn.BorderThickness = new Thickness(0, 0, 0, 0);
            blueBtn.BorderThickness = new Thickness(0, 0, 0, 0);
            drawingBrush = new SolidColorBrush(Color.FromArgb(255, 184, 0, 0));
        }

        private void ChangeToGreen(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            redBtn.BorderThickness = new Thickness(0, 0, 0, 0);
            greenBtn.BorderThickness = new Thickness(3, 3, 3, 3);
            blueBtn.BorderThickness = new Thickness(0, 0, 0, 0);
            drawingBrush = new SolidColorBrush(Color.FromArgb(255, 0, 184, 0));
        }

        private void changeToBlue(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            redBtn.BorderThickness = new Thickness(0, 0, 0, 0);
            greenBtn.BorderThickness = new Thickness(0, 0, 0, 0);
            blueBtn.BorderThickness = new Thickness(3, 3, 3, 3);
            drawingBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 184));
        }


        private void StartDrawing(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            PointerPoint p;
            if (medCanvasHolder.Visibility == Visibility.Visible)
                p = e.GetCurrentPoint((UIElement)medCanvas);
            else
                p = e.GetCurrentPoint((UIElement)wideCanvas);

            Rect r = p.Properties.ContactRect;
            Line l = new Line();

            l.X1 = p.Position.X - (lineWidth / 2);
            l.Y1 = p.Position.Y - (lineWidth / 2);
            l.X2 = p.Position.X - (lineWidth / 2);
            l.Y2 = p.Position.Y - (lineWidth / 2);
            lastPoint = new Point(p.Position.X, p.Position.Y);
            l.StrokeThickness = lineWidth;
            l.Stroke = drawingBrush;
            l.StrokeDashCap = PenLineCap.Round;
            if (medCanvasHolder.Visibility == Visibility.Visible)
                medCanvas.Children.Add(l);
            else
                wideCanvas.Children.Add(l);

        }

        private void Drawing(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            PointerPoint p;
            Line l = new Line();
            if (medCanvasHolder.Visibility == Visibility.Visible)
                p = e.GetCurrentPoint((UIElement)medCanvas);
            else
                p = e.GetCurrentPoint((UIElement)wideCanvas);

            Rect r = p.Properties.ContactRect;
            l.X1 = lastPoint.X - (lineWidth / 2);
            l.Y1 = lastPoint.Y - (lineWidth / 2);
            l.X2 = p.Position.X - (lineWidth / 2);
            l.Y2 = p.Position.Y - (lineWidth / 2);
            lastPoint = new Point(p.Position.X, p.Position.Y);
            l.StrokeThickness = lineWidth;
            l.StrokeDashCap = PenLineCap.Round;
            l.Stroke = drawingBrush;

            if (medCanvasHolder.Visibility == Visibility.Visible)
                medCanvas.Children.Add(l);
            else
                wideCanvas.Children.Add(l);

        }

        #endregion

        
    }
}
