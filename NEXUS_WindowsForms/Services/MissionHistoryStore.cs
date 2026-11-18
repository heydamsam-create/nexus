using System.Text.Json;
using NEXUS_WindowsForms.Models;

namespace NEXUS_WindowsForms.Services
{
    /// <summary>
    /// Lee y guarda resultados de misión en el perfil local del usuario.
    /// </summary>
    internal sealed class MissionHistoryStore
    {
        private readonly object _syncRoot = new();
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public MissionHistoryStore()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _filePath = Path.Combine(appDataPath, "NEXUS", "mission-history.json");
        }

        public bool TryLoad(out List<MissionResult> results, out string errorMessage)
        {
            lock (_syncRoot)
            {
                try
                {
                    results = ReadAll();
                    errorMessage = string.Empty;
                    return true;
                }
                catch (Exception exception)
                {
                    results = new List<MissionResult>();
                    errorMessage = $"No se pudo leer el historial: {exception.Message}";
                    return false;
                }
            }
        }

        public bool TrySave(MissionResult result, out bool alreadySaved, out string errorMessage)
        {
            lock (_syncRoot)
            {
                string temporaryPath = _filePath + ".tmp";

                try
                {
                    List<MissionResult> results = ReadAll();
                    alreadySaved = results.Any(item => item.Id == result.Id);
                    if (alreadySaved)
                    {
                        errorMessage = string.Empty;
                        return true;
                    }

                    results.Add(result);
                    Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

                    string json = JsonSerializer.Serialize(results, _jsonOptions);
                    File.WriteAllText(temporaryPath, json);
                    File.Move(temporaryPath, _filePath, true);

                    errorMessage = string.Empty;
                    return true;
                }
                catch (Exception exception)
                {
                    alreadySaved = false;
                    errorMessage = $"No se pudo guardar el resultado: {exception.Message}";

                    try
                    {
                        if (File.Exists(temporaryPath))
                        {
                            File.Delete(temporaryPath);
                        }
                    }
                    catch
                    {
                        // El error principal se conserva para mostrarlo en la aplicación.
                    }

                    return false;
                }
            }
        }

        private List<MissionResult> ReadAll()
        {
            if (!File.Exists(_filePath))
            {
                return new List<MissionResult>();
            }

            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<MissionResult>();
            }

            List<MissionResult>? savedResults = JsonSerializer.Deserialize<List<MissionResult>>(json, _jsonOptions);
            var uniqueResults = new List<MissionResult>();
            var seenIds = new HashSet<Guid>();

            foreach (MissionResult? result in savedResults ?? new List<MissionResult>())
            {
                if (result is null)
                {
                    continue;
                }

                if (result.Id == Guid.Empty)
                {
                    result.Id = Guid.NewGuid();
                }

                if (seenIds.Add(result.Id))
                {
                    uniqueResults.Add(result);
                }
            }

            return uniqueResults
                .OrderByDescending(result => result.CompletedAt)
                .ToList();
        }
    }
}
