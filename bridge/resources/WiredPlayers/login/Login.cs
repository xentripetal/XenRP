using GTANetworkAPI;
using WiredPlayers.model;
using WiredPlayers.database;
using WiredPlayers.globals;
using System.Collections.Generic;
using System.Threading;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace WiredPlayers.login
{
    public class Login : Script
    {
        private static Dictionary<string, Timer> spawnTimerList = new Dictionary<string, Timer>();

        public static void OnPlayerDisconnected(Client player, DisconnectionType type, string reason)
        {
            if (spawnTimerList.TryGetValue(player.SocialClubName, out Timer spawnTimer) == true)
            {
                spawnTimer.Dispose();
                spawnTimerList.Remove(player.SocialClubName);
            }
        }

        private void InitializePlayerData(Client player)
        {
            Vector3 worldSpawn = new Vector3(200.6641f, -932.0939f, 30.68681f);
            Vector3 rotation = new Vector3(0.0f, 0.0f, 0.0f);
            player.Position = new Vector3(152.26, -1004.47, -99.00);
            player.Dimension = Convert.ToUInt32(player.Value);

            // Set the default skin
            NAPI.Player.SetPlayerSkin(player, PedHash.Strperf01SMM);

            player.Health = 100;
            player.Armor = 0;
            player.Transparency = 255;

            // Clear weapons
            player.RemoveAllWeapons();

            // Initialize shared entity data
            player.SetSharedData(EntityData.PLAYER_SEX, 0);
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
            player.SetData(EntityData.PLAYER_JAILED, 0);
            player.SetData(EntityData.PLAYER_JAIL_TYPE, 0);
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

        private void InitializePlayerSkin(Client player)
        {
            player.SetData(EntityData.FIRST_HEAD_SHAPE, 0);
            player.SetData(EntityData.SECOND_HEAD_SHAPE, 0);
            player.SetData(EntityData.FIRST_SKIN_TONE, 0);
            player.SetData(EntityData.SECOND_SKIN_TONE, 0);
            player.SetData(EntityData.HEAD_MIX, 0.5f);
            player.SetData(EntityData.SKIN_MIX, 0.5f);

            // Hair generation
            player.SetData(EntityData.HAIR_MODEL, 0);
            player.SetData(EntityData.FIRST_HAIR_COLOR, 0);
            player.SetData(EntityData.SECOND_HAIR_COLOR, 0);

            // Beard generation
            player.SetData(EntityData.BEARD_MODEL, 0);
            player.SetData(EntityData.BEARD_COLOR, 0);

            // Chest hair generation
            player.SetData(EntityData.CHEST_MODEL, 0);
            player.SetData(EntityData.CHEST_COLOR, 0);

            // Face marks generation
            player.SetData(EntityData.BLEMISHES_MODEL, -1);
            player.SetData(EntityData.AGEING_MODEL, -1);
            player.SetData(EntityData.COMPLEXION_MODEL, -1);
            player.SetData(EntityData.SUNDAMAGE_MODEL, -1);
            player.SetData(EntityData.FRECKLES_MODEL, -1);

            // Eyes and eyebrows generation
            player.SetData(EntityData.EYES_COLOR, 0);
            player.SetData(EntityData.EYEBROWS_MODEL, 0);
            player.SetData(EntityData.EYEBROWS_COLOR, 0);

            // Cosmetics generation
            player.SetData(EntityData.MAKEUP_MODEL, -1);
            player.SetData(EntityData.BLUSH_MODEL, -1);
            player.SetData(EntityData.BLUSH_COLOR, 0);
            player.SetData(EntityData.LIPSTICK_MODEL, -1);
            player.SetData(EntityData.LIPSTICK_COLOR, 0);

            // Advanced facial model generation
            player.SetData(EntityData.NOSE_WIDTH, 0.0f);
            player.SetData(EntityData.NOSE_HEIGHT, 0.0f);
            player.SetData(EntityData.NOSE_LENGTH, 0.0f);
            player.SetData(EntityData.NOSE_BRIDGE, 0.0f);
            player.SetData(EntityData.NOSE_TIP, 0.0f);
            player.SetData(EntityData.NOSE_SHIFT, 0.0f);
            player.SetData(EntityData.BROW_HEIGHT, 0.0f);
            player.SetData(EntityData.BROW_WIDTH, 0.0f);
            player.SetData(EntityData.CHEEKBONE_HEIGHT, 0.0f);
            player.SetData(EntityData.CHEEKBONE_WIDTH, 0.0f);
            player.SetData(EntityData.CHEEKS_WIDTH, 0.0f);
            player.SetData(EntityData.EYES, 0.0f);
            player.SetData(EntityData.LIPS, 0.0f);
            player.SetData(EntityData.JAW_WIDTH, 0.0f);
            player.SetData(EntityData.JAW_HEIGHT, 0.0f);
            player.SetData(EntityData.CHIN_LENGTH, 0.0f);
            player.SetData(EntityData.CHIN_POSITION, 0.0f);
            player.SetData(EntityData.CHIN_WIDTH, 0.0f);
            player.SetData(EntityData.CHIN_SHAPE, 0.0f);
            player.SetData(EntityData.NECK_WIDTH, 0.0f);
        }

        private void LoadCharacterData(Client player, PlayerModel character)
        {
            string[] jail = character.jailed.Split(',');

            player.SetSharedData(EntityData.PLAYER_MONEY, character.money);
            player.SetSharedData(EntityData.PLAYER_BANK, character.bank);
            player.SetSharedData(EntityData.PLAYER_SEX, character.sex);

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

        private void PopulateCharacterSkin(Client player, SkinModel skinModel)
        {
            player.SetSharedData(EntityData.FIRST_HEAD_SHAPE, skinModel.firstHeadShape);
            player.SetSharedData(EntityData.SECOND_HEAD_SHAPE, skinModel.secondHeadShape);
            player.SetSharedData(EntityData.FIRST_SKIN_TONE, skinModel.firstSkinTone);
            player.SetSharedData(EntityData.SECOND_SKIN_TONE, skinModel.secondSkinTone);
            player.SetSharedData(EntityData.HEAD_MIX, skinModel.headMix);
            player.SetSharedData(EntityData.SKIN_MIX, skinModel.skinMix);

            player.SetSharedData(EntityData.HAIR_MODEL, skinModel.hairModel);
            player.SetSharedData(EntityData.FIRST_HAIR_COLOR, skinModel.firstHairColor);
            player.SetSharedData(EntityData.SECOND_HAIR_COLOR, skinModel.secondHairColor);

            player.SetSharedData(EntityData.BEARD_MODEL, skinModel.beardModel);
            player.SetSharedData(EntityData.BEARD_COLOR, skinModel.beardColor);

            player.SetSharedData(EntityData.CHEST_MODEL, skinModel.chestModel);
            player.SetSharedData(EntityData.CHEST_COLOR, skinModel.chestColor);

            player.SetSharedData(EntityData.BLEMISHES_MODEL, skinModel.blemishesModel);
            player.SetSharedData(EntityData.AGEING_MODEL, skinModel.ageingModel);
            player.SetSharedData(EntityData.COMPLEXION_MODEL, skinModel.complexionModel);
            player.SetSharedData(EntityData.SUNDAMAGE_MODEL, skinModel.sundamageModel);
            player.SetSharedData(EntityData.FRECKLES_MODEL, skinModel.frecklesModel);

            player.SetSharedData(EntityData.EYES_COLOR, skinModel.eyesColor);
            player.SetSharedData(EntityData.EYEBROWS_MODEL, skinModel.eyebrowsModel);
            player.SetSharedData(EntityData.EYEBROWS_COLOR, skinModel.eyebrowsColor);

            player.SetSharedData(EntityData.MAKEUP_MODEL, skinModel.makeupModel);
            player.SetSharedData(EntityData.BLUSH_MODEL, skinModel.blushModel);
            player.SetSharedData(EntityData.BLUSH_COLOR, skinModel.blushColor);
            player.SetSharedData(EntityData.LIPSTICK_MODEL, skinModel.lipstickModel);
            player.SetSharedData(EntityData.LIPSTICK_COLOR, skinModel.lipstickColor);

            player.SetSharedData(EntityData.NOSE_WIDTH, skinModel.noseWidth);
            player.SetSharedData(EntityData.NOSE_HEIGHT, skinModel.noseHeight);
            player.SetSharedData(EntityData.NOSE_LENGTH, skinModel.noseLength);
            player.SetSharedData(EntityData.NOSE_BRIDGE, skinModel.noseBridge);
            player.SetSharedData(EntityData.NOSE_TIP, skinModel.noseTip);
            player.SetSharedData(EntityData.NOSE_SHIFT, skinModel.noseShift);
            player.SetSharedData(EntityData.BROW_HEIGHT, skinModel.browHeight);
            player.SetSharedData(EntityData.BROW_WIDTH, skinModel.browWidth);
            player.SetSharedData(EntityData.CHEEKBONE_HEIGHT, skinModel.cheekboneHeight);
            player.SetSharedData(EntityData.CHEEKBONE_WIDTH, skinModel.cheekboneWidth);
            player.SetSharedData(EntityData.CHEEKS_WIDTH, skinModel.cheeksWidth);
            player.SetSharedData(EntityData.EYES, skinModel.eyes);
            player.SetSharedData(EntityData.LIPS, skinModel.lips);
            player.SetSharedData(EntityData.JAW_WIDTH, skinModel.jawWidth);
            player.SetSharedData(EntityData.JAW_HEIGHT, skinModel.jawHeight);
            player.SetSharedData(EntityData.CHIN_LENGTH, skinModel.chinLength);
            player.SetSharedData(EntityData.CHIN_POSITION, skinModel.chinPosition);
            player.SetSharedData(EntityData.CHIN_WIDTH, skinModel.chinWidth);
            player.SetSharedData(EntityData.CHIN_SHAPE, skinModel.chinShape);
            player.SetSharedData(EntityData.NECK_WIDTH, skinModel.neckWidth);

            // Timer to update player's face model
            Timer spawnTimer = new Timer(OnPlayerUpdateTimer, player, 350, Timeout.Infinite);
            spawnTimerList.Add(player.SocialClubName, spawnTimer);
        }

        public void OnPlayerUpdateTimer(object playerObject)
        {
            Client player = (Client)playerObject;
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
            List<TattooModel> playerTattooList = Globals.GetPlayerTattoos(playerId);

            Timer spawnTimer = spawnTimerList[player.SocialClubName];
            if (spawnTimer != null)
            {
                spawnTimer.Dispose();
                spawnTimerList.Remove(player.SocialClubName);
            }

            // Update player's face model
            player.TriggerEvent("updatePlayerCustomSkin", player, NAPI.Util.ToJson(playerTattooList));
        }

        [ServerEvent(Event.PlayerConnected)]
        public void OnPlayerConnected(Client player)
        {
            // Initialize the player data
            InitializePlayerData(player);

            Task.Factory.StartNew(() =>
            {
                AccountModel account = Database.GetAccount(player.SocialClubName);

                switch (account.status)
                {
                    case -1:
                        player.SendChatMessage(Constants.COLOR_INFO + Messages.INF_ACCOUNT_DISABLED);
                        player.Kick(Messages.INF_ACCOUNT_DISABLED);
                        break;
                    case 0:
                        // Show the register window
                        player.TriggerEvent("showRegisterWindow");
                        break;
                    default:
                        // Welcome message
                        string welcomeMessage = string.Format(Messages.GEN_WELCOME_MESSAGE, player.SocialClubName);
                        player.SendChatMessage(welcomeMessage);
                        player.SendChatMessage(Messages.GEN_WELCOME_HINT);
                        player.SendChatMessage(Messages.GEN_HELP_HINT);
                        player.SendChatMessage(Messages.GEN_TICKET_HINT);

                        if (account.lastCharacter > 0)
                        {
                            // Load selected character
                            PlayerModel character = Database.LoadCharacterInformationById(account.lastCharacter);
                            SkinModel skin = Database.GetCharacterSkin(account.lastCharacter);

                            player.Name = character.realName;
                            NAPI.Player.SetPlayerSkin(player, character.sex == 0 ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01);

                            LoadCharacterData(player, character);

                            PopulateCharacterSkin(player, skin);

                            Globals.PopulateCharacterClothes(player);
                        }

                        // Activate the login window
                        player.SetSharedData(EntityData.SERVER_TIME, DateTime.Now.ToString("HH:mm:ss"));

                        break;
                }
            });
        }

        [RemoteEvent("loginAccount")]
        public void LoginAccountEvent(Client player, string password)
        {
            Task.Factory.StartNew(() =>
            {
                bool login = Database.LoginAccount(player.SocialClubName, password);
                player.TriggerEvent(login ? "clearLoginWindow" : "showLoginError");
            });
        }

        [RemoteEvent("registerAccount")]
        public void RegisterAccountEvent(Client player, string password)
        {
            Task.Factory.StartNew(() =>
            {
                Database.RegisterAccount(player.SocialClubName, password);
                player.TriggerEvent("clearRegisterWindow");
            });
        }

        [RemoteEvent("changeCharacterSex")]
        public void ChangeCharacterSexEvent(Client player, int sex)
        {
            // Set the model of the player
            NAPI.Player.SetPlayerSkin(player, sex == 0 ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01);

            // Remove player's clothes
            player.SetClothes(11, 15, 0);
            player.SetClothes(3, 15, 0);
            player.SetClothes(8, 15, 0);

            // Save sex entity shared data
            player.SetSharedData(EntityData.PLAYER_SEX, sex);
        }

        [RemoteEvent("createCharacter")]
        public void CreateCharacterEvent(Client player, string playerName, int playerAge, string skinJson)
        {
            PlayerModel playerModel = new PlayerModel();
            SkinModel skinModel = JsonConvert.DeserializeObject<SkinModel>(skinJson);

            playerModel.realName = playerName;
            playerModel.age = playerAge;
            playerModel.sex = player.GetSharedData(EntityData.PLAYER_SEX);

            PopulateCharacterSkin(player, skinModel);

            Task.Factory.StartNew(() =>
            {
                int playerId = Database.CreateCharacter(player, playerModel, skinModel);

                if (playerId > 0)
                {
                    InitializePlayerData(player);
                    player.Transparency = 255;
                    player.SetData(EntityData.PLAYER_SQL_ID, playerId);
                    player.SetData(EntityData.PLAYER_NAME, playerName);
                    player.SetData(EntityData.PLAYER_AGE, playerAge);
                    player.SetSharedData(EntityData.PLAYER_SPAWN_POS, new Vector3(200.6641f, -932.0939f, 30.6868f));
                    player.SetSharedData(EntityData.PLAYER_SPAWN_ROT, new Vector3(0.0f, 0.0f, 0.0f));

                    Database.UpdateLastCharacter(player.SocialClubName, playerId);

                    player.TriggerEvent("characterCreatedSuccessfully");
                }
            });
        }

        [RemoteEvent("setCharacterIntoCreator")]
        public void SetCharacterIntoCreatorEvent(Client player)
        {
            // Change player's skin
            NAPI.Player.SetPlayerSkin(player, PedHash.FreemodeMale01);

            // Initialize player's face mode
            InitializePlayerSkin(player);

            // Remove clothes
            player.SetClothes(11, 15, 0);
            player.SetClothes(3, 15, 0);
            player.SetClothes(8, 15, 0);

            // Set player's position
            player.Transparency = 255;
            player.Rotation = new Vector3(0.0f, 0.0f, 180.0f);
            player.Position = new Vector3(152.3787f, -1000.644f, -99f);
        }

        [RemoteEvent("loadCharacter")]
        public void LoadCharacterEvent(Client player, string name)
        {
            Task.Factory.StartNew(() =>
            {
                PlayerModel playerModel = Database.LoadCharacterInformationByName(name);
                SkinModel skinModel = Database.GetCharacterSkin(playerModel.id);

                // Load player's model
                player.Name = playerModel.realName;
                NAPI.Player.SetPlayerSkin(player, playerModel.sex == 0 ? PedHash.FreemodeMale01 : PedHash.FreemodeFemale01);

                LoadCharacterData(player, playerModel);
                PopulateCharacterSkin(player, skinModel);
                Globals.PopulateCharacterClothes(player);

                // Update last selected character
                Database.UpdateLastCharacter(player.SocialClubName, playerModel.id);
            });
        }

        [RemoteEvent("getPlayerCustomSkin")]
        public void GetPlayerCustomSkinEvent(Client player, Client target)
        {
            int targetId = target.GetData(EntityData.PLAYER_SQL_ID);
            List<TattooModel> targetTattooList = Globals.GetPlayerTattoos(targetId);

            // Update the model clientside
            player.TriggerEvent("updatePlayerCustomSkin", target, NAPI.Util.ToJson(targetTattooList));
        }
    }
}