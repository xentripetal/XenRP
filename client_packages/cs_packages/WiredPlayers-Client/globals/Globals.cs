using RAGE;
using RAGE.Elements;
using System;
using System.Collections.Generic;
using WiredPlayers_Client.account;

namespace WiredPlayers_Client.globals
{
    class Globals : Events.Script
    {
        private static DateTime currentTime;
        public static bool playerLogged;

        public Globals()
        {
            Events.Add("changePlayerWalkingStyle", ChangePlayerWalkingStyleEvent);
            Events.Add("resetPlayerWalkingStyle", ResetPlayerWalkingStyleEvent);
            Events.AddDataHandler("SERVER_TIME", PlayerConnectionStateChanged);
            Events.OnGuiReady += OnGuiReadyEvent;
            Events.Tick += TickEvent;
        }

        private void ChangePlayerWalkingStyleEvent(object[] args)
        {
            // Get the player
            Player player = (Player)args[0];
            string clipSet = args[1].ToString();

            player.SetMovementClipset(clipSet, 0.1f);
        }

        private void ResetPlayerWalkingStyleEvent(object[] args)
        {
            // Get the player
            Player player = (Player)args[0];

            player.ResetMovementClipset(0.0f);
        }

        public static void OnGuiReadyEvent()
        {
            // Remove health regeneration
            RAGE.Game.Player.SetPlayerHealthRechargeMultiplier(0.0f);

            // Remove weapons from the vehicles
            RAGE.Game.Player.DisablePlayerVehicleRewards();

            // Freeze the player until he logs in
            Player.LocalPlayer.FreezePosition(true);
        }

        private void PlayerConnectionStateChanged(Entity entity, object arg)
        {
            string[] serverTime = Player.LocalPlayer.GetSharedData("SERVER_TIME").ToString().Split(":");

            int hours = int.Parse(serverTime[0]);
            int minutes = int.Parse(serverTime[1]);
            int seconds = int.Parse(serverTime[2]);

            // Set the hour from the server
            RAGE.Game.Clock.SetClockTime(hours, minutes, seconds);

            // Get the current timestamp
            currentTime = DateTime.UtcNow;

            // Show the login window
            Login.AccountLoginFormEvent(null);
        }

        private void TickEvent(List<Events.TickNametagData> nametags)
        {
            // Check if the player is connected
            if (!playerLogged) return;

            DateTime dateTime = DateTime.UtcNow;

            // Check for the key 'E' being pressed
            if (Input.IsDown(0x45) && Player.LocalPlayer.Vehicle == null)
            {
                // Reset the player's animation
                Events.CallRemote("checkPlayerEventKeyStopAnim");

                return;
            }

            // Check for the key 'F' being pressed
            if (Input.IsDown(0x46) && Player.LocalPlayer.Vehicle == null)
            {
                // Check if player can enter any place
                Events.CallRemote("checkPlayerEventKey");
                return;
            }

            // Check for the key 'K' being pressed
            if (Input.IsDown(0x4B) && Player.LocalPlayer.Vehicle != null)
            {
                if (!Player.LocalPlayer.Vehicle.IsSeatFree(-1, 0) && Player.LocalPlayer.Vehicle.GetPedInSeat(-1, 0) == Player.LocalPlayer.Handle)
                {
                    // Toggle vehicle's engine
                    Events.CallRemote("engineOnEventKey");
                }

                return;
            }
        }
    }
}
