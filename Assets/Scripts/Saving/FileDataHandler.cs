using System;
using System.IO;
using UnityEngine;

namespace Rebuild.Saving
{
    /// <summary>JSON file read/write under <see cref="Application.persistentDataPath"/>.
    /// `.bak` backup every 5 saves + AttemptRollback on Load failure (mirrors
    /// original FileDataHandler's saveIncrement toggle).</summary>
    public sealed class FileDataHandler
    {
        const int BackupEverySaves = 5;
        readonly string _dirPath;
        readonly string _fileName;
        int _saveCount;

        public FileDataHandler(string dirPath, string fileName)
        {
            _dirPath = dirPath;
            _fileName = fileName;
        }

        public string FullPath => Path.Combine(_dirPath, _fileName);
        public string BackupPath => FullPath + ".bak";

        public GameDataDTO Load()
        {
            var full = FullPath;
            if (!File.Exists(full))
            {
                // primary missing but bak survives → restore.
                if (File.Exists(BackupPath))
                {
                    try
                    {
                        File.Copy(BackupPath, full, overwrite: true);
                        Debug.Log("[Rebuild.Saving] Primary save missing — restored from .bak.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Rebuild.Saving] .bak restore failed: {e}");
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            try
            {
                string json = File.ReadAllText(full);
                var dto = JsonUtility.FromJson<GameDataDTO>(json);
                // JsonUtility can return null when the JSON is unparseable, or arrays sized 0
                // from a corrupt save — caller (DataPersistenceManager) treats null as NewGame.
                return dto;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Rebuild.Saving] Failed to load '{full}': {e}");
                // primary parse failed. Try .bak rollback once.
                return AttemptRollback();
            }
        }

        GameDataDTO AttemptRollback()
        {
            if (!File.Exists(BackupPath)) return null;
            try
            {
                File.Copy(BackupPath, FullPath, overwrite: true);
                string json = File.ReadAllText(FullPath);
                var dto = JsonUtility.FromJson<GameDataDTO>(json);
                Debug.LogWarning("[Rebuild.Saving] Corrupt primary — rolled back to .bak.");
                return dto;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Rebuild.Saving] .bak rollback failed: {e}");
                return null;
            }
        }

        public bool Save(GameDataDTO data)
        {
            var full = FullPath;
            try
            {
                if (!Directory.Exists(_dirPath)) Directory.CreateDirectory(_dirPath);
                data.lastSavedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                // prettyPrint=true so a human can diff saves while debugging .
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(full, json);
                _saveCount++;
                // backup every Nth save so a corrupt primary stays recoverable.
                if (_saveCount % BackupEverySaves == 0)
                {
                    try { File.Copy(full, BackupPath, overwrite: true); }
                    catch (Exception e) { Debug.LogWarning($"[Rebuild.Saving] .bak write failed: {e}"); }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Rebuild.Saving] Failed to save '{full}': {e}");
                return false;
            }
        }

        public void Delete()
        {
            var full = FullPath;
            try { if (File.Exists(full)) File.Delete(full); }
            catch (Exception e) { Debug.LogError($"[Rebuild.Saving] Failed to delete '{full}': {e}"); }
            try { if (File.Exists(BackupPath)) File.Delete(BackupPath); }
            catch (Exception e) { Debug.LogError($"[Rebuild.Saving] Failed to delete .bak: {e}"); }
        }
    }
}
