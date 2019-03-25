using RAGE;
using RAGE.Elements;
using WiredPlayers_Client.globals;
using System;
using System.Drawing;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WiredPlayers_Client.vehicles
{
    class Vehicles : Events.Script
    {
        private Blip vehicleLocationBlip = null;

        private static float kms = 0.0f;
        private static float gas = 0.0f;
        private static float distance = 0.0f;
        private static float consumed = 0.0f;
        public static Vector3 lastPosition = null;

        public Vehicles()
        {
            Events.Add("initializeSpeedometer", InitializeSpeedometerEvent);
            Events.Add("locateVehicle", LocateVehicleEvent);
            Events.Add("deleteVehicleLocation", DeleteVehicleLocationEvent);
            Events.Add("removeSpeedometer", RemoveSpeedometerEvent);
            Events.Add("toggleVehicleDoor", ToggleVehicleDoorEvent);
            Events.OnPlayerLeaveVehicle += PlayerLeaveVehicleEvent;
            Events.OnEntityStreamIn += EntityStreamInEvent;
        }

        public static void UpdateSpeedometer()
        {
            Vehicle vehicle = Player.LocalPlayer.Vehicle;
            Vector3 currentPosition = vehicle.Position;

            // Get speedometer's data
            Vector3 velocity = vehicle.GetVelocity();
            int health = vehicle.GetHealth();
            int maxHealth = vehicle.GetMaxHealth();

            int healthPercent = (int)Math.Round((decimal)(health  * 100) / maxHealth);
            int speed = (int)Math.Round(Math.Sqrt(velocity.X * velocity.X + velocity.Y * velocity.Y + velocity.Z * velocity.Z) * 3.6f);

            // Get the distance and consume
            distance = Vector3.Distance(currentPosition, lastPosition);
            consumed = distance * Constants.CONSUME_PER_METER;
            lastPosition = currentPosition;

            if(gas - consumed <= 0.0f)
            {
                // The fuel tank is empty
                Events.CallRemote("stopPlayerCar");
                consumed = 0.0f;
            }

            // Get the total gas and kms
            string totalKms = Math.Round((double)(kms + distance) / 10) / 100 + " km";
            string totalGas = Math.Round((double)(gas - consumed) * 100) / 100 + " litros";

            // Draw the speedometer
            RAGE.Game.UIText.Draw("Combustible: ", new Point(1025, 560), 0.5f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);
            RAGE.Game.UIText.Draw(totalGas, new Point(1175, 560), 0.5f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);
            RAGE.Game.UIText.Draw("Kilometraje: ", new Point(1025, 590), 0.5f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);
            RAGE.Game.UIText.Draw(totalKms, new Point(1175, 590), 0.5f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);
            RAGE.Game.UIText.Draw("Kmph: ", new Point(1025, 650), 0.5f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);
            RAGE.Game.UIText.Draw(speed.ToString(), new Point(1175, 650), 0.75f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);
            RAGE.Game.UIText.Draw("Integridad: ", new Point(1025, 620), 0.5f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);

            if (healthPercent < 30)
            {
                RAGE.Game.UIText.Draw(healthPercent + "%", new Point(1175, 620), 0.5f, Color.Red, RAGE.Game.Font.ChaletComprimeCologne, false);
            }
            else if (healthPercent < 60)
            {
                RAGE.Game.UIText.Draw(healthPercent + "%", new Point(1175, 620), 0.5f, Color.Yellow, RAGE.Game.Font.ChaletComprimeCologne, false);
            }
            else
            {
                RAGE.Game.UIText.Draw(healthPercent + "%", new Point(1175, 620), 0.5f, Color.White, RAGE.Game.Font.ChaletComprimeCologne, false);
            }

            // Update the vehicle's values
            kms += distance;
            gas -= consumed;

            // Reinitialize the variables
            distance = 0.0f;
            consumed = 0.0f;
        }

        private void InitializeSpeedometerEvent(object[] args)
        {
            // Initialize the kilometers and gas
            kms = (float)Convert.ToDouble(args[0]);
            gas = (float)Convert.ToDouble(args[1]);

            // Don't let the player turn the engine on if by default
            Player.LocalPlayer.Vehicle.SetEngineOn(Convert.ToBoolean(args[2]), true, true);

            // Initialize the counters
            distance = 0.0f;
            consumed = 0.0f;
            lastPosition = Player.LocalPlayer.Vehicle.Position;
        }

        private void LocateVehicleEvent(object[] args)
        {
            // Get the variables from the array
            Vector3 position = (Vector3)args[0];

            // Create the blip on the map
            vehicleLocationBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteVehicleLocationEvent(object[] args)
        {
            // Destroy the blip on the map
            vehicleLocationBlip.Destroy();
            vehicleLocationBlip = null;
        }

        private void RemoveSpeedometerEvent(object[] args)
        {
            // Reset the vehicle's position
            lastPosition = null;

            // Save the kilometers and gas
            Events.CallRemote("saveVehicleConsumes", Player.LocalPlayer.Vehicle, kms, gas);
        }

        private void ToggleVehicleDoorEvent(object[] args)
        {
            // Get the values from the server
            int vehicleId = Convert.ToInt32(args[0]);
            int door = Convert.ToInt32(args[1]);
            bool opened = Convert.ToBoolean(args[2]);

            // Get the vehicle from the server
            Vehicle vehicle = Entities.Vehicles.GetAtRemote((ushort)vehicleId);

            if(opened)
            {
                // Open the selected door
                vehicle.SetDoorOpen(door, false, false);
            }
            else
            {
                // Close the selected door
                vehicle.SetDoorShut(door, true);
            }
        }

        private void PlayerLeaveVehicleEvent()
        {
            if (lastPosition != null)
            {
                // Save and remove the speedometer
                RemoveSpeedometerEvent(null);
            }
        }

        private void EntityStreamInEvent(Entity entity)
        {
            if(entity.Type == RAGE.Elements.Type.Vehicle)
            {
                // Get the vehicle from the entity
                Vehicle vehicle = (Vehicle)entity;

                // Get the state for each one of the doors
                string doorsJson = entity.GetSharedData(Constants.VEHICLE_DOORS_STATE).ToString();
                List<bool> doorStateList = JsonConvert.DeserializeObject<List<bool>>(doorsJson);

                for(int i = 0; i < doorStateList.Count; i++)
                {
                    if (doorStateList[i])
                    {
                        // Open the selected door
                        vehicle.SetDoorOpen(i, false, false);
                    }
                    else
                    {
                        // Close the selected door
                        vehicle.SetDoorShut(i, true);
                    }
                }

            }
        }
    }
}
