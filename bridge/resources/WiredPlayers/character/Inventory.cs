using GTANetworkAPI;
using WiredPlayers.model;
using WiredPlayers.globals;
using WiredPlayers.database;
using System.Collections.Generic;
using System.Linq;

namespace WiredPlayers.character
{
    public class Inventory : Script
    {
        public static void LoadDatabaseItems()
        {
            // Create the item list
            Globals.itemList = Database.LoadAllItems();

            // Get the objects on the ground
            List<ItemModel> groundItems = Globals.itemList.Where(it => it.ownerEntity == Constants.ITEM_ENTITY_GROUND).ToList();

            foreach (ItemModel item in groundItems)
            {
                // Create each of the items on the ground
                item.objectHandle = NAPI.Object.CreateObject(int.Parse(item.hash), item.position, new Vector3(), 255, item.dimension);
            }
        }
    }
}
