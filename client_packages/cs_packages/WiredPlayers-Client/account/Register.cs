using RAGE;
using RAGE.Elements;
using WiredPlayers_Client.globals;

namespace WiredPlayers_Client.account
{
    class Register : Events.Script
    {
        public Register()
        {
            Events.Add("showRegisterWindow", ShowRegisterWindowEvent);
            Events.Add("createPlayerAccount", CreatePlayerAccountEvent);
            Events.Add("clearRegisterWindow", ClearRegisterWindowEvent);
        }

        private void ShowRegisterWindowEvent(object[] args)
        {
            // Create register window
            Browser.CreateBrowserEvent(new object[] { "package://statics/html/register.html" });
        }

        private void CreatePlayerAccountEvent(object[] args)
        {
            // Get the password from the array
            string password = args[0].ToString();

            // Create login window
            Events.CallRemote("registerAccount", password);
        }

        private void ClearRegisterWindowEvent(object[] args)
        {
            // Unfreeze the player
            Player.LocalPlayer.FreezePosition(false);

            // Show the message on the panel
            Browser.DestroyBrowserEvent(null);

            // Show the player as logged
            Globals.playerLogged = true;
        }
    }
}

