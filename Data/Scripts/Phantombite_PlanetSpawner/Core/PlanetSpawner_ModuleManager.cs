using System;
using System.Collections.Generic;
using VRage.Utils;

namespace PhantombitePlanetSpawner.Core
{
    /// <summary>
    /// Verwaltet alle PlanetSpawner Module mit Fehler-Isolation.
    /// </summary>
    public class ModuleManager
    {
        private const string LOG = "[PhantombitePlanetSpawner|ModuleManager]";

        private readonly List<IModule> _modules = new List<IModule>();

        public void Register(IModule module)
        {
            if (module == null) return;
            _modules.Add(module);
            MyLog.Default.WriteLineAndConsole(LOG + " Registriert: " + module.ModuleName);
        }

        public void InitAll()
        {
            foreach (var m in _modules)
            {
                try   { m.Init(); }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole(LOG + " FEHLER Init '" + m.ModuleName + "': " + ex);
                }
            }
        }

        public void UpdateAll()
        {
            foreach (var m in _modules)
            {
                try   { m.Update(); }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole(LOG + " FEHLER Update '" + m.ModuleName + "': " + ex);
                }
            }
        }

        public void SaveAll()
        {
            foreach (var m in _modules)
            {
                try   { m.SaveData(); }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole(LOG + " FEHLER SaveData '" + m.ModuleName + "': " + ex);
                }
            }
        }

        public void CloseAll()
        {
            foreach (var m in _modules)
            {
                try   { m.Close(); }
                catch (Exception ex)
                {
                    MyLog.Default.WriteLineAndConsole(LOG + " FEHLER Close '" + m.ModuleName + "': " + ex);
                }
            }
            _modules.Clear();
        }

        public T Get<T>() where T : class, IModule
        {
            foreach (var m in _modules)
                if (m is T) return (T)m;
            return null;
        }
    }
}