using System.Linq;
using RAGE;
using RAGE.Elements;
using RAGE.Game;
using XenRP.Client.globals;
using Player = RAGE.Elements.Player;

namespace XenRP.Client.weapons {
    internal class Weapons : Events.Script {
        private Blip weaponBlip;

        public Weapons() {
            Events.Add("makePlayerReload", MakePlayerReloadEvent);
            Events.Add("getPlayerWeapons", GetPlayerWeaponsEvent);
            Events.Add("showWeaponCheckpoint", ShowWeaponCheckpointEvent);
            Events.Add("deleteWeaponCheckpoint", DeleteWeaponCheckpointEvent);

            Events.OnPlayerWeaponShot += OnPlayerWeaponShotEvent;
        }

        public static bool IsValidWeapon(int weapon) {
            return Constants.VALID_WEAPONS.Where(w => Misc.GetHashKey(w) == (uint) weapon).Count() > 0;
        }

        private void MakePlayerReloadEvent(object[] args) {
            // Reload the weapon
            Player.LocalPlayer.TaskReloadWeapon(true);
        }

        private void GetPlayerWeaponsEvent(object[] args) {
            var callback = args[0].ToString();
        }

        private void ShowWeaponCheckpointEvent(object[] args) {
            // Get the variables from the array
            var position = (Vector3) args[0];

            // Set the checkpoint with the crates
            weaponBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteWeaponCheckpointEvent(object[] args) {
            // Delete the checkpoint on the map
            weaponBlip.Destroy();
            weaponBlip = null;
        }

        private void OnPlayerWeaponShotEvent(Vector3 targetPos, Player target, Events.CancelEventArgs cancel) {
            // Calculate the weapon the player is holding
            var weaponHash = Weapon.GetSelectedPedWeapon(Player.LocalPlayer.Handle);

            // Get the bullets remaining
            var bullets = Player.LocalPlayer.GetAmmoInWeapon(weaponHash);

            // Update the weapon's bullet amount
            Events.CallRemote("updateWeaponBullets", bullets);
        }
    }
}