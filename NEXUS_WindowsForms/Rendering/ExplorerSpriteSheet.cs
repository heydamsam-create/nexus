namespace NEXUS_WindowsForms.Rendering;

internal enum ExplorerDirection
{
    Down,
    Left,
    Right,
    Up
}

/// <summary>Sprites de explorador espacial dibujados una sola vez y reutilizados en cada fotograma.</summary>
internal sealed class ExplorerSpriteSheet : IDisposable
{
    private const int SpriteWidth = 40;
    private const int SpriteHeight = 52;
    private readonly Bitmap[,] _walking = new Bitmap[4, 3];
    private readonly Bitmap[] _idle = new Bitmap[4];
    private readonly Bitmap[] _interacting = new Bitmap[4];

    public ExplorerSpriteSheet()
    {
        for (int direction = 0; direction < 4; direction++)
        {
            _idle[direction] = CreateSprite((ExplorerDirection)direction, 0, false);
            _interacting[direction] = CreateSprite((ExplorerDirection)direction, 2, true);
            for (int frame = 0; frame < 3; frame++)
                _walking[direction, frame] = CreateSprite((ExplorerDirection)direction, frame, false);
        }
    }

    public Bitmap Get(ExplorerDirection direction, bool moving, int frame, bool interacting)
    {
        int index = (int)direction;
        if (interacting) return _interacting[index];
        return moving ? _walking[index, frame % 3] : _idle[index];
    }

    public void Dispose()
    {
        foreach (Bitmap bitmap in _idle) bitmap.Dispose();
        foreach (Bitmap bitmap in _interacting) bitmap.Dispose();
        foreach (Bitmap bitmap in _walking) bitmap.Dispose();
    }

    private static Bitmap CreateSprite(ExplorerDirection direction, int frame, bool interacting)
    {
        var sprite = new Bitmap(SpriteWidth, SpriteHeight);
        using Graphics graphics = Graphics.FromImage(sprite);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        float stride = frame switch { 1 => 3.5f, 2 => -3.5f, _ => 0f };
        if (!interacting)
        {
            using var shadowBrush = new SolidBrush(Color.FromArgb(72, 0, 0, 0));
            graphics.FillEllipse(shadowBrush, 6, 42, 28, 8);
        }

        using var suitBrush = new SolidBrush(Color.FromArgb(205, 220, 231));
        using var suitShadowBrush = new SolidBrush(Color.FromArgb(74, 103, 125));
        using var trimBrush = new SolidBrush(Color.FromArgb(49, 205, 229));
        using var darkBrush = new SolidBrush(Color.FromArgb(20, 39, 60));
        using var visorBrush = new SolidBrush(Color.FromArgb(12, 42, 64));
        using var visorGlowBrush = new SolidBrush(Color.FromArgb(70, 228, 255));
        using var outlinePen = new Pen(Color.FromArgb(123, 172, 194), 1.5f);
        using var trimPen = new Pen(Color.FromArgb(49, 205, 229), 2.2f);

        bool side = direction is ExplorerDirection.Left or ExplorerDirection.Right;
        float bodyX = side ? 12 : 10;
        float bodyWidth = side ? 18 : 20;
        if (direction is ExplorerDirection.Up or ExplorerDirection.Left or ExplorerDirection.Right)
        {
            float packX = direction == ExplorerDirection.Left ? 24 : direction == ExplorerDirection.Right ? 7 : 12;
            graphics.FillRectangle(suitShadowBrush, packX, 23, 12, 17);
            graphics.DrawRectangle(outlinePen, packX, 23, 12, 17);
            graphics.DrawLine(trimPen, packX + 3, 27, packX + 9, 27);
        }

        float leftLegX = 15 - (frame == 1 ? stride : frame == 2 ? -stride : 0);
        float rightLegX = 25 + (frame == 1 ? stride : frame == 2 ? -stride : 0);
        if (interacting) { leftLegX -= 1; rightLegX += 1; }
        using var legPen = new Pen(Color.FromArgb(188, 207, 219), 6)
            { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        using var bootPen = new Pen(Color.FromArgb(24, 42, 61), 7)
            { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        graphics.DrawLine(legPen, 17, 34, leftLegX, 42);
        graphics.DrawLine(legPen, 24, 34, rightLegX, 42);
        graphics.DrawLine(bootPen, leftLegX, 42, leftLegX + 1, 46);
        graphics.DrawLine(bootPen, rightLegX, 42, rightLegX + 1, 46);

        using var torsoPath = RoundedRectangle(bodyX, 22, bodyWidth, 17, 4);
        graphics.FillPath(suitBrush, torsoPath);
        graphics.DrawPath(outlinePen, torsoPath);
        graphics.FillRectangle(darkBrush, bodyX + 5, 28, bodyWidth - 10, 5);
        graphics.FillRectangle(trimBrush, bodyX + 6, 29, 5, 3);
        graphics.FillRectangle(trimBrush, bodyX + bodyWidth - 10, 29, 5, 3);

        using var armPen = new Pen(Color.FromArgb(207, 222, 233), 5)
            { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        float armSwing = frame == 1 ? stride : frame == 2 ? -stride : 0;
        if (interacting)
        {
            switch (direction)
            {
                case ExplorerDirection.Up: graphics.DrawLine(armPen, 16, 25, 16, 17); break;
                case ExplorerDirection.Down: graphics.DrawLine(armPen, 24, 25, 24, 17); break;
                case ExplorerDirection.Left: graphics.DrawLine(armPen, 14, 26, 8, 22); break;
                case ExplorerDirection.Right: graphics.DrawLine(armPen, 26, 26, 32, 22); break;
            }
        }
        else
        {
            graphics.DrawLine(armPen, bodyX + 2, 25, bodyX - 1, 32 + armSwing);
            graphics.DrawLine(armPen, bodyX + bodyWidth - 2, 25, bodyX + bodyWidth + 1, 32 - armSwing);
        }

        graphics.FillEllipse(suitBrush, 10, 4, 20, 21);
        graphics.DrawEllipse(outlinePen, 10, 4, 20, 21);
        switch (direction)
        {
            case ExplorerDirection.Down:
                graphics.FillEllipse(visorBrush, 12, 11, 16, 9);
                graphics.DrawArc(trimPen, 13, 10, 14, 11, 185, 170);
                graphics.FillRectangle(visorGlowBrush, 15, 15, 8, 2);
                break;
            case ExplorerDirection.Left:
                graphics.FillEllipse(visorBrush, 10, 10, 10, 12);
                graphics.FillRectangle(visorGlowBrush, 11, 14, 5, 2);
                break;
            case ExplorerDirection.Right:
                graphics.FillEllipse(visorBrush, 20, 10, 10, 12);
                graphics.FillRectangle(visorGlowBrush, 24, 14, 5, 2);
                break;
            case ExplorerDirection.Up:
                graphics.DrawArc(trimPen, 13, 8, 14, 8, 190, 160);
                graphics.FillEllipse(trimBrush, 18, 11, 4, 3);
                break;
        }
        graphics.FillRectangle(trimBrush, 18, 5, 4, 2);
        if (interacting)
        {
            using var pulsePen = new Pen(Color.FromArgb(175, 77, 234, 255), 2);
            graphics.DrawEllipse(pulsePen, 4, 3, 32, 42);
        }
        return sprite;
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRectangle(float x, float y, float width,
        float height, float radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        float diameter = radius * 2;
        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
