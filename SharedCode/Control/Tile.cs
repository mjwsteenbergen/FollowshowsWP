using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.Popups;

namespace SharedCode
{
    public class Tile
    {
        public static void setTile(int amount)
        {
            if(amount == 0)
            {
                BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
                TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                return;
            }

            string xml =
                "<tile>" +
                    "<visual version=\"2\">" +
                        "<binding template=\"TileSquare71x71IconWithBadge\">" +
                            "<image id=\"1\" src=\"Assets/Logo/square71x71logoBadge.scale-100.png\" alt=\"alt text\"/>" +
                        "</binding>" +
                        "<binding template=\"TileSquare150x150IconWithBadge\" fallback=\"TileSquarePeekImageAndText04\">" +
                            "<image id=\"1\" src=\"Assets/Logo/square71x71logoBadge.scale-240.png\" alt=\"alt text\"/>" +
                        "</binding>" +
                        //"<binding template=\"TileWide310x150ImageAndText01\" fallback=\"TileWideImageAndText01\">" +
                        //    "<image id=\"1\" src=\"" + wideTileImage + "\" alt=\"alt text\"/>" +
                        //    "<text id=\"1\">" + tileText + "</text>" +
                        //"</binding>" +
                    "</visual>" +
                "</tile>";
            

            XmlDocument tileXML = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare71x71IconWithBadge);
            tileXML.LoadXml(tileXML.GetXml().Replace("src=\"\"", "src=\"Assets/Logo/square71x71logoBadge.scale-100.png\""));

            tileXML.LoadXml(xml);

            // Create tile notification
            TileNotification newTile = new TileNotification(tileXML);

            // Send the XML notifications to tile updater
            TileUpdater updateTiler = TileUpdateManager.CreateTileUpdaterForApplication();
            String str = updateTiler.GetType().FullName;
            updateTiler.Update(newTile);



            //Create badgeUpdate
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<badge version='1' value='" + amount + "'/>");

            // Send the notification to the application’s tile.
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(new BadgeNotification(doc));
        }

        public async static Task add(int amount)
        {
            try
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.GetFileAsync("tile.txt");
                amount += int.Parse(await Windows.Storage.FileIO.ReadTextAsync(fil));
            }
            catch
            {
                Tile.setTile(93);
            }

            Tile.setTile(amount);

            try
            {
                StorageFolder temp = ApplicationData.Current.LocalFolder;
                StorageFile fil = await temp.CreateFileAsync("tile.txt", CreationCollisionOption.ReplaceExisting);
                await Windows.Storage.FileIO.WriteTextAsync(fil, amount.ToString());
            }
            catch
            {
                Tile.setTile(92);
            }
        }
    }
}
