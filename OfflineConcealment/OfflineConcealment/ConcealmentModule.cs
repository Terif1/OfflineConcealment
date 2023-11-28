using System.Collections.Generic;
using Sandbox.Game.Entities;
using VRageMath;

namespace OfflineConcealment
{
    public abstract class ConcealmentModule
    {
        public string Name;

        public bool Enabled;

        public bool AreaLocked;

        public List<BoundingSphereD> Areas;

        public string returnOnCheck = "";

        public virtual bool GridCheck(MyCubeGrid grid)
        {
            return true;
        }

        public virtual bool BlockCheck(MyCubeBlock block)
        {
            return true;
        }
    }
}