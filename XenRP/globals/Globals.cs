﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.business;
using XenRP.character;
using XenRP.chat;
using XenRP.database;
using XenRP.drivingschool;
using XenRP.factions;
using XenRP.house;
using XenRP.jobs;
using XenRP.messages.administration;
using XenRP.messages.error;
using XenRP.messages.general;
using XenRP.messages.information;
using XenRP.messages.success;
using XenRP.model;
using XenRP.parking;
using XenRP.vehicles;
using XenRP.weapons;

namespace XenRP.globals {
    public class Globals : Script {
        public static int orderGenerationTime;
        public static List<FastfoodOrderModel> fastFoodOrderList;
        public static List<OrderModel> truckerOrderList;
        public static List<ClothesModel> clothesList;
        public static List<TattooModel> tattooList;
        public static List<ItemModel> itemList;
        public static List<ScoreModel> scoreList;
        public static List<AdminTicketModel> adminTicketList;
        private static Timer playerListUpdater;
        private static Timer minuteTimer;
        private int fastFoodId = 1;

        public static Client GetPlayerById(int id) {
            // Get the player with the selected identifier
            return NAPI.Pools.GetAllPlayers().Where(pl => pl.Value == id).FirstOrDefault();
        }

        public static int GetTotalSeconds() {
            return (int) DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }

        private void UpdatePlayerList(object unused) {
            // Initialize the score list
            var scoreList = new List<ScoreModel>();

            // Get all the players ingame
            var playingPlayers = NAPI.Pools.GetAllPlayers().Where(p => p.GetData(EntityData.PLAYER_PLAYING) != null)
                .ToList();

            // Update the score list
            foreach (var player in playingPlayers) {
                var score = new ScoreModel(player.Value, player.Name, player.Ping);
                scoreList.Add(score);
            }

            // Update the list for all the players
            playingPlayers.ForEach(p => p.TriggerEvent("updatePlayerList", NAPI.Util.ToJson(scoreList)));
        }

        private void OnMinuteSpent(object unused) {
            // Adjust server's time
            var currentTime = TimeSpan.FromTicks(DateTime.Now.Ticks);
            NAPI.World.SetTime(currentTime.Hours, currentTime.Minutes, currentTime.Seconds);

            var totalSeconds = GetTotalSeconds();
            var onlinePlayers = NAPI.Pools.GetAllPlayers().Where(pl => pl.GetData(EntityData.PLAYER_PLAYING) != null)
                .ToList();

            foreach (var player in onlinePlayers) {
                int played = player.GetData(EntityData.PLAYER_PLAYED);
                if (played > 0 && played % 60 == 0) {
                    // Reduce job cooldown
                    int employeeCooldown = player.GetData(EntityData.PLAYER_EMPLOYEE_COOLDOWN);
                    if (employeeCooldown > 0) player.SetData(EntityData.PLAYER_EMPLOYEE_COOLDOWN, employeeCooldown - 1);

                    // Generate the payday
                    GeneratePlayerPayday(player);
                }

                player.SetData(EntityData.PLAYER_PLAYED, played + 1);

                // Check if the player is injured waiting for the hospital respawn
                if (player.GetData(EntityData.TIME_HOSPITAL_RESPAWN) != null &&
                    player.GetData(EntityData.TIME_HOSPITAL_RESPAWN) <= totalSeconds)
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_can_die);

                // Check if the player has job cooldown
                int jobCooldown = player.GetData(EntityData.PLAYER_JOB_COOLDOWN);
                if (jobCooldown > 0) player.SetData(EntityData.PLAYER_JOB_COOLDOWN, jobCooldown - 1);

                // Get the remaining jail time
                int jailTime = player.GetData(EntityData.PLAYER_JAILED);

                if (jailTime > 0) {
                    jailTime--;
                    player.SetData(EntityData.PLAYER_JAILED, jailTime);
                }
                else if (jailTime == 0) {
                    // Set the player position
                    player.Position =
                        Constants.JAIL_SPAWNS[
                            player.GetData(EntityData.PLAYER_JAIL_TYPE) == Constants.JAIL_TYPE_IC ? 3 : 4];

                    // Remove player from jail
                    player.SetData(EntityData.PLAYER_JAILED, -1);
                    player.SetData(EntityData.PLAYER_JAIL_TYPE, -1);

                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_unjailed);
                }

                if (player.GetData(EntityData.PLAYER_DRUNK_LEVEL) != null) {
                    // Lower alcohol level
                    float drunkLevel = player.GetData(EntityData.PLAYER_DRUNK_LEVEL) - 0.05f;

                    if (drunkLevel <= 0.0f) {
                        player.ResetData(EntityData.PLAYER_DRUNK_LEVEL);
                    }
                    else {
                        if (drunkLevel < Constants.WASTED_LEVEL) {
                            player.ResetSharedData(EntityData.PLAYER_WALKING_STYLE);
                            NAPI.ClientEvent.TriggerClientEventForAll("resetPlayerWalkingStyle", player.Handle);
                        }

                        player.SetData(EntityData.PLAYER_DRUNK_LEVEL, drunkLevel);
                    }
                }

                // Save the character's data
                Character.SaveCharacterData(player);
            }

            // Generate new fastfood orders
            if (orderGenerationTime <= totalSeconds && House.houseList.Count > 0) {
                var rnd = new Random();
                var generatedOrders = rnd.Next(7, 20);
                for (var i = 0; i < generatedOrders; i++) {
                    var order = new FastfoodOrderModel();
                    {
                        order.id = fastFoodId;
                        order.pizzas = rnd.Next(0, 4);
                        order.hamburgers = rnd.Next(0, 4);
                        order.sandwitches = rnd.Next(0, 4);
                        order.position = GetPlayerFastFoodDeliveryDestination();
                        order.limit = totalSeconds + 300;
                        order.taken = false;
                    }

                    fastFoodOrderList.Add(order);
                    fastFoodId++;
                }

                // Update the new timer time
                orderGenerationTime = totalSeconds + rnd.Next(2, 5) * 60;
            }

            // Remove old orders
            fastFoodOrderList.RemoveAll(order => !order.taken && order.limit <= totalSeconds);

            // Save all the vehicles
            Vehicles.SaveAllVehicles();
        }

        private void GeneratePlayerPayday(Client player) {
            var total = 0;
            int bank = player.GetSharedData(EntityData.PLAYER_BANK);
            int playerJob = player.GetData(EntityData.PLAYER_JOB);
            int playerRank = player.GetData(EntityData.PLAYER_RANK);
            int playerFaction = player.GetData(EntityData.PLAYER_FACTION);
            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.payday_title);

            // Generate the salary
            if (playerFaction > 0 && playerFaction <= Constants.LAST_STATE_FACTION)
                foreach (var faction in Constants.FACTION_RANK_LIST)
                    if (faction.faction == playerFaction && faction.rank == playerRank) {
                        total += faction.salary;
                        break;
                    }
            else
                foreach (var job in Constants.JOB_LIST)
                    if (job.job == playerJob) {
                        total += job.salary;
                        break;
                    }

            player.SendChatMessage(Constants.COLOR_HELP + GenRes.salary + total + "$");

            // Extra income from the level
            var levelEarnings = GetPlayerLevel(player) * Constants.PAID_PER_LEVEL;
            total += levelEarnings;
            if (levelEarnings > 0)
                player.SendChatMessage(Constants.COLOR_HELP + GenRes.extra_income + levelEarnings + "$");

            // Bank interest
            var bankInterest = (int) Math.Round(bank * 0.001);
            total += bankInterest;
            if (bankInterest > 0)
                player.SendChatMessage(Constants.COLOR_HELP + GenRes.bank_interest + bankInterest + "$");

            // Generación de impuestos por vehículos
            foreach (var vehicle in NAPI.Pools.GetAllVehicles()) {
                var vehicleHass = (VehicleHash) vehicle.Model;
                if (vehicle.GetData(EntityData.VEHICLE_OWNER) == player.Name &&
                    NAPI.Vehicle.GetVehicleClass(vehicleHass) != Constants.VEHICLE_CLASS_CYCLES) {
                    var vehicleTaxes =
                        (int) Math.Round(vehicle.GetData(EntityData.VEHICLE_PRICE) * Constants.TAXES_VEHICLE);
                    int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
                    string vehicleModel = vehicle.GetData(EntityData.VEHICLE_MODEL);
                    string vehiclePlate = vehicle.GetData(EntityData.VEHICLE_PLATE) == string.Empty
                        ? "LS " + (1000 + vehicleId)
                        : vehicle.GetData(EntityData.VEHICLE_PLATE);
                    player.SendChatMessage(Constants.COLOR_HELP + GenRes.vehicle_taxes_from + vehicleModel + " (" +
                                           vehiclePlate + "): -" + vehicleTaxes + "$");
                    total -= vehicleTaxes;
                }
            }

            // Vehicle taxes
            foreach (var parkedCar in Parking.parkedCars) {
                var vehicleHass = NAPI.Util.VehicleNameToModel(parkedCar.vehicle.model);
                if (parkedCar.vehicle.owner == player.Name &&
                    NAPI.Vehicle.GetVehicleClass(vehicleHass) != Constants.VEHICLE_CLASS_CYCLES) {
                    var vehicleTaxes = (int) Math.Round(parkedCar.vehicle.price * Constants.TAXES_VEHICLE);
                    var vehiclePlate = parkedCar.vehicle.plate == string.Empty
                        ? "LS " + (1000 + parkedCar.vehicle.id)
                        : parkedCar.vehicle.plate;
                    player.SendChatMessage(Constants.COLOR_HELP + GenRes.vehicle_taxes_from + parkedCar.vehicle.model +
                                           " (" + vehiclePlate + "): -" + vehicleTaxes + "$");
                    total -= vehicleTaxes;
                }
            }

            // House taxes
            foreach (var house in House.houseList)
                if (house.owner == player.Name) {
                    var houseTaxes = (int) Math.Round(house.price * Constants.TAXES_HOUSE);
                    player.SendChatMessage(Constants.COLOR_HELP + GenRes.house_taxes_from + house.name + ": -" +
                                           houseTaxes + "$");
                    total -= houseTaxes;
                }

            // Calculate the total balance
            player.SendChatMessage(Constants.COLOR_HELP + "=====================");
            player.SendChatMessage(Constants.COLOR_HELP + GenRes.total + total + "$");
            player.SetSharedData(EntityData.PLAYER_BANK, bank + total);

            Task.Factory.StartNew(() => {
                // Add the payment log
                Database.LogPayment("Payday", player.Name, "Payday", total);
            });
        }

        private Vector3 GetPlayerFastFoodDeliveryDestination() {
            var random = new Random();
            var element = random.Next(House.houseList.Count);
            return House.houseList[element].position;
        }

        public static ItemModel GetItemModelFromId(int itemId) {
            ItemModel item = null;
            foreach (var itemModel in itemList)
                if (itemModel.id == itemId) {
                    item = itemModel;
                    break;
                }

            return item;
        }

        public static ItemModel GetPlayerItemModelFromHash(int playerId, string hash) {
            ItemModel itemModel = null;
            foreach (var item in itemList)
                if (item.ownerEntity == Constants.ITEM_ENTITY_PLAYER && item.ownerIdentifier == playerId &&
                    item.hash == hash) {
                    itemModel = item;
                    break;
                }

            return itemModel;
        }

        public static ItemModel GetClosestItem(Client player) {
            ItemModel itemModel = null;
            foreach (var item in itemList)
                if (item.ownerEntity == Constants.ITEM_ENTITY_GROUND &&
                    player.Position.DistanceTo(item.position) < 2.0f) {
                    itemModel = item;
                    break;
                }

            return itemModel;
        }

        public static ItemModel GetClosestItemWithHash(Client player, string hash) {
            ItemModel itemModel = null;
            foreach (var item in itemList)
                if (item.ownerEntity == Constants.ITEM_ENTITY_GROUND && item.hash == hash &&
                    player.Position.DistanceTo(item.position) < 2.0f) {
                    itemModel = item;
                    break;
                }

            return itemModel;
        }

        public static ItemModel GetItemInEntity(int entityId, string entity) {
            ItemModel item = null;
            foreach (var itemModel in itemList)
                if (itemModel.ownerEntity == entity && itemModel.ownerIdentifier == entityId) {
                    item = itemModel;
                    break;
                }

            return item;
        }

        public static Vector3 GetForwardPosition(Entity entity, float distance) {
            var position = entity.Position;

            // Get the X and Y coordinates
            position.X += distance * (float) Math.Sin(180.0f - entity.Rotation.Z);
            position.Y += distance * (float) Math.Cos(180.0f - entity.Rotation.Z);

            return position;
        }

        private void SubstractPlayerItems(ItemModel item, int amount = 1) {
            item.amount -= amount;
            if (item.amount == 0)
                Task.Factory.StartNew(() => {
                    // Remove the item from the database
                    Database.RemoveItem(item.id);
                    itemList.Remove(item);
                });
        }

        private int GetPlayerInventoryTotal(Client player) {
            // Return the amount of items in the player's inventory
            return itemList.Count(item =>
                item.ownerEntity == Constants.ITEM_ENTITY_PLAYER &&
                player.GetData(EntityData.PLAYER_SQL_ID) == item.ownerIdentifier);
        }

        private List<InventoryModel> GetPlayerInventory(Client player) {
            var inventory = new List<InventoryModel>();
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
            foreach (var item in itemList)
                if (item.ownerEntity == Constants.ITEM_ENTITY_PLAYER && item.ownerIdentifier == playerId) {
                    var businessItem = Business.GetBusinessItemFromHash(item.hash);
                    if (businessItem != null && businessItem.type != Constants.ITEM_TYPE_WEAPON) {
                        // Create the item into the inventory
                        var inventoryItem = new InventoryModel();
                        {
                            inventoryItem.id = item.id;
                            inventoryItem.hash = item.hash;
                            inventoryItem.description = businessItem.description;
                            inventoryItem.type = businessItem.type;
                            inventoryItem.amount = item.amount;
                        }

                        // Add the item to the inventory
                        inventory.Add(inventoryItem);
                    }
                }

            return inventory;
        }

        private void UpdateInventory(Client player, ItemModel item, BusinessItemModel businessItem, int target) {
            // Create the inventory item to update
            var inventoryItem = new InventoryModel();
            {
                inventoryItem.id = item.id;
                inventoryItem.hash = item.hash;
                inventoryItem.description = businessItem.description;
                inventoryItem.type = businessItem.type;
                inventoryItem.amount = item.amount;
            }

            // Update the inventory
            player.TriggerEvent("updateInventory", NAPI.Util.ToJson(inventoryItem), target);
        }

        private void ConsumeItem(Client player, ItemModel item, BusinessItemModel businessItem, bool consumedFromHand) {
            item.amount--;

            // Check if it changes the health
            if (businessItem.health != 0) {
                player.Health += businessItem.health;
                if (player.Health > 100) player.Health = 100;
            }

            if (businessItem.alcoholLevel > 0) {
                float currentAlcohol = 0;
                if (player.GetData(EntityData.PLAYER_DRUNK_LEVEL) != null)
                    currentAlcohol = player.GetData(EntityData.PLAYER_DRUNK_LEVEL);
                player.SetData(EntityData.PLAYER_DRUNK_LEVEL, currentAlcohol + businessItem.alcoholLevel);

                if (currentAlcohol + businessItem.alcoholLevel > Constants.WASTED_LEVEL) {
                    player.SetSharedData(EntityData.PLAYER_WALKING_STYLE, "move_m@drunk@verydrunk");
                    NAPI.ClientEvent.TriggerClientEventForAll("changePlayerWalkingStyle", player.Handle,
                        "move_m@drunk@verydrunk");
                }
            }

            Task.Factory.StartNew(() => {
                if (item.amount == 0) {
                    // Remove the item from the hand
                    NAPI.ClientEvent.TriggerClientEventInDimension(player.Dimension, "dettachItemFromPlayer",
                        player.Value);

                    player.ResetSharedData(EntityData.PLAYER_RIGHT_HAND);

                    // Remove the item from the database
                    Database.RemoveItem(item.id);
                    itemList.Remove(item);
                }
                else {
                    // Update the amount into the database
                    Database.UpdateItem(item);
                }

                if (!consumedFromHand) UpdateInventory(player, item, businessItem, Constants.INVENTORY_TARGET_SELF);

                var message = string.Format(InfoRes.player_inventory_consume, businessItem.description.ToLower());
                player.SendChatMessage(Constants.COLOR_INFO + message);
            });
        }

        private void DropItem(Client player, ItemModel item, BusinessItemModel businessItem, bool droppedFromHand) {
            // Check if there are items of the same type near
            var closestItem = GetClosestItemWithHash(player, item.hash);

            // Check if it's a weapon or not
            var weaponHash = NAPI.Util.WeaponNameToModel(item.hash);

            // Get the dropped amount
            var amount = weaponHash != 0 ? item.amount : 1;
            item.amount -= amount;

            Task.Factory.StartNew(() => {
                if (closestItem != null) {
                    closestItem.amount += amount;

                    // Update the closest item's amount
                    Database.UpdateItem(closestItem);
                }
                else {
                    if (weaponHash != 0 || weaponHash == 0 && uint.TryParse(item.hash, out var hash))
                        NAPI.Task.Run(() => {
                            // Get the hash from the item dropped
                            var itemHash = weaponHash != 0
                                ? NAPI.Util.GetHashKey(Constants.WEAPON_ITEM_MODELS[weaponHash])
                                : uint.Parse(item.hash);

                            closestItem = item.Copy();
                            closestItem.amount = amount;
                            closestItem.ownerEntity = Constants.ITEM_ENTITY_GROUND;
                            closestItem.dimension = player.Dimension;
                            closestItem.position = new Vector3(player.Position.X, player.Position.Y,
                                player.Position.Z - 0.8f);
                            closestItem.objectHandle = NAPI.Object.CreateObject(itemHash, closestItem.position,
                                new Vector3(0.0f, 0.0f, 0.0f), (byte) closestItem.dimension);

                            // Create the new item
                            closestItem.id = Database.AddNewItem(closestItem);
                            itemList.Add(closestItem);
                        });
                }

                if (item.amount == 0) {
                    if (droppedFromHand) {
                        // Remove the attachment information
                        player.ResetSharedData(EntityData.PLAYER_RIGHT_HAND);

                        if (weaponHash != 0)
                            player.RemoveWeapon(weaponHash);
                        else
                            NAPI.ClientEvent.TriggerClientEventInDimension(player.Dimension, "dettachItemFromPlayer",
                                player.Value);
                    }

                    // There are no more items, we delete it
                    Database.RemoveItem(item.id);
                    itemList.Remove(item);
                }
                else {
                    // Update the item's amount
                    Database.UpdateItem(item);
                }

                if (!droppedFromHand) UpdateInventory(player, item, businessItem, Constants.INVENTORY_TARGET_SELF);

                var message = string.Format(InfoRes.player_inventory_drop, businessItem.description.ToLower());
                player.SendChatMessage(Constants.COLOR_INFO + message);
            });
        }

        public static void StoreItemOnHand(Client player) {
            // Get the item identifier
            string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
            var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;

            var item = GetItemModelFromId(itemId);

            if (NAPI.Util.WeaponNameToModel(item.hash) != 0)
                player.GiveWeapon(WeaponHash.Unarmed, 0);
            else
                NAPI.ClientEvent.TriggerClientEventInDimension(player.Dimension, "dettachItemFromPlayer", player.Value);

            // Reset the player data
            player.ResetSharedData(EntityData.PLAYER_RIGHT_HAND);

            Task.Factory.StartNew(() => {
                // Search for items of the same type
                ItemModel inventoryItem =
                    GetPlayerItemModelFromHash(player.GetData(EntityData.PLAYER_SQL_ID), item.hash);

                if (inventoryItem == null) {
                    // Store the item on the floor
                    item.ownerEntity = Constants.ITEM_ENTITY_PLAYER;

                    // Update the amount into the database
                    Database.UpdateItem(item);
                }
                else {
                    // Add the amount to the item in the inventory
                    inventoryItem.amount += item.amount;

                    // Delete the item on the hand
                    Database.RemoveItem(item.id);
                    itemList.Remove(item);
                }
            });
        }

        public static List<InventoryModel> GetPlayerInventoryAndWeapons(Client player) {
            var inventory = new List<InventoryModel>();
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
            foreach (var item in itemList)
                if (item.ownerEntity == Constants.ITEM_ENTITY_PLAYER && item.ownerIdentifier == playerId) {
                    var businessItem = Business.GetBusinessItemFromHash(item.hash);
                    if (businessItem != null) {
                        // Create the item into the inventory
                        var inventoryItem = new InventoryModel();
                        {
                            inventoryItem.id = item.id;
                            inventoryItem.hash = item.hash;
                            inventoryItem.description = businessItem.description;
                            inventoryItem.type = businessItem.type;
                            inventoryItem.amount = item.amount;
                        }

                        // Add the item to the inventory
                        inventory.Add(inventoryItem);
                    }
                }

            return inventory;
        }

        public static List<InventoryModel> GetVehicleTrunkInventory(Vehicle vehicle) {
            var inventory = new List<InventoryModel>();
            int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
            foreach (var item in itemList)
                if (item.ownerEntity == Constants.ITEM_ENTITY_VEHICLE && item.ownerIdentifier == vehicleId) {
                    // Check whether is a common item or a weapon
                    var inventoryItem = new InventoryModel();
                    var businessItem = Business.GetBusinessItemFromHash(item.hash);

                    if (businessItem != null) {
                        inventoryItem.description = businessItem.description;
                        inventoryItem.type = businessItem.type;
                    }
                    else {
                        inventoryItem.description = item.hash;
                        inventoryItem.type = Constants.ITEM_TYPE_WEAPON;
                    }

                    // Update the values
                    inventoryItem.id = item.id;
                    inventoryItem.hash = item.hash;
                    inventoryItem.amount = item.amount;

                    // Add the item to the inventory
                    inventory.Add(inventoryItem);
                }

            return inventory;
        }

        public static List<ClothesModel> GetPlayerClothes(int playerId) {
            // Get a list with the player's clothes
            return clothesList.Where(c => c.player == playerId).ToList();
        }

        public static ClothesModel GetDressedClothesInSlot(int playerId, int type, int slot) {
            // Get the clothes in the selected slot
            return clothesList.FirstOrDefault(
                c => c.player == playerId && c.type == type && c.slot == slot && c.dressed);
        }

        public static List<string> GetClothesNames(List<ClothesModel> clothesList) {
            var clothesNames = new List<string>();
            foreach (var clothes in clothesList)
            foreach (var businessClothes in Constants.BUSINESS_CLOTHES_LIST)
                if (businessClothes.clothesId == clothes.drawable && businessClothes.bodyPart == clothes.slot &&
                    businessClothes.type == clothes.type) {
                    clothesNames.Add(businessClothes.description);
                    break;
                }

            return clothesNames;
        }

        public static void UndressClothes(int playerId, int type, int slot) {
            foreach (var clothes in clothesList)
                if (clothes.player == playerId && clothes.type == type && clothes.slot == slot && clothes.dressed) {
                    clothes.dressed = false;

                    Task.Factory.StartNew(() => {
                        // Update the clothes' state
                        Database.UpdateClothes(clothes);
                    });

                    break;
                }
        }

        public static void ShowPlayerData(Client asker, Client player) {
            int rolePoints = player.GetData(EntityData.PLAYER_ROLE_POINTS);
            var sex = player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_MALE ? GenRes.sex_male : GenRes.sex_female;
            string age = player.GetData(EntityData.PLAYER_AGE) + GenRes.years;
            string money = player.GetSharedData(EntityData.PLAYER_MONEY) + "$";
            string bank = player.GetSharedData(EntityData.PLAYER_BANK) + "$";
            var job = GenRes.unemployed;
            var faction = GenRes.no_faction;
            var rank = GenRes.no_rank;
            var houses = string.Empty;
            var ownedVehicles = string.Empty;
            string lentVehicles = player.GetData(EntityData.PLAYER_VEHICLE_KEYS);
            TimeSpan played = TimeSpan.FromMinutes(player.GetData(EntityData.PLAYER_PLAYED));

            // Check if the player has a job
            foreach (var jobModel in Constants.JOB_LIST)
                if (player.GetData(EntityData.PLAYER_JOB) == jobModel.job) {
                    job = player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_MALE
                        ? jobModel.descriptionMale
                        : jobModel.descriptionFemale;
                    break;
                }

            // Check if the player is in any faction
            foreach (var factionModel in Constants.FACTION_RANK_LIST)
                if (player.GetData(EntityData.PLAYER_FACTION) == factionModel.faction &&
                    player.GetData(EntityData.PLAYER_RANK) == factionModel.rank) {
                    switch (factionModel.faction) {
                        case Constants.FACTION_POLICE:
                            faction = GenRes.police_faction;
                            break;
                        case Constants.FACTION_EMERGENCY:
                            faction = GenRes.emergency_faction;
                            break;
                        case Constants.FACTION_NEWS:
                            faction = GenRes.news_faction;
                            break;
                        case Constants.FACTION_TOWNHALL:
                            faction = GenRes.townhall_faction;
                            break;
                        case Constants.FACTION_TAXI_DRIVER:
                            faction = GenRes.transport_faction;
                            break;
                    }

                    // Set player's rank
                    rank = player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_MALE
                        ? factionModel.descriptionMale
                        : factionModel.descriptionFemale;
                    break;
                }

            // Check if the player has any rented house
            if (player.GetSharedData(EntityData.PLAYER_RENT_HOUSE) > 0)
                houses += " " + player.GetSharedData(EntityData.PLAYER_RENT_HOUSE);

            // Get player's owned houses
            foreach (var house in House.houseList)
                if (house.owner == player.Name)
                    houses += " " + house.id;

            // Check for the player's owned vehicles
            foreach (var vehicle in NAPI.Pools.GetAllVehicles())
                if (vehicle.GetData(EntityData.VEHICLE_OWNER) == player.Name)
                    ownedVehicles += " " + vehicle.GetData(EntityData.VEHICLE_ID);

            foreach (var parkedVehicle in Parking.parkedCars)
                if (parkedVehicle.vehicle.owner == player.Name)
                    ownedVehicles += " " + parkedVehicle.vehicle.id;

            // Show all the information
            asker.SendChatMessage(Constants.COLOR_INFO + InfoRes.basic_data);
            asker.SendChatMessage(Constants.COLOR_HELP + GenRes.name + player.Name + "; " + GenRes.sex + sex + "; " +
                                  GenRes.age + age + "; " + GenRes.money + money + "; " + GenRes.bank + bank);
            asker.SendChatMessage(Constants.COLOR_INFO + " ");
            asker.SendChatMessage(Constants.COLOR_INFO + InfoRes.job_data);
            asker.SendChatMessage(Constants.COLOR_HELP + GenRes.job + job + "; " + GenRes.faction + faction + "; " +
                                  GenRes.rank + rank);
            asker.SendChatMessage(Constants.COLOR_INFO + " ");
            asker.SendChatMessage(Constants.COLOR_INFO + InfoRes.properties);
            asker.SendChatMessage(Constants.COLOR_HELP + GenRes.houses + houses);
            asker.SendChatMessage(Constants.COLOR_HELP + GenRes.owned_vehicles + ownedVehicles);
            asker.SendChatMessage(Constants.COLOR_HELP + GenRes.lent_vehicles + lentVehicles);
            asker.SendChatMessage(Constants.COLOR_INFO + " ");
            asker.SendChatMessage(Constants.COLOR_INFO + InfoRes.additional_data);
            asker.SendChatMessage(Constants.COLOR_HELP + GenRes.played_time + (int) played.TotalHours + "h " +
                                  played.Minutes + "m; " + GenRes.role_points + rolePoints);
        }

        public static void AttachItemToPlayer(Client player, int itemId, string hash, string bodyPart, Vector3 position,
            Vector3 rotation, string entityKey) {
            var attachment = new AttachmentModel(itemId, hash, bodyPart, position, rotation);
            var attachmentJson = NAPI.Util.ToJson(attachment);

            player.SetSharedData(entityKey, attachmentJson);

            NAPI.ClientEvent.TriggerClientEventInDimension(player.Dimension, "attachItemToPlayer", player.Value,
                attachmentJson);
        }

        public static void RemoveItemOnHands(Client player) {
            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                // Get the item on the right hand
                string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
                var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;
                var currentItem = GetItemModelFromId(itemId);

                // Check if it's a weapon or not
                var weaponHash = NAPI.Util.WeaponNameToModel(currentItem.hash);

                if (weaponHash != 0)
                    player.GiveWeapon(WeaponHash.Unarmed, 0);
                else
                    NAPI.ClientEvent.TriggerClientEventInDimension(player.Dimension, "dettachItemFromPlayer",
                        player.Value);
            }
            else if (player.GetSharedData(EntityData.PLAYER_WEAPON_CRATE) != null) {
                // Reset the data
                player.ResetSharedData(EntityData.PLAYER_WEAPON_CRATE);

                // Remove the item from the hand
                NAPI.ClientEvent.TriggerClientEventInDimension(player.Dimension, "dettachItemFromPlayer", player.Value);
            }
        }

        private int GetPlayerLevel(Client player) {
            float playedHours = player.GetData(EntityData.PLAYER_PLAYED) / 100;
            return (int) Math.Round(Math.Log(playedHours) * Constants.LEVEL_MULTIPLIER);
        }

        [ServerEvent(Event.PlayerEnterVehicle)]
        public void OnPlayerEnterVehicle(Client player, Vehicle vehicle, sbyte seat) {
            //NAPI.Native.SendNativeToPlayer(player, Hash.SET_PED_HELMET, player, false);
        }

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart() {
            adminTicketList = new List<AdminTicketModel>();
            fastFoodOrderList = new List<FastfoodOrderModel>();
            truckerOrderList = new List<OrderModel>();

            // Area in the lobby to change the character
            NAPI.TextLabel.CreateTextLabel(GenRes.character_help, new Vector3(401.8016f, -1001.897f, -99.00404f), 20.0f,
                0.75f, 4, new Color(255, 255, 255), false);

            // Add car dealer's interior
            NAPI.World.RequestIpl("shr_int");
            NAPI.World.RequestIpl("shr_int_lod");
            NAPI.World.RemoveIpl("fakeint");
            NAPI.World.RemoveIpl("fakeint_lod");
            NAPI.World.RemoveIpl("fakeint_boards");
            NAPI.World.RemoveIpl("fakeint_boards_lod");
            NAPI.World.RemoveIpl("shutter_closed");

            // Add clubhouse's door
            NAPI.World.RequestIpl("hei_bi_hw1_13_door");

            // Close the doors from the lobby
            NAPI.Object.CreateObject(NAPI.Util.GetHashKey("bkr_prop_biker_door_entry"),
                new Vector3(404.5286f, -996.5656f, -98.80404f), new Vector3(0.0f, 0.0f, 90.0f));
            NAPI.Object.CreateObject(NAPI.Util.GetHashKey("bkr_prop_biker_door_entry"),
                new Vector3(401.2006f, -997.8686f, -98.80404f), new Vector3(0.0f, 0.0f, 270.0f));

            // Avoid player's respawn
            NAPI.Server.SetAutoRespawnAfterDeath(false);
            NAPI.Server.SetAutoSpawnOnConnect(false);

            // Disable global server chat
            NAPI.Server.SetGlobalServerChat(false);

            foreach (var interior in Constants.INTERIOR_LIST) {
                if (interior.blipId > 0) {
                    interior.blip = NAPI.Blip.CreateBlip(interior.entrancePosition);
                    interior.blip.Sprite = (uint) interior.blipId;
                    interior.blip.Name = interior.blipName;
                    interior.blip.ShortRange = true;
                }

                if (interior.captionMessage != string.Empty)
                    interior.textLabel = NAPI.TextLabel.CreateTextLabel(interior.captionMessage,
                        interior.entrancePosition, 20.0f, 0.75f, 4, new Color(255, 255, 255), false, 0);
            }

            // Fastfood orders
            var rnd = new Random();
            orderGenerationTime = GetTotalSeconds() + rnd.Next(0, 1) * 60;

            // Permanent timers
            minuteTimer = new Timer(OnMinuteSpent, null, 60000, 60000);
            playerListUpdater = new Timer(UpdatePlayerList, null, 750, 750);
        }

        [ServerEvent(Event.PlayerDisconnected)]
        public void OnPlayerDisconnected(Client player, DisconnectionType type, string reason) {
            if (player.GetData(EntityData.PLAYER_PLAYING) != null) {
                // Disconnect from the server
                player.ResetData(EntityData.PLAYER_PLAYING);

                // Remove opened ticket
                adminTicketList.RemoveAll(ticket => ticket.playerId == player.Value);

                // Other classes' disconnect function
                Chat.OnPlayerDisconnected(player, type, reason);
                DrivingSchool.OnPlayerDisconnected(player, type, reason);
                FastFood.OnPlayerDisconnected(player, type, reason);
                Fishing.OnPlayerDisconnected(player, type, reason);
                Garbage.OnPlayerDisconnected(player, type, reason);
                Hooker.OnPlayerDisconnected(player, type, reason);
                Thief.OnPlayerDisconnected(player);
                Vehicles.OnPlayerDisconnected(player);
                Weapons.OnPlayerDisconnected(player);

                // Save the character's data
                Character.SaveCharacterData(player);

                // Warn the players near to the disconnected one
                var message = string.Format(InfoRes.player_disconnected, player.Name, reason);
                Chat.SendMessageToNearbyPlayers(player, message, Constants.MESSAGE_DISCONNECT, 10.0f);
            }
        }

        [RemoteEvent("checkPlayerEventKeyStopAnim")]
        public void CheckPlayerEventKeyStopAnimEvent(Client player) {
            if (player.GetData(EntityData.PLAYER_ANIMATION) == null && player.GetData(EntityData.PLAYER_KILLED) == 0)
                player.StopAnimation();
        }

        [RemoteEvent("checkPlayerInventoryKey")]
        public void CheckPlayerInventoryKeyEvent(Client player) {
            if (GetPlayerInventoryTotal(player) > 0) {
                var inventory = GetPlayerInventory(player);
                player.TriggerEvent("showPlayerInventory", NAPI.Util.ToJson(inventory),
                    Constants.INVENTORY_TARGET_SELF);
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_items_inventory);
            }
        }

        [RemoteEvent("checkPlayerEventKey")]
        public void CheckPlayerEventKeyEvent(Client player) {
            if (player.GetData(EntityData.PLAYER_PLAYING) != null) {
                // Check if the player's close to an ATM
                for (var i = 0; i < Constants.ATM_LIST.Count; i++)
                    if (player.Position.DistanceTo(Constants.ATM_LIST[i]) <= 1.5f) {
                        player.TriggerEvent("showATM");
                        return;
                    }

                // Check if the player's in any business
                foreach (var business in Business.businessList)
                    if (player.Position.DistanceTo(business.position) <= 1.5f &&
                        player.Dimension == business.dimension) {
                        if (!Business.HasPlayerBusinessKeys(player, business) && business.locked) {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.business_locked);
                        }
                        else {
                            NAPI.World.RequestIpl(business.ipl);
                            player.Position = Business.GetBusinessExitPoint(business.ipl);
                            player.Dimension = Convert.ToUInt32(business.id);
                            player.SetData(EntityData.PLAYER_IPL, business.ipl);
                            player.SetData(EntityData.PLAYER_BUSINESS_ENTERED, business.id);
                        }

                        return;
                    }
                    else if (player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) == business.id) {
                        var exitPosition = Business.GetBusinessExitPoint(business.ipl);
                        if (player.Position.DistanceTo(exitPosition) < 2.5f) {
                            if (!Business.HasPlayerBusinessKeys(player, business) && business.locked) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.business_locked);
                            }
                            else if (player.GetData(EntityData.PLAYER_ROBBERY_START) != null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.stealing_progress);
                            }
                            else {
                                player.Position = business.position;
                                player.Dimension = business.dimension;
                                player.SetData(EntityData.PLAYER_BUSINESS_ENTERED, 0);
                                player.ResetData(EntityData.PLAYER_IPL);

                                foreach (var target in NAPI.Pools.GetAllPlayers())
                                    if (target.GetData(EntityData.PLAYER_PLAYING) != null &&
                                        target.GetData(EntityData.PLAYER_IPL) != null && target != player)
                                        if (target.GetData(EntityData.PLAYER_IPL) == business.ipl)
                                            return;
                                NAPI.World.RemoveIpl(business.ipl);
                            }
                        }

                        return;
                    }

                // Check if the player's in any house
                foreach (var house in House.houseList)
                    if (player.Position.DistanceTo(house.position) <= 1.5f && player.Dimension == house.dimension) {
                        if (!House.HasPlayerHouseKeys(player, house) && house.locked) {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.house_locked);
                        }
                        else {
                            NAPI.World.RequestIpl(house.ipl);
                            player.Position = House.GetHouseExitPoint(house.ipl);
                            player.Dimension = Convert.ToUInt32(house.id);
                            player.SetData(EntityData.PLAYER_IPL, house.ipl);
                            player.SetData(EntityData.PLAYER_HOUSE_ENTERED, house.id);
                        }

                        return;
                    }
                    else if (player.GetData(EntityData.PLAYER_HOUSE_ENTERED) == house.id) {
                        var exitPosition = House.GetHouseExitPoint(house.ipl);
                        if (player.Position.DistanceTo(exitPosition) < 2.5f) {
                            if (!House.HasPlayerHouseKeys(player, house) && house.locked) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.house_locked);
                            }
                            else if (player.GetData(EntityData.PLAYER_ROBBERY_START) != null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.stealing_progress);
                            }
                            else {
                                player.Position = house.position;
                                player.Dimension = house.dimension;
                                player.SetData(EntityData.PLAYER_HOUSE_ENTERED, 0);
                                player.ResetData(EntityData.PLAYER_IPL);

                                foreach (var target in NAPI.Pools.GetAllPlayers())
                                    if (target.GetData(EntityData.PLAYER_PLAYING) != null &&
                                        target.GetData(EntityData.PLAYER_IPL) != null && target != player)
                                        if (target.GetData(EntityData.PLAYER_IPL) == house.ipl)
                                            return;
                                NAPI.World.RemoveIpl(house.ipl);
                            }
                        }

                        return;
                    }

                // Check if the player's in any interior
                foreach (var interior in Constants.INTERIOR_LIST)
                    if (player.Position.DistanceTo(interior.entrancePosition) < 1.5f) {
                        NAPI.World.RequestIpl(interior.iplName);
                        player.Position = interior.exitPosition;
                        return;
                    }
                    else if (player.Position.DistanceTo(interior.exitPosition) < 1.5f) {
                        player.Position = interior.entrancePosition;
                        return;
                    }
            }
            else {
                var lobbyExit = new Vector3(404.6483f, -997.2951f, -99.00404f);

                if (lobbyExit.DistanceTo(player.Position) < 1.75f) {
                    // Player must have a character selected
                    if (player.GetData(EntityData.PLAYER_SQL_ID) == null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_character_selected);
                    }
                    else {
                        int playerSqlId = player.GetData(EntityData.PLAYER_SQL_ID);
                        var rightHand = GetItemInEntity(playerSqlId, Constants.ITEM_ENTITY_RIGHT_HAND);
                        var leftHand = GetItemInEntity(playerSqlId, Constants.ITEM_ENTITY_LEFT_HAND);

                        // Give the weapons to the player
                        Weapons.GivePlayerWeaponItems(player);

                        if (rightHand != null) {
                            var businessItem = Business.GetBusinessItemFromHash(rightHand.hash);
                            var weapon = NAPI.Util.WeaponNameToModel(rightHand.hash);

                            if (weapon != 0) {
                                player.GiveWeapon(weapon, rightHand.amount);

                                // Create the attachment
                                var attachment = new AttachmentModel(rightHand.id, rightHand.hash, "IK_R_Hand",
                                    new Vector3(), new Vector3());
                                player.SetSharedData(EntityData.PLAYER_RIGHT_HAND, NAPI.Util.ToJson(attachment));
                            }
                            else {
                                // Give the item to the player
                                player.GiveWeapon(WeaponHash.Unarmed, 1);
                                AttachItemToPlayer(player, rightHand.id, rightHand.hash, "IK_R_Hand",
                                    businessItem.position, businessItem.rotation, EntityData.PLAYER_RIGHT_HAND);
                            }
                        }

                        if (leftHand != null) {
                            var businessItem = Business.GetBusinessItemFromHash(leftHand.hash);
                            AttachItemToPlayer(player, leftHand.id, leftHand.hash, "IK_L_Hand", businessItem.position,
                                businessItem.rotation, EntityData.PLAYER_LEFT_HAND);
                            player.SetSharedData(EntityData.PLAYER_LEFT_HAND, leftHand.id);
                        }

                        // Calculate spawn dimension
                        if (player.GetData(EntityData.PLAYER_HOUSE_ENTERED) > 0) {
                            int houseId = player.GetData(EntityData.PLAYER_HOUSE_ENTERED);
                            var house = House.GetHouseById(houseId);
                            player.Dimension = Convert.ToUInt32(house.id);
                            NAPI.World.RequestIpl(house.ipl);
                        }
                        else if (player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) > 0) {
                            int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
                            var business = Business.GetBusinessById(businessId);
                            player.Dimension = Convert.ToUInt32(business.id);
                            NAPI.World.RequestIpl(business.ipl);
                        }
                        else {
                            player.Dimension = 0;
                        }

                        // Spawn the player into the world
                        player.Name = player.GetData(EntityData.PLAYER_NAME);
                        player.Position = player.GetData(EntityData.PLAYER_SPAWN_POS);
                        player.Rotation = player.GetData(EntityData.PLAYER_SPAWN_ROT);
                        player.Health = player.GetData(EntityData.PLAYER_HEALTH);
                        player.Armor = player.GetData(EntityData.PLAYER_ARMOR);

                        if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                            Vector3 deathPosition = null;
                            var deathPlace = string.Empty;
                            var deathHour = DateTime.Now.ToString("h:mm:ss tt");

                            if (player.GetData(EntityData.PLAYER_HOUSE_ENTERED) > 0) {
                                int houseId = player.GetData(EntityData.PLAYER_HOUSE_ENTERED);
                                var house = House.GetHouseById(houseId);
                                deathPosition = house.position;
                                deathPlace = house.name;
                            }
                            else if (player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) > 0) {
                                int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
                                var business = Business.GetBusinessById(businessId);
                                deathPosition = business.position;
                                deathPlace = business.name;
                            }
                            else {
                                deathPosition = player.Position;
                            }

                            // Creamos the report for the emergency department
                            var factionWarning = new FactionWarningModel(Constants.FACTION_EMERGENCY, player.Value,
                                deathPlace, deathPosition, -1, deathHour);
                            Faction.factionWarningList.Add(factionWarning);

                            var warnMessage = string.Format(InfoRes.emergency_warning,
                                Faction.factionWarningList.Count - 1);

                            foreach (var target in NAPI.Pools.GetAllPlayers())
                                if (target.GetData(EntityData.PLAYER_FACTION) == Constants.FACTION_EMERGENCY &&
                                    target.GetData(EntityData.PLAYER_ON_DUTY) == 0)
                                    target.SendChatMessage(Constants.COLOR_INFO + warnMessage);

                            player.Invincible = true;
                            player.SetData(EntityData.TIME_HOSPITAL_RESPAWN, GetTotalSeconds() + 240);
                            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.emergency_warn);
                        }

                        // Toggle connection flag
                        player.SetData(EntityData.PLAYER_PLAYING, true);
                        player.TriggerEvent("playerLoggedIn");
                    }
                }
                else if (player.Position.DistanceTo(new Vector3(401.8016f, -1001.897f, -99.00404f)) < 1.5f) {
                    Task.Factory.StartNew(() => {
                        // Show character menu
                        var playerList = Database.GetAccountCharacters(player.SocialClubName);
                        player.TriggerEvent("showPlayerCharacters", NAPI.Util.ToJson(playerList));
                    });
                }
            }
        }

        [RemoteEvent("processMenuAction")]
        public void ProcessMenuActionEvent(Client player, int itemId, string action) {
            var message = string.Empty;
            var item = GetItemModelFromId(itemId);
            var businessItem = Business.GetBusinessItemFromHash(item.hash);

            switch (action.ToLower()) {
                case Commands.COM_CONSUME:
                    // Consume the selected item
                    ConsumeItem(player, item, businessItem, false);

                    break;
                case Commands.ARG_OPEN:
                    switch (item.hash) {
                        case Constants.ITEM_HASH_PACK_BEER_AM:
                            ItemModel itemModel = GetPlayerItemModelFromHash(player.GetData(EntityData.PLAYER_SQL_ID),
                                Constants.ITEM_HASH_BOTTLE_BEER_AM);
                            if (itemModel == null) {
                                // Create the item
                                itemModel = new ItemModel();
                                {
                                    itemModel.hash = Constants.ITEM_HASH_BOTTLE_BEER_AM;
                                    itemModel.ownerEntity = Constants.ITEM_ENTITY_PLAYER;
                                    itemModel.ownerIdentifier = player.GetData(EntityData.PLAYER_SQL_ID);
                                    itemModel.amount = Constants.ITEM_OPEN_BEER_AMOUNT;
                                    itemModel.position = new Vector3(0.0f, 0.0f, 0.0f);
                                    itemModel.dimension = player.Dimension;
                                }

                                Task.Factory.StartNew(() => {
                                    // Create the new item
                                    itemModel.id = Database.AddNewItem(itemModel);
                                    itemList.Add(itemModel);
                                });
                            }
                            else {
                                // Add the amount to the current item
                                itemModel.amount += Constants.ITEM_OPEN_BEER_AMOUNT;


                                Task.Factory.StartNew(() => {
                                    // Update the amount into the database
                                    Database.UpdateItem(item);
                                });
                            }

                            break;
                    }

                    // Substract container amount
                    SubstractPlayerItems(item);

                    message = string.Format(InfoRes.player_inventory_open, businessItem.description.ToLower());
                    player.SendChatMessage(Constants.COLOR_INFO + message);

                    // Update the inventory
                    UpdateInventory(player, item, businessItem, Constants.INVENTORY_TARGET_SELF);

                    break;
                case Commands.ARG_EQUIP:
                    if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.right_hand_occupied);
                    }
                    else {
                        // Set the item into the hand
                        item.ownerEntity = Constants.ITEM_ENTITY_RIGHT_HAND;
                        AttachItemToPlayer(player, item.id, item.hash, "IK_R_Hand", businessItem.position,
                            businessItem.rotation, EntityData.PLAYER_RIGHT_HAND);

                        message = string.Format(InfoRes.player_inventory_equip, businessItem.description.ToLower());
                        player.SendChatMessage(Constants.COLOR_INFO + message);
                    }

                    break;
                case Commands.COM_DROP:
                    // Drop the item from the inventory
                    DropItem(player, item, businessItem, false);

                    break;
                case Commands.ARG_CONFISCATE:
                    Client target = player.GetData(EntityData.PLAYER_SEARCHED_TARGET);

                    // Transfer the item from the target to the player
                    item.ownerEntity = Constants.ITEM_ENTITY_PLAYER;
                    item.ownerIdentifier = player.GetData(EntityData.PLAYER_SQL_ID);

                    Task.Factory.StartNew(() => {
                        // Update the amount into the database
                        Database.UpdateItem(item);
                    });

                    var playerMessage = string.Format(InfoRes.police_retired_items_to, target.Name);
                    var targetMessage = string.Format(InfoRes.police_retired_items_from, player.Name);
                    player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                    target.SendChatMessage(Constants.COLOR_INFO + targetMessage);

                    // Update the inventory
                    UpdateInventory(player, item, businessItem, Constants.INVENTORY_TARGET_PLAYER);

                    break;
                case Commands.ARG_STORE:
                    Vehicle targetVehicle = player.GetData(EntityData.PLAYER_OPENED_TRUNK);

                    // Transfer the item from the player to the vehicle
                    item.ownerEntity = Constants.ITEM_ENTITY_VEHICLE;
                    item.ownerIdentifier = targetVehicle.GetData(EntityData.VEHICLE_ID);

                    // Remove the weapon if it's a weapon
                    foreach (var weapon in player.Weapons)
                        if (weapon.ToString() == item.hash) {
                            player.RemoveWeapon(weapon);
                            break;
                        }

                    Task.Factory.StartNew(() => {
                        // Update the amount into the database
                        Database.UpdateItem(item);
                    });

                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.trunk_stored_items);

                    // Update the inventory
                    UpdateInventory(player, item, businessItem, Constants.INVENTORY_TARGET_VEHICLE_PLAYER);

                    break;
                case Commands.ARG_WITHDRAW:
                    Vehicle sourceVehicle = player.GetData(EntityData.PLAYER_OPENED_TRUNK);

                    var weaponHash = NAPI.Util.WeaponNameToModel(item.hash);

                    if (weaponHash != 0) {
                        // Give the weapon to the player
                        item.ownerEntity = Constants.ITEM_ENTITY_WHEEL;
                        player.GiveWeapon(weaponHash, 0);
                        player.SetWeaponAmmo(weaponHash, item.amount);
                    }
                    else {
                        // Place the item into the inventory
                        item.ownerEntity = Constants.ITEM_ENTITY_PLAYER;
                    }

                    // Transfer the item from the vehicle to the player
                    item.ownerIdentifier = player.GetData(EntityData.PLAYER_SQL_ID);

                    Task.Factory.StartNew(() => {
                        // Update the amount into the database
                        Database.UpdateItem(item);
                    });

                    Chat.SendMessageToNearbyPlayers(player, InfoRes.trunk_item_withdraw, Constants.MESSAGE_ME, 20.0f);
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.trunk_withdraw_items);

                    // Update the inventory
                    UpdateInventory(player, item, businessItem, Constants.INVENTORY_TARGET_VEHICLE_TRUNK);

                    break;
            }
        }

        [RemoteEvent("closeVehicleTrunk")]
        public void CloseVehicleTrunkEvent(Client player) {
            Vehicle vehicle = player.GetData(EntityData.PLAYER_OPENED_TRUNK);
            vehicle.CloseDoor(Constants.VEHICLE_TRUNK);
            player.ResetData(EntityData.PLAYER_OPENED_TRUNK);
        }

        [RemoteEvent("getPlayerTattoos")]
        public void GetPlayerTattoosEvent(Client player, Client targetPlayer) {
            int targetId = targetPlayer.GetData(EntityData.PLAYER_SQL_ID);
            var playerTattooList = tattooList.Where(t => t.player == targetId).ToList();
            player.TriggerEvent("updatePlayerTattoos", NAPI.Util.ToJson(playerTattooList), targetPlayer);
        }

        [RemoteEvent("closeInventory")]
        public void CloseInventoryEvent(Client player) {
            // Reset the variables related
            player.ResetData(EntityData.PLAYER_OPENED_TRUNK);
            player.ResetData(EntityData.PLAYER_SEARCHED_TARGET);
        }

        [Command(Commands.COM_STORE)]
        public void StoreCommand(Client player) {
            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null)
                StoreItemOnHand(player);
            else
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.right_hand_empty);
        }

        [Command(Commands.COM_CONSUME)]
        public void ConsumeCommand(Client player) {
            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
                var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;
                var item = GetItemModelFromId(itemId);
                var businessItem = Business.GetBusinessItemFromHash(item.hash);

                // Check if it's consumable
                if (businessItem.type == Constants.ITEM_TYPE_CONSUMABLE)
                    ConsumeItem(player, item, businessItem, true);
                else
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.item_not_consumable);
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.right_hand_empty);
            }
        }

        [Command(Commands.COM_INVENTORY)]
        public void InventoryCommand(Client player) {
            if (GetPlayerInventoryTotal(player) > 0) {
                var inventory = GetPlayerInventory(player);
                player.TriggerEvent("showPlayerInventory", NAPI.Util.ToJson(inventory),
                    Constants.INVENTORY_TARGET_SELF);
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_items_inventory);
            }
        }

        [Command(Commands.COM_PURCHASE)]
        public void PurchaseCommand(Client player, int amount = 0) {
            // Check if the player is inside a business
            if (player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) > 0) {
                int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
                var business = Business.GetBusinessById(businessId);
                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                int playerSex = player.GetData(EntityData.PLAYER_SEX);

                switch (business.type) {
                    case Constants.BUSINESS_TYPE_CLOTHES:
                        // Get the clothes on the character
                        var clothes = GetPlayerClothes(playerId);

                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.about_complements);
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.for_avoid_clipping1);
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.for_avoid_clipping2);
                        player.TriggerEvent("showClothesBusinessPurchaseMenu", business.name, business.multiplier);
                        break;
                    case Constants.BUSINESS_TYPE_BARBER_SHOP:
                        // Load the players skin model
                        string skinModel = NAPI.Util.ToJson(player.GetData(EntityData.PLAYER_SKIN_MODEL));
                        player.TriggerEvent("showHairdresserMenu", playerSex, skinModel, business.name);
                        break;
                    case Constants.BUSINESS_TYPE_TATTOO_SHOP:
                        // Remove player's clothes
                        player.SetClothes(11, 15, 0);
                        player.SetClothes(3, 15, 0);
                        player.SetClothes(8, 15, 0);

                        if (playerSex == 0) {
                            player.SetClothes(4, 61, 0);
                            player.SetClothes(6, 34, 0);
                        }
                        else {
                            player.SetClothes(4, 15, 0);
                            player.SetClothes(6, 35, 0);
                        }

                        // Load tattoo list
                        var tattooList = Globals.tattooList.Where(t => t.player == playerId).ToList();
                        player.TriggerEvent("showTattooMenu", player.GetData(EntityData.PLAYER_SEX),
                            NAPI.Util.ToJson(tattooList), NAPI.Util.ToJson(Constants.TATTOO_LIST), business.name,
                            business.multiplier);

                        break;
                    default:
                        var businessItems = Business.GetBusinessSoldItems(business.type);

                        if (businessItems.Count > 0)
                            player.TriggerEvent("showBusinessPurchaseMenu", NAPI.Util.ToJson(businessItems),
                                business.name, business.multiplier);

                        break;
                }
            }
            else {
                // Get all the houses
                foreach (var house in House.houseList)
                    if (player.Position.DistanceTo(house.position) <= 1.5f && player.Dimension == house.dimension) {
                        House.BuyHouse(player, house);
                        return;
                    }

                // Check if the player's in the scrapyard
                foreach (var parking in Parking.parkingList)
                    if (player.Position.DistanceTo(parking.position) < 2.5f &&
                        parking.type == Constants.PARKING_TYPE_SCRAPYARD) {
                        if (amount > 0) {
                            int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);
                            if (playerMoney >= amount) {
                                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                                var item = GetPlayerItemModelFromHash(playerId, Constants.ITEM_HASH_BUSINESS_PRODUCTS);

                                if (item == null) {
                                    item = new ItemModel();
                                    {
                                        item.amount = amount;
                                        item.dimension = 0;
                                        item.position = new Vector3(0.0f, 0.0f, 0.0f);
                                        item.hash = Constants.ITEM_HASH_BUSINESS_PRODUCTS;
                                        item.ownerEntity = Constants.ITEM_ENTITY_PLAYER;
                                        item.ownerIdentifier = playerId;
                                        item.objectHandle = null;
                                    }

                                    Task.Factory.StartNew(() => {
                                        // Add the item into the database
                                        item.id = Database.AddNewItem(item);
                                        itemList.Add(item);
                                    });
                                }
                                else {
                                    item.amount += amount;

                                    Task.Factory.StartNew(() => {
                                        // Update the amount into the database
                                        Database.UpdateItem(item);
                                    });
                                }

                                player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney - amount);

                                var message = string.Format(InfoRes.products_bought, amount, amount);
                                player.SendChatMessage(Constants.COLOR_INFO + message);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_COMMAND_PURCHASE);
                        }

                        return;
                    }
            }
        }

        [Command(Commands.COM_SELL, Commands.HLP_SELL_COMMAND, GreedyArg = true)]
        public void SellCommand(Client player, string args) {
            var arguments = args.Split(' ');
            var price = 0;
            var targetId = 0;
            var objectId = 0;
            Client target = null;
            var priceString = string.Empty;
            if (arguments.Length > 0)
                switch (arguments[0].ToLower()) {
                    case Commands.ARG_VEHICLE:
                        if (arguments.Length > 3) {
                            if (int.TryParse(arguments[2], out targetId)) {
                                target = GetPlayerById(targetId);
                                priceString = arguments[3];
                            }
                            else if (arguments.Length == 5) {
                                target = NAPI.Player.GetPlayerFromName(arguments[2] + " " + arguments[3]);
                                priceString = arguments[4];
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_SELL_VEH_COMMAND);
                                return;
                            }

                            if (int.TryParse(priceString, out price)) {
                                if (price > 0) {
                                    if (int.TryParse(arguments[1], out objectId)) {
                                        var vehicle = Vehicles.GetVehicleById(objectId);

                                        if (vehicle == null) {
                                            var vehModel = Parking.GetParkedVehicleById(objectId);

                                            if (vehModel != null) {
                                                if (vehModel.owner == player.Name) {
                                                    var playerString = string.Format(InfoRes.vehicle_sell,
                                                        vehModel.model, target.Name, price);
                                                    var targetString = string.Format(InfoRes.vehicle_sold, player.Name,
                                                        vehModel.model, price);

                                                    target.SetData(EntityData.PLAYER_JOB_PARTNER, player);
                                                    target.SetData(EntityData.PLAYER_SELLING_PRICE, price);
                                                    target.SetData(EntityData.PLAYER_SELLING_HOUSE, objectId);

                                                    player.SendChatMessage(Constants.COLOR_INFO + playerString);
                                                    target.SendChatMessage(Constants.COLOR_INFO + targetString);
                                                }
                                                else {
                                                    player.SendChatMessage(
                                                        Constants.COLOR_ERROR + ErrRes.player_not_veh_owner);
                                                }
                                            }
                                            else {
                                                player.SendChatMessage(
                                                    Constants.COLOR_ERROR + ErrRes.vehicle_not_exists);
                                            }
                                        }
                                        else {
                                            foreach (var veh in NAPI.Pools.GetAllVehicles())
                                                if (veh.GetData(EntityData.VEHICLE_ID) == objectId) {
                                                    if (vehicle.GetData(EntityData.VEHICLE_OWNER) == player.Name) {
                                                        string vehicleModel = vehicle.GetData(EntityData.VEHICLE_MODEL);
                                                        var playerString = string.Format(InfoRes.vehicle_sell,
                                                            vehicleModel, target.Name, price);
                                                        var targetString = string.Format(InfoRes.vehicle_sold,
                                                            player.Name, vehicleModel, price);

                                                        target.SetData(EntityData.PLAYER_JOB_PARTNER, player);
                                                        target.SetData(EntityData.PLAYER_SELLING_PRICE, price);
                                                        target.SetData(EntityData.PLAYER_SELLING_VEHICLE, objectId);

                                                        player.SendChatMessage(Constants.COLOR_INFO + playerString);
                                                        target.SendChatMessage(Constants.COLOR_INFO + targetString);
                                                    }
                                                    else {
                                                        player.SendChatMessage(
                                                            Constants.COLOR_ERROR + ErrRes.player_not_veh_owner);
                                                    }

                                                    break;
                                                }
                                        }
                                    }
                                    else {
                                        player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_SELL_VEH_COMMAND);
                                    }
                                }
                                else {
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.price_positive);
                                }
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_SELL_VEH_COMMAND);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_SELL_VEH_COMMAND);
                        }

                        break;
                    case Commands.ARG_HOUSE:
                        if (arguments.Length < 2) {
                            player.SendChatMessage(Constants.COLOR_ERROR + Commands.HLP_SELL_HOUSE_COMMAND);
                        }
                        else {
                            if (int.TryParse(arguments[1], out objectId)) {
                                var house = House.GetHouseById(objectId);
                                if (house != null) {
                                    if (house.owner == player.Name) {
                                        foreach (var rndPlayer in NAPI.Pools.GetAllPlayers())
                                            if (rndPlayer.GetData(EntityData.PLAYER_PLAYING) != null &&
                                                rndPlayer.GetData(EntityData.PLAYER_HOUSE_ENTERED) == house.id) {
                                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.house_occupied);
                                                return;
                                            }

                                        if (arguments.Length == 2) {
                                            var sellValue = (int) Math.Round(house.price * 0.7);
                                            var playerString = string.Format(InfoRes.house_sell_state, sellValue);
                                            player.SetData(EntityData.PLAYER_SELLING_HOUSE_STATE, objectId);
                                            player.SendChatMessage(Constants.COLOR_INFO + playerString);
                                        }
                                        else {
                                            if (int.TryParse(arguments[2], out targetId)) {
                                                target = GetPlayerById(targetId);
                                                priceString = arguments[3];
                                            }
                                            else if (arguments.Length == 5) {
                                                target = NAPI.Player.GetPlayerFromName(
                                                    arguments[2] + " " + arguments[3]);
                                                priceString = arguments[4];
                                            }
                                            else {
                                                player.SendChatMessage(
                                                    Constants.COLOR_HELP + Commands.HLP_SELL_HOUSE_COMMAND);
                                                return;
                                            }

                                            if (int.TryParse(priceString, out price)) {
                                                if (price > 0) {
                                                    var playerString = string.Format(InfoRes.house_sell, target.Name,
                                                        price);
                                                    var targetString = string.Format(InfoRes.house_sold, player.Name,
                                                        price);

                                                    target.SetData(EntityData.PLAYER_JOB_PARTNER, player);
                                                    target.SetData(EntityData.PLAYER_SELLING_PRICE, price);
                                                    target.SetData(EntityData.PLAYER_SELLING_HOUSE, objectId);

                                                    player.SendChatMessage(Constants.COLOR_INFO + playerString);
                                                    target.SendChatMessage(Constants.COLOR_INFO + targetString);
                                                }
                                                else {
                                                    player.SendChatMessage(
                                                        Constants.COLOR_ERROR + ErrRes.price_positive);
                                                }
                                            }
                                            else {
                                                player.SendChatMessage(
                                                    Constants.COLOR_HELP + Commands.HLP_SELL_VEH_COMMAND);
                                            }
                                        }
                                    }
                                    else {
                                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_house_owner);
                                    }
                                }
                                else {
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.house_not_exists);
                                }
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + Commands.HLP_SELL_HOUSE_COMMAND);
                            }
                        }

                        break;
                    case Commands.ARG_WEAPON:
                        // Pending TODO
                        break;
                    case Commands.ARG_FISH:
                        if (player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) > 0) {
                            int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
                            var business = Business.GetBusinessById(businessId);

                            if (business != null && business.type == Constants.BUSINESS_TYPE_FISHING) {
                                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                                var fishModel = GetPlayerItemModelFromHash(playerId, Constants.ITEM_HASH_FISH);

                                if (fishModel == null) {
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_fish_sellable);
                                }
                                else {
                                    int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);
                                    var amount = (int) Math.Round(fishModel.amount * Constants.PRICE_FISH / 1000.0);

                                    Task.Factory.StartNew(() => {
                                        // Remove the item from the database
                                        Database.RemoveItem(fishModel.id);
                                        itemList.Remove(fishModel);
                                    });

                                    player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney + amount);

                                    var message = string.Format(InfoRes.fishing_won, amount);
                                    player.SendChatMessage(Constants.COLOR_INFO + message);
                                }
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_fishing_business);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_fishing_business);
                        }

                        break;
                    default:
                        player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_SELL_COMMAND);
                        break;
                }
            else
                player.SendChatMessage(Constants.COLOR_ERROR + Commands.HLP_SELL_COMMAND);
        }

        [Command(Commands.COM_HELP)]
        public void HelpCommand(Client player) {
            player.SendChatMessage(Constants.COLOR_ERROR + "Command not implemented.");
            //player.TriggerEvent("helptext");
        }

        [Command(Commands.COM_WELCOME)]
        public void WelcomeCommand(Client player) {
            player.SendChatMessage(Constants.COLOR_ERROR + "Command not implemented.");
            //player.TriggerEvent("welcomeHelp");
        }

        [Command(Commands.COM_SHOW, Commands.HLP_SHOW_DOC_COMMAND)]
        public void ShowCommand(Client player, string targetString, string documentation) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                string message;
                var currentLicense = 0;
                string nameChar = player.GetData(EntityData.PLAYER_NAME);
                int age = player.GetData(EntityData.PLAYER_AGE);
                var sexDescription = player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_MALE
                    ? GenRes.sex_male
                    : GenRes.sex_female;

                var target = int.TryParse(targetString, out var targetId)
                    ? GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);

                switch (documentation.ToLower()) {
                    case Commands.ARG_LICENSES:
                        string licenseMessage;
                        string playerLicenses = player.GetData(EntityData.PLAYER_LICENSES);
                        var playerLicensesArray = playerLicenses.Split(',');

                        message = string.Format(InfoRes.licenses_show, target.Name);
                        Chat.SendMessageToNearbyPlayers(player, message, Constants.MESSAGE_ME, 20.0f);

                        foreach (var license in playerLicensesArray) {
                            var currentLicenseStatus = int.Parse(license);
                            switch (currentLicense) {
                                case Constants.LICENSE_CAR:
                                    switch (currentLicenseStatus) {
                                        case -1:
                                            target.SendChatMessage(
                                                Constants.COLOR_HELP + InfoRes.car_license_not_available);
                                            break;
                                        case 0:
                                            target.SendChatMessage(
                                                Constants.COLOR_HELP + InfoRes.car_license_practical_pending);
                                            break;
                                        default:
                                            licenseMessage = string.Format(InfoRes.car_license_points,
                                                currentLicenseStatus);
                                            target.SendChatMessage(Constants.COLOR_HELP + licenseMessage);
                                            break;
                                    }

                                    break;
                                case Constants.LICENSE_MOTORCYCLE:
                                    switch (currentLicenseStatus) {
                                        case -1:
                                            target.SendChatMessage(
                                                Constants.COLOR_HELP + InfoRes.motorcycle_license_not_available);
                                            break;
                                        case 0:
                                            target.SendChatMessage(
                                                Constants.COLOR_HELP + InfoRes.motorcycle_license_practical_pending);
                                            break;
                                        default:
                                            licenseMessage = string.Format(InfoRes.motorcycle_license_points,
                                                currentLicenseStatus);
                                            target.SendChatMessage(Constants.COLOR_HELP + licenseMessage);
                                            break;
                                    }

                                    break;
                                case Constants.LICENSE_TAXI:
                                    if (currentLicenseStatus == -1)
                                        target.SendChatMessage(
                                            Constants.COLOR_HELP + InfoRes.taxi_license_not_available);
                                    else
                                        target.SendChatMessage(Constants.COLOR_HELP + InfoRes.taxi_license_up_to_date);
                                    break;
                            }

                            currentLicense++;
                        }

                        break;
                    case Commands.ARG_INSURANCE:
                        int playerMedicalInsurance = player.GetData(EntityData.PLAYER_MEDICAL_INSURANCE);
                        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        dateTime = dateTime.AddSeconds(playerMedicalInsurance);

                        if (playerMedicalInsurance > 0) {
                            message = string.Format(InfoRes.insurance_show, target.Name);
                            Chat.SendMessageToNearbyPlayers(player, message, Constants.MESSAGE_ME, 20.0f);

                            target.SendChatMessage(Constants.COLOR_INFO + GenRes.name + nameChar);
                            target.SendChatMessage(Constants.COLOR_INFO + GenRes.age + age);
                            target.SendChatMessage(Constants.COLOR_INFO + GenRes.sex + sexDescription);
                            target.SendChatMessage(Constants.COLOR_INFO + GenRes.expiry + dateTime.ToShortDateString());
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_medical_insurance);
                        }

                        break;
                    case Commands.ARG_IDENTIFICATION:
                        int playerDocumentation = player.GetData(EntityData.PLAYER_DOCUMENTATION);
                        if (playerDocumentation > 0) {
                            message = string.Format(InfoRes.identification_show, target.Name);
                            Chat.SendMessageToNearbyPlayers(player, message, Constants.MESSAGE_ME, 20.0f);

                            target.SendChatMessage(Constants.COLOR_INFO + GenRes.name + nameChar);
                            target.SendChatMessage(Constants.COLOR_INFO + GenRes.age + age);
                            target.SendChatMessage(Constants.COLOR_INFO + GenRes.sex + sexDescription);
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_undocumented);
                        }

                        break;
                }
            }
        }

        [Command(Commands.COM_PAY, Commands.HLP_PAY_COMMAND)]
        public void PayCommand(Client player, string targetString, int price) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                var target = int.TryParse(targetString, out var targetId)
                    ? GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);
                if (target == player) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.hooker_offered_himself);
                }
                else {
                    target.SetData(EntityData.PLAYER_PAYMENT, player);
                    target.SetData(EntityData.JOB_OFFER_PRICE, price);

                    var playerMessage = string.Format(InfoRes.payment_offer, price, target.Name);
                    var targetMessage = string.Format(InfoRes.payment_received, player.Name, price);
                    player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                    target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                }
            }
        }

        [Command(Commands.COM_GIVE, Commands.HLP_GIVE_COMMAND)]
        public void GiveCommand(Client player, string targetString) {
            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                var target = int.TryParse(targetString, out var targetId)
                    ? GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);

                if (target == null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                }
                else if (player.Position.DistanceTo(target.Position) > 2.0f) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                }
                else if (target.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.target_right_hand_not_empty);
                }
                else {
                    var playerMessage = string.Empty;
                    var targetMessage = string.Empty;

                    string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
                    var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;
                    var item = GetItemModelFromId(itemId);

                    // Check if it's a weapon
                    var weaponHash = NAPI.Util.WeaponNameToModel(item.hash);

                    if (weaponHash != 0) {
                        target.GiveWeapon(weaponHash, 0);
                        target.SetWeaponAmmo(weaponHash, item.amount);
                        target.RemoveWeapon(weaponHash);

                        playerMessage = string.Format(InfoRes.item_given, item.hash.ToLower(), target.Name);
                        targetMessage = string.Format(InfoRes.item_received, player.Name, item.hash.ToLower());
                    }
                    else {
                        var businessItem = Business.GetBusinessItemFromHash(item.hash);
                        item.objectHandle.Detach();
                        item.objectHandle.AttachTo(target, "PH_R_Hand", businessItem.position, businessItem.rotation);

                        playerMessage = string.Format(InfoRes.item_given, businessItem.description.ToLower(),
                            target.Name);
                        targetMessage = string.Format(InfoRes.item_received, player.Name,
                            businessItem.description.ToLower());
                    }

                    // Change item's owner
                    player.ResetSharedData(EntityData.PLAYER_RIGHT_HAND);
                    target.SetSharedData(EntityData.PLAYER_RIGHT_HAND, item.id);
                    item.ownerIdentifier = target.GetData(EntityData.PLAYER_SQL_ID);

                    Task.Factory.StartNew(() => {
                        // Update the amount into the database
                        Database.UpdateItem(item);
                    });

                    player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                    target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                }
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.right_hand_empty);
            }
        }

        [Command(Commands.COM_CANCEL, Commands.HLP_GLOBALS_CANCEL_COMMAND)]
        public void CancelCommand(Client player, string cancel) {
            switch (cancel.ToLower()) {
                case Commands.ARG_INTERVIEW:
                    if (player.GetData(EntityData.PLAYER_ON_AIR) != null) {
                        player.ResetData(EntityData.PLAYER_ON_AIR);
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.on_air_canceled);
                    }

                    break;
                case Commands.ARG_SERVICE:
                    if (player.GetData(EntityData.PLAYER_ALREADY_FUCKING) == null) {
                        player.ResetData(EntityData.PLAYER_ALREADY_FUCKING);
                        player.ResetData(EntityData.PLAYER_JOB_PARTNER);
                        player.ResetData(EntityData.HOOKER_TYPE_SERVICE);
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.hooker_service_canceled);
                    }

                    break;
                case Commands.ARG_MONEY:
                    if (player.GetData(EntityData.PLAYER_PAYMENT) != null) {
                        player.ResetData(EntityData.PLAYER_PAYMENT);
                        player.ResetData(EntityData.PLAYER_JOB_PARTNER);
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.payment_canceled);
                    }

                    break;
                case Commands.ARG_ORDER:
                    if (player.GetData(EntityData.PLAYER_DELIVER_ORDER) != null) {
                        player.ResetData(EntityData.PLAYER_DELIVER_ORDER);
                        player.ResetData(EntityData.PLAYER_JOB_CHECKPOINT);
                        player.ResetData(EntityData.PLAYER_JOB_VEHICLE);
                        player.ResetData(EntityData.PLAYER_JOB_WON);

                        // Remove the checkpoints
                        player.TriggerEvent("fastFoodDeliverFinished");

                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.deliverer_order_canceled);
                    }

                    break;
                case Commands.ARG_REPAINT:
                    if (player.GetData(EntityData.PLAYER_REPAINT_VEHICLE) != null) {
                        // Get the mechanic and the vehicle
                        Client target = player.GetData(EntityData.PLAYER_JOB_PARTNER);
                        Vehicle vehicle = player.GetData(EntityData.PLAYER_REPAINT_VEHICLE);

                        // Get old colors
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
                            vehicle.CustomPrimaryColor = new Color(int.Parse(primaryColor[0]),
                                int.Parse(primaryColor[1]), int.Parse(primaryColor[2]));
                            vehicle.CustomSecondaryColor = new Color(int.Parse(secondaryColor[0]),
                                int.Parse(secondaryColor[1]), int.Parse(secondaryColor[2]));
                        }

                        player.ResetData(EntityData.PLAYER_JOB_PARTNER);
                        player.ResetData(EntityData.PLAYER_REPAINT_VEHICLE);
                        player.ResetData(EntityData.PLAYER_REPAINT_COLOR_TYPE);
                        player.ResetData(EntityData.PLAYER_REPAINT_FIRST_COLOR);
                        player.ResetData(EntityData.PLAYER_REPAINT_SECOND_COLOR);
                        player.ResetData(EntityData.JOB_OFFER_PRICE);

                        // Remove repaint window
                        target.TriggerEvent("closeRepaintWindow");

                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.repaint_canceled);
                    }

                    break;
                default:
                    player.SendChatMessage(Constants.COLOR_ERROR + Commands.HLP_GLOBALS_CANCEL_COMMAND);
                    break;
            }
        }

        [Command(Commands.COM_ACCEPT, Commands.HLP_GLOBALS_ACCEPT_COMMAND)]
        public void AcceptCommand(Client player, string accept) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0)
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            else
                switch (accept.ToLower()) {
                    case Commands.ARG_REPAIR:
                        if (player.GetData(EntityData.PLAYER_REPAIR_VEHICLE) != null) {
                            Client mechanic = player.GetData(EntityData.PLAYER_JOB_PARTNER);

                            if (mechanic != null && mechanic.Position.DistanceTo(player.Position) < 5.0f) {
                                int price = player.GetData(EntityData.JOB_OFFER_PRICE);
                                int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);

                                if (playerMoney >= price) {
                                    // Get the vehicle to repair and the broken part
                                    string type = player.GetData(EntityData.PLAYER_REPAIR_TYPE);
                                    Vehicle vehicle = player.GetData(EntityData.PLAYER_REPAIR_VEHICLE);

                                    int mechanicId = mechanic.GetData(EntityData.PLAYER_SQL_ID);
                                    int mechanicMoney = mechanic.GetSharedData(EntityData.PLAYER_MONEY);
                                    var item = GetPlayerItemModelFromHash(mechanicId,
                                        Constants.ITEM_HASH_BUSINESS_PRODUCTS);

                                    switch (type.ToLower()) {
                                        case Commands.ARG_CHASSIS:
                                            vehicle.Repair();
                                            break;
                                        case Commands.ARG_DOORS:
                                            for (var i = 0; i < 6; i++)
                                                if (vehicle.IsDoorBroken(i))
                                                    vehicle.FixDoor(i);
                                            break;
                                        case Commands.ARG_TYRES:
                                            for (var i = 0; i < 4; i++)
                                                if (vehicle.IsTyrePopped(i))
                                                    vehicle.FixTyre(i);
                                            break;
                                        case Commands.ARG_WINDOWS:
                                            for (var i = 0; i < 4; i++)
                                                if (vehicle.IsWindowBroken(i))
                                                    vehicle.FixWindow(i);
                                            break;
                                    }

                                    if (player != mechanic) {
                                        player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney - price);
                                        mechanic.SetSharedData(EntityData.PLAYER_MONEY, mechanicMoney + price);
                                    }

                                    item.amount -= player.GetData(EntityData.JOB_OFFER_PRODUCTS);

                                    if (item.amount == 0)
                                        Task.Factory.StartNew(() => {
                                            // Remove the item from the database
                                            Database.RemoveItem(item.id);
                                            itemList.Remove(item);
                                        });
                                    else
                                        Task.Factory.StartNew(() => {
                                            // Update the amount into the database
                                            Database.UpdateItem(item);
                                        });

                                    player.ResetData(EntityData.PLAYER_JOB_PARTNER);
                                    player.ResetData(EntityData.PLAYER_REPAIR_VEHICLE);
                                    player.ResetData(EntityData.PLAYER_REPAIR_TYPE);
                                    player.ResetData(EntityData.JOB_OFFER_PRODUCTS);
                                    player.ResetData(EntityData.JOB_OFFER_PRICE);

                                    var playerMessage = string.Format(InfoRes.vehicle_repaired_by, mechanic.Name,
                                        price);
                                    var mechanicMessage =
                                        string.Format(InfoRes.vehicle_repaired_by, player.Name, price);
                                    player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                    mechanic.SendChatMessage(Constants.COLOR_INFO + mechanicMessage);

                                    Task.Factory.StartNew(() => {
                                        // Save the log into the database
                                        Database.LogPayment(player.Name, mechanic.Name, Commands.COM_REPAIR, price);
                                    });
                                }
                                else {
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                                }
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_repair_offered);
                        }

                        break;
                    case Commands.ARG_REPAINT:
                        if (player.GetData(EntityData.PLAYER_REPAINT_VEHICLE) != null) {
                            Client mechanic = player.GetData(EntityData.PLAYER_JOB_PARTNER);

                            if (mechanic != null && mechanic.Position.DistanceTo(player.Position) < 5.0f) {
                                int price = player.GetData(EntityData.JOB_OFFER_PRICE);
                                int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);

                                if (playerMoney >= price) {
                                    Vehicle vehicle = player.GetData(EntityData.PLAYER_REPAINT_VEHICLE);
                                    int colorType = player.GetData(EntityData.PLAYER_REPAINT_COLOR_TYPE);
                                    string firstColor = player.GetData(EntityData.PLAYER_REPAINT_FIRST_COLOR);
                                    string secondColor = player.GetData(EntityData.PLAYER_REPAINT_SECOND_COLOR);
                                    int pearlescentColor = player.GetData(EntityData.PLAYER_REPAINT_PEARLESCENT);

                                    int mechanicId = mechanic.GetData(EntityData.PLAYER_SQL_ID);
                                    int mechanicMoney = mechanic.GetSharedData(EntityData.PLAYER_MONEY);
                                    var item = GetPlayerItemModelFromHash(mechanicId,
                                        Constants.ITEM_HASH_BUSINESS_PRODUCTS);

                                    // Repaint the vehicle
                                    vehicle.SetData(EntityData.VEHICLE_COLOR_TYPE, colorType);
                                    vehicle.SetData(EntityData.VEHICLE_FIRST_COLOR, firstColor);
                                    vehicle.SetData(EntityData.VEHICLE_SECOND_COLOR, secondColor);
                                    vehicle.SetData(EntityData.VEHICLE_PEARLESCENT_COLOR, pearlescentColor);

                                    // Update the vehicle's color
                                    var vehicleModel = new VehicleModel();
                                    {
                                        vehicleModel.id = vehicle.GetData(EntityData.VEHICLE_ID);
                                        vehicleModel.colorType = colorType;
                                        vehicleModel.firstColor = firstColor;
                                        vehicleModel.secondColor = secondColor;
                                        vehicleModel.pearlescent = pearlescentColor;
                                    }

                                    Task.Factory.StartNew(() => {
                                        // Update the vehicle's color into the database
                                        Database.UpdateVehicleColor(vehicleModel);
                                    });

                                    if (player != mechanic) {
                                        player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney - price);
                                        mechanic.SetSharedData(EntityData.PLAYER_MONEY, mechanicMoney + price);
                                    }

                                    item.amount -= player.GetData(EntityData.JOB_OFFER_PRODUCTS);

                                    if (item.amount == 0)
                                        Task.Factory.StartNew(() => {
                                            // Remove the item from the database
                                            Database.RemoveItem(item.id);
                                            itemList.Remove(item);
                                        });
                                    else
                                        Task.Factory.StartNew(() => {
                                            // Update the amount into the database
                                            Database.UpdateItem(item);
                                        });

                                    player.ResetData(EntityData.PLAYER_JOB_PARTNER);
                                    player.ResetData(EntityData.PLAYER_REPAINT_VEHICLE);
                                    player.ResetData(EntityData.PLAYER_REPAINT_COLOR_TYPE);
                                    player.ResetData(EntityData.PLAYER_REPAINT_FIRST_COLOR);
                                    player.ResetData(EntityData.PLAYER_REPAINT_SECOND_COLOR);
                                    player.ResetData(EntityData.JOB_OFFER_PRODUCTS);
                                    player.ResetData(EntityData.JOB_OFFER_PRICE);

                                    var playerMessage = string.Format(InfoRes.vehicle_repainted_by, mechanic.Name,
                                        price);
                                    var mechanicMessage =
                                        string.Format(InfoRes.vehicle_repainted_to, player.Name, price);
                                    player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                    mechanic.SendChatMessage(Constants.COLOR_INFO + mechanicMessage);

                                    // Remove repaint menu
                                    mechanic.TriggerEvent("closeRepaintWindow");

                                    Task.Factory.StartNew(() => {
                                        // Save the log into the database
                                        Database.LogPayment(player.Name, mechanic.Name, Commands.COM_REPAINT, price);
                                    });
                                }
                                else {
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                                }
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_repaint_offered);
                        }

                        break;
                    case Commands.ARG_SERVICE:

                        if (player.GetData(EntityData.HOOKER_TYPE_SERVICE) == null) {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_service_offered);
                        }
                        else if (player.GetData(EntityData.PLAYER_ALREADY_FUCKING) != null) {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.already_fucking);
                        }
                        else if (player.VehicleSeat != (int) VehicleSeat.Driver) {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_vehicle_driving);
                        }
                        else {
                            if (player.Vehicle.EngineStatus) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.engine_on);
                            }
                            else {
                                Client target = player.GetData(EntityData.PLAYER_JOB_PARTNER);
                                if (player.GetData(EntityData.HOOKER_TYPE_SERVICE) != null) {
                                    int amount = player.GetData(EntityData.JOB_OFFER_PRICE);
                                    int money = player.GetSharedData(EntityData.PLAYER_MONEY);

                                    if (target.GetData(EntityData.PLAYER_PLAYING) != null) {
                                        if (amount > money) {
                                            player.SendChatMessage(
                                                Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                                        }
                                        else {
                                            int targetMoney = target.GetSharedData(EntityData.PLAYER_MONEY);
                                            player.SetSharedData(EntityData.PLAYER_MONEY, money - amount);
                                            target.SetSharedData(EntityData.PLAYER_MONEY, targetMoney + amount);

                                            var playerMessage = string.Format(InfoRes.service_paid, amount);
                                            var targetMessage = string.Format(InfoRes.service_received, amount);
                                            player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                            target.SendChatMessage(Constants.COLOR_INFO + targetMessage);

                                            player.SetData(EntityData.PLAYER_ANIMATION, target);
                                            player.SetData(EntityData.PLAYER_ALREADY_FUCKING, target);
                                            target.SetData(EntityData.PLAYER_ALREADY_FUCKING, player);

                                            // Reset the entity data
                                            player.ResetData(EntityData.JOB_OFFER_PRICE);
                                            player.ResetData(EntityData.PLAYER_JOB_PARTNER);

                                            // Check the type of the service
                                            if (player.GetData(EntityData.HOOKER_TYPE_SERVICE) ==
                                                Constants.HOOKER_SERVICE_BASIC) {
                                                player.PlayAnimation("mini@prostitutes@sexlow_veh",
                                                    "low_car_bj_loop_player", (int) Constants.AnimationFlags.Loop);
                                                target.PlayAnimation("mini@prostitutes@sexlow_veh",
                                                    "low_car_bj_loop_female", (int) Constants.AnimationFlags.Loop);

                                                // Timer to finish the service
                                                var sexTimer = new Timer(Hooker.OnSexServiceTimer, player, 120000,
                                                    Timeout.Infinite);
                                                Hooker.sexTimerList.Add(player.Value, sexTimer);
                                            }
                                            else {
                                                player.PlayAnimation("mini@prostitutes@sexlow_veh",
                                                    "low_car_sex_loop_player", (int) Constants.AnimationFlags.Loop);
                                                target.PlayAnimation("mini@prostitutes@sexlow_veh",
                                                    "low_car_sex_loop_female", (int) Constants.AnimationFlags.Loop);

                                                // Timer to finish the service
                                                var sexTimer = new Timer(Hooker.OnSexServiceTimer, player, 180000,
                                                    Timeout.Infinite);
                                                Hooker.sexTimerList.Add(player.Value, sexTimer);
                                            }

                                            Task.Factory.StartNew(() => {
                                                // Save the log into the database
                                                Database.LogPayment(player.Name, target.Name, GenRes.hooker, amount);
                                            });
                                        }
                                    }
                                    else {
                                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                                    }
                                }
                            }
                        }

                        break;
                    case Commands.ARG_INTERVIEW:
                        if (!player.IsInVehicle) {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle);
                        }
                        else {
                            NetHandle vehicle = player.Vehicle;
                            if (player.VehicleSeat != (int) VehicleSeat.RightRear) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_in_right_rear);
                            }
                            else {
                                Client target = player.GetData(EntityData.PLAYER_JOB_PARTNER);
                                player.SetData(EntityData.PLAYER_ON_AIR, true);
                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.already_on_air);
                                target.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.interview_accepted);
                            }
                        }

                        break;
                    case Commands.ARG_MONEY:
                        if (player.GetData(EntityData.PLAYER_PAYMENT) != null) {
                            Client target = player.GetData(EntityData.PLAYER_PAYMENT);
                            int amount = player.GetData(EntityData.JOB_OFFER_PRICE);

                            if (target.GetData(EntityData.PLAYER_PLAYING) != null) {
                                int money = target.GetSharedData(EntityData.PLAYER_MONEY);

                                if (amount > 0 && money >= amount) {
                                    int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);
                                    player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney + amount);
                                    target.SetSharedData(EntityData.PLAYER_MONEY, money - amount);

                                    // Reset the entity data
                                    player.ResetData(EntityData.JOB_OFFER_PRICE);
                                    player.ResetData(EntityData.PLAYER_PAYMENT);

                                    // Send the messages to both players
                                    var playerMessage = string.Format(InfoRes.player_paid, target.Name, amount);
                                    var targetMessage = string.Format(InfoRes.target_paid, amount, player.Name);
                                    player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                    target.SendChatMessage(Constants.COLOR_INFO + targetMessage);

                                    Task.Factory.StartNew(() => {
                                        // Save the logs into database
                                        Database.LogPayment(target.Name, player.Name, GenRes.payment_players, amount);
                                    });
                                }
                                else {
                                    target.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                                }
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                            }
                        }

                        break;
                    case Commands.ARG_VEHICLE:
                        if (player.GetData(EntityData.PLAYER_SELLING_VEHICLE) != null) {
                            Client target = player.GetData(EntityData.PLAYER_JOB_PARTNER);
                            int amount = player.GetData(EntityData.PLAYER_SELLING_PRICE);
                            int vehicleId = player.GetData(EntityData.PLAYER_SELLING_VEHICLE);

                            if (target.GetData(EntityData.PLAYER_PLAYING) != null) {
                                int money = player.GetSharedData(EntityData.PLAYER_BANK);

                                if (money >= amount) {
                                    var vehicleModel = string.Empty;
                                    var vehicle = Vehicles.GetVehicleById(vehicleId);

                                    if (vehicle == null) {
                                        var vehModel = Parking.GetParkedVehicleById(vehicleId);
                                        vehModel.owner = player.Name;
                                        vehicleModel = vehModel.model;
                                    }
                                    else {
                                        vehicle.SetData(EntityData.VEHICLE_OWNER, player.Name);
                                        vehicleModel = vehicle.GetData(EntityData.VEHICLE_MODEL);
                                    }

                                    int targetMoney = target.GetSharedData(EntityData.PLAYER_BANK);
                                    player.SetSharedData(EntityData.PLAYER_BANK, money - amount);
                                    target.SetSharedData(EntityData.PLAYER_BANK, targetMoney + amount);

                                    player.ResetData(EntityData.PLAYER_SELLING_VEHICLE);
                                    player.ResetData(EntityData.PLAYER_SELLING_PRICE);

                                    var playerString = string.Format(InfoRes.vehicle_buy, target.Name, vehicleModel,
                                        amount);
                                    var targetString = string.Format(InfoRes.vehicle_bought, player.Name, vehicleModel,
                                        amount);
                                    player.SendChatMessage(Constants.COLOR_INFO + playerString);
                                    target.SendChatMessage(Constants.COLOR_INFO + targetString);

                                    Task.Factory.StartNew(() => {
                                        // Save the logs into database
                                        Database.LogPayment(target.Name, player.Name, GenRes.vehicle_sale, amount);
                                    });
                                }
                                else {
                                    var message = string.Format(ErrRes.carshop_no_money, amount);
                                    player.SendChatMessage(Constants.COLOR_ERROR + message);
                                }
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                            }
                        }

                        break;
                    case Commands.ARG_HOUSE:
                        if (player.GetData(EntityData.PLAYER_SELLING_HOUSE) != null) {
                            Client target = player.GetData(EntityData.PLAYER_JOB_PARTNER);
                            int amount = player.GetData(EntityData.PLAYER_SELLING_PRICE);
                            int houseId = player.GetData(EntityData.PLAYER_SELLING_HOUSE);

                            if (target.GetData(EntityData.PLAYER_PLAYING) != null) {
                                int money = player.GetSharedData(EntityData.PLAYER_BANK);

                                if (money >= amount) {
                                    var house = House.GetHouseById(houseId);

                                    if (house.owner == target.Name) {
                                        house.owner = player.Name;
                                        house.tenants = 2;

                                        int targetMoney = target.GetSharedData(EntityData.PLAYER_BANK);
                                        player.SetSharedData(EntityData.PLAYER_BANK, money - amount);
                                        target.SetSharedData(EntityData.PLAYER_BANK, targetMoney + amount);

                                        player.ResetData(EntityData.PLAYER_SELLING_HOUSE);
                                        player.ResetData(EntityData.PLAYER_SELLING_PRICE);

                                        var playerString = string.Format(InfoRes.house_buyto, target.Name, amount);
                                        var targetString = string.Format(InfoRes.house_bought, player.Name, amount);
                                        player.SendChatMessage(Constants.COLOR_INFO + playerString);
                                        target.SendChatMessage(Constants.COLOR_INFO + targetString);

                                        Task.Factory.StartNew(() => {
                                            // Update the house
                                            Database.KickTenantsOut(house.id);
                                            Database.UpdateHouse(house);

                                            // Log the payment into database
                                            Database.LogPayment(target.Name, player.Name, GenRes.house_sale, amount);
                                        });
                                    }
                                    else {
                                        player.SendChatMessage(ErrRes.house_sell_generic);
                                        target.SendChatMessage(ErrRes.house_sell_generic);
                                    }
                                }
                                else {
                                    var message = string.Format(ErrRes.carshop_no_money, amount);
                                    target.SendChatMessage(Constants.COLOR_ERROR + message);
                                }
                            }
                        }

                        break;
                    case Commands.ARG_STATE_HOUSE:
                        if (player.GetData(EntityData.PLAYER_SELLING_HOUSE_STATE) != null) {
                            HouseModel house =
                                House.GetHouseById(player.GetData(EntityData.PLAYER_SELLING_HOUSE_STATE));
                            var amount = (int) Math.Round(house.price * Constants.HOUSE_SALE_STATE);

                            if (player.GetData(EntityData.PLAYER_PLAYING) != null) {
                                if (house.owner == player.Name) {
                                    house.locked = true;
                                    house.owner = string.Empty;
                                    house.status = Constants.HOUSE_STATE_BUYABLE;
                                    house.houseLabel.Text = House.GetHouseLabelText(house);
                                    NAPI.World.RemoveIpl(house.ipl);
                                    house.tenants = 2;

                                    int playerMoney = player.GetSharedData(EntityData.PLAYER_BANK);
                                    player.SetSharedData(EntityData.PLAYER_BANK, playerMoney + amount);

                                    player.SendChatMessage(Constants.COLOR_SUCCESS +
                                                           string.Format(SuccRes.house_sold, amount));

                                    Task.Factory.StartNew(() => {
                                        // Update the house
                                        Database.KickTenantsOut(house.id);
                                        Database.UpdateHouse(house);

                                        // Log the payment into the database
                                        Database.LogPayment(player.Name, GenRes.state, GenRes.house_sale, amount);
                                    });
                                }
                                else {
                                    player.SendChatMessage(ErrRes.house_sell_generic);
                                }
                            }
                        }

                        break;
                    default:
                        player.SendChatMessage(Constants.COLOR_ERROR + Commands.HLP_GLOBALS_ACCEPT_COMMAND);
                        break;
                }
        }

        [Command(Commands.COM_PICK_UP)]
        public void PickUpCommand(Client player) {
            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.right_hand_occupied);
            }
            else if (player.GetSharedData(EntityData.PLAYER_WEAPON_CRATE) != null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.both_hand_occupied);
            }
            else {
                var item = GetClosestItem(player);
                if (item != null) {
                    // Get the item on the ground
                    var playerItem = GetPlayerItemModelFromHash(player.Value, item.hash);

                    if (playerItem != null) {
                        // Add the amount to the player item
                        playerItem.amount += item.amount;

                        Task.Factory.StartNew(() => {
                            Database.RemoveItem(item.id);
                            itemList.Remove(item);
                        });
                    }
                    else {
                        playerItem = item;
                    }

                    // Get the new owner of the item
                    playerItem.ownerEntity = Constants.ITEM_ENTITY_RIGHT_HAND;
                    playerItem.ownerIdentifier = player.GetData(EntityData.PLAYER_SQL_ID);

                    // Delete the item on the ground
                    item.objectHandle.Delete();

                    // Play the animation
                    player.PlayAnimation("random@domestic", "pickup_low", 0);

                    // Add the item to the player
                    var businessItem = Business.GetBusinessItemFromHash(playerItem.hash);
                    var weaponHash = NAPI.Util.WeaponNameToModel(playerItem.hash);

                    if (weaponHash != 0) {
                        // Give the weapon to the player
                        player.GiveWeapon(weaponHash, 0);
                        player.SetWeaponAmmo(weaponHash, playerItem.amount);
                    }
                    else {
                        // Create the object on the player's right hand
                        AttachItemToPlayer(player, playerItem.id, playerItem.hash, "IK_R_Hand", businessItem.position,
                            businessItem.rotation, EntityData.PLAYER_RIGHT_HAND);
                    }

                    Task.Factory.StartNew(() => {
                        // Update the item's owner
                        Database.UpdateItem(item);
                    });

                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_picked_item);
                }
                else {
                    var weaponCrate = Weapons.GetClosestWeaponCrate(player);
                    if (weaponCrate != null) {
                        var index = Weapons.weaponCrateList.IndexOf(weaponCrate);
                        weaponCrate.carriedEntity = Constants.ITEM_ENTITY_PLAYER;
                        weaponCrate.carriedIdentifier = player.Value;
                        player.PlayAnimation("anim@heists@box_carry@", "idle",
                            (int) (Constants.AnimationFlags.Loop | Constants.AnimationFlags.OnlyAnimateUpperBody |
                                   Constants.AnimationFlags.AllowPlayerControl));

                        // Create the object on the player's arms
                        AttachItemToPlayer(player, index, weaponCrate.crateObject.Model.ToString(), "IK_R_Hand",
                            new Vector3(0.0f, -0.5f, -0.25f), new Vector3(), EntityData.PLAYER_WEAPON_CRATE);

                        // Remove the item's object
                        weaponCrate.crateObject.Delete();
                        weaponCrate.crateObject = null;
                    }
                    else {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_items_near);
                    }
                }
            }
        }

        [Command(Commands.COM_DROP)]
        public void DropCommand(Client player) {
            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                // Get the item on the right hand
                string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
                var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;

                var item = GetItemModelFromId(itemId);
                var businessItem = Business.GetBusinessItemFromHash(item.hash);

                // Drop the item on the hand
                DropItem(player, item, businessItem, true);
            }
            else if (player.GetSharedData(EntityData.PLAYER_WEAPON_CRATE) != null) {
                var weaponCrate = Weapons.GetPlayerCarriedWeaponCrate(player.Value);

                if (weaponCrate != null) {
                    // Update the new position and carrier
                    weaponCrate.position = new Vector3(player.Position.X, player.Position.Y, player.Position.Z - 1.0f);
                    weaponCrate.carriedEntity = string.Empty;
                    weaponCrate.carriedIdentifier = 0;

                    // Create the object on the floor
                    weaponCrate.crateObject =
                        NAPI.Object.CreateObject(481432069, weaponCrate.position, new Vector3(), 0);

                    // Dettach the object from the player
                    RemoveItemOnHands(player);
                    player.StopAnimation();

                    var message = string.Format(InfoRes.player_inventory_drop, GenRes.weapon_crate);
                    player.SendChatMessage(Constants.COLOR_INFO + message);
                }
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.right_hand_empty);
            }
        }

        [Command(Commands.COM_TICKET, Commands.HLP_HELP_REQUEST, GreedyArg = true)]
        public void TicketCommand(Client player, string message) {
            foreach (var ticket in adminTicketList)
                if (player.Value == ticket.playerId) {
                    ticket.question = message;
                    return;
                }

            // Create a new ticket
            var adminTicket = new AdminTicketModel();
            {
                adminTicket.playerId = player.Value;
                adminTicket.question = message;
            }

            // Add the ticket to the list
            adminTicketList.Add(adminTicket);

            // Send the message to the staff online
            foreach (var target in NAPI.Pools.GetAllPlayers())
                if (target.GetData(EntityData.PLAYER_PLAYING) != null &&
                    target.GetData(EntityData.PLAYER_ADMIN_RANK) > 0)
                    target.SendChatMessage(Constants.COLOR_ADMIN_INFO + AdminRes.new_admin_ticket);
                else if (target == player) player.SendChatMessage(Constants.COLOR_SUCCESS + SuccRes.help_request_sent);
        }

        [Command(Commands.COM_DOOR)]
        public void DoorCommand(Client player) {
            // Check if the player's in his house
            foreach (var house in House.houseList)
                if (player.Position.DistanceTo(house.position) <= 1.5f && player.Dimension == house.dimension ||
                    player.GetData(EntityData.PLAYER_HOUSE_ENTERED) == house.id) {
                    if (House.HasPlayerHouseKeys(player, house) == false) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_house_owner);
                    }
                    else {
                        house.locked = !house.locked;

                        Task.Factory.StartNew(() => {
                            // Update the house
                            Database.UpdateHouse(house);
                        });

                        player.SendChatMessage(house.locked
                            ? Constants.COLOR_INFO + InfoRes.house_locked
                            : Constants.COLOR_INFO + InfoRes.house_opened);
                    }

                    return;
                }

            // Check if the player's in his business
            foreach (var business in Business.businessList)
                if (player.Position.DistanceTo(business.position) <= 1.5f && player.Dimension == business.dimension ||
                    player.GetData(EntityData.PLAYER_BUSINESS_ENTERED) == business.id) {
                    if (Business.HasPlayerBusinessKeys(player, business) == false) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_business_owner);
                    }
                    else {
                        business.locked = !business.locked;

                        Task.Factory.StartNew(() => {
                            // Update the business
                            Database.UpdateBusiness(business);
                        });

                        player.SendChatMessage(business.locked
                            ? Constants.COLOR_INFO + InfoRes.business_locked
                            : Constants.COLOR_INFO + InfoRes.business_opened);
                    }

                    return;
                }

            // He's not in any house or business
            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_house_business);
        }

        [Command(Commands.COM_COMPLEMENT, Commands.HLP_COMPLEMENT_COMMAND)]
        public void ComplementCommand(Client player, string type, string action) {
            ClothesModel clothes = null;
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);

            if (action.ToLower() == Commands.ARG_WEAR || action.ToLower() == Commands.ARG_REMOVE)
                switch (type.ToLower()) {
                    case Commands.ARG_MASK:
                        clothes = GetDressedClothesInSlot(playerId, 0, Constants.CLOTHES_MASK);
                        if (action.ToLower() == Commands.ARG_WEAR) {
                            if (clothes == null) {
                                clothes = GetPlayerClothes(playerId)
                                    .Where(c => c.slot == Constants.CLOTHES_MASK && c.type == 0).First();
                                if (clothes == null)
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_mask_bought);
                                else
                                    player.SetClothes(clothes.slot, clothes.drawable, clothes.texture);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.mask_equiped);
                            }
                        }
                        else {
                            if (clothes == null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_mask_equiped);
                            }
                            else {
                                player.SetClothes(Constants.CLOTHES_MASK, 0, 0);
                                UndressClothes(playerId, 0, Constants.CLOTHES_MASK);
                            }
                        }

                        break;
                    case Commands.ARG_BAG:
                        clothes = GetDressedClothesInSlot(playerId, 0, Constants.CLOTHES_BAGS);
                        if (action.ToLower() == Commands.ARG_WEAR) {
                            if (clothes == null) {
                                clothes = GetPlayerClothes(playerId)
                                    .Where(c => c.slot == Constants.CLOTHES_BAGS && c.type == 0).First();
                                if (clothes == null)
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_bag_bought);
                                else
                                    player.SetClothes(clothes.slot, clothes.drawable, clothes.texture);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.bag_equiped);
                            }
                        }
                        else {
                            if (clothes == null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_bag_equiped);
                            }
                            else {
                                player.SetClothes(Constants.CLOTHES_BAGS, 0, 0);
                                UndressClothes(playerId, 0, Constants.CLOTHES_BAGS);
                            }
                        }

                        break;
                    case Commands.ARG_ACCESSORY:
                        clothes = GetDressedClothesInSlot(playerId, 0, Constants.CLOTHES_ACCESSORIES);
                        if (action.ToLower() == Commands.ARG_WEAR) {
                            if (clothes == null) {
                                clothes = GetPlayerClothes(playerId)
                                    .Where(c => c.slot == Constants.CLOTHES_ACCESSORIES && c.type == 0).First();
                                if (clothes == null)
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_accessory_bought);
                                else
                                    player.SetClothes(clothes.slot, clothes.drawable, clothes.texture);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.accessory_equiped);
                            }
                        }
                        else {
                            if (clothes == null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_accessory_equiped);
                            }
                            else {
                                player.SetClothes(Constants.CLOTHES_ACCESSORIES, 0, 0);
                                UndressClothes(playerId, 0, Constants.CLOTHES_ACCESSORIES);
                            }
                        }

                        break;
                    case Commands.ARG_HAT:
                        clothes = GetDressedClothesInSlot(playerId, 1, Constants.ACCESSORY_HATS);
                        if (action.ToLower() == Commands.ARG_WEAR) {
                            if (clothes == null) {
                                clothes = GetPlayerClothes(playerId)
                                    .Where(c => c.slot == Constants.ACCESSORY_HATS && c.type == 1).First();
                                if (clothes == null)
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_hat_bought);
                                else
                                    player.SetAccessories(clothes.slot, clothes.drawable, clothes.texture);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.hat_equiped);
                            }
                        }
                        else {
                            if (clothes == null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_hat_equiped);
                            }
                            else {
                                if (player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_FEMALE)
                                    player.SetAccessories(Constants.ACCESSORY_HATS, 57, 0);
                                else
                                    player.SetAccessories(Constants.ACCESSORY_HATS, 8, 0);
                                UndressClothes(playerId, 1, Constants.ACCESSORY_HATS);
                            }
                        }

                        break;
                    case Commands.ARG_GLASSES:
                        clothes = GetDressedClothesInSlot(playerId, 1, Constants.ACCESSORY_GLASSES);
                        if (action.ToLower() == Commands.ARG_WEAR) {
                            if (clothes == null) {
                                clothes = GetPlayerClothes(playerId)
                                    .Where(c => c.slot == Constants.ACCESSORY_GLASSES && c.type == 1).First();
                                if (clothes == null)
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_glasses_bought);
                                else
                                    player.SetAccessories(clothes.slot, clothes.drawable, clothes.texture);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.glasses_equiped);
                            }
                        }
                        else {
                            if (clothes == null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_glasses_equiped);
                            }
                            else {
                                if (player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_FEMALE)
                                    player.SetAccessories(Constants.ACCESSORY_GLASSES, 5, 0);
                                else
                                    player.SetAccessories(Constants.ACCESSORY_GLASSES, 0, 0);
                                UndressClothes(playerId, 1, Constants.ACCESSORY_GLASSES);
                            }
                        }

                        break;
                    case Commands.ARG_EARRINGS:
                        clothes = GetDressedClothesInSlot(playerId, 1, Constants.ACCESSORY_EARS);
                        if (action.ToLower() == Commands.ARG_WEAR) {
                            if (clothes == null) {
                                clothes = GetPlayerClothes(playerId)
                                    .Where(c => c.slot == Constants.ACCESSORY_EARS && c.type == 1).First();
                                if (clothes == null)
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_ear_bought);
                                else
                                    player.SetAccessories(clothes.slot, clothes.drawable, clothes.texture);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.ear_equiped);
                            }
                        }
                        else {
                            if (clothes == null) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_ear_equiped);
                            }
                            else {
                                if (player.GetData(EntityData.PLAYER_SEX) == Constants.SEX_FEMALE)
                                    player.SetAccessories(Constants.ACCESSORY_EARS, 12, 0);
                                else
                                    player.SetAccessories(Constants.ACCESSORY_EARS, 3, 0);
                                UndressClothes(playerId, 1, Constants.ACCESSORY_EARS);
                            }
                        }

                        break;
                    default:
                        player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_COMPLEMENT_COMMAND);
                        break;
                }
            else
                player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_COMPLEMENT_COMMAND);
        }

        [Command(Commands.COM_PLAYER, Alias = Commands.COM_PLAYER_ALIAS)]
        public void PlayerCommand(Client player) {
            // Get players basic data
            PlayerData.RetrieveBasicDataEvent(player, player);
        }
    }
}