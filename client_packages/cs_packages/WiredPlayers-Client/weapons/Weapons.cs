using System;
using RAGE;
using RAGE.Elements;

namespace WiredPlayers_Client.weapons
{
    class Weapons : Events.Script
    {
        private Checkpoint weaponCheckpoint = null;

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
            weaponCheckpoint = new Checkpoint(0, position, 9.0f, new Vector3(0.0f, 0.0f, 0.0f), new RGBA(255, 0, 0, 70));
        }

        private void DeleteWeaponCheckpointEvent(object[] args)
        {
            // Delete the checkpoint on the map
            weaponCheckpoint.Destroy();
            weaponCheckpoint = null;
        }

        private void OnPlayerWeaponShotEvent(Vector3 targetPos, Player target, Events.CancelEventArgs cancel)
        {
            // Calculate the weapon the player is holding
            uint weaponHash = RAGE.Game.Invoker.Invoke<uint>(RAGE.Game.Natives.GetSelectedPedWeapon, Player.LocalPlayer.Handle);

            // Get the bullets remaining
            int bullets = Player.LocalPlayer.GetAmmoInWeapon(weaponHash);

            // Update the weapon's bullet amount
            Events.CallRemote("updateWeaponBullets", bullets);
        }
    }
}
