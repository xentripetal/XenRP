using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using RAGE.Game;
using XenRP.Client.globals;
using Entity = RAGE.Elements.Entity;
using Player = RAGE.Elements.Player;
using Type = RAGE.Elements.Type;
using Vehicle = RAGE.Elements.Vehicle;

namespace XenRP.Client.vehicles {
    internal class Vehicles : Events.Script {
        private static bool seatbelt;
        private static float kms;
        private static float gas;
        private static float distance;
        private static float consumed;

        public static Vector3 lastPosition;
        public static Vehicle lastVehicle;
        private Blip vehicleLocationBlip;

        public Vehicles() {
            Events.Add("initializeSpeedometer", InitializeSpeedometerEvent);
            Events.Add("locateVehicle", LocateVehicleEvent);
            Events.Add("deleteVehicleLocation", DeleteVehicleLocationEvent);
            Events.Add("removeSpeedometer", RemoveSpeedometerEvent);
            Events.Add("toggleVehicleDoor", ToggleVehicleDoorEvent);
            Events.Add("toggleSeatbelt", ToggleSeatbeltEvent);
            Events.OnPlayerLeaveVehicle += PlayerLeaveVehicleEvent;
            Events.OnEntityStreamIn += EntityStreamInEvent;

            // Initialize the seatbelt state
            Player.LocalPlayer.SetConfigFlag(32, !seatbelt);
        }

        public static void UpdateSpeedometer() {
            lastVehicle = Player.LocalPlayer.Vehicle;
            var currentPosition = lastVehicle.Position;

            // Get speedometer's data
            var velocity = lastVehicle.GetVelocity();
            var health = lastVehicle.GetHealth();
            var maxHealth = lastVehicle.GetMaxHealth();

            var healthPercent = (int) Math.Round((decimal) (health * 100) / maxHealth);
            var speed = (int) Math.Round(
                Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y + velocity.Z * velocity.Z) * 3.6f);

            // Get the distance and consume
            distance = Vector3.Distance(currentPosition, lastPosition);
            consumed = distance * Constants.CONSUME_PER_METER;
            lastPosition = currentPosition;

            if (gas - consumed <= 0.0f) {
                // The fuel tank is empty
                Events.CallRemote("stopPlayerCar");
                consumed = 0.0f;
            }

            // Get the total gas and kms
            var totalKms = Math.Round((double) (kms + distance) / 10) / 100 + " km";
            var totalGas = Math.Round((double) (gas - consumed) * 100) / 100 + " litros";

            // Draw the speedometer
            UIText.Draw("Combustible: ", new Point(1025, 560), 0.5f, Color.White, Font.ChaletComprimeCologne, false);
            UIText.Draw(totalGas, new Point(1175, 560), 0.5f, Color.White, Font.ChaletComprimeCologne, false);
            UIText.Draw("Kilometraje: ", new Point(1025, 590), 0.5f, Color.White, Font.ChaletComprimeCologne, false);
            UIText.Draw(totalKms, new Point(1175, 590), 0.5f, Color.White, Font.ChaletComprimeCologne, false);
            UIText.Draw("Kmph: ", new Point(1025, 650), 0.5f, Color.White, Font.ChaletComprimeCologne, false);
            UIText.Draw(speed.ToString(), new Point(1175, 650), 0.75f, Color.White, Font.ChaletComprimeCologne, false);
            UIText.Draw("Integridad: ", new Point(1025, 620), 0.5f, Color.White, Font.ChaletComprimeCologne, false);

            if (healthPercent < 30)
                UIText.Draw(healthPercent + "%", new Point(1175, 620), 0.5f, Color.Red, Font.ChaletComprimeCologne,
                    false);
            else if (healthPercent < 60)
                UIText.Draw(healthPercent + "%", new Point(1175, 620), 0.5f, Color.Yellow, Font.ChaletComprimeCologne,
                    false);
            else
                UIText.Draw(healthPercent + "%", new Point(1175, 620), 0.5f, Color.White, Font.ChaletComprimeCologne,
                    false);

            // Update the vehicle's values
            kms += distance;
            gas -= consumed;

            // Reinitialize the variables
            distance = 0.0f;
            consumed = 0.0f;
        }

        private void InitializeSpeedometerEvent(object[] args) {
            // Initialize the kilometers and gas
            kms = (float) Convert.ToDouble(args[0]);
            gas = (float) Convert.ToDouble(args[1]);

            // Don't let the player turn the engine on if by default
            Player.LocalPlayer.Vehicle.SetEngineOn(Convert.ToBoolean(args[2]), true, true);

            // Initialize the counters
            distance = 0.0f;
            consumed = 0.0f;
            lastPosition = Player.LocalPlayer.Vehicle.Position;
        }

        private void LocateVehicleEvent(object[] args) {
            // Get the variables from the array
            var position = (Vector3) args[0];

            // Create the blip on the map
            vehicleLocationBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteVehicleLocationEvent(object[] args) {
            // Destroy the blip on the map
            vehicleLocationBlip.Destroy();
            vehicleLocationBlip = null;
        }

        public static void RemoveSpeedometerEvent(object[] args) {
            if (seatbelt) {
                seatbelt = false;
                Events.CallRemote("toggleSeatbelt", seatbelt);
            }

            // Reset the vehicle's position
            lastPosition = null;

            // Save the kilometers and gas
            Events.CallRemote("saveVehicleConsumes", lastVehicle, kms, gas);

            // Reset the player's vehicle
            lastVehicle = null;
        }

        private void ToggleVehicleDoorEvent(object[] args) {
            // Get the values from the server
            var vehicleId = Convert.ToInt32(args[0]);
            var door = Convert.ToInt32(args[1]);
            var opened = Convert.ToBoolean(args[2]);

            // Get the vehicle from the server
            var vehicle = Entities.Vehicles.GetAtRemote((ushort) vehicleId);

            if (opened)
                vehicle.SetDoorOpen(door, false, false);
            else
                vehicle.SetDoorShut(door, true);
        }

        private void ToggleSeatbeltEvent(object[] args) {
            // Change the seatbelt state
            seatbelt = !seatbelt;
            Player.LocalPlayer.SetConfigFlag(32, !seatbelt);

            // Send the message to the players nearby
            Events.CallRemote("toggleSeatbelt", seatbelt);
        }

        private void PlayerLeaveVehicleEvent() {
            if (lastPosition != null) RemoveSpeedometerEvent(null);
        }

        private void EntityStreamInEvent(Entity entity) {
            if (entity.Type == Type.Vehicle) {
                // Get the vehicle from the entity
                var vehicle = (Vehicle) entity;

                // Get the state for each one of the doors
                var doorsJson = entity.GetSharedData(Constants.VEHICLE_DOORS_STATE).ToString();
                var doorStateList = JsonConvert.DeserializeObject<List<bool>>(doorsJson);

                for (var i = 0; i < doorStateList.Count; i++)
                    if (doorStateList[i])
                        vehicle.SetDoorOpen(i, false, false);
                    else
                        vehicle.SetDoorShut(i, true);
            }
        }
    }
}