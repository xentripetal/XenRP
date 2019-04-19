﻿using System;
using System.Threading.Tasks;
using GTANetworkAPI;
using XenRP.database;
using XenRP.globals;
using XenRP.model;

namespace XenRP.character {
    public class Character : Script {
        public static void InitializePlayerData(Client player) {
            var worldSpawn = new Vector3(200.6641f, -932.0939f, 30.68681f);
            var rotation = new Vector3(0.0f, 0.0f, 0.0f);
            player.Position = new Vector3(402.9364f, -996.7154f, -99.00024f);
            player.Dimension = Convert.ToUInt32(player.Value);

            player.Health = 100;
            player.Armor = 0;

            // Clear weapons
            player.RemoveAllWeapons();

            // Initialize shared entity data
            player.SetData(EntityData.PLAYER_SEX, 0);
            player.SetSharedData(EntityData.PLAYER_MONEY, 0);
            player.SetSharedData(EntityData.PLAYER_BANK, 3500);

            // Initialize entity data
            player.SetData(EntityData.PLAYER_NAME, string.Empty);
            player.SetData(EntityData.PLAYER_SPAWN_POS, worldSpawn);
            player.SetData(EntityData.PLAYER_SPAWN_ROT, rotation);
            player.SetData(EntityData.PLAYER_ADMIN_NAME, string.Empty);
            player.SetData(EntityData.PLAYER_ADMIN_RANK, 0);
            player.SetData(EntityData.PLAYER_AGE, 18);
            player.SetData(EntityData.PLAYER_HEALTH, 100);
            player.SetData(EntityData.PLAYER_ARMOR, 0);
            player.SetData(EntityData.PLAYER_PHONE, 0);
            player.SetData(EntityData.PLAYER_RADIO, 0);
            player.SetData(EntityData.PLAYER_KILLED, 0);
            player.SetData(EntityData.PLAYER_JAILED, -1);
            player.SetData(EntityData.PLAYER_JAIL_TYPE, -1);
            player.SetData(EntityData.PLAYER_FACTION, 0);
            player.SetData(EntityData.PLAYER_JOB, 0);
            player.SetData(EntityData.PLAYER_RANK, 0);
            player.SetData(EntityData.PLAYER_ON_DUTY, 0);
            player.SetData(EntityData.PLAYER_RENT_HOUSE, 0);
            player.SetData(EntityData.PLAYER_HOUSE_ENTERED, 0);
            player.SetData(EntityData.PLAYER_BUSINESS_ENTERED, 0);
            player.SetData(EntityData.PLAYER_DOCUMENTATION, 0);
            player.SetData(EntityData.PLAYER_VEHICLE_KEYS, "0,0,0,0,0");
            player.SetData(EntityData.PLAYER_JOB_POINTS, "0,0,0,0,0,0,0");
            player.SetData(EntityData.PLAYER_LICENSES, "-1,-1,-1");
            player.SetData(EntityData.PLAYER_ROLE_POINTS, 0);
            player.SetData(EntityData.PLAYER_MEDICAL_INSURANCE, 0);
            player.SetData(EntityData.PLAYER_WEAPON_LICENSE, 0);
            player.SetData(EntityData.PLAYER_JOB_COOLDOWN, 0);
            player.SetData(EntityData.PLAYER_EMPLOYEE_COOLDOWN, 0);
            player.SetData(EntityData.PLAYER_JOB_DELIVER, 0);
            player.SetData(EntityData.PLAYER_PLAYED, 0);
            player.SetData(EntityData.PLAYER_STATUS, 0);
        }

        public static void SaveCharacterData(Client player) {
            var character = new PlayerModel();
            {
                character.position = player.Position;
                character.rotation = player.Rotation;
                character.health = player.Health;
                character.armor = player.Armor;
                character.id = player.GetData(EntityData.PLAYER_SQL_ID);
                character.phone = player.GetData(EntityData.PLAYER_PHONE);
                character.radio = player.GetData(EntityData.PLAYER_RADIO);
                character.killed = player.GetData(EntityData.PLAYER_KILLED);
                character.faction = player.GetData(EntityData.PLAYER_FACTION);
                character.job = player.GetData(EntityData.PLAYER_JOB);
                character.rank = player.GetData(EntityData.PLAYER_RANK);
                character.duty = player.GetData(EntityData.PLAYER_ON_DUTY);
                character.carKeys = player.GetData(EntityData.PLAYER_VEHICLE_KEYS);
                character.documentation = player.GetData(EntityData.PLAYER_DOCUMENTATION);
                character.licenses = player.GetData(EntityData.PLAYER_LICENSES);
                character.insurance = player.GetData(EntityData.PLAYER_MEDICAL_INSURANCE);
                character.weaponLicense = player.GetData(EntityData.PLAYER_WEAPON_LICENSE);
                character.houseRent = player.GetData(EntityData.PLAYER_RENT_HOUSE);
                character.houseEntered = player.GetData(EntityData.PLAYER_HOUSE_ENTERED);
                character.businessEntered = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
                character.employeeCooldown = player.GetData(EntityData.PLAYER_EMPLOYEE_COOLDOWN);
                character.jobCooldown = player.GetData(EntityData.PLAYER_JOB_COOLDOWN);
                character.jobDeliver = player.GetData(EntityData.PLAYER_JOB_DELIVER);
                character.jobPoints = player.GetData(EntityData.PLAYER_JOB_POINTS);
                character.rolePoints = player.GetData(EntityData.PLAYER_ROLE_POINTS);
                character.played = player.GetData(EntityData.PLAYER_PLAYED);
                character.jailed = player.GetData(EntityData.PLAYER_JAIL_TYPE) + "," +
                                   player.GetData(EntityData.PLAYER_JAILED);

                character.money = player.GetSharedData(EntityData.PLAYER_MONEY);
                character.bank = player.GetSharedData(EntityData.PLAYER_BANK);
            }

            Task.Factory.StartNew(() => {
                // Save the player into database
                Database.SaveCharacterInformation(character);
            });
        }

        public static void LoadCharacterData(Client player, PlayerModel character) {
            var jail = character.jailed.Split(',');

            player.SetSharedData(EntityData.PLAYER_MONEY, character.money);
            player.SetSharedData(EntityData.PLAYER_BANK, character.bank);
            player.SetData(EntityData.PLAYER_SEX, character.sex);

            player.SetData(EntityData.PLAYER_SQL_ID, character.id);
            player.SetData(EntityData.PLAYER_NAME, character.realName);
            player.SetData(EntityData.PLAYER_HEALTH, character.health);
            player.SetData(EntityData.PLAYER_ARMOR, character.armor);
            player.SetData(EntityData.PLAYER_AGE, character.age);
            player.SetData(EntityData.PLAYER_ADMIN_RANK, character.adminRank);
            player.SetData(EntityData.PLAYER_ADMIN_NAME, character.adminName);
            player.SetData(EntityData.PLAYER_SPAWN_POS, character.position);
            player.SetData(EntityData.PLAYER_SPAWN_ROT, character.rotation);
            player.SetData(EntityData.PLAYER_PHONE, character.phone);
            player.SetData(EntityData.PLAYER_RADIO, character.radio);
            player.SetData(EntityData.PLAYER_KILLED, character.killed);
            player.SetData(EntityData.PLAYER_JAIL_TYPE, int.Parse(jail[0]));
            player.SetData(EntityData.PLAYER_JAILED, int.Parse(jail[1]));
            player.SetData(EntityData.PLAYER_FACTION, character.faction);
            player.SetData(EntityData.PLAYER_JOB, character.job);
            player.SetData(EntityData.PLAYER_RANK, character.rank);
            player.SetData(EntityData.PLAYER_ON_DUTY, character.duty);
            player.SetData(EntityData.PLAYER_VEHICLE_KEYS, character.carKeys);
            player.SetData(EntityData.PLAYER_DOCUMENTATION, character.documentation);
            player.SetData(EntityData.PLAYER_LICENSES, character.licenses);
            player.SetData(EntityData.PLAYER_MEDICAL_INSURANCE, character.insurance);
            player.SetData(EntityData.PLAYER_WEAPON_LICENSE, character.weaponLicense);
            player.SetData(EntityData.PLAYER_RENT_HOUSE, character.houseRent);
            player.SetData(EntityData.PLAYER_HOUSE_ENTERED, character.houseEntered);
            player.SetData(EntityData.PLAYER_BUSINESS_ENTERED, character.businessEntered);
            player.SetData(EntityData.PLAYER_EMPLOYEE_COOLDOWN, character.employeeCooldown);
            player.SetData(EntityData.PLAYER_JOB_COOLDOWN, character.jobCooldown);
            player.SetData(EntityData.PLAYER_JOB_DELIVER, character.jobDeliver);
            player.SetData(EntityData.PLAYER_JOB_POINTS, character.jobPoints);
            player.SetData(EntityData.PLAYER_ROLE_POINTS, character.rolePoints);
            player.SetData(EntityData.PLAYER_PLAYED, character.played);
            player.SetData(EntityData.PLAYER_STATUS, character.status);
        }
    }
}