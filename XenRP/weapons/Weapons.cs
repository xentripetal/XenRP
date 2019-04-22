using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.database;
using XenRP.factions;
using XenRP.globals;
using XenRP.messages.administration;
using XenRP.messages.error;
using XenRP.messages.information;
using XenRP.model;

namespace XenRP.weapons {
    public class Weapons : Script {
        private static Timer weaponTimer;
        private static List<Timer> vehicleWeaponTimer;
        public static List<WeaponCrateModel> weaponCrateList;

        public static void GivePlayerWeaponItems(Client player) {
            var itemId = 0;
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
            foreach (var item in Globals.itemList)
                if (!int.TryParse(item.hash, out itemId) && item.ownerIdentifier == playerId &&
                    item.ownerEntity == Constants.ITEM_ENTITY_WHEEL) {
                    var weaponHash = NAPI.Util.WeaponNameToModel(item.hash);
                    player.GiveWeapon(weaponHash, 0);
                    player.SetWeaponAmmo(weaponHash, item.amount);
                }
        }

        public static void GivePlayerNewWeapon(Client player, WeaponHash weapon, int bullets, bool licensed) {
            // Create weapon model
            var weaponModel = new ItemModel();
            {
                weaponModel.hash = weapon.ToString();
                weaponModel.amount = bullets;
                weaponModel.ownerEntity = Constants.ITEM_ENTITY_WHEEL;
                weaponModel.ownerIdentifier = player.GetData(EntityData.PLAYER_SQL_ID);
                weaponModel.position = new Vector3(0.0f, 0.0f, 0.0f);
                weaponModel.dimension = 0;
            }

            Task.Factory.StartNew(() => {
                weaponModel.id = Database.AddNewItem(weaponModel);
                Globals.itemList.Add(weaponModel);

                // Give the weapon to the player
                player.GiveWeapon(weapon, 0);
                player.SetWeaponAmmo(weapon, bullets);

                if (licensed) Database.AddLicensedWeapon(weaponModel.id, player.Name);
            });
        }

        public static string GetGunAmmunitionType(WeaponHash weapon) {
            // Get the ammunition type given a weapon
            var gunModel = Constants.GUN_LIST.Where(gun => weapon == gun.weapon).FirstOrDefault();

            return gunModel == null ? string.Empty : gunModel.ammunition;
        }

        public static int GetGunAmmunitionCapacity(WeaponHash weapon) {
            // Get the capacity from a weapons's clip magazine
            var gunModel = Constants.GUN_LIST.Where(gun => weapon == gun.weapon).FirstOrDefault();

            return gunModel == null ? 0 : gunModel.capacity;
        }

        public static ItemModel GetEquippedWeaponItemModelByHash(int playerId, WeaponHash weapon) {
            // Get the equipped weapon's item model
            return Globals.itemList.Where(itemModel =>
                    itemModel.ownerIdentifier == playerId &&
                    (itemModel.ownerEntity == Constants.ITEM_ENTITY_WHEEL ||
                     itemModel.ownerEntity == Constants.ITEM_ENTITY_RIGHT_HAND) && weapon.ToString() == itemModel.hash)
                .FirstOrDefault();
        }

        public static WeaponCrateModel GetClosestWeaponCrate(Client player, float distance = 1.5f) {
            // Get the closest weapon crate
            return weaponCrateList.Where(weaponCrateModel =>
                player.Position.DistanceTo(weaponCrateModel.position) < distance &&
                weaponCrateModel.carriedEntity == string.Empty).FirstOrDefault();
        }

        public static WeaponCrateModel GetPlayerCarriedWeaponCrate(int playerId) {
            // Get the weapon crate carried by the player
            return weaponCrateList.Where(weaponCrateModel =>
                weaponCrateModel.carriedEntity == Constants.ITEM_ENTITY_PLAYER &&
                weaponCrateModel.carriedIdentifier == playerId).FirstOrDefault();
        }

        public static void WeaponsPrewarn() {
            // Send the warning message to all factions
            foreach (var player in NAPI.Pools.GetAllPlayers())
                if (player.GetData(EntityData.PLAYER_PLAYING) != null &&
                    player.GetData(EntityData.PLAYER_FACTION) > Constants.LAST_STATE_FACTION)
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.weapon_prewarn);

            // Timer for the next warning
            weaponTimer = new Timer(OnWeaponPrewarn, null, 600000, Timeout.Infinite);
        }

        public static void OnPlayerDisconnected(Client player) {
            var weaponCrate = GetPlayerCarriedWeaponCrate(player.Value);

            if (weaponCrate != null) {
                weaponCrate.position = new Vector3(player.Position.X, player.Position.Y, player.Position.X - 1.0f);
                weaponCrate.carriedEntity = string.Empty;
                weaponCrate.carriedIdentifier = 0;

                // Place the crate on the floor
                weaponCrate.crateObject.Detach();
                weaponCrate.crateObject.Position = weaponCrate.position;
            }
        }

        private static List<Vector3> GetRandomWeaponSpawns(int spawnPosition) {
            var random = new Random();
            var weaponSpawns = new List<Vector3>();
            var cratesInSpawn = GetSpawnsInPosition(spawnPosition);

            while (weaponSpawns.Count < Constants.MAX_CRATES_SPAWN) {
                var crateSpawn = cratesInSpawn[random.Next(cratesInSpawn.Count)].position;
                if (weaponSpawns.Contains(crateSpawn) == false) weaponSpawns.Add(crateSpawn);
            }

            return weaponSpawns;
        }

        private static List<CrateSpawnModel> GetSpawnsInPosition(int spawnPosition) {
            var crateSpawnList = new List<CrateSpawnModel>();
            foreach (var crateSpawn in Constants.CRATE_SPAWN_LIST)
                if (crateSpawn.spawnPoint == spawnPosition)
                    crateSpawnList.Add(crateSpawn);
            return crateSpawnList;
        }

        private static CrateContentModel GetRandomCrateContent(int type, int chance) {
            var crateContent = new CrateContentModel();

            foreach (var weaponAmmo in Constants.WEAPON_CHANCE_LIST)
                if (weaponAmmo.type == type && weaponAmmo.minChance <= chance && weaponAmmo.maxChance >= chance) {
                    crateContent.item = weaponAmmo.hash;
                    crateContent.amount = weaponAmmo.amount;
                    break;
                }

            return crateContent;
        }

        private static void OnWeaponPrewarn(object unused) {
            weaponTimer.Dispose();

            var currentSpawn = 0;
            weaponCrateList = new List<WeaponCrateModel>();

            var random = new Random();
            var spawnPosition = random.Next(Constants.MAX_WEAPON_SPAWNS);

            // Get crates' spawn points
            var weaponSpawns = GetRandomWeaponSpawns(spawnPosition);

            NAPI.Task.Run(() => {
                foreach (var spawn in weaponSpawns) {
                    // Calculate weapon or ammunition crate
                    var type = currentSpawn % 2;
                    var chance = random.Next(type == 0 ? Constants.MAX_WEAPON_CHANCE : Constants.MAX_AMMO_CHANCE);
                    var crateContent = GetRandomCrateContent(type, chance);

                    // We create the crate
                    var weaponCrate = new WeaponCrateModel();
                    {
                        weaponCrate.contentItem = crateContent.item;
                        weaponCrate.contentAmount = crateContent.amount;
                        weaponCrate.position = spawn;
                        weaponCrate.carriedEntity = string.Empty;
                        weaponCrate.crateObject = NAPI.Object.CreateObject(481432069, spawn, new Vector3(), 0);
                    }

                    weaponCrateList.Add(weaponCrate);
                    currentSpawn++;
                }
            });

            // Warn all the factions about the place
            foreach (var player in NAPI.Pools.GetAllPlayers())
                if (player.GetData(EntityData.PLAYER_PLAYING) != null &&
                    player.GetData(EntityData.PLAYER_FACTION) > Constants.LAST_STATE_FACTION)
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.weapon_spawn_island);

            // Timer to warn the police
            weaponTimer = new Timer(OnPoliceCalled, null, 240000, Timeout.Infinite);
        }

        private static void OnPoliceCalled(object unused) {
            weaponTimer.Dispose();

            // Send the warning message to all the police members
            foreach (var player in NAPI.Pools.GetAllPlayers())
                if (player.GetData(EntityData.PLAYER_PLAYING) != null && Faction.IsPoliceMember(player))
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.weapon_spawn_island);

            // Finish the event
            weaponTimer = new Timer(OnWeaponEventFinished, null, 3600000, Timeout.Infinite);
        }

        private static void OnVehicleUnpackWeapons(object vehicleObject) {
            var vehicle = (Vehicle) vehicleObject;
            int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);

            foreach (var weaponCrate in weaponCrateList)
                if (weaponCrate.carriedEntity == Constants.ITEM_ENTITY_VEHICLE &&
                    weaponCrate.carriedIdentifier == vehicleId) {
                    // Unpack the weapon in the crate
                    var item = new ItemModel();
                    {
                        item.hash = weaponCrate.contentItem;
                        item.amount = weaponCrate.contentAmount;
                        item.ownerEntity = Constants.ITEM_ENTITY_VEHICLE;
                        item.ownerIdentifier = vehicleId;
                    }

                    // Delete the crate
                    weaponCrate.carriedIdentifier = 0;
                    weaponCrate.carriedEntity = string.Empty;

                    Task.Factory.StartNew(() => {
                        item.id = Database.AddNewItem(item);
                        Globals.itemList.Add(item);
                    });
                }

            // Warn driver about unpacked crates
            foreach (var player in NAPI.Pools.GetAllPlayers())
                if (player.GetData(EntityData.PLAYER_VEHICLE) == vehicle) {
                    player.ResetData(EntityData.PLAYER_VEHICLE);
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.weapons_unpacked);
                    break;
                }

            vehicle.ResetData(EntityData.VEHICLE_WEAPON_UNPACKING);
        }

        private static void OnWeaponEventFinished(object unused) {
            NAPI.Task.Run(() => {
                weaponTimer.Dispose();

                foreach (var crate in weaponCrateList)
                    if (crate.crateObject != null)
                        crate.crateObject.Delete();

                // Destroy weapon crates
                weaponCrateList = new List<WeaponCrateModel>();
                weaponTimer = null;
            });
        }

        private int GetVehicleWeaponCrates(int vehicleId) {
            // Get the crates on the vehicle
            return weaponCrateList.Where(w =>
                w.carriedEntity == Constants.ITEM_ENTITY_VEHICLE && w.carriedIdentifier == vehicleId).Count();
        }

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart() {
            vehicleWeaponTimer = new List<Timer>();
            weaponCrateList = new List<WeaponCrateModel>();
        }

        [ServerEvent(Event.PlayerEnterVehicle)]
        public void OnPlayerEnterVehicle(Client player, Vehicle vehicle, sbyte seat) {
            if (vehicle.GetData(EntityData.VEHICLE_ID) != null && player.VehicleSeat == (int) VehicleSeat.Driver) {
                int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
                if (vehicle.GetData(EntityData.VEHICLE_WEAPON_UNPACKING) == null &&
                    GetVehicleWeaponCrates(vehicleId) > 0) {
                    // Mark the delivery point
                    var weaponPosition = new Vector3(-2085.543f, 2600.857f, -0.4712417f);
                    var weaponCheckpoint = NAPI.Checkpoint.CreateCheckpoint(4, weaponPosition,
                        new Vector3(0.0f, 0.0f, 0.0f), 2.5f, new Color(198, 40, 40, 200));
                    player.SetData(EntityData.PLAYER_JOB_COLSHAPE, weaponCheckpoint);
                    player.SendChatMessage(Constants.COLOR_INFO + InfoRes.weapon_position_mark);
                    player.TriggerEvent("showWeaponCheckpoint", weaponPosition);
                }
            }
        }

        [ServerEvent(Event.PlayerExitVehicle)]
        public void OnPlayerExitVehicle(Client player, Vehicle vehicle) {
            if (vehicle.GetData(EntityData.VEHICLE_ID) != null) {
                int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
                if (player.GetData(EntityData.PLAYER_JOB_COLSHAPE) != null && GetVehicleWeaponCrates(vehicleId) > 0)
                    player.TriggerEvent("deleteWeaponCheckpoint");
            }
        }

        [ServerEvent(Event.PlayerEnterCheckpoint)]
        public void OnPlayerEnterCheckpoint(Checkpoint checkpoint, Client player) {
            if (player.GetData(EntityData.PLAYER_JOB_COLSHAPE) != null)
                if (checkpoint == player.GetData(EntityData.PLAYER_JOB_COLSHAPE) &&
                    player.VehicleSeat == (int) VehicleSeat.Driver) {
                    var vehicle = player.Vehicle;
                    int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
                    if (GetVehicleWeaponCrates(vehicleId) > 0) {
                        // Delete the checkpoint
                        Checkpoint weaponCheckpoint = player.GetData(EntityData.PLAYER_JOB_COLSHAPE);
                        player.ResetData(EntityData.PLAYER_JOB_COLSHAPE);
                        player.TriggerEvent("deleteWeaponCheckpoint");
                        weaponCheckpoint.Delete();

                        // Freeze the vehicle
                        vehicle.EngineStatus = false;
                        player.SetData(EntityData.PLAYER_VEHICLE, vehicle);
                        vehicle.SetData(EntityData.VEHICLE_WEAPON_UNPACKING, true);

                        vehicleWeaponTimer.Add(new Timer(OnVehicleUnpackWeapons, vehicle, 60000, Timeout.Infinite));

                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.wait_for_weapons);
                    }
                }
        }

        [ServerEvent(Event.PlayerWeaponSwitch)]
        public void OnPlayerWeaponSwitch(Client player, WeaponHash oldWeapon, WeaponHash newWeapon) {
            if (player.GetData(EntityData.PLAYER_PLAYING) != null) {
                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);

                if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) {
                    string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
                    var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;
                    var item = Globals.GetItemModelFromId(itemId);

                    if (NAPI.Util.WeaponNameToModel(item.hash) == 0) {
                        var weaponItem = GetEquippedWeaponItemModelByHash(playerId, newWeapon);
                        player.GiveWeapon(WeaponHash.Unarmed, 1);
                        return;
                    }
                }

                // Get previous and new weapon models
                var oldWeaponModel = GetEquippedWeaponItemModelByHash(playerId, oldWeapon);
                var currentWeaponModel = GetEquippedWeaponItemModelByHash(playerId, newWeapon);

                if (oldWeaponModel != null) {
                    // Unequip previous weapon
                    oldWeaponModel.ownerEntity = Constants.ITEM_ENTITY_WHEEL;

                    Task.Factory.StartNew(() => {
                        // Update the weapon into the database
                        Database.UpdateItem(oldWeaponModel);
                    });
                }

                if (currentWeaponModel != null) {
                    // Equip new weapon
                    currentWeaponModel.ownerEntity = Constants.ITEM_ENTITY_RIGHT_HAND;

                    Task.Factory.StartNew(() => {
                        // Update the weapon into the database
                        Database.UpdateItem(currentWeaponModel);
                    });
                }

                // Check if it's armed
                if (newWeapon == WeaponHash.Unarmed) {
                    player.ResetSharedData(EntityData.PLAYER_RIGHT_HAND);
                }
                else {
                    // Add the attachment to the player
                    var attachment = new AttachmentModel(currentWeaponModel.id, currentWeaponModel.hash, "IK_R_Hand",
                        new Vector3(), new Vector3());
                    player.SetSharedData(EntityData.PLAYER_RIGHT_HAND, NAPI.Util.ToJson(attachment));
                }
            }
        }

        [RemoteEvent("reloadPlayerWeapon")]
        public void ReloadPlayerWeaponEvent(Client player, int currentBullets) {
            var weapon = player.CurrentWeapon;
            var maxCapacity = GetGunAmmunitionCapacity(weapon);

            if (currentBullets < maxCapacity) {
                var bulletType = GetGunAmmunitionType(weapon);
                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                var bulletItem = Globals.GetPlayerItemModelFromHash(playerId, bulletType);
                if (bulletItem != null) {
                    var bulletsLeft = maxCapacity - currentBullets;
                    if (bulletsLeft >= bulletItem.amount) {
                        currentBullets += bulletItem.amount;

                        Task.Factory.StartNew(() => {
                            Database.RemoveItem(bulletItem.id);
                            Globals.itemList.Remove(bulletItem);
                        });
                    }
                    else {
                        currentBullets += bulletsLeft;
                        bulletItem.amount -= bulletsLeft;

                        Task.Factory.StartNew(() => {
                            // Update the remaining bullets
                            Database.UpdateItem(bulletItem);
                        });
                    }

                    // Add ammunition to the weapon
                    var weaponItem = GetEquippedWeaponItemModelByHash(playerId, weapon);
                    weaponItem.amount = currentBullets;

                    Task.Factory.StartNew(() => {
                        // Update the bullets in the weapon
                        Database.UpdateItem(weaponItem);
                    });

                    // Reload the weapon
                    player.SetWeaponAmmo(weapon, currentBullets);
                    player.TriggerEvent("makePlayerReload");
                }
            }
        }

        [RemoteEvent("updateWeaponBullets")]
        public void UpdateWeaponBullets(Client player, int bullets) {
            // Get the weapon from the hand
            string rightHand = player.GetSharedData(EntityData.PLAYER_RIGHT_HAND).ToString();
            var itemId = NAPI.Util.FromJson<AttachmentModel>(rightHand).itemId;
            var item = Globals.GetItemModelFromId(itemId);

            Task.Factory.StartNew(() => {
                // Set the bullets on the weapon
                item.amount = bullets;

                // Update the remaining bullets
                Database.UpdateItem(item);
            });
        }

        [Command(Commands.COM_WEAPONS_EVENT)]
        public void WeaponsEventCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_ADMIN_RANK) > Constants.STAFF_S_GAME_MASTER) {
                if (weaponTimer == null) {
                    WeaponsPrewarn();
                    player.SendChatMessage(Constants.COLOR_ADMIN_INFO + AdminRes.weapon_event_started);
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.weapon_event_on_course);
                }
            }
        }
    }
}