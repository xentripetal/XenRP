using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GTANetworkAPI;
using WiredPlayers.character;
using WiredPlayers.database;
using WiredPlayers.globals;
using WiredPlayers.messages.error;
using WiredPlayers.messages.information;
using WiredPlayers.model;

namespace WiredPlayers.business {
    public class Business : Script {
        public static List<BusinessModel> businessList;

        public static void LoadDatabaseBusiness() {
            businessList = Database.LoadAllBusiness();
            foreach (var businessModel in businessList) {
                // We create the entrance TextLabel for each business
                businessModel.businessLabel = NAPI.TextLabel.CreateTextLabel(businessModel.name, businessModel.position,
                    30.0f, 0.75f, 4, new Color(255, 255, 255), false, businessModel.dimension);

                // We mark the blip in the map
                foreach (var blipModel in Constants.BUSINESS_BLIP_LIST)
                    if (blipModel.id == businessModel.id) {
                        var businessBlip = NAPI.Blip.CreateBlip(businessModel.position);
                        businessBlip.Name = businessModel.name;
                        businessBlip.Sprite = (uint) blipModel.blip;
                        businessBlip.ShortRange = true;
                        break;
                    }
            }
        }

        public static BusinessModel GetBusinessById(int businessId) {
            // Get the business given an specific identifier
            return businessList.Where(business => business.id == businessId).FirstOrDefault();
        }

        public static BusinessModel GetClosestBusiness(Client player, float distance = 2.0f) {
            BusinessModel business = null;
            foreach (var businessModel in businessList)
                if (player.Position.DistanceTo(businessModel.position) < distance) {
                    business = businessModel;
                    distance = player.Position.DistanceTo(business.position);
                }

            return business;
        }

        public static List<BusinessItemModel> GetBusinessSoldItems(int business) {
            // Get the items sold in a business
            return Constants.BUSINESS_ITEM_LIST.Where(businessItem => businessItem.business == business).ToList();
        }

        public static BusinessItemModel GetBusinessItemFromName(string itemName) {
            // Get the item from its name
            return Constants.BUSINESS_ITEM_LIST.Where(businessItem => businessItem.description == itemName)
                .FirstOrDefault();
        }

        public static BusinessItemModel GetBusinessItemFromHash(string itemHash) {
            // Get the item from its hash
            return Constants.BUSINESS_ITEM_LIST.Where(businessItem => businessItem.hash == itemHash).FirstOrDefault();
        }

        public static List<BusinessClothesModel> GetBusinessClothesFromSlotType(int sex, int type, int slot) {
            // Get the clothes for a sex from their slot and type
            return Constants.BUSINESS_CLOTHES_LIST.Where(clothes =>
                clothes.type == type && (clothes.sex == sex || Constants.SEX_NONE == clothes.sex) &&
                clothes.bodyPart == slot).ToList();
        }

        public static int GetClothesProductsPrice(int id, int sex, int type, int slot) {
            // Get the products needed for the given clothes
            var clothesModel = Constants.BUSINESS_CLOTHES_LIST.Where(c =>
                c.type == type && (c.sex == sex || Constants.SEX_NONE == c.sex) && c.bodyPart == slot &&
                c.clothesId == id).FirstOrDefault();

            return clothesModel == null ? 0 : clothesModel.products;
        }

        public static string GetBusinessTypeIpl(int type) {
            // Get the IPL given the business type
            var businessIpl = Constants.BUSINESS_IPL_LIST.Where(ipl => ipl.type == type).FirstOrDefault();

            return businessIpl == null ? string.Empty : businessIpl.ipl;
        }

        public static Vector3 GetBusinessExitPoint(string ipl) {
            // Get the exit point from the given IPL
            return Constants.BUSINESS_IPL_LIST.Where(iplModel => iplModel.ipl == ipl).FirstOrDefault()?.position;
        }

        public static bool HasPlayerBusinessKeys(Client player, BusinessModel business) {
            return player.Name == business.owner;
        }

        private List<BusinessTattooModel> GetBusinessZoneTattoos(int sex, int zone) {
            // Get the tattoos matching a body part
            return Constants.TATTOO_LIST.Where(tattoo =>
                tattoo.slot == zone && (tattoo.maleHash.Length > 0 && sex == Constants.SEX_MALE ||
                                        tattoo.femaleHash.Length > 0 && sex == Constants.SEX_FEMALE)).ToList();
        }

        [RemoteEvent("businessPurchaseMade")]
        public void BusinessPurchaseMadeEvent(Client player, string itemName, int amount) {
            int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
            var business = GetBusinessById(businessId);
            var businessItem = GetBusinessItemFromName(itemName);

            if (business.type == Constants.BUSINESS_TYPE_AMMUNATION &&
                businessItem.type == Constants.ITEM_TYPE_WEAPON &&
                player.GetData(EntityData.PLAYER_WEAPON_LICENSE) < Globals.GetTotalSeconds()) {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.weapon_license_expired);
            }
            else {
                var hash = 0;
                var price = (int) Math.Round(businessItem.products * business.multiplier) * amount;
                int money = player.GetSharedData(EntityData.PLAYER_MONEY);

                if (money < price) {
                    player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
                }
                else {
                    var purchaseMessage = string.Format(InfoRes.business_item_purchased, price);
                    int playerId = player.GetData(EntityData.PLAYER_SQL_ID);

                    Task.Factory.StartNew(() => {
                        // We look for the item in the inventory
                        var itemModel = Globals.GetPlayerItemModelFromHash(playerId, businessItem.hash);
                        if (itemModel == null) {
                            // We create the purchased item
                            itemModel = new ItemModel();
                            {
                                itemModel.hash = businessItem.hash;
                                itemModel.ownerIdentifier = player.GetData(EntityData.PLAYER_SQL_ID);
                                itemModel.amount = businessItem.uses * amount;
                                itemModel.position = new Vector3(0.0f, 0.0f, 0.0f);
                                itemModel.dimension = 0;
                            }

                            if (businessItem.type == Constants.ITEM_TYPE_WEAPON)
                                itemModel.ownerEntity = Constants.ITEM_ENTITY_WHEEL;
                            else
                                itemModel.ownerEntity = int.TryParse(itemModel.hash, out hash)
                                    ? Constants.ITEM_ENTITY_RIGHT_HAND
                                    : Constants.ITEM_ENTITY_PLAYER;

                            // Adding the item to the list and database
                            itemModel.id = Database.AddNewItem(itemModel);
                            Globals.itemList.Add(itemModel);
                        }
                        else {
                            itemModel.amount += businessItem.uses * amount;

                            if (int.TryParse(itemModel.hash, out hash))
                                itemModel.ownerEntity = Constants.ITEM_ENTITY_RIGHT_HAND;

                            // Update the item's amount
                            Database.UpdateItem(itemModel);
                        }

                        // If the item has a valid hash, we give it in hand
                        if (itemModel.ownerEntity == Constants.ITEM_ENTITY_RIGHT_HAND) {
                            // Remove the previous item if there was any
                            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null)
                                Globals.RemoveItemOnHands(player);

                            // Give the new item to the player
                            Globals.AttachItemToPlayer(player, itemModel.id, itemModel.hash, "IK_R_Hand",
                                businessItem.position, businessItem.rotation, EntityData.PLAYER_RIGHT_HAND);
                        }
                        else if (businessItem.type == Constants.ITEM_TYPE_WEAPON) {
                            // Remove the previous item if there was any
                            if (player.GetSharedData(EntityData.PLAYER_RIGHT_HAND) != null)
                                Globals.RemoveItemOnHands(player);

                            // We give the weapon to the player
                            player.GiveWeapon(NAPI.Util.WeaponNameToModel(itemModel.hash), itemModel.amount);

                            // Add the attachment to the player
                            var attachment = new AttachmentModel(itemModel.id, "IK_R_Hand", itemModel.hash,
                                businessItem.position, businessItem.rotation);
                            player.SetSharedData(EntityData.PLAYER_RIGHT_HAND, NAPI.Util.ToJson(attachment));

                            // Checking if it's been bought in the Ammu-Nation
                            if (business.type == Constants.BUSINESS_TYPE_AMMUNATION)
                                Database.AddLicensedWeapon(itemModel.id, player.Name);
                        }

                        // If it's a phone, we create a new number
                        if (itemModel.hash == Constants.ITEM_HASH_TELEPHONE)
                            if (player.GetData(EntityData.PLAYER_PHONE) == 0) {
                                var random = new Random();
                                var phone = random.Next(100000, 999999);
                                player.SetData(EntityData.PLAYER_PHONE, phone);

                                // Sending the message with the new number to the player
                                var message = string.Format(InfoRes.player_phone, phone);
                                player.SendChatMessage(Constants.COLOR_INFO + message);
                            }

                        // We substract the product and add funds to the business
                        if (business.owner != string.Empty) {
                            business.funds += price;
                            business.products -= businessItem.products;

                            // Update the business
                            Database.UpdateBusiness(business);
                        }

                        player.SetSharedData(EntityData.PLAYER_MONEY, money - price);
                        player.SendChatMessage(Constants.COLOR_INFO + purchaseMessage);
                    });
                }
            }
        }

        [RemoteEvent("getClothesByType")]
        public void GetClothesByTypeEvent(Client player, int type, int slot) {
            int sex = player.GetData(EntityData.PLAYER_SEX);
            var clothesList = GetBusinessClothesFromSlotType(sex, type, slot);

            if (clothesList.Count > 0)
                player.TriggerEvent("showTypeClothes", NAPI.Util.ToJson(clothesList));
            else
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.business_clothes_not_available);
        }

        [RemoteEvent("dressEquipedClothes")]
        public void DressEquipedClothesEvent(Client player, int type, int slot) {
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
            var clothes = Globals.GetDressedClothesInSlot(playerId, type, slot);

            if (type == 0)
                player.SetClothes(slot, clothes == null ? 0 : clothes.drawable, clothes == null ? 0 : clothes.texture);
            else
                player.SetAccessories(slot, clothes == null ? 255 : clothes.drawable,
                    clothes == null ? 255 : clothes.texture);
        }

        [RemoteEvent("clothesItemSelected")]
        public void ClothesItemSelectedEvent(Client player, string clothesJson) {
            var clothesModel = NAPI.Util.FromJson<BusinessClothesModel>(clothesJson);

            // Get the player's clothes
            int playerId = player.GetData(EntityData.PLAYER_SQL_ID);
            var ownedClothes = Globals.GetPlayerClothes(playerId);

            if (ownedClothes.Any(c =>
                c.slot == clothesModel.bodyPart && c.type == clothesModel.type &&
                c.drawable == clothesModel.clothesId && c.texture == clothesModel.texture)) {
                // The player already has those clothes
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_owns_clothes);
                return;
            }

            // Store the data from the purchase
            var clothes = new ClothesModel();
            {
                clothes.type = clothesModel.type;
                clothes.slot = clothesModel.bodyPart;
                clothes.drawable = clothesModel.clothesId;
                clothes.texture = clothesModel.texture;
                clothes.player = playerId;
                clothes.dressed = true;
            }
            ;

            int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
            int sex = player.GetData(EntityData.PLAYER_SEX);
            var products =
                GetClothesProductsPrice(clothesModel.clothesId, sex, clothesModel.type, clothesModel.bodyPart);
            var business = GetBusinessById(businessId);
            var price = (int) Math.Round(products * business.multiplier);

            // We check whether the player has enough money
            int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);

            if (playerMoney >= price) {
                // Substracting paid money
                player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney - price);

                // We substract the product and add funds to the business
                if (business.owner != string.Empty) {
                    business.funds += price;
                    business.products -= products;

                    Task.Factory.StartNew(() => {
                        // Update the business
                        Database.UpdateBusiness(business);
                    });
                }

                // Undress previous clothes
                Globals.UndressClothes(playerId, clothesModel.type, clothesModel.bodyPart);

                Task.Factory.StartNew(() => {
                    // Storing the clothes into database
                    clothes.id = Database.AddClothes(clothes);
                    Globals.clothesList.Add(clothes);

                    // Confirmation message sent to the player
                    var purchaseMessage = string.Format(InfoRes.business_item_purchased, price);
                    player.SendChatMessage(Constants.COLOR_INFO + purchaseMessage);
                });
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
            }
        }

        [RemoteEvent("changeHairStyle")]
        public void ChangeHairStyleEvent(Client player, string skinJson) {
            int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);
            int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
            var business = GetBusinessById(businessId);
            var price = (int) Math.Round(business.multiplier * Constants.PRICE_BARBER_SHOP);

            if (playerMoney >= price) {
                int playerId = player.GetData(EntityData.PLAYER_SQL_ID);

                // Getting the new hairstyle from the JSON
                var skinModel = NAPI.Util.FromJson<SkinModel>(skinJson);

                // Saving new entity data
                player.SetData(EntityData.PLAYER_SKIN_MODEL, skinModel);

                // Substract money to the player
                player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney - price);

                Task.Factory.StartNew(() => {
                    // We update values in the database
                    Database.UpdateCharacterHair(playerId, skinModel);

                    // We substract the product and add funds to the business
                    if (business.owner != string.Empty) {
                        business.funds += price;
                        business.products -= Constants.PRICE_BARBER_SHOP;
                        Database.UpdateBusiness(business);
                    }

                    // Delete the browser
                    player.TriggerEvent("destroyBrowser");

                    // Confirmation message sent to the player
                    var playerMessage = string.Format(InfoRes.haircut_purchased, price);
                    player.SendChatMessage(Constants.COLOR_INFO + playerMessage);
                });
            }
            else {
                // The player has not the required money
                var message = string.Format(ErrRes.haircut_money, price);
                player.SendChatMessage(Constants.COLOR_ERROR + message);
            }
        }

        [RemoteEvent("loadZoneTattoos")]
        public void LoadZoneTattoosEvent(Client player, int zone) {
            int sex = player.GetData(EntityData.PLAYER_SEX);
            var tattooList = GetBusinessZoneTattoos(sex, zone);

            // We update the menu with the tattoos
            player.TriggerEvent("showZoneTattoos", NAPI.Util.ToJson(tattooList));
        }

        [RemoteEvent("purchaseTattoo")]
        public void PurchaseTattooEvent(Client player, int tattooZone, int tattooIndex) {
            int sex = player.GetData(EntityData.PLAYER_SEX);
            int businessId = player.GetData(EntityData.PLAYER_BUSINESS_ENTERED);
            var business = GetBusinessById(businessId);

            // Getting the tattoo and its price
            var businessTattoo = GetBusinessZoneTattoos(sex, tattooZone).ElementAt(tattooIndex);
            int playerMoney = player.GetSharedData(EntityData.PLAYER_MONEY);
            var price = (int) Math.Round(business.multiplier * businessTattoo.price);

            if (price <= playerMoney) {
                var tattoo = new TattooModel();
                {
                    tattoo.player = player.GetData(EntityData.PLAYER_SQL_ID);
                    tattoo.slot = tattooZone;
                    tattoo.library = businessTattoo.library;
                    tattoo.hash = sex == Constants.SEX_MALE ? businessTattoo.maleHash : businessTattoo.femaleHash;
                }

                Task.Factory.StartNew(() => {
                    if (Database.AddTattoo(tattoo)) {
                        // We add the tattoo to the list
                        Globals.tattooList.Add(tattoo);

                        // Substract player money
                        player.SetSharedData(EntityData.PLAYER_MONEY, playerMoney - price);

                        // We substract the product and add funds to the business
                        if (business.owner != string.Empty) {
                            business.funds += price;
                            business.products -= businessTattoo.price;
                            Database.UpdateBusiness(business);
                        }

                        // Confirmation message sent to the player
                        var playerMessage = string.Format(InfoRes.tattoo_purchased, price);
                        player.SendChatMessage(Constants.COLOR_INFO + playerMessage);

                        // Reload client tattoo list
                        player.TriggerEvent("addPurchasedTattoo", NAPI.Util.ToJson(tattoo));
                    }
                    else {
                        // Player already had that tattoo
                        player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.tattoo_duplicated);
                    }
                });
            }
            else {
                player.SendChatMessage(Constants.COLOR_ERROR + ErrRes.player_not_enough_money);
            }
        }

        [RemoteEvent("loadCharacterClothes")]
        public void LoadCharacterClothesEvent(Client player) {
            // Generate player's clothes
            Customization.ApplyPlayerClothes(player);
        }
    }
}