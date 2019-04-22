using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.business;
using XenRP.database;
using XenRP.factions;
using XenRP.globals;
using XenRP.house;
using XenRP.messages.error;
using XenRP.messages.general;
using XenRP.messages.information;
using XenRP.messages.success;
using XenRP.model;
using XenRP.vehicles;

namespace XenRP.jobs {
    public class Thief : Script {
        private static Dictionary<int, Timer> robberyTimerList;

        public Thief() {
            foreach (var pawnShop in Constants.PAWN_SHOP)
                // Create pawn shops
                NAPI.TextLabel.CreateTextLabel(GenRes.pawn_shop, pawnShop, 10.0f, 0.5f, 4, new Color(255, 255, 255),
                    false, 0);

            // Initialize the variables
            robberyTimerList = new Dictionary<int, Timer>();
        }

        [ServerEvent(Event.PlayerDisconnected)]
        public static void OnPlayerDisconnected(Client player) {
            if (robberyTimerList.TryGetValue(player.Value, out var robberyTimer)) {
                robberyTimer.Dispose();
                robberyTimerList.Remove(player.Value);
            }
        }

        private void OnLockpickTimer(object playerObject) {
            var player = (Client) playerObject;

            Vehicle vehicle = player.GetData(EntityData.PLAYER_LOCKPICKING);
            vehicle.Locked = false;

            player.StopAnimation();
            player.ResetData(EntityData.PLAYER_LOCKPICKING);
            player.ResetData(EntityData.PLAYER_ANIMATION);

            if (robberyTimerList.TryGetValue(player.Value, out var robberyTimer)) {
                robberyTimer.Dispose();
                robberyTimerList.Remove(player.Value);
            }

            player.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.lockpicked);
        }

        private void OnHotwireTimer(object playerObject) {
            var player = (Client) playerObject;

            Vehicle vehicle = player.GetData(EntityData.PLAYER_HOTWIRING);
            vehicle.EngineStatus = true;

            player.StopAnimation();
            player.ResetData(EntityData.PLAYER_HOTWIRING);
            player.ResetData(EntityData.PLAYER_ANIMATION);

            if (robberyTimerList.TryGetValue(player.Value, out var robberyTimer)) {
                robberyTimer.Dispose();
                robberyTimerList.Remove(player.Value);
            }

            // Get all the members from any police faction
            var members = NAPI.Pools.GetAllPlayers()
                .Where(m => Faction.IsPoliceMember(m) && m.GetData(EntityData.PLAYER_ON_DUTY) == 1).ToList();

            foreach (var target in members) {
                target.SendChatMessage(Constants.COLOR_INFO + InfoRes.police_warning);
                target.SetData(EntityData.PLAYER_EMERGENCY_WITH_WARN, player.Position);
            }

            player.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.veh_hotwireed);
        }

        private void OnPlayerRob(object playerObject) {
            var player = (Client) playerObject;
            int playerSqlId = player.GetData(EntityData.PLAYER_SQL_ID);
            int timeElapsed = Scheduler.GetTotalSeconds() - player.GetData(EntityData.PLAYER_ROBBERY_START);
            var stolenItemsDecimal = timeElapsed / Constants.ITEMS_ROBBED_PER_TIME;
            var totalStolenItems = (int) Math.Round(stolenItemsDecimal);

            // Check if the player has stolen items
            var stolenItemModel = Globals.GetPlayerItemModelFromHash(playerSqlId, Constants.ITEM_HASH_STOLEN_OBJECTS);

            if (stolenItemModel == null) {
                stolenItemModel = new ItemModel();
                {
                    stolenItemModel.amount = totalStolenItems;
                    stolenItemModel.hash = Constants.ITEM_HASH_STOLEN_OBJECTS;
                    stolenItemModel.ownerEntity = Constants.ITEM_ENTITY_PLAYER;
                    stolenItemModel.ownerIdentifier = playerSqlId;
                    stolenItemModel.dimension = 0;
                    stolenItemModel.position = new Vector3(0.0f, 0.0f, 0.0f);
                }

                Task.Factory.StartNew(() => {
                    stolenItemModel.id = Database.AddNewItem(stolenItemModel);
                    Globals.itemList.Add(stolenItemModel);
                });
            }
            else {
                stolenItemModel.amount += totalStolenItems;

                Task.Factory.StartNew(() => {
                    // Update the amount into the database
                    Database.UpdateItem(stolenItemModel);
                });
            }

            // Allow player movement
            player.Freeze(false);
            player.StopAnimation();
            player.ResetData(EntityData.PLAYER_ANIMATION);
            player.ResetData(EntityData.PLAYER_ROBBERY_START);

            if (robberyTimerList.TryGetValue(player.Value, out var robberyTimer)) {
                robberyTimer.Dispose();
                robberyTimerList.Remove(player.Value);
            }

            // Avisamos de los objetos robados
            var message = string.Format(InfoRes.player_robbed, totalStolenItems);
            player.SendChatMessage(Constants.COLOR_INFO + message);

            // Check if the player commited the maximum thefts allowed
            int totalThefts = player.GetData(EntityData.PLAYER_JOB_DELIVER);
            if (Constants.MAX_THEFTS_IN_ROW == totalThefts) {
                // Apply a cooldown to the player
                player.SetData(EntityData.PLAYER_JOB_DELIVER, 0);
                player.SetData(EntityData.PLAYER_JOB_COOLDOWN, 60);
                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_rob_pressure);
            }
            else {
                player.SetData(EntityData.PLAYER_JOB_DELIVER, totalThefts + 1);
            }
        }

        private void GeneratePoliceRobberyWarning(Client player) {
            Vector3 robberyPosition;
            var robberyPlace = string.Empty;
            var robberyHour = DateTime.Now.ToString("h:mm:ss tt");

            // Check if he robbed into a house or business
            if (player.GetData(EntityData.PLAYER_HOUSE_ENTERED) > 0) {
                int houseId = player.GetData(EntityData.PLAYER_HOUSE_ENTERED);
                var house = House.GetHouseById(houseId);
                robberyPosition = house.position;
                robberyPlace = house.name;
            }
            else if (player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) > 0) {
                int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
                var business = Business.GetBusinessById(businessId);
                robberyPosition = business.position;
                robberyPlace = business.name;
            }
            else {
                robberyPosition = player.Position;
            }

            // Create the police report
            var policeWarning = new FactionWarningModel(Constants.FACTION_POLICE, player.Value, robberyPlace,
                robberyPosition, -1, robberyHour);
            var sheriffWarning = new FactionWarningModel(Constants.FACTION_SHERIFF, player.Value, robberyPlace,
                robberyPosition, -1, robberyHour);
            Faction.factionWarningList.Add(policeWarning);
            Faction.factionWarningList.Add(sheriffWarning);

            var warnMessage = string.Format(InfoRes.emergency_warning, Faction.factionWarningList.Count - 1);

            // Get all the members from any police faction
            var members = NAPI.Pools.GetAllPlayers()
                .Where(m => Faction.IsPoliceMember(m) && m.GetData(EntityData.PLAYER_ON_DUTY) == 1).ToList();

            foreach (var target in members)
                // Send the warning
                target.SendChatMessage(Constants.COLOR_INFO + warnMessage);
        }

        [ServerEvent(Event.PlayerExitVehicle)]
        public void OnPlayerExitVehicle(Client player, Vehicle vehicle) {
            if (player.GetData(EntityData.PLAYER_JOB) == Constants.JOB_THIEF) {
                if (player.GetData(EntityData.PLAYER_HOTWIRING) != null) {
                    // Remove player's hotwire
                    player.ResetData(EntityData.PLAYER_HOTWIRING);
                    player.StopAnimation();

                    if (robberyTimerList.TryGetValue(player.Value, out var robberyTimer)) {
                        robberyTimer.Dispose();
                        robberyTimerList.Remove(player.Value);
                    }

                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_stopped_hotwire);
                }
                else if (player.GetData(EntityData.PLAYER_ROBBERY_START) != null) {
                    OnPlayerRob(player);
                }
            }
        }

        [Command(Commands.COM_FORCE)]
        public void ForceCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_THIEF) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_thief);
                }
                else if (player.GetData(EntityData.PLAYER_LOCKPICKING) != null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.already_lockpicking);
                }
                else {
                    var vehicle = Vehicles.GetClosestVehicle(player);
                    if (vehicle == null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_vehicles_near);
                    }
                    else if (Vehicles.HasPlayerVehicleKeys(player, vehicle)) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_cant_lockpick_own_vehicle);
                    }
                    else if (!vehicle.Locked) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.veh_already_unlocked);
                    }
                    else {
                        // Generate police report
                        GeneratePoliceRobberyWarning(player);

                        player.SetData(EntityData.PLAYER_LOCKPICKING, vehicle);
                        player.PlayAnimation("missheistfbisetup1", "hassle_intro_loop_f",
                            (int) Constants.AnimationFlags.Loop);
                        player.SetData(EntityData.PLAYER_ANIMATION, true);

                        // Timer to finish forcing the door
                        var robberyTimer = new Timer(OnLockpickTimer, player, 10000, Timeout.Infinite);
                        robberyTimerList.Add(player.Value, robberyTimer);
                    }
                }
            }
        }

        [Command(Commands.COM_STEAL)]
        public void StealCommand(Client player) {
            if (player.Position.DistanceTo(new Vector3(-286.7586f, -849.3693f, 31.74337f)) > 1150.0f) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_thief_area);
            }
            else if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_THIEF) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_thief);
            }
            else if (player.GetData(EntityData.PLAYER_ROBBERY_START) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_already_stealing);
            }
            else if (player.GetData(EntityData.PLAYER_JOB_COOLDOWN) > 0) {
                int timeLeft = player.GetData(EntityData.PLAYER_JOB_COOLDOWN) - Scheduler.GetTotalSeconds();
                var message = string.Format(ErrRes.player_cooldown_thief, timeLeft);
                player.SendChatMessage(Constants.COLOR_ERROR + message);
            }
            else {
                if (player.GetData(EntityData.PLAYER_HOUSE_ENTERED) > 0 ||
                    player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) > 0) {
                    int houseId = player.GetData(EntityData.PLAYER_HOUSE_ENTERED);
                    var house = House.GetHouseById(houseId);
                    if (house != null && House.HasPlayerHouseKeys(player, house)) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_cant_rob_own_house);
                    }
                    else {
                        // Generate the police report
                        GeneratePoliceRobberyWarning(player);

                        // Start stealing items
                        player.PlayAnimation("misscarstealfinalecar_5_ig_3", "crouchloop",
                            (int) Constants.AnimationFlags.Loop);
                        player.SetData(EntityData.PLAYER_ROBBERY_START, Scheduler.GetTotalSeconds());
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.searching_value_items);
                        player.SetData(EntityData.PLAYER_ANIMATION, true);
                        player.Freeze(true);

                        // Timer to finish the robbery
                        var robberyTimer = new Timer(OnPlayerRob, player, 20000, Timeout.Infinite);
                        robberyTimerList.Add(player.Value, robberyTimer);
                    }
                }
                else if (player.VehicleSeat == (int) VehicleSeat.Driver) {
                    var vehicle = player.Vehicle;
                    if (Vehicles.HasPlayerVehicleKeys(player, vehicle)) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_cant_rob_own_vehicle);
                    }
                    else if (vehicle.EngineStatus) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.engine_on);
                    }
                    else {
                        // Generate the police report
                        GeneratePoliceRobberyWarning(player);

                        // Start stealing items
                        player.PlayAnimation("veh@plane@cuban@front@ds@base", "hotwire",
                            (int) (Constants.AnimationFlags.Loop | Constants.AnimationFlags.AllowPlayerControl));
                        player.SetData(EntityData.PLAYER_ROBBERY_START, Scheduler.GetTotalSeconds());
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.searching_value_items);
                        player.SetData(EntityData.PLAYER_ANIMATION, true);

                        // Timer to finish the robbery
                        var robberyTimer = new Timer(OnPlayerRob, player, 35000, Timeout.Infinite);
                        robberyTimerList.Add(player.Value, robberyTimer);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_cant_rob);
                }
            }
        }

        [Command(Commands.COM_HOTWIRE)]
        public void HotwireCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_THIEF) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_thief);
            }
            else if (player.GetData(EntityData.PLAYER_HOTWIRING) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_already_hotwiring);
            }
            else if (!player.IsInVehicle) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle);
            }
            else {
                var vehicle = player.Vehicle;
                if (player.VehicleSeat != (int) VehicleSeat.Driver) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_vehicle_driving);
                }
                else if (Vehicles.HasPlayerVehicleKeys(player, vehicle)) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_cant_hotwire_own_vehicle);
                }
                else if (vehicle.EngineStatus) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.engine_already_started);
                }
                else {
                    int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
                    var position = vehicle.Position;

                    player.SetData(EntityData.PLAYER_HOTWIRING, vehicle);
                    player.SetData(EntityData.PLAYER_ANIMATION, true);
                    player.PlayAnimation("veh@plane@cuban@front@ds@base", "hotwire",
                        (int) (Constants.AnimationFlags.Loop | Constants.AnimationFlags.AllowPlayerControl));

                    // Create timer to finish the hotwire
                    var robberyTimer = new Timer(OnHotwireTimer, player, 15000, Timeout.Infinite);
                    robberyTimerList.Add(player.Value, robberyTimer);

                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.hotwire_started);

                    Task.Factory.StartNew(() => {
                        // Add hotwire log to the database
                        Database.LogHotwire(player.Name, vehicleId, position);
                    });
                }
            }
        }

        [Command(Commands.COM_PAWN)]
        public void PawnCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_THIEF) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_thief);
            }
            else {
                foreach (var pawnShop in Constants.PAWN_SHOP)
                    if (player.Position.DistanceTo(pawnShop) < 1.5f) {
                        int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                        var stolenItems =
                            Globals.GetPlayerItemModelFromHash(playerId, Constants.ITEM_HASH_STOLEN_OBJECTS);
                        if (stolenItems != null) {
                            // Calculate the earnings
                            var wonAmount = stolenItems.amount * Constants.PRICE_STOLEN;
                            var message = string.Format(InfoRes.player_pawned_items, wonAmount);
                            int money = player.GetSharedData(EntityData.PLAYER_MONEY) + wonAmount;

                            Task.Factory.StartNew(() => {
                                // Delete stolen items
                                Database.RemoveItem(stolenItems.id);
                                Globals.itemList.Remove(stolenItems);
                            });

                            player.SetSharedData(EntityData.PLAYER_MONEY, money);
                            player.SendChatMessage(Constants.COLOR_INFO + message);
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_stolen_items);
                        }

                        return;
                    }

                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_pawn_show);
            }
        }
    }
}