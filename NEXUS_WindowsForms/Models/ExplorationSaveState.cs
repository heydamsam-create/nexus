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
    public bool SecurityDoorUnlocked { get; set; }
}
