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

    // Se conserva para poder leer y escribir partidas guardadas por la versión anterior.
    public bool SecurityDoorUnlocked { get; set; }
}
