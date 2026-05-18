using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using PhantombitePlanetSpawner.Core;
using PhantombitePlanetSpawner.Modules;

namespace PhantombitePlanetSpawner.Modules
{
    /// <summary>
    /// PlanetSpawner_FileManager
    ///
    /// Verwaltet die Planet-Config im World-Storage.
    /// Datei: PlanetSpawner_Config.ini
    ///
    /// Format:
    ///   [Planet_Sulvax]
    ///   Enable=false
    ///   SubtypeId=Sulvax
    ///   Seed=12345
    ///   Diameter=120000
    ///   PosX=578292.44
    ///   PosY=87234.60
    ///   PosZ=3629005.91
    /// </summary>
    public class PlanetSpawner_FileManager : IModule
    {
        public string ModuleName => "PlanetSpawner_FileManager";

        private const string SRC         = "PlanetSpawner_FileManager";
        private const string CONFIG_FILE = "PlanetSpawner_Config.ini";

        private const int   AUTO_SEED     = 12345;
        private const float AUTO_DIAMETER = 120000f;

        public List<PlanetConfig> Planets { get; private set; } = new List<PlanetConfig>();

        public class PlanetConfig
        {
            public string SectionName;
            public bool   Enable = true;
            public string SubtypeId;
            public int    Seed;
            public float  Diameter;
            public double PosX;
            public double PosY;
            public double PosZ;
        }

        // ── IModule ───────────────────────────────────────────────────────────

        public void Init()
        {
            EnsureConfigExists();
            LoadConfig();
        }

        public void Update()   { }
        public void SaveData() { }

        public void Close()
        {
            Planets.Clear();
        }

        // ── Auto-Detection ────────────────────────────────────────────────────

        public List<string> AutoAddMissingPlanets(List<string> modPlanetIds)
        {
            var added    = new List<string>();
            var existing = new HashSet<string>();

            foreach (var p in Planets)
                if (!string.IsNullOrEmpty(p.SubtypeId))
                    existing.Add(p.SubtypeId);

            foreach (var id in modPlanetIds)
            {
                if (existing.Contains(id)) continue;

                AppendPlanetToConfig(id);
                Planets.Add(new PlanetConfig
                {
                    SectionName = "Planet_" + id,
                    Enable      = false,
                    SubtypeId   = id,
                    Seed        = AUTO_SEED,
                    Diameter    = AUTO_DIAMETER,
                    PosX = 0, PosY = 0, PosZ = 0
                });
                added.Add(id);
            }

            return added;
        }

        private void AppendPlanetToConfig(string subtypeId)
        {
            try
            {
                string existing = ReadFile(CONFIG_FILE) ?? "";
                var sb = new StringBuilder();
                sb.Append(existing);
                sb.AppendLine();
                sb.AppendLine("# Automatisch erkannt — Koordinaten eintragen und Enable=true setzen");
                sb.AppendLine("[Planet_" + subtypeId + "]");
                sb.AppendLine("Enable=false");
                sb.AppendLine("SubtypeId=" + subtypeId);
                sb.AppendLine("Seed=" + AUTO_SEED);
                sb.AppendLine("Diameter=" + (int)AUTO_DIAMETER);
                sb.AppendLine("PosX=0");
                sb.AppendLine("PosY=0");
                sb.AppendLine("PosZ=0");
                WriteFile(CONFIG_FILE, sb.ToString());
                Log("Auto-hinzugefuegt: " + subtypeId);
            }
            catch (Exception ex)
            {
                Error("AppendPlanetToConfig Fehler: " + ex.Message);
            }
        }

        // ── Config ────────────────────────────────────────────────────────────

        private void EnsureConfigExists()
        {
            try
            {
                if (!FileExists(CONFIG_FILE))
                {
                    DeployDefaultConfig();
                    Log("Standard-Config erstellt: " + CONFIG_FILE);
                }
            }
            catch (Exception ex)
            {
                Error("EnsureConfigExists Fehler: " + ex.Message);
            }
        }

        private void DeployDefaultConfig()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# ==============================================================================");
            sb.AppendLine("# PlanetSpawner Config — Phantombite_PlanetSpawner");
            sb.AppendLine("# ==============================================================================");
            sb.AppendLine("# Enable     = true / false");
            sb.AppendLine("# SubtypeId  = SubtypeId aus der Planeten-SBC");
            sb.AppendLine("# Seed       = Zahl fuer die Planetengenerierung");
            sb.AppendLine("# Diameter   = Durchmesser in Meter");
            sb.AppendLine("# PosX/Y/Z   = Mittelpunkt in Weltkoordinaten");
            sb.AppendLine("# ==============================================================================");
            WriteFile(CONFIG_FILE, sb.ToString());
        }

        private void LoadConfig()
        {
            try
            {
                Planets.Clear();
                string content = ReadFile(CONFIG_FILE);
                if (content == null)
                {
                    Error("Config nicht lesbar");
                    return;
                }

                PlanetConfig current = null;

                foreach (var rawLine in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    if (line.StartsWith("[Planet_") && line.EndsWith("]"))
                    {
                        if (current != null) Planets.Add(current);
                        current = new PlanetConfig
                        {
                            SectionName = line.Substring(1, line.Length - 2),
                            Enable      = true
                        };
                        continue;
                    }

                    if (current == null) continue;

                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    switch (key)
                    {
                        case "Enable":    current.Enable = val.ToLower() == "true"; break;
                        case "SubtypeId": current.SubtypeId = val; break;
                        case "Seed":      int.TryParse(val, out current.Seed); break;
                        case "Diameter":
                            float.TryParse(val,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out current.Diameter);
                            break;
                        case "PosX":
                            double.TryParse(val,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out current.PosX);
                            break;
                        case "PosY":
                            double.TryParse(val,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out current.PosY);
                            break;
                        case "PosZ":
                            double.TryParse(val,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out current.PosZ);
                            break;
                    }
                }

                if (current != null) Planets.Add(current);
                Log(Planets.Count + " Eintrag/Eintraege geladen");
            }
            catch (Exception ex)
            {
                Error("LoadConfig Fehler: " + ex.Message);
            }
        }

        // ── File I/O ──────────────────────────────────────────────────────────

        private string ReadFile(string filename)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(filename, typeof(PlanetSpawner_FileManager)))
                    return null;
                using (var r = MyAPIGateway.Utilities.ReadFileInWorldStorage(filename, typeof(PlanetSpawner_FileManager)))
                    return r.ReadToEnd();
            }
            catch (Exception ex)
            {
                Error("ReadFile Fehler: " + ex.Message);
                return null;
            }
        }

        private void WriteFile(string filename, string content)
        {
            try
            {
                using (var w = MyAPIGateway.Utilities.WriteFileInWorldStorage(filename, typeof(PlanetSpawner_FileManager)))
                    w.Write(content);
            }
            catch (Exception ex)
            {
                Error("WriteFile Fehler: " + ex.Message);
            }
        }

        private bool FileExists(string filename)
        {
            try { return MyAPIGateway.Utilities.FileExistsInWorldStorage(filename, typeof(PlanetSpawner_FileManager)); }
            catch { return false; }
        }

        // ── Log Shortcuts ─────────────────────────────────────────────────────

        private void Log(string message)   => PlanetSpawner_Logger.Instance?.Info(SRC, message);
        private void Error(string message) => PlanetSpawner_Logger.Instance?.Error(SRC, message);
    }
}