using RAGE;
using RAGE.Elements;
using WiredPlayers_Client.globals;
using System;

namespace WiredPlayers_Client.character
{
    class PlayerData : Events.Script
    {
        private Player target = null;
        private bool extended = false;

        public PlayerData()
        {
            Events.Add("showPlayerData", ShowPlayerDataEvent);
        }

        private void ShowPlayerDataEvent(object[] args)
        {
            // Get the data from the input
            string age = args[1].ToString();
            string sex = args[2].ToString();
            string money = args[3].ToString();
            string bank = args[4].ToString();
            string job = args[5].ToString();
            string rank = args[6].ToString();

            if (args[0] != null)
            {
                // Get the player
                int playerId = Convert.ToInt32(args[0]);
                target = Entities.Players.GetAtRemote((ushort)playerId);
            }

            if(Browser.customBrowser == null)
            {
                // Check if the extended information should be shown

                // Create the window with the basic data
                Browser.CreateBrowserEvent(null);
            }
            else
            {
                // Update the window
                Browser.ExecuteFunctionEvent(null);
            }
        }
    }
}
