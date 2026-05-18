using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using PhantombitePlanetSpawner.Core;
using PhantombitePlanetSpawner.Modules;

namespace PhantombitePlanetSpawner.Modules
{
    /// <summary>
    /// PlanetSpawner_Main — Hauptlogik
    ///
    /// Phase 0: Alle Mod-Planeten aus den geladenen Definitionen auflisten.
    ///          Neu erkannte Planeten werden automatisch mit Enable=false in die Config eingetragen.
    /// Phase 1: Config Check
    ///          [OK]       — Gueltig und aktiv
    ///          [Disabled] — Enable=false, wird uebersprungen
    ///          [Error]    — Ungueltig oder SBC nicht gefunden
    /// Phase 2: Planetenpruefung
    ///          [OK]    — Planet vorhanden
    ///          [Spawn] — Planet nicht gefunden, gespawnt
    ///          [Error] — Spawn fehlgeschlagen
    ///          [Skip]  — Disabled oder Error aus Phase 1
    /// </summary>
    public class PlanetSpawner_Main : IModule
    {
        public string ModuleName => "PlanetSpawner_Main";

        private const string SRC = "PlanetSpawner_Main";

        private static readonly HashSet<string> VanillaPlanets = new HashSet<string>
        {
            "EarthLike", "Mars", "Moon", "Alien", "Europa", "Titan", "Triton", "Pertam",
            "EarthLikeTutorial", "MoonTutorial", "MarsTutorial",
            "EarthLikeModExample", "SystemTestMap"
        };

        private readonly HashSet<string> _availableDefinitions = new HashSet<string>();
        private readonly HashSet<string> _validSubtypeIds      = new HashSet<string>();

        public void Init()    { }
        public void Update()  { }
        public void SaveData(){ }

        public void Close()
        {
            _availableDefinitions.Clear();
            _validSubtypeIds.Clear();
        }

        public List<string> CheckAndSpawnAll(PlanetSpawner_FileManager fileManager)
        {
            _availableDefinitions.Clear();
            _validSubtypeIds.Clear();

            var spawned = new List<string>();

            Log("=======================================");
            Log("  Planet Spawner - Start");
            Log("=======================================");

            Phase0_ListModPlanets(fileManager);
            Phase1_ConfigCheck(fileManager.Planets);
            Phase2_CheckAndSpawn(fileManager.Planets, spawned);

            Log("=======================================");
            Log("  Planet Spawner - Abgeschlossen");
            Log("=======================================");

            return spawned;
        }

        // ── Phase 0 ───────────────────────────────────────────────────────────

        private void Phase0_ListModPlanets(PlanetSpawner_FileManager fileManager)
        {
            Log("[Phase 0] Spawnbare Mod-Planeten:", 1);

            var defs       = MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions();
            var modPlanets = new List<string>();

            foreach (var def in defs)
            {
                string id = def.Id.SubtypeName;
                _availableDefinitions.Add(id);
                if (!VanillaPlanets.Contains(id))
                    modPlanets.Add(id);
            }

            if (modPlanets.Count == 0)
            {
                Log("  Keine Mod-Planeten gefunden.", 1);
                Log("---------------------------------------", 1);
                return;
            }

            foreach (var name in modPlanets)
                Log("  - " + name);
            Log("  Gesamt: " + modPlanets.Count + " Mod-Planet(en)", 1);

            var added = fileManager.AutoAddMissingPlanets(modPlanets);

            if (added.Count > 0)
            {
                Log("  Automatisch zur Config Hinzugefügt:", 1);
                foreach (var name in added)
                    Log("  " + Pad(name, 20) + "[Disabled]", 1);
            }

            Log("---------------------------------------", 1);
        }

        // ── Phase 1 ───────────────────────────────────────────────────────────

        private void Phase1_ConfigCheck(List<PlanetSpawner_FileManager.PlanetConfig> planets)
        {
            Log("[Phase 1] Config Check:", 1);

            if (planets == null || planets.Count == 0)
            {
                Log("  Config leer.", 1);
                Log("---------------------------------------", 1);
                return;
            }

            foreach (var planet in planets)
            {
                string label = !string.IsNullOrEmpty(planet.SectionName)
                    ? planet.SectionName
                    : (!string.IsNullOrEmpty(planet.SubtypeId) ? planet.SubtypeId : "(kein Name)");

                if (!planet.Enable)
                {
                    Log("  " + Pad(label, 22) + "[Disabled]", 1);
                    continue;
                }

                bool basicValid = !string.IsNullOrEmpty(planet.SubtypeId)
                               && planet.Diameter > 0
                               && planet.Seed != 0;

                bool defExists = basicValid && _availableDefinitions.Contains(planet.SubtypeId);

                if (basicValid && defExists)
                {
                    Log("  " + Pad(label, 22) + "[OK]", 1);
                    _validSubtypeIds.Add(planet.SubtypeId);
                }
                else
                {
                    Log("  " + Pad(label, 22) + "[Error]", 1);
                }
            }

            Log("---------------------------------------", 1);
        }

        // ── Phase 2 ───────────────────────────────────────────────────────────

        private void Phase2_CheckAndSpawn(List<PlanetSpawner_FileManager.PlanetConfig> planets, List<string> spawned)
        {
            Log("[Phase 2] Planetenpruefung:");

            if (planets == null || planets.Count == 0)
            {
                Log("  Keine Eintraege zu verarbeiten.", 1);
                Log("---------------------------------------", 1);
                return;
            }

            if (MyAPIGateway.Session == null || MyAPIGateway.Session.VoxelMaps == null)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "VoxelMaps nicht verfuegbar");
                Log("---------------------------------------", 1);
                return;
            }

            var voxelMaps = new List<IMyVoxelBase>();
            try
            {
                MyAPIGateway.Session.VoxelMaps.GetInstances(voxelMaps, v => v != null);
            }
            catch (System.Exception ex)
            {
                PlanetSpawner_Logger.Instance?.Error(SRC, "GetInstances Fehler: " + ex.Message);
                Log("---------------------------------------", 1);
                return;
            }

            foreach (var planet in planets)
            {
                if (planet == null) continue;

                string display = Pad(planet.SubtypeId ?? "(kein Name)", 12);

                if (!planet.Enable || string.IsNullOrEmpty(planet.SubtypeId)
                    || !_validSubtypeIds.Contains(planet.SubtypeId))
                {
                    Log("  " + display + "[Skip]", 1);
                    continue;
                }

                string storageName = planet.SubtypeId + "-" + planet.Seed + "d" + (int)planet.Diameter;

                bool found = false;
                foreach (var voxel in voxelMaps)
                {
                    if (voxel == null) continue;
                    if (voxel.StorageName == storageName || voxel.StorageName.StartsWith(planet.SubtypeId))
                    {
                        Log("  " + display + "[OK]    " + voxel.StorageName);
                        found = true;
                        break;
                    }
                }

                if (found) continue;

                Vector3D center   = new Vector3D(planet.PosX, planet.PosY, planet.PosZ);
                Vector3D spawnPos = center - new Vector3D(planet.Diameter / 2.0);

                IMyVoxelBase result = null;
                try
                {
                    result = MyAPIGateway.Session.VoxelMaps.SpawnPlanet(
                        planet.SubtypeId, planet.Diameter, planet.Seed, spawnPos);
                }
                catch (System.Exception ex)
                {
                    PlanetSpawner_Logger.Instance?.Error(SRC, "SpawnPlanet Fehler: " + ex.Message);
                    Log("  " + display + "[Error]", 1);
                    continue;
                }

                if (result != null)
                {
                    Log("  " + display + "[Spawn] " + result.StorageName);
                    spawned.Add(planet.SubtypeId);
                }
                else
                {
                    Log("  " + display + "[Error]", 1);
                }
            }

            Log("---------------------------------------", 1);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string Pad(string s, int width)
        {
            if (s == null) s = "";
            return s.Length >= width ? s + " " : s + new string(' ', width - s.Length);
        }

        private void Log(string message, int level = 0)
        {
            PlanetSpawner_Logger.Instance?.Log(SRC, message, level);
        }
    }
}