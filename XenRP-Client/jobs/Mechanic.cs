using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;
using XenRP.Client.model;
using Ui = RAGE.Game.Ui;

namespace XenRP.Client.jobs {
    internal class Mechanic : Events.Script {
        public Mechanic() {
            Events.Add("showTunningMenu", ShowTunningMenuEvent);
            Events.Add("showRepaintMenu", ShowRepaintMenuEvent);
            Events.Add("addVehicleComponent", AddVehicleComponentEvent);
            Events.Add("confirmVehicleModification", ConfirmVehicleModificationEvent);
            Events.Add("cancelVehicleModification", CancelVehicleModificationEvent);
            Events.Add("repaintVehicle", RepaintVehicleEvent);
            Events.Add("closeRepaintWindow", CloseRepaintWindowEvent);
        }

        private void ShowTunningMenuEvent(object[] args) {
            // Initialize the list
            var vehicleComponents = new List<CarPiece>();

            foreach (var pieceGroup in Constants.CAR_PIECE_LIST) {
                // Get the number of mods for each component group
                var modNumber = Player.LocalPlayer.Vehicle.GetNumMods(pieceGroup.slot);

                if (modNumber > 0) {
                    // Initialize the components list
                    pieceGroup.components = new List<CarPiece>();

                    for (var i = 0; i < modNumber; i++) {
                        // Create the component
                        var piece = new CarPiece(i, pieceGroup.desc + " " + (i + 1));
                        pieceGroup.components.Add(piece);
                    }

                    // Add all the pieces to the list
                    vehicleComponents.Add(pieceGroup);
                }
            }

            // Show the tunning menu
            Browser.CreateBrowserEvent(new object[] {
                "package://statics/html/sideMenu.html", "populateTunningMenu",
                JsonConvert.SerializeObject(vehicleComponents)
            });
        }

        private void ShowRepaintMenuEvent(object[] args) {
            // Disable the HUD
            Ui.DisplayHud(false);
            Ui.DisplayRadar(false);

            // Show the paint menu
            Browser.CreateBrowserEvent(new object[] {"package://statics/html/repaintVehicle.html"});
        }

        private void AddVehicleComponentEvent(object[] args) {
            // Get the variables from the array
            var slot = Convert.ToInt32(args[0]);
            var component = Convert.ToInt32(args[1]);

            // Añadimos el componente al vehículo
            Player.LocalPlayer.Vehicle.SetMod(slot, component, false);
        }

        private void ConfirmVehicleModificationEvent(object[] args) {
            // Get the variables from the array
            var slot = Convert.ToInt32(args[0]);
            var mod = Convert.ToInt32(args[1]);

            // Add the tunning to the vehicle
            Events.CallRemote("confirmVehicleModification", slot, mod);
        }

        private void CancelVehicleModificationEvent(object[] args) {
            // Clear the tunning from the vehicle
            Events.CallRemote("cancelVehicleModification");
        }

        private void RepaintVehicleEvent(object[] args) {
            // Get the variables from the array
            var colorType = Convert.ToInt32(args[0]);
            var firstColor = args[1].ToString();
            var secondColor = args[2].ToString();
            var pearlescentColor = Convert.ToInt32(args[3]);
            var paid = Convert.ToInt32(args[4]);

            // Repaint the vehicle
            Events.CallRemote("repaintVehicle", colorType, firstColor, secondColor, pearlescentColor, paid);
        }

        private void CloseRepaintWindowEvent(object[] args) {
            // Enable the HUD
            Ui.DisplayHud(true);
            Ui.DisplayRadar(true);

            // Destroy the browser
            Browser.DestroyBrowserEvent(null);

            // Restore the vehicle's colors
            Events.CallRemote("cancelVehicleRepaint");
        }
    }
}