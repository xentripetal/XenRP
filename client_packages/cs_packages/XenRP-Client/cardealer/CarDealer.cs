using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using RAGE.Game;
using XenRP.Client.globals;
using XenRP.Client.model;
using Player = RAGE.Elements.Player;
using Vehicle = RAGE.Elements.Vehicle;

namespace XenRP.Client.cardealer {
    internal class CarDealer : Events.Script {
        private Blip carShopTestBlip;
        private string carShopVehiclesJson;
        private int dealership;
        private int previewCamera;
        private Vehicle previewVehicle;

        public CarDealer() {
            Events.Add("showVehicleCatalog", ShowVehicleCatalogEvent);
            Events.Add("previewCarShopVehicle", PreviewCarShopVehicleEvent);
            Events.Add("rotatePreviewVehicle", RotatePreviewVehicleEvent);
            Events.Add("previewVehicleChangeColor", PreviewVehicleChangeColorEvent);
            Events.Add("showCatalog", ShowCatalogEvent);
            Events.Add("closeCatalog", CloseCatalogEvent);
            Events.Add("checkVehiclePayable", CheckVehiclePayableEvent);
            Events.Add("purchaseVehicle", PurchaseVehicleEvent);
            Events.Add("testVehicle", TestVehicleEvent);
            Events.Add("showCarshopCheckpoint", ShowCarshopCheckpointEvent);
            Events.Add("deleteCarshopCheckpoint", DeleteCarshopCheckpointEvent);
        }

        private void ShowVehicleCatalogEvent(object[] args) {
            // Get the variables from the arguments
            carShopVehiclesJson = args[0].ToString();
            dealership = Convert.ToInt32(args[1]);

            // Disable the chat
            Chat.Activate(false);
            Chat.Show(false);

            // Show the catalog
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/vehicleCatalog.html", "populateVehicleList", dealership, carShopVehiclesJson});
        }

        private void PreviewCarShopVehicleEvent(object[] args) {
            // Get the variables from the arguments
            var model = args[0].ToString();

            if (previewVehicle != null) previewVehicle.Destroy();

            // Destroy the catalog
            Browser.DestroyBrowserEvent(null);

            switch (dealership) {
                case 2:
                    previewVehicle = new Vehicle(Misc.GetHashKey(model), new Vector3(-878.5726f, -1353.408f, 0.1741f),
                        90.0f);
                    previewCamera = Cam.CreateCameraWithParams(Misc.GetHashKey("DEFAULT_SCRIPTED_CAMERA"), -882.3361f,
                        -1342.628f, 5.0783f, -20.0f, 0.0f, 200.0f, 90.0f, true, 2);
                    break;
                default:
                    previewVehicle = new Vehicle(Misc.GetHashKey(model), new Vector3(-31.98111f, -1090.434f, 26.42225f),
                        180.0f);
                    previewCamera = Cam.CreateCameraWithParams(Misc.GetHashKey("DEFAULT_SCRIPTED_CAMERA"), -37.83527f,
                        -1088.096f, 27.92234f, -20.0f, 0.0f, 250, 90.0f, true, 2);
                    break;
            }

            // Make the camera point the vehicle
            Cam.SetCamActive(previewCamera, true);
            Cam.RenderScriptCams(true, false, 0, true, false, 0);

            // Disable the HUD
            Ui.DisplayHud(false);
            Ui.DisplayRadar(false);

            // Vehicle preview menu
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/vehiclePreview.html", "checkVehiclePayable"});
        }

        private void RotatePreviewVehicleEvent(object[] args) {
            // Get the variables from the arguments
            var rotation = (float) Convert.ToDouble(args[0]);

            // Set the vehicle's heading
            previewVehicle.SetHeading(rotation);
        }

        private void PreviewVehicleChangeColorEvent(object[] args) {
            // Get the variables from the arguments
            var colorHex = args[0].ToString().Substring(1);
            var colorMain = (bool) args[1];

            // Get the RGB from HEX string
            var red = Convert.ToInt32(colorHex.Substring(0, 2), 16);
            var green = Convert.ToInt32(colorHex.Substring(2, 2), 16);
            var blue = Convert.ToInt32(colorHex.Substring(4, 2), 16);

            if (colorMain)
                previewVehicle.SetCustomPrimaryColour(red, green, blue);
            else
                previewVehicle.SetCustomSecondaryColour(red, green, blue);
        }

        private void ShowCatalogEvent(object[] args) {
            // Destroy preview menu
            Browser.DestroyBrowserEvent(null);

            // Destroy the vehicle
            previewVehicle.Destroy();
            previewVehicle = null;

            // Enable the HUD
            Ui.DisplayHud(true);
            Ui.DisplayRadar(true);

            // Position the camera behind the character
            Cam.DestroyCam(previewCamera, true);
            Cam.RenderScriptCams(false, false, 0, true, false, 0);

            // Show the catalog
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/vehicleCatalog.html", "populateVehicleList", dealership, carShopVehiclesJson});
        }

        private void CloseCatalogEvent(object[] args) {
            // Destroy preview catalog
            Browser.DestroyBrowserEvent(null);

            // Enable the chat
            Chat.Activate(true);
            Chat.Show(true);
        }

        private void CheckVehiclePayableEvent(object[] args) {
            // Get the vehicles' list
            var vehicleList = JsonConvert.DeserializeObject<List<CarDealerVehicle>>(carShopVehiclesJson);

            foreach (var veh in vehicleList)
                if (Misc.GetHashKey(veh.model) == previewVehicle.Model) {
                    // Check if the player has enough money in the bank
                    var playerBankMoney = (int) Player.LocalPlayer.GetSharedData("PLAYER_BANK");

                    if (playerBankMoney >= veh.price)
                        Browser.ExecuteFunctionEvent(new object[] {"showVehiclePurchaseButton"});
                    break;
                }
        }

        private void PurchaseVehicleEvent(object[] args) {
            // Get the colors variables
            int primaryRed = 0, primaryGreen = 0, primaryBlue = 0;
            int secondaryRed = 0, secondaryGreen = 0, secondaryBlue = 0;

            // Get the vehicle's data
            var model = previewVehicle.Model.ToString();
            previewVehicle.GetCustomPrimaryColour(ref primaryRed, ref primaryGreen, ref primaryBlue);
            previewVehicle.GetCustomSecondaryColour(ref secondaryRed, ref secondaryGreen, ref secondaryBlue);

            // Get color strings
            var firstColor = string.Format("{0},{1},{2}", primaryRed, primaryGreen, primaryBlue);
            var secondColor = string.Format("{0},{1},{2}", secondaryRed, secondaryGreen, secondaryBlue);

            // Destroy preview menu
            CloseCatalogEvent(null);

            // Destroy preview vehicle
            previewVehicle.Destroy();
            previewVehicle = null;

            // Enable the HUD
            Ui.DisplayHud(true);
            Ui.DisplayRadar(true);

            // Position the camera behind the character
            Cam.DestroyCam(previewCamera, true);
            Cam.RenderScriptCams(false, false, 0, true, false, 0);

            // Purchase the vehicle
            Events.CallRemote("purchaseVehicle", model, firstColor, secondColor);
        }

        private void TestVehicleEvent(object[] args) {
            // Get the vehicle's data
            var model = previewVehicle.Model.ToString();

            // Destroy preview menu
            CloseCatalogEvent(null);

            // Destroy preview vehicle
            previewVehicle.Destroy();
            previewVehicle = null;

            // Enable the HUD
            Ui.DisplayHud(true);
            Ui.DisplayRadar(true);

            // Position the camera behind the character
            Cam.DestroyCam(previewCamera, true);
            Cam.RenderScriptCams(false, false, 0, true, false, 0);

            // Purchase the vehicle
            Events.CallRemote("testVehicle", model);
        }

        private void ShowCarshopCheckpointEvent(object[] args) {
            // Get the variables from the arguments
            var position = (Vector3) args[0];

            // Add a blip with the delivery place
            carShopTestBlip = new Blip(1, position, string.Empty, 1f, 1);
        }

        private void DeleteCarshopCheckpointEvent(object[] args) {
            // Delete the blip
            carShopTestBlip.Destroy();
            carShopTestBlip = null;
        }
    }
}