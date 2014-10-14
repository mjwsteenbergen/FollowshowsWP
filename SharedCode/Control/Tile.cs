using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Popups;

namespace SharedCode
{
    public class Tile
    {
        public Tile()
        {
            string tileText = "YOLO";
            string mediumTileImage = "nope";
            string wideTileImage = "nope";

            string xml = 
                "<tile>" +
                    "<visual version=\"2\">" +
                        "<binding template=\"TileSquare150x150PeekImageAndText04\" fallback=\"TileSquarePeekImageAndText04\">" +
                            "<image id=\"1\" src=\"" + mediumTileImage + "\" alt=\"alt text\"/>" +
                            "<text id=\"1\">" + tileText + "</text>" +
                        "</binding>" +
                        "<binding template=\"TileWide310x150ImageAndText01\" fallback=\"TileWideImageAndText01\">" +
                            "<image id=\"1\" src=\"" + wideTileImage + "\" alt=\"alt text\"/>" +
                            "<text id=\"1\">" + tileText + "</text>" +
                        "</binding>" +
                    "</visual>" +
                "</tile>";

            XmlDocument tileXML = new XmlDocument();
            tileXML.LoadXml(xml);

            // Create tile notification
            TileNotification newTile = new TileNotification(tileXML);

            // Send the XML notifications to tile updater
            TileUpdater updateTiler = TileUpdateManager.CreateTileUpdaterForApplication();
            updateTiler.EnableNotificationQueue(false);
            updateTiler.Update(newTile);

            MessageDialog dialog = new MessageDialog("hoi");
            dialog.ShowAsync();
        }
    }
}
