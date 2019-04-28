﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.business;
using XenRP.database;
using XenRP.globals;
using XenRP.messages.error;
using XenRP.messages.information;
using XenRP.model;

namespace XenRP.vehicles {
    public class Mechanic : Script {
        public static List<TunningModel> tunningList;

        [ServerEvent(Event.ResourceStart)]
        public void LoadRuningList() {
            tunningList = DBMechanicCommands.LoadAllTunning();
        }

        public static void AddTunningToVehicle(Vehicle vehicle) {
            foreach (var tunning in tunningList)
                if (vehicle.GetData(EntityData.VEHICLE_ID) == tunning.vehicle)
                    vehicle.SetMod(tunning.slot, tunning.component);
        }

        public static bool PlayerInValidRepairPlace(Client player) {
            // Check if the player is in any workshop
            foreach (var business in Business.businessList)
                if (business.type == Constants.BUSINESS_TYPE_MECHANIC &&
                    player.Position.DistanceTo(business.position) < 25.0f)
                    return true;

            // Check if the player has a towtruck near
            foreach (var vehicle in NAPI.Pools.GetAllVehicles()) {
                var vehicleHash = (VehicleHash) vehicle.Model;
                if (vehicleHash == VehicleHash.TowTruck || vehicleHash == VehicleHash.TowTruck2) return true;
            }

            return false;
        }

        private int GetVehicleTunningComponent(int vehicleId, int slot) {
            // Get the component on the specified slot
            var tunning = tunningList
                .Where(tunningModel => tunningModel.vehicle == vehicleId && tunningModel.slot == slot).FirstOrDefault();

            return tunning == null ? 255 : tunning.component;
        }

        [RemoteEvent("repaintVehicle")]
        public void RepaintVehicleEvent(Client player, int colorType, string firstColor, string secondColor,
            int pearlescentColor, int vehiclePaid) {
            // Get player's vehicle
            Vehicle vehicle = player.GetData(EntityData.PLAYER_VEHICLE);

            switch (colorType) {
                case 0:
                    // Predefined color
                    vehicle.PrimaryColor = int.Parse(firstColor);
                    vehicle.SecondaryColor = int.Parse(secondColor);

                    if (pearlescentColor >= 0) vehicle.PearlescentColor = pearlescentColor;
                    break;
                case 1:
                    // Custom color
                    var firstColorArray = firstColor.Split(',');
                    var secondColorArray = secondColor.Split(',');
                    vehicle.CustomPrimaryColor = new Color(int.Parse(firstColorArray[0]), int.Parse(firstColorArray[1]),
                        int.Parse(firstColorArray[2]));
                    vehicle.CustomSecondaryColor = new Color(int.Parse(secondColorArray[0]),
                        int.Parse(secondColorArray[1]), int.Parse(secondColorArray[2]));
                    break;
            }

            if (vehiclePaid > 0) {
                // Check for the product amount
                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                var item = Globals.GetPlayerItemModelFromHash(playerId, Constants.ITEM_HASH_BUSINESS_PRODUCTS);

                if (item != null && item.amount >= 250) {
                    int vehicleFaction = vehicle.GetData(EntityData.VEHICLE_FACTION);

                    // Search for a player with vehicle keys
                    foreach (var target in NAPI.Pools.GetAllPlayers())
                        if (Vehicles.HasPlayerVehicleKeys(target, vehicle) || vehicleFaction > 0 &&
                            target.GetData(EntityData.PLAYER_FACTION) == vehicleFaction)
                            if (target.Position.DistanceTo(player.Position) < 4.0f) {
                                // Vehicle repaint data
                                target.SetData(EntityData.PLAYER_JOB_PARTNER, player);
                                target.SetData(EntityData.PLAYER_REPAINT_VEHICLE, vehicle);
                                target.SetData(EntityData.PLAYER_REPAINT_COLOR_TYPE, colorType);
                                target.SetData(EntityData.PLAYER_REPAINT_FIRST_COLOR, firstColor);
                                target.SetData(EntityData.PLAYER_REPAINT_SECOND_COLOR, secondColor);
                                target.SetData(EntityData.PLAYER_REPAINT_PEARLESCENT, pearlescentColor);
                                target.SetData(EntityData.JOB_OFFER_PRICE, vehiclePaid);
                                target.SetData(EntityData.JOB_OFFER_PRODUCTS, 250);

                                var playerMessage = string.Format(InfoRes.mechanic_repaint_offer, target.Name,
                                    vehiclePaid);
                                var targetMessage = string.Format(InfoRes.mechanic_repaint_accept, player.Name,
                                    vehiclePaid);
                                player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                                return;
                            }

                    // There's no player with vehicle's keys near
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                }
                else {
                    var message = string.Format(ErrRes.not_required_products, 250);
                    player.SendChatMessage(Constants.COLOR_ERROR + message);
                }
            }
        }

        [RemoteEvent("cancelVehicleRepaint")]
        public void CancelVehicleRepaintEvent(Client player) {
            // Get player's vehicle
            Vehicle vehicle = player.GetData(EntityData.PLAYER_VEHICLE);

            // Get previous colors
            int vehicleColorType = vehicle.GetData(EntityData.VEHICLE_COLOR_TYPE);
            string primaryVehicleColor = vehicle.GetData(EntityData.VEHICLE_FIRST_COLOR);
            string secondaryVehicleColor = vehicle.GetData(EntityData.VEHICLE_SECOND_COLOR);
            int vehiclePearlescentColor = vehicle.GetData(EntityData.VEHICLE_PEARLESCENT_COLOR);

            if (vehicleColorType == Constants.VEHICLE_COLOR_TYPE_PREDEFINED) {
                vehicle.PrimaryColor = int.Parse(primaryVehicleColor);
                vehicle.SecondaryColor = int.Parse(secondaryVehicleColor);
                vehicle.PearlescentColor = vehiclePearlescentColor;
            }
            else {
                var primaryColor = primaryVehicleColor.Split(',');
                var secondaryColor = secondaryVehicleColor.Split(',');
                vehicle.CustomPrimaryColor = new Color(int.Parse(primaryColor[0]), int.Parse(primaryColor[1]),
                    int.Parse(primaryColor[2]));
                vehicle.CustomSecondaryColor = new Color(int.Parse(secondaryColor[0]), int.Parse(secondaryColor[1]),
                    int.Parse(secondaryColor[2]));
            }
        }

        [RemoteEvent("modifyVehicle")]
        public void ModifyVehicleEvent(Client player, int slot, int component) {
            var vehicle = player.Vehicle;

            if (component > 0)
                player.Vehicle.SetMod(slot, component);
            else
                player.Vehicle.RemoveMod(slot);
        }

        [RemoteEvent("cancelVehicleModification")]
        public void CancelVehicleModificationEvent(Client player) {
            int vehicleId = player.Vehicle.GetData(EntityData.VEHICLE_ID);

            for (var i = 0; i < 49; i++) {
                // Get the component in the slot
                var component = GetVehicleTunningComponent(vehicleId, i);

                // Remove or add the tunning part
                player.Vehicle.SetMod(i, component);
            }
        }

        [RemoteEvent("confirmVehicleModification")]
        public void ConfirmVehicleModificationEvent(Client player, int slot, int mod) {
            // Get the vehicle's id
            int vehicleId = player.Vehicle.GetData(EntityData.VEHICLE_ID);

            // Get player's product amount
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
            var item = Globals.GetPlayerItemModelFromHash(playerId, Constants.ITEM_HASH_BUSINESS_PRODUCTS);

            // Calculate the cost for the tunning
            var totalProducts = Constants.TUNNING_PRICE_LIST.Where(x => x.slot == slot).First().products;

            if (item != null && item.amount >= totalProducts) {
                // Add component to database
                var tunningModel = new TunningModel();
                {
                    tunningModel.slot = slot;
                    tunningModel.component = mod;
                    tunningModel.vehicle = vehicleId;
                }

                Task.Factory.StartNew(() => {
                    tunningModel.id = DBMechanicCommands.AddTunning(tunningModel);
                    tunningList.Add(tunningModel);

                    // Remove consumed products
                    item.amount -= totalProducts;

                    // Update the amount into the database
                    Database.UpdateItem(item);

                    // Confirmation message
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.vehicle_tunning);
                });
            }
            else {
                var message = string.Format(ErrRes.not_required_products, totalProducts);
                player.SendChatMessage(Constants.COLOR_ERROR + message);
            }
        }

        [Command(Commands.COM_REPAIR, Commands.HLP_MECHANIC_REPAIR_COMMAND)]
        public void RepairCommand(Client player, int vehicleId, string type, int price = 0) {
            if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_MECHANIC) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_mechanic);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (PlayerInValidRepairPlace(player) == false) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_valid_repair_place);
            }
            else {
                var vehicle = Vehicles.GetVehicleById(vehicleId);
                if (vehicle == null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_not_exists);
                }
                else if (vehicle.Position.DistanceTo(player.Position) > 5.0f) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.wanted_vehicle_far);
                }
                else {
                    var spentProducts = 0;

                    switch (type.ToLower()) {
                        case Commands.ARG_CHASSIS:
                            spentProducts = Constants.PRICE_VEHICLE_CHASSIS;
                            break;
                        case Commands.ARG_DOORS:
                            for (var i = 0; i < 6; i++)
                                if (vehicle.IsDoorBroken(i))
                                    spentProducts += Constants.PRICE_VEHICLE_DOORS;
                            break;
                        case Commands.ARG_TYRES:
                            for (var i = 0; i < 4; i++)
                                if (vehicle.IsTyrePopped(i))
                                    spentProducts += Constants.PRICE_VEHICLE_TYRES;
                            break;
                        case Commands.ARG_WINDOWS:
                            for (var i = 0; i < 4; i++)
                                if (vehicle.IsWindowBroken(i))
                                    spentProducts += Constants.PRICE_VEHICLE_WINDOWS;
                            break;
                        default:
                            player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_MECHANIC_REPAIR_COMMAND);
                            return;
                    }

                    if (price > 0) {
                        // Get player's products
                        int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                        var item = Globals.GetPlayerItemModelFromHash(playerId, Constants.ITEM_HASH_BUSINESS_PRODUCTS);

                        if (item != null && item.amount >= spentProducts) {
                            int vehicleFaction = vehicle.GetData(EntityData.VEHICLE_FACTION);

                            // Check a player with vehicle keys
                            foreach (var target in NAPI.Pools.GetAllPlayers())
                                if (Vehicles.HasPlayerVehicleKeys(target, vehicle) || vehicleFaction > 0 &&
                                    target.GetData(EntityData.PLAYER_FACTION) == vehicleFaction)
                                    if (target.Position.DistanceTo(player.Position) < 4.0f) {
                                        // Fill repair entity data
                                        target.SetData(EntityData.PLAYER_JOB_PARTNER, player);
                                        target.SetData(EntityData.PLAYER_REPAIR_VEHICLE, vehicle);
                                        target.SetData(EntityData.PLAYER_REPAIR_TYPE, type);
                                        target.SetData(EntityData.JOB_OFFER_PRODUCTS, spentProducts);
                                        target.SetData(EntityData.JOB_OFFER_PRICE, price);

                                        var playerMessage = string.Format(InfoRes.mechanic_repair_offer, target.Name,
                                            price);
                                        var targetMessage = string.Format(InfoRes.mechanic_repair_accept, player.Name,
                                            price);
                                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                                        return;
                                    }

                            // There's no player with the vehicle's keys near
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                        }
                        else {
                            var message = string.Format(ErrRes.not_required_products, spentProducts);
                            player.SendChatMessage(Constants.COLOR_ERROR + message);
                        }
                    }
                    else {
                        var message = string.Format(InfoRes.repair_price, spentProducts);
                        player.SendChatMessage(Constants.COLOR_INFO + message);
                    }
                }
            }
        }

        [Command(Commands.COM_REPAINT, Commands.HLP_MECHANIC_REPAINT_COMMAND)]
        public void RepaintCommand(Client player, int vehicleId) {
            if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_MECHANIC) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_mechanic);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                foreach (var business in Business.businessList)
                    if (business.type == Constants.BUSINESS_TYPE_MECHANIC &&
                        player.Position.DistanceTo(business.position) < 25.0f) {
                        var vehicle = Vehicles.GetVehicleById(vehicleId);
                        if (vehicle != null) {
                            player.SetData(EntityData.PLAYER_VEHICLE, vehicle);
                            player.TriggerEvent("showRepaintMenu");
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_not_exists);
                        }

                        return;
                    }

                // Player is not in any workshop
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_mechanic_workshop);
            }
        }

        [Command(Commands.COM_TUNNING)]
        public void TunningCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_MECHANIC) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_mechanic);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                foreach (var business in Business.businessList)
                    if (business.type == Constants.BUSINESS_TYPE_MECHANIC &&
                        player.Position.DistanceTo(business.position) < 25.0f) {
                        if (player.IsInVehicle) {
                            player.SetData(EntityData.PLAYER_VEHICLE, player.Vehicle);
                            player.TriggerEvent("showTunningMenu");
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle);
                        }

                        return;
                    }

                // Player is not in any workshop
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_mechanic_workshop);
            }
        }
    }
}