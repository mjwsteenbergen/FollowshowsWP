using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedCode;
using Windows.ApplicationModel.Background;

namespace BackGroundTask
{
    public sealed class Backgroud : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
 	        API api = API.getAPI();
            await api.login();
            await api.getQueue();
            await api.getCalendar();
            await api.getTracker();
            await api.store();

            Tile s = new Tile();

        }
    }
}
