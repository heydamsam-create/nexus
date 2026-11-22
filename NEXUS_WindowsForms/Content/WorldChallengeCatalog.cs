namespace NEXUS_WindowsForms.Content;

internal sealed record WorldEventDefinition(string Id, string RoomId, string ChallengeId, string AlertText,
    float DamageIntervalSeconds = 0, int OxygenDamage = 0, int HullDamage = 0, bool DamageAnywhere = false);

internal sealed record WorldChallengeDefinition(string Id, string Title, string Instruction,
    int[] Sequence, string SuccessText, string? RequiredItemId = null,
    int OxygenChange = 0, int HullChange = 0, int DataChange = 0,
    int FailureOxygenDamage = 0, int FailureHullDamage = 0);

/// <summary>Eventos de sala y secuencias de reparación de la expedición.</summary>
internal static class WorldChallengeCatalog
{
    public static readonly WorldEventDefinition[] Events =
    {
        new("life_support_failure", "control_center", "life_support_reset",
            "AURA // ALERTA DE EMERGENCIA: el soporte vital está inestable. Reinicia la consola del puente.",
            12, OxygenDamage: 2, DamageAnywhere: true),
        new("communications_blackout", "communications", "communications_restore",
            "AURA // FALLO DE COMUNICACIONES: la antena perdió sincronía. Recalibra el relé de sala."),
        new("lab_power_surge", "main_lab", "generator_repair",
            "AURA // SOBRECARGA ELÉCTRICA: el generador está aislado. Requiere un fusible del almacén.",
            10, HullDamage: 2),
        new("research_lockdown", "research", "security_override",
            "AURA // BLOQUEO DE SEGURIDAD: sincroniza el teclado auxiliar antes de usar la consola."),
        new("anomaly_instability", "anomaly_chamber", "anomaly_stabilize",
            "AURA // PULSO ANÓMALO: el campo está dañando el casco. Calibra el nodo de resonancia.",
            10, HullDamage: 2)
    };

    public static readonly WorldChallengeDefinition[] Challenges =
    {
        new("life_support_reset", "CONSOLA DE SOPORTE VITAL",
            "Reinicia el flujo: secuencia de emergencia 3 - 1 - 3.", new[] { 3, 1, 3 },
            "Soporte vital restaurado. AURA estabilizó el suministro de oxígeno.", OxygenChange: 12,
            FailureOxygenDamage: 2),
        new("communications_restore", "RELÉ DE COMUNICACIONES",
            "Recalibra la antena: patrón de sincronía 2 - 4 - 1.", new[] { 2, 4, 1 },
            "Comunicaciones restauradas. La señal de Eridani-9 ya puede analizarse.", DataChange: 6,
            FailureOxygenDamage: 1),
        new("generator_repair", "GENERADOR AUXILIAR",
            "Instala el fusible y cierra el circuito: secuencia 1 - 4 - 2.", new[] { 1, 4, 2 },
            "Generador reparado. El laboratorio vuelve a tener energía.", "spare_fuse",
            HullChange: 8, DataChange: 3, FailureHullDamage: 2),
        new("security_override", "TECLADO DE SEGURIDAD",
            "Introduce el código de contingencia: 4 - 2 - 1.", new[] { 4, 2, 1 },
            "Bloqueo de seguridad cancelado. La consola está disponible.", DataChange: 2,
            FailureOxygenDamage: 1),
        new("anomaly_stabilize", "NODO DE RESONANCIA",
            "Responde al pulso en orden inverso: 3 - 2 - 1.", new[] { 3, 2, 1 },
            "Resonancia estabilizada. AURA puede leer el núcleo sin dañar el casco.",
            HullChange: 8, DataChange: 10, FailureHullDamage: 2)
    };

    public static WorldEventDefinition? EventForRoom(string roomId) =>
        Events.FirstOrDefault(item => item.RoomId.Equals(roomId, StringComparison.OrdinalIgnoreCase));

    public static WorldEventDefinition? FindEvent(string eventId) =>
        Events.FirstOrDefault(item => item.Id.Equals(eventId, StringComparison.OrdinalIgnoreCase));

    public static WorldChallengeDefinition? FindChallenge(string challengeId) =>
        Challenges.FirstOrDefault(item => item.Id.Equals(challengeId, StringComparison.OrdinalIgnoreCase));
}
