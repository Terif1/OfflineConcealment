using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRageMath;

namespace OfflineConcealment.DefaultModules
{
    
    /// <summary>
    /// Intended for excluding your npc factions
    /// also useful for those who...convince you
    /// </summary>
    public class ExcludedFactions : ConcealmentModule
    {
        //you can define any value here
        public List<string> ExcludedFactionTagsList = new List<string>(new []
        {
            "SPRT",
            "SPDR"
        });
        //Defines values related to the module. ALL MUST HAVE SOME VALUE
        public ExcludedFactions()
        {
            Name = "ExcludedFactions";
            Enabled = false;
            AreaLocked = false;
            Areas = new List<BoundingSphereD>();
            returnOnCheck = "Your grid will not conceal due to being owned by an excluded faction";
        }
        
        //Checks for each grid. true = continue and conceal, false = exclude
        public override bool GridCheck(MyCubeGrid grid)
        {
            var faction = MySession.Static.Factions.GetPlayerFaction(grid.BigOwners[0]);
            if (faction != null && ExcludedFactionTagsList.Contains(faction.Tag))
            {
                return false;
            }
            return true;
        }

        //Checks for each block. true = continue and conceal, false = exclude
        public override bool BlockCheck(MyCubeBlock block)
        {
            return true;
        }
    }
}