namespace NEXUS_WindowsForms.Models;

/// <summary>
/// Estado pequeño y serializable de la zona 2D para continuar una expedición.
/// </summary>
internal sealed class ExplorationSaveState
{
    public float PlayerX { get; set; } = 190;
    public float PlayerY { get; set; } = 190;
    public List<string> CollectedItems { get; set; } = new();
    public List<string> ExploredRooms { get; set; } = new();
    public List<string> CompletedObjectives { get; set; } = new();
    public List<string> UnlockedDoors { get; set; } = new();
    public List<string> UsedItems { get; set; } = new();
    public List<string> ReadDocuments { get; set; } = new();
    public List<string> FoundSecrets { get; set; } = new();
    public List<string> SeenDialogues { get; set; } = new();
    public List<string> NarrativeDecisions { get; set; } = new();
    public List<string> TriggeredWorldEvents { get; set; } = new();
    public List<string> ActiveWorldEvents { get; set; } = new();
    public List<string> CompletedWorldEvents { get; set; } = new();
    public List<string> CompletedChallenges { get; set; } = new();
    public Dictionary<string, float> WorldEventClocks { get; set; } = new();
    public List<EnemySaveState> Enemies { get; set; } = new();
    public string? ActiveChallengeId { get; set; }
    public int ActiveChallengeStep { get; set; }
    public string? ActiveDialogueId { get; set; }
    public int ActiveDialogueLineIndex { get; set; }
    public int ActiveDialogueChoiceIndex { get; set; }

    // Se conserva para poder leer y escribir partidas guardadas por la versión anterior.
    public bool SecurityDoorUnlocked { get; set; }
}

/// <summary>Posición y estado de alerta de una amenaza para restaurar la expedición desde JSON.</summary>
internal sealed class EnemySaveState
{
    public string Id { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public bool Alerted { get; set; }
    // Defaults preserve compatibility with saves created before combat was added.
    public int Health { get; set; } = 100;
    public bool IsDead { get; set; }
}
