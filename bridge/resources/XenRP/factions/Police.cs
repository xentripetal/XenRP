﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.database;
using XenRP.drivingschool;
using XenRP.globals;
using XenRP.messages.error;
using XenRP.messages.general;
using XenRP.messages.information;
using XenRP.model;
using XenRP.vehicles;
using XenRP.weapons;

namespace XenRP.factions {
    public class Police : Script {
        public static List<CrimeModel> crimeList;
        public static List<PoliceControlModel> policeControlList;
        private readonly TextLabel equipmentLabel;

        public Police() {
            // Initialize reinforces updater
            var reinforcesTimer = new Timer(UpdateReinforcesRequests, null, 250, 250);
            equipmentLabel = NAPI.TextLabel.CreateTextLabel("/" + Commands.COM_EQUIPMENT,
                new Vector3(450.8223, -992.0941, 30.78958), 10.0f, 0.5f, 4, new Color(190, 235, 100), false, 0);
            NAPI.TextLabel.CreateTextLabel(GenRes.equipment_help, new Vector3(450.8223, -992.0941, 30.68958), 10.0f,
                0.5f, 4, new Color(255, 255, 255), false, 0);

            // Create blips
            var policePaleto = NAPI.Blip.CreateBlip(new Vector3(-436.4092f, 6022.894f, 31.22858f));
            policePaleto.Name = GenRes.police_station;
            policePaleto.ShortRange = true;
            policePaleto.Sprite = 60;

            var policeSandy = NAPI.Blip.CreateBlip(new Vector3(1857.865f, 3679.907f, 33.55733f));
            policeSandy.Name = GenRes.police_station;
            policeSandy.ShortRange = true;
            policeSandy.Sprite = 60;
        }

        private List<string> GetDifferentPoliceControls() {
            var policeControls = new List<string>();

            foreach (var policeControl in policeControlList)
                if (policeControls.Contains(policeControl.name) == false && policeControl.name != string.Empty)
                    policeControls.Add(policeControl.name);

            return policeControls;
        }

        private void RemoveClosestPoliceControlItem(Client player, int hash) {
            // Get the closest police control item
            var policeControl = policeControlList.Where(control =>
                control.controlObject != null && control.controlObject.Position.DistanceTo(player.Position) < 2.0f &&
                control.item == hash).FirstOrDefault();

            if (policeControl != null) {
                policeControl.controlObject.Delete();
                policeControl.controlObject = null;
            }
        }

        private void UpdateReinforcesRequests(object unused) {
            var policeReinforces = new List<ReinforcesModel>();
            var policeMembers = NAPI.Pools.GetAllPlayers().Where(x => Faction.IsPoliceMember(x)).ToList();

            foreach (var police in policeMembers)
                if (police.GetData(EntityData.PLAYER_REINFORCES) != null) {
                    var reinforces = new ReinforcesModel(police.Value, police.Position);
                    policeReinforces.Add(reinforces);
                }

            var reinforcesJsonList = NAPI.Util.ToJson(policeReinforces);

            foreach (var police in policeMembers)
                if (police.GetData(EntityData.PLAYER_PLAYING) != null)
                    police.TriggerEvent("updatePoliceReinforces", reinforcesJsonList);
        }

        [RemoteEvent("applyCrimesToPlayer")]
        public void ApplyCrimesToPlayerEvent(Client player, string crimeJson) {
            int fine = 0, jail = 0;
            Client target = player.GetData(EntityData.PLAYER_INCRIMINATED_TARGET);
            var crimeList = NAPI.Util.FromJson<List<CrimeModel>>(crimeJson);

            // Calculate fine amount and jail time
            foreach (var crime in crimeList) {
                fine += crime.fine;
                jail += crime.jail;
            }

            var random = new Random();
            target.Position = Constants.JAIL_SPAWNS[random.Next(3)];
            player.SetData(EntityData.PLAYER_INCRIMINATED_TARGET, target);

            // Remove money and jail the player
            int money = target.GetSharedData(EntityData.PLAYER_MONEY);
            target.SetSharedData(EntityData.PLAYER_MONEY, money - fine);
            target.SetData(EntityData.PLAYER_JAIL_TYPE, Constants.JAIL_TYPE_IC);
            target.SetData(EntityData.PLAYER_JAILED, jail);
        }

        [RemoteEvent("policeControlSelected")]
        public void PoliceControlSelectedEvent(Client player, string policeControl) {
            if (player.GetSharedData(EntityData.PLAYER_POLICE_CONTROL) == Constants.ACTION_LOAD) {
                foreach (var policeControlModel in policeControlList)
                    if (policeControlModel.controlObject == null && policeControlModel.name == policeControl)
                        policeControlModel.controlObject = NAPI.Object.CreateObject(policeControlModel.item,
                            policeControlModel.position, policeControlModel.rotation);
            }
            else if (player.GetSharedData(EntityData.PLAYER_POLICE_CONTROL) == Constants.ACTION_SAVE) {
                var copiedPoliceControlModels = new List<PoliceControlModel>();
                var deletedPoliceControlModels = new List<PoliceControlModel>();
                foreach (var policeControlModel in policeControlList)
                    if (policeControlModel.controlObject != null && policeControlModel.name != policeControl) {
                        if (policeControlModel.name != string.Empty) {
                            var policeControlCopy = policeControlModel;
                            policeControlCopy.name = policeControl;

                            Task.Factory.StartNew(() => {
                                policeControlCopy.id = Database.AddPoliceControlItem(policeControlCopy);
                                copiedPoliceControlModels.Add(policeControlCopy);
                            });
                        }
                        else {
                            policeControlModel.name = policeControl;

                            Task.Factory.StartNew(() => {
                                // Add the new element
                                policeControlModel.id = Database.AddPoliceControlItem(policeControlModel);
                            });
                        }
                    }
                    else if (policeControlModel.controlObject == null && policeControlModel.name == policeControl) {
                        Task.Factory.StartNew(() => {
                            Database.DeletePoliceControlItem(policeControlModel.id);
                            deletedPoliceControlModels.Add(policeControlModel);
                        });
                    }

                policeControlList.AddRange(copiedPoliceControlModels);
                policeControlList = policeControlList.Except(deletedPoliceControlModels).ToList();
            }
            else {
                foreach (var policeControlModel in policeControlList)
                    if (policeControlModel.controlObject != null && policeControlModel.name == policeControl)
                        policeControlModel.controlObject.Delete();
                policeControlList.RemoveAll(control => control.name == policeControl);

                Task.Factory.StartNew(() => {
                    // Delete the police control
                    Database.DeletePoliceControl(policeControl);
                });
            }
        }

        [RemoteEvent("updatePoliceControlName")]
        public void UpdatePoliceControlNameEvent(Client player, string policeControlSource,
            string policeControlTarget) {
            if (player.GetSharedData(EntityData.PLAYER_POLICE_CONTROL) == Constants.ACTION_SAVE) {
                var copiedPoliceControlModels = new List<PoliceControlModel>();
                var deletedPoliceControlModels = new List<PoliceControlModel>();
                foreach (var policeControlModel in policeControlList)
                    if (policeControlModel.controlObject != null && policeControlModel.name != policeControlTarget) {
                        if (policeControlModel.name != string.Empty) {
                            var policeControlCopy = policeControlModel.Copy();
                            policeControlModel.controlObject = null;
                            policeControlCopy.name = policeControlTarget;

                            Task.Factory.StartNew(() => {
                                policeControlCopy.id = Database.AddPoliceControlItem(policeControlCopy);
                                copiedPoliceControlModels.Add(policeControlCopy);
                            });
                        }
                        else {
                            policeControlModel.name = policeControlTarget;

                            Task.Factory.StartNew(() => {
                                // Add new element to the control
                                policeControlModel.id = Database.AddPoliceControlItem(policeControlModel);
                            });
                        }
                    }

                policeControlList.AddRange(copiedPoliceControlModels);
            }
            else {
                policeControlList.Where(s => s.name == policeControlSource).ToList()
                    .ForEach(t => t.name = policeControlTarget);

                Task.Factory.StartNew(() => {
                    // Rename the control
                    Database.RenamePoliceControl(policeControlSource, policeControlTarget);
                });
            }
        }

        [Command(Commands.COM_CHECK)]
        public void CheckCommand(Client player) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
            else {
                var vehicle = Vehicles.GetClosestVehicle(player, 3.5f);
                if (vehicle == null) {
                    int vehicleId = vehicle.GetData(EntityData.VEHICLE_ID);
                    var checkTitle = string.Format(GenRes.vehicle_check_title, vehicleId);
                    string model = vehicle.GetData(EntityData.VEHICLE_MODEL);
                    string plate = vehicle.GetData(EntityData.VEHICLE_PLATE);
                    string owner = vehicle.GetData(EntityData.VEHICLE_OWNER);
                    player.SendChatMessage(checkTitle);
                    player.SendChatMessage(GenRes.vehicle_model + model);
                    player.SendChatMessage(GenRes.vehicle_plate + plate);
                    player.SendChatMessage(GenRes.owner + owner);

                    var message = string.Format(InfoRes.check_vehicle_plate, player.Name, model);

                    foreach (var target in NAPI.Pools.GetAllPlayers())
                        if (player != target && player.Position.DistanceTo(target.Position) < 20.0f)
                            target.SendChatMessage(Constants.COLOR_CHAT_ME + message);
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_vehicles_near);
                }
            }
        }

        [Command(Commands.COM_FRISK, Commands.HLP_FRISK_COMMAND)]
        public void FriskCommand(Client player, string targetString) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
            else {
                var target = int.TryParse(targetString, out var targetId)
                    ? Globals.GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);

                if (target != null) {
                    if (target == player) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_searched_himself);
                    }
                    else {
                        var message = string.Format(InfoRes.player_frisk, player.Name, target.Name);
                        var inventory = Globals.GetPlayerInventoryAndWeapons(target);
                        player.SetData(EntityData.PLAYER_SEARCHED_TARGET, target);

                        foreach (var nearPlayer in NAPI.Pools.GetAllPlayers())
                            if (player != nearPlayer && player.Position.DistanceTo(nearPlayer.Position) < 20.0f)
                                nearPlayer.SendChatMessage(Constants.COLOR_CHAT_ME + message);

                        // Show target's inventory to the player
                        player.TriggerEvent("showPlayerInventory", NAPI.Util.ToJson(inventory),
                            Constants.INVENTORY_TARGET_PLAYER);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                }
            }
        }

        [Command(Commands.COM_INCRIMINATE, Commands.HLP_INCRIMINATE_COMMAND)]
        public void IncriminateCommand(Client player, string targetString) {
            if (player.GetData(EntityData.PLAYER_JAIL_AREA) == null) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_jail_area);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
            else {
                var target = int.TryParse(targetString, out var targetId)
                    ? Globals.GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);

                if (target != null) {
                    if (target == player) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_incriminated_himself);
                    }
                    else {
                        player.SetData(EntityData.PLAYER_INCRIMINATED_TARGET, target);
                        player.TriggerEvent("showCrimesMenu", NAPI.Util.ToJson(crimeList));
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                }
            }
        }

        [Command(Commands.COM_FINE, Commands.HLP_FINE_COMMAND)]
        public void FineCommand(Client player, string name = "", string surname = "", string amount = "",
            string reason = "") {
            if (name == string.Empty) {
                player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_FINE_COMMAND);
            }
            else if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
            else {
                Client target = null;

                if (int.TryParse(name, out var targetId)) {
                    target = Globals.GetPlayerById(targetId);
                    reason = amount;
                    amount = surname;
                }
                else {
                    target = NAPI.Player.GetPlayerFromName(name + " " + surname);
                }

                if (target != null && target.GetData(EntityData.PLAYER_PLAYING) != null) {
                    if (player.Position.DistanceTo(target.Position) > 2.5f) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                    }
                    else if (target == player) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_fined_himself);
                    }
                    else {
                        var fine = new FineModel();
                        {
                            fine.officer = player.Name;
                            fine.target = target.Name;
                            fine.amount = int.Parse(amount);
                            fine.reason = reason;
                        }

                        var playerMessage = string.Format(InfoRes.fine_given, target.Name);
                        var targetMessage = string.Format(InfoRes.fine_received, player.Name);
                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);

                        Task.Factory.StartNew(() => {
                            // Insert the fine into the database
                            Database.InsertFine(fine);
                        });
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                }
            }
        }

        [Command(Commands.COM_HANDCUFF, Commands.HLP_HANDCUFF_COMMAND)]
        public void HandcuffCommand(Client player, string targetString) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
            else {
                var target = int.TryParse(targetString, out var targetId)
                    ? Globals.GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);

                if (target != null) {
                    if (player.Position.DistanceTo(target.Position) > 1.5f) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                    }
                    else if (target == player) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_handcuffed_himself);
                    }
                    else if (target.GetSharedData(EntityData.PLAYER_HANDCUFFED) == null) {
                        if (target.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null) Globals.StoreItemOnHand(target);

                        // Handcuff the player
                        Globals.AttachItemToPlayer(target, 0, NAPI.Util.GetHashKey("prop_cs_cuffs_01").ToString(),
                            "IK_R_Hand", new Vector3(), new Vector3(), EntityData.PLAYER_HANDCUFFED);
                        target.PlayAnimation("mp_arresting", "idle",
                            (int) (Constants.AnimationFlags.Loop | Constants.AnimationFlags.OnlyAnimateUpperBody |
                                   Constants.AnimationFlags.AllowPlayerControl));

                        var playerMessage = string.Format(InfoRes.cuffed, target.Name);
                        var targetMessage = string.Format(InfoRes.cuffed_by, player.Name);
                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                    }
                    else {
                        // Remove the cuffs from the player
                        NAPI.ClientEvent.TriggerClientEventInDimension(target.Dimension, "dettachItemFromPlayer",
                            target.Value);
                        target.SetSharedData(EntityData.PLAYER_HANDCUFFED, null);
                        target.StopAnimation();

                        var playerMessage = string.Format(InfoRes.uncuffed, target.Name);
                        var targetMessage = string.Format(InfoRes.uncuffed_by, player.Name);
                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                }
            }
        }

        [Command(Commands.COM_EQUIPMENT, Commands.HLP_EQUIPMENT_COMMAND, GreedyArg = true)]
        public void EquipmentCommand(Client player, string action, string type = "") {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0)
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            else if (player.Position.DistanceTo(equipmentLabel.Position) > 2.0f)
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_in_room_lockers);
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0)
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            else if (Faction.IsPoliceMember(player))
                switch (action.ToLower()) {
                    case Commands.ARG_BASIC:
                        player.Armor = 100;

                        Weapons.GivePlayerNewWeapon(player, WeaponHash.Flashlight, 0, false);
                        Weapons.GivePlayerNewWeapon(player, WeaponHash.Nightstick, 0, true);
                        Weapons.GivePlayerNewWeapon(player, WeaponHash.StunGun, 0, true);

                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.equip_basic_received);
                        break;
                    case Commands.ARG_AMMUNITION:
                        if (player.GetData(EntityData.PLAYER_RANK) > 1) {
                            // Get the player weapons and ammunition
                            player.TriggerEvent("getPlayerWeapons", "");

                            var playerWeaps = player.Weapons;
                            foreach (var playerWeap in playerWeaps) {
                                var ammunition = Weapons.GetGunAmmunitionType(playerWeap);
                                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
                                var bulletItem = Globals.GetPlayerItemModelFromHash(playerId, ammunition);
                                if (bulletItem != null) {
                                    switch (playerWeap) {
                                        case WeaponHash.CombatPistol:
                                            bulletItem.amount += Constants.STACK_PISTOL_CAPACITY;
                                            break;
                                        case WeaponHash.SMG:
                                            bulletItem.amount += Constants.STACK_MACHINEGUN_CAPACITY;
                                            break;
                                        case WeaponHash.CarbineRifle:
                                            bulletItem.amount += Constants.STACK_ASSAULTRIFLE_CAPACITY;
                                            break;
                                        case WeaponHash.PumpShotgun:
                                            bulletItem.amount += Constants.STACK_SHOTGUN_CAPACITY;
                                            break;
                                        case WeaponHash.SniperRifle:
                                            bulletItem.amount += Constants.STACK_SNIPERRIFLE_CAPACITY;
                                            break;
                                    }

                                    Task.Factory.StartNew(() => {
                                        // Update the bullet's amount
                                        Database.UpdateItem(bulletItem);
                                    });
                                }
                                else {
                                    bulletItem = new ItemModel();
                                    {
                                        bulletItem.hash = ammunition;
                                        bulletItem.ownerEntity = Constants.ITEM_ENTITY_PLAYER;
                                        bulletItem.ownerIdentifier = playerId;
                                        bulletItem.amount = 30;
                                        bulletItem.position = new Vector3(0.0f, 0.0f, 0.0f);
                                        bulletItem.dimension = 0;
                                    }

                                    Task.Factory.StartNew(() => {
                                        bulletItem.id = Database.AddNewItem(bulletItem);
                                        Globals.itemList.Add(bulletItem);
                                    });
                                }
                            }

                            player.SendChatMessage(Constants.COLOR_INFO + InfoRes.equip_ammo_received);
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_police_rank);
                        }

                        break;
                    case Commands.ARG_WEAPON:
                        if (player.GetData(EntityData.PLAYER_RANK) > 1) {
                            var selectedWeap = new WeaponHash();
                            switch (type.ToLower()) {
                                case Commands.ARG_PISTOL:
                                    selectedWeap = WeaponHash.CombatPistol;
                                    break;
                                case Commands.ARG_MACHINE_GUN:
                                    selectedWeap = WeaponHash.SMG;
                                    break;
                                case Commands.ARG_ASSAULT:
                                    selectedWeap = WeaponHash.CarbineRifle;
                                    break;
                                case Commands.ARG_SNIPER:
                                    selectedWeap = WeaponHash.SniperRifle;
                                    break;
                                case Commands.ARG_SHOTGUN:
                                    selectedWeap = WeaponHash.PumpShotgun;
                                    break;
                                default:
                                    selectedWeap = 0;
                                    player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_EQUIPMENT_WEAP_COMMAND);
                                    break;
                            }

                            if (selectedWeap != 0) {
                                Weapons.GivePlayerNewWeapon(player, selectedWeap, 0, true);
                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.equip_weap_received);
                            }
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_police_rank);
                        }

                        break;
                    default:
                        player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_EQUIPMENT_COMMAND);
                        break;
                }
        }

        [Command(Commands.COM_CONTROL, Commands.HLP_POLICE_CONTROL_COMMAND)]
        public void ControlCommand(Client player, string action) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
            else {
                var policeControls = GetDifferentPoliceControls();
                switch (action.ToLower()) {
                    case Commands.ARG_LOAD:
                        if (policeControls.Count > 0) {
                            player.SetSharedData(EntityData.PLAYER_POLICE_CONTROL, Constants.ACTION_LOAD);
                            player.TriggerEvent("loadPoliceControlList", NAPI.Util.ToJson(policeControls));
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_police_controls);
                        }

                        break;
                    case Commands.ARG_SAVE:
                        player.SetSharedData(EntityData.PLAYER_POLICE_CONTROL, Constants.ACTION_SAVE);
                        if (policeControls.Count > 0)
                            player.TriggerEvent("loadPoliceControlList", NAPI.Util.ToJson(policeControls));
                        else
                            player.TriggerEvent("showPoliceControlName");
                        break;
                    case Commands.ARG_RENAME:
                        if (policeControls.Count > 0) {
                            player.SetSharedData(EntityData.PLAYER_POLICE_CONTROL, Constants.ACTION_RENAME);
                            player.TriggerEvent("loadPoliceControlList", NAPI.Util.ToJson(policeControls));
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_police_controls);
                        }

                        break;
                    case Commands.ARG_REMOVE:
                        if (policeControls.Count > 0) {
                            player.SetSharedData(EntityData.PLAYER_POLICE_CONTROL, Constants.ACTION_DELETE);
                            player.TriggerEvent("loadPoliceControlList", NAPI.Util.ToJson(policeControls));
                        }
                        else {
                            player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.no_police_controls);
                        }

                        break;
                    case Commands.ARG_CLEAR:
                        foreach (var policeControl in policeControlList)
                            if (policeControl.controlObject != null)
                                policeControl.controlObject.Delete();
                        player.SendChatMessage(Constants.COLOR_INFO + InfoRes.police_control_cleared);
                        break;
                    default:
                        player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_POLICE_CONTROL_COMMAND);
                        break;
                }
            }
        }

        [Command(Commands.COM_PUT, Commands.HLP_POLICE_PUT_COMMAND)]
        public void PutCommand(Client player, string item) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
                return;
            }

            if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
                return;
            }

            if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
                return;
            }

            PoliceControlModel policeControl;

            switch (item.ToLower()) {
                case Commands.ARG_CONE:
                    policeControl = new PoliceControlModel(0, string.Empty, Constants.POLICE_DEPLOYABLE_CONE,
                        player.Position, player.Rotation);
                    policeControl.position = new Vector3(policeControl.position.X, policeControl.position.Y,
                        policeControl.position.Z - 1.0f);
                    policeControl.controlObject = NAPI.Object.CreateObject(Constants.POLICE_DEPLOYABLE_CONE,
                        policeControl.position, policeControl.rotation);
                    policeControlList.Add(policeControl);
                    break;
                case Commands.ARG_BEACON:
                    policeControl = new PoliceControlModel(0, string.Empty, Constants.POLICE_DEPLOYABLE_BEACON,
                        player.Position, player.Rotation);
                    policeControl.position = new Vector3(policeControl.position.X, policeControl.position.Y,
                        policeControl.position.Z - 1.0f);
                    policeControl.controlObject = NAPI.Object.CreateObject(Constants.POLICE_DEPLOYABLE_BEACON,
                        policeControl.position, policeControl.rotation);
                    policeControlList.Add(policeControl);
                    break;
                case Commands.ARG_BARRIER:
                    policeControl = new PoliceControlModel(0, string.Empty, Constants.POLICE_DEPLOYABLE_BARRIER,
                        player.Position, player.Rotation);
                    policeControl.position = new Vector3(policeControl.position.X, policeControl.position.Y,
                        policeControl.position.Z - 1.0f);
                    policeControl.controlObject = NAPI.Object.CreateObject(Constants.POLICE_DEPLOYABLE_BARRIER,
                        policeControl.position, policeControl.rotation);
                    policeControlList.Add(policeControl);
                    break;
                case Commands.ARG_SPIKES:
                    policeControl = new PoliceControlModel(0, string.Empty, Constants.POLICE_DEPLOYABLE_SPIKES,
                        player.Position, player.Rotation);
                    policeControl.position = new Vector3(policeControl.position.X, policeControl.position.Y,
                        policeControl.position.Z - 1.0f);
                    policeControl.controlObject = NAPI.Object.CreateObject(Constants.POLICE_DEPLOYABLE_SPIKES,
                        policeControl.position, policeControl.rotation);
                    policeControlList.Add(policeControl);
                    break;
                default:
                    player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_POLICE_PUT_COMMAND);
                    break;
            }
        }

        [Command(Commands.COM_REMOVE, Commands.HLP_POLICE_REMOVE_COMMAND)]
        public void RemoveCommand(Client player, string item) {
            if (player.GetData(EntityData.PLAYER_KILLED) != 0)
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0)
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            else if (!Faction.IsPoliceMember(player))
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            else
                switch (item.ToLower()) {
                    case Commands.ARG_CONE:
                        RemoveClosestPoliceControlItem(player, Constants.POLICE_DEPLOYABLE_CONE);
                        break;
                    case Commands.ARG_BEACON:
                        RemoveClosestPoliceControlItem(player, Constants.POLICE_DEPLOYABLE_BEACON);
                        break;
                    case Commands.ARG_BARRIER:
                        RemoveClosestPoliceControlItem(player, Constants.POLICE_DEPLOYABLE_BARRIER);
                        break;
                    case Commands.ARG_SPIKES:
                        RemoveClosestPoliceControlItem(player, Constants.POLICE_DEPLOYABLE_SPIKES);
                        break;
                    default:
                        player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_POLICE_REMOVE_COMMAND);
                        break;
                }
        }

        [Command(Commands.COM_REINFORCES)]
        public void ReinforcesCommand(Client player) {
            if (!Faction.IsPoliceMember(player)) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
            else if (player.GetData(EntityData.PLAYER_ON_DUTY) == 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_on_duty);
            }
            else if (player.GetData(EntityData.PLAYER_KILLED) != 0) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_is_dead);
            }
            else {
                // Get police department's members
                var policeMembers = NAPI.Pools.GetAllPlayers().Where(p => Faction.IsPoliceMember(p)).ToList();

                if (player.GetData(EntityData.PLAYER_REINFORCES) != null) {
                    var targetMessage = string.Format(InfoRes.target_reinforces_canceled, player.Name);

                    foreach (var target in policeMembers)
                        if (target.GetData(EntityData.PLAYER_PLAYING) != null &&
                            target.GetData(EntityData.PLAYER_ON_DUTY) == 1) {
                            // Remove the blip from the map
                            target.TriggerEvent("reinforcesRemove", player.Value);

                            if (player == target)
                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_reinforces_canceled);
                            else
                                target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                        }

                    // Remove player's reinforces
                    player.ResetData(EntityData.PLAYER_REINFORCES);
                }
                else {
                    var targetMessage = string.Format(InfoRes.target_reinforces_asked, player.Name);

                    foreach (var target in policeMembers)
                        if (target.GetData(EntityData.PLAYER_PLAYING) != null &&
                            target.GetData(EntityData.PLAYER_ON_DUTY) == 1) {
                            if (player == target)
                                player.SendChatMessage(Constants.COLOR_INFO + InfoRes.player_reinforces_asked);
                            else
                                target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                        }

                    // Ask for reinforces
                    player.SetData(EntityData.PLAYER_REINFORCES, true);
                }
            }
        }

        [Command(Commands.COM_LICENSE, Commands.HLP_LICENSE_COMMAND, GreedyArg = true)]
        public void LicenseCommand(Client player, string args) {
            if (Faction.IsPoliceMember(player) && player.GetData(EntityData.PLAYER_RANK) == 6) {
                var arguments = args.Trim().Split(' ');
                if (arguments.Length == 3 || arguments.Length == 4) {
                    Client target;

                    // Get the target player
                    if (int.TryParse(arguments[2], out var targetId) && arguments.Length == 3)
                        target = Globals.GetPlayerById(targetId);
                    else
                        target = NAPI.Player.GetPlayerFromName(arguments[2] + arguments[3]);

                    // Check whether the target player is connected
                    if (target == null || target.GetData(EntityData.PLAYER_PLAYING) == null) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_found);
                    }
                    else if (player.Position.DistanceTo(target.Position) > 2.5f) {
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_too_far);
                    }
                    else {
                        string playerMessage;
                        string targetMessage;

                        switch (arguments[0].ToLower()) {
                            case Commands.ARG_GIVE:
                                switch (arguments[1].ToLower()) {
                                    case Commands.ARG_WEAPON:
                                        // Add one month to the license
                                        target.SetData(EntityData.PLAYER_WEAPON_LICENSE,
                                            Globals.GetTotalSeconds() + 2628000);

                                        playerMessage = string.Format(InfoRes.weapon_license_given, target.Name);
                                        targetMessage = string.Format(InfoRes.weapon_license_received, player.Name);
                                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                                        break;
                                    default:
                                        player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_LICENSE_COMMAND);
                                        break;
                                }

                                break;
                            case Commands.ARG_REMOVE:
                                switch (arguments[1].ToLower()) {
                                    case Commands.ARG_WEAPON:
                                        // Adjust the date to the current one
                                        target.SetData(EntityData.PLAYER_WEAPON_LICENSE, Globals.GetTotalSeconds());

                                        playerMessage = string.Format(InfoRes.weapon_license_removed, target.Name);
                                        targetMessage = string.Format(InfoRes.weapon_license_lost, player.Name);
                                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                                        break;
                                    case Commands.ARG_CAR:
                                        // Remove car license
                                        DrivingSchool.SetPlayerLicense(target, Constants.LICENSE_CAR, -1);

                                        playerMessage = string.Format(InfoRes.car_license_removed, target.Name);
                                        targetMessage = string.Format(InfoRes.car_license_lost, player.Name);
                                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                                        break;
                                    case Commands.ARG_MOTORCYCLE:
                                        // Remove motorcycle license
                                        DrivingSchool.SetPlayerLicense(target, Constants.LICENSE_MOTORCYCLE, -1);

                                        playerMessage = string.Format(InfoRes.moto_license_removed, target.Name);
                                        targetMessage = string.Format(InfoRes.moto_license_lost, player.Name);
                                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                                        target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
                                        break;
                                }

                                break;
                            default:
                                player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_LICENSE_COMMAND);
                                break;
                        }
                    }
                }
                else {
                    player.SendChatMessage(Constants.COLOR_HELP + Commands.HLP_LICENSE_COMMAND);
                }
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.not_police_chief);
            }
        }

        [Command(Commands.COM_BREATHALYZER, Commands.HLP_ALCOHOLIMETER_COMMAND)]
        public void BreathalyzerCommand(Client player, string targetString) {
            if (Faction.IsPoliceMember(player) && player.GetData(EntityData.PLAYER_RANK) > 0) {
                var alcoholLevel = 0.0f;
                var target = int.TryParse(targetString, out var targetId)
                    ? Globals.GetPlayerById(targetId)
                    : NAPI.Player.GetPlayerFromName(targetString);

                if (target.GetData(EntityData.PLAYER_DRUNK_LEVEL) != null)
                    alcoholLevel = target.GetData(EntityData.PLAYER_DRUNK_LEVEL);

                var playerMessage = string.Format(InfoRes.alcoholimeter_test, target.Name, alcoholLevel);
                var targetMessage = string.Format(InfoRes.alcoholimeter_receptor, player.Name, alcoholLevel);
                player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                target.SendChatMessage(Constants.COLOR_INFO + targetMessage);
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_police_faction);
            }
        }
    }
}