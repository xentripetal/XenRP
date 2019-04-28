using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GTANetworkAPI;
using MySql.Data.MySqlClient;
using XenRP.admin;
using XenRP.business;
using XenRP.character;
using XenRP.factions;
using XenRP.globals;
using XenRP.house;
using XenRP.messages.general;
using XenRP.model;
using XenRP.parking;
using XenRP.vehicles;

namespace XenRP.database {
    public class Database : Script {
        private static string connectionString;

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart() {
            // Set the encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


            // Create the database connection string
            var host = NAPI.Resource.GetSetting<string>(this, "host");
            var user = NAPI.Resource.GetSetting<string>(this, "username");
            var pass = NAPI.Resource.GetSetting<string>(this, "password");
            var db = NAPI.Resource.GetSetting<string>(this, "database");
            connectionString = "SERVER=" + host + "; DATABASE=" + db + "; UID=" + user + "; PASSWORD=" + pass +
                               "; SSLMODE=none;";


            // Vehicle loading
            Vehicles.LoadDatabaseVehicles();

            // Item loading
            Inventory.LoadDatabaseItems();

            // Crimes loading
            LoadCrimes();

            // Police controls loading
            LoadAllPoliceControls();

            // Radio frequency channels loading
            Faction.channelList = LoadAllChannels();

            // Blood units loading
            Emergency.bloodList = LoadAllBlood();

            // Clothes loading
            Globals.clothesList = LoadAllClothes();

            // Tattoos loading
            Globals.tattooList = LoadAllTattoos();

            // Phone contacts loading
            Telephone.contactList = LoadAllContacts();

            // Special permission loading
            Admin.permissionList = LoadAllPermissions();
        }

        public static MySqlConnection GetConnection() {
            return new MySqlConnection(connectionString);
        }

        public static AccountModel GetAccount(string socialName) {
            var account = new AccountModel();
            {
                account.status = 0;
            }

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT forumName, status, lastCharacter FROM accounts WHERE socialName = @socialName LIMIT 1";
                command.Parameters.AddWithValue("@socialName", socialName);

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows) {
                        reader.Read();
                        account.forumName = reader.GetString("forumName");
                        account.status = reader.GetInt16("status");
                        account.lastCharacter = reader.GetInt16("lastCharacter");
                        account.registered = true;
                    }
                }
            }

            return account;
        }

        public static int LoginAccount(string socialName, string password) {
            var status = -1;

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT `status` FROM accounts WHERE socialName = @socialName AND password = SHA2(@password, '256') LIMIT 1";
                command.Parameters.AddWithValue("@socialName", socialName);
                command.Parameters.AddWithValue("@password", password);

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows) {
                        reader.Read();
                        status = reader.GetInt32("status");
                    }
                }
            }

            return status;
        }

        public static void RegisterAccount(string socialName, string password) {
            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "INSERT INTO `accounts` (`socialName`, `password`) VALUES (@socialName, SHA2(@password, '256'))";
                command.Parameters.AddWithValue("@socialName", socialName);
                command.Parameters.AddWithValue("@password", password);

                command.ExecuteNonQuery();
            }
        }

        public static void ApproveAccount(string socialName) {
            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = "UPDATE `accounts` SET `status` = 1 WHERE `socialName`= @socialName LIMIT 1";
                command.Parameters.AddWithValue("@socialName", socialName);

                command.ExecuteNonQuery();
            }
        }

        public static void RegisterApplication(string socialName, int mistakes) {
            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText =
                    "INSERT INTO `applications` (`account`, `mistakes`) VALUES (@socialName, @mistakes)";
                command.Parameters.AddWithValue("@socialName", socialName);
                command.Parameters.AddWithValue("@mistakes", mistakes);

                command.ExecuteNonQuery();
            }
        }

        public static int GetPlayerStatus(string name) {
            var status = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT status FROM users WHERE name = @name LIMIT 1";
                command.Parameters.AddWithValue("@name", name);

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows) status = reader.GetInt16("status");
                }
            }

            return status;
        }

        public static List<string> GetAccountCharacters(string account) {
            var characters = new List<string>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT name FROM users WHERE socialName = @account";
                command.Parameters.AddWithValue("@account", account);

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var name = reader.GetString("name");
                        characters.Add(name);
                    }
                }
            }

            return characters;
        }

        public static int CreateCharacter(Client player, PlayerModel playerModel, SkinModel skin) {
            var playerId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                        "INSERT INTO users (name, age, sex, socialName) VALUES (@playerName, @playerAge, @playerSex, @socialName)";
                    command.Parameters.AddWithValue("@playerName", playerModel.realName);
                    command.Parameters.AddWithValue("@playerAge", playerModel.age);
                    command.Parameters.AddWithValue("@playerSex", playerModel.sex);
                    command.Parameters.AddWithValue("@socialName", player.SocialClubName);
                    command.ExecuteNonQuery();

                    // Get the inserted identifier
                    playerId = (int) command.LastInsertedId;

                    // Store player's skin
                    command.CommandText =
                        "INSERT INTO skins VALUES (@playerId, @firstHeadShape, @secondHeadShape, @firstSkinTone, @secondSkinTone, @headMix, @skinMix, ";
                    command.CommandText +=
                        "@hairModel, @firstHairColor, @secondHairColor, @beardModel, @beardColor, @chestModel, @chestColor, @blemishesModel, @ageingModel, ";
                    command.CommandText +=
                        "@complexionModel, @sundamageModel, @frecklesModel, @noseWidth, @noseHeight, @noseLength, @noseBridge, @noseTip, @noseShift, @browHeight, ";
                    command.CommandText +=
                        "@browWidth, @cheekboneHeight, @cheekboneWidth, @cheeksWidth, @eyes, @lips, @jawWidth, @jawHeight, @chinLength, @chinPosition, @chinWidth, ";
                    command.CommandText +=
                        "@chinShape, @neckWidth, @eyesColor, @eyebrowsModel, @eyebrowsColor, @makeupModel, @blushModel, @blushColor, @lipstickModel, @lipstickColor)";
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.Parameters.AddWithValue("@firstHeadShape", skin.firstHeadShape);
                    command.Parameters.AddWithValue("@secondHeadShape", skin.secondHeadShape);
                    command.Parameters.AddWithValue("@firstSkinTone", skin.firstSkinTone);
                    command.Parameters.AddWithValue("@secondSkinTone", skin.secondSkinTone);
                    command.Parameters.AddWithValue("@headMix", skin.headMix);
                    command.Parameters.AddWithValue("@skinMix", skin.skinMix);
                    command.Parameters.AddWithValue("@hairModel", skin.hairModel);
                    command.Parameters.AddWithValue("@firstHairColor", skin.firstHairColor);
                    command.Parameters.AddWithValue("@secondHairColor", skin.secondHairColor);
                    command.Parameters.AddWithValue("@beardModel", skin.beardModel);
                    command.Parameters.AddWithValue("@beardColor", skin.beardColor);
                    command.Parameters.AddWithValue("@chestModel", skin.chestModel);
                    command.Parameters.AddWithValue("@chestColor", skin.chestColor);
                    command.Parameters.AddWithValue("@blemishesModel", skin.blemishesModel);
                    command.Parameters.AddWithValue("@ageingModel", skin.ageingModel);
                    command.Parameters.AddWithValue("@complexionModel", skin.complexionModel);
                    command.Parameters.AddWithValue("@sundamageModel", skin.sundamageModel);
                    command.Parameters.AddWithValue("@frecklesModel", skin.frecklesModel);
                    command.Parameters.AddWithValue("@noseWidth", skin.noseWidth);
                    command.Parameters.AddWithValue("@noseHeight", skin.noseHeight);
                    command.Parameters.AddWithValue("@noseLength", skin.noseLength);
                    command.Parameters.AddWithValue("@noseBridge", skin.noseBridge);
                    command.Parameters.AddWithValue("@noseTip", skin.noseTip);
                    command.Parameters.AddWithValue("@noseShift", skin.noseShift);
                    command.Parameters.AddWithValue("@browHeight", skin.browHeight);
                    command.Parameters.AddWithValue("@browWidth", skin.browWidth);
                    command.Parameters.AddWithValue("@cheekboneHeight", skin.cheekboneHeight);
                    command.Parameters.AddWithValue("@cheekboneWidth", skin.cheekboneWidth);
                    command.Parameters.AddWithValue("@cheeksWidth", skin.cheeksWidth);
                    command.Parameters.AddWithValue("@eyes", skin.eyes);
                    command.Parameters.AddWithValue("@lips", skin.lips);
                    command.Parameters.AddWithValue("@jawWidth", skin.jawWidth);
                    command.Parameters.AddWithValue("@jawHeight", skin.jawHeight);
                    command.Parameters.AddWithValue("@chinLength", skin.chinLength);
                    command.Parameters.AddWithValue("@chinPosition", skin.chinPosition);
                    command.Parameters.AddWithValue("@chinWidth", skin.chinWidth);
                    command.Parameters.AddWithValue("@chinShape", skin.chinShape);
                    command.Parameters.AddWithValue("@neckWidth", skin.neckWidth);
                    command.Parameters.AddWithValue("@eyesColor", skin.eyesColor);
                    command.Parameters.AddWithValue("@eyebrowsModel", skin.eyebrowsModel);
                    command.Parameters.AddWithValue("@eyebrowsColor", skin.eyebrowsColor);
                    command.Parameters.AddWithValue("@makeupModel", skin.makeupModel);
                    command.Parameters.AddWithValue("@blushModel", skin.blushModel);
                    command.Parameters.AddWithValue("@blushColor", skin.blushColor);
                    command.Parameters.AddWithValue("@lipstickModel", skin.lipstickModel);
                    command.Parameters.AddWithValue("@lipstickColor", skin.lipstickColor);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION CreateCharacter] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION CreateCharacter] " + ex.StackTrace);
                    player.TriggerEvent("characterNameDuplicated", playerModel.realName);
                }
            }

            return playerId;
        }

        public static SkinModel GetCharacterSkin(int characterId) {
            var skin = new SkinModel();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM skins WHERE characterId = @characterId LIMIT 1";
                command.Parameters.AddWithValue("@characterId", characterId);

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows) {
                        reader.Read();
                        skin.firstHeadShape = reader.GetInt32("firstHeadShape");
                        skin.secondHeadShape = reader.GetInt32("secondHeadShape");
                        skin.firstSkinTone = reader.GetInt32("firstSkinTone");
                        skin.secondSkinTone = reader.GetInt32("secondSkinTone");
                        skin.headMix = reader.GetFloat("headMix");
                        skin.skinMix = reader.GetFloat("skinMix");
                        skin.hairModel = reader.GetInt32("hairModel");
                        skin.firstHairColor = reader.GetInt32("firstHairColor");
                        skin.secondHairColor = reader.GetInt32("secondHairColor");
                        skin.beardModel = reader.GetInt32("beardModel");
                        skin.beardColor = reader.GetInt32("beardColor");
                        skin.chestModel = reader.GetInt32("chestModel");
                        skin.chestColor = reader.GetInt32("chestColor");
                        skin.blemishesModel = reader.GetInt32("blemishesModel");
                        skin.ageingModel = reader.GetInt32("ageingModel");
                        skin.complexionModel = reader.GetInt32("complexionModel");
                        skin.sundamageModel = reader.GetInt32("sundamageModel");
                        skin.frecklesModel = reader.GetInt32("frecklesModel");
                        skin.noseWidth = reader.GetFloat("noseWidth");
                        skin.noseHeight = reader.GetFloat("noseHeight");
                        skin.noseLength = reader.GetFloat("noseLength");
                        skin.noseBridge = reader.GetFloat("noseBridge");
                        skin.noseTip = reader.GetFloat("noseTip");
                        skin.noseShift = reader.GetFloat("noseShift");
                        skin.browHeight = reader.GetFloat("browHeight");
                        skin.browWidth = reader.GetFloat("browWidth");
                        skin.cheekboneHeight = reader.GetFloat("cheekboneHeight");
                        skin.cheekboneWidth = reader.GetFloat("cheekboneWidth");
                        skin.cheeksWidth = reader.GetFloat("cheeksWidth");
                        skin.eyes = reader.GetFloat("eyes");
                        skin.lips = reader.GetFloat("lips");
                        skin.jawWidth = reader.GetFloat("jawWidth");
                        skin.jawHeight = reader.GetFloat("jawHeight");
                        skin.chinLength = reader.GetFloat("chinLength");
                        skin.chinPosition = reader.GetFloat("chinPosition");
                        skin.chinWidth = reader.GetFloat("chinWidth");
                        skin.chinShape = reader.GetFloat("chinShape");
                        skin.neckWidth = reader.GetFloat("neckWidth");
                        skin.eyesColor = reader.GetInt32("eyesColor");
                        skin.eyebrowsModel = reader.GetInt32("eyebrowsModel");
                        skin.eyebrowsColor = reader.GetInt32("eyebrowsColor");
                        skin.makeupModel = reader.GetInt32("makeupModel");
                        skin.blushModel = reader.GetInt32("blushModel");
                        skin.blushColor = reader.GetInt32("blushColor");
                        skin.lipstickModel = reader.GetInt32("lipstickModel");
                        skin.lipstickColor = reader.GetInt32("lipstickColor");
                    }
                }
            }

            return skin;
        }

        public static void UpdateCharacterHair(int playerId, SkinModel skin) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                        "UPDATE skins SET hairModel = @hairModel, firstHairColor = @firstHairColor, secondHairColor = @secondHairColor, beardModel = @beardModel, ";
                    command.CommandText +=
                        "beardColor = @beardColor, eyebrowsModel = @eyebrowsModel, eyebrowsColor = @eyebrowsColor WHERE characterId = @playerId LIMIT 1";
                    command.Parameters.AddWithValue("@hairModel", skin.hairModel);
                    command.Parameters.AddWithValue("@firstHairColor", skin.firstHairColor);
                    command.Parameters.AddWithValue("@secondHairColor", skin.secondHairColor);
                    command.Parameters.AddWithValue("@beardModel", skin.beardModel);
                    command.Parameters.AddWithValue("@beardColor", skin.beardColor);
                    command.Parameters.AddWithValue("@eyebrowsModel", skin.eyebrowsModel);
                    command.Parameters.AddWithValue("@eyebrowsColor", skin.eyebrowsColor);
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateCharacterHair] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateCharacterHair] " + ex.StackTrace);
                }
            }
        }

        public static PlayerModel LoadCharacterInformationById(int characterId) {
            var character = new PlayerModel();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM users WHERE id = @characterId LIMIT 1";
                command.Parameters.AddWithValue("@characterId", characterId);

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows) {
                        reader.Read();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");
                        var rot = reader.GetFloat("rotation");

                        character.id = reader.GetInt32("id");
                        character.realName = reader.GetString("name");
                        character.adminRank = reader.GetInt32("adminRank");
                        character.adminName = reader.GetString("adminName");
                        character.position = new Vector3(posX, posY, posZ);
                        character.rotation = new Vector3(0.0, 0.0, rot);
                        character.money = reader.GetInt32("money");
                        character.bank = reader.GetInt32("bank");
                        character.health = reader.GetInt32("health");
                        character.armor = reader.GetInt32("armor");
                        character.age = reader.GetInt32("age");
                        character.sex = reader.GetInt32("sex");
                        character.faction = reader.GetInt32("faction");
                        character.job = reader.GetInt32("job");
                        character.rank = reader.GetInt32("rank");
                        character.duty = reader.GetInt32("duty");
                        character.phone = reader.GetInt32("phone");
                        character.radio = reader.GetInt32("radio");
                        character.killed = reader.GetInt32("killed");
                        character.jailed = reader.GetString("jailed");
                        character.carKeys = reader.GetString("carKeys");
                        character.documentation = reader.GetInt32("documentation");
                        character.licenses = reader.GetString("licenses");
                        character.insurance = reader.GetInt32("insurance");
                        character.weaponLicense = reader.GetInt32("weaponLicense");
                        character.houseRent = reader.GetInt32("houseRent");
                        character.houseEntered = reader.GetInt32("houseEntered");
                        character.businessEntered = reader.GetInt32("businessEntered");
                        character.employeeCooldown = reader.GetInt32("employeeCooldown");
                        character.jobCooldown = reader.GetInt32("jobCooldown");
                        character.jobDeliver = reader.GetInt32("jobDeliver");
                        character.jobPoints = reader.GetString("jobPoints");
                        character.rolePoints = reader.GetInt32("rolePoints");
                        character.status = reader.GetInt32("status");
                        character.played = reader.GetInt32("played");
                    }
                }
            }

            return character;
        }

        public static PlayerModel LoadCharacterInformationByName(string characterName) {
            var character = new PlayerModel();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM users WHERE name = @characterName LIMIT 1 ";
                command.Parameters.AddWithValue("@characterName", characterName);

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows) {
                        reader.Read();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");
                        var rot = reader.GetFloat("rotation");

                        character.id = reader.GetInt32("id");
                        character.realName = reader.GetString("name");
                        character.adminRank = reader.GetInt32("adminRank");
                        character.adminName = reader.GetString("adminName");
                        character.position = new Vector3(posX, posY, posZ);
                        character.rotation = new Vector3(0.0, 0.0, rot);
                        character.money = reader.GetInt32("money");
                        character.bank = reader.GetInt32("bank");
                        character.health = reader.GetInt32("health");
                        character.armor = reader.GetInt32("armor");
                        character.age = reader.GetInt32("age");
                        character.sex = reader.GetInt32("sex");
                        character.faction = reader.GetInt32("faction");
                        character.job = reader.GetInt32("job");
                        character.rank = reader.GetInt32("rank");
                        character.duty = reader.GetInt32("duty");
                        character.phone = reader.GetInt32("phone");
                        character.radio = reader.GetInt32("radio");
                        character.killed = reader.GetInt32("killed");
                        character.jailed = reader.GetString("jailed");
                        character.carKeys = reader.GetString("carKeys");
                        character.documentation = reader.GetInt32("documentation");
                        character.licenses = reader.GetString("licenses");
                        character.insurance = reader.GetInt32("insurance");
                        character.weaponLicense = reader.GetInt32("weaponLicense");
                        character.houseRent = reader.GetInt32("houseRent");
                        character.houseEntered = reader.GetInt32("houseEntered");
                        character.businessEntered = reader.GetInt32("businessEntered");
                        character.employeeCooldown = reader.GetInt32("employeeCooldown");
                        character.jobCooldown = reader.GetInt32("jobCooldown");
                        character.jobDeliver = reader.GetInt32("jobDeliver");
                        character.jobPoints = reader.GetString("jobPoints");
                        character.rolePoints = reader.GetInt32("rolePoints");
                        character.status = reader.GetInt32("status");
                        character.played = reader.GetInt32("played");
                    }
                }
            }

            return character;
        }

        public static void SaveCharacterInformation(PlayerModel player) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                        "UPDATE users SET posX = @posX, posY = @posY, posZ = @posZ, rotation = @rotation, money = @money, bank = @bank, health = @health, armor = @armor, ";
                    command.CommandText +=
                        "radio = @radio, killed = @killed, jailed = @jailed, faction = @faction, job = @job, `rank` = @rank, duty = @duty, phone = @phone, carKeys = @carKeys, ";
                    command.CommandText +=
                        "documentation = @documentation, licenses = @licenses, insurance = @insurance, weaponLicense = @weaponLicense, houseRent = @houseRent, ";
                    command.CommandText +=
                        "houseEntered = @houseEntered, businessEntered = @businessEntered, employeeCooldown = @employeeCooldown, jobCooldown = @jobCooldown, ";
                    command.CommandText +=
                        "jobDeliver = @jobDeliver, jobPoints = @jobPoints, rolePoints = @rolePoints, played = @played WHERE id = @playerId LIMIT 1";
                    command.Parameters.AddWithValue("@posX", player.position.X);
                    command.Parameters.AddWithValue("@posY", player.position.Y);
                    command.Parameters.AddWithValue("@posZ", player.position.Z);
                    command.Parameters.AddWithValue("@rotation", player.rotation.Z);
                    command.Parameters.AddWithValue("@money", player.money);
                    command.Parameters.AddWithValue("@bank", player.bank);
                    command.Parameters.AddWithValue("@health", player.health);
                    command.Parameters.AddWithValue("@armor", player.armor);
                    command.Parameters.AddWithValue("@radio", player.radio);
                    command.Parameters.AddWithValue("@killed", player.killed);
                    command.Parameters.AddWithValue("@jailed", player.jailed);
                    command.Parameters.AddWithValue("@faction", player.faction);
                    command.Parameters.AddWithValue("@job", player.job);
                    command.Parameters.AddWithValue("@rank", player.rank);
                    command.Parameters.AddWithValue("@duty", player.duty);
                    command.Parameters.AddWithValue("@phone", player.phone);
                    command.Parameters.AddWithValue("@carKeys", player.carKeys);
                    command.Parameters.AddWithValue("@documentation", player.documentation);
                    command.Parameters.AddWithValue("@licenses", player.licenses);
                    command.Parameters.AddWithValue("@insurance", player.insurance);
                    command.Parameters.AddWithValue("@weaponLicense", player.weaponLicense);
                    command.Parameters.AddWithValue("@houseRent", player.houseRent);
                    command.Parameters.AddWithValue("@houseEntered", player.houseEntered);
                    command.Parameters.AddWithValue("@businessEntered", player.businessEntered);
                    command.Parameters.AddWithValue("@employeeCooldown", player.employeeCooldown);
                    command.Parameters.AddWithValue("@jobCooldown", player.jobCooldown);
                    command.Parameters.AddWithValue("@jobDeliver", player.jobDeliver);
                    command.Parameters.AddWithValue("@jobPoints", player.jobPoints);
                    command.Parameters.AddWithValue("@rolePoints", player.rolePoints);
                    command.Parameters.AddWithValue("@played", player.played);
                    command.Parameters.AddWithValue("@playerId", player.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION SaveCharacterInformation] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION SaveCharacterInformation] " + ex.StackTrace);
                }
            }
        }

        public static void UpdateLastCharacter(string socialName, int playerId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                        "UPDATE accounts SET lastCharacter = @playerId WHERE socialName = @socialName";
                    command.Parameters.AddWithValue("@playerId", playerId);
                    command.Parameters.AddWithValue("@socialName", socialName);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateLastCharacter] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateLastCharacter] " + ex.StackTrace);
                }
            }
        }

        public static bool FindCharacter(string name) {
            var found = false;

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT id FROM users WHERE name = @name LIMIT 1";
                command.Parameters.AddWithValue("@name", name);

                using (var reader = command.ExecuteReader()) {
                    found = reader.HasRows;
                }
            }

            return found;
        }


        public static List<VehicleModel> LoadAllVehicles() {
            var vehicleList = new List<VehicleModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM vehicles";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var vehicle = new VehicleModel();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");
                        var rotation = reader.GetFloat("rotation");

                        vehicle.id = reader.GetInt32("id");
                        vehicle.model = reader.GetString("model");
                        vehicle.colorType = reader.GetInt32("colorType");
                        vehicle.firstColor = reader.GetString("firstColor");
                        vehicle.secondColor = reader.GetString("secondColor");
                        vehicle.pearlescent = reader.GetInt32("pearlescent");
                        vehicle.owner = reader.GetString("owner");
                        vehicle.plate = reader.GetString("plate");
                        vehicle.dimension = reader.GetUInt32("dimension");
                        vehicle.faction = reader.GetInt32("faction");
                        vehicle.engine = reader.GetInt32("engine");
                        vehicle.locked = reader.GetInt32("locked");
                        vehicle.price = reader.GetInt32("price");
                        vehicle.parking = reader.GetInt32("parking");
                        vehicle.parked = reader.GetInt32("parkedTime");
                        vehicle.gas = reader.GetFloat("gas");
                        vehicle.kms = reader.GetFloat("kms");
                        vehicle.position = new Vector3(posX, posY, posZ);
                        vehicle.rotation = new Vector3(0.0, 0.0, rotation);

                        vehicleList.Add(vehicle);
                    }
                }
            }

            return vehicleList;
        }

        public static int AddNewVehicle(VehicleModel vehicle) {
            var vehId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText =
                        "INSERT INTO vehicles (model, posX, posY, posZ, rotation, firstColor, secondColor, dimension, faction, owner, plate, gas) ";
                    command.CommandText +=
                        "VALUES (@model, @posX, @posY, @posZ, @rotation, @firstColor, @secondColor, @dimension, @faction, @owner, @plate, @gas)";
                    command.Parameters.AddWithValue("@model", vehicle.model);
                    command.Parameters.AddWithValue("@posX", vehicle.position.X);
                    command.Parameters.AddWithValue("@posY", vehicle.position.Y);
                    command.Parameters.AddWithValue("@posZ", vehicle.position.Z);
                    command.Parameters.AddWithValue("@rotation", vehicle.rotation.Z);
                    command.Parameters.AddWithValue("@firstColor", vehicle.firstColor);
                    command.Parameters.AddWithValue("@secondColor", vehicle.secondColor);
                    command.Parameters.AddWithValue("@dimension", vehicle.dimension);
                    command.Parameters.AddWithValue("@faction", vehicle.faction);
                    command.Parameters.AddWithValue("@owner", vehicle.owner);
                    command.Parameters.AddWithValue("@plate", vehicle.plate);
                    command.Parameters.AddWithValue("@gas", vehicle.gas);

                    command.ExecuteNonQuery();
                    vehId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewVehicle] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewVehicle] " + ex.StackTrace);
                }
            }

            return vehId;
        }

        public static void UpdateVehicleColor(VehicleModel vehicle) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE vehicles SET colorType = @colorType, firstColor = @firstColor, ";
                    command.CommandText +=
                        "secondColor = @secondColor, pearlescent = @pearlescent WHERE id = @vehId LIMIT 1";
                    command.Parameters.AddWithValue("@colorType", vehicle.colorType);
                    command.Parameters.AddWithValue("@firstColor", vehicle.firstColor);
                    command.Parameters.AddWithValue("@secondColor", vehicle.secondColor);
                    command.Parameters.AddWithValue("@pearlescent", vehicle.pearlescent);
                    command.Parameters.AddWithValue("@vehId", vehicle.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehicleColor] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehicleColor] " + ex.StackTrace);
                }
            }
        }

        public static void UpdateVehiclePosition(VehicleModel vehicle) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE vehicles SET posX = @posX, posY = @posY, posZ = @posZ, rotation = @rotation WHERE id = @vehId LIMIT 1";
                    command.Parameters.AddWithValue("@posX", vehicle.position.X);
                    command.Parameters.AddWithValue("@posY", vehicle.position.Y);
                    command.Parameters.AddWithValue("@posZ", vehicle.position.Z);
                    command.Parameters.AddWithValue("@rotation", vehicle.rotation.Z);
                    command.Parameters.AddWithValue("@vehId", vehicle.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehiclePosition] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehiclePosition] " + ex.StackTrace);
                }
            }
        }

        public static void UpdateVehicleSingleValue(string table, int value, int vehicleId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE vehicles SET " + table + " = @value WHERE id = @vehId LIMIT 1";
                    command.Parameters.AddWithValue("@value", value);
                    command.Parameters.AddWithValue("@vehId", vehicleId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehicleSingleValue] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehicleSingleValue] " + ex.StackTrace);
                }
            }
        }

        public static void UpdateVehicleSingleString(string table, string value, int vehicleId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE vehicles SET " + table + " = @value WHERE id = @vehId LIMIT 1";
                    command.Parameters.AddWithValue("@value", value);
                    command.Parameters.AddWithValue("@vehId", vehicleId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehicleSingleString] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateVehicleSingleString] " + ex.StackTrace);
                }
            }
        }

        public static void SaveVehicle(VehicleModel vehicle) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE vehicles SET posX = @posX, posY = @posY, posZ = @posZ, rotation = @rotation, colorType = @colorType, ";
                    command.CommandText +=
                        "firstColor = @firstColor, secondColor = @secondColor, pearlescent = @pearlescent, dimension = @dimension, ";
                    command.CommandText +=
                        "engine = @engine, locked = @locked, faction = @faction, owner = @owner, plate = @plate, price = @price, ";
                    command.CommandText +=
                        "parking = @parking, parkedTime = @parkedTime, gas = @gas, kms = @kms WHERE id = @vehId LIMIT 1";
                    command.Parameters.AddWithValue("@posX", vehicle.position.X);
                    command.Parameters.AddWithValue("@posY", vehicle.position.Y);
                    command.Parameters.AddWithValue("@posZ", vehicle.position.Z);
                    command.Parameters.AddWithValue("@rotation", vehicle.rotation.Z);
                    command.Parameters.AddWithValue("@colorType", vehicle.colorType);
                    command.Parameters.AddWithValue("@firstColor", vehicle.firstColor);
                    command.Parameters.AddWithValue("@secondColor", vehicle.secondColor);
                    command.Parameters.AddWithValue("@pearlescent", vehicle.pearlescent);
                    command.Parameters.AddWithValue("@dimension", vehicle.dimension);
                    command.Parameters.AddWithValue("@engine", vehicle.engine);
                    command.Parameters.AddWithValue("@locked", vehicle.locked);
                    command.Parameters.AddWithValue("@faction", vehicle.faction);
                    command.Parameters.AddWithValue("@owner", vehicle.owner);
                    command.Parameters.AddWithValue("@plate", vehicle.plate);
                    command.Parameters.AddWithValue("@price", vehicle.price);
                    command.Parameters.AddWithValue("@parking", vehicle.parking);
                    command.Parameters.AddWithValue("@parkedTime", vehicle.parked);
                    command.Parameters.AddWithValue("@gas", vehicle.gas);
                    command.Parameters.AddWithValue("@kms", vehicle.kms);
                    command.Parameters.AddWithValue("@vehId", vehicle.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION SaveVehicle] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION SaveVehicle] " + ex.StackTrace);
                }
            }
        }

        public static void SaveAllVehicles(List<VehicleModel> vehicleList) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE vehicles SET posX = @posX, posY = @posY, posZ = @posZ, rotation = @rotation, colorType = @colorType, ";
                    command.CommandText +=
                        "firstColor = @firstColor, secondColor = @secondColor, pearlescent = @pearlescent, dimension = @dimension, ";
                    command.CommandText +=
                        "engine = @engine, locked = @locked, faction = @faction, owner = @owner, plate = @plate, price = @price, ";
                    command.CommandText +=
                        "parking = @parking, parkedTime = @parkedTime, gas = @gas, kms = @kms WHERE id = @vehId LIMIT 1";

                    foreach (var vehicle in vehicleList) {
                        command.Parameters.Clear();

                        command.Parameters.AddWithValue("@posX", vehicle.position.X);
                        command.Parameters.AddWithValue("@posY", vehicle.position.Y);
                        command.Parameters.AddWithValue("@posZ", vehicle.position.Z);
                        command.Parameters.AddWithValue("@rotation", vehicle.rotation.Z);
                        command.Parameters.AddWithValue("@colorType", vehicle.colorType);
                        command.Parameters.AddWithValue("@firstColor", vehicle.firstColor);
                        command.Parameters.AddWithValue("@secondColor", vehicle.secondColor);
                        command.Parameters.AddWithValue("@pearlescent", vehicle.pearlescent);
                        command.Parameters.AddWithValue("@dimension", vehicle.dimension);
                        command.Parameters.AddWithValue("@engine", vehicle.engine);
                        command.Parameters.AddWithValue("@locked", vehicle.locked);
                        command.Parameters.AddWithValue("@faction", vehicle.faction);
                        command.Parameters.AddWithValue("@owner", vehicle.owner);
                        command.Parameters.AddWithValue("@plate", vehicle.plate);
                        command.Parameters.AddWithValue("@price", vehicle.price);
                        command.Parameters.AddWithValue("@parking", vehicle.parking);
                        command.Parameters.AddWithValue("@parkedTime", vehicle.parked);
                        command.Parameters.AddWithValue("@gas", vehicle.gas);
                        command.Parameters.AddWithValue("@kms", vehicle.kms);
                        command.Parameters.AddWithValue("@vehId", vehicle.id);

                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION SaveAllVehicles] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION SaveAllVehicles] " + ex.StackTrace);
                }
            }
        }

        public static void RemoveVehicle(int vehicleId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM vehicles WHERE id = @vehicleId LIMIT 1";
                    command.Parameters.AddWithValue("@vehicleId", vehicleId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveVehicle] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveVehicle] " + ex.StackTrace);
                }
            }
        }



        public static void LogHotwire(string playerName, int vehicleId, Vector3 position) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO hotwires VALUES (@vehicleId, @playerName, @posX, @posY, @posZ, NOW())";
                    command.Parameters.AddWithValue("@playerName", playerName);
                    command.Parameters.AddWithValue("@vehicleId", vehicleId);
                    command.Parameters.AddWithValue("@posX", position.X);
                    command.Parameters.AddWithValue("@posY", position.Y);
                    command.Parameters.AddWithValue("@posZ", position.Z);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION LogHotwire] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION LogHotwire] " + ex.StackTrace);
                }
            }
        }

        public static List<ItemModel> LoadAllItems() {
            var itemList = new List<ItemModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM items";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var item = new ItemModel();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");

                        item.id = reader.GetInt32("id");
                        item.hash = reader.GetString("hash");
                        item.ownerEntity = reader.GetString("ownerEntity");
                        item.ownerIdentifier = reader.GetInt32("ownerIdentifier");
                        item.amount = reader.GetInt32("amount");
                        item.position = new Vector3(posX, posY, posZ);
                        item.dimension = reader.GetUInt32("dimension");

                        itemList.Add(item);
                    }
                }
            }

            return itemList;
        }

        public static int AddNewItem(ItemModel item) {
            var itemId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO `items` (`hash`, `ownerEntity`, `ownerIdentifier`, `amount`, `posX`, `posY`, `posZ`)";
                    command.CommandText +=
                        " VALUES (@hash, @ownerEntity, @ownerIdentifier, @amount, @posX, @posY, @posZ)";
                    command.Parameters.AddWithValue("@hash", item.hash);
                    command.Parameters.AddWithValue("@ownerEntity", item.ownerEntity);
                    command.Parameters.AddWithValue("@ownerIdentifier", item.ownerIdentifier);
                    command.Parameters.AddWithValue("@amount", item.amount);
                    command.Parameters.AddWithValue("@posX", item.position.X);
                    command.Parameters.AddWithValue("@posY", item.position.Y);
                    command.Parameters.AddWithValue("@posZ", item.position.Z);

                    command.ExecuteNonQuery();
                    itemId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewItem] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewItem] " + ex.StackTrace);
                }
            }

            return itemId;
        }

        public static void UpdateItem(ItemModel item) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE `items` SET `ownerEntity` = @ownerEntity, `ownerIdentifier` = @ownerIdentifier, `amount` = @amount, ";
                    command.CommandText +=
                        "`posX` = @posX, `posY` = @posY, `posZ` = @posZ, `dimension` = @dimension WHERE `id` = @id LIMIT 1";
                    command.Parameters.AddWithValue("@ownerEntity", item.ownerEntity);
                    command.Parameters.AddWithValue("@ownerIdentifier", item.ownerIdentifier);
                    command.Parameters.AddWithValue("@amount", item.amount);
                    command.Parameters.AddWithValue("@posX", item.position.X);
                    command.Parameters.AddWithValue("@posY", item.position.Y);
                    command.Parameters.AddWithValue("@posZ", item.position.Z);
                    command.Parameters.AddWithValue("@dimension", item.dimension);
                    command.Parameters.AddWithValue("@id", item.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateItem] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateItem] " + ex.StackTrace);
                }
            }
        }

        public static void RemoveItem(int id) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM items WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveItem] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveItem] " + ex.StackTrace);
                }
            }
        }



        public static void KickTenantsOut(int houseId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE users SET houseRent = 0 where houseRent = @houseRent";
                    command.Parameters.AddWithValue("@houseRent", houseId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION KickTenantsOut] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION KickTenantsOut] " + ex.StackTrace);
                }
            }
        }


        public static void LoadCrimes() {
            // Initialize the list
            Police.crimeList = new List<CrimeModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText = "SELECT * FROM `crimes`";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var crime = new CrimeModel();
                        {
                            crime.crime = reader.GetString("description");
                            crime.jail = reader.GetInt32("jail");
                            crime.fine = reader.GetInt32("fine");
                            crime.reminder = reader.GetString("reminder");
                        }

                        Police.crimeList.Add(crime);
                    }
                }
            }
        }

        public static void LoadAllPoliceControls() {
            Police.policeControlList = new List<PoliceControlModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM controls";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var policeControl = new PoliceControlModel();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");
                        var rot = reader.GetFloat("rotation");

                        policeControl.id = reader.GetInt32("id");
                        policeControl.name = reader.GetString("name");
                        policeControl.item = reader.GetInt32("item");
                        policeControl.position = new Vector3(posX, posY, posZ);
                        policeControl.rotation = new Vector3(0.0f, 0.0f, rot);

                        Police.policeControlList.Add(policeControl);
                    }
                }
            }
        }

        public static List<ParkingModel> LoadAllParkings() {
            var parkingList = new List<ParkingModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM parkings";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var parking = new ParkingModel();
                        var posX = reader.GetFloat("posX");
                        var posY = reader.GetFloat("posY");
                        var posZ = reader.GetFloat("posZ");

                        parking.id = reader.GetInt32("id");
                        parking.type = reader.GetInt32("type");
                        parking.houseId = reader.GetInt32("house");
                        parking.capacity = reader.GetInt32("capacity");
                        parking.position = new Vector3(posX, posY, posZ);

                        parkingList.Add(parking);
                    }
                }
            }

            return parkingList;
        }

        public static int AddParking(ParkingModel parking) {
            var parkingId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO parkings (type, posX, posY, posZ) VALUES (@type, @posX, @posY, @posZ)";
                    command.Parameters.AddWithValue("@type", parking.type);
                    command.Parameters.AddWithValue("@posX", parking.position.X);
                    command.Parameters.AddWithValue("@posY", parking.position.Y);
                    command.Parameters.AddWithValue("@posZ", parking.position.Z);

                    command.ExecuteNonQuery();
                    parkingId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddParking] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddParking] " + ex.StackTrace);
                }
            }

            return parkingId;
        }

        public static void UpdateParking(ParkingModel parking) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE parkings SET type = @type, house = @house, posX = @posX, posY = @posY, posZ = @posZ, capacity = @capacity WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@type", parking.type);
                    command.Parameters.AddWithValue("@house", parking.houseId);
                    command.Parameters.AddWithValue("@posX", parking.position.X);
                    command.Parameters.AddWithValue("@posY", parking.position.Y);
                    command.Parameters.AddWithValue("@posZ", parking.position.Z);
                    command.Parameters.AddWithValue("@capacity", parking.capacity);
                    command.Parameters.AddWithValue("@id", parking.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateParking] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateParking] " + ex.StackTrace);
                }
            }
        }

        public static void DeleteParking(int parkingId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM parkings WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", parkingId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeleteParking] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeleteParking] " + ex.StackTrace);
                }
            }
        }

        public static void RenamePoliceControl(string sourceName, string targetName) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE controls SET name = @targetName WHERE name = @sourceName";
                    command.Parameters.AddWithValue("@targetName", targetName);
                    command.Parameters.AddWithValue("@sourceName", sourceName);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION RenamePoliceControl] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION RenamePoliceControl] " + ex.StackTrace);
                }
            }
        }

        public static void DeletePoliceControl(string name) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM controls WHERE name = @name";
                    command.Parameters.AddWithValue("@name", name);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeletePoliceControl] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeletePoliceControl] " + ex.StackTrace);
                }
            }
        }

        public static int AddPoliceControlItem(PoliceControlModel policeControlItem) {
            var policeControlId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO controls (name, item, posX, posY, posZ, rotation) VALUES (@name, @item, @posX, @posY, @posZ, @rotation)";
                    command.Parameters.AddWithValue("@name", policeControlItem.name);
                    command.Parameters.AddWithValue("@item", policeControlItem.item);
                    command.Parameters.AddWithValue("@posX", policeControlItem.position.X);
                    command.Parameters.AddWithValue("@posY", policeControlItem.position.Y);
                    command.Parameters.AddWithValue("@posZ", policeControlItem.position.Z);
                    command.Parameters.AddWithValue("@rotation", policeControlItem.rotation.Z);

                    command.ExecuteNonQuery();
                    policeControlId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddPoliceControlItem] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddPoliceControlItem] " + ex.StackTrace);
                }
            }

            return policeControlId;
        }

        public static void DeletePoliceControlItem(int policeControlItemId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM controls WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", policeControlItemId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeletePoliceControlItem] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeletePoliceControlItem] " + ex.StackTrace);
                }
            }
        }

        public static List<FineModel> LoadPlayerFines(string name) {
            var fineList = new List<FineModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM fines WHERE target = @target";
                command.Parameters.AddWithValue("@target", name);

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var fine = new FineModel();
                        {
                            fine.officer = reader.GetString("officer");
                            fine.target = reader.GetString("target");
                            fine.amount = reader.GetInt32("amount");
                            fine.reason = reader.GetString("reason");
                            fine.date = reader.GetString("date");
                        }

                        fineList.Add(fine);
                    }
                }
            }

            return fineList;
        }

        public static void InsertFine(FineModel fine) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "INSERT INTO fines VALUES (@officer, @target, @amount, @reason, NOW())";
                    command.Parameters.AddWithValue("@officer", fine.officer);
                    command.Parameters.AddWithValue("@target", fine.target);
                    command.Parameters.AddWithValue("@amount", fine.amount);
                    command.Parameters.AddWithValue("@reason", fine.reason);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION InsertFine] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION InsertFine] " + ex.StackTrace);
                }
            }
        }

        public static void RemoveFines(List<FineModel> fineList) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "DELETE FROM fines WHERE officer = @officer AND target = @target AND date = @date LIMIT 1";

                    foreach (var fine in fineList) {
                        var dateTime = DateTime.ParseExact(fine.date, "d/M/yyyy h:mm:ss tt",
                            CultureInfo.InvariantCulture);
                        fine.date = dateTime.ToString("yyyy-dd-MM HH:mm:ss");

                        command.Parameters.Clear();

                        command.Parameters.AddWithValue("@officer", fine.officer);
                        command.Parameters.AddWithValue("@target", fine.target);
                        command.Parameters.AddWithValue("@date", fine.date);

                        // Ejecutamos la query
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveFines] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveFines] " + ex.StackTrace);
                }
            }
        }

        public static List<ChannelModel> LoadAllChannels() {
            var channelList = new List<ChannelModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM channels";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var channel = new ChannelModel();
                        {
                            channel.id = reader.GetInt32("id");
                            channel.owner = reader.GetInt32("owner");
                            channel.password = reader.GetString("password");
                        }

                        channelList.Add(channel);
                    }
                }
            }

            return channelList;
        }

        public static int AddChannel(ChannelModel channel) {
            var channelId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "INSERT INTO channels (owner, password) VALUES (@owner, @password)";
                    command.Parameters.AddWithValue("@owner", channel.owner);
                    command.Parameters.AddWithValue("@password", channel.password);

                    command.ExecuteNonQuery();
                    channelId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddChannel] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddChannel] " + ex.StackTrace);
                }
            }

            return channelId;
        }

        public static void UpdateChannel(ChannelModel channel) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE channels SET password = @password WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@password", channel.password);
                    command.Parameters.AddWithValue("@id", channel.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateChannel] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateChannel] " + ex.StackTrace);
                }
            }
        }

        public static void RemoveChannel(int channelId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM channels WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", channelId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveChannel] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION RemoveChannel] " + ex.StackTrace);
                }
            }
        }

        public static void DisconnectFromChannel(int channelId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE users SET radio = 0 WHERE radio = @radio";
                    command.Parameters.AddWithValue("@radio", channelId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION DisconnectFromChannel] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION DisconnectFromChannel] " + ex.StackTrace);
                }
            }
        }

        public static List<BloodModel> LoadAllBlood() {
            var bloodList = new List<BloodModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM blood";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var blood = new BloodModel();
                        {
                            blood.id = reader.GetInt32("id");
                            blood.doctor = reader.GetInt32("doctor");
                            blood.patient = reader.GetInt32("patient");
                            blood.used = reader.GetBoolean("used");
                        }

                        bloodList.Add(blood);
                    }
                }
            }

            return bloodList;
        }

        public static List<AnnoucementModel> LoadAllAnnoucements() {
            var annoucementList = new List<AnnoucementModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM news";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var announcementModel = new AnnoucementModel();
                        {
                            announcementModel.id = reader.GetInt32("id");
                            announcementModel.winner = reader.GetInt32("journalist");
                            announcementModel.amount = reader.GetInt32("amount");
                            announcementModel.annoucement = reader.GetString("annoucement");
                            announcementModel.journalist = reader.GetInt32("winner");
                            announcementModel.given = reader.GetBoolean("given");
                        }

                        annoucementList.Add(announcementModel);
                    }
                }
            }

            return annoucementList;
        }

        public static int AddBloodTransaction(BloodModel blood) {
            var bloodId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO blood (doctor, patient, bloodType, used, date) VALUES (@doctor, @patient, @bloodType, @used, NOW())";
                    command.Parameters.AddWithValue("@doctor", blood.doctor);
                    command.Parameters.AddWithValue("@patient", blood.patient);
                    command.Parameters.AddWithValue("@bloodType", blood.type);
                    command.Parameters.AddWithValue("@used", blood.used);

                    command.ExecuteNonQuery();
                    bloodId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddBloodTransaction] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddBloodTransaction] " + ex.StackTrace);
                }
            }

            return bloodId;
        }

        public static int SendAnnoucement(AnnoucementModel annoucement) {
            var annoucementId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO news (winner, annoucement, amount, given, date) VALUES (@winner, @annoucement, @amount, @given, NOW())";
                    command.Parameters.AddWithValue("@winner", annoucement.winner);
                    command.Parameters.AddWithValue("@annoucement", annoucement.annoucement);
                    command.Parameters.AddWithValue("@amount", annoucement.amount);
                    command.Parameters.AddWithValue("@given", annoucement.given);

                    command.ExecuteNonQuery();
                    annoucementId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION SendAnnoucement] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION SendAnnoucement] " + ex.StackTrace);
                }
            }

            return annoucementId;
        }

        public static int GivePrize(AnnoucementModel prize) {
            var prizeId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO news (winner, journalist, annoucement, amount, given, date) VALUES (@winner, @journalist, @annoucement, @amount, @given, NOW())";
                    command.Parameters.AddWithValue("@winner", prize.winner);
                    command.Parameters.AddWithValue("@journalist", prize.journalist);
                    command.Parameters.AddWithValue("@annoucement", prize.annoucement);
                    command.Parameters.AddWithValue("@amount", prize.amount);
                    command.Parameters.AddWithValue("@given", prize.given);

                    command.ExecuteNonQuery();
                    prizeId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION GivePrize] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION GivePrize] " + ex.StackTrace);
                }
            }

            return prizeId;
        }

        public static List<ClothesModel> LoadAllClothes() {
            var clothesList = new List<ClothesModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM clothes";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var clothes = new ClothesModel();
                        {
                            clothes.id = reader.GetInt32("id");
                            clothes.player = reader.GetInt32("player");
                            clothes.type = reader.GetInt32("type");
                            clothes.slot = reader.GetInt32("slot");
                            clothes.drawable = reader.GetInt32("drawable");
                            clothes.texture = reader.GetInt32("texture");
                            clothes.dressed = reader.GetBoolean("dressed");
                        }

                        clothesList.Add(clothes);
                    }
                }
            }

            return clothesList;
        }

        public static int AddClothes(ClothesModel clothes) {
            var clothesId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO clothes (player, type, slot, drawable, texture, dressed) VALUES (@player, @type, @slot, @drawable, @texture, @dressed)";
                    command.Parameters.AddWithValue("@player", clothes.player);
                    command.Parameters.AddWithValue("@type", clothes.type);
                    command.Parameters.AddWithValue("@slot", clothes.slot);
                    command.Parameters.AddWithValue("@drawable", clothes.drawable);
                    command.Parameters.AddWithValue("@texture", clothes.texture);
                    command.Parameters.AddWithValue("@dressed", clothes.dressed);

                    command.ExecuteNonQuery();

                    clothesId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddClothes] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddClothes] " + ex.StackTrace);
                }
            }

            return clothesId;
        }

        public static void UpdateClothes(ClothesModel clothes) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "UPDATE clothes SET dressed = @dressed WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@dressed", clothes.dressed);
                    command.Parameters.AddWithValue("@id", clothes.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateClothes] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION UpdateClothes] " + ex.StackTrace);
                }
            }
        }

        public static List<TattooModel> LoadAllTattoos() {
            var tattooList = new List<TattooModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM tattoos";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var tattoo = new TattooModel();
                        {
                            tattoo.player = reader.GetInt32("player");
                            tattoo.slot = reader.GetInt32("zone");
                            tattoo.library = reader.GetString("library");
                            tattoo.hash = reader.GetString("hash");
                        }

                        tattooList.Add(tattoo);
                    }
                }
            }

            return tattooList;
        }

        public static bool AddTattoo(TattooModel tattoo) {
            var inserted = false;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO tattoos (player, zone, library, hash) VALUES (@player, @zone, @library, @hash)";
                    command.Parameters.AddWithValue("@player", tattoo.player);
                    command.Parameters.AddWithValue("@zone", tattoo.slot);
                    command.Parameters.AddWithValue("@library", tattoo.library);
                    command.Parameters.AddWithValue("@hash", tattoo.hash);

                    command.ExecuteNonQuery();
                    inserted = true;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddTattoo] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddTattoo] " + ex.StackTrace);
                }
            }

            return inserted;
        }

        public static List<ContactModel> LoadAllContacts() {
            var contactList = new List<ContactModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM contacts";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var contact = new ContactModel();
                        {
                            contact.id = reader.GetInt32("id");
                            contact.owner = reader.GetInt32("owner");
                            contact.contactNumber = reader.GetInt32("contactNumber");
                            contact.contactName = reader.GetString("contactName");
                        }

                        contactList.Add(contact);
                    }
                }
            }

            return contactList;
        }

        public static int AddNewContact(ContactModel contact) {
            var contactId = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO contacts (owner, contactNumber, contactName) VALUES (@owner, @contactNumber, @contactName)";
                    command.Parameters.AddWithValue("@owner", contact.owner);
                    command.Parameters.AddWithValue("@contactNumber", contact.contactNumber);
                    command.Parameters.AddWithValue("@contactName", contact.contactName);

                    command.ExecuteNonQuery();
                    contactId = (int) command.LastInsertedId;
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewContact] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddNewContact] " + ex.StackTrace);
                }
            }

            return contactId;
        }

        public static void ModifyContact(ContactModel contact) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "UPDATE contacts SET contactNumber = @contactNumber, contactName = @contactName WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@contactNumber", contact.contactNumber);
                    command.Parameters.AddWithValue("@contactName", contact.contactName);
                    command.Parameters.AddWithValue("@id", contact.id);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION ModifyContact] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION ModifyContact] " + ex.StackTrace);
                }
            }
        }

        public static void DeleteContact(int contactId) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "DELETE FROM contacts WHERE id = @id LIMIT 1";
                    command.Parameters.AddWithValue("@id", contactId);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeleteContact] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION DeleteContact] " + ex.StackTrace);
                }
            }
        }

        public static void AddCallLog(int phone, int target, int time) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO calls (phone, target, time, date) VALUES (@phone, @target, @time, NOW())";
                    command.Parameters.AddWithValue("@phone", phone);
                    command.Parameters.AddWithValue("@target", target);
                    command.Parameters.AddWithValue("@time", time);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddCallLog] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddCallLog] " + ex.StackTrace);
                }
            }
        }

        public static void AddSMSLog(int phone, int target, string message) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO sms (phone, target, message, date) VALUES (@phone, @target, @message, NOW())";
                    command.Parameters.AddWithValue("@phone", phone);
                    command.Parameters.AddWithValue("@target", target);
                    command.Parameters.AddWithValue("@message", message);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddSMSLog] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddSMSLog] " + ex.StackTrace);
                }
            }
        }

        public static List<TestModel> GetRandomQuestions(int license) {
            var testList = new List<TestModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT DISTINCT(id) AS id, question FROM questions WHERE license = @license ORDER BY RAND() LIMIT @questions";
                command.Parameters.AddWithValue("@license", license);
                command.Parameters.AddWithValue("@questions", Constants.MAX_LICENSE_QUESTIONS);

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var test = new TestModel();
                        {
                            test.id = reader.GetInt32("id");
                            test.text = reader.GetString("question");
                        }

                        testList.Add(test);
                    }
                }
            }

            return testList;
        }

        public static List<TestModel> GetQuestionAnswers(List<int> questionIds) {
            var testList = new List<TestModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT `id`, `question`, `answer` FROM answers WHERE FIND_IN_SET(`question`, @question) != 0";
                command.Parameters.AddWithValue("@question", string.Join(",", questionIds));

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var test = new TestModel();
                        {
                            test.id = reader.GetInt32("id");
                            test.question = reader.GetInt32("question");
                            test.text = reader.GetString("answer");
                        }

                        testList.Add(test);
                    }
                }
            }

            return testList;
        }

        public static List<TestModel> GetQuestionAnswers(int question) {
            var testList = new List<TestModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT id, answer FROM answers WHERE question = @question";
                command.Parameters.AddWithValue("@question", question);

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var test = new TestModel();
                        {
                            test.id = reader.GetInt32("id");
                            test.text = reader.GetString("answer");
                            test.question = question;
                        }

                        testList.Add(test);
                    }
                }
            }

            return testList;
        }

        public static int CheckCorrectAnswers(Dictionary<int, int> application) {
            var mistakes = 0;

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();

                command.CommandText =
                    "SELECT `id`, `question` FROM `answers` WHERE FIND_IN_SET(`question`, @questions) != 0 AND `correct` = 1";
                command.Parameters.AddWithValue("@questions", string.Join(",", new List<int>(application.Keys)));

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var answerId = reader.GetInt32("id");
                        var questionId = reader.GetInt32("question");

                        if (application[questionId] != answerId) mistakes++;
                    }
                }
            }

            return mistakes;
        }

        public static bool CheckAnswerCorrect(int answer) {
            var correct = false;

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT correct FROM answers WHERE id = @id LIMIT 1";
                command.Parameters.AddWithValue("@id", answer);

                using (var reader = command.ExecuteReader()) {
                    if (reader.HasRows) {
                        reader.Read();
                        correct = reader.GetBoolean("correct");
                    }
                }
            }

            return correct;
        }

        public static void AddAdminLog(string admin, string player, string action, int time, string reason) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText =
                        "INSERT INTO `admin` (`source`, `target`, `action`, `time`, `reason`, `date`) VALUES (@source, @target, @action, @time, @reason, CURRENT_TIMESTAMP)";
                    command.Parameters.AddWithValue("@source", admin);
                    command.Parameters.AddWithValue("@target", player);
                    command.Parameters.AddWithValue("@action", action);
                    command.Parameters.AddWithValue("@time", time);
                    command.Parameters.AddWithValue("@reason", reason);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddAdminLog] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddAdminLog] " + ex.StackTrace);
                }
            }
        }

        public static void AddLicensedWeapon(int itemId, string buyer) {
            using (var connection = new MySqlConnection(connectionString)) {
                try {
                    connection.Open();
                    var command = connection.CreateCommand();

                    command.CommandText = "INSERT INTO licensed (item, buyer, date) VALUES (@item, @buyer, NOW())";
                    command.Parameters.AddWithValue("@item", itemId);
                    command.Parameters.AddWithValue("@buyer", buyer);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddLicensedWeapon] " + ex.Message);
                    NAPI.Util.ConsoleOutput("[EXCEPTION AddLicensedWeapon] " + ex.StackTrace);
                }
            }
        }

        public static List<PermissionModel> LoadAllPermissions() {
            var permissionList = new List<PermissionModel>();

            using (var connection = new MySqlConnection(connectionString)) {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM permissions";

                using (var reader = command.ExecuteReader()) {
                    while (reader.Read()) {
                        var permission = new PermissionModel();
                        {
                            permission.playerId = reader.GetInt32("playerId");
                            permission.command = reader.GetString("command");
                            permission.option = reader.GetString("option");
                        }

                        permissionList.Add(permission);
                    }
                }
            }

            return permissionList;
        }
    }
}