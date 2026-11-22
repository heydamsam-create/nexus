using NEXUS_WindowsForms.Models;
using NEXUS_WindowsForms.Rendering;
using NEXUS_WindowsForms.Content;

namespace NEXUS_WindowsForms.Controls;

internal sealed class ExplorationActionEventArgs : EventArgs
{
    public string Decision { get; }
    public int OxygenChange { get; }
    public int HullChange { get; }
    public int DataChange { get; }
    public bool CompletesExploration { get; }

    public ExplorationActionEventArgs(string decision, int oxygenChange = 0, int hullChange = 0,
        int dataChange = 0, bool completesExploration = false)
    {
        Decision = decision;
        OxygenChange = oxygenChange;
        HullChange = hullChange;
        DataChange = dataChange;
        CompletesExploration = completesExploration;
    }
}

/// <summary>
/// Mapa 2D dibujado con GDI+. El movimiento y las colisiones se calculan en píxeles del mundo.
/// </summary>
internal sealed class ExplorationView : Control
{
    private const int TileSize = 44;
    private const int MapColumns = 38;
    private const int MapRows = 30;
    private const float PlayerRadius = 12f;
    private const float MovementSpeed = 210f;
    private static readonly int[] ConnectorColumns = { 6, 18, 30 };

    private readonly bool[,] _walls = new bool[MapColumns, MapRows];
    private readonly HashSet<string> _unlockedDoors = new(StringComparer.OrdinalIgnoreCase);
    private static readonly RoomSpec[] Rooms =
    {
        new("control_center", "CENTRO DE CONTROL", 2, 2, 10, 8, Color.FromArgb(17, 39, 63), Color.FromArgb(52, 207, 239)),
        new("communications", "SALA DE COMUNICACIONES", 14, 2, 10, 8, Color.FromArgb(20, 34, 66), Color.FromArgb(105, 141, 255)),
        new("main_lab", "LABORATORIO PRINCIPAL", 26, 2, 10, 8, Color.FromArgb(15, 52, 57), Color.FromArgb(67, 230, 195)),
        new("storage", "ALMACÉN DE SUMINISTROS", 2, 20, 10, 8, Color.FromArgb(47, 38, 34), Color.FromArgb(244, 174, 87)),
        new("research", "ZONA DE INVESTIGACIÓN", 14, 20, 10, 8, Color.FromArgb(38, 31, 58), Color.FromArgb(191, 127, 255)),
        new("anomaly_chamber", "CÁMARA DE LA ANOMALÍA", 26, 20, 10, 8, Color.FromArgb(43, 24, 54), Color.FromArgb(255, 91, 198))
    };
    private static readonly DoorSpec[] Doors =
    {
        new("control_access", 6, 9, "control_center", null, null, true),
        new("communications_access", 18, 9, "communications", "access_card", null),
        new("laboratory_access", 30, 9, "main_lab", "access_card", null),
        new("storage_access", 6, 20, "storage", null, null, true),
        new("research_access", 18, 20, "research", "power_cell", null),
        new("anomaly_access", 30, 20, "anomaly_chamber", null, "security_console")
    };
    private readonly RectangleF[] _furniture =
    {
        new(3 * TileSize + 5, 3 * TileSize + 4, 33, 26),
        new(9 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(15 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(21 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(27 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(33 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(3 * TileSize + 4, 22 * TileSize + 4, 34, 28),
        new(9 * TileSize + 4, 22 * TileSize + 4, 34, 28),
        new(15 * TileSize + 4, 22 * TileSize + 4, 34, 28),
        new(21 * TileSize + 4, 22 * TileSize + 4, 34, 28),
        new(27 * TileSize + 4, 22 * TileSize + 4, 34, 28),
        new(33 * TileSize + 4, 22 * TileSize + 4, 34, 28)
    };
    private readonly ExplorerSpriteSheet _sprites = new();
    private readonly Dictionary<string, Bitmap> _roomFloorTextures = Rooms.ToDictionary(
        room => room.Id, room => CreateFloorTexture(room.FloorColor), StringComparer.OrdinalIgnoreCase);
    private readonly Bitmap _floorTile = CreateFloorTexture(Color.FromArgb(13, 27, 43));
    private float _cameraX;
    private float _cameraY;
    private float _animationClock;
    private bool _isMoving;
    private float _interactionTimer;
    private ExplorerDirection _facing = ExplorerDirection.Down;
    private int _oxygen = 100;
    private int _hull = 100;
    private int _data;
    private bool _inventoryOpen;
    private int _inventorySelection;
    private bool _missionLogOpen;
    private NarrativeDialogue? _activeDialogue;
    private int _dialogueLineIndex;
    private int _dialogueChoiceIndex;
    private WorldChallengeDefinition? _activeChallenge;
    private int _challengeStep;
    private string _statusMessage = "Explora la nave. Acércate a un marcador y pulsa E.";

    private readonly WorldObject[] _objects =
    {
        new("ship_log", "BITÁCORA DEL PUENTE", 4, 4, "Registro: la nave entró en órbita de Eridani-9.", 0, 0, 3, "document"),
        new("access_card", "TARJETA DE ACCESO", 4, 23, "Tarjeta de seguridad recogida.", 0, 0, 0, "item"),
        new("oxygen_cell", "CÉLULA DE OXÍGENO", 7, 23, "Célula de oxígeno añadida al inventario.", 0, 0, 0, "item"),
        new("toolkit", "KIT DE REPARACIÓN", 9, 25, "Kit de reparación añadido al inventario.", 0, 0, 0, "item"),
        new("manifest_document", "MANIFIESTO DE CARGA", 4, 26, "El almacén recibió suministros antes de la anomalía.", 0, 0, 2, "document"),
        new("spare_fuse", "FUSIBLE AUXILIAR", 7, 26, "Fusible de repuesto añadido al inventario.", 0, 0, 0, "item"),
        new("life_support_reset", "CONSOLA DE SOPORTE VITAL", 4, 6, "", 0, 0, 0, "challenge"),
        new("signal_terminal", "ANTENA DE COMUNICACIONES", 18, 5, "Descifrar la señal repetida de Eridani-9.", 0, 0, 12, "objective"),
        new("communications_restore", "RELÉ DE COMUNICACIONES", 22, 7, "", 0, 0, 0, "challenge"),
        new("communications_log", "REGISTRO DE TRANSMISIONES", 22, 5, "La señal contiene una respuesta inteligente.", 0, 0, 3, "document"),
        new("secret_archive", "ARCHIVO OCULTO", 22, 8, "", 0, 0, 8, "secret", "toolkit", "secret_datacube"),
        new("power_cell", "CÉLULA DE ENERGÍA", 30, 5, "Célula de energía añadida al inventario.", 0, 0, 0, "item"),
        new("generator_repair", "GENERADOR AUXILIAR", 34, 7, "", 0, 0, 0, "challenge"),
        new("lab_document", "INFORME DE LABORATORIO", 27, 7, "El campo de la anomalía reacciona a pulsos eléctricos.", 0, 0, 3, "document"),
        new("research_log", "CUADERNO DE INVESTIGACIÓN", 15, 24, "El acceso a la cámara requiere validar el protocolo.", 0, 0, 3, "document"),
        new("security_override", "TECLADO DE SEGURIDAD", 22, 24, "", 0, 0, 0, "challenge"),
        new("security_console", "CONSOLA DE INVESTIGACIÓN", 18, 25, "", 0, -4, 5, "objective"),
        new("anomaly_stabilize", "NODO DE RESONANCIA", 33, 23, "", 0, 0, 0, "challenge"),
        new("anomaly_terminal", "NÚCLEO DE DATOS", 30, 24, "Asegurar los datos del núcleo y completar la expedición.", 0, 0, 18, "objective"),
        new("anomaly_record", "ECO DE LA ANOMALÍA", 33, 25, "El patrón parece responder a la presencia humana.", 0, 0, 5, "document")
    };

    public ExplorationSaveState State { get; private set; }
    public event EventHandler? StateChanged;
    public event EventHandler<ExplorationActionEventArgs>? ActionCompleted;

    public ExplorationView(ExplorationSaveState? state = null)
    {
        State = state ?? new ExplorationSaveState();
        NormalizeState();
        State.PlayerX = Math.Clamp(State.PlayerX, TileSize * 2f, (MapColumns - 2) * TileSize);
        State.PlayerY = Math.Clamp(State.PlayerY, TileSize * 2f, (MapRows - 2) * TileSize);
        BuildMap();
        ApplySavedDoorState();
        DiscoverCurrentRoom();
        RestoreActiveDialogue();
        RestoreActiveChallenge();
        BackColor = Color.FromArgb(4, 11, 24);
        ForeColor = NexusTheme.Text;
        DoubleBuffered = true;
        // La vista debe poder recibir el teclado al abrir una consola interactiva.
        TabStop = true;
        MinimumSize = new Size(420, 300);
    }

    public string ObjectiveSummary =>
        $"{GetNextObjectiveLabel().Replace("OBJ: ", string.Empty, StringComparison.Ordinal)}\r\n" +
        $"SISTEMAS RESTAURADOS: {State.CompletedChallenges.Count}/{WorldChallengeCatalog.Challenges.Length}\r\n" +
        "J: ver objetivos principales y secundarios";

    public bool InventoryOpen => _inventoryOpen;
    public bool DialogueOpen => _activeDialogue is not null;
    public bool DialogueAtChoices => _activeDialogue is not null && _dialogueLineIndex >= _activeDialogue.Lines.Count;
    public int DialogueChoiceSelection => _dialogueChoiceIndex;
    public bool ChallengeOpen => _activeChallenge is not null;
    public int ChallengeStep => _challengeStep;
    public int ChallengeLength => _activeChallenge?.Sequence.Length ?? 0;
    public bool MissionLogOpen => _missionLogOpen;
    public bool BlocksMovement => _inventoryOpen || _missionLogOpen || DialogueOpen || ChallengeOpen;

    public void ToggleInventory()
    {
        if (DialogueOpen || ChallengeOpen || _missionLogOpen) return;
        _inventoryOpen = !_inventoryOpen;
        _inventorySelection = Math.Clamp(_inventorySelection, 0, Math.Max(0, GetInventoryItems().Count - 1));
        Invalidate();
    }

    public void ToggleMissionLog()
    {
        if (DialogueOpen || ChallengeOpen || _inventoryOpen) return;
        _missionLogOpen = !_missionLogOpen;
        Invalidate();
    }

    public void AdvanceDialogue()
    {
        if (_activeDialogue is null || _dialogueLineIndex >= _activeDialogue.Lines.Count) return;
        _dialogueLineIndex++;
        State.ActiveDialogueLineIndex = _dialogueLineIndex;
        StateChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void MoveDialogueChoice(int direction)
    {
        if (_activeDialogue is null || _dialogueLineIndex < _activeDialogue.Lines.Count ||
            _activeDialogue.Choices.Count == 0) return;
        _dialogueChoiceIndex = (_dialogueChoiceIndex + direction + _activeDialogue.Choices.Count) %
            _activeDialogue.Choices.Count;
        State.ActiveDialogueChoiceIndex = _dialogueChoiceIndex;
        Invalidate();
    }

    public void ChooseDialogueOption(int index)
    {
        NarrativeDialogue? dialogue = _activeDialogue;
        if (dialogue is null || _dialogueLineIndex < dialogue.Lines.Count ||
            index < 0 || index >= dialogue.Choices.Count) return;

        NarrativeChoice choice = dialogue.Choices[index];
        string savedDecision = $"{dialogue.Id}:{choice.Id}";
        AddUnique(State.NarrativeDecisions, savedDecision);
        AddUnique(State.SeenDialogues, dialogue.Id);
        State.ActiveDialogueId = null;
        State.ActiveDialogueLineIndex = 0;
        State.ActiveDialogueChoiceIndex = 0;
        _activeDialogue = null;
        _dialogueLineIndex = 0;
        _dialogueChoiceIndex = 0;
        _statusMessage = $"DECISIÓN REGISTRADA: {choice.Text}";
        StateChanged?.Invoke(this, EventArgs.Empty);
        ActionCompleted?.Invoke(this, new ExplorationActionEventArgs($"{dialogue.Title}: {choice.Text}",
            completesExploration: dialogue.TriggerId == "anomaly_terminal"));
        Invalidate();
    }

    public void MoveInventorySelection(int direction)
    {
        List<InventoryEntry> items = GetInventoryItems();
        if (items.Count == 0) return;
        _inventorySelection = (_inventorySelection + direction + items.Count) % items.Count;
        Invalidate();
    }

    public void UseSelectedInventoryItem()
    {
        List<InventoryEntry> items = GetInventoryItems();
        if (items.Count == 0)
        {
            _statusMessage = "El inventario está vacío.";
            Invalidate();
            return;
        }

        _inventorySelection = Math.Clamp(_inventorySelection, 0, items.Count - 1);
        InventoryEntry selected = items[_inventorySelection];
        switch (selected.Id)
        {
            case "oxygen_cell":
                if (_oxygen >= 100)
                {
                    _statusMessage = "El oxígeno ya está al máximo.";
                    break;
                }
                State.UsedItems.Add(selected.Id);
                _statusMessage = "Célula utilizada: oxígeno restaurado.";
                ActionCompleted?.Invoke(this, new ExplorationActionEventArgs("Usar célula de oxígeno", 18));
                StateChanged?.Invoke(this, EventArgs.Empty);
                break;
            case "toolkit":
                if (_hull >= 100)
                {
                    _statusMessage = "La integridad del casco ya está al máximo.";
                    break;
                }
                State.UsedItems.Add(selected.Id);
                _statusMessage = "Kit utilizado: casco reparado.";
                ActionCompleted?.Invoke(this, new ExplorationActionEventArgs("Usar kit de reparación", 0, 15));
                StateChanged?.Invoke(this, EventArgs.Empty);
                break;
            case "power_cell":
                _statusMessage = "La célula alimenta el acceso de investigación al acercarte a su puerta.";
                break;
            case "spare_fuse":
                _statusMessage = "Fusible reservado para el generador auxiliar del laboratorio.";
                break;
            case "access_card":
                _statusMessage = "La tarjeta se valida automáticamente en las puertas de comunicaciones y laboratorio.";
                break;
            case "secret_datacube":
                if (!State.UsedItems.Contains(selected.Id, StringComparer.OrdinalIgnoreCase))
                {
                    State.UsedItems.Add(selected.Id);
                    _statusMessage = "El archivo secreto añade datos al expediente.";
                    ActionCompleted?.Invoke(this, new ExplorationActionEventArgs("Analizar archivo secreto", 0, 0, 8));
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
                break;
            default:
                if (IsDocument(selected.Id))
                {
                    bool firstRead = !State.ReadDocuments.Contains(selected.Id, StringComparer.OrdinalIgnoreCase);
                    AddUnique(State.ReadDocuments, selected.Id);
                    CompleteObjective($"read:{selected.Id}");
                    _statusMessage = GetDocumentText(selected.Id);
                    if (firstRead)
                    {
                        StartDialogueFor(selected.Id);
                        StateChanged?.Invoke(this, EventArgs.Empty);
                        ActionCompleted?.Invoke(this, new ExplorationActionEventArgs($"Leer {GetItemName(selected.Id)}"));
                    }
                }
                break;
        }

        _inventoryOpen = false;
        Invalidate();
    }

    public bool MovePlayer(int horizontal, int vertical, float elapsedSeconds)
    {
        if (BlocksMovement) return false;
        if (horizontal == 0 && vertical == 0)
        {
            _animationClock += Math.Clamp(elapsedSeconds, 0f, 0.05f);
            _interactionTimer = Math.Max(0, _interactionTimer - elapsedSeconds);
            _isMoving = false;
            Invalidate();
            return false;
        }

        _isMoving = true;
        _animationClock += Math.Clamp(elapsedSeconds, 0f, 0.05f);
        _interactionTimer = Math.Max(0, _interactionTimer - elapsedSeconds);
        _facing = Math.Abs(horizontal) > Math.Abs(vertical)
            ? horizontal < 0 ? ExplorerDirection.Left : ExplorerDirection.Right
            : vertical < 0 ? ExplorerDirection.Up : ExplorerDirection.Down;
        float length = MathF.Sqrt(horizontal * horizontal + vertical * vertical);
        float distance = MovementSpeed * Math.Clamp(elapsedSeconds, 0f, 0.05f);
        float deltaX = horizontal / length * distance;
        float deltaY = vertical / length * distance;
        bool moved = MoveAxis(deltaX, horizontalAxis: true);
        moved |= MoveAxis(deltaY, horizontalAxis: false);

        if (moved)
        {
            if (DiscoverCurrentRoom())
            {
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
            Invalidate();
        }

        return moved;
    }

    public void RestoreState(ExplorationSaveState state)
    {
        State = state ?? new ExplorationSaveState();
        NormalizeState();
        State.PlayerX = Math.Clamp(State.PlayerX, TileSize * 2f, (MapColumns - 2) * TileSize);
        State.PlayerY = Math.Clamp(State.PlayerY, TileSize * 2f, (MapRows - 2) * TileSize);
        _unlockedDoors.Clear();
        BuildMap();
        ApplySavedDoorState();
        DiscoverCurrentRoom();
        _inventorySelection = 0;
        _missionLogOpen = false;
        RestoreActiveDialogue();
        RestoreActiveChallenge();
        Invalidate();
    }

    public void Interact()
    {
        if (BlocksMovement) return;
        _interactionTimer = 0.55f;
        _animationClock += 0.03f;

        DoorSpec? door = Doors
            .Where(candidate => IsDoorClosed(candidate))
            .OrderBy(DistanceTo)
            .FirstOrDefault();
        if (door is not null && DistanceTo(door) <= 72f)
        {
            if (!DoorRequirementMet(door))
            {
                _statusMessage = DoorRequirementMessage(door);
                Invalidate();
                return;
            }

            UnlockDoor(door);
            _statusMessage = $"PUERTA DESBLOQUEADA: {GetRoom(door.RoomId).Title}.";
            StateChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return;
        }

        WorldObject? nearby = _objects
            .Where(item => !State.CollectedItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
            .Where(item => !item.IsSecret || DistanceTo(item) <= 180f)
            .OrderBy(item => DistanceTo(item))
            .FirstOrDefault();

        if (nearby is null || DistanceTo(nearby) > 68f)
        {
            _statusMessage = "No hay ningún objeto o terminal al alcance.";
            Invalidate();
            return;
        }

        if (nearby.Id == "signal_terminal" && !State.CompletedChallenges.Contains("communications_restore", StringComparer.OrdinalIgnoreCase))
        {
            _statusMessage = "AURA // SIN COMUNICACIONES: repara el relé de la sala antes de analizar la señal.";
            Invalidate();
            return;
        }

        if (nearby.Id == "power_cell" && !State.CompletedChallenges.Contains("generator_repair", StringComparer.OrdinalIgnoreCase))
        {
            _statusMessage = "AURA // GENERADOR SIN ENERGÍA: instala el fusible del almacén en el generador auxiliar.";
            Invalidate();
            return;
        }

        if (nearby.Id == "security_console" && !State.CompletedChallenges.Contains("security_override", StringComparer.OrdinalIgnoreCase))
        {
            _statusMessage = "AURA // BLOQUEO ACTIVO: sincroniza primero el teclado de seguridad.";
            Invalidate();
            return;
        }

        if (nearby.Id == "anomaly_terminal" && !State.CompletedChallenges.Contains("anomaly_stabilize", StringComparer.OrdinalIgnoreCase))
        {
            _statusMessage = "AURA // NÚCLEO INESTABLE: calibra el nodo de resonancia antes de extraer datos.";
            Invalidate();
            return;
        }

        if (nearby.Kind == "challenge")
        {
            BeginChallenge(nearby.Id);
            return;
        }

        if (nearby.RequiredItemId is not null && !HasAvailableItem(nearby.RequiredItemId))
        {
            _statusMessage = nearby.RequiredItemId == "toolkit"
                ? "ARCHIVO SELLADO: necesitas el kit de reparación del almacén."
                : $"INTERACCIÓN BLOQUEADA: necesitas {GetItemName(nearby.RequiredItemId)}.";
            Invalidate();
            return;
        }

        if (nearby.Id == "security_console")
        {
            bool hasTerminal = State.CompletedObjectives.Contains("signal_terminal", StringComparer.OrdinalIgnoreCase);
            bool hasCard = State.CollectedItems.Contains("access_card", StringComparer.OrdinalIgnoreCase);
            bool hasPower = HasAvailableItem("power_cell");
            if (!hasTerminal || !hasCard || !hasPower)
            {
                _statusMessage = "La consola requiere la señal, la tarjeta y una célula de energía.";
                Invalidate();
                return;
            }
        }

        State.CollectedItems.Add(nearby.Id);
        if (nearby.InventoryId is not null && !State.CollectedItems.Contains(nearby.InventoryId, StringComparer.OrdinalIgnoreCase))
        {
            State.CollectedItems.Add(nearby.InventoryId);
        }

        if (nearby.Kind == "document")
        {
            AddUnique(State.ReadDocuments, nearby.Id);
            CompleteObjective($"read:{nearby.Id}");
            _statusMessage = GetDocumentText(nearby.Id);
            StartDialogueFor(nearby.Id);
            ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(nearby.Title,
                nearby.OxygenChange, nearby.HullChange, nearby.DataChange));
            StateChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return;
        }

        if (nearby.Kind == "secret")
        {
            AddUnique(State.FoundSecrets, nearby.Id);
            AddUnique(State.CollectedItems, nearby.InventoryId ?? "secret_datacube");
            CompleteObjective("secret_archive");
            _statusMessage = "ARCHIVO SECRETO RECUPERADO: se añadió un cubo de datos al inventario.";
            StartDialogueFor(nearby.Id);
            ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(nearby.Title,
                nearby.OxygenChange, nearby.HullChange, nearby.DataChange));
            StateChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
            return;
        }

        if (nearby.Id is "signal_terminal" or "access_card" or "security_console" or "anomaly_terminal")
        {
            CompleteObjective(nearby.Id);
        }
        if (nearby.Id == "security_console")
        {
            AddUnique(State.UsedItems, "power_cell");
            DoorSpec anomalyDoor = Doors.Single(candidate => candidate.Id == "anomaly_access");
            UnlockDoor(anomalyDoor);
            State.SecurityDoorUnlocked = true;
            CompleteObjective("security_door");
        }

        _statusMessage = nearby.Id switch
        {
            "signal_terminal" => "Señal descifrada. La investigación puede continuar.",
            "access_card" => "Tarjeta de acceso recogida. Comunicaciones y laboratorio disponibles.",
            "oxygen_cell" => "Célula de oxígeno recogida. Abre el inventario para usarla.",
            "toolkit" => "Kit de reparación recogido. Abre el inventario para usarlo.",
            "spare_fuse" => "Fusible auxiliar recogido. Llévalo al generador del laboratorio.",
            "power_cell" => "Célula de energía recogida. Desbloquea la zona de investigación.",
            "security_console" => "Protocolo validado. La cámara de la anomalía está desbloqueada.",
            "anomaly_terminal" => "Datos del núcleo asegurados. Exploración completada.",
            _ => "Interacción completada."
        };

        StartDialogueFor(nearby.Id);

        ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(
            nearby.Title,
            nearby.OxygenChange,
            nearby.HullChange,
            nearby.DataChange,
            nearby.Id == "anomaly_terminal" && !DialogueOpen));
        StateChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    public void SetStatus(string message)
    {
        _statusMessage = message;
        Invalidate();
    }

    public void SetMissionVitals(int oxygen, int hull, int data)
    {
        _oxygen = Math.Clamp(oxygen, 0, 100);
        _hull = Math.Clamp(hull, 0, 100);
        _data = Math.Clamp(data, 0, 100);
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sprites.Dispose();
            _floorTile.Dispose();
            foreach (Bitmap texture in _roomFloorTextures.Values)
                texture.Dispose();
        }
        base.Dispose(disposing);
    }

    public void SubmitChallengeKey(int key)
    {
        WorldChallengeDefinition? challenge = _activeChallenge;
        if (challenge is null || key is < 1 or > 4) return;

        if (challenge.Sequence[_challengeStep] != key)
        {
            _challengeStep = 0;
            State.ActiveChallengeStep = 0;
            _statusMessage = $"AURA // SECUENCIA INCORRECTA. {challenge.Instruction}";
            StateChanged?.Invoke(this, EventArgs.Empty);
            ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(
                $"Fallo de calibración: {challenge.Title}", challenge.FailureOxygenDamage * -1,
                challenge.FailureHullDamage * -1));
            Invalidate();
            return;
        }

        _challengeStep++;
        State.ActiveChallengeStep = _challengeStep;
        if (_challengeStep >= challenge.Sequence.Length)
        {
            AddUnique(State.CompletedChallenges, challenge.Id);
            CompleteObjective(challenge.Id);
            if (challenge.RequiredItemId is not null)
                AddUnique(State.UsedItems, challenge.RequiredItemId);

            WorldEventDefinition? worldEvent = WorldChallengeCatalog.Events
                .FirstOrDefault(item => item.ChallengeId.Equals(challenge.Id, StringComparison.OrdinalIgnoreCase));
            if (worldEvent is not null)
            {
                State.ActiveWorldEvents.RemoveAll(item => item.Equals(worldEvent.Id, StringComparison.OrdinalIgnoreCase));
                AddUnique(State.CompletedWorldEvents, worldEvent.Id);
                State.WorldEventClocks.Remove(worldEvent.Id);
            }

            _activeChallenge = null;
            State.ActiveChallengeId = null;
            State.ActiveChallengeStep = 0;
            _statusMessage = challenge.SuccessText;
            StateChanged?.Invoke(this, EventArgs.Empty);
            ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(challenge.Title,
                challenge.OxygenChange, challenge.HullChange, challenge.DataChange));
        }
        else
        {
            _statusMessage = $"AURA // CALIBRACIÓN {challenge.Title}: {_challengeStep}/{challenge.Sequence.Length} pulsos.";
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        Invalidate();
    }

    public void AdvanceWorld(float elapsedSeconds)
    {
        if (_inventoryOpen || _missionLogOpen || DialogueOpen) return;
        string? currentRoom = GetRoomIdAtTile((int)(State.PlayerX / TileSize), (int)(State.PlayerY / TileSize));
        foreach (string eventId in State.ActiveWorldEvents.ToArray())
        {
            WorldEventDefinition? worldEvent = WorldChallengeCatalog.FindEvent(eventId);
            if (worldEvent is null || worldEvent.DamageIntervalSeconds <= 0 ||
                (!worldEvent.DamageAnywhere && !string.Equals(currentRoom, worldEvent.RoomId, StringComparison.OrdinalIgnoreCase)))
                continue;

            State.WorldEventClocks.TryGetValue(eventId, out float elapsed);
            elapsed += Math.Clamp(elapsedSeconds, 0, 0.25f);
            if (elapsed < worldEvent.DamageIntervalSeconds)
            {
                State.WorldEventClocks[eventId] = elapsed;
                continue;
            }

            State.WorldEventClocks[eventId] = elapsed - worldEvent.DamageIntervalSeconds;
            _statusMessage = worldEvent.OxygenDamage > 0
                ? $"AURA // SOPORTE VITAL INESTABLE: O₂ -{worldEvent.OxygenDamage}%."
                : $"AURA // INTERFERENCIA ACTIVA: casco -{worldEvent.HullDamage}%.";
            ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(
                $"Daño ambiental: {worldEvent.Id}", -worldEvent.OxygenDamage, -worldEvent.HullDamage));
            StateChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        float viewWidth = ClientSize.Width;
        float viewHeight = ClientSize.Height;
        _cameraX = Math.Clamp(State.PlayerX - viewWidth / 2f, 0, MapColumns * TileSize - viewWidth);
        _cameraY = Math.Clamp(State.PlayerY - viewHeight / 2f, 0, MapRows * TileSize - viewHeight);
        graphics.TranslateTransform(-_cameraX, -_cameraY);

        DrawMap(graphics);
        DrawSystemEventEffects(graphics);
        DrawAnomalyField(graphics);
        DrawFurniture(graphics);
        DrawObjects(graphics);
        DrawPlayer(graphics);

        graphics.ResetTransform();
        DrawOverlay(graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && _activeChallenge is not null)
        {
            Focus();
            Rectangle challengePanel = GetChallengePanelBounds();
            for (int index = 0; index < 4; index++)
            {
                if (GetChallengeKeyBounds(challengePanel, index).Contains(e.Location))
                {
                    SubmitChallengeKey(index + 1);
                    return;
                }
            }
            return;
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        Focus();
        if (_activeDialogue is null || _dialogueLineIndex < _activeDialogue.Lines.Count) return;

        Rectangle panel = GetDialoguePanelBounds();
        int optionY = panel.Bottom - 82;
        for (int index = 0; index < _activeDialogue.Choices.Count; index++)
        {
            Rectangle optionBounds = new(panel.X + 24, optionY + index * 36, panel.Width - 48, 30);
            if (optionBounds.Contains(e.Location))
            {
                ChooseDialogueOption(index);
                return;
            }
        }
    }

    private void StartDialogueFor(string triggerId)
    {
        if (_activeDialogue is not null) return;
        NarrativeDialogue? dialogue = NexusNarrative.ForTrigger(triggerId);
        if (dialogue is null || State.SeenDialogues.Contains(dialogue.Id, StringComparer.OrdinalIgnoreCase)) return;

        _activeDialogue = dialogue;
        _dialogueLineIndex = 0;
        _dialogueChoiceIndex = 0;
        State.ActiveDialogueId = dialogue.Id;
        State.ActiveDialogueLineIndex = 0;
        State.ActiveDialogueChoiceIndex = 0;
    }

    private void RestoreActiveDialogue()
    {
        _activeDialogue = string.IsNullOrWhiteSpace(State.ActiveDialogueId)
            ? null
            : NexusNarrative.Find(State.ActiveDialogueId);
        if (_activeDialogue is null || State.SeenDialogues.Contains(_activeDialogue.Id, StringComparer.OrdinalIgnoreCase))
        {
            _activeDialogue = null;
            State.ActiveDialogueId = null;
            return;
        }

        _dialogueLineIndex = Math.Clamp(State.ActiveDialogueLineIndex, 0, _activeDialogue.Lines.Count);
        _dialogueChoiceIndex = Math.Clamp(State.ActiveDialogueChoiceIndex, 0,
            Math.Max(0, _activeDialogue.Choices.Count - 1));
    }

    private void BeginChallenge(string objectId)
    {
        WorldChallengeDefinition? challenge = WorldChallengeCatalog.FindChallenge(objectId);
        if (challenge is null) return;
        if (State.CompletedChallenges.Contains(challenge.Id, StringComparer.OrdinalIgnoreCase))
        {
            _statusMessage = $"AURA // {challenge.Title}: sistema operativo.";
            Invalidate();
            return;
        }

        WorldEventDefinition? worldEvent = WorldChallengeCatalog.Events
            .FirstOrDefault(item => item.ChallengeId.Equals(challenge.Id, StringComparison.OrdinalIgnoreCase));
        if (worldEvent is null || !State.ActiveWorldEvents.Contains(worldEvent.Id, StringComparer.OrdinalIgnoreCase))
        {
            _statusMessage = "AURA // EL SISTEMA NO REQUIERE REINICIO.";
            Invalidate();
            return;
        }

        if (challenge.RequiredItemId is not null && !HasAvailableItem(challenge.RequiredItemId))
        {
            _statusMessage = $"AURA // REPARACIÓN BLOQUEADA: necesitas {GetItemName(challenge.RequiredItemId)}.";
            Invalidate();
            return;
        }

        _activeChallenge = challenge;
        _challengeStep = Math.Clamp(State.ActiveChallengeId == challenge.Id ? State.ActiveChallengeStep : 0,
            0, challenge.Sequence.Length - 1);
        State.ActiveChallengeId = challenge.Id;
        State.ActiveChallengeStep = _challengeStep;
        _statusMessage = challenge.Instruction;
        StateChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void RestoreActiveChallenge()
    {
        _activeChallenge = string.IsNullOrWhiteSpace(State.ActiveChallengeId)
            ? null
            : WorldChallengeCatalog.FindChallenge(State.ActiveChallengeId);
        if (_activeChallenge is null || State.CompletedChallenges.Contains(_activeChallenge.Id, StringComparer.OrdinalIgnoreCase))
        {
            _activeChallenge = null;
            State.ActiveChallengeId = null;
            State.ActiveChallengeStep = 0;
            return;
        }

        _challengeStep = Math.Clamp(State.ActiveChallengeStep, 0, _activeChallenge.Sequence.Length - 1);
    }

    private void BuildMap()
    {
        for (int x = 0; x < MapColumns; x++)
        for (int y = 0; y < MapRows; y++)
            _walls[x, y] = true;

        foreach (RoomSpec room in Rooms)
            CarveRoom(room.X, room.Y, room.Width, room.Height);

        // Corredor central y conectores verticales de las tres salas de cada ala.
        CarveRoom(2, 13, 34, 3);
        foreach (int column in ConnectorColumns)
        {
            CarveRoom(column, 10, 1, 3);
            CarveRoom(column, 16, 1, 4);
        }

        foreach (DoorSpec door in Doors)
            _walls[door.X, door.Y] = !IsDoorUnlocked(door);
    }

    private void CarveRoom(int x, int y, int width, int height)
    {
        for (int column = x; column < x + width; column++)
        for (int row = y; row < y + height; row++)
            _walls[column, row] = false;
    }

    private bool MoveAxis(float delta, bool horizontalAxis)
    {
        if (delta == 0)
        {
            return false;
        }

        const float maxStep = 7f;
        int steps = Math.Max(1, (int)MathF.Ceiling(MathF.Abs(delta) / maxStep));
        float step = delta / steps;
        bool moved = false;
        for (int index = 0; index < steps; index++)
        {
            float nextX = State.PlayerX + (horizontalAxis ? step : 0);
            float nextY = State.PlayerY + (horizontalAxis ? 0 : step);
            if (HitsWall(nextX, nextY))
            {
                break;
            }

            State.PlayerX = nextX;
            State.PlayerY = nextY;
            moved = true;
        }

        return moved;
    }

    private bool HitsWall(float x, float y)
    {
        var playerBounds = new RectangleF(x - PlayerRadius, y - PlayerRadius, PlayerRadius * 2, PlayerRadius * 2);
        if (_furniture.Any(piece => piece.IntersectsWith(playerBounds)))
        {
            return true;
        }

        int left = (int)MathF.Floor((x - PlayerRadius) / TileSize);
        int right = (int)MathF.Floor((x + PlayerRadius) / TileSize);
        int top = (int)MathF.Floor((y - PlayerRadius) / TileSize);
        int bottom = (int)MathF.Floor((y + PlayerRadius) / TileSize);
        for (int column = left; column <= right; column++)
        for (int row = top; row <= bottom; row++)
        {
            if (column < 0 || column >= MapColumns || row < 0 || row >= MapRows || _walls[column, row])
            {
                return true;
            }
        }

        return false;
    }

    private void DrawMap(Graphics graphics)
    {
        using var floorBrush = new SolidBrush(Color.FromArgb(13, 27, 48));
        using var wallBrush = new SolidBrush(Color.FromArgb(24, 46, 69));
        using var gridPen = new Pen(Color.FromArgb(38, 73, 98));
        using var edgePen = new Pen(Color.FromArgb(40, 171, 207), 2);
        using var innerWallPen = new Pen(Color.FromArgb(16, 34, 52), 2);
        using var lightPen = new Pen(Color.FromArgb(85, 38, 201, 235), 3);
        using var boltBrush = new SolidBrush(Color.FromArgb(119, 151, 174));
        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(125, 172, 199));

        graphics.FillRectangle(floorBrush, 0, 0, MapColumns * TileSize, MapRows * TileSize);
        foreach (RoomSpec room in Rooms)
        {
            using var roomBrush = new SolidBrush(room.FloorColor);
            FillRoom(graphics, roomBrush, room.X, room.Y, room.Width, room.Height);
        }

        int startX = Math.Max(0, (int)(_cameraX / TileSize) - 1);
        int endX = Math.Min(MapColumns, (int)((_cameraX + ClientSize.Width) / TileSize) + 2);
        int startY = Math.Max(0, (int)(_cameraY / TileSize) - 1);
        int endY = Math.Min(MapRows, (int)((_cameraY + ClientSize.Height) / TileSize) + 2);
        for (int x = startX; x < endX; x++)
        for (int y = startY; y < endY; y++)
        {
            var tile = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);
            if (_walls[x, y])
            {
                graphics.FillRectangle(wallBrush, tile);
                graphics.DrawRectangle(edgePen, tile);
                graphics.DrawRectangle(innerWallPen, tile.X + 5, tile.Y + 5, tile.Width - 10, tile.Height - 10);
                graphics.DrawLine(innerWallPen, tile.X + 7, tile.Y + tile.Height - 8,
                    tile.Right - 7, tile.Y + tile.Height - 8);
                graphics.FillEllipse(boltBrush, tile.X + 4, tile.Y + 4, 3, 3);
                graphics.FillEllipse(boltBrush, tile.Right - 7, tile.Y + 4, 3, 3);
            }
            else
            {
                string? roomId = GetRoomIdAtTile(x, y);
                Bitmap floorTexture = _floorTile;
                if (roomId is not null && _roomFloorTextures.TryGetValue(roomId, out Bitmap? texture) && texture is not null)
                    floorTexture = texture;
                graphics.DrawImageUnscaled(floorTexture, tile.Location);
                graphics.DrawRectangle(gridPen, tile);
                if ((x + y) % 5 == 0)
                {
                    graphics.DrawLine(lightPen, tile.X + 8, tile.Bottom - 5, tile.Right - 8, tile.Bottom - 5);
                }
            }
        }

        foreach (RoomSpec room in Rooms)
        {
            using var roomTitleBrush = new SolidBrush(room.AccentColor);
            DrawRoomTitle(graphics, font, roomTitleBrush, room.Title, room.X, room.Y);
        }
        DrawDoors(graphics, font);
    }

    private static Bitmap CreateFloorTexture(Color floorColor)
    {
        var texture = new Bitmap(TileSize, TileSize);
        using Graphics graphics = Graphics.FromImage(texture);
        graphics.Clear(floorColor);
        Color panelColor = Color.FromArgb(Math.Min(255, floorColor.R + 3),
            Math.Min(255, floorColor.G + 5), Math.Min(255, floorColor.B + 7));
        using var seamPen = new Pen(Color.FromArgb(90, 91, 135, 158), 1);
        using var panelBrush = new SolidBrush(panelColor);
        using var glintPen = new Pen(Color.FromArgb(20, 71, 133, 157), 1);
        graphics.DrawRectangle(seamPen, 2, 2, TileSize - 5, TileSize - 5);
        graphics.FillRectangle(panelBrush, 7, 7, TileSize - 14, TileSize - 14);
        graphics.DrawLine(glintPen, 8, 10, TileSize - 9, 10);
        graphics.DrawLine(glintPen, 8, TileSize - 10, TileSize - 9, TileSize - 10);
        using var rivetBrush = new SolidBrush(Color.FromArgb(93, 135, 156));
        graphics.FillEllipse(rivetBrush, 4, 4, 2, 2);
        graphics.FillEllipse(rivetBrush, TileSize - 6, TileSize - 6, 2, 2);
        return texture;
    }

    private void DrawDoors(Graphics graphics, Font font)
    {
        foreach (DoorSpec specification in Doors)
        {
            bool open = IsDoorUnlocked(specification);
            Rectangle door = new(specification.X * TileSize, specification.Y * TileSize, TileSize, TileSize);
            Color doorColor = open ? Color.FromArgb(29, 105, 91) : Color.FromArgb(90, 57, 53);
            using var doorBrush = new SolidBrush(doorColor);
            using var doorPen = new Pen(open ? Color.FromArgb(65, 226, 185) : Color.FromArgb(241, 151, 85), 2);
            using var insetBrush = new SolidBrush(open ? Color.FromArgb(37, 154, 127) : Color.FromArgb(120, 73, 63));
            graphics.FillRectangle(doorBrush, door);
            graphics.DrawRectangle(doorPen, door);
            Rectangle inset = new(door.X + 7, door.Y + 6, door.Width - 14, door.Height - 12);
            graphics.FillRectangle(insetBrush, inset);
            graphics.DrawRectangle(doorPen, inset);
            if (DistanceTo(specification) <= 110f)
            {
                string marker = open ? "ABIERTO" : GetDoorShortLabel(specification);
                graphics.DrawString(marker, font, Brushes.White, door.X - 15, door.Y - 20);
            }
        }
    }

    private void DrawFurniture(Graphics graphics)
    {
        using var shellBrush = new SolidBrush(Color.FromArgb(30, 54, 76));
        using var insetBrush = new SolidBrush(Color.FromArgb(8, 21, 35));
        using var edgePen = new Pen(Color.FromArgb(63, 111, 138), 1.4f);
        using var amberBrush = new SolidBrush(Color.FromArgb(232, 166, 75));
        int fixtureIndex = 0;
        foreach (RectangleF piece in _furniture)
        {
            Rectangle rect = Rectangle.Round(piece);
            int roomTileX = Math.Clamp((int)(rect.X / TileSize), 0, MapColumns - 1);
            int roomTileY = Math.Clamp((int)(rect.Y / TileSize), 0, MapRows - 1);
            RoomSpec room = GetRoomForTile(roomTileX, roomTileY) ?? Rooms[0];
            using var roomGlow = new Pen(Color.FromArgb(175, room.AccentColor), 2);
            graphics.FillRectangle(shellBrush, rect);
            graphics.DrawRectangle(edgePen, rect);
            graphics.FillRectangle(insetBrush, rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 9);
            graphics.DrawLine(roomGlow, rect.X + 7, rect.Y + 7, rect.Right - 7, rect.Y + 7);
            if (fixtureIndex % 3 == 0)
            {
                graphics.FillRectangle(Brushes.Cyan, rect.X + 8, rect.Y + 10, rect.Width - 16, 7);
                graphics.DrawLine(Pens.White, rect.X + 11, rect.Y + 13, rect.Right - 11, rect.Y + 13);
                graphics.DrawLine(edgePen, rect.X + 8, rect.Y + 21, rect.Right - 8, rect.Y + 21);
            }
            else if (fixtureIndex % 3 == 1)
            {
                for (int shelf = 1; shelf < 4; shelf++)
                    graphics.DrawLine(edgePen, rect.X + 7, rect.Y + shelf * 5, rect.Right - 7, rect.Y + shelf * 5);
                graphics.DrawLine(roomGlow, rect.X + rect.Width / 2, rect.Y + 5,
                    rect.X + rect.Width / 2, rect.Bottom - 5);
            }
            else
            {
                using var equipmentGlass = new SolidBrush(Color.FromArgb(95, room.AccentColor));
                graphics.FillEllipse(equipmentGlass, rect.X + 9, rect.Y + 8, rect.Width - 18, rect.Height - 14);
                graphics.DrawEllipse(roomGlow, rect.X + 9, rect.Y + 8, rect.Width - 18, rect.Height - 14);
            }

            for (int lamp = 0; lamp < 3; lamp++)
            {
                Brush light = lamp == 0 ? amberBrush : Brushes.Cyan;
                graphics.FillEllipse(light, rect.X + 8 + lamp * 7, rect.Bottom - 7, 3, 3);
            }
            fixtureIndex++;
        }
    }

    private void DrawAnomalyField(Graphics graphics)
    {
        float centerX = 31 * TileSize + TileSize / 2f;
        float centerY = 23 * TileSize + TileSize / 2f;
        float pulseSpeed = State.ActiveWorldEvents.Contains("anomaly_instability", StringComparer.OrdinalIgnoreCase) ? 3.2f : 1.8f;
        float pulse = 3f + MathF.Sin(_animationClock * pulseSpeed) * 3f;
        bool unstable = State.ActiveWorldEvents.Contains("anomaly_instability", StringComparer.OrdinalIgnoreCase);
        using var fieldBrush = new SolidBrush(unstable ? Color.FromArgb(55, 255, 54, 107) : Color.FromArgb(27, 233, 71, 209));
        using var outerPen = new Pen(unstable ? Color.FromArgb(180, 255, 77, 112) : Color.FromArgb(96, 245, 88, 220), 2);
        using var innerPen = new Pen(Color.FromArgb(160, 92, 226, 255), 1.5f);
        graphics.FillEllipse(fieldBrush, centerX - 42 - pulse, centerY - 42 - pulse,
            84 + pulse * 2, 84 + pulse * 2);
        graphics.DrawEllipse(outerPen, centerX - 36 - pulse, centerY - 36 - pulse,
            72 + pulse * 2, 72 + pulse * 2);
        graphics.DrawEllipse(innerPen, centerX - 22, centerY - 22, 44, 44);
        for (int ray = 0; ray < 8; ray++)
        {
            float angle = ray * MathF.PI / 4;
            graphics.DrawLine(innerPen,
                centerX + MathF.Cos(angle) * 24, centerY + MathF.Sin(angle) * 24,
                centerX + MathF.Cos(angle) * 34, centerY + MathF.Sin(angle) * 34);
        }
    }

    private void DrawSystemEventEffects(Graphics graphics)
    {
        if (State.ActiveWorldEvents.Contains("communications_blackout", StringComparer.OrdinalIgnoreCase))
        {
            RoomSpec room = GetRoom("communications");
            using var blackoutBrush = new SolidBrush(Color.FromArgb(105, 0, 4, 14));
            using var alarmPen = new Pen(Color.FromArgb(150, 244, 86, 82), 2);
            FillRoom(graphics, blackoutBrush, room.X, room.Y, room.Width, room.Height);
            float pulse = 0.5f + 0.5f * MathF.Sin(_animationClock * 5f);
            alarmPen.Color = Color.FromArgb((int)(85 + pulse * 150), 244, 86, 82);
            graphics.DrawLine(alarmPen, room.X * TileSize + 12, room.Y * TileSize + 36,
                (room.X + room.Width) * TileSize - 12, room.Y * TileSize + 36);
        }

        if (State.ActiveWorldEvents.Contains("lab_power_surge", StringComparer.OrdinalIgnoreCase))
        {
            float alpha = 0.5f + 0.5f * MathF.Sin(_animationClock * 9f);
            using var surgeBrush = new SolidBrush(Color.FromArgb((int)(40 + alpha * 95), 255, 151, 62));
            float x = 34 * TileSize + TileSize / 2f;
            float y = 7 * TileSize + TileSize / 2f;
            graphics.FillEllipse(surgeBrush, x - 24, y - 24, 48, 48);
            using var sparkPen = new Pen(Color.FromArgb(170, 255, 200, 118), 2);
            graphics.DrawLine(sparkPen, x - 14, y - 8, x - 3, y + 2);
            graphics.DrawLine(sparkPen, x + 10, y - 12, x + 1, y - 1);
        }

        if (State.ActiveWorldEvents.Contains("life_support_failure", StringComparer.OrdinalIgnoreCase))
        {
            using var alertPen = new Pen(Color.FromArgb(145, 255, 82, 91), 2);
            float x = 4 * TileSize + TileSize / 2f;
            float y = 6 * TileSize + TileSize / 2f;
            graphics.DrawEllipse(alertPen, x - 20, y - 20, 40, 40);
        }

        if (State.ActiveWorldEvents.Contains("research_lockdown", StringComparer.OrdinalIgnoreCase))
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_animationClock * 4f);
            using var lockdownPen = new Pen(Color.FromArgb((int)(90 + pulse * 150), 255, 111, 84), 2);
            float x = 22 * TileSize + TileSize / 2f;
            float y = 24 * TileSize + TileSize / 2f;
            graphics.DrawRectangle(lockdownPen, x - 22, y - 20, 44, 40);
        }
    }

    private static void FillRoom(Graphics graphics, Brush brush, int x, int y, int width, int height) =>
        graphics.FillRectangle(brush, x * TileSize, y * TileSize, width * TileSize, height * TileSize);

    private static void DrawRoomTitle(Graphics graphics, Font font, Brush brush, string text, int x, int y) =>
        graphics.DrawString(text, font, brush, x * TileSize + 10, y * TileSize + 9);

    private void DrawObjects(Graphics graphics)
    {
        using var markerBrush = new SolidBrush(Color.FromArgb(52, 220, 255));
        using var doneBrush = new SolidBrush(Color.FromArgb(93, 225, 167));
        using var markerPen = new Pen(Color.FromArgb(150, 52, 220, 255), 2);
        using var labelFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(208, 232, 247));
        using var objectGlow = new SolidBrush(Color.FromArgb(24, 52, 220, 255));
        using var interactionPen = new Pen(Color.FromArgb(155, 93, 238, 255), 1.5f);
        float pulse = 3 + MathF.Sin(_animationClock * 4) * 2;

        foreach (WorldObject item in _objects)
        {
            if (State.CollectedItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            float x = item.X * TileSize + TileSize / 2f;
            float y = item.Y * TileSize + TileSize / 2f;
            if (item.IsSecret && DistanceTo(item) > 180f)
                continue;

            bool objective = item.Kind is "objective" or "challenge" ||
                item.Id is "access_card" or "power_cell" or "spare_fuse";
            RectangleF glow = new(x - 17 - pulse, y - 17 - pulse, 34 + pulse * 2, 34 + pulse * 2);
            graphics.FillEllipse(objectGlow, glow);
            DrawInteractable(graphics, item.Id, x, y, objective ? markerBrush : doneBrush, markerPen);
            if (!item.IsSecret || DistanceTo(item) <= 100f)
                graphics.DrawString(item.Title, labelFont, labelBrush, x - 58, y + 26);
            if (DistanceTo(item) <= 94f)
            {
                graphics.DrawEllipse(interactionPen, x - 21, y - 21, 42, 42);
            }
        }

    }

    private static void DrawInteractable(Graphics graphics, string id, float x, float y, Brush markerBrush, Pen markerPen)
    {
        using var shell = new SolidBrush(Color.FromArgb(28, 48, 67));
        using var trim = new Pen(Color.FromArgb(111, 197, 217), 1.5f);
        switch (id)
        {
            case "signal_terminal":
            case "security_console":
            case "anomaly_terminal":
            case "life_support_reset":
            case "communications_restore":
            case "generator_repair":
            case "security_override":
            case "anomaly_stabilize":
                RectangleF console = new(x - 13, y - 13, 26, 25);
                graphics.FillRectangle(shell, console);
                graphics.DrawRectangle(trim, console.X, console.Y, console.Width, console.Height);
                graphics.FillRectangle(markerBrush, x - 9, y - 10, 18, 11);
                graphics.DrawLine(Pens.Cyan, x - 7, y - 7, x + 6, y - 7);
                graphics.DrawLine(Pens.Cyan, x - 6, y - 3, x + 4, y - 3);
                graphics.FillRectangle(shell, x - 3, y + 12, 6, 4);
                break;
            case "ship_log":
            case "manifest_document":
            case "communications_log":
            case "lab_document":
            case "research_log":
            case "anomaly_record":
                graphics.FillRectangle(shell, x - 12, y - 9, 24, 18);
                graphics.DrawRectangle(trim, x - 12, y - 9, 24, 18);
                graphics.FillRectangle(markerBrush, x - 8, y - 5, 16, 10);
                graphics.DrawLine(Pens.White, x - 5, y - 2, x + 4, y - 2);
                graphics.DrawLine(Pens.White, x - 5, y + 1, x + 5, y + 1);
                break;
            case "secret_archive":
                graphics.FillRectangle(shell, x - 11, y - 11, 22, 22);
                graphics.DrawRectangle(trim, x - 11, y - 11, 22, 22);
                graphics.FillEllipse(markerBrush, x - 5, y - 5, 10, 10);
                break;
            case "access_card":
                graphics.FillRectangle(markerBrush, x - 12, y - 7, 24, 14);
                graphics.DrawRectangle(markerPen, x - 12, y - 7, 24, 14);
                graphics.FillRectangle(shell, x + 2, y - 3, 6, 2);
                break;
            case "spare_fuse":
                graphics.FillRectangle(shell, x - 9, y - 10, 18, 20);
                graphics.DrawRectangle(markerPen, x - 9, y - 10, 18, 20);
                graphics.DrawLine(trim, x - 5, y - 5, x + 5, y - 5);
                graphics.DrawLine(trim, x - 5, y + 5, x + 5, y + 5);
                break;
            case "oxygen_cell":
            case "power_cell":
                graphics.FillRectangle(shell, x - 8, y - 12, 16, 24);
                graphics.DrawRectangle(markerPen, x - 8, y - 12, 16, 24);
                graphics.FillRectangle(markerBrush, x - 4, y - 7, 8, 14);
                break;
            case "toolkit":
                graphics.FillRectangle(shell, x - 11, y - 8, 22, 16);
                graphics.DrawRectangle(markerPen, x - 11, y - 8, 22, 16);
                graphics.DrawLine(trim, x - 5, y - 8, x - 2, y - 12);
                graphics.DrawLine(trim, x + 5, y - 8, x + 2, y - 12);
                break;
            default:
                graphics.FillEllipse(markerBrush, x - 9, y - 9, 18, 18);
                graphics.DrawEllipse(markerPen, x - 12, y - 12, 24, 24);
                break;
        }
    }

    private void DrawPlayer(Graphics graphics)
    {
        float x = State.PlayerX;
        float y = State.PlayerY;
        using var glowBrush = new SolidBrush(Color.FromArgb(55, 40, 224, 255));
        graphics.FillEllipse(glowBrush, x - 22, y - 22, 44, 44);
        int frame = _isMoving ? (int)(_animationClock * 9) % 3 : 0;
        Bitmap sprite = _sprites.Get(_facing, _isMoving, frame, _interactionTimer > 0);
        graphics.DrawImage(sprite, x - sprite.Width / 2f, y - sprite.Height / 2f, sprite.Width, sprite.Height);
    }

    private void DrawOverlay(Graphics graphics)
    {
        using var panelBrush = new SolidBrush(Color.FromArgb(220, 7, 18, 34));
        using var borderPen = new Pen(Color.FromArgb(40, 190, 225), 1);
        using var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var textFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);
        using var titleBrush = new SolidBrush(NexusTheme.Accent);
        using var textBrush = new SolidBrush(NexusTheme.Text);

        Rectangle status = new(12, 12, Math.Max(120, Math.Min(ClientSize.Width - 24,
            ClientSize.Width >= 590 ? 322 : ClientSize.Width - 24)), 66);
        graphics.FillRectangle(panelBrush, status);
        graphics.DrawRectangle(borderPen, status);
        graphics.DrawString(CurrentRoomName(), titleFont, titleBrush, status.X + 10, status.Y + 7);
        graphics.DrawString(_statusMessage, textFont, textBrush,
            new RectangleF(status.X + 10, status.Y + 28, status.Width - 20, 32));

        DrawGameHud(graphics, panelBrush, borderPen, titleFont, textFont, titleBrush, textBrush);

        string prompt = GetPrompt();
        Rectangle help = new(12, Math.Max(12, ClientSize.Height - 35), ClientSize.Width - 24, 23);
        graphics.FillRectangle(panelBrush, help);
        graphics.DrawRectangle(borderPen, help);
        graphics.DrawString("W A S D / FLECHAS: MOVER     E: INTERACTUAR     J: OBJETIVOS     " + prompt,
            textFont, textBrush, help.X + 8, help.Y + 4);

        graphics.DrawString("I: INVENTARIO", textFont, titleBrush, help.Right - 95, help.Y + 4);
        if (_inventoryOpen)
            DrawInventory(graphics, panelBrush, borderPen, titleFont, textFont, titleBrush, textBrush);
        else if (_missionLogOpen)
            DrawMissionLog(graphics, panelBrush, borderPen, titleFont, textFont, titleBrush, textBrush);
        else if (_activeChallenge is not null)
            DrawChallenge(graphics, panelBrush, borderPen, titleFont, textFont, titleBrush, textBrush);
        if (_activeDialogue is not null)
            DrawDialogue(graphics, panelBrush, borderPen, titleFont, textFont, titleBrush, textBrush);
    }

    private void DrawChallenge(Graphics graphics, Brush panelBrush, Pen borderPen, Font titleFont,
        Font textFont, Brush titleBrush, Brush textBrush)
    {
        if (_activeChallenge is null) return;
        Rectangle panel = GetChallengePanelBounds();
        graphics.FillRectangle(panelBrush, panel);
        graphics.DrawRectangle(borderPen, panel);
        graphics.DrawString($"AURA // {_activeChallenge.Title}", titleFont, titleBrush, panel.X + 18, panel.Y + 14);
        graphics.DrawString(_activeChallenge.Instruction, textFont, textBrush,
            new RectangleF(panel.X + 18, panel.Y + 44, panel.Width - 36, 44));
        graphics.DrawString($"SECUENCIA: {_challengeStep}/{_activeChallenge.Sequence.Length}", textFont,
            titleBrush, panel.X + 18, panel.Y + 91);
        for (int index = 0; index < 4; index++)
        {
            Rectangle key = GetChallengeKeyBounds(panel, index);
            using var keyBrush = new SolidBrush(Color.FromArgb(50, 22, 118, 151));
            graphics.FillRectangle(keyBrush, key);
            graphics.DrawRectangle(borderPen, key);
            using var keyFont = new Font("Segoe UI", 13f, FontStyle.Bold);
            graphics.DrawString((index + 1).ToString(), keyFont, titleBrush, key.X + key.Width / 2 - 5,
                key.Y + 5);
        }
        graphics.DrawString("PULSA 1–4 O HAZ CLIC EN UN NÚMERO", textFont, textBrush,
            panel.X + 18, panel.Bottom - 20);
    }

    private static Rectangle GetChallengeKeyBounds(Rectangle panel, int index)
    {
        int availableWidth = panel.Width - 44;
        int gap = 10;
        int keyWidth = Math.Max(42, (availableWidth - gap * 3) / 4);
        int totalWidth = keyWidth * 4 + gap * 3;
        int startX = panel.X + (panel.Width - totalWidth) / 2;
        return new Rectangle(startX + index * (keyWidth + gap), panel.Bottom - 68, keyWidth, 36);
    }

    private Rectangle GetChallengePanelBounds()
    {
        int width = Math.Max(280, Math.Min(590, ClientSize.Width - 32));
        int safeWidth = Math.Min(width, Math.Max(1, ClientSize.Width - 16));
        int height = 208;
        int x = (ClientSize.Width - safeWidth) / 2;
        int y = Math.Max(16, ClientSize.Height - height - 48);
        return new Rectangle(x, y, safeWidth, Math.Min(height, Math.Max(120, ClientSize.Height - y - 42)));
    }

    private void DrawDialogue(Graphics graphics, Brush panelBrush, Pen borderPen, Font titleFont,
        Font textFont, Brush titleBrush, Brush textBrush)
    {
        if (_activeDialogue is null) return;
        Rectangle panel = GetDialoguePanelBounds();
        graphics.FillRectangle(panelBrush, panel);
        graphics.DrawRectangle(borderPen, panel);
        graphics.DrawString(_activeDialogue.Title, titleFont, titleBrush, panel.X + 18, panel.Y + 12);
        if (_dialogueLineIndex < _activeDialogue.Lines.Count)
        {
            NarrativeLine line = _activeDialogue.Lines[_dialogueLineIndex];
            using var speakerFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            graphics.DrawString(line.Speaker, speakerFont, titleBrush, panel.X + 18, panel.Y + 42);
            var textBounds = new RectangleF(panel.X + 18, panel.Y + 66, panel.Width - 36, 92);
            graphics.DrawString(line.Text, textFont, textBrush, textBounds);
            graphics.DrawString("E / ENTER: CONTINUAR", textFont, titleBrush, panel.X + 18, panel.Bottom - 30);
            return;
        }

        graphics.DrawString("ELIGE TU RESPUESTA", textFont, titleBrush, panel.X + 18, panel.Y + 42);
        int optionY = panel.Bottom - 82;
        for (int index = 0; index < _activeDialogue.Choices.Count; index++)
        {
            Rectangle optionBounds = new(panel.X + 24, optionY + index * 36, panel.Width - 48, 30);
            using var optionBrush = new SolidBrush(index == _dialogueChoiceIndex
                ? Color.FromArgb(70, 32, 145, 173)
                : Color.FromArgb(42, 22, 55, 76));
            graphics.FillRectangle(optionBrush, optionBounds);
            graphics.DrawRectangle(borderPen, optionBounds);
            graphics.DrawString($"[{index + 1}]  {_activeDialogue.Choices[index].Text}", textFont,
                textBrush, optionBounds.X + 8, optionBounds.Y + 7);
        }
        graphics.DrawString("A / D, ← / →: SELECCIONAR   E / ENTER: CONFIRMAR   1 / 2", textFont, titleBrush,
            panel.X + 18, panel.Y + 68);
    }

    private void DrawMissionLog(Graphics graphics, Brush panelBrush, Pen borderPen, Font titleFont,
        Font textFont, Brush titleBrush, Brush textBrush)
    {
        int width = Math.Min(650, ClientSize.Width - 32);
        int height = Math.Min(500, ClientSize.Height - 70);
        var panel = new Rectangle((ClientSize.Width - width) / 2, (ClientSize.Height - height) / 2,
            width, height);
        graphics.FillRectangle(panelBrush, panel);
        graphics.DrawRectangle(borderPen, panel);
        graphics.DrawString("REGISTRO DE MISIÓN // J PARA CERRAR", titleFont, titleBrush,
            panel.X + 18, panel.Y + 14);
        IReadOnlyList<NarrativeObjective> objectives = NexusNarrative.GetObjectives(State);
        NarrativeObjective[] mainObjectives = objectives.Where(item => !item.IsSecondary).ToArray();
        NarrativeObjective[] sideObjectives = objectives.Where(item => item.IsSecondary).ToArray();
        int columnWidth = (panel.Width - 54) / 2;
        for (int column = 0; column < 2; column++)
        {
            NarrativeObjective[] list = column == 0 ? mainObjectives : sideObjectives;
            int x = panel.X + 18 + column * (columnWidth + 18);
            graphics.DrawString(column == 0 ? "OBJETIVOS PRINCIPALES" : "OBJETIVOS SECUNDARIOS",
                textFont, titleBrush, x, panel.Y + 42);
            for (int index = 0; index < list.Length; index++)
            {
                NarrativeObjective objective = list[index];
                string marker = objective.IsComplete ? "[✓]" : "[ ]";
                Color color = objective.IsComplete ? Color.FromArgb(103, 228, 177) : NexusTheme.Text;
                using var objectiveBrush = new SolidBrush(color);
                int y = panel.Y + 68 + index * 30;
                graphics.DrawString($"{marker} {objective.Title}", textFont, objectiveBrush,
                    new RectangleF(x, y, columnWidth, 19));
                using var roomFont = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                graphics.DrawString(objective.Room, roomFont, textBrush, x + 20, y + 15);
            }
        }
        graphics.DrawString("Los documentos y las decisiones quedan guardados con la expedición.",
            textFont, titleBrush, panel.X + 18, panel.Bottom - 30);
    }

    private Rectangle GetDialoguePanelBounds()
    {
        int width = Math.Max(280, Math.Min(720, ClientSize.Width - 32));
        const int height = 236;
        int safeWidth = Math.Min(width, Math.Max(1, ClientSize.Width - 16));
        int x = (ClientSize.Width - safeWidth) / 2;
        int y = Math.Max(16, ClientSize.Height - height - 48);
        return new Rectangle(x, y, safeWidth, Math.Min(height, Math.Max(120, ClientSize.Height - y - 42)));
    }

    private void DrawGameHud(Graphics graphics, Brush panelBrush, Pen borderPen, Font titleFont,
        Font textFont, Brush titleBrush, Brush textBrush)
    {
        if (ClientSize.Width < 590)
        {
            return;
        }

        int x = ClientSize.Width - 184;
        var hud = new Rectangle(x, 12, 172, 154);
        graphics.FillRectangle(panelBrush, hud);
        graphics.DrawRectangle(borderPen, hud);
        graphics.DrawString("NEXUS // EXPLORACIÓN", titleFont, titleBrush, x + 8, 18);
        DrawMiniBar(graphics, x + 8, 42, "O₂", _oxygen, Color.FromArgb(55, 213, 247), textFont, textBrush);
        DrawMiniBar(graphics, x + 8, 62, "CASCO", _hull, Color.FromArgb(86, 177, 255), textFont, textBrush);
        DrawMiniBar(graphics, x + 8, 82, "DATOS", _data, Color.FromArgb(96, 236, 187), textFont, textBrush);

        string objective = State.CompletedObjectives.Contains("anomaly_terminal", StringComparer.OrdinalIgnoreCase)
            ? "OBJ: EXPEDICIÓN COMPLETA"
            : State.CompletedChallenges.Contains("life_support_reset", StringComparer.OrdinalIgnoreCase)
                ? GetNextObjectiveLabel()
                : "OBJ: REPARAR SOPORTE VITAL";
        graphics.DrawString(objective, textFont, textBrush, x + 8, 106);
        List<InventoryEntry> inventoryItems = GetInventoryItems();
        string inventory = inventoryItems.Count == 0
            ? "INV: VACÍO"
            : $"INV: {inventoryItems[0].ShortName}{(inventoryItems.Count > 1 ? $" +{inventoryItems.Count - 1}" : string.Empty)}";
        graphics.DrawString(inventory, textFont, textBrush, x + 8, 126);
        int discoveredRooms = Rooms.Count(room => State.ExploredRooms.Contains(room.Id, StringComparer.OrdinalIgnoreCase));
        graphics.DrawString($"ZONAS: {discoveredRooms}/{Rooms.Length}", textFont, titleBrush, x + 8, 145);

        var map = new Rectangle(ClientSize.Width - 176, 176, 164, 105);
        graphics.FillRectangle(panelBrush, map);
        graphics.DrawRectangle(borderPen, map);
        DrawMiniMap(graphics, map);
    }

    private string GetNextObjectiveLabel()
    {
        if (!State.CollectedItems.Contains("access_card", StringComparer.OrdinalIgnoreCase)) return "OBJ: TARJETA DE ACCESO";
        if (!State.CollectedItems.Contains("spare_fuse", StringComparer.OrdinalIgnoreCase)) return "OBJ: FUSIBLE EN ALMACÉN";
        if (!State.CompletedChallenges.Contains("communications_restore", StringComparer.OrdinalIgnoreCase)) return "OBJ: RESTAURAR COMMS";
        if (!State.CompletedObjectives.Contains("signal_terminal", StringComparer.OrdinalIgnoreCase)) return "OBJ: DESCIFRAR SEÑAL";
        if (!State.CompletedChallenges.Contains("generator_repair", StringComparer.OrdinalIgnoreCase)) return "OBJ: REPARAR GENERADOR";
        if (!State.CollectedItems.Contains("power_cell", StringComparer.OrdinalIgnoreCase)) return "OBJ: CÉLULA EN LABORATORIO";
        if (!State.CompletedChallenges.Contains("security_override", StringComparer.OrdinalIgnoreCase)) return "OBJ: ANULAR SEGURIDAD";
        if (!State.CompletedObjectives.Contains("security_console", StringComparer.OrdinalIgnoreCase)) return "OBJ: VALIDAR CONSOLA";
        if (!State.CompletedChallenges.Contains("anomaly_stabilize", StringComparer.OrdinalIgnoreCase)) return "OBJ: ESTABILIZAR NÚCLEO";
        return "OBJ: ASEGURAR DATOS";
    }

    private static void DrawMiniBar(Graphics graphics, int x, int y, string label, int value,
        Color color, Font font, Brush textBrush)
    {
        graphics.DrawString(label, font, textBrush, x, y);
        int barX = x + 48;
        using var track = new SolidBrush(Color.FromArgb(37, 57, 75));
        using var fill = new SolidBrush(color);
        graphics.FillRectangle(track, barX, y + 3, 74, 8);
        graphics.FillRectangle(fill, barX, y + 3, (int)(74 * value / 100f), 8);
        graphics.DrawString($"{value}%", font, textBrush, barX + 78, y);
    }

    private void DrawMiniMap(Graphics graphics, Rectangle bounds)
    {
        int mapX = bounds.X + 8;
        int mapY = bounds.Y + 7;
        float scaleX = (bounds.Width - 16f) / MapColumns;
        float scaleY = (bounds.Height - 14f) / MapRows;
        using var roomBrush = new SolidBrush(Color.FromArgb(48, 88, 111));
        using var hallBrush = new SolidBrush(Color.FromArgb(23, 46, 64));
        using var playerBrush = new SolidBrush(Color.FromArgb(75, 231, 255));
        using var discoveredPen = new Pen(Color.FromArgb(92, 169, 205), 1);
        foreach (RoomSpec room in Rooms)
        {
            if (!State.ExploredRooms.Contains(room.Id, StringComparer.OrdinalIgnoreCase)) continue;
            var roomRect = new Rectangle((int)(mapX + room.X * scaleX), (int)(mapY + room.Y * scaleY),
                Math.Max(2, (int)(room.Width * scaleX)), Math.Max(2, (int)(room.Height * scaleY)));
            graphics.FillRectangle(roomBrush, roomRect);
            graphics.DrawRectangle(discoveredPen, roomRect);
        }

        if (State.ExploredRooms.Contains("central_corridor", StringComparer.OrdinalIgnoreCase))
        {
            graphics.FillRectangle(hallBrush, mapX + 2 * scaleX, mapY + 13 * scaleY,
                34 * scaleX, 3 * scaleY);
            foreach (int column in ConnectorColumns)
            {
                graphics.FillRectangle(hallBrush, mapX + column * scaleX, mapY + 10 * scaleY,
                    scaleX, 3 * scaleY);
                graphics.FillRectangle(hallBrush, mapX + column * scaleX, mapY + 16 * scaleY,
                    scaleX, 4 * scaleY);
            }
        }

        foreach (DoorSpec door in Doors)
        {
            bool known = State.ExploredRooms.Contains(door.RoomId, StringComparer.OrdinalIgnoreCase) ||
                State.ExploredRooms.Contains("central_corridor", StringComparer.OrdinalIgnoreCase);
            if (!known) continue;
            using var doorBrush = new SolidBrush(IsDoorUnlocked(door)
                ? Color.FromArgb(73, 231, 195)
                : Color.FromArgb(255, 170, 86));
            graphics.FillRectangle(doorBrush, mapX + door.X * scaleX - 2,
                mapY + door.Y * scaleY - 2, 5, 5);
        }

        foreach (WorldObject objective in _objects.Where(item => item.Kind is "objective" or "challenge" ||
                     item.Id is "access_card" or "power_cell" or "spare_fuse"))
        {
            string roomId = GetRoomIdAtTile(objective.X, objective.Y) ?? string.Empty;
            if (!State.ExploredRooms.Contains(roomId, StringComparer.OrdinalIgnoreCase) ||
                State.CollectedItems.Contains(objective.Id, StringComparer.OrdinalIgnoreCase)) continue;
            graphics.FillEllipse(Brushes.Cyan, mapX + objective.X * scaleX - 1.5f,
                mapY + objective.Y * scaleY - 1.5f, 4, 4);
        }

        graphics.FillEllipse(playerBrush, mapX + State.PlayerX / TileSize * scaleX - 3,
            mapY + State.PlayerY / TileSize * scaleY - 3, 6, 6);
    }

    private void DrawInventory(Graphics graphics, Brush panelBrush, Pen borderPen, Font titleFont,
        Font textFont, Brush titleBrush, Brush textBrush)
    {
        var panel = new Rectangle(ClientSize.Width / 2 - 185, ClientSize.Height / 2 - 125, 370, 250);
        graphics.FillRectangle(panelBrush, panel);
        graphics.DrawRectangle(borderPen, panel);
        graphics.DrawString("INVENTARIO DE EXPLORACIÓN", titleFont, titleBrush, panel.X + 14, panel.Y + 12);
        List<InventoryEntry> items = GetInventoryItems();
        if (items.Count == 0)
        {
            graphics.DrawString("No llevas objetos. Explora las salas y pulsa E junto a los marcadores.",
                textFont, textBrush, new RectangleF(panel.X + 16, panel.Y + 54, panel.Width - 32, 50));
        }
        else
        {
            int firstVisible = Math.Clamp(_inventorySelection - 3, 0, Math.Max(0, items.Count - 7));
            for (int row = 0; row < 7 && firstVisible + row < items.Count; row++)
            {
                int index = firstVisible + row;
                InventoryEntry item = items[index];
                int y = panel.Y + 42 + row * 22;
                bool selected = index == _inventorySelection;
                if (selected)
                {
                    using var selectionBrush = new SolidBrush(Color.FromArgb(72, 31, 135, 170));
                    graphics.FillRectangle(selectionBrush, panel.X + 10, y - 2, panel.Width - 20, 21);
                }
                string status = State.ReadDocuments.Contains(item.Id, StringComparer.OrdinalIgnoreCase) ? "LEÍDO" : item.Category;
                graphics.DrawString($"{(selected ? "›" : "•")}  {item.Name}", textFont,
                    selected ? titleBrush : textBrush, panel.X + 16, y);
                graphics.DrawString(status, textFont, textBrush, panel.Right - 82, y);
            }
        }

        graphics.DrawString("I CERRAR     ↑ / ↓ SELECCIONAR     E USAR / LEER",
            textFont, textBrush, panel.X + 14, panel.Bottom - 24);
    }

    private List<InventoryEntry> GetInventoryItems()
    {
        string[] order =
        {
            "access_card", "power_cell", "oxygen_cell", "toolkit", "spare_fuse", "secret_datacube",
            "ship_log", "manifest_document", "communications_log", "lab_document", "research_log", "anomaly_record"
        };
        return order
            .Where(id => State.CollectedItems.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Where(id => !State.UsedItems.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Select(id => new InventoryEntry(id, GetItemName(id), GetItemCategory(id)))
            .ToList();
    }

    private static string GetItemName(string id) => id switch
    {
        "access_card" => "Tarjeta de acceso",
        "power_cell" => "Célula de energía",
        "oxygen_cell" => "Célula de oxígeno",
        "toolkit" => "Kit de reparación",
        "spare_fuse" => "Fusible auxiliar",
        "secret_datacube" => "Cubo de datos secreto",
        "ship_log" => "Bitácora del puente",
        "manifest_document" => "Manifiesto de carga",
        "communications_log" => "Registro de transmisiones",
        "lab_document" => "Informe de laboratorio",
        "research_log" => "Cuaderno de investigación",
        "anomaly_record" => "Eco de la anomalía",
        _ => id
    };

    private static string GetItemCategory(string id) => id switch
    {
        "access_card" => "ACCESO",
        "power_cell" or "oxygen_cell" => "ENERGÍA",
        "toolkit" => "HERRAMIENTA",
        "spare_fuse" => "REPUESTO",
        "secret_datacube" => "ESPECIAL",
        _ => "DOCUMENTO"
    };

    private static bool IsDocument(string id) => id is "ship_log" or "manifest_document" or
        "communications_log" or "lab_document" or "research_log" or "anomaly_record";

    private static string GetDocumentText(string id) => id switch
    {
        "ship_log" => "BITÁCORA: NEXUS entró en órbita de Eridani-9 tras recibir una señal persistente.",
        "manifest_document" => "MANIFIESTO: el almacén recibió células, herramientas y tarjetas antes del incidente.",
        "communications_log" => "TRANSMISIÓN: la señal cambia cuando la nave responde. No es un eco automático.",
        "lab_document" => "INFORME: el campo de la anomalía reacciona a pulsos eléctricos controlados.",
        "research_log" => "INVESTIGACIÓN: la consola debe sincronizar la señal y mantener energía estable.",
        "anomaly_record" => "ECO: el patrón luminoso parece responder a la presencia de la tripulación.",
        _ => "Documento sin datos legibles."
    };

    private string GetPrompt()
    {
        DoorSpec? lockedDoor = Doors
            .Where(IsDoorClosed)
            .OrderBy(DistanceTo)
            .FirstOrDefault();
        if (lockedDoor is not null && DistanceTo(lockedDoor) <= 78f)
            return $"E: ABRIR {GetRoom(lockedDoor.RoomId).Title}";

        WorldObject? nearby = _objects
            .Where(item => !State.CollectedItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
            .Where(item => !item.IsSecret || DistanceTo(item) <= 180f)
            .OrderBy(DistanceTo)
            .FirstOrDefault();
        if (nearby is not null && DistanceTo(nearby) <= 68f)
        {
            return $"E: {nearby.Title}";
        }
        return string.Empty;
    }

    private string CurrentRoomName()
    {
        int tileX = (int)(State.PlayerX / TileSize);
        int tileY = (int)(State.PlayerY / TileSize);
        string? roomId = GetRoomIdAtTile(tileX, tileY);
        return roomId == "central_corridor" ? "CORREDOR CENTRAL" :
            roomId is null ? "INSTALACIÓN NEXUS" : GetRoom(roomId).Title;
    }

    private bool DiscoverCurrentRoom()
    {
        string? roomId = GetRoomIdAtTile((int)(State.PlayerX / TileSize), (int)(State.PlayerY / TileSize));
        if (roomId is null) return false;
        bool changed = false;
        if (!State.ExploredRooms.Contains(roomId, StringComparer.OrdinalIgnoreCase))
        {
            State.ExploredRooms.Add(roomId);
            _statusMessage = roomId == "central_corridor"
                ? "CORREDOR CENTRAL CARTOGRAFIADO. Las salidas aparecen en el minimapa."
                : $"Nueva zona cartografiada: {GetRoom(roomId).Title}.";
            changed = true;
        }

        WorldEventDefinition? roomEvent = WorldChallengeCatalog.EventForRoom(roomId);
        if (roomEvent is not null && !State.TriggeredWorldEvents.Contains(roomEvent.Id, StringComparer.OrdinalIgnoreCase))
        {
            AddUnique(State.TriggeredWorldEvents, roomEvent.Id);
            AddUnique(State.ActiveWorldEvents, roomEvent.Id);
            State.WorldEventClocks[roomEvent.Id] = 0;
            _statusMessage = roomEvent.AlertText;
            changed = true;
        }

        return changed;
    }

    private float DistanceTo(WorldObject item) => DistanceTo(item.X, item.Y);

    private float DistanceTo(DoorSpec door) => DistanceTo(door.X, door.Y);

    private float DistanceTo(int tileX, int tileY)
    {
        float x = tileX * TileSize + TileSize / 2f;
        float y = tileY * TileSize + TileSize / 2f;
        float dx = State.PlayerX - x;
        float dy = State.PlayerY - y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private void CompleteObjective(string id)
    {
        if (!State.CompletedObjectives.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            State.CompletedObjectives.Add(id);
        }
    }

    private bool IsDoorUnlocked(DoorSpec door)
    {
        if (door.StartsUnlocked || _unlockedDoors.Contains(door.Id) ||
            State.UnlockedDoors.Contains(door.Id, StringComparer.OrdinalIgnoreCase)) return true;
        return door.Id == "anomaly_access" && State.SecurityDoorUnlocked;
    }

    private void ApplySavedDoorState()
    {
        foreach (DoorSpec door in Doors)
        {
            if (!IsDoorUnlocked(door)) continue;
            AddUnique(State.UnlockedDoors, door.Id);
            _unlockedDoors.Add(door.Id);
            _walls[door.X, door.Y] = false;
        }
        State.SecurityDoorUnlocked = IsDoorUnlocked(Doors.Single(door => door.Id == "anomaly_access"));
    }

    private void UnlockDoor(DoorSpec door)
    {
        AddUnique(State.UnlockedDoors, door.Id);
        _unlockedDoors.Add(door.Id);
        _walls[door.X, door.Y] = false;
        if (door.Id == "anomaly_access") State.SecurityDoorUnlocked = true;
    }

    private bool IsDoorClosed(DoorSpec door) => !IsDoorUnlocked(door);

    private bool DoorRequirementMet(DoorSpec door)
    {
        bool itemRequirement = door.RequiredItemId is null || HasAvailableItem(door.RequiredItemId);
        bool objectiveRequirement = door.RequiredObjectiveId is null ||
            State.CompletedObjectives.Contains(door.RequiredObjectiveId, StringComparer.OrdinalIgnoreCase);
        return itemRequirement && objectiveRequirement;
    }

    private bool HasAvailableItem(string itemId) =>
        State.CollectedItems.Contains(itemId, StringComparer.OrdinalIgnoreCase) &&
        !State.UsedItems.Contains(itemId, StringComparer.OrdinalIgnoreCase);

    private string DoorRequirementMessage(DoorSpec door)
    {
        if (door.RequiredItemId is "access_card")
            return "PUERTA BLOQUEADA: busca la tarjeta de acceso en el almacén.";
        if (door.RequiredItemId is "power_cell")
            return "PUERTA BLOQUEADA: célula de energía requerida. Busca el laboratorio principal; se abre con la tarjeta del almacén.";
        if (door.RequiredObjectiveId is "security_console")
            return "PUERTA BLOQUEADA: valida la consola de investigación primero.";
        return "PUERTA BLOQUEADA: faltan credenciales para este acceso.";
    }

    private static string GetDoorShortLabel(DoorSpec door) => door.RequiredItemId switch
    {
        "access_card" => "TARJETA",
        "power_cell" => "ENERGÍA",
        _ when door.RequiredObjectiveId is not null => "SEGURIDAD",
        _ => "ACCESO"
    };

    private static RoomSpec GetRoom(string id) => Rooms.First(room => room.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static RoomSpec? GetRoomForTile(int x, int y) => Rooms.FirstOrDefault(room =>
        x >= room.X && x < room.X + room.Width && y >= room.Y && y < room.Y + room.Height);

    private static string? GetRoomIdAtTile(int x, int y)
    {
        RoomSpec? room = GetRoomForTile(x, y);
        if (room is not null) return room.Id;
        if (y is >= 13 and <= 15 && x is >= 2 and < 36) return "central_corridor";
        foreach (int column in ConnectorColumns)
        {
            if (x != column) continue;
            DoorSpec? connector = Doors.FirstOrDefault(door => door.X == column &&
                ((y is >= 9 and <= 12 && door.Y == 9) || (y is >= 16 and <= 20 && door.Y == 20)));
            if (connector is not null) return connector.RoomId;
        }
        return null;
    }

    private void NormalizeState()
    {
        State.CollectedItems ??= new List<string>();
        State.ExploredRooms ??= new List<string>();
        State.CompletedObjectives ??= new List<string>();
        State.UnlockedDoors ??= new List<string>();
        State.UsedItems ??= new List<string>();
        State.ReadDocuments ??= new List<string>();
        State.FoundSecrets ??= new List<string>();
        State.SeenDialogues ??= new List<string>();
        State.NarrativeDecisions ??= new List<string>();
        State.TriggeredWorldEvents ??= new List<string>();
        State.ActiveWorldEvents ??= new List<string>();
        State.CompletedWorldEvents ??= new List<string>();
        State.CompletedChallenges ??= new List<string>();
        State.WorldEventClocks ??= new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        // Translate names and the former security-door flag from earlier saved JSON snapshots.
        var legacyRooms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PUENTE DE NEXUS"] = "control_center",
            ["LABORATORIO DE SEÑALES"] = "communications",
            ["ALMACÉN"] = "storage",
            ["CÁMARA DEL NÚCLEO"] = "anomaly_chamber",
            ["PASILLO DE SERVICIO"] = "central_corridor"
        };
        bool isLegacySnapshot = State.ExploredRooms.Any(legacyRooms.ContainsKey);
        for (int index = 0; index < State.ExploredRooms.Count; index++)
        {
            string priorName = State.ExploredRooms[index];
            if (legacyRooms.TryGetValue(priorName, out string? currentId))
                State.ExploredRooms[index] = currentId;
        }

        for (int index = 0; index < State.UnlockedDoors.Count; index++)
        {
            if (State.UnlockedDoors[index].Equals("anomaly_door", StringComparison.OrdinalIgnoreCase))
                State.UnlockedDoors[index] = "anomaly_access";
        }

        bool oldFinalDoorUnlocked = State.SecurityDoorUnlocked ||
            State.CompletedObjectives.Contains("security_console", StringComparer.OrdinalIgnoreCase);
        if (oldFinalDoorUnlocked)
        {
            AddUnique(State.UnlockedDoors, "anomaly_access");
            State.SecurityDoorUnlocked = true;
        }

        // Older maps placed the lower-right core at x=16..25 / y=14..21.
        if (isLegacySnapshot && State.ExploredRooms.Contains("anomaly_chamber", StringComparer.OrdinalIgnoreCase) &&
            State.PlayerX / TileSize is >= 16 and < 26 && State.PlayerY / TileSize is >= 14 and < 22)
        {
            State.PlayerX += 10 * TileSize;
            State.PlayerY += 6 * TileSize;
        }
        else if (isLegacySnapshot && State.ExploredRooms.Contains("storage", StringComparer.OrdinalIgnoreCase) &&
                 State.PlayerX / TileSize is >= 2 and < 12 && State.PlayerY / TileSize is >= 14 and < 22)
        {
            State.PlayerY += 6 * TileSize;
        }
        else if (isLegacySnapshot && State.ExploredRooms.Contains("communications", StringComparer.OrdinalIgnoreCase) &&
                 State.PlayerX / TileSize is >= 16 and < 26 && State.PlayerY / TileSize is >= 2 and < 10)
        {
            State.PlayerX -= 2 * TileSize;
        }

        State.ActiveDialogueLineIndex = Math.Max(0, State.ActiveDialogueLineIndex);
        State.ActiveDialogueChoiceIndex = Math.Max(0, State.ActiveDialogueChoiceIndex);
        State.ActiveChallengeStep = Math.Max(0, State.ActiveChallengeStep);
    }

    private static void AddUnique(List<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.OrdinalIgnoreCase))
            values.Add(value);
    }

    private sealed record RoomSpec(string Id, string Title, int X, int Y, int Width, int Height,
        Color FloorColor, Color AccentColor);

    private sealed record DoorSpec(string Id, int X, int Y, string RoomId, string? RequiredItemId,
        string? RequiredObjectiveId, bool StartsUnlocked = false);

    private sealed record InventoryEntry(string Id, string Name, string Category)
    {
        public string ShortName => Name.Length > 18 ? Name[..18] : Name;
    }

    private sealed record WorldObject(string Id, string Title, int X, int Y, string Objective,
        int OxygenChange, int HullChange, int DataChange, string Kind = "item",
        string? RequiredItemId = null, string? InventoryId = null)
    {
        public bool IsSecret => Kind == "secret";
    }
}
