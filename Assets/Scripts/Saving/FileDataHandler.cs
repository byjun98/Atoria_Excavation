using System;
using System.IO;
using UnityEngine;

namespace Rebuild.Saving
{



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
            if (!File.Exists(full)) return AttemptRollback();
            try
            {
                string json = File.ReadAllText(full);
                return JsonUtility.FromJson<GameDataDTO>(json) ?? AttemptRollback();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Rebuild.Saving] Failed to load '{full}': {e}");

                return AttemptRollback();
            }
        }

        GameDataDTO AttemptRollback()
        {
            if (!File.Exists(BackupPath)) return null;
            try
            {
                string json = File.ReadAllText(BackupPath);
                var dto = JsonUtility.FromJson<GameDataDTO>(json);
                if (dto == null) return null;
                try { WriteSnapshot(FullPath, json); }
                catch (Exception e) { Debug.LogWarning($"[Rebuild.Saving] Loaded backup, but could not replace primary: {e}"); }
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

                string json = JsonUtility.ToJson(data, prettyPrint: true);
                WriteSnapshot(full, json);
                _saveCount++;

                if (_saveCount % BackupEverySaves == 0)
                {
                    try { WriteSnapshot(BackupPath, json); }
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

        static void WriteSnapshot(string path, string json)
        {
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(flushToDisk: true);
                }

                if (File.Exists(path)) File.Replace(tempPath, path, null);
                else File.Move(tempPath, path);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch (IOException e) { Debug.LogWarning($"[Rebuild.Saving] Temporary save cleanup failed: {e}"); }
                }
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
