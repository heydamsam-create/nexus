namespace NEXUS_WindowsForms.Models
{
    /// <summary>
    /// Resultado de una misión, preparado para el futuro historial de NEXUS.
    /// </summary>
    public sealed class MissionResult
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string MissionName { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public string Outcome { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int Score { get; set; }
        public int OxygenRemaining { get; set; }
        public int HullIntegrity { get; set; }
        public int DataRecovered { get; set; }
        public List<string> Decisions { get; set; } = new();
    }
}
