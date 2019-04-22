﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.business;
using XenRP.chat;
using XenRP.database;
using XenRP.globals;
using XenRP.jobs;
using XenRP.messages.error;
using XenRP.messages.information;
using XenRP.messages.success;
using XenRP.model;
using XenRP.parking;
using XenRP.weapons;

namespace XenRP.vehicles {
    public class Vehicles : Script {
        private static Dictionary<int, Timer> gasTimerList;
        private static Dictionary<int, Timer> vehicleRespawnTimerList;

        public static void LoadDatabaseVehicles() {
            // Create the timer dictionaries
            gasTimerList = new Dictionary<int, Timer>();
            vehicleRespawnTimerList = new Dictionary<int, Timer>();

            var vehicleList = Database.LoadAllVehicles();
            Parking.parkedCars = new List<ParkedCarModel>();

            foreach (var vehModel in vehicleList)
                if (vehModel.parking == 0) {
                    // Create the vehicle ingame
                    CreateIngameVehicle(vehModel);
                }
                else {
                    // Link the car to the parking
                    var parkedCarModel = new ParkedCarModel();
                    {
                        parkedCarModel.parkingId = vehModel.parking;
                        parkedCarModel.vehicle = vehModel;
                    }

                    // Add the vehicle to the parking
                    Parking.parkedCars.Add(parkedCarModel);
                }
        }

        public static void CreateVehicle(Client player, VehicleModel vehModel, bool adminCreated) {
            Task.Factory.StartNew(() => {
                NAPI.Task.Run(() => {
                    // Add the vehicle to the database
                    vehModel.id = Database.AddNewVehicle(vehModel);

                    // Create the vehicle ingame
                    CreateIngameVehicle(vehModel);

                    if (!adminCreated) {
                        int moneyLeft = player.GetSharedData(EntityData.PLAYER_BANK) - vehModel.price;
                        var purchaseMssage = string.Format(SuccRes.vehicle_purchased, vehModel.model, vehModel.price);
                        player.SendChatMessage(Constants.COLOR_SUCCESS + purchaseMssage);
                        player.SetSharedData(EntityData.PLAYER_BANK, moneyLeft);
                    }
                });
            });
        }

        public static Vehicle CreateIngameVehicle(VehicleModel vehModel) {
            var vehicle = NAPI.Vehicle.CreateVehicle(NAPI.Util.VehicleNameToModel(vehModel.model), vehModel.position,
                vehModel.rotation.Z, new Color(0, 0, 0), new Color(0, 0, 0));
            vehicle.NumberPlate = vehModel.plate == string.Empty ? "LS " + (1000 + vehModel.id) : vehModel.plate;
            vehicle.EngineStatus = vehModel.engine != 0;
            vehicle.Locked = vehModel.locked != 0;
            vehicle.Dimension = Convert.ToUInt32(vehModel.dimension);

            if (vehModel.colorType == Constants.VEHICLE_COLOR_TYPE_PREDEFINED) {
                vehicle.PrimaryColor = int.Parse(vehModel.firstColor);
                vehicle.SecondaryColor = int.Parse(vehModel.secondColor);
                vehicle.PearlescentColor = vehModel.pearlescent;
            }
            else {
                var firstColor = vehModel.firstColor.Split(',');
                var secondColor = vehModel.secondColor.Split(',');
                vehicle.CustomPrimaryColor = new Color(int.Parse(firstColor[0]), int.Parse(firstColor[1]),
                    int.Parse(firstColor[2]));
                vehicle.CustomSecondaryColor = new Color(int.Parse(secondColor[0]), int.Parse(secondColor[1]),
                    int.Parse(secondColor[2]));
            }

            if (vehicle.Model == (uint) VehicleHash.Ambulance) vehicle.Livery = 1;

            vehicle.SetData(EntityData.VEHICLE_ID, vehModel.id);
            vehicle.SetData(EntityData.VEHICLE_MODEL, vehModel.model);
            vehicle.SetData(EntityData.VEHICLE_POSITION, vehModel.position);
            vehicle.SetData(EntityData.VEHICLE_ROTATION, vehModel.rotation);
            vehicle.SetData(EntityData.VEHICLE_DIMENSION, vehModel.dimension);
            vehicle.SetData(EntityData.VEHICLE_COLOR_TYPE, vehModel.colorType);
            vehicle.SetData(EntityData.VEHICLE_FIRST_COLOR, vehModel.firstColor);
            vehicle.SetData(EntityData.VEHICLE_SECOND_COLOR, vehModel.secondColor);
            vehicle.SetData(EntityData.VEHICLE_PEARLESCENT_COLOR, vehModel.pearlescent);
            vehicle.SetData(EntityData.VEHICLE_FACTION, vehModel.faction);
            vehicle.SetData(EntityData.VEHICLE_PLATE, vehModel.plate);
            vehicle.SetData(EntityData.VEHICLE_OWNER, vehModel.owner);
            vehicle.SetData(EntityData.VEHICLE_PRICE, vehModel.price);
            vehicle.SetData(EntityData.VEHICLE_PARKING, vehModel.parking);
            vehicle.SetData(EntityData.VEHICLE_PARKED, vehModel.parked);
            vehicle.SetData(EntityData.VEHICLE_GAS, vehModel.gas);
            vehicle.SetData(EntityData.VEHICLE_KMS, vehModel.kms);
            vehicle.SetSharedData(EntityData.VEHICLE_SIREN_SOUND, true);
            vehicle.SetSharedData(EntityData.VEHICLE_DOORS_STATE,
                NAPI.Util.ToJson(new List<bool> {false, false, false, false, false, false}));

            // Set vehicle's tunning
            Mechanic.AddTunningToVehicle(vehicle);

            return vehicle;
        }

        public static void OnPlayerDisconnected(Client player) {
            if (gasTimerList.TryGetValue(player.Value, out var gasTimer)) {
                gasTimer.Dispose();
                gasTimerList.Remove(player.Value);
            }
        }

        public static bool HasPlayerVehicleKeys(Client player, object vehicle) {
            var hasKeys = false;
            int vehicleId = vehicle is Vehicle
                ? ((Vehicle) vehicle).GetData(EntityData.VEHICLE_ID)
                : ((VehicleModel) vehicle).id;
            string vehicleOwner = vehicle is Vehicle
                ? ((Vehicle) vehicle).GetData(EntityData.VEHICLE_OWNER)
                : ((VehicleModel) vehicle).owner;

            if (vehicleOwner == player.Name) {
                hasKeys = true;
            }
            else {
                string keyString = player.GetData(EntityData.PLAYER_VEHICLE_KEYS);
                hasKeys = keyString.Split(',').Any(key => int.Parse(key) == vehicleId);
            }

            return hasKeys;
        }

        public static Vehicle GetVehicleById(int vehicleId) {
            // Get the vehicle given the identifier
            return NAPI.Pools.GetAllVehicles().Where(veh => veh.GetData(EntityData.VEHICLE_ID) == vehicleId)
                .FirstOrDefault();
        }

        public static Vehicle GetClosestVehicle(Client player, float distance = 2.5f) {
            Vehicle vehicle = null;
            foreach (var veh in NAPI.Pools.GetAllVehicles()) {
                var vehPos = veh.Position;
                var distanceVehicleToPlayer = player.Position.DistanceTo(vehPos);

                if (distanceVehicleToPlayer < distance && player.Dimension == veh.Dimension) {
                    distance = distanceVehicleToPlayer;
                    vehicle = veh;
                }
            }

            return vehicle;
        }

        public static void SaveAllVehicles() {
            var vehicleList = new List<VehicleModel>();
            var citizenVehicles = NAPI.Pools.GetAllVehicles().Where(veh =>
                    veh.GetData(EntityData.VEHICLE_TESTING) == null && veh.GetData(EntityData.VEHICLE_FACTION) == 0)
                .ToList();

            foreach (var vehicle in citizenVehicles) {
                var vehicleModel = new VehicleModel();
                {
                    vehicleModel.id = vehicle.GetData(EntityData.VEHICLE_ID);
                    vehicleModel.model = vehicle.GetData(EntityData.VEHICLE_MODEL);
                    vehicleModel.position = vehicle.Position;
                    vehicleModel.rotation = vehicle.Rotation;
                    vehicleModel.dimension = vehicle.Dimension;
                    vehicleModel.colorType = vehicle.GetData(EntityData.VEHICLE_COLOR_TYPE);
                    vehicleModel.firstColor = vehicle.GetData(EntityData.VEHICLE_FIRST_COLOR);
                    vehicleModel.secondColor = vehicle.GetData(EntityData.VEHICLE_SECOND_COLOR);
                    vehicleModel.pearlescent = vehicle.GetData(EntityData.VEHICLE_PEARLESCENT_COLOR);
                    vehicleModel.faction = vehicle.GetData(EntityData.VEHICLE_FACTION);
                    vehicleModel.plate = vehicle.GetData(EntityData.VEHICLE_PLATE);
                    vehicleModel.owner = vehicle.GetData(EntityData.VEHICLE_OWNER);
                    vehicleModel.price = vehicle.GetData(EntityData.VEHICLE_PRICE);
                    vehicleModel.parking = vehicle.GetData(EntityData.VEHICLE_PARKING);
                    vehicleModel.parked = vehicle.GetData(EntityData.VEHICLE_PARKED);
                    vehicleModel.gas = vehicle.GetData(EntityData.VEHICLE_GAS);
                    vehicleModel.kms = vehicle.GetData(EntityData.VEHICLE_KMS);
                }

                // Add vehicle into the list
                vehicleList.Add(vehicleModel);
            }

            Task.Factory.StartNew(() => {
                // Save all the vehicles
                Database.SaveAllVehicles(vehicleList);
            });
        }

        private bool IsVehicleTrunkInUse(Vehicle vehicle) {
            var trunkUsed = false;

            foreach (var player in NAPI.Pools.GetAllPlayers())
                if (player.GetData(EntityData.PLAYER_OPENED_TRUNK) != null) {
                    Vehicle openedVehicle = player.GetData(EntityData.PLAYER_OPENED_TRUNK);
                    if (openedVehicle == vehicle) {
                        trunkUsed = true;
                        break;
                    }
                }

            return trunkUsed;
        }

        private void OnVehicleDeathTimer(object vehicleObject) {
            var vehicle = (Vehicle) vehicleObject;

            // Get the needed data for recreating the vehicle
            var vehicleModel = new VehicleModel();
            {
                vehicleModel.id = vehicle.GetData(EntityData.VEHICLE_ID);
                vehicleModel.model = vehicle.GetData(EntityData.VEHICLE_MODEL);
                vehicleModel.position = vehicle.GetData(EntityData.VEHICLE_POSITION);
                vehicleModel.rotation = vehicle.GetData(EntityData.VEHICLE_ROTATION);
                vehicleModel.dimension = vehicle.GetData(EntityData.VEHICLE_DIMENSION);
                vehicleModel.colorType = vehicle.GetData(EntityData.VEHICLE_COLOR_TYPE);
                vehicleModel.firstColor = vehicle.GetData(EntityData.VEHICLE_FIRST_COLOR);
                vehicleModel.secondColor = vehicle.GetData(EntityData.VEHICLE_SECOND_COLOR);
                vehicleModel.pearlescent = vehicle.GetData(EntityData.VEHICLE_PEARLESCENT_COLOR);
                vehicleModel.faction = vehicle.GetData(EntityData.VEHICLE_FACTION);
                vehicleModel.plate = vehicle.GetData(EntityData.VEHICLE_PLATE);
                vehicleModel.owner = vehicle.GetData(EntityData.VEHICLE_OWNER);
                vehicleModel.price = vehicle.GetData(EntityData.VEHICLE_PRICE);
                vehicleModel.parking = vehicle.GetData(EntityData.VEHICLE_PARKING);
                vehicleModel.parked = vehicle.GetData(EntityData.VEHICLE_PARKED);
                vehicleModel.gas = vehicle.GetData(EntityData.VEHICLE_GAS);
                vehicleModel.kms = vehicle.GetData(EntityData.VEHICLE_KMS);
            }

            NAPI.Task.Run(() => {
                // Delete the vehicle
                vehicle.Delete();
            });

            if (vehicleModel.faction == Constants.FACTION_NONE && vehicle.Occupants.Count > 0) {
                var scrapyard = Parking.parkingList.Where(p => p.type == Constants.PARKING_TYPE_SCRAPYARD)
                    .FirstOrDefault();

                if (scrapyard != null) {
                    // Link the vehicle to the scrapyard
                    var parkedCar = new ParkedCarModel();
                    {
                        parkedCar.parkingId = scrapyard.id;
                        parkedCar.vehicle = vehicleModel;
                    }

                    // Add the vehicle to the parking
                    Parking.parkedCars.Add(parkedCar);
                    vehicleModel.parking = scrapyard.id;
                }

                Task.Factory.StartNew(() => {
                    // Save vehicle data
                    Database.SaveVehicle(vehicleModel);
                });
            }
            else {
                NAPI.Task.Run(() => {
                    // Recreate the vehicle in the same position
                    CreateIngameVehicle(vehicleModel);
                });
            }

            var vehicleRespawnTimer = vehicleRespawnTimerList[vehicleModel.id];
            if (vehicleRespawnTimer != null) {
                vehicleRespawnTimer.Dispose();
                vehicleRespawnTimerList.Remove(vehicleModel.id);
            }
        }

        private void OnVehicleRefueled(object vehicleObject) {
            var vehicle = (Vehicle) vehicleObject;
            Client player = vehicle.GetData(EntityData.VEHICLE_REFUELING);

            vehicle.ResetData(EntityData.VEHICLE_REFUELING);
            player.ResetData(EntityData.PLAYER_REFUELING);

            if (gasTimerList.TryGetValue(player.Value, out var gasTimer)) {
                gasTimer.Dispose();
                gasTimerList.Remove(player.Value);
            }

            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.vehicle_refueled);
        }

        [ServerEvent(Event.PlayerEnterCheckpoint)]
        public void OnPlayerEnterCheckpoint(Checkpoint checkpoint, Client player) {
            if (player.GetData(EntityData.PLAYER_PARKED_VEHICLE) != null) {
                Checkpoint vehicleCheckpoint = player.GetData(EntityData.PLAYER_PARKED_VEHICLE);

                if (vehicleCheckpoint == checkpoint) {
                    // Delete the checkpoint
                    vehicleCheckpoint.Delete();
                    player.ResetData(EntityData.PLAYER_PARKED_VEHICLE);
                    player.TriggerEvent("deleteVehicleLocation");
                }
            }
        }

        [ServerEvent(Event.PlayerEnterVehicle)]
        public void OnPlayerEnterVehicle(Client player, Vehicle vehicle, sbyte seat) {
            if (Convert.ToInt32(seat) == (int) VehicleSeat.Driver) {
                if (vehicle.GetData(EntityData.VEHICLE_TESTING) != null) {
                    if (player.GetData(EntityData.PLAYER_TESTING_VEHICLE) != null) {
                        Vehicle testingVehicle = player.GetData(EntityData.PLAYER_TESTING_VEHICLE);
                        if (vehicle != testingVehicle) {
                            player.WarpOutOfVehicle();
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_testing_vehicle);
                            return;
                        }
                    }
                    else {
                        player.WarpOutOfVehicle();
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_testing_vehicle);
                        return;
                    }
                }
                else {
                    int vehFaction = vehicle.GetData(EntityData.VEHICLE_FACTION);

                    if (vehFaction > 0) {
                        // Get player's faction and job
                        int playerFaction = player.GetData(EntityData.PLAYER_FACTION);
                        int playerJob = player.GetData(EntityData.PLAYER_JOB) + Constants.MAX_FACTION_VEHICLES;

                        if (player.GetData(EntityData.PLAYER_ADMIN_RANK) == Constants.STAFF_NONE &&
                            vehFaction == Constants.FACTION_ADMIN) {
                            player.WarpOutOfVehicle();
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.admin_vehicle);
                            return;
                        }

                        if (vehFaction > 0 && vehFaction < Constants.MAX_FACTION_VEHICLES &&
                            playerFaction != vehFaction && vehFaction != Constants.FACTION_DRIVING_SCHOOL &&
                            vehFaction != Constants.FACTION_ADMIN) {
                            player.WarpOutOfVehicle();
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle_faction);
                            return;
                        }

                        if (vehFaction > Constants.MAX_FACTION_VEHICLES && playerJob != vehFaction) {
                            player.WarpOutOfVehicle();
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle_job);
                            return;
                        }
                    }
                }

                // Engine toggle message
                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.how_to_start_engine);

                // Initialize speedometer and engine status
                float kms = vehicle.GetData(EntityData.VEHICLE_KMS);
                float gas = vehicle.GetData(EntityData.VEHICLE_GAS);
                player.TriggerEvent("initializeSpeedometer", kms, gas, vehicle.EngineStatus);
            }
        }

        [ServerEvent(Event.VehicleDeath)]
        public void OnVehicleDeath(Vehicle vehicle) {
            int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
            var vehicleRespawnTimer = new Timer(OnVehicleDeathTimer, vehicle, 7500, Timeout.Infinite);
            vehicleRespawnTimerList.Add(vehicleId, vehicleRespawnTimer);
        }

        [RemoteEvent("stopPlayerCar")]
        public void StopPlayerCarEvent(Client player) {
            // Turn the engine off
            player.Vehicle.EngineStatus = false;
        }

        [RemoteEvent("engineOnEventKey")]
        public void EngineOnEventKeyEvent(Client player) {
            var vehicle = player.Vehicle;

            if (vehicle.GetData(EntityData.VEHICLE_TESTING) == null) {
                // Get player's faction and job
                int playerFaction = player.GetData(EntityData.PLAYER_FACTION);
                int playerJob = player.GetData(EntityData.PLAYER_JOB) + Constants.MAX_FACTION_VEHICLES;

                int vehicleFaction = vehicle.GetData(EntityData.VEHICLE_FACTION);

                if (player.GetData(EntityData.PLAYER_ALREADY_FUCKING) != null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.cant_toogle_engine_while_fucking);
                }
                else if (vehicle.GetData(EntityData.VEHICLE_REFUELING) != null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_start_refueling);
                }
                else if (vehicle.GetData(EntityData.VEHICLE_WEAPON_UNPACKING) != null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_start_weapon_unpacking);
                }
                else if (!HasPlayerVehicleKeys(player, vehicle) && vehicleFaction == Constants.FACTION_NONE) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                }
                else if (player.GetData(EntityData.PLAYER_ADMIN_RANK) == Constants.STAFF_NONE &&
                         vehicleFaction == Constants.FACTION_ADMIN) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.admin_vehicle);
                }
                else if (vehicleFaction > 0 && vehicleFaction < Constants.MAX_FACTION_VEHICLES &&
                         playerFaction != vehicleFaction && vehicleFaction != Constants.FACTION_DRIVING_SCHOOL &&
                         vehicleFaction != Constants.FACTION_ADMIN) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle_faction);
                }
                else if (vehicleFaction > Constants.MAX_FACTION_VEHICLES && playerJob != vehicleFaction) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle_job);
                }
                else {
                    vehicle.EngineStatus = !vehicle.EngineStatus;
                    player.SendNotification(vehicle.EngineStatus
                        ? InfoRes.vehicle_turned_on
                        : InfoRes.vehicle_turned_off);
                }
            }
        }

        [RemoteEvent("saveVehicleConsumes")]
        public void SaveVehicleConsumesEvent(Client player, Vehicle vehicle, float kms, float gas) {
            // Update kms and gas
            vehicle.SetData(EntityData.VEHICLE_KMS, kms);
            vehicle.SetData(EntityData.VEHICLE_GAS, gas);
        }

        [RemoteEvent("toggleSeatbelt")]
        public void ToggleSeatbeltEvent(Client player, bool seatbelt) {
            // Send the message to the nearby players
            Chat.SendMessageToNearbyPlayers(player, seatbelt ? InfoRes.seatbelt_fasten : InfoRes.seatbelt_unfasten,
                Constants.MESSAGE_ME, 20.0f);
        }

        [Command(Commands.COM_SEATBELT)]
        public void SeatbeltCommand(Client player) {
            if (!player.IsInVehicle)
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_vehicle);
            else
                player.TriggerEvent("toggleSeatbelt");
        }

        [Command(Commands.COM_LOCK)]
        public void LockCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                var vehicle = GetClosestVehicle(player);
                if (vehicle == null) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_vehicles_near);
                }
                else if (!HasPlayerVehicleKeys(player, vehicle)) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                }
                else {
                    string vehicleModel = vehicle.GetData(EntityData.VEHICLE_MODEL);
                    var vehicleHash = NAPI.Util.VehicleNameToModel(vehicleModel);
                    if (NAPI.Vehicle.GetVehicleClass(vehicleHash) == Constants.VEHICLE_CLASS_CYCLES ||
                        NAPI.Vehicle.GetVehicleClass(vehicleHash) == Constants.VEHICLE_CLASS_MOTORCYCLES ||
                        NAPI.Vehicle.GetVehicleClass(vehicleHash) == Constants.VEHICLE_CLASS_BOATS) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_not_lockable);
                    }
                    else {
                        vehicle.Locked = !vehicle.Locked;
                        Chat.SendMessageToNearbyPlayers(player,
                            vehicle.Locked ? SuccRes.veh_locked : SuccRes.veh_unlocked, Constants.MESSAGE_ME, 20.0f);
                    }
                }
            }
        }

        [Command(Commands.COM_HOOD)]
        public void HoodCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                var vehicle = GetClosestVehicle(player, 3.75f);

                // Get the status of the doors
                List<bool> doorState =
                    NAPI.Util.FromJson<List<bool>>(vehicle.GetSharedData(EntityData.VEHICLE_DOORS_STATE));

                if (vehicle != null) {
                    if (HasPlayerVehicleKeys(player, vehicle) == false &&
                        player.GetData(EntityData.PLAYER_JOB) != Constants.JOB_MECHANIC) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                    }
                    else if (!doorState[Constants.VEHICLE_HOOD]) {
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.hood_opened);

                        doorState[Constants.VEHICLE_HOOD] = !doorState[Constants.VEHICLE_HOOD];
                        vehicle.SetSharedData(EntityData.VEHICLE_DOORS_STATE, NAPI.Util.ToJson(doorState));

                        player.TriggerEvent("toggleVehicleDoor", vehicle.Value, Constants.VEHICLE_HOOD,
                            doorState[Constants.VEHICLE_HOOD]);
                    }
                    else {
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.hood_closed);

                        doorState[Constants.VEHICLE_HOOD] = !doorState[Constants.VEHICLE_HOOD];
                        vehicle.SetSharedData(EntityData.VEHICLE_DOORS_STATE, NAPI.Util.ToJson(doorState));

                        player.TriggerEvent("toggleVehicleDoor", vehicle.Value, Constants.VEHICLE_HOOD,
                            doorState[Constants.VEHICLE_HOOD]);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_vehicles_near);
                }
            }
        }

        [Command(Commands.COM_TRUNK, Commands.HLP_TRUNK_COMMAND)]
        public void TrunkCommand(Client player, string action) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                // Get closest vehicle
                var vehicle = GetClosestVehicle(player, 3.75f);

                if (vehicle != null) {
                    List<InventoryModel> inventory = null;

                    // Get the status of the doors
                    List<bool> doorState =
                        NAPI.Util.FromJson<List<bool>>(vehicle.GetSharedData(EntityData.VEHICLE_DOORS_STATE));

                    switch (action.ToLower()) {
                        case Commands.ARG_OPEN:
                            if (!HasPlayerVehicleKeys(player, vehicle) && vehicle.GetData(EntityData.VEHICLE_FACTION) ==
                                Constants.FACTION_NONE) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                            }
                            else if (doorState[Constants.VEHICLE_TRUNK]) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_trunk_opened);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.trunk_opened);

                                doorState[Constants.VEHICLE_TRUNK] = !doorState[Constants.VEHICLE_TRUNK];
                                vehicle.SetSharedData(EntityData.VEHICLE_DOORS_STATE, NAPI.Util.ToJson(doorState));

                                player.TriggerEvent("toggleVehicleDoor", vehicle.Value, Constants.VEHICLE_TRUNK,
                                    doorState[Constants.VEHICLE_TRUNK]);
                            }

                            break;
                        case Commands.ARG_CLOSE:
                            if (!HasPlayerVehicleKeys(player, vehicle) && vehicle.GetData(EntityData.VEHICLE_FACTION) ==
                                Constants.FACTION_NONE) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                            }
                            else if (!doorState[Constants.VEHICLE_TRUNK]) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_trunk_closed);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.trunk_closed);

                                doorState[Constants.VEHICLE_TRUNK] = !doorState[Constants.VEHICLE_TRUNK];
                                vehicle.SetSharedData(EntityData.VEHICLE_DOORS_STATE, NAPI.Util.ToJson(doorState));

                                player.TriggerEvent("toggleVehicleDoor", vehicle.Value, Constants.VEHICLE_TRUNK,
                                    doorState[Constants.VEHICLE_TRUNK]);
                            }

                            break;
                        case Commands.ARG_STORE:
                            if (!doorState[Constants.VEHICLE_TRUNK]) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_trunk_closed);
                            }
                            else if (IsVehicleTrunkInUse(vehicle)) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_trunk_in_use);
                            }
                            else {
                                if (player.GetSharedData(EntityData.PLAYER_WEAPON_CRATE) != null) {
                                    // Get player's hand item
                                    string attachmentJson = player.GetSharedData(EntityData.PLAYER_WEAPON_CRATE);
                                    var attachment = NAPI.Util.FromJson<AttachmentModel>(attachmentJson);
                                    var weaponCrate = Weapons.weaponCrateList.ElementAt(attachment.itemId);

                                    // Store the item in the trunk
                                    weaponCrate.carriedEntity = Constants.ITEM_ENTITY_VEHICLE;
                                    weaponCrate.carriedIdentifier = vehicle.GetData(EntityData.VEHICLE_ID);

                                    // Remove player's weapon box
                                    player.StopAnimation();
                                    Globals.RemoveItemOnHands(player);

                                    // Check for any crate into vehicle's trunk
                                    var vehicleCrates = Weapons.weaponCrateList.Where(c =>
                                        c.carriedEntity == Constants.ITEM_ENTITY_VEHICLE &&
                                        vehicle.GetData(EntityData.VEHICLE_ID) == c.carriedIdentifier).Count();

                                    if (vehicleCrates == 1)
                                        foreach (var target in vehicle.Occupants)
                                            if (target.VehicleSeat == (int) VehicleSeat.Driver) {
                                                var weaponPosition = new Vector3(-2085.543f, 2600.857f, -0.4712417f);
                                                var weaponCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4,
                                                    weaponPosition, new Vector3(), 2.5f, new Color(198, 40, 40, 200));
                                                target.SetData(EntityData.PLAYER_JOB_CHECKPOINT, weaponCheckpoint);
                                                target.SendChatMessage(
                                                    Constants.COLOR_INFO + InfoRes.weapon_position_mark);
                                                target.TriggerEvent("showWeaponCheckpoint", weaponPosition);
                                                break;
                                            }

                                    // Send the message to the player
                                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.trunk_stored_items);
                                }
                                else if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                                    int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                                    var rightHand = Globals.GetItemInEntity(playerId, Constants.ITEM_ENTITY_RIGHT_HAND);
                                    rightHand.ownerIdentifier = vehicle.GetData(EntityData.VEHICLE_ID);
                                    rightHand.ownerEntity = Constants.ITEM_ENTITY_VEHICLE;

                                    // If it's a weapon, we remove it from the player
                                    if (int.TryParse(rightHand.hash, out var itemHash) == false) {
                                        var weapon = NAPI.Util.WeaponNameToModel(rightHand.hash);
                                        player.RemoveWeapon(weapon);
                                    }

                                    Task.Factory.StartNew(() => {
                                        // Update item into database
                                        Database.UpdateItem(rightHand);
                                    });

                                    // Send the message to the player
                                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.trunk_stored_items);
                                }
                                else {
                                    // Get player's inventory
                                    inventory = Globals.GetPlayerInventoryAndWeapons(player);

                                    if (inventory.Count > 0) {
                                        player.SetData(EntityData.PLAYER_OPENED_TRUNK, vehicle);
                                        player.TriggerEvent("showPlayerInventory", NAPI.Util.ToJson(inventory),
                                            Constants.INVENTORY_TARGET_VEHICLE_PLAYER);
                                    }
                                    else {
                                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_items_inventory);
                                    }
                                }
                            }

                            break;
                        case Commands.ARG_WITHDRAW:
                            if (!doorState[Constants.VEHICLE_TRUNK]) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_trunk_closed);
                            }
                            else if (IsVehicleTrunkInUse(vehicle)) {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_trunk_in_use);
                            }
                            else {
                                // Load items into the trunk
                                inventory = Globals.GetVehicleTrunkInventory(vehicle);
                                if (inventory.Count > 0) {
                                    player.SetData(EntityData.PLAYER_OPENED_TRUNK, vehicle);
                                    player.TriggerEvent("showPlayerInventory", NAPI.Util.ToJson(inventory),
                                        Constants.INVENTORY_TARGET_VEHICLE_TRUNK);
                                }
                                else {
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_items_trunk);
                                }
                            }

                            break;
                        default:
                            player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_TRUNK_COMMAND);
                            break;
                    }
                }
                else {
                    // There's no vehicle near
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_vehicles_near);
                }
            }
        }

        [Command(Commands.COM_KEYS, Commands.HLP_KEYS_COMMAND, GreedyArg = true)]
        public void KeysCommand(Client player, string action, int vehicleId, string targetString = "") {
            Vehicle vehicle = null;

            // Get lent keys
            string playerKeys = player.GetData(EntityData.PLAYER_VEHICLE_KEYS);
            var playerKeysArray = playerKeys.Split(',');

            switch (action.ToLower()) {
                case Commands.ARG_SEE:
                    foreach (var key in playerKeysArray)
                        if (int.Parse(key) == vehicleId) {
                            vehicle = GetVehicleById(vehicleId);

                            if (vehicle != null) {
                                string model = vehicle.GetData(EntityData.VEHICLE_MODEL);
                                string owner = vehicle.GetData(EntityData.VEHICLE_OWNER);
                                player.SendChatMessage(Constants.COLOR_INFO +
                                                       string.Format(InfoRes.vehicle_keys_info, vehicleId, model,
                                                           owner));
                            }
                            else {
                                // Check if the vehicle is parked
                                var vehicleModel = Parking.GetParkedVehicleById(vehicleId);

                                if (vehicleModel != null) {
                                    var message = string.Format(InfoRes.vehicle_keys_info, vehicleId,
                                        vehicleModel.model, vehicleModel.owner);
                                    player.SendChatMessage(Constants.COLOR_INFO + message);
                                }
                                else {
                                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_not_exists);
                                }
                            }

                            return;
                        }

                    // The player doesn't have the keys
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                    break;
                case Commands.ARG_LEND:
                    vehicle = GetVehicleById(vehicleId);

                    if (vehicle == null) {
                        // Check if the vehicle is parked
                        var vehicleModel = Parking.GetParkedVehicleById(vehicleId);

                        if (vehicle == null) {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_not_exists);
                            return;
                        }
                    }

                    if (!HasPlayerVehicleKeys(player, vehicle)) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                    }
                    else {
                        if (targetString.Length > 0) {
                            var target = int.TryParse(targetString, out var targetId)
                                ? Globals.GetPlayerById(targetId)
                                : NAPI.Player.GetPlayerFromName(targetString);

                            if (target != null && target.Position.DistanceTo(player.Position) < 5.0f) {
                                string targetKeys = target.GetData(EntityData.PLAYER_VEHICLE_KEYS);
                                var targetKeysArray = targetKeys.Split(',');
                                for (var i = 0; i < targetKeysArray.Length; i++)
                                    if (int.Parse(targetKeysArray[i]) == 0) {
                                        targetKeysArray[i] = vehicleId.ToString();
                                        var playerMessage = string.Format(InfoRes.vehicle_keys_given, target.Name);
                                        var targetMessage = string.Format(InfoRes.vehicle_keys_received, player.Name);
                                        target.SetData(EntityData.PLAYER_VEHICLE_KEYS,
                                            string.Join(",", targetKeysArray));
                                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                                        return;
                                    }

                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_keys_full);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_KEYS_COMMAND);
                        }
                    }

                    break;
                case Commands.ARG_DROP:
                    for (var i = 0; i < playerKeysArray.Length; i++)
                        if (playerKeysArray[i] == vehicleId.ToString()) {
                            playerKeysArray[i] = "0";
                            Array.Sort(playerKeysArray);
                            Array.Reverse(playerKeysArray);
                            player.SetData(EntityData.PLAYER_VEHICLE_KEYS, string.Join(",", playerKeysArray));
                            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.vehicle_keys_thrown);
                            return;
                        }

                    // Send a message telling that keys are not found
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                    break;
                default:
                    player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_KEYS_COMMAND);
                    break;
            }
        }

        [Command(Commands.COM_LOCATE, Commands.HLP_LOCATE_COMMAND)]
        public void LocateCommand(Client player, int vehicleId) {
            var vehicle = GetVehicleById(vehicleId);

            if (vehicle == null) {
                // Check if the vehicle is parked
                var vehModel = Parking.GetParkedVehicleById(vehicleId);

                if (vehModel != null) {
                    if (HasPlayerVehicleKeys(player, vehModel) == false) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                    }
                    else {
                        var parking = Parking.GetParkingById(vehModel.parking);

                        // Set the checkpoint into the zone
                        var locationCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4, parking.position,
                            new Vector3(0.0f, 0.0f, 0.0f), 2.5f, new Color(198, 40, 40, 200));
                        player.SetData(EntityData.PLAYER_PARKED_VEHICLE, locationCheckpoint);
                        player.TriggerEvent("locateVehicle", parking.position);

                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.vehicle_parked);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_not_exists);
                }
            }
            else {
                if (HasPlayerVehicleKeys(player, vehicle) == false)
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                else
                    foreach (var veh in NAPI.Pools.GetAllVehicles())
                        if (veh.GetData(EntityData.VEHICLE_ID) == vehicleId) {
                            // Set the checkpoint into the zone
                            var vehiclePosition = veh.Position;
                            var locationCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4, vehiclePosition,
                                new Vector3(0.0f, 0.0f, 0.0f), 2.5f, new Color(198, 40, 40, 200));
                            player.SetData(EntityData.PLAYER_PARKED_VEHICLE, locationCheckpoint);
                            player.TriggerEvent("locateVehicle", vehiclePosition);

                            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.vehicle_parked);
                            break;
                        }
            }
        }

        [Command(Commands.COM_REFUEL, Commands.HLP_FUEL_COMMAND)]
        public void RefuelCommand(Client player, int amount) {
            foreach (var business in Business.businessList)
                if (business.type == Constants.BUSINESS_TYPE_GAS_STATION &&
                    player.Position.DistanceTo(business.position) < 20.5f) {
                    var vehicle = GetClosestVehicle(player);

                    float currentGas = vehicle.GetData(EntityData.VEHICLE_GAS);
                    int faction = player.GetData(EntityData.PLAYER_FACTION);
                    int job = player.GetData(EntityData.PLAYER_JOB);

                    if (vehicle == null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_vehicles_near);
                    }
                    else if (player.Vehicle != null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_refuel_into_vehicle);
                    }
                    else if (vehicle.GetData(EntityData.VEHICLE_REFUELING) != null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_refueling);
                    }
                    else if (player.GetData(EntityData.PLAYER_REFUELING) != null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_refueling);
                    }
                    else if (vehicle.EngineStatus) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.engine_on);
                    }
                    else if (currentGas == 50.0f) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.vehicle_tank_full);
                    }
                    else {
                        int vehicleFaction = vehicle.GetData(EntityData.VEHICLE_FACTION);
                        if (HasPlayerVehicleKeys(player, vehicle) || vehicleFaction == faction ||
                            vehicleFaction + 100 == job) {
                            var gasRefueled = 0.0f;
                            var gasLeft = 50.0f - currentGas;
                            var maxMoney = (int) Math.Ceiling(gasLeft * Constants.PRICE_GAS * business.multiplier);
                            int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);

                            if (amount == 0 || amount > maxMoney) {
                                amount = maxMoney;
                                gasRefueled = gasLeft;
                            }
                            else if (amount > 0) {
                                gasRefueled = amount / (Constants.PRICE_GAS * business.multiplier);
                            }

                            if (amount > 0 && playerMoney >= amount) {
                                vehicle.SetData(EntityData.VEHICLE_GAS, currentGas + gasRefueled);
                                player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney - amount);

                                player.SetData(EntityData.PLAYER_REFUELING, vehicle);
                                vehicle.SetData(EntityData.VEHICLE_REFUELING, player);

                                // Timer called when vehicle's refueled
                                var gasTimer = new Timer(OnVehicleRefueled, vehicle, (int) Math.Round(gasLeft * 1000),
                                    Timeout.Infinite);
                                gasTimerList.Add(player.Value, gasTimer);

                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.vehicle_refueling);
                            }
                            else {
                                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                        }
                    }

                    return;
                }

            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_fuel_station_near);
        }

        [Command(Commands.COM_FILL)]
        public void FillCommand(Client player) {
            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
                var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;
                var item = Globals.GetItemModelFromId(itemId);

                if (item.hash == Constants.ITEM_HASH_JERRYCAN) {
                    var vehicle = GetClosestVehicle(player);
                    if (vehicle != null) {
                        if (HasPlayerVehicleKeys(player, vehicle) ||
                            player.GetData(EntityData.PLAYER_JOB) == Constants.JOB_MECHANIC) {
                            float gas = vehicle.GetData(EntityData.VEHICLE_GAS);
                            vehicle.SetData(EntityData.VEHICLE_GAS,
                                gas + Constants.GAS_CAN_LITRES > 50.0f ? 50.0f : gas + Constants.GAS_CAN_LITRES);

                            // Remove the item from the hand
                            NAPI.ClientEvent.TriggerClientEventInDimension(player.Dimension, "dettachItemFromPlayer",
                                player.Value);
                            player.ResetSharedData(EntityData.PLAYER_RIGHT_HAND);

                            Task.Factory.StartNew(() => {
                                // Remove the item
                                Database.RemoveItem(item.id);
                                Globals.itemList.Remove(item);
                            });

                            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.vehicle_refilled);
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_car_keys);
                        }
                    }
                    else {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_vehicles_near);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_no_jerrycan);
                }
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.right_hand_empty);
            }
        }

        [Command(Commands.COM_SCRAP)]
        public void ScrapCommand(Client player) {
            if (player.VehicleSeat == (int) VehicleSeat.Driver) {
                var parking = Parking.GetClosestParking(player, 3.5f);
                if (parking != null && parking.type == Constants.PARKING_TYPE_SCRAPYARD) {
                    var vehicle = player.Vehicle;
                    if (vehicle.GetData(EntityData.VEHICLE_OWNER) == player.Name) {
                        // Get vehicle basic data
                        int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
                        int vehiclePrice = vehicle.GetData(EntityData.VEHICLE_PRICE);
                        float vehicleKms = vehicle.GetData(EntityData.VEHICLE_KMS);

                        // Get player's money
                        int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);

                        // Calculate amount won
                        var vehicleMaxValue = (int) Math.Round(vehiclePrice * 0.5f);
                        var vehicleMinValue = (int) Math.Round(vehiclePrice * 0.1f);
                        var vehicleReduction = vehicleKms / Constants.REDUCTION_PER_KMS / 1000;
                        var amountGiven = vehicleMaxValue - (int) Math.Round(vehicleReduction / 100 * vehicleMaxValue);

                        if (amountGiven < vehicleMinValue) amountGiven = vehicleMinValue;

                        // Payment to the player
                        player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney + amountGiven);

                        player.WarpOutOfVehicle();
                        vehicle.Delete();

                        Task.Factory.StartNew(() => {
                            // Delete the vehicle
                            Database.RemoveVehicle(vehicleId);
                        });

                        var message = string.Format(SuccRes.vehicle_scrapyard, amountGiven);
                        player.SendChatMessage(Constants.COLOR_SUCCESS + message);
                    }
                    else {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_veh_owner);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_scrapyard_near);
                }
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_vehicle_driving);
            }
        }
    }
}