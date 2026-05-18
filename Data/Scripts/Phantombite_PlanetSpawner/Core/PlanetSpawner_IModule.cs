namespace PhantombitePlanetSpawner.Core
{
    /// <summary>
    /// Basis-Interface fuer alle PlanetSpawner Module.
    /// </summary>
    public interface IModule
    {
        string ModuleName { get; }
        void Init();
        void Update();
        void SaveData();
        void Close();
    }
}