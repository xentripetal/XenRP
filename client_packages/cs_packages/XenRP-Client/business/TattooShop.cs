using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RAGE;
using RAGE.Game;
using XenRP.Client.globals;
using XenRP.Client.model;
using Player = RAGE.Elements.Player;

namespace XenRP.Client.business {
    internal class TattooShop : Events.Script {
        private int playerSex;
        private List<Tattoo> playerTattoos;
        private List<Tattoo> tattooList;
        private List<Tattoo> zoneTattoos;

        public TattooShop() {
            Events.Add("showTattooMenu", ShowTattooMenuEvent);
            Events.Add("getZoneTattoos", GetZoneTattoosEvent);
            Events.Add("addPlayerTattoo", AddPlayerTattooEvent);
            Events.Add("clearTattoos", ClearTattoosEvent);
            Events.Add("purchaseTattoo", PurchaseTattooEvent);
            Events.Add("exitTattooShop", ExitTattooShopEvent);
        }

        private void ShowTattooMenuEvent(object[] args) {
            // Get the variables from the arguments
            var playerTattoosJson = args[1].ToString();
            var tattoosJson = args[2].ToString();
            var business = args[3].ToString();
            var price = (float) Convert.ToDouble(args[4]);
            playerSex = Convert.ToInt32(args[0]);


            // Initialize the player tattoos
            playerTattoos = JsonConvert.DeserializeObject<List<Tattoo>>(playerTattoosJson);
            tattooList = JsonConvert.DeserializeObject<List<Tattoo>>(tattoosJson);
            var tattooZoneJson = JsonConvert.SerializeObject(Constants.TATTOO_ZONES);

            // Show tattoos menu
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/sideMenu.html", "populateTattooMenu", tattooZoneJson, business, price});
        }

        private void GetZoneTattoosEvent(object[] args) {
            // Get the variables from the arguments
            var zone = Convert.ToInt32(args[0]);

            // Get the tattoos from the zone
            zoneTattoos = tattooList.Where(tattoo => tattoo.slot == zone).ToList();

            // Show the tattoos for the selected zone
            Browser.ExecuteFunctionEvent(new object[]
                {"populateZoneTattoos", Globals.EscapeJsonCharacters(JsonConvert.SerializeObject(zoneTattoos))});
        }

        private void AddPlayerTattooEvent(object[] args) {
            // Get the variables from the arguments
            var index = Convert.ToInt32(args[0]);

            // Load the player's tattoos
            ClearTattoosEvent(null);

            // Add the tattoo to the player
            var tattooHash = playerSex == Constants.SEX_MALE
                ? Misc.GetHashKey(zoneTattoos[index].maleHash)
                : Misc.GetHashKey(zoneTattoos[index].femaleHash);
            Player.LocalPlayer.SetDecoration(Misc.GetHashKey(zoneTattoos[index].library), tattooHash);
        }

        private void ClearTattoosEvent(object[] args) {
            // Clear all the tattoos
            Player.LocalPlayer.ClearDecorations();

            foreach (var tattoo in playerTattoos)
                // Add the tattoo to the player
                Player.LocalPlayer.SetDecoration(Misc.GetHashKey(tattoo.library), Misc.GetHashKey(tattoo.hash));
        }

        private void PurchaseTattooEvent(object[] args) {
            // Get the variables from the arguments
            var slot = Convert.ToInt32(args[0]);
            var index = Convert.ToInt32(args[1]);

            // Add the new tattoo to the list
            var tattoo = new Tattoo();
            {
                tattoo.slot = slot;
                tattoo.library = zoneTattoos[index].library;
                tattoo.hash = playerSex == Constants.SEX_MALE
                    ? zoneTattoos[index].maleHash
                    : zoneTattoos[index].femaleHash;
            }
            playerTattoos.Add(tattoo);

            // Purchase the tattoo
            Events.CallRemote("purchaseTattoo", slot, index);
        }

        private void ExitTattooShopEvent(object[] args) {
            // Close the purchase menu
            Browser.DestroyBrowserEvent(null);

            // Dress the character
            Events.CallRemote("loadCharacterClothes");
        }
    }
}