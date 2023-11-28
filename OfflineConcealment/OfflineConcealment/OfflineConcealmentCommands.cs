using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game.ModAPI;
using VRageMath;

namespace OfflineConcealment
{
    
    public class OfflineConcealmentCommands : CommandModule
    {

        public OfflineConcealment Plugin => (OfflineConcealment)Context.Plugin;

        [Command("doesconceal", "diagnoses if your grid would conceal lacking nearby players and if not why")]
        [Permission(MyPromoteLevel.None)]
        public void doesConceal(string gridname = "")
        {
            if (gridname == "")
            {
                List<MyCubeGrid> grids = Findlookatgrid(Context.Player);
                if (grids.Count == 1)
                {
                    var grid = grids[0];
                    if (grid.BigOwners.Contains(Context.Player.IdentityId))
                    {
                        var re = Plugin.DoesConceal(grid);
                        if (!re.Item1)
                        {
                            if (re.Item2 == "player")
                            {
                                Context.Respond("Your grid will conceal when no players are around");
                            }
                            else
                            {
                                Context.Respond(re.Item2);
                            }
                        }
                    }
                    else
                    {
                        Context.Respond("You do not own that grid, therefore you cannot run this command on that grid.");
                    }
                }
                else if (grids.Count > 1)
                {
                    Context.Respond("There are multiple grids at that position, please look only one grid or enter the grid name");
                }
                else if (grids.Count < 1)
                {
                    Context.Respond("No Grid Found, Please try again");
                }
            }
            else
            {
                foreach (var grid in MyEntities.GetEntities().OfType<MyCubeGrid>())
                {
                    if (grid.DisplayName == gridname)
                    {
                        if (grid.BigOwners.Contains(Context.Player.IdentityId))
                        {
                            var re = Plugin.DoesConceal(grid);
                            if (!re.Item1)
                            {
                                if (re.Item2 == "player")
                                {
                                    Context.Respond("Your grid will conceal when no players are around");
                                }
                                else
                                {
                                    Context.Respond(re.Item2);
                                }
                            }
                        }
                        else
                        {
                            Context.Respond("You do not own that grid, therefore you cannot run this command on that grid.");
                        }
                    }
                }
            }
        }
        private List<MyCubeGrid> Findlookatgrid(IMyPlayer plr)
        {
            var matrix = plr.Character.GetHeadMatrix(true);
            var ray = new RayD(matrix.Translation + matrix.Forward.Normalized(), matrix.Translation + (matrix.Forward.Normalized() * (500f)));
            var list = new List<MyCubeGrid>();
            foreach (var grid in MyEntities.GetEntities().OfType<MyCubeGrid>())
            {
                if (ray.Intersects(grid.GetPhysicalGroupAABB()).HasValue)
                {
                    list.Add(grid);
                }
            }

            return list;
        }
        [Command("doesconcealmod", "This is an admin level command to check if any grid is going to conceal if not near a player, but it doesn't require ownership"), Permission(MyPromoteLevel.Admin)]
        public void Doesconcealmod(string gridname = "")
        {
            if (gridname == "")
            {
                List<MyCubeGrid> grids = Findlookatgrid(Context.Player);
                if (grids.Count == 1)
                {
                    var grid = grids[0];
                    var re = Plugin.DoesConceal(grid);
                    if (!re.Item1)
                    {
                        if (re.Item2 == "player")
                        {
                            Context.Respond("Your grid will conceal when no players are around");
                        }
                        else
                        {
                            Context.Respond(re.Item2);
                        }
                    }
                }
                else if (grids.Count > 1)
                {
                    Context.Respond("There are multiple grids at that position, please look only one grid or enter the grid name");
                }
                else if (grids.Count < 1)
                {
                    Context.Respond("No Grid Found, Please try again");
                }
            }
            else
            {
                var found = false;
                foreach (var grid in MyEntities.GetEntities().OfType<MyCubeGrid>())
                {
                    if (grid.DisplayName == gridname)
                    {
                        var re = Plugin.DoesConceal(grid);
                        if (!re.Item1)
                        {
                            if (re.Item2 == "player")
                            {
                                Context.Respond("Your grid will conceal when no players are around");
                            }
                            else
                            {
                                Context.Respond(re.Item2);
                            }
                        }

                        found = true;
                    }
                }

                if (!found)
                {
                    Context.Respond("No such grid");
                }
            }
        }

        [Command("conceal on", "enables concealment")]
        [Permission(MyPromoteLevel.Admin)]
        public void on()
        {
            Plugin.Config.Enabled = true;
            Plugin.Start();
            Context.Respond("Concealment starting");
        }
        
        [Command("conceal off", "disables concealment")]
        [Permission(MyPromoteLevel.Admin)]
        public void off()
        {
            Plugin.Config.Enabled = false;
            Context.Respond("Concealment Stopped");
        }

        [Command("module on", "enables a module")]
        [Permission(MyPromoteLevel.Admin)]
        public void modOn(string Name)
        {
            var returned = false;
            foreach (var module in Plugin.Modules)
            {
                if (module.Name == Name)
                {
                    module.Enabled = true;
                    Context.Respond($"Enabled {module.Name}");
                    returned = true;
                }
            }

            if (!returned)
            {
                Context.Respond("No such module found");
            }
        }
        
        [Command("module off", "enables a module")]
        [Permission(MyPromoteLevel.Admin)]
        public void modOff(string Name)
        {
            var returned = false;
            foreach (var module in Plugin.Modules)
            {
                if (module.Name == Name)
                {
                    module.Enabled = false;
                    Context.Respond($"Disabled {module.Name}");
                    returned = true;
                }
            }

            if (!returned)
            {
                Context.Respond("No such module found");
            }
        }

        [Command("reveal all", "Brings all grids out of concealment")]
        [Permission(MyPromoteLevel.Admin)]
        public void revealall()
        {
            Plugin.RevealAll();
            Context.Respond("Done");
        }
        
        [Command("conceal all", "Brings all grids into concealment")]
        [Permission(MyPromoteLevel.Admin)]
        public void concealall()
        {
            Plugin.ConcealAll();
            Context.Respond("Done");
        }
        
        [Command("listconcealed", "List all grids currently concealed in the world")]
        [Permission(MyPromoteLevel.Admin)]
        public void List()
        {
            var str = "The following grids are concealed:\n \n";
            foreach (var group in Plugin.AllStatus)
            {
                if (group.Value.Concealed)
                {
                    var grid = MyEntities.GetEntityById(group.Key);
                        str += $"{grid.DisplayName}\n";
                        
                }

                str += "\n";
            }

            var box = new DialogMessage("Concealed Grids", "",
                "All grids currently not being processed due to concealment", str, "Close");
            ModCommunication.SendMessageTo(box, Context.Player.SteamUserId);
        }
        
        [Command("conceal reload", "reloads the config for concealment from the config files")]
        [Permission(MyPromoteLevel.Admin)]
        public void reload()
        {
            Plugin.RevealAll();
            Plugin.SetupConfig();
            Plugin.Modules.Clear();
            Plugin.LoadModules();
        }
        
    }
}
