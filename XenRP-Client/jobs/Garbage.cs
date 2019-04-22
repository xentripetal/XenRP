using RAGE;
using RAGE.Elements;

namespace XenRP.Client.jobs {
    internal class Garbage : Events.Script {
        private Blip garbageBlip;

        public Garbage() {
            Events.Add("showGarbageCheckPoint", ShowGarbageCheckPointEvent);
            Events.Add("deleteGarbageCheckPoint", DeleteGarbageCheckPointEvent);
        }

        private void ShowGarbageCheckPointEvent(object[] args) {
            // Get the variables from the array
            var position = (Vector3) args[0];

            // Create a blip on the map
            garbageBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteGarbageCheckPointEvent(object[] args) {
            // Destroy the blip on the map
            garbageBlip.Destroy();
            garbageBlip = null;
        }
    }
}