using GTANetworkAPI;
using XenRP.globals;

namespace XenRP.admin {
    public class Testing : Script {
        [Command("object")]
        public void ObjectCommand(Client player, string obj) {
            int modelId;
            if (!int.TryParse(obj, out modelId)) {
                modelId = (int) NAPI.Util.PickupNameToModel(obj);
                if (modelId == 0) {
                    player.SendChatMessage(Constants.COLOR_ERROR +
                                           "Invalid model given. must be an int or a pickup name.");
                    return;
                }
            }

            var test = NAPI.Object.CreateObject(int.Parse(obj), player.Position + new Vector3(0, 0, -1),
                player.Rotation);
            test.FreezePosition = false;
            player.SendChatMessage(Constants.COLOR_HELP + "Object created.");
        }

        [Command("reload")]
        public void Reload(Client player) {
            NAPI.Resource.StopResource("XenRP");
            NAPI.Resource.StartResource("XenRP");
        }
    }
}