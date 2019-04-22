﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.database;
using XenRP.globals;
using XenRP.messages.error;
using XenRP.messages.information;
using XenRP.model;

namespace XenRP.jobs {
    public class Fishing : Script {
        private static Dictionary<int, Timer> fishingTimerList;

        public Fishing() {
            // Initialize the variables
            fishingTimerList = new Dictionary<int, Timer>();
        }

        [ServerEvent(Event.PlayerDisconnected)]
        public static void OnPlayerDisconnected(Client player, DisconnectionType type, string reason) {
            if (fishingTimerList.TryGetValue(player.Value, out var fishingTimer)) {
                // Remove the timer
                fishingTimer.Dispose();
                fishingTimerList.Remove(player.Value);
            }
        }

        private void OnFishingPrewarnTimer(object playerObject) {
            var player = (Client) playerObject;

            if (fishingTimerList.TryGetValue(player.Value, out var fishingTimer)) {
                // Remove the timer
                fishingTimer.Dispose();
                fishingTimerList.Remove(player.Value);
            }

            // Start the minigame
            player.TriggerEvent("fishingBaitTaken");

            // Send the message and play fishing animation
            player.PlayAnimation("amb@world_human_stand_fishing@idle_a", "idle_c", (int) Constants.AnimationFlags.Loop);
            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.something_baited);
        }

        private int GetPlayerFishingLevel(Client player) {
            // Get player points
            var fishingPoints = Job.GetJobPoints(player, Constants.JOB_FISHERMAN);

            // Calculamos el nivel
            if (fishingPoints > 600) return 5;
            if (fishingPoints > 300) return 4;
            if (fishingPoints > 150) return 3;
            if (fishingPoints > 50) return 2;

            return 1;
        }

        [RemoteEvent("startFishingTimer")]
        public void StartFishingTimerEvent(Client player) {
            var random = new Random();

            // Timer for the game to start
            var fishingTimer = new Timer(OnFishingPrewarnTimer, player, random.Next(1250, 2500), Timeout.Infinite);
            fishingTimerList.Add(player.Value, fishingTimer);

            // Confirmation message
            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_fishing_rod_thrown);
        }

        [RemoteEvent("fishingCanceled")]
        public void FishingCanceledEvent(Client player) {
            if (fishingTimerList.TryGetValue(player.Value, out var fishingTimer)) {
                fishingTimer.Dispose();
                fishingTimerList.Remove(player.Value);
            }

            // Cancel the fishing
            player.StopAnimation();
            player.ResetData(EntityData.PLAYER_FISHING);

            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.fishing_canceled);
        }

        [RemoteEvent("fishingSuccess")]
        public void FishingSuccessEvent(Client player) {
            // Calculate failure chance
            var failed = false;
            var random = new Random();
            var successChance = random.Next(100);

            // Getting player's level
            var fishingLevel = GetPlayerFishingLevel(player);

            switch (fishingLevel) {
                case 1:
                    failed = successChance >= 70;
                    break;
                case 2:
                    failed = successChance >= 80;
                    break;
                default:
                    failed = successChance >= 90;
                    fishingLevel = 3;
                    break;
            }

            if (!failed) {
                // Get player earnings
                var fishWeight = random.Next(fishingLevel * 100, fishingLevel * 750);
                int playerDatabaseId = player.GetData(EntityData.PLAYER_SQL_ID);
                var fishItem = Globals.GetPlayerItemModelFromHash(playerDatabaseId, Constants.ITEM_HASH_FISH);

                if (fishItem == null) {
                    fishItem = new ItemModel();
                    {
                        fishItem.amount = fishWeight;
                        fishItem.hash = Constants.ITEM_HASH_FISH;
                        fishItem.ownerEntity = Constants.ITEM_ENTITY_PLAYER;
                        fishItem.ownerIdentifier = playerDatabaseId;
                        fishItem.position = new Vector3(0.0f, 0.0f, 0.0f);
                        fishItem.dimension = 0;
                    }

                    Task.Factory.StartNew(() => {
                        // Add the fish item to database
                        fishItem.id = Database.AddNewItem(fishItem);
                        Globals.itemList.Add(fishItem);
                    });
                }
                else {
                    Task.Factory.StartNew(() => {
                        // Update the inventory
                        fishItem.amount += fishWeight;
                        Database.UpdateItem(fishItem);
                    });
                }

                // Send the message to the player
                var message = string.Format(InfoRes.fished_weight, fishWeight);
                player.SendChatMessage(Constants.COLOR_INFO + message);
            }
            else {
                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.garbage_fished);
            }

            // Add one skill point to the player
            var fishingPoints = Job.GetJobPoints(player, Constants.JOB_FISHERMAN);
            Job.SetJobPoints(player, Constants.JOB_FISHERMAN, fishingPoints + 1);

            // Cancel fishing
            player.StopAnimation();
            player.ResetData(EntityData.PLAYER_FISHING);
        }

        [RemoteEvent("fishingFailed")]
        public void FishingFailedEvent(Client player) {
            // Cancel fishing
            player.StopAnimation();
            player.ResetData(EntityData.PLAYER_FISHING);

            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.fishing_failed);
        }

        [Command(Commands.COM_FISH)]
        public void FishCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_FISHING) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_already_fishing);
            }
            else if (player.Vehicle != null && player.VehicleSeat == (int) VehicleSeat.Driver) {
                var vehicleModel = (VehicleHash) player.Vehicle.Model;

                if (vehicleModel == VehicleHash.Marquis || vehicleModel == VehicleHash.Tug) {
                    if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_FISHERMAN) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_fisherman);
                    }
                    else if (player.GetData(EntityData.PLAYER_FISHABLE) == null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_fishing_zone);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_fishing_boat);
                }
            }
            else if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
                var fishingRodId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;
                var fishingRod = Globals.GetItemModelFromId(fishingRodId);

                if (fishingRod != null && fishingRod.hash == Constants.ITEM_HASH_FISHING_ROD) {
                    int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                    var bait = Globals.GetPlayerItemModelFromHash(playerId, Constants.ITEM_HASH_BAIT);
                    if (bait != null && bait.amount > 0) {
                        foreach (var fishingPosition in Constants.FISHING_POSITION_LIST) {
                            // Check if the player is close to the area
                            if (player.Position.DistanceTo(fishingPosition) > 2.5f) continue;

                            // Set player looking to the sea
                            player.Rotation = new Vector3(0.0f, 0.0f, 52.532f);

                            if (bait.amount == 1) {
                                Task.Factory.StartNew(() => {
                                    // Remove the baits from the inventory
                                    Globals.itemList.Remove(bait);
                                    Database.RemoveItem(bait.id);
                                });
                            }
                            else {
                                bait.amount--;

                                Task.Factory.StartNew(() => {
                                    // Update the amount
                                    Database.UpdateItem(bait);
                                });
                            }

                            // Start fishing minigame
                            player.SetData(EntityData.PLAYER_FISHING, true);
                            player.PlayAnimation("amb@world_human_stand_fishing@base", "base",
                                (int) Constants.AnimationFlags.Loop);
                            player.TriggerEvent("startPlayerFishing");
                            return;
                        }

                        // Player's not in the fishing zone
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_fishing_zone);
                    }
                    else {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_no_baits);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_fishing_rod);
                }
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_rod_boat);
            }
        }
    }
}