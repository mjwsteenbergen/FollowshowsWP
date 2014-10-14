using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.AccessCache;

namespace Tiles_CreateLocalTile.Helpers
{
    public class ImageSaver
    {
        public ImageSaver() { }

        public static async Task<string> SaveImageToFolder(UIElement render, int width, int height, string filename)
        {
            //////////////////////////////////////////////////////
            // We've rendered the XAML
            RenderTargetBitmap rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(render, width, height);
            /////////////////////////////////////////////////////

            IBuffer b = await rtb.GetPixelsAsync();

            ///////////////////////////////////////////////////
            //   1) create the new folder
            //   2) create the new file
            //   3) save the rendered XAML to an image file
            //   4) return the image path

            ////////////////////////////////////////////////////
            // Folder access
            //StorageFile currentFile = null;
            var localFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("TileImages", CreationCollisionOption.OpenIfExists);
            var currentFile = await localFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            
            using (IRandomAccessStream outputFileStream = await currentFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // create our bitmap encoder
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputFileStream);

                // turn our Buffer into as byte array
                byte[] imageBytes = b.ToArray();

                // use the encoder to save the image
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                    BitmapAlphaMode.Premultiplied,
                                    (uint)rtb.PixelWidth,
                                    (uint)rtb.PixelHeight,
                                    96.0, 96.0,
                                    imageBytes);
                await encoder.FlushAsync();
                StorageItemAccessList sial = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
                sial.AddOrReplace(filename, currentFile);
            }

            // return the file location so we can use it in our tile
            return "ms-appdata:///local/" + localFolder.DisplayName + "/" + currentFile.DisplayName;
        }

    }
}
