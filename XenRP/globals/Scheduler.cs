using System;
using System.Threading;
using GTANetworkAPI;
using MySqlX.XDevAPI;

namespace XenRP.globals {
    public delegate void MinuteHandler();
    public class Scheduler : Script {
        public static event MinuteHandler Minute;

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart() {
            var minuteTimer = new Timer(x=> {
                Minute?.Invoke();
            }, null, 60000, 60000);
        }
        
        public static int GetTotalSeconds() {
            return (int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}