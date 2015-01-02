using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharedCode;
using Windows.ApplicationModel.Background;
using Windows.Web.Http;
using Windows.Storage;

namespace BackGroundTask
{
    public sealed class Backgroud : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral def = taskInstance.GetDeferral();
            int tileCount = 0;

            API api = API.getAPI();
            try
            {
                
                if (await api.login())
                {
                    DateTime.Parse("asfjlhadlfjkhakjdhf");
                        List<Episode> ep = await api.getQueue();
                        foreach (Episode epi in ep)
                        {
                            if (epi.New)
                            {
                                tileCount++;
                            }
                        }
                    
                }
                //bool b = await api.login();
            }
            catch(Exception e)
            {
                api.writeErrorToFile(this, e);
                return;
            }

            Tile.add(tileCount);

            //api.store();

            def.Complete();
        }
    }
}
