using System;
using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;
using Type = RAGE.Elements.Type;

namespace XenRP.Client.factions {
    internal class Faction : Events.Script {
        private Blip factionWarningBlip;

        public Faction() {
            Events.Add("showFactionWarning", ShowFactionWarningEvent);
            Events.Add("deleteFactionWarning", DeleteFactionWarningEvent);
            Events.Add("toggleSirenState", ToggleSirenStateEvent);
            Events.OnEntityStreamIn += OnEntityStreamInEvent;
        }

        private void ShowFactionWarningEvent(object[] args) {
            // Get the variables from the arguments
            var position = (Vector3) args[0];

            // Create the blip on the map
            factionWarningBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteFactionWarningEvent(object[] args) {
            // Destroy the blip on the map
            factionWarningBlip.Destroy();
            factionWarningBlip = null;
        }

        private void ToggleSirenStateEvent(object[] args) {
            // Get the vehicle from the parameter passed
            var value = Convert.ToInt32(args[0]);
            var siren = Convert.ToBoolean(args[1]);
            var vehicle = Entities.Vehicles.GetAtRemote((ushort) value);

            // Set the siren state
            vehicle.SetSirenSound(siren);
        }

        private void OnEntityStreamInEvent(Entity entity) {
            if (entity.Type == Type.Vehicle) {
                // Get the vehicle from the entity
                var vehicle = (Vehicle) entity;

                // Check if it's an emergency vehicle
                if (vehicle.GetClass() != Constants.VEHICLE_CLASS_EMERGENCY) return;

                // Set the state of the siren sound
                vehicle.SetSirenSound(!Convert.ToBoolean(vehicle.GetSharedData(Constants.VEHICLE_SIREN_SOUND)));
            }
        }
    }
}