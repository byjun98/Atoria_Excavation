namespace Rebuild.Saving
{
    /// <summary>implemented by every persistable manager.
    /// DataPersistenceManager calls these in dependency order on load (Gold first → Level →
    /// SkillTree → Anvil → Mine → Artifacts) and in any order on save (push-only).</summary>
    public interface IDataPersistence
    {
        /// <summary>Populate the manager's runtime state from the loaded DTO.</summary>
        void LoadData(GameDataDTO data);

        /// <summary>Write the manager's current runtime state into the DTO before it's serialised.</summary>
        void SaveData(GameDataDTO data);
    }
}
