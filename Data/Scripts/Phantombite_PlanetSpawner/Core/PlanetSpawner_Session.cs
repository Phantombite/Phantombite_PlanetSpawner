using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game;
using PhantombitePlanetSpawner.Core;
using PhantombitePlanetSpawner.Modules;

namespace PhantombitePlanetSpawner.Core
{
    /// <summary>
    /// PlanetSpawner Session — Entry Point
    ///
    /// MyUpdateOrder.BeforeSimulation — Commands werden gequeuet und im
    /// Simulation-Thread ausgefuehrt. Nur so funktionieren VoxelMaps und SpawnPlanet.
    ///
    /// Commands (alle Admin-only):
    ///   !pbc planetspawner check  — Planeten prüfen und fehlende spawnen
    ///   !pbc planetspawner list   — Alle konfigurierten Planeten auflisten
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class PlanetSpawner_Session : MySessionComponentBase
    {
        private const string SRC          = "PlanetSpawner_Session";
        private const string VERSION      = "1.0.0";
        private const long   CHANNEL      = 1995010L;
        private const long   CHANNEL_CORE = 1995000L;

        private ModuleManager             _modules;
        private PlanetSpawner_FileManager _fileManager;
        private PlanetSpawner_Main        _main;

        private Action _pendingAction = null;

        // ── LoadData ──────────────────────────────────────────────────────────

        public override void LoadData()
        {
            try
            {
                MyAPIGateway.Utilities.RegisterMessageHandler(CHANNEL, OnMessageReceived);
            }
            catch (Exception ex)
            {
                // Logger noch nicht verfuegbar — direkt loggen
                VRage.Utils.MyLog.Default.WriteLineAndConsole(
                    "[ERROR] Phantombite_PlanetSpawner/PlanetSpawner_Session : LoadData Fehler: " + ex);
            }
        }

        // ── UpdateBeforeSimulation — Command Queue ────────────────────────────

        public override void UpdateBeforeSimulation()
        {
            if (_pendingAction == null) return;

            var action     = _pendingAction;
            _pendingAction = null;

            try { action(); }
            catch (Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "Fehler in pending action: " + ex);
            }
        }

        // ── Message Handler ───────────────────────────────────────────────────

        private void OnMessageReceived(object data)
        {
            try
            {
                string msg = data as string;
                if (string.IsNullOrEmpty(msg)) return;

                if (msg == "READY")              { OnReady();       return; }
                if (msg.StartsWith("CMD|"))      { OnCommand(msg);  return; }
                if (msg.StartsWith("LOGLEVEL|")) { OnLogLevel(msg); return; }
                if (msg.StartsWith("PERFLEVEL|"))
                {
                    int lvl;
                    if (int.TryParse(msg.Substring(10), out lvl))
                    {
                        PlanetSpawner_Logger.Instance?.Log(SRC, "PERFLEVEL empfangen: " + lvl + " (kein Throttle)", 1);
                        MyAPIGateway.Utilities.SendModMessage(CHANNEL_CORE, "PERFACK|planetspawner|" + lvl);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "Fehler in OnMessageReceived: " + ex);
            }
        }

        // ── LOGLEVEL ──────────────────────────────────────────────────────────

        private void OnLogLevel(string msg)
        {
            string[] parts = msg.Split('|');
            if (parts.Length < 2) return;
            PlanetSpawner_Logger.Instance?.SetLogLevel(parts[1]);
        }

        // ── READY ─────────────────────────────────────────────────────────────

        private void OnReady()
        {
            try
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    InitModules();
                    PlanetSpawner_Logger.Instance?.Info(SRC, "READY empfangen — starte Initialisierung");
                    _main.CheckAndSpawnAll(_fileManager);
                    PlanetSpawner_Logger.Instance?.Info(SRC, "Initialisierung abgeschlossen");
                }
            }
            catch (Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "Fehler in OnReady: " + ex);
            }
            finally
            {
                SendRegister();
            }
        }

        // ── CMD — Command Router ──────────────────────────────────────────────

        private void OnCommand(string msg)
        {
            if (!MyAPIGateway.Multiplayer.IsServer) return;

            try
            {
                string[] parts = msg.Split('|');
                if (parts.Length < 3) return;

                string cmdName = parts[1].ToLower();

                ulong  steamId   = 0;
                string steamPart = parts[parts.Length - 1];
                if (steamPart.StartsWith("STEAM:"))
                    ulong.TryParse(steamPart.Substring(6), out steamId);

                string arg = "";
                if (parts.Length > 3)
                {
                    var argParts = new List<string>();
                    for (int i = 2; i < parts.Length - 1; i++)
                        argParts.Add(parts[i]);
                    arg = string.Join("|", argParts);
                }

                PlanetSpawner_Logger.Instance?.Debug(SRC,
                    "Command: '" + cmdName + "' Arg: '" + arg + "' — Queue fuer naechsten Tick");

                switch (cmdName)
                {
                    case "check":
                        _pendingAction = () => ExecuteCheck(steamId);
                        break;
                    case "list":
                        _pendingAction = () => ExecuteList(steamId);
                        break;
                    default:
                        SendCmdResult(cmdName, arg, steamId, false, "Unbekannter Command: " + cmdName);
                        break;
                }
            }
            catch (Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "Fehler in OnCommand: " + ex);
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────

        private void ExecuteCheck(ulong steamId)
        {
            try
            {
                PlanetSpawner_Logger.Instance?.Info(SRC, "ExecuteCheck (Simulation-Thread)");
                InitModules();

                List<string> spawned = _main.CheckAndSpawnAll(_fileManager);

                string result = spawned != null && spawned.Count > 0
                    ? "Gespawnt: " + string.Join(", ", spawned)
                    : "Alle Planeten vorhanden";

                SendCmdResult("check", "", steamId, true, result);
            }
            catch (Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "Fehler in ExecuteCheck: " + ex);
                SendCmdResult("check", "", steamId, false, "Fehler beim Check");
            }
        }

        private void ExecuteList(ulong steamId)
        {
            try
            {
                PlanetSpawner_Logger.Instance?.Info(SRC, "ExecuteList");
                InitModules();

                if (_fileManager.Planets == null || _fileManager.Planets.Count == 0)
                {
                    SendCmdResult("list", "", steamId, true, "Keine Planeten konfiguriert.");
                    return;
                }

                bool isSingleplayer = MyAPIGateway.Session != null &&
                                      MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;

                var sb = new StringBuilder();
                sb.AppendLine("=== Planet Spawner Liste ===");

                foreach (var planet in _fileManager.Planets)
                {
                    string name   = !string.IsNullOrEmpty(planet.SubtypeId) ? planet.SubtypeId : "(?)";
                    string status = planet.Enable ? "[ON] " : "[OFF]";
                    string coords = (planet.PosX == 0 && planet.PosY == 0 && planet.PosZ == 0)
                        ? " — Koordinaten fehlen!"
                        : string.Format(" X={0:F0} Y={1:F0} Z={2:F0}", planet.PosX, planet.PosY, planet.PosZ);
                    string line = status + " " + name + coords;
                    sb.AppendLine(line);

                    if (isSingleplayer)
                        MyAPIGateway.Utilities.ShowMessage("[PlanetSpawner]", line);
                }

                PlanetSpawner_Logger.Instance?.Info(SRC, sb.ToString());

                var compact = new StringBuilder();
                foreach (var planet in _fileManager.Planets)
                {
                    if (compact.Length > 0) compact.Append(" | ");
                    string name   = !string.IsNullOrEmpty(planet.SubtypeId) ? planet.SubtypeId : "(?)";
                    string status = planet.Enable ? "[ON]" : "[OFF]";
                    compact.Append(name + " " + status);
                }

                SendCmdResult("list", "", steamId, true, compact.ToString());
            }
            catch (Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "Fehler in ExecuteList: " + ex);
                SendCmdResult("list", "", steamId, false, "Fehler beim Auflisten");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void InitModules()
        {
            _modules?.CloseAll();
            _modules = new ModuleManager();

            var logger = new PlanetSpawner_Logger();
            _modules.Register(logger);

            _fileManager = new PlanetSpawner_FileManager();
            _modules.Register(_fileManager);

            _main = new PlanetSpawner_Main();
            _modules.Register(_main);

            _modules.InitAll();
        }

        private void SendRegister()
        {
            string msg = "REGISTER|planetspawner|Planet Spawner — Spawnt Mod Planeten beim Serverstart|" + VERSION + "|" + CHANNEL
                + "|check:1:Planeten prüfen und fehlende spawnen"
                + "|list:1:Alle konfigurierten Planeten auflisten";
            MyAPIGateway.Utilities.SendModMessage(CHANNEL_CORE, msg);
            MyAPIGateway.Utilities.SendModMessage(CHANNEL_CORE, "PERFACK|planetspawner|0");
            PlanetSpawner_Logger.Instance?.Log(SRC, "REGISTER + PERFACK|0 gesendet an Core");
        }

        private void SendCmdResult(string cmdName, string args, ulong steamId, bool ok, string message)
        {
            string status = ok ? "ok" : "error";
            string result = "CMDRESULT|planetspawner|" + cmdName + "|" + args + "|" + steamId + "|" + status + "|" + message;
            MyAPIGateway.Utilities.SendModMessage(CHANNEL_CORE, result);
            PlanetSpawner_Logger.Instance?.Info(SRC, "CMDRESULT [" + status + "]: " + message);
        }

        // ── UnloadData ────────────────────────────────────────────────────────

        protected override void UnloadData()
        {
            try
            {
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.UnregisterMessageHandler(CHANNEL, OnMessageReceived);
                _modules?.CloseAll();
            }
            catch (Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "Fehler in UnloadData: " + ex);
            }
        }
    }
}