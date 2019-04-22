using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;
using XenRP.Client.model;

namespace XenRP.Client.factions {
    internal class Police : Events.Script {
        public static bool handcuffed;
        private readonly Dictionary<int, Blip> reinforces;
        private string crimesJson;
        private string crimesList;
        private string selectedControl;

        public Police() {
            Events.Add("showCrimesMenu", ShowCrimesMenuEvent);
            Events.Add("applyCrimes", ApplyCrimesEvent);
            Events.Add("executePlayerCrimes", ExecutePlayerCrimesEvent);
            Events.Add("backCrimesMenu", BackCrimesMenuEvent);
            Events.Add("loadPoliceControlList", LoadPoliceControlListEvent);
            Events.Add("proccessPoliceControlAction", ProccessPoliceControlActionEvent);
            Events.Add("policeControlSelectedName", PoliceControlSelectedNameEvent);
            Events.Add("updatePoliceReinforces", UpdatePoliceReinforcesEvent);
            Events.Add("reinforcesRemove", ReinforcesRemoveEvent);

            Events.AddDataHandler("PLAYER_HANDCUFFED", PlayerHandcuffedStateChanged);

            // Initialize the reinforces
            reinforces = new Dictionary<int, Blip>();
        }

        private void ShowCrimesMenuEvent(object[] args) {
            // Save crimes list
            crimesJson = args[0].ToString();

            // Show crimes menu
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/sideMenu.html", "populateCrimesMenu", crimesJson, string.Empty});
        }

        private void ApplyCrimesEvent(object[] args) {
            // Store crimes to be applied
            crimesList = args[0].ToString();

            // Destroy crimes menu
            Browser.DestroyBrowserEvent(null);

            // Show the confirmation window
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/crimesConfirm.html", "populateCrimesConfirmMenu", crimesList});
        }

        private void ExecutePlayerCrimesEvent(object[] args) {
            // Destroy the confirmation menu
            Browser.DestroyBrowserEvent(null);

            // Apply crimes to the player
            Events.CallRemote("applyCrimesToPlayer", crimesList);
        }

        private void BackCrimesMenuEvent(object[] args) {
            // Destroy the confirmation menu
            Browser.DestroyBrowserEvent(null);

            // Show crimes menu
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/sideMenu.html", "populateCrimesMenu", crimesJson, crimesList});
        }

        private void LoadPoliceControlListEvent(object[] args) {
            // Show the menu with the police control list
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/sideMenu.html", "populatePoliceControlMenu", args[0].ToString()});
        }

        private void ProccessPoliceControlActionEvent(object[] args) {
            // Get the variables from the arguments
            var control = args[0] == null ? string.Empty : args[0].ToString();

            // Check the selected option
            var controlOption = (int) Player.LocalPlayer.GetSharedData("PLAYER_POLICE_CONTROL");

            switch (controlOption) {
                case 1:
                    if (control.Length == 0)
                        Browser.CreateBrowserEvent(new object[] {"package://statics/html/policeControlName.html"});
                    else
                        Events.CallRemote("policeControlSelected", control);
                    break;
                case 2:
                    // Show the window to change control's name
                    Browser.CreateBrowserEvent(new object[] {"package://statics/html/policeControlName.html"});
                    selectedControl = control;
                    break;
                default:
                    // Execute the option over the police control
                    Events.CallRemote("policeControlSelected", control);
                    break;
            }
        }

        private void PoliceControlSelectedNameEvent(object[] args) {
            // Save the police control with a new name
            Events.CallRemote("updatePoliceControlName", selectedControl, args[0].ToString());
        }

        private void UpdatePoliceReinforcesEvent(object[] args) {
            var updatedReinforces = JsonConvert.DeserializeObject<List<Reinforces>>(args[0].ToString());

            // Search for policemen asking for reinforces
            foreach (var reinforcesModel in updatedReinforces) {
                // Get the identifier
                var police = reinforcesModel.playerId;
                var position = reinforcesModel.position;

                if (reinforces.ContainsKey(police)) {
                    // Update the blip's position
                    reinforces[police].SetCoords(position.X, position.Y, position.Z);
                }
                else {
                    // Create a blip on the map
                    var reinforcesBlip = new Blip(487, position, string.Empty, 1, 38);

                    // Add the new member to the array
                    reinforces[police] = reinforcesBlip;
                }
            }
        }

        private void ReinforcesRemoveEvent(object[] args) {
            // Get the variables from the arguments
            var officer = Convert.ToInt32(args[0]);

            // Delete officer's reinforces
            reinforces[officer].Destroy();
            reinforces.Remove(officer);
        }

        private void PlayerHandcuffedStateChanged(Entity entity, object arg) {
            // Toggle the handcuffed state
            handcuffed = arg != null;
        }
    }
}