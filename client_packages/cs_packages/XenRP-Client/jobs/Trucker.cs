using System.Collections.Generic;
using System.Linq;
using RAGE;
using RAGE.Elements;
using RAGE.Game;
using XenRP.Client.globals;
using Player = RAGE.Elements.Player;
using Vehicle = RAGE.Elements.Vehicle;

namespace XenRP.Client.jobs {
    internal class Trucker : Events.Script {
        private static List<MapObject> crateList;

        public Trucker() {
            Events.Add("createTruckerCrates", CreateTruckerCratesEvent);
        }

        public static void CheckPlayerStoredCrate() {
            if (crateList != null && crateList.Count > 0 &&
                Misc.GetHashKey("forklift") == Player.LocalPlayer.Vehicle.Model) {
                // Check if the player has any crate near
                if (GetCrateInRange(Player.LocalPlayer.Vehicle, 1.5f) == null) return;

                // Store the crate into the closest vehicle
                var truck = StoreCrateIntoVehicle("mule");

                if (truck != null) {
                }
            }
            else {
                Player.LocalPlayer.Vehicle.SetDoorOpen(2, false, false);
                Player.LocalPlayer.Vehicle.SetDoorOpen(3, false, false);
            }
        }

        private void CreateTruckerCratesEvent(object[] args) {
            // Initialize the crates
            crateList = new List<MapObject>();

            foreach (var crate in Constants.TRUCKER_CRATES) {
                var crateObject = new MapObject(Misc.GetHashKey("prop_boxpile_04a"), crate,
                    new Vector3(0.0f, 0.0f, 0.0f));
                crateObject.SetPhysicsParams(17.5f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f,
                    -1.0f);
                crateObject.SetActivatePhysicsAsSoonAsItIsUnfrozen(true);
                crateObject.FreezePosition(false);

                // Add the crate to the list
                crateList.Add(crateObject);
            }
        }

        private static Vehicle StoreCrateIntoVehicle(string model, float distance = 2.5f) {
            var vehicleHash = Misc.GetHashKey(model);

            // Get the list of vehicles with selected model and trunk opened
            var truckList = Entities.Vehicles.Streamed.Where(veh => veh.IsDoorFullyOpen(2) && veh.Model == vehicleHash)
                .ToList();

            foreach (var truck in truckList) {
                // Get the closest crate
                var crate = GetCrateInRange(truck, distance);

                if (crate != null) {
                    // Remove the crate from the game
                    crateList.Remove(crate);
                    crate.Destroy();

                    return truck;
                }
            }

            return null;
        }

        private static MapObject GetCrateInRange(Vehicle truck, float distance) {
            foreach (var crate in crateList)
                if (crate.Position.DistanceTo(truck.Position) <= distance)
                    return crate;

            return null;
        }
    }
}