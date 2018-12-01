using RAGE;
using RAGE.Elements;

namespace WiredPlayers_Client.jobs
{
    class Garbage : Events.Script
    {
        private Blip garbageBlip = null;

        public Garbage()
        {
            Events.Add("showGarbageCheckPoint", ShowGarbageCheckPointEvent);
            Events.Add("deleteGarbageCheckPoint", DeleteGarbageCheckPointEvent);
        }

        private void ShowGarbageCheckPointEvent(object[] args)
        {
            // Get the variables from the array
            Vector3 position = (Vector3)args[0];

            // Create a blip on the map
            garbageBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteGarbageCheckPointEvent(object[] args)
        {
            // Destroy the blip on the map
            garbageBlip.Destroy();
            garbageBlip = null;
        }
    }
}
