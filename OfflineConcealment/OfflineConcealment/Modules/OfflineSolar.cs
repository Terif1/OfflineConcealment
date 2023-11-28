using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.Entities.Blocks;
using VRage;
using VRageMath;

namespace OfflineConcealment.DefaultModules
{
    public class OfflineSolar : ConcealmentModule
    {
        public Dictionary<long, MyTuple<bool, List<MyBatteryBlock>>> gridData = new Dictionary<long, MyTuple<bool, List<MyBatteryBlock>>>();

        //Defines values related to the module. ALL MUST HAVE SOME VALUE
        public OfflineSolar()
        {
            Name = "OfflineRefining";
            Enabled = true;
            AreaLocked = false;
            Areas = new List<BoundingSphereD>();
        }

        //Checks for each grid. true = continue and conceal, false = exclude
        public override bool GridCheck(MyCubeGrid grid)
        {
            if (!gridData.Keys.Contains(grid.EntityId)) gridData.Add(grid.EntityId, new MyTuple<bool, List<MyBatteryBlock>>(false, new List<MyBatteryBlock>()));
            return true;
        }

        //Checks for each block. true = continue and conceal, false = exclude
        public override bool BlockCheck(MyCubeBlock block)
        {
            var returnvar = true;
            if (block is MySolarPanel s && s.Enabled && s.CurrentOutput > 0.1F)
            {
                var data = gridData[s.Parent.EntityId];
                data.Item1 = true;
                if (data.Item2.Any())
                    foreach (var battery in data.Item2)
                        if (battery.IsCharging && battery.StoredPowerRatio < 0.25F)
                        {
                            returnvar = false;
                        }

                data.Item2.Clear();
            }

            if (block is MyBatteryBlock b && b.Enabled &&
                (b.ChargeMode == ChargeMode.Auto || b.ChargeMode == ChargeMode.Recharge))
            {
                var data = gridData[b.Parent.EntityId];
                if (data.Item1)
                {
                    if (b.IsCharging && b.StoredPowerRatio < 0.25F) returnvar = false;
                }
                else
                {
                    try
                    {
                        data.Item2.Add(b);
                    }
                    catch (AggregateException e)
                    {
                        OfflineConcealment.Log.Error(e);
                        data.Item2.Add(b);
                    }
                }
            }

            return returnvar;
        }
    }
}