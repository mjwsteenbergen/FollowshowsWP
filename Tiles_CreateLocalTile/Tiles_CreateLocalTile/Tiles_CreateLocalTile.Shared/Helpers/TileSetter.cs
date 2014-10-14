using System;
using System.Collections.Generic;
using System.Text;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Tiles_CreateLocalTile.Helpers
{
    public static class TileSetter
    {
        public static void CreateTiles(string mediumTileImage, string wideTileImage, string tileText)
        {
            XmlDocument tileXML = new XmlDocument();
            
            ////////////////////////////////////////////////////////
            // Find all the available tile template formats at:
            //      http://msdn.microsoft.com/en-us/library/windows/apps/Hh761491.aspx
  
            string tileString = "<tile>" +
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
            tileXML.LoadXml(tileString);

            // Create tile notification
            TileNotification newTile = new TileNotification(tileXML);

            // Send the XML notifications to tile updater
            TileUpdater updateTiler = TileUpdateManager.CreateTileUpdaterForApplication();
            updateTiler.EnableNotificationQueue(false);
            updateTiler.Update(newTile);


        }
    }
}
