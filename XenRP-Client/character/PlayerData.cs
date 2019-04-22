using System;
using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;

namespace XenRP.Client.character {
    internal class PlayerData : Events.Script {
        private Player target;

        public PlayerData() {
            Events.Add("showPlayerData", ShowPlayerDataEvent);
        }

        private void ShowPlayerDataEvent(object[] args) {
            // Get the data from the input
            var age = args[1].ToString();
            var sex = args[2].ToString();
            var money = args[3].ToString();
            var bank = args[4].ToString();
            var job = args[5].ToString();
            var rank = args[6].ToString();

            if (args[0] != null) {
                // Get the player
                var playerId = Convert.ToInt32(args[0]);
                target = Entities.Players.GetAtRemote((ushort) playerId);
            }

            if (Browser.customBrowser == null)
                Browser.CreateBrowserEvent(null);
            else
                Browser.ExecuteFunctionEvent(null);
        }
    }
}