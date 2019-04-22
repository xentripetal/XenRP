using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;
using XenRP.Client.model;

namespace XenRP.Client.house {
    internal class House : Events.Script {
        private List<ClothesModel> clothes;

        public House() {
            Events.Add("showPlayerWardrobe", ShowPlayerWardrobeEvent);
            Events.Add("getPlayerPurchasedClothes", GetPlayerPurchasedClothesEvent);
            Events.Add("showPlayerClothes", ShowPlayerClothesEvent);
            Events.Add("previewPlayerClothes", PreviewPlayerClothesEvent);
            Events.Add("changePlayerClothes", ChangePlayerClothesEvent);
        }

        private void ShowPlayerWardrobeEvent(object[] args) {
            // Show wardrobe's menu
            Browser.CreateBrowserEvent(new object[] {
                "package://WiredPlayers/statics/html/sideMenu.html", "populateWardrobeMenu",
                JsonConvert.SerializeObject(Constants.CLOTHES_TYPES)
            });
        }

        private void GetPlayerPurchasedClothesEvent(object[] args) {
            // Get the variables from the array
            var index = Convert.ToInt32(args[0]);

            // Get the player's clothes
            Events.CallRemote("getPlayerPurchasedClothes", Constants.CLOTHES_TYPES[index].type,
                Constants.CLOTHES_TYPES[index].slot);
        }

        private void ShowPlayerClothesEvent(object[] args) {
            // Get the variables from the array
            var clothesNames = JsonConvert.DeserializeObject<List<string>>(args[1].ToString());
            clothes = JsonConvert.DeserializeObject<List<ClothesModel>>(args[0].ToString());

            for (var i = 0; i < clothesNames.Count; i++)
                // Add the name for each clothes
                clothes[i].description = clothesNames[i];

            // Show clothes of the selected type
            Browser.ExecuteFunctionEvent(new object[] {"populateWardrobeClothes", args[0].ToString()});
        }

        private void PreviewPlayerClothesEvent(object[] args) {
            // Get the variables from the array
            var index = Convert.ToInt32(args[0]);

            if (clothes[index].type == 0)
                Player.LocalPlayer.SetComponentVariation(clothes[index].slot, clothes[index].drawable,
                    clothes[index].texture, 0);
            else
                Player.LocalPlayer.SetPropIndex(clothes[index].slot, clothes[index].drawable, clothes[index].texture,
                    true);
        }

        private void ChangePlayerClothesEvent(object[] args) {
            // Get the variables from the array
            var index = Convert.ToInt32(args[0]);

            // Equip the clothes
            Events.CallRemote("wardrobeClothesItemSelected", clothes[index].clothesId);
        }
    }
}