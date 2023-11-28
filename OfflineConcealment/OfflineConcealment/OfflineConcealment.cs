using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CSharp;
using NLog.Config;
using NLog.Targets;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;
using Torch.Utils;
using VRage.Game.Entity;
using VRageMath;
using Task = System.Threading.Tasks.Task;

namespace OfflineConcealment
{
    public class OfflineConcealment : TorchPluginBase, IWpfPlugin
    {

        public static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        public static readonly Logger MassLog = LogManager.GetLogger("ConcealmentLog");


        private static readonly string CONFIG_FILE_NAME = "OfflineConcealmentConfig.cfg";

        public string ModulePath = "";

        private OfflineConcealmentControl _control;
        public UserControl GetControl() => _control ?? (_control = new OfflineConcealmentControl(this));

        private Persistent<OfflineConcealmentConfig> _config;
        public OfflineConcealmentConfig Config => _config?.Data;
        
        public List<ConcealmentModule> Modules = new List<ConcealmentModule>();

        public HashSet<ConcealmentModule> ActiveModules = new HashSet<ConcealmentModule>();

        public ConcurrentQueue<Action> MainThreadInvoke = new ConcurrentQueue<Action>();

        public ConcurrentQueue<MyCubeGrid> ReturnToOffThread = new ConcurrentQueue<MyCubeGrid>();

        public IConcealmentLogic Logic;

        public int runcycles = 0;

        public int testingGrids = 0;

        public Thread offThread;

        /// <summary>
        /// represents entity ID and bool for concealed. true for concealed, false for not
        /// </summary>
        public Dictionary<long, ConcealmentData> AllStatus = new Dictionary<long, ConcealmentData>();

        public override void Init(ITorchBase torch)
        {
            for (int i = LogManager.Configuration.LoggingRules.Count - 1; i >= 0; i--) {
                

                if (LogManager.Configuration.LoggingRules[i].LoggerNamePattern == "ConcealmentLog")
                    LogManager.Configuration.LoggingRules.RemoveAt(i);
            }
                
            var target = new FileTarget {FileName = "Logs/Concealment-" + DateTime.Now.Day + "-" + DateTime.Now.Month + "-" + DateTime.Now.Year + ".log",
                Layout = "${var:logStamp} ${var:logContent}"};
            
            var rule = new LoggingRule("ConcealmentLog", LogLevel.Debug, target)
            {
                Final = true
            };
            
            LogManager.Configuration.LoggingRules.Insert(0, rule);
            LogManager.Configuration.Reload();
            base.Init(torch);

            SetupConfig();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Save();
            
            LoadModules();
        }

        public void LoadModules()
        {
            if (Config.ConcealmentModulesDirectory == "")
            {
                Directory.CreateDirectory(StoragePath + "/ConcealmentModules");
                Config.ConcealmentModulesDirectory = StoragePath + "/ConcealmentModules";
                Save();
                var str = new StreamWriter(Config.ConcealmentModulesDirectory + "/OfflineRefining.cs");
                str.Write(DefaultModulesValues.Refining);
                str.Close();
                var str1 = new StreamWriter(Config.ConcealmentModulesDirectory + "/ExcludedSubtypes.cs");
                str1.Write(DefaultModulesValues.Subtypes);
                str1.Close();
                var str2 = new StreamWriter(Config.ConcealmentModulesDirectory + "/ExcludedFactions.cs");
                str2.Write(DefaultModulesValues.Factions);
                str2.Close();
                var str3 = new StreamWriter(Config.ConcealmentModulesDirectory + "/ConcealmentLogic.cs");
                str3.Write(DefaultModulesValues.Logic);
                str3.Close();
            }

            ModulePath = Config.ConcealmentModulesDirectory;

            var references = new List<MetadataReference>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic || assembly.Location == null || assembly.Location == "")
                {
                    continue;
                }
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }

            var pa = Assembly.GetAssembly(typeof(TorchBase)).Location;
            pa = pa.Replace("\\Torch.dll", "");
            pa += "/Plugins";

            foreach (var path in Directory.GetFiles(pa))
            {
                if (path.EndsWith("OfflineConcealment.zip"))
                {
                    ZipArchive archive = new ZipArchive(File.Open(path, FileMode.Open));
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.Name.EndsWith("OfflineConcealment.dll"))
                        {
                            references.Add(MetadataReference.CreateFromImage(entry.Open().ReadToEnd()));
                        }
                    }
                }
            }

            foreach (var filePath in Directory.GetFiles(ModulePath))
            {
                var name = Path.GetRandomFileName();
                var comp = CSharpCompilation.Create(name, new SyntaxTree[]
                {
                    CSharpSyntaxTree.ParseText(File.ReadAllText(filePath))
                }, references, new CSharpCompilationOptions((OutputKind.DynamicallyLinkedLibrary)));
                var stream = new MemoryStream();
                var result = comp.Emit(stream);
                if (!result.Success)
                {
                    foreach (var diagnostic in result.Diagnostics)
                    {
                        Log.Fatal(diagnostic.ToString);
                    }
                    Log.Fatal($"Failed loading {name}, please check the code");
                }

                var assembly = Assembly.Load(stream.ToArray());
                object obj = null;
                foreach (var type in assembly.GetTypes())
                {
                    Log.Error(type);
                    if (type.Namespace == "OfflineConcealment.DefaultModules" || type.Namespace == "OfflineConcealment.Modules")
                    {
                        obj = type.GetConstructor(new Type[0])
                            ?.Invoke(Array.Empty<object>()
                            );
                    }
                }
                Log.Warn(obj);
                if (obj is ConcealmentModule module)
                {
                    if (module.Name == null)
                    {
                        Log.Fatal($"{name} does not contain a it's name field");
                    }
                    Modules.Add(module);
                }
                else if (obj is IConcealmentLogic logic)
                {
                    Logic = logic;
                }
                else
                {
                    Log.Fatal($"{name} is not a concealment module, skipping");
                }
                
            }
        }

        public void Start()
        {
            offThread = new Thread(OffThreadUpdate);
            offThread.Start();
        }

        public void OffThreadUpdate()
        {
            while (Config.Enabled)
            {
                if (runcycles % 60 == 0)
                {
                    foreach (var grid in MyEntities.GetEntities().OfType<MyCubeGrid>())
                    {
                        if (!AllStatus.Keys.Contains(grid.EntityId))
                        {
                            AllStatus.Add(grid.EntityId, new ConcealmentData(false, 0));
                        }
                    }
                }
                var spheres = new List<BoundingSphereD>();
                foreach (var plr in MySession.Static.Players.GetOnlinePlayers())
                {
                    if (plr.Character != null)
                    {
                        spheres.Add(new BoundingSphereD(plr.Character.PositionComp.GetPosition(), Config.ConcealRangeMeters));
                    }
                }
                var grids = 0;
                var concealed = 0;
                var copy = new long[AllStatus.Keys.Count];
                AllStatus.Keys.CopyTo(copy, 0);
                foreach (var pair in copy)
                {
                    MainThreadInvoke.Enqueue(() => {var exists = MyEntities.TryGetEntityById(pair, out var en);
                        if (!exists || !(en is MyCubeGrid grid))
                        {
                            AllStatus.Remove(pair);
                            return;
                        }
                        ReturnToOffThread.Enqueue(grid);});
                }
                Thread.Sleep(200);
                while (ReturnToOffThread.TryDequeue(out var grid))
                {
                    grids++;
                    //if in concealment
                    if (AllStatus.TryGetValue(grid.EntityId, out var value))
                    {
                        if (value.Concealed)
                        {
                            var unconcealed = false;
                            foreach (var sphere in spheres)
                            {
                                if (sphere.Contains(grid.GetPhysicalGroupAABB()) != ContainmentType.Disjoint)
                                {
                                    RevealGrid(grid);
                                    MassLog.Info($"Revealed {grid.DisplayName} for reason player");

                                    unconcealed = true;
                                    break;
                                }
                            }
                            if (!unconcealed && Config.Recheck)
                            {
                                if (value.CyclesSinceLastRefresh >= Config.Recheckcycles && testingGrids < Config.RecheckLimit)
                                {
                                    RevealGrid(grid);
                                    MassLog.Info($"Revealed {grid.DisplayName} for reason recheck");
                                    testingGrids++;
                                }

                                value.CyclesSinceLastRefresh++;
                            }
                            if (!unconcealed)
                            {
                                concealed++;
                            }
                        }
                        else
                        {
                            var conceale = true;
                            foreach (var sphere in spheres)
                            {
                                if (sphere.Contains(grid.GetPhysicalGroupAABB()) != ContainmentType.Disjoint)
                                {
                                    conceale = false;
                                    break;
                                }
                            }

                            var a = DoesConceal(grid);
                            if (conceale && a.Item1)
                            {
                                ConcealGrid(grid);
                                MassLog.Info($"Concealed {grid.DisplayName} for reason {a.Item2}");
                            }
                        }
                    }
                }

                if (grids == 0)
                {
                    grids++;
                }
                Log.Info($"Concealed {concealed}/{grids}, {concealed/grids:P}");
                var str = "Enabled Modules: ";
                foreach (var module in Modules)
                {
                    if (module.Enabled)
                    {
                        str += $"{module.Name}, ";
                    }
                }
                Log.Info(str);
                runcycles++;
                Task.Run(async () =>
                {
                    await Task.Delay(1000 * Config.UpdateIntervalSeconds);
                }).GetAwaiter().GetResult();
            }

            RevealAll();
        }

        public void RevealAll()
        {
            foreach (var pair in AllStatus)
            {
                if (pair.Value.Concealed)
                {
                    var grid = MyEntities.GetEntityById(pair.Key);
                    RevealGrid(grid);
                    MassLog.Info($"Revealed {grid.DisplayName} for reason reveal all requested");
                }
            }
        }

        public void ConcealAll()
        {
            foreach (var pair in AllStatus)
            {
                if (!pair.Value.Concealed)
                {
                    var grid = (MyCubeGrid)MyEntities.GetEntityById(pair.Key);
                    ConcealGrid(grid);
                    MassLog.Info($"Concealed {grid.DisplayName} for reason Conceal all requested");
                }
            }
        }

        public int counter = 0;
        
        public override void Update()
        {
            if (counter == 60*Config.SecondsDelayOnStart)
            {
                Start();
                //OffThreadUpdate();
            }
    
            if (MainThreadInvoke.Any())
            {
                while (MainThreadInvoke.TryDequeue(out var action))
                {
                    action.Invoke();
                }
            }

            counter++;
        }

        public void ConcealGrid(MyCubeGrid grid)
        {
            if (Logic != null)
            {
                Logic.OnConceal(grid);
            }
            MyEntities.UnregisterForUpdate(grid);
            grid.OnClose += RevealGrid;
            AllStatus[grid.EntityId].Concealed = true;
        }

        public void RevealGrid(MyEntity en) => RevealGrid((MyCubeGrid)en);
        public void RevealGrid(MyCubeGrid grid)
        {
            grid.OnClose -= RevealGrid;
            MyEntities.RegisterForUpdate(grid);
            if (Logic != null)
            {
                Logic.OnReveal(grid);
            }
            AllStatus[grid.EntityId].Concealed = false;
        }
        
        /// <summary>
        /// Runs checks for if a grid can conceal
        /// </summary>
        /// <param name="grid">the grid to check</param>
        /// <returns>true for should conceal, false for keep out, plus string reason from module</returns>
        public Tuple<bool, string> DoesConceal(MyCubeGrid grid)
        {
            var conceal = true;
            string reason = "players";

            foreach (var plr in MySession.Static.Players.GetOnlinePlayers())
            {
                if (plr.Character != null && new BoundingSphereD(plr.Character.PositionComp.GetPosition(), Config.ConcealRangeMeters).Contains(grid.GetPhysicalGroupAABB()) != ContainmentType.Disjoint)
                {
                    conceal = false;
                    break;
                }
            }
            
            if (conceal)
            {
                foreach (var module in ActiveModules)
                {
                    if (module.AreaLocked)
                    {
                        var contains = false;
                        foreach (var area in module.Areas)
                        {
                            if (area.Contains(grid.GetPhysicalGroupAABB()) != ContainmentType.Disjoint)
                            {
                                contains = true;
                                break;
                            }
                        }

                        if (!contains)
                        {
                            continue;
                        }   
                    }
                    var g = module.GridCheck(grid);
                    if (!g)
                    {
                        conceal = false;
                        reason = module.returnOnCheck;
                        break;
                    }

                    foreach (var block in grid.GetFatBlocks())
                    {
                        if (module.BlockCheck(block))
                        {
                            conceal = false;
                            reason = module.returnOnCheck;
                            break;
                        }
                    }

                    if (!conceal)
                    {
                        break;
                    }
                }
            }

            return new Tuple<bool, string>(conceal, reason);

        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            switch (state)
            {

                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    break;
            }
        }

        public void SetupConfig()
        {

            var configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try
            {

                _config = Persistent<OfflineConcealmentConfig>.Load(configFile);

            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {

                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<OfflineConcealmentConfig>(configFile, new OfflineConcealmentConfig());
                _config.Save();
            }
        }

        public void Save()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }
    }
}
