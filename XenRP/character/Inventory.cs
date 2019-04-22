using System.Linq;
using GTANetworkAPI;
using XenRP.database;
using XenRP.globals;

namespace XenRP.character {
    public class Inventory : Script {
        public static void LoadDatabaseItems() {
            // Create the item list
            Globals.itemList = Database.LoadAllItems();

            // Get the objects on the ground
            var groundItems = Globals.itemList.Where(it => it.ownerEntity == Constants.ITEM_ENTITY_GROUND).ToList();

            foreach (var item in groundItems) {
                // Get the hash from the object
                var weaponHash = NAPI.Util.WeaponNameToModel(item.hash);
                var hash = weaponHash == 0
                    ? int.Parse(item.hash)
                    : (int) NAPI.Util.GetHashKey(Constants.WEAPON_ITEM_MODELS[weaponHash]);

                // Create each of the items on the ground
                item.objectHandle = NAPI.Object.CreateObject(hash, item.position, new Vector3(), 255, item.dimension);
            }
        }
    }
}