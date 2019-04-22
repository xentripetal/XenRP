using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;

namespace XenRP.Client.account {
    internal class Login : Events.Script {
        public Login() {
            Events.Add("accountLoginForm", AccountLoginFormEvent);
            Events.Add("requestPlayerLogin", RequestPlayerLoginEvent);
            Events.Add("showLoginError", ShowLoginErrorEvent);
            Events.Add("clearLoginWindow", ClearLoginWindowEvent);
        }

        public static void AccountLoginFormEvent(object[] args) {
            // Create login window
            Browser.CreateBrowserEvent(new object[] {"package://statics/html/login.html"});
        }

        private void RequestPlayerLoginEvent(object[] args) {
            // Get the password from the array
            var password = args[0].ToString();

            // Check for the credentials
            Events.CallRemote("loginAccount", password);
        }

        private void ShowLoginErrorEvent(object[] args) {
            // Show the message on the panel
            Browser.ExecuteFunctionEvent(new object[] {"showLoginError"});
        }

        private void ClearLoginWindowEvent(object[] args) {
            // Unfreeze the player
            Player.LocalPlayer.FreezePosition(false);

            // Show the message on the panel
            Browser.DestroyBrowserEvent(null);
        }
    }
}