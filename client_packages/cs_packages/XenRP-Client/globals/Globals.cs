using System;
using System.Collections.Generic;
using System.Drawing;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using RAGE.Game;
using RAGE.NUI;
using XenRP.Client.account;
using XenRP.Client.factions;
using XenRP.Client.jobs;
using XenRP.Client.model;
using XenRP.Client.vehicles;
using Entity = RAGE.Elements.Entity;
using Player = RAGE.Elements.Player;
using Type = RAGE.Elements.Type;

namespace XenRP.Client.globals {
    internal class Globals : Events.Script {
        public static bool viewingPlayers;
        public static bool playerLogged;
        private static Dictionary<int, AttachmentModel> playerAttachments;
        private DateTime lastTimeChecked;
        private string playerMoney;

        public Globals() {
            Events.Add("updatePlayerList", UpdatePlayerListEvent);
            Events.Add("hideConnectedPlayers", HideConnectedPlayersEvent);
            Events.Add("changePlayerWalkingStyle", ChangePlayerWalkingStyleEvent);
            Events.Add("resetPlayerWalkingStyle", ResetPlayerWalkingStyleEvent);
            Events.Add("attachItemToPlayer", AttachItemToPlayerEvent);
            Events.Add("dettachItemFromPlayer", DettachItemFromPlayerEvent);
            Events.Add("playerLoggedIn", PlayerLoggedInEvent);
            Events.AddDataHandler("SERVER_TIME", PlayerConnectionStateChanged);
            Events.OnEntityStreamIn += OnEntityStreamInEvent;
            Events.OnEntityStreamOut += OnEntityStreamOutEvent;
            Events.OnGuiReady += OnGuiReadyEvent;
            Events.Tick += TickEvent;

            playerAttachments = new Dictionary<int, AttachmentModel>();
        }

        public static string EscapeJsonCharacters(string jsonString) {
            // Escape the apostrophe on JSON
            return jsonString.Replace("'", "\\'");
        }

        private void UpdatePlayerListEvent(object[] args) {
            if (!playerLogged || !viewingPlayers || Browser.customBrowser == null) return;

            // Update the player list
            Browser.ExecuteFunctionEvent(new object[] {"updatePlayerList", args[0].ToString()});
        }

        private void HideConnectedPlayersEvent(object[] args) {
            // Cancel the player list view
            viewingPlayers = false;

            // Destroy the browser
            Browser.DestroyBrowserEvent(null);
        }

        private void ChangePlayerWalkingStyleEvent(object[] args) {
            // Get the player
            var player = (Player) args[0];
            var clipSet = args[1].ToString();

            player.SetMovementClipset(clipSet, 0.1f);
        }

        private void ResetPlayerWalkingStyleEvent(object[] args) {
            // Get the player
            var player = (Player) args[0];

            player.ResetMovementClipset(0.0f);
        }

        private void AttachItemToPlayerEvent(object[] args) {
            // Get the remote player
            var playerId = Convert.ToInt32(args[0]);
            var attachedPlayer = Entities.Players.GetAtRemote((ushort) playerId);

            // Check if the player is in the stream range
            if (Entities.Players.Streamed.Contains(attachedPlayer) || Player.LocalPlayer.Equals(attachedPlayer)) {
                // Get the attachment
                var attachment = JsonConvert.DeserializeObject<AttachmentModel>(args[1].ToString());

                // Create the object for that player
                var boneIndex = attachedPlayer.GetBoneIndexByName(attachment.bodyPart);
                attachment.attach = new MapObject(Convert.ToUInt32(attachment.hash), attachedPlayer.Position,
                    new Vector3(), 255, attachedPlayer.Dimension);
                RAGE.Game.Entity.AttachEntityToEntity(attachment.attach.Handle, attachedPlayer.Handle, boneIndex,
                    attachment.offset.X, attachment.offset.Y, attachment.offset.Z, attachment.rotation.X,
                    attachment.rotation.Y, attachment.rotation.Z, false, false, false, false, 2, true);

                // Add the attachment to the dictionary
                playerAttachments.Add(playerId, attachment);
            }
        }

        private void DettachItemFromPlayerEvent(object[] args) {
            // Get the remote player
            var playerId = Convert.ToInt32(args[0]);

            if (playerAttachments.ContainsKey(playerId)) {
                // Get the attachment
                var attachment = playerAttachments[playerId].attach;

                // Remove it from the player and world
                attachment.Destroy();
                playerAttachments.Remove(playerId);
            }
        }

        private void PlayerLoggedInEvent(object[] args) {
            // Show the player as logged
            playerLogged = true;
        }

        public static void OnEntityStreamInEvent(Entity entity) {
            if (entity.Type == Type.Player) {
                // Get the identifier of the player
                int playerId = entity.RemoteId;
                var attachedPlayer = Entities.Players.GetAtRemote((ushort) playerId);

                // Get the attachment on the right hand
                var attachmentJson = attachedPlayer.GetSharedData(Constants.ITEM_ENTITY_RIGHT_HAND);

                if (attachmentJson == null)
                    attachmentJson = attachedPlayer.GetSharedData(Constants.ITEM_ENTITY_WEAPON_CRATE);

                if (attachmentJson != null) {
                    var attachment = JsonConvert.DeserializeObject<AttachmentModel>(attachmentJson.ToString());

                    // If the attached item is a weapon, we don't stream it
                    if (Weapon.IsWeaponValid(Convert.ToUInt32(attachment.hash))) return;

                    var boneIndex = attachedPlayer.GetBoneIndexByName(attachment.bodyPart);
                    attachment.attach = new MapObject(Convert.ToUInt32(attachment.hash), attachedPlayer.Position,
                        new Vector3(), 255, attachedPlayer.Dimension);
                    RAGE.Game.Entity.AttachEntityToEntity(attachment.attach.Handle, attachedPlayer.Handle, boneIndex,
                        attachment.offset.X, attachment.offset.Y, attachment.offset.Z, attachment.rotation.X,
                        attachment.rotation.Y, attachment.rotation.Z, false, false, false, true, 0, true);

                    // Add the attachment to the dictionary
                    playerAttachments.Add(playerId, attachment);
                }
            }
        }

        public static void OnEntityStreamOutEvent(Entity entity) {
            if (entity.Type == Type.Player) {
                // Get the player's identifier
                int playerId = entity.RemoteId;

                if (playerAttachments.ContainsKey(playerId)) {
                    // Get the attached object
                    var attachment = playerAttachments[playerId].attach;

                    // Destroy the attachment
                    attachment.Destroy();
                    playerAttachments.Remove(playerId);
                }
            }
        }

        public static void OnGuiReadyEvent() {
            // Remove health regeneration
            RAGE.Game.Player.SetPlayerHealthRechargeMultiplier(0.0f);

            // Remove weapons from the vehicles
            RAGE.Game.Player.DisablePlayerVehicleRewards();

            // Remove the fade out after player's death
            Misc.SetFadeOutAfterDeath(false);

            // Freeze the player until he logs in
            Player.LocalPlayer.FreezePosition(true);
        }

        private void PlayerConnectionStateChanged(Entity entity, object arg) {
            if (entity == Player.LocalPlayer) {
                var serverTime = Player.LocalPlayer.GetSharedData("SERVER_TIME").ToString().Split(":");

                var hours = int.Parse(serverTime[0]);
                var minutes = int.Parse(serverTime[1]);
                var seconds = int.Parse(serverTime[2]);

                // Set the hour from the server
                Clock.SetClockTime(hours, minutes, seconds);

                // Get the current timestamp
                lastTimeChecked = DateTime.UtcNow;

                // Show the login window
                Login.AccountLoginFormEvent(null);
            }
        }

        private void TickEvent(List<Events.TickNametagData> nametags) {
            // Get the current time
            var dateTime = DateTime.UtcNow;

            // Check if the player is connected
            if (playerLogged) {
                // Disable reloading
                Pad.DisableControlAction(0, 140, true);

                if (Vehicles.lastPosition != null) {
                    if (Player.LocalPlayer.Vehicle == null)
                        Vehicles.RemoveSpeedometerEvent(null);
                    else
                        Vehicles.UpdateSpeedometer();
                }

                // Update the player's money each 450ms
                if (dateTime.Ticks - lastTimeChecked.Ticks >= 4500000) {
                    // Check if the player is loaded
                    var money = Player.LocalPlayer.GetSharedData(Constants.HAND_MONEY);

                    if (money != null) {
                        playerMoney = Convert.ToInt32(money) + "$";
                        lastTimeChecked = dateTime;
                    }
                }

                if (Fishing.fishingState > 0) Fishing.DrawFishingMinigame();

                // Draw the money
                UIResText.Draw(playerMoney, 1900, 60, Font.Pricedown, 0.5f, Color.DarkOliveGreen,
                    UIResText.Alignment.Right, true, true, 0);

                // Check if the player
                if (Pad.IsControlJustPressed(0, (int) Control.VehicleSubPitchDownOnly) &&
                    Player.LocalPlayer.Vehicle != null) Trucker.CheckPlayerStoredCrate();

                // Check if the player is handcuffed
                if (Police.handcuffed) {
                    Pad.DisableControlAction(0, 12, true);
                    Pad.DisableControlAction(0, 13, true);
                    Pad.DisableControlAction(0, 14, true);
                    Pad.DisableControlAction(0, 15, true);
                    Pad.DisableControlAction(0, 16, true);
                    Pad.DisableControlAction(0, 17, true);
                    Pad.DisableControlAction(0, 22, true);
                    Pad.DisableControlAction(0, 24, true);
                    Pad.DisableControlAction(0, 25, true);
                }
            }

            // Detect if a key has been pressed
            var key = Keys.DetectPressedKey(dateTime.Ticks);

            if (key >= 0) Keys.FireKeyPressed(key);
        }
    }
}