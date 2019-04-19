using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RAGE;
using XenRP.Client.globals;
using XenRP.Client.model;

namespace XenRP.Client.business {
    internal class Business : Events.Script {
        private string businessItems = string.Empty;
        private float businessPriceMultiplier;

        public Business() {
            Events.Add("showBusinessPurchaseMenu", ShowBusinessPurchaseMenuEvent);
            Events.Add("purchaseItem", PurchaseItemEvent);
        }

        private void ShowBusinessPurchaseMenuEvent(object[] args) {
            // Store the products and price
            businessItems = args[0].ToString();
            var business = args[1].ToString();
            businessPriceMultiplier = (float) Convert.ToDouble(args[2]);

            // Bank menu creation
            Browser.CreateBrowserEvent(new object[] {
                "package://statics/html/sideMenu.html", "populateBusinessItems", businessItems, business,
                businessPriceMultiplier
            });
        }

        private void PurchaseItemEvent(object[] args) {
            // Store the products and price
            var index = Convert.ToInt32(args[0]);
            var amount = Convert.ToInt32(args[1]);

            // Get the purchased item and its cost
            var purchasedItem = JsonConvert.DeserializeObject<List<BusinessItem>>(businessItems)[index];
            Events.CallRemote("businessPurchaseMade", purchasedItem.description, amount);
        }
    }
}