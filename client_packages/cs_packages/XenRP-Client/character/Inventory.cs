using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RAGE;
using XenRP.Client.globals;

namespace XenRP.Client.character {
    internal class Inventory : Events.Script {
        private static int targetType;

        public Inventory() {
            Events.Add("showPlayerInventory", ShowPlayerInventoryEvent);
            Events.Add("getInventoryOptions", GetInventoryOptionsEvent);
            Events.Add("executeAction", ExecuteActionEvent);
            Events.Add("updateInventory", UpdateInventoryEvent);
            Events.Add("closeInventory", CloseInventoryEvent);
        }

        private void ShowPlayerInventoryEvent(object[] args) {
            // Store all the inventory data
            targetType = Convert.ToInt32(args[1]);

            // Show player's inventory
            Browser.CreateBrowserEvent(new object[] {
                "package://statics/html/inventory.html", "populateInventory", args[0].ToString(), "general.inventory"
            });
        }

        private void GetInventoryOptionsEvent(object[] args) {
            // Get the variables from the arguments
            var itemType = Convert.ToInt32(args[0]);
            var itemHash = args[1].ToString();

            var optionsList = new List<string>();
            var dropable = false;

            switch (targetType) {
                case 0:
                    // Player's inventory
                    if (itemType == 0)
                        optionsList.Add("general.consume");
                    else if (itemType == 2) optionsList.Add("general.open");

                    if (itemHash.All(char.IsDigit)) optionsList.Add("general.equip");

                    dropable = true;
                    break;
                case 1:
                    // Player frisk
                    optionsList.Add("general.confiscate");
                    break;
                case 2:
                    // Inventory store into the trunk
                    optionsList.Add("general.store");
                    break;
                case 3:
                    // Vehicle trunk
                    optionsList.Add("general.withdraw");
                    break;
            }

            // Show the options into the inventory
            Browser.ExecuteFunctionEvent(new object[]
                {"showInventoryOptions", JsonConvert.SerializeObject(optionsList), dropable});
        }

        private void ExecuteActionEvent(object[] args) {
            // Get the variables from the arguments
            var item = Convert.ToInt32(args[0]);
            var option = args[1].ToString();

            // Execute the selected action
            Events.CallRemote("processMenuAction", item, option);
        }

        private void UpdateInventoryEvent(object[] args) {
            // Update the items in the inventory
            Browser.ExecuteFunctionEvent(new object[] {"updateInventory", args[0].ToString()});
        }

        private void CloseInventoryEvent(object[] args) {
            // Remove the browser
            Browser.DestroyBrowserEvent(null);

            // Clear the variables related
            Events.CallRemote("closeInventory");
        }
    }
}