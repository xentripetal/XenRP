using RAGE;
using RAGE.Elements;

namespace WiredPlayers_Client.weapons
{
    class Weapons : Events.Script
    {
        private Blip weaponBlip = null;

        public Weapons()
        {
            Events.Add("getPlayerWeapons", GetPlayerWeaponsEvent);
            Events.Add("showWeaponCheckpoint", ShowWeaponCheckpointEvent);
            Events.Add("deleteWeaponCheckpoint", DeleteWeaponCheckpointEvent);

            Events.OnPlayerWeaponShot += OnPlayerWeaponShotEvent;
        }

        private void GetPlayerWeaponsEvent(object[] args)
        {
            string callback = args[0].ToString();
        }

        private void ShowWeaponCheckpointEvent(object[] args)
        {
            // Get the variables from the array
            Vector3 position = (Vector3)args[0];

            // Set the checkpoint with the crates
            weaponBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteWeaponCheckpointEvent(object[] args)
        {
            // Delete the checkpoint on the map
            weaponBlip.Destroy();
            weaponBlip = null;
        }

        private void OnPlayerWeaponShotEvent(Vector3 targetPos, Player target, Events.CancelEventArgs cancel)
        {
            // Calculate the weapon the player is holding
            uint weaponHash = RAGE.Game.Weapon.GetSelectedPedWeapon(Player.LocalPlayer.Handle);

            // Get the bullets remaining
            int bullets = Player.LocalPlayer.GetAmmoInWeapon(weaponHash);

            // Update the weapon's bullet amount
            Events.CallRemote("updateWeaponBullets", bullets);
        }
    }
}
