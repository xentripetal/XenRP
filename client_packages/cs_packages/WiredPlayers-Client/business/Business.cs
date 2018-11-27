using RAGE;
using Newtonsoft.Json;
using System.Collections.Generic;
using WiredPlayers_Client.globals;
using WiredPlayers_Client.model;

namespace WiredPlayers_Client.business
{
    class Business : Events.Script
    {
        private string businessItems = string.Empty;
        private float businessPriceMultiplier = 0.0f;

        public Business()
        {
            Events.Add("showBusinessPurchaseMenu", ShowBusinessPurchaseMenuEvent);
            Events.Add("purchaseItem", PurchaseItemEvent);
        }
        
        private void ShowBusinessPurchaseMenuEvent(object[] args)
        {
            // Store the products and price
            businessItems = args[0].ToString();
            string business = args[1].ToString();
            businessPriceMultiplier = (float)args[2];

            // Bank menu creation
            Browser.ExecuteFunctionEvent(new object[] { "package://statics/html/sideMenu.html", "populateBusinessItems", businessItems, business, businessPriceMultiplier });
        }

        private void PurchaseItemEvent(object[] args)
        {
            // Store the products and price
            int index = (int)args[0];
            int amount = (int)args[1];

            // Get the purchased item and its cost
            BusinessItem purchasedItem = JsonConvert.DeserializeObject<List<BusinessItem>>(businessItems)[index];
            Events.CallRemote("businessPurchaseMade", purchasedItem.description, amount);
        }
    }
}