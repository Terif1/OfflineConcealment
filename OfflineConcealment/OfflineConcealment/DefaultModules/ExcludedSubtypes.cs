using System.Collections.Generic;
using Sandbox.Game.Entities;
using VRageMath;

namespace OfflineConcealment.DefaultModules
{
    public class ExcludedSubtypes : ConcealmentModule
    {
        //you can define any value here
        public List<string> ExcludedSubtypeList = new List<string>(new []
        {
            "AdminBlock",
            "SuperRefinery"
        });
        //Defines values related to the module. ALL MUST HAVE SOME VALUE
        public ExcludedSubtypes()
        {
            Name = "Excluded Subtypes";
            Enabled = false;
            AreaLocked = false;
            Areas = new List<BoundingSphereD>();
            returnOnCheck = "Your grid will not conceal due to having a block with a subtype set to keep it unconcealed";
        }
        
        //Checks for each grid. true = continue and conceal, false = exclude
        public override bool GridCheck(MyCubeGrid grid)
        {
            return true;
        }

        //Checks for each block. true = continue and conceal, false = exclude
        public override bool BlockCheck(MyCubeBlock block)
        {
            if (block.DefinitionId != null && ExcludedSubtypeList.Contains(block.DefinitionId.Value.SubtypeId.String))
            {
                return false;
            }
            return true;
        }
    }
}