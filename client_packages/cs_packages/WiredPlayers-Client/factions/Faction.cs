using RAGE;
using RAGE.Elements;

namespace WiredPlayers_Client.factions
{
    class Faction : Events.Script
    {
        private Blip factionWarningBlip = null;

        public Faction()
        {
            Events.Add("showFactionWarning", ShowFactionWarningEvent);
            Events.Add("deleteFactionWarning", DeleteFactionWarningEvent);
        }

        private void ShowFactionWarningEvent(object[] args)
        {
            // Get the variables from the arguments
            Vector3 position = (Vector3)args[0];

            // Create the blip on the map
            factionWarningBlip = new Blip(1, position, string.Empty, 1, 1);
        }

        private void DeleteFactionWarningEvent(object[] args)
        {
            // Destroy the blip on the map
            factionWarningBlip.Destroy();
            factionWarningBlip = null;
        }
    }
}
