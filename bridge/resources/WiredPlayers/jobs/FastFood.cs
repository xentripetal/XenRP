using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GTANetworkAPI;
using WiredPlayers.globals;
using WiredPlayers.messages.error;
using WiredPlayers.messages.information;
using WiredPlayers.model;

namespace WiredPlayers.jobs {
    public class FastFood : Script {
        private static Dictionary<int, Timer> fastFoodTimerList;

        public FastFood() {
            // Initialize the class data
            fastFoodTimerList = new Dictionary<int, Timer>();
        }

        public static void OnPlayerDisconnected(Client player, DisconnectionType type, string reason) {
            if (fastFoodTimerList.TryGetValue(player.Value, out var fastFoodTimer)) {
                // Destroy the timer
                fastFoodTimer.Dispose();
                fastFoodTimerList.Remove(player.Value);
            }
        }

        public static void CheckFastfoodOrders(Client player) {
            // Get the deliverable orders
            var fastFoodOrders = Globals.fastFoodOrderList.Where(o => !o.taken).ToList();

            if (fastFoodOrders.Count == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.order_none);
                return;
            }

            var distancesList = new List<float>();

            foreach (var order in fastFoodOrders) {
                var distance = player.Position.DistanceTo(order.position);
                distancesList.Add(distance);
            }

            player.TriggerEvent("showFastfoodOrders", NAPI.Util.ToJson(fastFoodOrders),
                NAPI.Util.ToJson(distancesList));
        }

        private int GetFastFoodOrderAmount(Client player) {
            var amount = 0;
            int orderId = player.GetData(EntityData.PLAYER_DELIVER_ORDER);
            foreach (var order in Globals.fastFoodOrderList)
                if (order.id == orderId) {
                    amount += order.pizzas * Constants.PRICE_PIZZA;
                    amount += order.hamburgers * Constants.PRICE_HAMBURGER;
                    amount += order.sandwitches * Constants.PRICE_SANDWICH;
                    break;
                }

            return amount;
        }

        private FastfoodOrderModel GetFastfoodOrderFromId(int orderId) {
            // Get the fastfood order from the specified identifier
            return Globals.fastFoodOrderList.Where(orderModel => orderModel.id == orderId).FirstOrDefault();
        }

        private void RespawnFastfoodVehicle(Vehicle vehicle) {
            vehicle.Repair();
            vehicle.Position = vehicle.GetData(EntityData.VEHICLE_POSITION);
            vehicle.Rotation = vehicle.GetData(EntityData.VEHICLE_ROTATION);
        }

        private void OnFastFoodTimer(object playerObject) {
            var player = (Client) playerObject;
            Vehicle vehicle = player.GetData(EntityData.PLAYER_JOB_VEHICLE);

            // Vehicle respawn
            RespawnFastfoodVehicle(vehicle);

            // Cancel the order
            player.ResetData(EntityData.PLAYER_DELIVER_ORDER);
            player.ResetData(EntityData.PLAYER_JOB_CHECKPOINT);
            player.ResetData(EntityData.PLAYER_JOB_VEHICLE);
            player.ResetData(EntityData.PLAYER_JOB_WON);

            // Delete map blip
            player.TriggerEvent("fastFoodDeliverFinished");

            // Remove timer from the list
            var fastFoodTimer = fastFoodTimerList[player.Value];
            if (fastFoodTimer != null) {
                fastFoodTimer.Dispose();
                fastFoodTimerList.Remove(player.Value);
            }

            // Send the message to the player
            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.job_vehicle_abandoned);
        }

        [ServerEvent(Event.PlayerEnterVehicle)]
        public void OnPlayerEnterVehicle(Client player, Vehicle vehicle, sbyte seat) {
            if (vehicle.GetData(EntityData.VEHICLE_FACTION) ==
                Constants.JOB_FASTFOOD + Constants.MAX_FACTION_VEHICLES) {
                if (player.GetData(EntityData.PLAYER_DELIVER_ORDER) == null &&
                    player.GetData(EntityData.PLAYER_JOB_VEHICLE) == null) {
                    // Stop the vehicle's speedometer
                    player.TriggerEvent("removeSpeedometer");

                    player.WarpOutOfVehicle();
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_delivering_order);
                }
                else if (player.GetData(EntityData.PLAYER_JOB_VEHICLE) != null &&
                         player.GetData(EntityData.PLAYER_JOB_VEHICLE) != vehicle) {
                    // Stop the vehicle's speedometer
                    player.TriggerEvent("removeSpeedometer");

                    player.WarpOutOfVehicle();
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_your_job_vehicle);
                }
                else {
                    if (fastFoodTimerList.TryGetValue(player.Value, out var fastFoodTimer)) {
                        fastFoodTimer.Dispose();
                        fastFoodTimerList.Remove(player.Value);
                    }

                    if (player.GetData(EntityData.PLAYER_JOB_VEHICLE) == null) {
                        int orderId = player.GetData(EntityData.PLAYER_DELIVER_ORDER);
                        var order = GetFastfoodOrderFromId(orderId);
                        var playerFastFoodCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4, order.position,
                            new Vector3(0.0f, 0.0f, 0.0f), 2.5f, new Color(198, 40, 40, 200));

                        player.SetData(EntityData.PLAYER_JOB_CHECKPOINT, playerFastFoodCheckpoint);
                        player.SetData(EntityData.PLAYER_JOB_VEHICLE, vehicle);

                        player.TriggerEvent("fastFoodDestinationCheckPoint", order.position);
                    }
                }
            }
        }

        [ServerEvent(Event.PlayerExitVehicle)]
        public void OnPlayerExitVehicle(Client player, Vehicle vehicle) {
            if (vehicle.GetData(EntityData.VEHICLE_FACTION) ==
                Constants.JOB_FASTFOOD + Constants.MAX_FACTION_VEHICLES &&
                player.GetData(EntityData.PLAYER_JOB_VEHICLE) != null)
                if (player.GetData(EntityData.PLAYER_JOB_VEHICLE) == vehicle) {
                    var warn = string.Format(InfoRes.job_vehicle_left, 60);
                    player.SendChatMessage(Constants.COLOR_INFO + warn);

                    // Timer with the time left to get into the vehicle
                    var fastFoodTimer = new Timer(OnFastFoodTimer, player, 60000, Timeout.Infinite);
                    fastFoodTimerList.Add(player.Value, fastFoodTimer);
                }
        }

        [ServerEvent(Event.PlayerEnterCheckpoint)]
        public void OnPlayerEnterCheckpoint(Checkpoint checkpoint, Client player) {
            if (player.GetData(EntityData.PLAYER_JOB) == Constants.JOB_FASTFOOD) {
                // Get the player's deliver checkpoint
                Checkpoint playerDeliverColShape = player.GetData(EntityData.PLAYER_JOB_CHECKPOINT);

                if (playerDeliverColShape == checkpoint) {
                    if (player.GetData(EntityData.PLAYER_DELIVER_START) != null) {
                        if (!player.IsInVehicle) {
                            Vehicle vehicle = player.GetData(EntityData.PLAYER_JOB_VEHICLE);
                            playerDeliverColShape.Position = vehicle.GetData(EntityData.VEHICLE_POSITION);

                            int elapsed = Globals.GetTotalSeconds() - player.GetData(EntityData.PLAYER_DELIVER_START);
                            var extra = (int) Math.Round((player.GetData(EntityData.PLAYER_DELIVER_TIME) - elapsed) /
                                                         2.0f);
                            var amount = GetFastFoodOrderAmount(player) + extra;

                            player.ResetData(EntityData.PLAYER_DELIVER_START);
                            player.SetData(EntityData.PLAYER_JOB_WON, amount > 0 ? amount : 25);

                            player.TriggerEvent("fastFoodDeliverBack", playerDeliverColShape.Position);

                            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.deliver_completed);
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.deliver_in_vehicle);
                        }
                    }
                    else {
                        Vehicle vehicle = player.GetData(EntityData.PLAYER_JOB_VEHICLE);
                        if (player.Vehicle == vehicle && player.VehicleSeat == (int) VehicleSeat.Driver) {
                            int won = player.GetData(EntityData.PLAYER_JOB_WON);
                            int money = player.GetSharedData(EntityData.PLAYER_MONEY);
                            int orderId = player.GetData(EntityData.PLAYER_DELIVER_ORDER);
                            var message = string.Format(InfoRes.job_won, won);
                            Globals.fastFoodOrderList.RemoveAll(order => order.id == orderId);

                            // Stop the vehicle's speedometer
                            player.TriggerEvent("removeSpeedometer");

                            playerDeliverColShape.Delete();
                            player.WarpOutOfVehicle();

                            player.SetSharedData(EntityData.PLAYER_MONEY, money + won);
                            player.SendChatMessage(Constants.COLOR_INFO + message);

                            player.ResetData(EntityData.PLAYER_DELIVER_ORDER);
                            player.ResetData(EntityData.PLAYER_JOB_CHECKPOINT);
                            player.ResetData(EntityData.PLAYER_JOB_VEHICLE);
                            player.ResetData(EntityData.PLAYER_JOB_WON);

                            player.TriggerEvent("fastFoodDeliverFinished");

                            // We get the motorcycle to its spawn point
                            RespawnFastfoodVehicle(vehicle);
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_your_job_vehicle);
                        }
                    }
                }
            }
        }

        [RemoteEvent("takeFastFoodOrder")]
        public void TakeFastFoodOrderEvent(Client player, int orderId) {
            foreach (var order in Globals.fastFoodOrderList)
                if (order.id == orderId) {
                    if (order.taken) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.order_taken);
                    }
                    else {
                        // Get the time to reach the destination
                        var start = Globals.GetTotalSeconds();
                        var time = (int) Math.Round(player.Position.DistanceTo(order.position) / 9.5f);

                        // We take the order
                        order.taken = true;

                        player.SetData(EntityData.PLAYER_DELIVER_ORDER, orderId);
                        player.SetData(EntityData.PLAYER_DELIVER_START, start);
                        player.SetData(EntityData.PLAYER_DELIVER_TIME, time);

                        // Information message sent to the player
                        var orderMessage = string.Format(InfoRes.deliver_order, time);
                        player.SendChatMessage(Constants.COLOR_INFO + orderMessage);
                    }

                    return;
                }

            // Order has been deleted
            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.order_timeout);
        }
    }
}