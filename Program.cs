using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

// 2D Platformer �V Single-file WinForms prototype
// �ؼСG�i�]�ʡB���D�B��T�b�V�I���B²��ĤH�P�������B�򥻰ʵe�ɧǡC
// �ظm�G.NET 6/7/8 Windows�BWinForms�C�⦹�ɩR�W�� Program.cs �����ظm����Y�i�C

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new GameForm());
    }
}

public class GameForm : Form
{
    const int WIDTH = 960;
    const int HEIGHT = 540;

    readonly System.Windows.Forms.Timer _timer;
    readonly Stopwatch _clock = new Stopwatch();

    // Input
    readonly HashSet<Keys> _keys = new HashSet<Keys>();

    // World
    readonly List<RectangleF> _platforms = new();
    readonly List<Patroller> _enemies = new();
    readonly List<RectangleF> _coins = new();

    // Player
    readonly Player _player = new Player(new PointF(80, 200));

    // �ۭq�����I
    PointF? _customRespawn = null;
    bool _spacePressed = false;

    int _collected = 0;

    // Camera
    float _camX = 0f; // �������H

    // Sprites (�i��������ڹϤ�)
    readonly Font _hudFont = new Font("Consolas", 14, FontStyle.Bold);

    public GameForm()
    {
        Text = "2D Platformer (WinForms Minimal)";
        ClientSize = new Size(WIDTH, HEIGHT);
        DoubleBuffered = true; // ��ְ{�{
        BackColor = Color.FromArgb(30, 32, 40);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        // ���d�G²�� tile/���x�A�H world space ���
        BuildLevel();

        // �p�ɾ��G60FPS �ؼ�
        _timer = new System.Windows.Forms.Timer { Interval = 1000 / 60 };
        _timer.Tick += (s, e) =>
        {
            if (!_clock.IsRunning) _clock.Start();
            float dt = (float)_clock.Elapsed.TotalSeconds;
            _clock.Restart();
            UpdateGame(dt);
            Invalidate();
        };
        _timer.Start();

        // ��J
        KeyDown += (s, e) => _keys.Add(e.KeyCode);
        KeyUp += (s, e) => _keys.Remove(e.KeyCode);

        // ���� Alt Ĳ�o�t���n
        KeyPreview = true;
    }

    void BuildLevel()
    {
        // �򩳦a��
        _platforms.Add(new RectangleF(0, 400, 3000, 40));

        // ���C���x
        _platforms.Add(new RectangleF(200, 340, 140, 20));
        _platforms.Add(new RectangleF(380, 300, 160, 20));
        _platforms.Add(new RectangleF(620, 260, 160, 20));
        _platforms.Add(new RectangleF(860, 320, 160, 20));
        _platforms.Add(new RectangleF(1100, 280, 160, 20));
        _platforms.Add(new RectangleF(1400, 240, 200, 20));

        // ��
        _platforms.Add(new RectangleF(560, 200, 20, 200));

        // �ĤH�]���k���ޡ^
        _enemies.Add(new Patroller(new RectangleF(360, 280, 40, 20), 320, 520, 60f));
        _enemies.Add(new Patroller(new RectangleF(1080, 260, 40, 20), 1040, 1240, 80f));

        // ������
        _coins.Add(new RectangleF(210, 310, 14, 14));
        _coins.Add(new RectangleF(410, 270, 14, 14));
        _coins.Add(new RectangleF(650, 230, 14, 14));
        _coins.Add(new RectangleF(890, 290, 14, 14));
        _coins.Add(new RectangleF(1140, 250, 14, 14));
        _coins.Add(new RectangleF(1460, 210, 14, 14));
    }

    Random _rng = new Random();
    private float _lastGeneratedX=1600;

    void GenerateNextChunk(float startX)
    {
        const float MOVE_SPEED = 120;
        const float JUMP_VY = -280;
        const float GRAVITY = 600;

        float jumpTime = (Math.Abs(JUMP_VY) / GRAVITY) * 2;
        float maxJumpX = MOVE_SPEED * jumpTime * 0.8f;
        float maxUp = (JUMP_VY * JUMP_VY) / (2 * GRAVITY)-20;

        int platformCount = _rng.Next(2, 5);
        float lastPlatformX = startX;
        float lastPlatformY = 400 - _rng.Next(0, 120);

        for (int i = 0; i < platformCount; i++)
        {
            float gap = _rng.Next(80, (int)maxJumpX);
            float nextX = lastPlatformX + gap;

            float deltaY = (float)Math.Sin(_rng.Next(0, 100000)) *((int)maxUp);
            float nextY = Math.Clamp(lastPlatformY + deltaY, 180, 380);

            float width = _rng.Next(80, 200);
            var plat = new RectangleF(nextX, nextY, width, 20);
            _platforms.Add(plat);

            if (_rng.NextDouble() < 0.5)
            {
                float patrolLeft = nextX;
                float patrolRight = nextX + width - 40;
                if (patrolRight > patrolLeft + 40)
                    _enemies.Add(new Patroller(new RectangleF(nextX + 20, nextY - 20, 40, 20),
                                               patrolLeft, patrolRight, 60f));
            }

            if (_rng.NextDouble() < 0.3)
            {
                _coins.Add(new RectangleF(nextX + width / 2, nextY - 30, 14, 14));
            }

            lastPlatformX = nextX + width;
            lastPlatformY = nextY;
        }

        _lastGeneratedX = lastPlatformX;
    }
    void UpdateGame(float dt)
    {
        // Input �� ����t��
        float ax = 0f;
        if (_keys.Contains(Keys.Left) || _keys.Contains(Keys.A)) ax -= 1f;
        if (_keys.Contains(Keys.Right) || _keys.Contains(Keys.D)) ax += 1f;

        bool pressedJump =  _keys.Contains(Keys.Up) || _keys.Contains(Keys.W);

        _player.Update(dt, ax, pressedJump, _platforms);

        // �ĤH���� & �P���a�I��
        foreach (var e in _enemies)
        {
            e.Update(dt, _platforms);
            if (RectIntersects(_player.Bounds, e.Bounds))
            {
                // �q�W����~�����ѡA�_�h���ˡ]²�Ʀ��^��_�I�^
                if (_player.Velocity.Y > 0 && _player.Bounds.Bottom - e.Bounds.Top < 14)
                {
                    _player.Bounce(180f);
                    e.Alive = false;
                    _collected++;
                }
                else
                {
                    Respawn();
                    return;
                }
                

            }
        }
        _enemies.RemoveAll(en => !en.Alive);

        // ������
        for (int i = _coins.Count - 1; i >= 0; --i)
        {
            if (RectIntersects(_player.Bounds, _coins[i]))
            {
                _coins.RemoveAt(i);
                _collected++;
            }
        }

        // ²�����Y���H�]�����^
        float targetCam = _player.Position.X - WIDTH * 0.4f;
        _camX += (targetCam - _camX) * MathF.Min(1f, dt * 8f);
        _camX = MathF.Max(0, _camX);
        if (_camX > _lastGeneratedX -1000)
        {
            GenerateNextChunk(_lastGeneratedX);
        }
        // �إߦۭq�����I�G���� 10 ����
        // �ť���ح����I�]�קK����Ĳ�o�^
        if (_keys.Contains(Keys.Space))
        {
            if (!_spacePressed && _collected >= 10)
            {
                _customRespawn = _player.Position;
                _collected -= 10;
            }
            _spacePressed = true;
        }
        else
        {
            _spacePressed = false;
        }


        // �p�G����e���U�� �� ����
        if (_player.Bounds.Y > 700)
        {
            Respawn();
        }
    }

    void Respawn()
    {
        if (_customRespawn.HasValue)
        {
            _player.Reset(_customRespawn.Value);
        }
        else
        {
            _player.Reset(new PointF(80, 200));
            _camX = 0f;
        }
    }


    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.TranslateTransform(-_camX, 0);

        // �I���h
        DrawParallax(g);

        // ���x
        foreach (var p in _platforms)
            DrawPlatform(g, p);

        // ������
        foreach (var c in _coins)
            DrawCoin(g, c);

        // �ĤH
        foreach (var en in _enemies)
            DrawEnemy(g, en.Bounds);

        // ���a
        DrawPlayer(g, _player);

        // HUD�]�����H���Y�^
        g.ResetTransform();
        g.DrawString($"COINS: {_collected}", _hudFont, Brushes.White, 10, 10);
        g.DrawString($"ON GROUND: {_player.OnGround}", _hudFont, Brushes.White, 10, 30);
        g.DrawString($"POS: {(int)_player.Position.X},{(int)_player.Position.Y}", _hudFont, Brushes.White, 10, 50);
        g.DrawString($"FPS: ~{(int)(1000.0 / Math.Max(1, _timer.Interval))}", _hudFont, Brushes.Gray, WIDTH - 120, 10);
    }

    static bool RectIntersects(RectangleF a, RectangleF b)
        => a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;

    void DrawParallax(Graphics g)
    {
        // ²�檺���t�I��
        using var dark = new SolidBrush(Color.FromArgb(24, 26, 32));
        using var mid = new SolidBrush(Color.FromArgb(40, 44, 56));
        using var light = new SolidBrush(Color.FromArgb(58, 64, 80));

        var rc = new RectangleF(_camX, 0, WIDTH, HEIGHT);
        g.FillRectangle(dark, rc);
        // �s��
        g.TranslateTransform(-_camX * 0.4f, 0);
        g.FillPolygon(mid, new[] { new PointF(0, 360), new PointF(160, 280), new PointF(320, 360) });
        g.FillPolygon(mid, new[] { new PointF(400, 360), new PointF(560, 270), new PointF(720, 360) });
        g.FillPolygon(mid, new[] { new PointF(900, 360), new PointF(1080, 290), new PointF(1260, 360) });

        g.TranslateTransform(_camX * 0.4f, 0);
        g.TranslateTransform(-_camX * 0.2f, 0);
        g.FillRectangle(light, new RectangleF(0, 380, 2000, 2));
        g.TranslateTransform(_camX * 0.2f, 0);
    }

    void DrawPlatform(Graphics g, RectangleF r)
    {
        using var baseBrush = new SolidBrush(Color.FromArgb(90, 200, 90));
        using var edgePen = new Pen(Color.FromArgb(30, 120, 30), 2);
        g.FillRectangle(baseBrush, r);
        g.DrawRectangle(edgePen, r.X, r.Y, r.Width, r.Height);
    }

    void DrawCoin(Graphics g, RectangleF r)
    {
        using var b = new SolidBrush(Color.Gold);
        using var pen = new Pen(Color.Orange, 2);
        g.FillEllipse(b, r);
        g.DrawEllipse(pen, r);
    }

    void DrawEnemy(Graphics g, RectangleF r)
    {
        using var b = new SolidBrush(Color.FromArgb(220, 80, 80));
        g.FillRectangle(b, r);
        g.DrawRectangle(Pens.DarkRed, r.X, r.Y, r.Width, r.Height);
        // ����
        g.FillEllipse(Brushes.Black, r.X + 8, r.Y + 4, 6, 6);
        g.FillEllipse(Brushes.Black, r.Right - 14, r.Y + 4, 6, 6);
    }

    void DrawPlayer(Graphics g, Player p)
    {
        // �x�� + ²���ʵe�]�B���\�ʡ^
        var r = p.Bounds;
        using var body = new SolidBrush(Color.FromArgb(90, 160, 240));
        g.FillRectangle(body, r);
        g.DrawRectangle(Pens.SteelBlue, r.X, r.Y, r.Width, r.Height);

        // �}�B�]�̾ڰʵe�ۦ�^
        float phase = p.AnimPhase;
        float leg = MathF.Sin(phase) * 3f;
        using var pen = new Pen(Color.White, 2);
        g.DrawLine(pen, r.X + 8, r.Bottom, r.X + 8, r.Bottom + leg);
        g.DrawLine(pen, r.Right - 8, r.Bottom, r.Right - 8, r.Bottom - leg);

        // ²��¦V
        if (p.Facing >= 0)
        {
            g.FillEllipse(Brushes.White, r.Right - 14, r.Y + 6, 8, 8);
            g.FillEllipse(Brushes.Black, r.Right - 12, r.Y + 8, 4, 4);
        }
        else
        {
            g.FillEllipse(Brushes.White, r.X + 6, r.Y + 6, 8, 8);
            g.FillEllipse(Brushes.Black, r.X + 8, r.Y + 8, 4, 4);
        }
    }
}

public class Player
{
    public PointF Position;
    public SizeF Size = new SizeF(26, 36);
    public PointF Velocity; // px/s
    public bool OnGround;
    public int Facing = 1; // 1:right, -1:left
    public float AnimPhase;

    // �հ�
    const float MOVE_SPEED = 140f;
    const float AIR_ACCEL = 900f;
    const float GROUND_ACCEL = 2000f;
    const float GRAVITY = 900f;
    const float JUMP_VY = -440f;
    const float MAX_FALL = 800f;
    const float FRICTION = 8f;

    // ���D�w�� / ���a�e���]�ﵽ��P�^
    float coyoteTime = 0f; // ���a�e���ɶ�
    float jumpBuffer = 0f; // ���e�������w��

    public Player(PointF spawn)
    {
        Reset(spawn);
    }

    public void Reset(PointF spawn)
    {
        Position = spawn;
        Velocity = PointF.Empty;
        OnGround = false;
        Facing = 1;
        AnimPhase = 0f;
        coyoteTime = 0f;
        jumpBuffer = 0f;
    }

    public RectangleF Bounds => new RectangleF(Position.X - Size.Width / 2, Position.Y - Size.Height, Size.Width, Size.Height);

    public void Update(float dt, float ax, bool jumpPressed, List<RectangleF> solids)
    {
        // �˼Ƽe���P�w��
        coyoteTime = MathF.Max(0, coyoteTime - dt);
        jumpBuffer = jumpPressed ? 0.15f : MathF.Max(0, jumpBuffer - dt);

        // ���V
        if (Math.Abs(ax) > 0.01f) Facing = ax > 0 ? 1 : -1;

        // �����[�t�� + ����
        float accel = OnGround ? GROUND_ACCEL : AIR_ACCEL;
        float targetVx = ax * MOVE_SPEED;
        float vx = Lerp(Velocity.X, targetVx, 1f - MathF.Exp(-accel * dt / 1000f));

        // �B�~�a�������A�P��ɳv�B�I��
        if (OnGround && Math.Abs(ax) < 0.01f)
            vx = Lerp(vx, 0f, 1f - MathF.Exp(-FRICTION * dt));

        float vy = Velocity.Y + GRAVITY * dt;
        if (vy > MAX_FALL) vy = MAX_FALL;

        // ���D�]�䴩�e��/�w�ġ^
        if ((OnGround || coyoteTime > 0f) && jumpBuffer > 0f)
        {
            vy = JUMP_VY;
            OnGround = false;
            coyoteTime = 0f;
            jumpBuffer = 0f;
        }

        // �v�b���� + ��T����
        var newPos = new PointF(Position.X, Position.Y);

        // X �b
        newPos.X += vx * dt * 1f;
        var boundsX = new RectangleF(newPos.X - Size.Width / 2, Position.Y - Size.Height, Size.Width, Size.Height);
        ResolveCollisionsAxis(ref boundsX, solids, axisX: true);
        newPos.X = boundsX.X + Size.Width / 2;

        // Y �b
        newPos.Y += vy * dt * 1f;
        var boundsY = new RectangleF(boundsX.X, newPos.Y - Size.Height, Size.Width, Size.Height);
        bool wasOnGround = OnGround;
        OnGround = false;
        ResolveCollisionsAxis(ref boundsY, solids, axisX: false);
        if (!wasOnGround && OnGround) coyoteTime = 0.08f; // �踨�a�ֶq�e���A�ϳs����í�w
        newPos.Y = boundsY.Y + Size.Height;

        Position = newPos;
        Velocity = new PointF(vx, OnGround ? 0 : vy);

        // �ʵe�ۦ�]���ʤ~�\�ʡA�Ť���C�^
        float walkSpeed = Math.Abs(vx);
        float rate = OnGround ? (0.1f + walkSpeed / MOVE_SPEED * 6f) : 2f;
        AnimPhase += rate * dt;

        // �P��᪺�u���G�Y�V�W�t�ץB��}����A�d��t
        if (vy < 0 && !jumpPressed)
        {
            Velocity = new PointF(Velocity.X, vy * 0.98f);
        }

        // ��s jumpBuffer�]�Y��e�������A�O���w�Įɶ��^
        if (jumpPressed) jumpBuffer = 0.15f;
        

    }
    

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    void ResolveCollisionsAxis(ref RectangleF box, List<RectangleF> solids, bool axisX)
    {
        foreach (var s in solids)
        {
            if (!Intersects(box, s)) continue;
            if (axisX)
            {
                // �q���Υk���}
                if (box.X + box.Width / 2 < s.X + s.Width / 2)
                {
                    // �q�����i�J �� �a���K��
                    float overlap = (box.Right) - s.Left;
                    box.X -= overlap;
                }
                else
                {
                    float overlap = s.Right - box.Left;
                    box.X += overlap;
                }
            }
            else
            {
                // �q�W�ΤU���}
                if (box.Y + box.Height / 2 < s.Y + s.Height / 2)
                {
                    float overlap = (box.Bottom) - s.Top;
                    box.Y -= overlap;
                    OnGround = true; // �ۤW�踨�U
                }
                else
                {
                    float overlap = s.Bottom - box.Top;
                    box.Y += overlap;
                }
            }
        }
    }

    public void Bounce(float vy)
    {
        Velocity = new PointF(Velocity.X, -Math.Abs(vy));
        OnGround = false;
    }

    static bool Intersects(RectangleF a, RectangleF b)
        => a.Left < b.Right && a.Right > b.Left && a.Top < b.Bottom && a.Bottom > b.Top;
}

public class Patroller
{
    public RectangleF Bounds;
    public float Left;
    public float Right;
    public float Speed;
    public bool Alive = true;

    float dir = 1f;

    public Patroller(RectangleF rect, float left, float right, float speed)
    {
        Bounds = rect; Left = left; Right = right; Speed = speed;
    }

    public void Update(float dt, List<RectangleF> solids)
    {
        float dx = dir * Speed * dt;
        Bounds = new RectangleF(Bounds.X + dx, Bounds.Y, Bounds.Width, Bounds.Height);
        if (Bounds.Left < Left) { Bounds.X = Left; dir = 1f; }
        if (Bounds.Right > Right) { Bounds.X = Right - Bounds.Width; dir = -1f; }

        // �����O�@�G�p�G���b���O�W��A�N�O���F�_�h�M��̪�a��
        var below = solids.FirstOrDefault(s =>
            Bounds.Bottom <= s.Top + 2 &&
            Bounds.Right > s.Left && Bounds.Left < s.Right &&
            s.Top - Bounds.Bottom >= -100 && s.Top - Bounds.Bottom <= 200);
        if (below != RectangleF.Empty)
        {
            Bounds.Y = below.Top - Bounds.Height;
        }
    }
}
