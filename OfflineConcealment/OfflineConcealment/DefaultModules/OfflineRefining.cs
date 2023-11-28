using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using VRageMath;

namespace OfflineConcealment.DefaultModules
{
    public class OfflineRefining : ConcealmentModule
    {
        //Defines values related to the module. ALL MUST HAVE SOME VALUE
        public OfflineRefining()
        {
            Name = "OfflineRefining";
            Enabled = false;
            AreaLocked = false;
            Areas = new List<BoundingSphereD>();
            returnOnCheck = "Your grid will not conceal due to having an active refinery";
        }
        
        //Checks for each grid. true = continue and conceal, false = exclude
        public override bool GridCheck(MyCubeGrid grid)
        {
            return true;
        }

        //Checks for each block. true = continue and conceal, false = exclude
        public override bool BlockCheck(MyCubeBlock block)
        {
            if (block is MyProductionBlock producer && producer.IsProducing)
            {
                return false;
            }
            return true;
        }
    }
}