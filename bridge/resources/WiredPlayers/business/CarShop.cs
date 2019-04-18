﻿using System;
using System.Collections.Generic;
using System.Linq;
using GTANetworkAPI;
using WiredPlayers.globals;
using WiredPlayers.messages.error;
using WiredPlayers.messages.general;
using WiredPlayers.messages.information;
using WiredPlayers.model;
using WiredPlayers.vehicles;

namespace WiredPlayers.business {
    public class CarShop : Script {
        private TextLabel carShopTextLabel;
        private TextLabel motorbikeShopTextLabel;
        private TextLabel shipShopTextLabel;

        private int GetClosestCarShop(Client player, float distance = 2.0f) {
            var carShop = -1;
            if (player.Position.DistanceTo(carShopTextLabel.Position) < distance)
                carShop = 0;
            else if (player.Position.DistanceTo(motorbikeShopTextLabel.Position) < distance)
                carShop = 1;
            else if (player.Position.DistanceTo(shipShopTextLabel.Position) < distance) carShop = 2;
            return carShop;
        }

        private List<CarShopVehicleModel> GetVehicleListInCarShop(int carShop) {
            // Get all the vehicles in the list
            return Constants.CARSHOP_VEHICLE_LIST.Where(vehicle => vehicle.carShop == carShop).ToList();
        }

        private int GetVehiclePrice(VehicleHash vehicleHash) {
            // Get the price of the vehicle
            var carDealerVehicle = Constants.CARSHOP_VEHICLE_LIST.Where(vehicle => vehicle.hash == vehicleHash)
                .FirstOrDefault();

            return carDealerVehicle == null ? 0 : carDealerVehicle.price;
        }

        private string GetVehicleModel(VehicleHash vehicleHash) {
            // Get the price of the vehicle
            var carDealerVehicle = Constants.CARSHOP_VEHICLE_LIST.Where(vehicle => vehicle.hash == vehicleHash)
                .FirstOrDefault();

            return carDealerVehicle == null ? string.Empty : carDealerVehicle.hash.ToString();
        }

        private bool SpawnPurchasedVehicle(Client player, List<Vector3> spawns, VehicleHash vehicleHash,
            int vehiclePrice, string firstColor, string secondColor) {
            for (var i = 0; i < spawns.Count; i++) {
                // Check if the spawn point has a vehicle on it
                var spawnOccupied = NAPI.Pools.GetAllVehicles().Where(veh => spawns[i].DistanceTo(veh.Position) < 2.5f)
                    .Any();

                if (!spawnOccupied) {
                    // Basic data for vehicle creation
                    var vehicleModel = new VehicleModel();
                    {
                        vehicleModel.model = GetVehicleModel(vehicleHash);
                        vehicleModel.plate = string.Empty;
                        vehicleModel.position = spawns[i];
                        vehicleModel.rotation = new Vector3(0.0f, 0.0f, 218.0f);
                        vehicleModel.owner = player.GetData(EntityData.PLAYER_NAME);
                        vehicleModel.colorType = Constants.VEHICLE_COLOR_TYPE_CUSTOM;
                        vehicleModel.firstColor = firstColor;
                        vehicleModel.secondColor = secondColor;
                        vehicleModel.pearlescent = 0;
                        vehicleModel.price = vehiclePrice;
                        vehicleModel.parking = 0;
                        vehicleModel.parked = 0;
                        vehicleModel.engine = 0;
                        vehicleModel.locked = 0;
                        vehicleModel.gas = 50.0f;
                        vehicleModel.kms = 0.0f;
                    }

                    // Creating the purchased vehicle
                    Vehicles.CreateVehicle(player, vehicleModel, false);

                    return true;
                }
            }

            return false;
        }

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart() {
            // Car dealer creation
            carShopTextLabel = NAPI.TextLabel.CreateTextLabel("/" + Commands.COM_CATALOG,
                new Vector3(-215.6841f, 6219.168f, 31.49166f), 10.0f, 0.5f, 4, new Color(190, 235, 100));
            NAPI.TextLabel.CreateTextLabel(GenRes.catalog_help, new Vector3(-215.6841f, 6219.168f, 31.39166f), 10.0f,
                0.5f, 4, new Color(255, 255, 255));
            var carShopBlip = NAPI.Blip.CreateBlip(new Vector3(-215.6841f, 6219.168f, 31.49166f));
            carShopBlip.Name = GenRes.car_dealer;
            carShopBlip.ShortRange = true;
            carShopBlip.Sprite = 225;

            // Motorcycle dealer creation
            motorbikeShopTextLabel = NAPI.TextLabel.CreateTextLabel("/" + Commands.COM_CATALOG,
                new Vector3(2129.325f, 4794.172f, 40.88499f), 10.0f, 0.5f, 4, new Color(190, 235, 100));
            NAPI.TextLabel.CreateTextLabel(GenRes.catalog_help, new Vector3(2129.325f, 4794.172f, 40.78499f), 10.0f,
                0.5f, 4, new Color(255, 255, 255));
            var motorbikeShopBlip = NAPI.Blip.CreateBlip(new Vector3(2129.325f, 4794.172f, 40.88499f));
            motorbikeShopBlip.Name = GenRes.motorcycle_dealer;
            motorbikeShopBlip.ShortRange = true;
            motorbikeShopBlip.Sprite = 226;

            // Boat dealer creation
            shipShopTextLabel = NAPI.TextLabel.CreateTextLabel("/" + Commands.COM_CATALOG,
                new Vector3(1225.489f, 2712.924f, 37.76197f), 10.0f, 0.5f, 4, new Color(190, 235, 100));
            NAPI.TextLabel.CreateTextLabel(GenRes.catalog_help, new Vector3(1225.489f, 2712.924f, 37.66197f), 10.0f,
                0.5f, 4, new Color(255, 255, 255));
            var shipShopBlip = NAPI.Blip.CreateBlip(new Vector3(1225.489f, 2712.924f, 37.76197f));
            shipShopBlip.Name = GenRes.boat_dealer;
            shipShopBlip.ShortRange = true;
            shipShopBlip.Sprite = 455;
        }

        [ServerEvent(Event.PlayerEnterCheckpoint)]
        public void OnPlayerEnterCheckpoint(Checkpoint checkpoint, Client player) {
            if (player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE) != null &&
                player.GetData(EntityData.PLAYER_TESTING_VEHICLE) != null)
                if (player.IsInVehicle && player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE) == checkpoint) {
                    Vehicle vehicle = player.GetData(EntityData.PLAYER_TESTING_VEHICLE);
                    if (player.Vehicle == vehicle) {
                        // Stop the vehicle's speedometer
                        player.TriggerEvent("removeSpeedometer");

                        // We destroy the vehicle and the checkpoint
                        Checkpoint testCheckpoint = player.GetData(EntityData.PLAYER_DRIVING_COLSHAPE);
                        player.WarpOutOfVehicle();
                        testCheckpoint.Delete();
                        vehicle.Delete();

                        // Variable cleaning
                        player.ResetData(EntityData.PLAYER_TESTING_VEHICLE);
                        player.ResetData(EntityData.PLAYER_DRIVING_COLSHAPE);

                        // Deleting checkpoint
                        player.TriggerEvent("deleteCarshopCheckpoint");
                    }
                }
        }

        [RemoteEvent("purchaseVehicle")]
        public void PurchaseVehicleEvent(Client player, string hash, string firstColor, string secondColor) {
            var carShop = GetClosestCarShop(player);
            var vehicleHash = (VehicleHash) uint.Parse(hash);
            var vehiclePrice = GetVehiclePrice(vehicleHash);

            if (vehiclePrice > 0 && player.GetSharedData(EntityData.PLAYER_BANK) >= vehiclePrice) {
                var vehicleSpawned = false;

                switch (carShop) {
                    case 0:
                        // Create a new car
                        vehicleSpawned = SpawnPurchasedVehicle(player, Constants.CARSHOP_SPAWNS, vehicleHash,
                            vehiclePrice, firstColor, secondColor);
                        break;
                    case 1:
                        // Create a new motorcycle
                        vehicleSpawned = SpawnPurchasedVehicle(player, Constants.BIKESHOP_SPAWNS, vehicleHash,
                            vehiclePrice, firstColor, secondColor);
                        break;
                    case 2:
                        // Create a new ship
                        vehicleSpawned = SpawnPurchasedVehicle(player, Constants.SHIP_SPAWNS, vehicleHash, vehiclePrice,
                            firstColor, secondColor);
                        break;
                }

                if (!vehicleSpawned) player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.carshop_spawn_occupied);
            }
            else {
                var message = string.Format(ErrRes.carshop_no_money, vehiclePrice);
                player.SendChatMessage(Constants.COLOR_ERROR + message);
            }
        }

        [RemoteEvent("testVehicle")]
        public void TestVehicleEvent(Client player, string hash) {
            Vehicle vehicle = null;
            Checkpoint testFinishCheckpoint = null;
            var vehicleModel = (VehicleHash) uint.Parse(hash);

            switch (GetClosestCarShop(player)) {
                case 0:
                    vehicle = NAPI.Vehicle.CreateVehicle(vehicleModel, new Vector3(-51.54087f, -1076.941f, 26.94754f),
                        75.0f, new Color(0, 0, 0), new Color(0, 0, 0));
                    testFinishCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4,
                        new Vector3(-28.933f, -1085.566f, 25.565f), new Vector3(0.0f, 0.0f, 0.0f), 2.5f,
                        new Color(198, 40, 40, 200));
                    break;
                case 1:
                    vehicle = NAPI.Vehicle.CreateVehicle(vehicleModel, new Vector3(307.0036f, -1162.707f, 29.29191f),
                        180.0f, new Color(0, 0, 0), new Color(0, 0, 0));
                    testFinishCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4,
                        new Vector3(267.412f, -1159.755f, 28.263f), new Vector3(0.0f, 0.0f, 0.0f), 2.5f,
                        new Color(198, 40, 40, 200));
                    break;
                case 2:
                    vehicle = NAPI.Vehicle.CreateVehicle(vehicleModel, new Vector3(-717.3467f, -1319.792f, -0.42f),
                        180.0f, new Color(0, 0, 0), new Color(0, 0, 0));
                    testFinishCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4,
                        new Vector3(-711.267f, -1351.501f, -1.359f), new Vector3(0.0f, 0.0f, 0.0f), 2.5f,
                        new Color(198, 40, 40, 200));
                    break;
            }

            // Vehicle variable initialization
            vehicle.SetData(EntityData.VEHICLE_KMS, 0.0f);
            vehicle.SetData(EntityData.VEHICLE_GAS, 50.0f);
            vehicle.SetData(EntityData.VEHICLE_TESTING, true);
            vehicle.SetSharedData(EntityData.VEHICLE_DOORS_STATE,
                NAPI.Util.ToJson(new List<bool> {false, false, false, false, false, false}));
            player.SetData(EntityData.PLAYER_TESTING_VEHICLE, vehicle);
            player.SetIntoVehicle(vehicle, (int) VehicleSeat.Driver);
            vehicle.EngineStatus = true;

            // Adding the checkpoint
            player.SetData(EntityData.PLAYER_DRIVING_COLSHAPE, testFinishCheckpoint);
            player.TriggerEvent("showCarshopCheckpoint", testFinishCheckpoint.Position);

            // Confirmation message sent to the player
            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_test_vehicle);
        }

        [Command(Commands.COM_CATALOG)]
        public void CatalogoCommand(Client player) {
            var carShop = GetClosestCarShop(player);

            if (carShop > -1) {
                // We get the vehicle list
                var carList = GetVehicleListInCarShop(carShop);

                // Getting the speed for each vehicle in the list
                foreach (var carShopVehicle in carList) {
                    carShopVehicle.model = carShopVehicle.hash.ToString();
                    var vehicleHash = NAPI.Util.VehicleNameToModel(carShopVehicle.model);
                    carShopVehicle.speed = (int) Math.Round(NAPI.Vehicle.GetVehicleMaxSpeed(vehicleHash) * 3.6f);
                }

                // We show the catalog
                player.TriggerEvent("showVehicleCatalog", NAPI.Util.ToJson(carList), carShop);
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_in_carshop);
            }
        }
    }
}