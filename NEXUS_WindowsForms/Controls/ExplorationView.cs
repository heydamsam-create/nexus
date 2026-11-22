using NEXUS_WindowsForms.Models;
using NEXUS_WindowsForms.Rendering;

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
    private const int MapRows = 26;
    private const float PlayerRadius = 12f;
    private const float MovementSpeed = 210f;

    private readonly bool[,] _walls = new bool[MapColumns, MapRows];
    private readonly HashSet<string> _unlockedDoors = new(StringComparer.OrdinalIgnoreCase);
    private readonly RectangleF[] _furniture =
    {
        new(3 * TileSize + 5, 3 * TileSize + 4, 33, 26),
        new(9 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(17 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(24 * TileSize + 4, 3 * TileSize + 4, 34, 26),
        new(3 * TileSize + 4, 15 * TileSize + 4, 34, 28),
        new(10 * TileSize + 4, 15 * TileSize + 4, 34, 28),
        new(17 * TileSize + 4, 15 * TileSize + 4, 34, 28),
        new(22 * TileSize + 4, 19 * TileSize + 4, 34, 28)
    };
    private readonly ExplorerSpriteSheet _sprites = new();
    private readonly Bitmap _floorTile = CreateFloorTexture();
    private float _cameraX;
    private float _cameraY;
    private float _animationClock;
    private bool _isMoving;
    private float _interactionTimer;
    private ExplorerDirection _facing = ExplorerDirection.Down;
    private int _oxygen = 100;
    private int _hull = 100;
    private int _data;
    private string _statusMessage = "Explora la nave. Acércate a un marcador y pulsa E.";

    private readonly WorldObject[] _objects =
    {
        new("ship_log", "REGISTRO DE NAVE", 5, 5, "Recuperar el registro de navegación", 0, 0, 8),
        new("signal_terminal", "TERMINAL DE SEÑAL", 20, 5, "Descifrar la señal de Eridani-9", 0, 0, 12),
        new("access_card", "TARJETA DE ACCESO", 5, 17, "Recoger la tarjeta de acceso", 0, 0, 0),
        new("oxygen_cell", "CÉLULA DE OXÍGENO", 8, 17, "Recoger una célula de oxígeno", 18, 0, 0),
        new("security_console", "CONSOLA DE SEGURIDAD", 23, 8, "Activar el mecanismo de seguridad", 0, -4, 5),
        new("anomaly_terminal", "NÚCLEO DE DATOS", 25, 17, "Asegurar los datos del núcleo", 0, 0, 18)
    };

    public ExplorationSaveState State { get; private set; }
    public event EventHandler? StateChanged;
    public event EventHandler<ExplorationActionEventArgs>? ActionCompleted;

    public ExplorationView(ExplorationSaveState? state = null)
    {
        State = state ?? new ExplorationSaveState();
        State.CollectedItems ??= new List<string>();
        State.ExploredRooms ??= new List<string>();
        State.CompletedObjectives ??= new List<string>();
        State.PlayerX = Math.Clamp(State.PlayerX, TileSize * 2f, (MapColumns - 2) * TileSize);
        State.PlayerY = Math.Clamp(State.PlayerY, TileSize * 2f, (MapRows - 2) * TileSize);
        BuildMap();
        ApplySavedDoorState();
        BackColor = Color.FromArgb(4, 11, 24);
        ForeColor = NexusTheme.Text;
        DoubleBuffered = true;
        TabStop = false;
        MinimumSize = new Size(420, 300);
    }

    public string ObjectiveSummary =>
        $"{ObjectiveMark("signal_terminal")} Descifrar señal\r\n" +
        $"{ObjectiveMark("access_card")} Obtener tarjeta\r\n" +
        $"{ObjectiveMark("security_console")} Activar mecanismo\r\n" +
        $"{ObjectiveMark("anomaly_terminal")} Asegurar núcleo";

    public bool MovePlayer(int horizontal, int vertical, float elapsedSeconds)
    {
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
        State.CollectedItems ??= new List<string>();
        State.ExploredRooms ??= new List<string>();
        State.CompletedObjectives ??= new List<string>();
        State.PlayerX = Math.Clamp(State.PlayerX, TileSize * 2f, (MapColumns - 2) * TileSize);
        State.PlayerY = Math.Clamp(State.PlayerY, TileSize * 2f, (MapRows - 2) * TileSize);
        _unlockedDoors.Clear();
        BuildMap();
        ApplySavedDoorState();
        Invalidate();
    }

    public void Interact()
    {
        _interactionTimer = 0.55f;
        _animationClock += 0.03f;
        WorldObject? nearby = _objects
            .Where(item => !State.CollectedItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
            .OrderBy(item => DistanceTo(item))
            .FirstOrDefault();

        float doorDistance = DistanceTo(21, 13);
        if (doorDistance <= 68f && !_unlockedDoors.Contains("anomaly_door"))
        {
            _statusMessage = "PUERTA BLOQUEADA: valida primero la consola de seguridad del laboratorio.";
            Invalidate();
            return;
        }

        if (nearby is null || DistanceTo(nearby) > 68f)
        {
            _statusMessage = "No hay ningún objeto o terminal al alcance.";
            Invalidate();
            return;
        }

        if (nearby.Id == "security_console")
        {
            bool hasTerminal = State.CompletedObjectives.Contains("signal_terminal", StringComparer.OrdinalIgnoreCase);
            bool hasCard = State.CollectedItems.Contains("access_card", StringComparer.OrdinalIgnoreCase);
            if (!hasTerminal || !hasCard)
            {
                _statusMessage = "La consola requiere la señal descifrada y la tarjeta de acceso.";
                Invalidate();
                return;
            }
        }

        State.CollectedItems.Add(nearby.Id);
        if (nearby.Id is "signal_terminal" or "access_card" or "security_console" or "anomaly_terminal")
        {
            CompleteObjective(nearby.Id);
        }
        if (nearby.Id == "security_console")
        {
            _unlockedDoors.Add("anomaly_door");
            SetSecurityDoorOpen(true);
            State.SecurityDoorUnlocked = true;
            CompleteObjective("security_door");
        }

        _statusMessage = nearby.Id switch
        {
            "ship_log" => "Registro de navegación recuperado.",
            "signal_terminal" => "Señal descifrada. El acceso de seguridad ya puede validarse.",
            "access_card" => "Tarjeta de acceso recogida.",
            "oxygen_cell" => "Célula de oxígeno recogida: +18%.",
            "security_console" => "Mecanismo activado. Se habilitó el control del núcleo.",
            "anomaly_terminal" => "Datos del núcleo asegurados. Exploración completada.",
            _ => "Interacción completada."
        };

        ActionCompleted?.Invoke(this, new ExplorationActionEventArgs(
            nearby.Title,
            nearby.OxygenChange,
            nearby.HullChange,
            nearby.DataChange,
            nearby.Id == "anomaly_terminal"));
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
        }
        base.Dispose(disposing);
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
        DrawFurniture(graphics);
        DrawObjects(graphics);
        DrawPlayer(graphics);

        graphics.ResetTransform();
        DrawOverlay(graphics);
    }

    private void BuildMap()
    {
        for (int x = 0; x < MapColumns; x++)
        for (int y = 0; y < MapRows; y++)
            _walls[x, y] = true;

        CarveRoom(2, 2, 10, 8);
        CarveRoom(16, 2, 10, 8);
        CarveRoom(2, 14, 10, 8);
        CarveRoom(16, 14, 10, 8);
        CarveRoom(12, 5, 4, 3);
        CarveRoom(5, 10, 3, 4);
        CarveRoom(20, 10, 3, 4);

        // El cierre entre el pasillo de investigación y el núcleo solo se abre tras cumplir requisitos.
        for (int x = 20; x <= 22; x++)
        {
            _walls[x, 13] = true;
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
        using var roomBrush = new SolidBrush(Color.FromArgb(16, 36, 59));
        using var wallBrush = new SolidBrush(Color.FromArgb(24, 46, 69));
        using var gridPen = new Pen(Color.FromArgb(38, 73, 98));
        using var edgePen = new Pen(Color.FromArgb(40, 171, 207), 2);
        using var innerWallPen = new Pen(Color.FromArgb(16, 34, 52), 2);
        using var lightPen = new Pen(Color.FromArgb(85, 38, 201, 235), 3);
        using var boltBrush = new SolidBrush(Color.FromArgb(119, 151, 174));
        using var font = new Font("Segoe UI", 9f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.FromArgb(125, 172, 199));

        graphics.FillRectangle(floorBrush, 0, 0, MapColumns * TileSize, MapRows * TileSize);
        FillRoom(graphics, roomBrush, 2, 2, 10, 8);
        FillRoom(graphics, roomBrush, 16, 2, 10, 8);
        FillRoom(graphics, roomBrush, 2, 14, 10, 8);
        FillRoom(graphics, roomBrush, 16, 14, 10, 8);

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
                graphics.DrawImageUnscaled(_floorTile, tile.Location);
                graphics.DrawRectangle(gridPen, tile);
                if ((x + y) % 5 == 0)
                {
                    graphics.DrawLine(lightPen, tile.X + 8, tile.Bottom - 5, tile.Right - 8, tile.Bottom - 5);
                }
            }
        }

        DrawRoomTitle(graphics, font, labelBrush, "PUENTE", 3, 2);
        DrawRoomTitle(graphics, font, labelBrush, "LABORATORIO", 17, 2);
        DrawRoomTitle(graphics, font, labelBrush, "ALMACÉN", 3, 14);
        DrawRoomTitle(graphics, font, labelBrush, "CÁMARA DEL NÚCLEO", 17, 14);

        DrawSecurityDoor(graphics, font);
    }

    private static Bitmap CreateFloorTexture()
    {
        var texture = new Bitmap(TileSize, TileSize);
        using Graphics graphics = Graphics.FromImage(texture);
        graphics.Clear(Color.FromArgb(15, 31, 49));
        using var seamPen = new Pen(Color.FromArgb(31, 61, 81), 1);
        using var panelBrush = new SolidBrush(Color.FromArgb(18, 39, 57));
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

    private void DrawSecurityDoor(Graphics graphics, Font font)
    {
        Rectangle door = new(20 * TileSize, 13 * TileSize, 3 * TileSize, TileSize);
        bool open = _unlockedDoors.Contains("anomaly_door");
        using var doorBrush = new SolidBrush(open ? Color.FromArgb(21, 93, 82) : Color.FromArgb(50, 39, 54));
        using var doorPen = new Pen(open ? Color.FromArgb(65, 226, 185) : Color.FromArgb(241, 151, 85), 2);
        using var insetBrush = new SolidBrush(open ? Color.FromArgb(30, 157, 128) : Color.FromArgb(107, 66, 62));
        graphics.FillRectangle(doorBrush, door);
        graphics.DrawRectangle(doorPen, door);
        for (int panel = 0; panel < 3; panel++)
        {
            Rectangle inset = new(door.X + 10 + panel * 43, door.Y + 6, 34, door.Height - 12);
            graphics.FillRectangle(insetBrush, inset);
            graphics.DrawRectangle(doorPen, inset);
        }
        graphics.DrawString(open ? "ACCESO DESBLOQUEADO" : "CIERRE DE SEGURIDAD", font,
            Brushes.White, door.X + 15, door.Y + 13);
    }

    private void DrawFurniture(Graphics graphics)
    {
        using var shellBrush = new SolidBrush(Color.FromArgb(30, 54, 76));
        using var insetBrush = new SolidBrush(Color.FromArgb(8, 21, 35));
        using var edgePen = new Pen(Color.FromArgb(63, 111, 138), 1.4f);
        using var glowPen = new Pen(Color.FromArgb(130, 40, 214, 244), 2);
        using var amberBrush = new SolidBrush(Color.FromArgb(232, 166, 75));
        foreach (RectangleF piece in _furniture)
        {
            Rectangle rect = Rectangle.Round(piece);
            graphics.FillRectangle(shellBrush, rect);
            graphics.DrawRectangle(edgePen, rect);
            graphics.FillRectangle(insetBrush, rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 9);
            graphics.DrawLine(glowPen, rect.X + 7, rect.Y + 7, rect.Right - 7, rect.Y + 7);
            for (int lamp = 0; lamp < 3; lamp++)
            {
                Brush light = lamp == 0 ? amberBrush : Brushes.Cyan;
                graphics.FillEllipse(light, rect.X + 8 + lamp * 7, rect.Bottom - 7, 3, 3);
            }
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
            bool objective = item.Id is "signal_terminal" or "access_card" or "security_console" or "anomaly_terminal";
            RectangleF glow = new(x - 17 - pulse, y - 17 - pulse, 34 + pulse * 2, 34 + pulse * 2);
            graphics.FillEllipse(objectGlow, glow);
            DrawInteractable(graphics, item.Id, x, y, objective ? markerBrush : doneBrush, markerPen);
            graphics.DrawString(item.Title, labelFont, labelBrush, x - 52, y + 26);
            if (DistanceTo(item) <= 94f)
            {
                graphics.DrawEllipse(interactionPen, x - 21, y - 21, 42, 42);
            }
        }

        if (_unlockedDoors.Contains("anomaly_door"))
        {
            graphics.DrawString("ACCESO ABIERTO", labelFont, Brushes.White,
                20 * TileSize + 28, 13 * TileSize + 27);
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
                RectangleF console = new(x - 13, y - 13, 26, 25);
                graphics.FillRectangle(shell, console);
                graphics.DrawRectangle(trim, console.X, console.Y, console.Width, console.Height);
                graphics.FillRectangle(markerBrush, x - 9, y - 10, 18, 11);
                graphics.DrawLine(Pens.Cyan, x - 7, y - 7, x + 6, y - 7);
                graphics.DrawLine(Pens.Cyan, x - 6, y - 3, x + 4, y - 3);
                graphics.FillRectangle(shell, x - 3, y + 12, 6, 4);
                break;
            case "ship_log":
                graphics.FillRectangle(shell, x - 12, y - 9, 24, 18);
                graphics.DrawRectangle(trim, x - 12, y - 9, 24, 18);
                graphics.FillRectangle(markerBrush, x - 8, y - 5, 16, 10);
                graphics.DrawLine(Pens.White, x - 5, y - 2, x + 4, y - 2);
                graphics.DrawLine(Pens.White, x - 5, y + 1, x + 5, y + 1);
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
        graphics.DrawString("W A S D / FLECHAS: MOVER     E: INTERACTUAR     " + prompt,
            textFont, textBrush, help.X + 8, help.Y + 4);
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

        int completed = State.CompletedObjectives.Count(id => id is "signal_terminal" or "access_card" or "security_console" or "anomaly_terminal");
        string objective = completed switch
        {
            0 => "OBJ: DESCIFRAR SEÑAL",
            1 => State.CollectedItems.Contains("access_card") ? "OBJ: ACTIVAR CONSOLA" : "OBJ: OBTENER TARJETA",
            2 => "OBJ: ACTIVAR MECANISMO",
            3 => "OBJ: ASEGURAR NÚCLEO",
            _ => "OBJ: EXPLORACIÓN COMPLETA"
        };
        graphics.DrawString(objective, textFont, textBrush, x + 8, 106);
        string inventory = State.CollectedItems.Count == 0
            ? "INV: VACÍO"
            : $"INV: {State.CollectedItems.Count} OBJ.  •  {(State.SecurityDoorUnlocked ? "TARJETA USADA" : State.CollectedItems.Contains("access_card") ? "TARJETA" : "EQUIPO")}";
        graphics.DrawString(inventory, textFont, textBrush, x + 8, 126);

        var map = new Rectangle(ClientSize.Width - 176, 176, 164, 105);
        graphics.FillRectangle(panelBrush, map);
        graphics.DrawRectangle(borderPen, map);
        DrawMiniMap(graphics, map);
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
        for (int cx = 0; cx < MapColumns; cx++)
        for (int cy = 0; cy < MapRows; cy++)
        {
            if (_walls[cx, cy]) continue;
            graphics.FillRectangle((cx is >= 2 and < 12 && cy is >= 2 and < 10) ||
                (cx is >= 16 and < 26 && cy is >= 2 and < 10) ||
                (cx is >= 2 and < 12 && cy is >= 14 and < 22) ||
                (cx is >= 16 and < 26 && cy is >= 14 and < 22) ? roomBrush : hallBrush,
                mapX + cx * scaleX, mapY + cy * scaleY, Math.Max(1, scaleX), Math.Max(1, scaleY));
        }
        graphics.FillEllipse(playerBrush, mapX + State.PlayerX / TileSize * scaleX - 3,
            mapY + State.PlayerY / TileSize * scaleY - 3, 6, 6);
    }

    private string GetPrompt()
    {
        WorldObject? nearby = _objects
            .Where(item => !State.CollectedItems.Contains(item.Id, StringComparer.OrdinalIgnoreCase))
            .OrderBy(DistanceTo)
            .FirstOrDefault();
        if (nearby is not null && DistanceTo(nearby) <= 68f)
        {
            return $"E: {nearby.Title}";
        }
        return DistanceTo(21, 13) <= 68f && !_unlockedDoors.Contains("anomaly_door")
            ? "E: PUERTA DE SEGURIDAD"
            : string.Empty;
    }

    private string CurrentRoomName()
    {
        int tileX = (int)(State.PlayerX / TileSize);
        int tileY = (int)(State.PlayerY / TileSize);
        if (tileX is >= 2 and < 12 && tileY is >= 2 and < 10) return "PUENTE DE NEXUS";
        if (tileX is >= 16 and < 26 && tileY is >= 2 and < 10) return "LABORATORIO DE SEÑALES";
        if (tileX is >= 2 and < 12 && tileY is >= 14 and < 22) return "ALMACÉN";
        if (tileX is >= 16 and < 26 && tileY is >= 14 and < 22) return "CÁMARA DEL NÚCLEO";
        return "PASILLO DE SERVICIO";
    }

    private bool DiscoverCurrentRoom()
    {
        string roomId = CurrentRoomName();
        if (!State.ExploredRooms.Contains(roomId, StringComparer.OrdinalIgnoreCase))
        {
            State.ExploredRooms.Add(roomId);
            _statusMessage = $"Nueva zona cartografiada: {roomId}.";
            return true;
        }

        return false;
    }

    private float DistanceTo(WorldObject item) => DistanceTo(item.X, item.Y);

    private float DistanceTo(int tileX, int tileY)
    {
        float x = tileX * TileSize + TileSize / 2f;
        float y = tileY * TileSize + TileSize / 2f;
        float dx = State.PlayerX - x;
        float dy = State.PlayerY - y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private string ObjectiveMark(string id) =>
        State.CompletedObjectives.Contains(id, StringComparer.OrdinalIgnoreCase) ? "[✓]" : "[ ]";

    private void CompleteObjective(string id)
    {
        if (!State.CompletedObjectives.Contains(id, StringComparer.OrdinalIgnoreCase))
        {
            State.CompletedObjectives.Add(id);
        }
    }

    private void SetSecurityDoorOpen(bool isOpen)
    {
        for (int x = 20; x <= 22; x++)
        {
            _walls[x, 13] = !isOpen;
        }
    }

    private void ApplySavedDoorState()
    {
        if (State.SecurityDoorUnlocked)
        {
            _unlockedDoors.Add("anomaly_door");
            SetSecurityDoorOpen(true);
        }
    }

    private sealed record WorldObject(string Id, string Title, int X, int Y, string Objective,
        int OxygenChange, int HullChange, int DataChange);
}
