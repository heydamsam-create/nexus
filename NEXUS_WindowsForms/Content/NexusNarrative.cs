using NEXUS_WindowsForms.Models;

namespace NEXUS_WindowsForms.Content;

internal sealed record NarrativeLine(string Speaker, string Text);
internal sealed record NarrativeChoice(string Id, string Text);
internal sealed record NarrativeDialogue(string Id, string TriggerId, string Title,
    IReadOnlyList<NarrativeLine> Lines, IReadOnlyList<NarrativeChoice> Choices);
internal sealed record NarrativeObjective(string Title, string Room, bool IsComplete, bool IsSecondary);

/// <summary>Contenido narrativo y objetivos de Operación Umbral de Eos.</summary>
internal static class NexusNarrative
{
    private static readonly NarrativeDialogue[] Dialogues =
    {
        new("bridge_intro", "ship_log", "REGISTRO DEL PUENTE", new[]
        {
            new NarrativeLine("AURA // IA DE NEXUS", "Año 2297. El puente conserva el último informe de la comandante Vance. La expedición científica llegó a Eridani-9 siguiendo una señal desde el cráter de Eos."),
            new NarrativeLine("AURA // IA DE NEXUS", "La tripulación está incomunicada desde la interferencia. Puedo reconstruir la ruta con los registros que recuperes."),
            new NarrativeLine("AURA // IA DE NEXUS", "¿Cómo quieres proceder con mis informes?")
        }, new[]
        {
            new NarrativeChoice("TRUST_AI", "Confiar en AURA y revisar sus datos."),
            new NarrativeChoice("VERIFY_AI", "Verificar cada informe con los registros de la tripulación.")
        }),
        new("signal_contact", "signal_terminal", "CANAL DE COMUNICACIONES", new[]
        {
            new NarrativeLine("MARA ILYAN // REGISTRO", "Si alguien escucha esto: la señal repite fragmentos de nuestros propios mensajes, pero responde antes de que terminemos de transmitir."),
            new NarrativeLine("AURA // IA DE NEXUS", "El patrón admite una respuesta codificada. También puedo mantener silencio y continuar registrando sus variaciones."),
            new NarrativeLine("AURA // IA DE NEXUS", "Selecciona el protocolo de primer contacto.")
        }, new[]
        {
            new NarrativeChoice("ANSWER_SIGNAL", "Responder con el protocolo científico."),
            new NarrativeChoice("OBSERVE_SIGNAL", "Mantener silencio y observar.")
        }),
        new("research_protocol", "research_log", "PROTOCOLO DE INVESTIGACIÓN", new[]
        {
            new NarrativeLine("DRA. SATO // NOTA DE LABORATORIO", "El campo de Eos reacciona a pulsos eléctricos. No sabemos si el núcleo es una máquina, una forma de vida o un fenómeno natural."),
            new NarrativeLine("AURA // IA DE NEXUS", "La célula de energía permite estabilizar el mecanismo de contención. Una descarga controlada también podría aislar la señal."),
            new NarrativeLine("AURA // IA DE NEXUS", "Tu protocolo se conservará para la decisión final.")
        }, new[]
        {
            new NarrativeChoice("STABILIZE", "Preparar una contención para estudiarlo."),
            new NarrativeChoice("ISOLATE", "Preparar el aislamiento de la señal.")
        }),
        new("hidden_archive", "secret_archive", "ARCHIVO SELLADO", new[]
        {
            new NarrativeLine("ING. ROJAS // MENSAJE PRIVADO", "La señal respondió usando una frase que solo la tripulación conocía. AURA clasificó el mensaje como ruido antes de que yo pudiera copiarlo."),
            new NarrativeLine("AURA // IA DE NEXUS", "El archivo fue ocultado por orden de seguridad. No tengo permiso para confirmar quién dio la orden."),
            new NarrativeLine("AURA // IA DE NEXUS", "¿Cómo registramos este hallazgo?")
        }, new[]
        {
            new NarrativeChoice("PRESERVE_EVIDENCE", "Conservar el archivo original."),
            new NarrativeChoice("SHARE_EVIDENCE", "Añadirlo al informe de la expedición.")
        }),
        new("anomaly_contact", "anomaly_terminal", "NÚCLEO DE EOS", new[]
        {
            new NarrativeLine("AURA // IA DE NEXUS", "El núcleo está sincronizado con la señal recibida. El patrón que elegiste en el laboratorio puede ayudar a contenerlo o aislarlo."),
            new NarrativeLine("SEÑAL DESCONOCIDA", "NEXUS… recordamos… vuestra llegada…"),
            new NarrativeLine("AURA // IA DE NEXUS", "La expedición puede responder una última vez antes de resolver la anomalía.")
        }, new[]
        {
            new NarrativeChoice("ACKNOWLEDGE", "Responder: hemos venido a comprender."),
            new NarrativeChoice("DISCONNECT", "Cortar el enlace y proteger la nave.")
        })
    };

    public static NarrativeDialogue? ForTrigger(string triggerId) =>
        Dialogues.FirstOrDefault(dialogue => dialogue.TriggerId.Equals(triggerId, StringComparison.OrdinalIgnoreCase));

    public static NarrativeDialogue? Find(string dialogueId) =>
        Dialogues.FirstOrDefault(dialogue => dialogue.Id.Equals(dialogueId, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<NarrativeObjective> GetObjectives(ExplorationSaveState state)
    {
        bool HasItem(string id) => state.CollectedItems.Contains(id, StringComparer.OrdinalIgnoreCase);
        bool HasObjective(string id) => state.CompletedObjectives.Contains(id, StringComparer.OrdinalIgnoreCase);
        bool HasRead(string id) => state.ReadDocuments.Contains(id, StringComparer.OrdinalIgnoreCase);
        bool HasSecret(string id) => state.FoundSecrets.Contains(id, StringComparer.OrdinalIgnoreCase);

        return new[]
        {
            new NarrativeObjective("Recuperar la bitácora del puente", "CENTRO DE CONTROL", HasRead("ship_log"), false),
            new NarrativeObjective("Encontrar la tarjeta de acceso", "ALMACÉN", HasItem("access_card"), false),
            new NarrativeObjective("Descifrar la señal", "COMUNICACIONES", HasObjective("signal_terminal"), false),
            new NarrativeObjective("Recuperar la célula de energía", "LABORATORIO PRINCIPAL", HasItem("power_cell"), false),
            new NarrativeObjective("Validar el protocolo de seguridad", "INVESTIGACIÓN", HasObjective("security_console"), false),
            new NarrativeObjective("Asegurar los datos del núcleo", "CÁMARA DE LA ANOMALÍA", HasObjective("anomaly_terminal"), false),
            new NarrativeObjective("Leer el manifiesto de carga", "ALMACÉN", HasRead("manifest_document"), true),
            new NarrativeObjective("Leer las transmisiones", "COMUNICACIONES", HasRead("communications_log"), true),
            new NarrativeObjective("Leer el informe del laboratorio", "LABORATORIO PRINCIPAL", HasRead("lab_document"), true),
            new NarrativeObjective("Leer las notas de investigación", "INVESTIGACIÓN", HasRead("research_log"), true),
            new NarrativeObjective("Recuperar el archivo oculto", "COMUNICACIONES", HasSecret("secret_archive"), true),
            new NarrativeObjective("Leer el registro de la anomalía", "CÁMARA DE LA ANOMALÍA", HasRead("anomaly_record"), true)
        };
    }
}
