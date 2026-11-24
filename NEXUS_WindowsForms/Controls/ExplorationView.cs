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
    private const float EnemyDetectionRadius = 272f;
    private const float EnemyContactDistance = 30f;
    private const float EnemyAttackInterval = 1.1f;
    private const float EnemySaveInterval = 3f;
    private const float PlayerShotInterval = 0.34f;
    private const float PlayerShotRange = 460f;
    private const int PlayerShotDamage = 50;

    private readonly bool[,] _walls = new bool[MapColumns, MapRows];
    private readonly HashSet<string> _unlockedDoors = new(StringComparer.OrdinalIgnoreCase);
    private static readonly RoomSpec[] Rooms =
    {
        new("control_center", "CENTRO DE CONTROL", 2, 2, 11, 9, Color.FromArgb(24, 34, 43), Color.FromArgb(59, 199, 232)),
        new("communications", "SALA DE COMUNICACIONES", 18, 2, 11, 8, Color.FromArgb(22, 31, 46), Color.FromArgb(87, 151, 239)),
        new("main_lab", "LABORATORIO BIOLÓGICO", 27, 12, 9, 8, Color.FromArgb(22, 39, 38), Color.FromArgb(77, 210, 177)),
        new("storage", "ALMACÉN TÉCNICO", 2, 20, 11, 8, Color.FromArgb(39, 36, 31), Color.FromArgb(219, 164, 93)),
        new("research", "ENFERMERÍA", 15, 20, 10, 8, Color.FromArgb(35, 35, 44), Color.FromArgb(132, 190, 229)),
        new("anomaly_chamber", "CÁMARA DE CONTENCIÓN ALIENÍGENA", 27, 21, 9, 8, Color.FromArgb(39, 27, 39), Color.FromArgb(228, 102, 155))
    };
    private static readonly Rectangle[] Corridors =
    {
        new(13, 5, 5, 3),       // Control ↔ communications: raised cross-connector.
        new(7, 11, 3, 9),       // West maintenance spine.
        new(15, 8, 3, 8),       // Communications ↔ central spine.
        new(15, 15, 3, 5),      // Central spine approaches the infirmary access door.
        new(28, 10, 3, 2),      // Communications ↔ biological lab threshold.
        new(24, 23, 3, 3)        // Infirmary ↔ containment threshold.
    };
    private static readonly DoorSpec[] Doors =
    {
        new("control_access", 12, 6, "control_center", null, null, true),
        new("communications_access", 18, 6, "communications", "access_card", null),
        new("laboratory_access", 29, 12, "main_lab", "access_card", null),
        new("storage_access", 8, 20, "storage", null, null, true),
        new("research_access", 17, 20, "research", "power_cell", null),
        new("anomaly_access", 27, 24, "anomaly_chamber", null, "security_console")
    };
    private static readonly StationFixture[] _fixtures =
    {
        Fixture("control_center", 3, 3, 2, "bridge"), Fixture("control_center", 10, 8, 1, "console"),
        Fixture("communications", 20, 3, 2, "rack"), Fixture("communications", 26, 8, 1, "console"),
        Fixture("main_lab", 32, 13, 2, "tank"), Fixture("main_lab", 28, 18, 1, "table"),
        Fixture("storage", 3, 21, 1, "crate"), Fixture("storage", 10, 21, 2, "rack"),
        Fixture("storage", 10, 26, 1, "crate"),
        Fixture("research", 20, 22, 2, "bed"), Fixture("research", 23, 21, 1, "cabinet"),
        Fixture("research", 17, 26, 1, "bed"),
        Fixture("anomaly_chamber", 33, 22, 2, "containment"), Fixture("anomaly_chamber", 33, 27, 2, "containment"),
        Fixture("control_center", 3, 9, 1, "cabinet"), Fixture("communications", 20, 8, 1, "console"),
        Fixture("main_lab", 35, 18, 1, "cabinet"), Fixture("research", 22, 26, 1, "cabinet")
    };
    private readonly RectangleF[] _furniture = _fixtures.Select(item => item.Bounds).ToArray();
    private readonly List<EnemyActor> _enemies = new();
    private readonly ExplorerSpriteSheet _sprites = new();
    private readonly Dictionary<string, Bitmap> _roomFloorTextures = Rooms.ToDictionary(
        room => room.Id, room => CreateFloorTexture(room.FloorColor), StringComparer.OrdinalIgnoreCase);
    private readonly Bitmap _floorTile = CreateFloorTexture(Color.FromArgb(13, 27, 43));
    private float _cameraX;
    private float _cameraY;
    private float _cameraZoom = 1f;
    private float _animationClock;
    private bool _isMoving;
    private float _interactionTimer;
    private float _enemyPathTimer;
    private float _enemySaveTimer;
    private float _shotCooldown;
    private float _playerHitTimer;
    private float _muzzleFlashTimer;
    private PointF _shotStart;
    private PointF _muzzleFlashEnd;
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
        new("life_support_reset", "CONSOLA DE SOPORTE VITAL", 11, 5, "", 0, 0, 0, "challenge"),
        new("signal_terminal", "ANTENA DE COMUNICACIONES", 22, 4, "Descifrar la señal repetida de Eridani-9.", 0, 0, 12, "objective"),
        new("communications_restore", "RELÉ DE COMUNICACIONES", 24, 7, "", 0, 0, 0, "challenge"),
        new("communications_log", "REGISTRO DE TRANSMISIONES", 26, 4, "La señal contiene una respuesta inteligente.", 0, 0, 3, "document"),
        new("secret_archive", "ARCHIVO OCULTO", 24, 8, "", 0, 0, 8, "secret", "toolkit", "secret_datacube"),
        new("power_cell", "CÉLULA DE ENERGÍA", 30, 14, "Célula de energía añadida al inventario.", 0, 0, 0, "item"),
        new("generator_repair", "GENERADOR AUXILIAR", 34, 17, "", 0, 0, 0, "challenge"),
        new("lab_document", "INFORME DE LABORATORIO", 34, 13, "El campo de la anomalía reacciona a pulsos eléctricos.", 0, 0, 3, "document"),
        new("research_log", "CUADERNO DE INVESTIGACIÓN", 17, 24, "El acceso a la cámara requiere validar el protocolo.", 0, 0, 3, "document"),
        new("security_override", "TECLADO DE SEGURIDAD", 23, 24, "", 0, 0, 0, "challenge"),
        new("security_console", "CONSOLA DE INVESTIGACIÓN", 19, 26, "", 0, -4, 5, "objective"),
        new("anomaly_stabilize", "NODO DE RESONANCIA", 30, 23, "", 0, 0, 0, "challenge"),
        new("anomaly_terminal", "NÚCLEO DE DATOS", 30, 26, "Asegurar los datos del núcleo y completar la expedición.", 0, 0, 18, "objective"),
        new("anomaly_record", "ECO DE LA ANOMALÍA", 34, 26, "El patrón parece responder a la presencia humana.", 0, 0, 5, "document")
    };

    public ExplorationSaveState State { get; private set; }
    public event EventHandler? StateChanged;
    public event EventHandler<ExplorationActionEventArgs>? ActionCompleted;

    public ExplorationView(ExplorationSaveState? state = null)
    {
        State = state ?? new ExplorationSaveState();
        NormalizeState();
        RestoreEnemies();
        State.PlayerX = Math.Clamp(State.PlayerX, TileSize * 2f, (MapColumns - 2) * TileSize);
        State.PlayerY = Math.Clamp(State.PlayerY, TileSize * 2f, (MapRows - 2) * TileSize);
        BuildMap();
        ApplySavedDoorState();
        EnsurePlayerOnWalkableFloor();
        EnsureEnemiesOnWalkableFloor();
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
    public bool PlayerDefeated => _hull <= 0;
    public bool BlocksMovement => _inventoryOpen || _missionLogOpen || DialogueOpen || ChallengeOpen || PlayerDefeated;

    public void ToggleInventory()
    {
        if (DialogueOpen || ChallengeOpen || _missionLogOpen || PlayerDefeated) return;
        _inventoryOpen = !_inventoryOpen;
        _inventorySelection = Math.Clamp(_inventorySelection, 0, Math.Max(0, GetInventoryItems().Count - 1));
        Invalidate();
    }

    public void ToggleMissionLog()
    {
        if (DialogueOpen || ChallengeOpen || _inventoryOpen || PlayerDefeated) return;
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
        if (BlocksMovement || PlayerDefeated) return false;
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
        RestoreEnemies();
        State.PlayerX = Math.Clamp(State.PlayerX, TileSize * 2f, (MapColumns - 2) * TileSize);
        State.PlayerY = Math.Clamp(State.PlayerY, TileSize * 2f, (MapRows - 2) * TileSize);
        _unlockedDoors.Clear();
        BuildMap();
        ApplySavedDoorState();
        EnsurePlayerOnWalkableFloor();
        EnsureEnemiesOnWalkableFloor();
        DiscoverCurrentRoom();
        _inventorySelection = 0;
        _missionLogOpen = false;
        RestoreActiveDialogue();
        RestoreActiveChallenge();
        Invalidate();
    }

    public void Interact()
    {
        if (BlocksMovement || PlayerDefeated) return;
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
        bool newlyDefeated = _hull > 0 && hull <= 0;
        _oxygen = Math.Clamp(oxygen, 0, 100);
        _hull = Math.Clamp(hull, 0, 100);
        _data = Math.Clamp(data, 0, 100);
        if (newlyDefeated)
            _statusMessage = "ALERTA CRÍTICA // INTEGRIDAD DEL TRAJE AGOTADA.";
        Invalidate();
    }

    private void FireInFacingDirection()
    {
        PointF direction = _facing switch
        {
            ExplorerDirection.Up => new PointF(0, -1),
            ExplorerDirection.Left => new PointF(-1, 0),
            ExplorerDirection.Right => new PointF(1, 0),
            _ => new PointF(0, 1)
        };
        FireAt(State.PlayerX + direction.X * PlayerShotRange,
            State.PlayerY + direction.Y * PlayerShotRange);
    }

    private void FireAt(float targetX, float targetY)
    {
        if (PlayerDefeated || BlocksMovement || _shotCooldown > 0) return;
        float dx = targetX - State.PlayerX;
        float dy = targetY - State.PlayerY;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        if (length < 1f) return;

        _shotCooldown = PlayerShotInterval;
        _facing = Math.Abs(dx) > Math.Abs(dy)
            ? dx < 0 ? ExplorerDirection.Left : ExplorerDirection.Right
            : dy < 0 ? ExplorerDirection.Up : ExplorerDirection.Down;
        float range = Math.Min(PlayerShotRange, length);
        float unitX = dx / length;
        float unitY = dy / length;
        _shotStart = new PointF(State.PlayerX, State.PlayerY);
        _muzzleFlashEnd = _shotStart;
        _muzzleFlashTimer = 0.12f;
        EnemyActor? struckEnemy = null;

        for (float distance = 8f; distance <= range; distance += 8f)
        {
            float shotX = State.PlayerX + unitX * distance;
            float shotY = State.PlayerY + unitY * distance;
            if (ShotHitsObstacle(shotX, shotY))
            {
                _muzzleFlashEnd = new PointF(shotX, shotY);
                break;
            }
            EnemyActor? enemy = _enemies.FirstOrDefault(candidate => !candidate.Save.IsDead &&
                DistanceSquared(shotX, shotY, candidate.Save.X, candidate.Save.Y) <= 19f * 19f);
            if (enemy is not null)
            {
                struckEnemy = enemy;
                _muzzleFlashEnd = new PointF(enemy.Save.X, enemy.Save.Y);
                break;
            }
            _muzzleFlashEnd = new PointF(shotX, shotY);
        }

        if (struckEnemy is not null)
        {
            struckEnemy.Save.Alerted = true;
            struckEnemy.Save.Health = Math.Max(0, struckEnemy.Save.Health - PlayerShotDamage);
            struckEnemy.HitFlashRemaining = 0.24f;
            if (struckEnemy.Save.Health == 0)
            {
                struckEnemy.Save.IsDead = true;
                _statusMessage = $"AMENAZA ELIMINADA // {struckEnemy.Name}.";
            }
            else
            {
                _statusMessage = $"IMPACTO CONFIRMADO // {struckEnemy.Name}: {struckEnemy.Save.Health}/100.";
            }
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            _statusMessage = "DISPARO DE ENERGÍA // SIN IMPACTO.";
        }
        Invalidate();
    }

    private bool ShotHitsObstacle(float x, float y)
    {
        int tileX = (int)(x / TileSize);
        int tileY = (int)(y / TileSize);
        if (tileX < 0 || tileX >= MapColumns || tileY < 0 || tileY >= MapRows || _walls[tileX, tileY])
            return true;
        return _furniture.Any(fixture => fixture.Contains(x, y));
    }

    private bool HasLineOfSight(float startX, float startY, float endX, float endY)
    {
        float dx = endX - startX;
        float dy = endY - startY;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        int steps = Math.Max(1, (int)MathF.Ceiling(distance / 8f));
        for (int step = 1; step < steps; step++)
        {
            float fraction = step / (float)steps;
            if (ShotHitsObstacle(startX + dx * fraction, startY + dy * fraction)) return false;
        }
        return true;
    }

    private static float DistanceSquared(float x1, float y1, float x2, float y2)
    {
        float dx = x1 - x2;
        float dy = y1 - y2;
        return dx * dx + dy * dy;
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
        if (_inventoryOpen || _missionLogOpen || DialogueOpen || PlayerDefeated) return;
        float elapsed = Math.Clamp(elapsedSeconds, 0f, 0.25f);
        _animationClock += elapsed;
        _shotCooldown = Math.Max(0f, _shotCooldown - elapsed);
        _playerHitTimer = Math.Max(0f, _playerHitTimer - elapsed);
        _muzzleFlashTimer = Math.Max(0f, _muzzleFlashTimer - elapsed);
        string? currentRoom = GetRoomIdAtTile((int)(State.PlayerX / TileSize), (int)(State.PlayerY / TileSize));
        foreach (string eventId in State.ActiveWorldEvents.ToArray())
        {
            WorldEventDefinition? worldEvent = WorldChallengeCatalog.FindEvent(eventId);
            if (worldEvent is null || worldEvent.DamageIntervalSeconds <= 0 ||
                (!worldEvent.DamageAnywhere && !string.Equals(currentRoom, worldEvent.RoomId, StringComparison.OrdinalIgnoreCase)))
                continue;

            State.WorldEventClocks.TryGetValue(eventId, out float eventElapsed);
            eventElapsed += elapsed;
            if (eventElapsed < worldEvent.DamageIntervalSeconds)
            {
                State.WorldEventClocks[eventId] = eventElapsed;
                continue;
            }

            State.WorldEventClocks[eventId] = eventElapsed - worldEvent.DamageIntervalSeconds;
            _statusMessage = worldEvent.OxygenDamage > 0
                ? $"AURA // SOPORTE VITAL INESTABLE: O₂ -{worldEvent.OxygenDamage}%."
                : $"AURA // INTERFERENCIA ACTIVA: casco -{worldEvent.HullDamage}%.";
            ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(
                $"Daño ambiental: {worldEvent.Id}", -worldEvent.OxygenDamage, -worldEvent.HullDamage));
            StateChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        UpdateEnemies(elapsed);
        if (_playerHitTimer > 0 || _muzzleFlashTimer > 0)
            Invalidate();
        _enemySaveTimer += elapsed;
        if (_enemySaveTimer >= EnemySaveInterval)
        {
            _enemySaveTimer = 0;
            StateChanged?.Invoke(this, EventArgs.Empty);
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
        _cameraZoom = Math.Clamp(Math.Min(viewWidth / 760f, viewHeight / 460f), 0.95f, 1.4f);
        float worldViewWidth = viewWidth / _cameraZoom;
        float worldViewHeight = viewHeight / _cameraZoom;
        _cameraX = Math.Clamp(State.PlayerX - worldViewWidth / 2f,
            0, MapColumns * TileSize - worldViewWidth);
        _cameraY = Math.Clamp(State.PlayerY - worldViewHeight / 2f,
            0, MapRows * TileSize - worldViewHeight);
        using (var worldTransform = new System.Drawing.Drawing2D.Matrix(
                   _cameraZoom, 0, 0, _cameraZoom, -_cameraX * _cameraZoom, -_cameraY * _cameraZoom))
        {
            graphics.Transform = worldTransform;
        }

        DrawMap(graphics);
        DrawStationLighting(graphics);
        DrawSystemEventEffects(graphics);
        DrawAnomalyField(graphics);
        DrawFurniture(graphics);
        DrawEnemies(graphics);
        DrawCombatEffects(graphics);
        DrawObjects(graphics);
        DrawPlayer(graphics);

        graphics.ResetTransform();
        DrawOverlay(graphics);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
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

        if (e.Button == MouseButtons.Left && _activeChallenge is null &&
            _activeDialogue is null && !_inventoryOpen && !_missionLogOpen)
        {
            if (GetShootButtonBounds().Contains(e.Location))
                FireInFacingDirection();
            else
                FireAt(e.X / _cameraZoom + _cameraX, e.Y / _cameraZoom + _cameraY);
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
            CarveRoomShape(room);

        foreach (Rectangle corridor in Corridors)
            CarveRoom(corridor.X, corridor.Y, corridor.Width, corridor.Height);

        // Intentional open thresholds between the control wing, comms, and service spine.
        CarveRoom(8, 10, 1, 1);
        CarveRoom(15, 9, 3, 1);
        CarveRoom(27, 9, 4, 1);

        // Doors are the only deliberate openings in each room's perimeter.
        foreach (DoorSpec door in Doors)
            _walls[door.X, door.Y] = false;

        foreach (DoorSpec door in Doors)
            _walls[door.X, door.Y] = !IsDoorUnlocked(door);
    }

    private void EnsurePlayerOnWalkableFloor()
    {
        if (!HitsWall(State.PlayerX, State.PlayerY)) return;
        FindNearestWalkable(State.PlayerX, State.PlayerY, out float x, out float y);
        State.PlayerX = x;
        State.PlayerY = y;
    }

    private void EnsureEnemiesOnWalkableFloor()
    {
        foreach (EnemyActor enemy in _enemies)
        {
            if (enemy.Save.IsDead || !HitsWall(enemy.Save.X, enemy.Save.Y)) continue;
            EnemySpawn spawn = GetEnemySpawn(enemy.Id);
            enemy.Save.X = spawn.TileX * TileSize + TileSize / 2f;
            enemy.Save.Y = spawn.TileY * TileSize + TileSize / 2f;
            enemy.NextX = enemy.NextY = -1;
        }
    }

    private void FindNearestWalkable(float worldX, float worldY, out float resultX, out float resultY)
    {
        int originX = Math.Clamp((int)(worldX / TileSize), 0, MapColumns - 1);
        int originY = Math.Clamp((int)(worldY / TileSize), 0, MapRows - 1);
        var queue = new Queue<Point>();
        var visited = new HashSet<Point>();
        var origin = new Point(originX, originY);
        queue.Enqueue(origin);
        visited.Add(origin);
        Point[] neighbors = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };
        while (queue.Count > 0)
        {
            Point tile = queue.Dequeue();
            float centerX = tile.X * TileSize + TileSize / 2f;
            float centerY = tile.Y * TileSize + TileSize / 2f;
            if (!HitsWall(centerX, centerY))
            {
                resultX = centerX;
                resultY = centerY;
                return;
            }
            foreach (Point offset in neighbors)
            {
                Point next = new(tile.X + offset.X, tile.Y + offset.Y);
                if (next.X < 0 || next.X >= MapColumns || next.Y < 0 || next.Y >= MapRows ||
                    !visited.Add(next)) continue;
                queue.Enqueue(next);
            }
        }
        resultX = 4 * TileSize + TileSize / 2f;
        resultY = 4 * TileSize + TileSize / 2f;
    }

    private void CarveRoomShape(RoomSpec room)
    {
        for (int x = room.X + 1; x < room.X + room.Width - 1; x++)
        for (int y = room.Y + 1; y < room.Y + room.Height - 1; y++)
        {
            bool cutCorner = room.Id switch
            {
                "control_center" => (x == room.X + 1 && y == room.Y + 1) ||
                                    (x == room.X + room.Width - 2 && y == room.Y + room.Height - 2),
                "communications" => (x == room.X + room.Width - 2 && y == room.Y + 1) ||
                                    (x == room.X + 1 && y == room.Y + room.Height - 2),
                "main_lab" => (x == room.X + 1 && y == room.Y + 1) ||
                              (x == room.X + room.Width - 2 && y == room.Y + 1),
                "storage" => x == room.X + room.Width - 2 &&
                             (y == room.Y + 1 || y == room.Y + room.Height - 2),
                "research" => x == room.X + 1 && y == room.Y + 1,
                _ => (x == room.X + 1 && y == room.Y + 1) ||
                     (x == room.X + 1 && y == room.Y + room.Height - 2) ||
                     (x == room.X + room.Width - 2 && y == room.Y + 1)
            };
            if (!cutCorner) _walls[x, y] = false;
        }
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
        int endX = Math.Min(MapColumns, (int)((_cameraX + ClientSize.Width / _cameraZoom) / TileSize) + 2);
        int startY = Math.Max(0, (int)(_cameraY / TileSize) - 1);
        int endY = Math.Min(MapRows, (int)((_cameraY + ClientSize.Height / _cameraZoom) / TileSize) + 2);
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
            int titleColumn = room.Id switch
            {
                "main_lab" => room.X + 3,
                "research" => room.X + 3,
                _ => room.X + 1
            };
            DrawRoomTitle(graphics, font, roomTitleBrush, room.Title, titleColumn, room.Y);
            DrawSectorSign(graphics, room, font, labelBrush);
        }
        DrawCorridorMarkings(graphics);
        DrawDoors(graphics, font);
    }

    private static Bitmap CreateFloorTexture(Color floorColor)
    {
        var texture = new Bitmap(TileSize, TileSize);
        using Graphics graphics = Graphics.FromImage(texture);
        graphics.Clear(floorColor);
        Color panelColor = Color.FromArgb(Math.Min(255, floorColor.R + 3),
            Math.Min(255, floorColor.G + 5), Math.Min(255, floorColor.B + 7));
        using var seamPen = new Pen(Color.FromArgb(105, 126, 153, 167), 1);
        using var panelBrush = new SolidBrush(panelColor);
        using var glintPen = new Pen(Color.FromArgb(30, 99, 167, 185), 1);
        using var damagePen = new Pen(Color.FromArgb(32, 2, 8, 14), 2);
        graphics.DrawRectangle(seamPen, 2, 2, TileSize - 5, TileSize - 5);
        graphics.FillRectangle(panelBrush, 7, 7, TileSize - 14, TileSize - 14);
        graphics.DrawLine(glintPen, 8, 10, TileSize - 9, 10);
        graphics.DrawLine(glintPen, 8, TileSize - 10, TileSize - 9, TileSize - 10);
        graphics.DrawLine(damagePen, 11, 31, 20, 27);
        graphics.DrawLine(damagePen, 24, 30, 31, 33);
        using var rivetBrush = new SolidBrush(Color.FromArgb(112, 148, 165));
        graphics.FillEllipse(rivetBrush, 4, 4, 3, 3);
        graphics.FillEllipse(rivetBrush, TileSize - 7, 4, 3, 3);
        graphics.FillEllipse(rivetBrush, 4, TileSize - 7, 3, 3);
        graphics.FillEllipse(rivetBrush, TileSize - 7, TileSize - 7, 3, 3);
        return texture;
    }

    private static void DrawSectorSign(Graphics graphics, RoomSpec room, Font font, Brush mutedBrush)
    {
        string code = room.Id switch
        {
            "control_center" => "A-01 // PUENTE",
            "communications" => "A-02 // COMMS",
            "main_lab" => "B-01 // BIO-LAB",
            "storage" => "B-02 // MANTENIMIENTO",
            "research" => "C-01 // MEDICAL",
            _ => "C-02 // CONTENCIÓN"
        };
        Rectangle panel = new((room.X + room.Width - 5) * TileSize,
            (room.Y + room.Height - 1) * TileSize + 8, 152, 23);
        using var sign = new SolidBrush(Color.FromArgb(205, 8, 16, 25));
        using var edge = new Pen(Color.FromArgb(115, room.AccentColor), 1);
        graphics.FillRectangle(sign, panel);
        graphics.DrawRectangle(edge, panel);
        graphics.DrawString(code, font, mutedBrush, panel.X + 7, panel.Y + 4);
    }

    private void DrawCorridorMarkings(Graphics graphics)
    {
        using var pipePen = new Pen(Color.FromArgb(110, 69, 111, 129), 3);
        using var edgePen = new Pen(Color.FromArgb(120, 36, 193, 220), 2);
        using var hazardBrush = new SolidBrush(Color.FromArgb(95, 225, 154, 62));
        foreach (Rectangle corridor in Corridors)
        {
            RectangleF area = new(corridor.X * TileSize, corridor.Y * TileSize,
                corridor.Width * TileSize, corridor.Height * TileSize);
            if (corridor.Width >= corridor.Height)
            {
                float center = area.Y + area.Height / 2f;
                graphics.DrawLine(pipePen, area.X + 7, center, area.Right - 7, center);
                graphics.DrawLine(edgePen, area.X + 7, center - 8, area.Right - 7, center - 8);
            }
            else
            {
                float center = area.X + area.Width / 2f;
                graphics.DrawLine(pipePen, center, area.Y + 7, center, area.Bottom - 7);
                graphics.DrawLine(edgePen, center - 8, area.Y + 7, center - 8, area.Bottom - 7);
            }
        }
        foreach (DoorSpec door in Doors)
        {
            int hazardY = door.Y < 15 ? door.Y * TileSize + TileSize - 7 : door.Y * TileSize + 1;
            Rectangle hazard = new(door.X * TileSize - 8, hazardY, 60, 6);
            graphics.FillRectangle(hazardBrush, hazard);
            for (int stripe = 0; stripe < 4; stripe++)
                graphics.DrawLine(Pens.DarkOrange, hazard.X + stripe * 15, hazard.Bottom,
                    hazard.X + stripe * 15 + 6, hazard.Y);
        }
    }

    private void DrawStationLighting(Graphics graphics)
    {
        using var ambient = new SolidBrush(Color.FromArgb(26, 1, 6, 13));
        graphics.FillRectangle(ambient, 0, 0, MapColumns * TileSize, MapRows * TileSize);
        foreach (RoomSpec room in Rooms)
        {
            float x = (room.X + room.Width / 2f) * TileSize;
            float y = (room.Y + 1.5f) * TileSize;
            FillLightGlow(graphics, x, y, 126f, room.AccentColor, 46);
            FillLightGlow(graphics, (room.X + room.Width - 2f) * TileSize,
                (room.Y + room.Height - 2f) * TileSize, 82f, room.AccentColor, 28);
        }

        float emergencyPulse = 0.5f + 0.5f * MathF.Sin(_animationClock * 4.2f);
        FillLightGlow(graphics, 18 * TileSize, 14 * TileSize,
            150f, Color.FromArgb(255, 63, 50), (int)(18 + emergencyPulse * 38));
    }

    private static void FillLightGlow(Graphics graphics, float centerX, float centerY,
        float radius, Color color, int alpha)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddEllipse(centerX - radius, centerY - radius, radius * 2, radius * 2);
        using var glow = new System.Drawing.Drawing2D.PathGradientBrush(path)
        {
            CenterColor = Color.FromArgb(alpha, color),
            SurroundColors = new[] { Color.FromArgb(0, color) }
        };
        graphics.FillPath(glow, path);
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
            graphics.DrawLine(doorPen, door.X + door.Width / 2, door.Y + 7,
                door.X + door.Width / 2, door.Bottom - 7);
            graphics.FillRectangle(open ? Brushes.Cyan : Brushes.OrangeRed,
                door.X + 4, door.Y + door.Height / 2 - 2, 4, 4);
            graphics.FillRectangle(open ? Brushes.Cyan : Brushes.OrangeRed,
                door.Right - 8, door.Y + door.Height / 2 - 2, 4, 4);
            if (DistanceTo(specification) <= 110f)
            {
                string marker = open ? "ABIERTO" : GetDoorShortLabel(specification);
                graphics.DrawString(marker, font, Brushes.White, door.X - 15, door.Y - 20);
            }
        }
    }

    private void DrawFurniture(Graphics graphics)
    {
        using var shellBrush = new SolidBrush(Color.FromArgb(31, 46, 57));
        using var insetBrush = new SolidBrush(Color.FromArgb(8, 16, 24));
        using var edgePen = new Pen(Color.FromArgb(93, 122, 139), 1.5f);
        using var amberBrush = new SolidBrush(Color.FromArgb(220, 148, 68));
        using var paleBrush = new SolidBrush(Color.FromArgb(95, 158, 184));
        foreach (StationFixture fixture in _fixtures)
        {
            Rectangle rect = Rectangle.Round(fixture.Bounds);
            int roomTileX = Math.Clamp((int)(rect.X / TileSize), 0, MapColumns - 1);
            int roomTileY = Math.Clamp((int)(rect.Y / TileSize), 0, MapRows - 1);
            RoomSpec room = GetRoom(fixture.RoomId);
            using var roomGlow = new Pen(Color.FromArgb(175, room.AccentColor), 2);
            using var glass = new SolidBrush(Color.FromArgb(110, room.AccentColor));
            graphics.FillRectangle(shellBrush, rect);
            graphics.DrawRectangle(edgePen, rect);
            graphics.FillRectangle(insetBrush, rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
            switch (fixture.Kind)
            {
                case "bridge":
                    graphics.FillRectangle(glass, rect.X + 7, rect.Y + 7, rect.Width - 14, 9);
                    graphics.DrawLine(Pens.LightCyan, rect.X + 9, rect.Y + 10, rect.Right - 9, rect.Y + 10);
                    graphics.DrawLine(roomGlow, rect.X + 7, rect.Bottom - 7, rect.Right - 7, rect.Bottom - 7);
                    break;
                case "rack":
                    for (int shelf = 1; shelf < 5; shelf++)
                        graphics.DrawLine(edgePen, rect.X + 6, rect.Y + shelf * rect.Height / 5,
                            rect.Right - 6, rect.Y + shelf * rect.Height / 5);
                    for (int lamp = 0; lamp < 3; lamp++)
                        graphics.FillRectangle(lamp == 0 ? amberBrush : glass, rect.X + 7,
                            rect.Y + 7 + lamp * 11, 4, 5);
                    break;
                case "tank":
                case "containment":
                    graphics.FillEllipse(glass, rect.X + 8, rect.Y + 4, rect.Width - 16, rect.Height - 8);
                    graphics.DrawEllipse(roomGlow, rect.X + 8, rect.Y + 4, rect.Width - 16, rect.Height - 8);
                    graphics.DrawLine(edgePen, rect.X + 8, rect.Y + rect.Height / 2,
                        rect.Right - 8, rect.Y + rect.Height / 2);
                    if (fixture.Kind == "containment")
                    {
                        graphics.DrawLine(Pens.OrangeRed, rect.X + 10, rect.Y + 8,
                            rect.Right - 10, rect.Bottom - 8);
                        graphics.DrawLine(Pens.OrangeRed, rect.Right - 10, rect.Y + 8,
                            rect.X + 10, rect.Bottom - 8);
                    }
                    break;
                case "bed":
                    graphics.FillRectangle(paleBrush, rect.X + 6, rect.Y + 8, rect.Width - 12, rect.Height - 16);
                    graphics.DrawRectangle(roomGlow, rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 10);
                    graphics.FillRectangle(Brushes.WhiteSmoke, rect.X + 8, rect.Y + 8, 12, 8);
                    break;
                case "crate":
                    graphics.DrawLine(roomGlow, rect.X + 5, rect.Y + 5, rect.Right - 5, rect.Bottom - 5);
                    graphics.DrawLine(roomGlow, rect.Right - 5, rect.Y + 5, rect.X + 5, rect.Bottom - 5);
                    graphics.DrawLine(edgePen, rect.X + 5, rect.Y + rect.Height / 2,
                        rect.Right - 5, rect.Y + rect.Height / 2);
                    break;
                case "cabinet":
                    graphics.DrawLine(roomGlow, rect.X + rect.Width / 2, rect.Y + 5,
                        rect.X + rect.Width / 2, rect.Bottom - 5);
                    graphics.FillEllipse(amberBrush, rect.X + rect.Width / 2 - 2, rect.Y + rect.Height / 2, 4, 4);
                    break;
                default:
                    graphics.FillRectangle(glass, rect.X + 6, rect.Y + 6, rect.Width - 12, 11);
                    graphics.DrawLine(Pens.LightCyan, rect.X + 8, rect.Y + 10, rect.Right - 8, rect.Y + 10);
                    graphics.DrawLine(edgePen, rect.X + 6, rect.Bottom - 8, rect.Right - 6, rect.Bottom - 8);
                    break;
            }
            graphics.FillEllipse(amberBrush, rect.X + 6, rect.Bottom - 6, 3, 3);
            graphics.FillEllipse(glass, rect.Right - 9, rect.Bottom - 6, 3, 3);
        }
    }

    private void DrawEnemies(Graphics graphics)
    {
        using var shell = new SolidBrush(Color.FromArgb(30, 40, 50));
        using var shadow = new SolidBrush(Color.FromArgb(110, 1, 3, 8));
        using var eye = new SolidBrush(Color.FromArgb(255, 255, 88, 74));
        using var trim = new Pen(Color.FromArgb(225, 255, 126, 91), 2);
        using var nameFont = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        foreach (EnemyActor enemy in _enemies)
        {
            if (enemy.Save.IsDead) continue;
            float bob = MathF.Sin(_animationClock * 4.5f + enemy.Phase) * 2f;
            float x = enemy.Save.X;
            float y = enemy.Save.Y + bob;
            using var aura = new SolidBrush(Color.FromArgb(enemy.HitFlashRemaining > 0 ? 130 : enemy.Save.Alerted ? 66 : 33,
                enemy.HitFlashRemaining > 0 ? 245 : 255, enemy.HitFlashRemaining > 0 ? 245 : 55,
                enemy.HitFlashRemaining > 0 ? 255 : 66));
            graphics.FillEllipse(aura, x - 22, y - 19, 44, 38);
            graphics.FillEllipse(shadow, x - 15, y + 8, 30, 9);

            if (enemy.Kind == "specimen")
            {
                PointF[] body =
                {
                    new(x, y - 16), new(x + 11, y - 7), new(x + 15, y + 5),
                    new(x + 6, y + 13), new(x - 8, y + 12), new(x - 15, y + 2), new(x - 10, y - 9)
                };
                using var membrane = new SolidBrush(Color.FromArgb(184, 92, 46, 84));
                graphics.FillPolygon(membrane, body);
                graphics.DrawPolygon(trim, body);
                graphics.DrawLine(trim, x - 9, y - 7, x - 18, y - 14);
                graphics.DrawLine(trim, x + 9, y - 7, x + 18, y - 14);
                graphics.DrawLine(trim, x - 8, y + 8, x - 15, y + 15);
                graphics.DrawLine(trim, x + 8, y + 8, x + 15, y + 15);
            }
            else
            {
                graphics.FillRectangle(shell, x - 14, y - 11, 28, 20);
                graphics.DrawRectangle(trim, x - 14, y - 11, 28, 20);
                graphics.DrawLine(trim, x - 11, y - 8, x - 18, y - 13);
                graphics.DrawLine(trim, x + 11, y - 8, x + 18, y - 13);
                graphics.DrawLine(trim, x - 10, y + 7, x - 16, y + 13);
                graphics.DrawLine(trim, x + 10, y + 7, x + 16, y + 13);
            }
            graphics.FillEllipse(eye, x - 4, y - 4, 8, 5);
            if (enemy.Save.Alerted || enemy.HitFlashRemaining > 0)
            {
                RectangleF healthTrack = new(x - 17, y - 25, 34, 4);
                using var track = new SolidBrush(Color.FromArgb(205, 24, 27, 34));
                using var health = new SolidBrush(Color.FromArgb(235, 255, 86, 78));
                graphics.FillRectangle(track, healthTrack);
                graphics.FillRectangle(health, healthTrack.X, healthTrack.Y,
                    healthTrack.Width * Math.Clamp(enemy.Save.Health, 0, 100) / 100f, healthTrack.Height);
            }
            if (enemy.Save.Alerted && DistanceToWorld(enemy.Save.X, enemy.Save.Y) < 150f)
                graphics.DrawString(enemy.Name, nameFont, Brushes.OrangeRed, x - 45, y - 29);
        }
    }

    private void UpdateEnemies(float elapsed)
    {
        _enemyPathTimer -= elapsed;
        if (_enemyPathTimer <= 0)
        {
            _enemyPathTimer = 0.28f;
            foreach (EnemyActor enemy in _enemies)
                if (!enemy.Save.IsDead && enemy.Save.Alerted) SetEnemyNextPathPoint(enemy);
        }

        foreach (EnemyActor enemy in _enemies)
        {
            if (enemy.Save.IsDead) continue;
            enemy.HitFlashRemaining = Math.Max(0f, enemy.HitFlashRemaining - elapsed);
            float distance = DistanceToWorld(enemy.Save.X, enemy.Save.Y);
            bool canSeePlayer = distance <= EnemyDetectionRadius && HasLineOfSight(enemy.Save.X, enemy.Save.Y,
                State.PlayerX, State.PlayerY);
            if (!enemy.Save.Alerted && canSeePlayer)
            {
                enemy.Save.Alerted = true;
                _statusMessage = $"AURA // AMENAZA DETECTADA: {enemy.Name}.";
                StateChanged?.Invoke(this, EventArgs.Empty);
            }

            enemy.AttackCooldown = Math.Max(0, enemy.AttackCooldown - elapsed);
            if (enemy.Save.Alerted && distance <= EnemyContactDistance && canSeePlayer)
            {
                if (enemy.AttackCooldown <= 0)
                {
                    enemy.AttackCooldown = EnemyAttackInterval;
                    _playerHitTimer = 0.42f;
                    _statusMessage = $"ALERTA DE IMPACTO // {enemy.Name}: integridad del traje -12%.";
                    ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(
                        $"Impacto de amenaza: {enemy.Id}", hullChange: -12));
                    StateChanged?.Invoke(this, EventArgs.Empty);
                    if (PlayerDefeated) break;
                }
                continue;
            }

            if (!enemy.Save.Alerted || enemy.NextX < 0 || enemy.NextY < 0) continue;
            float dx = enemy.NextX - enemy.Save.X;
            float dy = enemy.NextY - enemy.Save.Y;
            float length = MathF.Sqrt(dx * dx + dy * dy);
            if (length < 2f)
            {
                enemy.NextX = -1;
                enemy.NextY = -1;
                continue;
            }

            float distanceToMove = Math.Min(length, enemy.Speed * elapsed);
            float nextX = enemy.Save.X + dx / length * distanceToMove;
            float nextY = enemy.Save.Y + dy / length * distanceToMove;
            if (!HitsWall(nextX, enemy.Save.Y)) enemy.Save.X = nextX;
            if (!HitsWall(enemy.Save.X, nextY)) enemy.Save.Y = nextY;
            Invalidate();
        }
    }

    private void SetEnemyNextPathPoint(EnemyActor enemy)
    {
        enemy.NextX = -1;
        enemy.NextY = -1;
        int targetX = Math.Clamp((int)(State.PlayerX / TileSize), 0, MapColumns - 1);
        int targetY = Math.Clamp((int)(State.PlayerY / TileSize), 0, MapRows - 1);
        int enemyX = Math.Clamp((int)(enemy.Save.X / TileSize), 0, MapColumns - 1);
        int enemyY = Math.Clamp((int)(enemy.Save.Y / TileSize), 0, MapRows - 1);
        if (enemyX == targetX && enemyY == targetY)
        {
            enemy.NextX = State.PlayerX;
            enemy.NextY = State.PlayerY;
            return;
        }

        int[,] distance = new int[MapColumns, MapRows];
        for (int x = 0; x < MapColumns; x++)
        for (int y = 0; y < MapRows; y++) distance[x, y] = -1;
        if (!CanOccupyEnemyTile(targetX, targetY)) return;

        var queue = new Queue<Point>();
        queue.Enqueue(new Point(targetX, targetY));
        distance[targetX, targetY] = 0;
        Point[] neighbors = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
        while (queue.Count > 0)
        {
            Point current = queue.Dequeue();
            foreach (Point offset in neighbors)
            {
                int x = current.X + offset.X;
                int y = current.Y + offset.Y;
                if (x < 0 || x >= MapColumns || y < 0 || y >= MapRows || distance[x, y] >= 0 ||
                    !CanOccupyEnemyTile(x, y)) continue;
                distance[x, y] = distance[current.X, current.Y] + 1;
                queue.Enqueue(new Point(x, y));
            }
        }

        if (distance[enemyX, enemyY] <= 0) return;
        Point best = new(enemyX, enemyY);
        foreach (Point offset in neighbors)
        {
            int x = enemyX + offset.X;
            int y = enemyY + offset.Y;
            if (x < 0 || x >= MapColumns || y < 0 || y >= MapRows || distance[x, y] < 0 ||
                distance[x, y] >= distance[best.X, best.Y]) continue;
            best = new Point(x, y);
        }
        if (best.X != enemyX || best.Y != enemyY)
        {
            enemy.NextX = best.X * TileSize + TileSize / 2f;
            enemy.NextY = best.Y * TileSize + TileSize / 2f;
        }
    }

    private bool CanOccupyEnemyTile(int x, int y)
    {
        if (_walls[x, y]) return false;
        float centerX = x * TileSize + TileSize / 2f;
        float centerY = y * TileSize + TileSize / 2f;
        return !_furniture.Any(piece => piece.IntersectsWith(
            new RectangleF(centerX - 8, centerY - 8, 16, 16)));
    }

    private void RestoreEnemies()
    {
        State.Enemies ??= new List<EnemySaveState>();
        EnemySpawn[] spawns =
        {
            new("security_drone", "DRON DE SEGURIDAD", "drone", 23, 6, 74f),
            new("medical_sentinel", "SENTINELA DE AISLAMIENTO", "drone", 20, 24, 65f),
            new("containment_specimen", "ORGANISMO DE CONTENCIÓN", "specimen", 31, 25, 84f)
        };
        _enemies.Clear();
        foreach (EnemySpawn spawn in spawns)
        {
            EnemySaveState? saved = State.Enemies.FirstOrDefault(item =>
                item.Id.Equals(spawn.Id, StringComparison.OrdinalIgnoreCase));
            if (saved is null)
            {
                saved = new EnemySaveState
                {
                    Id = spawn.Id,
                    X = spawn.TileX * TileSize + TileSize / 2f,
                    Y = spawn.TileY * TileSize + TileSize / 2f
                };
                State.Enemies.Add(saved);
            }
            saved.Health = Math.Clamp(saved.Health, 0, 100);
            if (saved.Health == 0) saved.IsDead = true;
            _enemies.Add(new EnemyActor(spawn.Id, spawn.Name, spawn.Kind, spawn.Speed, saved));
        }
    }

    private static EnemySpawn GetEnemySpawn(string id) => id switch
    {
        "security_drone" => new("security_drone", "DRON DE SEGURIDAD", "drone", 23, 6, 74f),
        "medical_sentinel" => new("medical_sentinel", "SENTINELA DE AISLAMIENTO", "drone", 20, 24, 65f),
        _ => new("containment_specimen", "ORGANISMO DE CONTENCIÓN", "specimen", 31, 25, 84f)
    };

    private void DrawAnomalyField(Graphics graphics)
    {
        float centerX = 32 * TileSize + TileSize / 2f;
        float centerY = 25 * TileSize + TileSize / 2f;
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
            float y = 17 * TileSize + TileSize / 2f;
            graphics.FillEllipse(surgeBrush, x - 24, y - 24, 48, 48);
            using var sparkPen = new Pen(Color.FromArgb(170, 255, 200, 118), 2);
            graphics.DrawLine(sparkPen, x - 14, y - 8, x - 3, y + 2);
            graphics.DrawLine(sparkPen, x + 10, y - 12, x + 1, y - 1);
        }

        if (State.ActiveWorldEvents.Contains("life_support_failure", StringComparer.OrdinalIgnoreCase))
        {
            using var alertPen = new Pen(Color.FromArgb(145, 255, 82, 91), 2);
            float x = 11 * TileSize + TileSize / 2f;
            float y = 5 * TileSize + TileSize / 2f;
            graphics.DrawEllipse(alertPen, x - 20, y - 20, 40, 40);
        }

        if (State.ActiveWorldEvents.Contains("research_lockdown", StringComparer.OrdinalIgnoreCase))
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(_animationClock * 4f);
            using var lockdownPen = new Pen(Color.FromArgb((int)(90 + pulse * 150), 255, 111, 84), 2);
            float x = 23 * TileSize + TileSize / 2f;
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
        using var glowBrush = new SolidBrush(_playerHitTimer > 0
            ? Color.FromArgb(140, 255, 53, 66)
            : Color.FromArgb(55, 40, 224, 255));
        graphics.FillEllipse(glowBrush, x - 22, y - 22, 44, 44);
        int frame = _isMoving ? (int)(_animationClock * 9) % 3 : 0;
        Bitmap sprite = _sprites.Get(_facing, _isMoving, frame, _interactionTimer > 0);
        graphics.DrawImage(sprite, x - sprite.Width / 2f, y - sprite.Height / 2f, sprite.Width, sprite.Height);
        DrawEnergyPistol(graphics, x, y);
    }

    private void DrawEnergyPistol(Graphics graphics, float x, float y)
    {
        using var body = new SolidBrush(Color.FromArgb(208, 29, 49, 66));
        using var outline = new Pen(Color.FromArgb(225, 91, 210, 235), 1.5f);
        using var emitter = new SolidBrush(Color.FromArgb(245, 89, 235, 255));
        RectangleF weapon = _facing switch
        {
            ExplorerDirection.Up => new RectangleF(x - 5, y - 18, 10, 17),
            ExplorerDirection.Left => new RectangleF(x - 18, y - 4, 17, 9),
            ExplorerDirection.Right => new RectangleF(x + 1, y - 4, 17, 9),
            _ => new RectangleF(x - 5, y + 1, 10, 17)
        };
        graphics.FillRectangle(body, weapon);
        graphics.DrawRectangle(outline, weapon.X, weapon.Y, weapon.Width, weapon.Height);
        RectangleF muzzle = _facing switch
        {
            ExplorerDirection.Up => new RectangleF(weapon.X + 2, weapon.Y - 3, 6, 3),
            ExplorerDirection.Left => new RectangleF(weapon.X - 3, weapon.Y + 2, 3, 5),
            ExplorerDirection.Right => new RectangleF(weapon.Right, weapon.Y + 2, 3, 5),
            _ => new RectangleF(weapon.X + 2, weapon.Bottom, 6, 3)
        };
        graphics.FillRectangle(emitter, muzzle);
    }

    private void DrawCombatEffects(Graphics graphics)
    {
        if (_muzzleFlashTimer <= 0) return;
        int alpha = Math.Clamp((int)(255 * _muzzleFlashTimer / 0.12f), 0, 255);
        using var beam = new Pen(Color.FromArgb(alpha, 66, 228, 255), 4f);
        using var core = new Pen(Color.FromArgb(alpha, 218, 252, 255), 1.5f);
        using var impact = new SolidBrush(Color.FromArgb(alpha, 123, 247, 255));
        graphics.DrawLine(beam, _shotStart, _muzzleFlashEnd);
        graphics.DrawLine(core, _shotStart, _muzzleFlashEnd);
        graphics.FillEllipse(impact, _muzzleFlashEnd.X - 6, _muzzleFlashEnd.Y - 6, 12, 12);
    }

    private void DrawOverlay(Graphics graphics)
    {
        using var panelBrush = new SolidBrush(Color.FromArgb(220, 7, 18, 34));
        using var borderPen = new Pen(Color.FromArgb(40, 190, 225), 1);
        using var titleFont = new Font("Segoe UI", 10f, FontStyle.Bold);
        using var textFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
        using var titleBrush = new SolidBrush(NexusTheme.Accent);
        using var textBrush = new SolidBrush(NexusTheme.Text);

        int statusWidth = ClientSize.Width >= 590
            ? Math.Min(ClientSize.Width - 24, 350)
            : Math.Max(150, ClientSize.Width / 2 - 18);
        Rectangle status = new(12, 12, statusWidth, 72);
        graphics.FillRectangle(panelBrush, status);
        graphics.DrawRectangle(borderPen, status);
        graphics.DrawString(CurrentRoomName(), titleFont, titleBrush, status.X + 10, status.Y + 7);
        graphics.DrawString(_statusMessage, textFont, textBrush,
            new RectangleF(status.X + 10, status.Y + 30, status.Width - 20, 38));

        DrawGameHud(graphics, panelBrush, borderPen, titleFont, textFont, titleBrush, textBrush);

        Rectangle fireButton = GetShootButtonBounds();
        using (var fireBrush = new SolidBrush(Color.FromArgb(230, 11, 39, 58)))
        using (var firePen = new Pen(PlayerDefeated ? Color.DimGray : Color.FromArgb(210, 57, 214, 242), 2))
        using (var fireText = new SolidBrush(PlayerDefeated ? Color.Gray : Color.FromArgb(173, 246, 255)))
        using (var fireFont = new Font("Segoe UI", 11f, FontStyle.Bold))
        {
            graphics.FillRectangle(fireBrush, fireButton);
            graphics.DrawRectangle(firePen, fireButton);
            graphics.DrawString(_shotCooldown > 0 ? "RECARGANDO" : "DISPARAR",
                fireFont, fireText, fireButton.X + 17, fireButton.Y + 9);
        }
        graphics.DrawString("RATÓN: APUNTAR", textFont, titleBrush,
            fireButton.Right + 8, fireButton.Y + 11);

        string prompt = GetPrompt();
        Rectangle help = new(12, Math.Max(12, ClientSize.Height - 36), ClientSize.Width - 24, 26);
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
        if (PlayerDefeated)
            DrawDefeatOverlay(graphics, titleFont, textFont, textBrush);
    }

    private Rectangle GetShootButtonBounds() => new(12, Math.Max(12, ClientSize.Height - 88), 164, 42);

    private void DrawDefeatOverlay(Graphics graphics, Font titleFont, Font textFont, Brush textBrush)
    {
        Rectangle panel = new(Math.Max(12, ClientSize.Width / 2 - 205),
            Math.Max(12, ClientSize.Height / 2 - 74), Math.Min(410, ClientSize.Width - 24), 148);
        using var shade = new SolidBrush(Color.FromArgb(180, 1, 3, 9));
        using var alert = new SolidBrush(Color.FromArgb(245, 13, 15, 25));
        using var alertBorder = new Pen(Color.FromArgb(245, 255, 76, 81), 2);
        graphics.FillRectangle(shade, ClientRectangle);
        graphics.FillRectangle(alert, panel);
        graphics.DrawRectangle(alertBorder, panel);
        graphics.DrawString("TRAJE COMPROMETIDO", titleFont, Brushes.OrangeRed,
            panel.X + 18, panel.Y + 20);
        graphics.DrawString("La integridad llegó a cero. La expedición queda detenida.",
            textFont, textBrush, new RectangleF(panel.X + 18, panel.Y + 52, panel.Width - 36, 38));
        graphics.DrawString("Guarda el estado o vuelve al menú para iniciar otra expedición.",
            textFont, textBrush, new RectangleF(panel.X + 18, panel.Y + 95, panel.Width - 36, 32));
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
        int panelWidth = Math.Max(165, Math.Min(210, ClientSize.Width / 2 - 24));
        int x = ClientSize.Width - panelWidth - 12;
        var hud = new Rectangle(x, 12, panelWidth, 184);
        graphics.FillRectangle(panelBrush, hud);
        graphics.DrawRectangle(borderPen, hud);
        graphics.DrawString("NEXUS // EXPLORACIÓN", titleFont, titleBrush, x + 8, 18);
        DrawMiniBar(graphics, x + 8, 42, "O₂", _oxygen, Color.FromArgb(55, 213, 247), textFont, textBrush);
        DrawMiniBar(graphics, x + 8, 62, "VIDA", _hull, Color.FromArgb(255, 98, 93), textFont, textBrush,
            showPoints: true);
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
        graphics.DrawString("ARMA: PULSO IÓNICO  //  50 D", textFont, titleBrush, x + 8, 159);

        int mapY = hud.Bottom + 7;
        int mapHeight = Math.Min(132, Math.Max(70, ClientSize.Height - mapY - 44));
        var map = new Rectangle(x, mapY, panelWidth, mapHeight);
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
        Color color, Font font, Brush textBrush, bool showPoints = false)
    {
        graphics.DrawString(label, font, textBrush, x, y);
        int barX = x + 48;
        using var track = new SolidBrush(Color.FromArgb(37, 57, 75));
        using var fill = new SolidBrush(color);
        graphics.FillRectangle(track, barX, y + 3, 74, 8);
        graphics.FillRectangle(fill, barX, y + 3, (int)(74 * value / 100f), 8);
        graphics.DrawString(showPoints ? $"{value} PT" : $"{value}%", font, textBrush, barX + 78, y);
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
            foreach (Rectangle corridor in Corridors)
            {
                graphics.FillRectangle(hallBrush, mapX + corridor.X * scaleX, mapY + corridor.Y * scaleY,
                    corridor.Width * scaleX, corridor.Height * scaleY);
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

        foreach (EnemyActor enemy in _enemies)
        {
            string roomId = GetRoomIdAtTile((int)(enemy.Save.X / TileSize), (int)(enemy.Save.Y / TileSize)) ?? string.Empty;
            if (!State.ExploredRooms.Contains(roomId, StringComparer.OrdinalIgnoreCase)) continue;
            using var threatBrush = new SolidBrush(enemy.Save.Alerted
                ? Color.FromArgb(255, 83, 66)
                : Color.FromArgb(188, 116, 76));
            graphics.FillEllipse(threatBrush, mapX + enemy.Save.X / TileSize * scaleX - 2.5f,
                mapY + enemy.Save.Y / TileSize * scaleY - 2.5f, 5, 5);
        }
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

    private float DistanceToWorld(float x, float y)
    {
        float dx = State.PlayerX - x;
        float dy = State.PlayerY - y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

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
        DoorSpec? door = Doors.FirstOrDefault(candidate => candidate.X == x && candidate.Y == y);
        if (door is not null) return door.RoomId;
        return Corridors.Any(corridor => corridor.Contains(x, y)) ? "central_corridor" : null;
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
        State.Enemies ??= new List<EnemySaveState>();
        foreach (EnemySaveState enemy in State.Enemies)
        {
            enemy.Id ??= string.Empty;
            enemy.X = Math.Clamp(enemy.X, TileSize * 2f, (MapColumns - 2) * TileSize);
            enemy.Y = Math.Clamp(enemy.Y, TileSize * 2f, (MapRows - 2) * TileSize);
        }

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

    private static StationFixture Fixture(string roomId, int tileX, int tileY, int tileWidth, string kind)
    {
        float width = tileWidth * TileSize - 10f;
        float height = 30f;
        float x = tileX * TileSize + (TileSize - width) / 2f;
        float y = tileY * TileSize + (TileSize - height) / 2f;
        return new StationFixture(roomId, new RectangleF(x, y, width, height), kind);
    }

    private sealed record StationFixture(string RoomId, RectangleF Bounds, string Kind);
    private sealed record EnemySpawn(string Id, string Name, string Kind, int TileX, int TileY, float Speed);

    private sealed class EnemyActor
    {
        public string Id { get; }
        public string Name { get; }
        public string Kind { get; }
        public float Speed { get; }
        public EnemySaveState Save { get; }
        public float Phase { get; }
        public float AttackCooldown { get; set; }
        public float HitFlashRemaining { get; set; }
        public float NextX { get; set; } = -1;
        public float NextY { get; set; } = -1;

        public EnemyActor(string id, string name, string kind, float speed, EnemySaveState save)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Speed = speed;
            Save = save;
            Phase = (id.GetHashCode(StringComparison.Ordinal) & 0x7fffffff) % 100 / 10f;
        }
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
