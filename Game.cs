using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// Fereastra aplicației și bucla principală (main game loop).
/// Față de laboratorul 3: o scenă cu mai multe obiecte, fiecare cu matricea lui model,
/// o cameră cu matricele view și projection, tastatura și mouse-ul.
/// </summary>
public class Game : GameWindow
{
    // Obiecte care NU depind de OpenGL pot fi create imediat.
    private readonly Camera _camera = new();

    // Obiectele care folosesc OpenGL se creează în OnLoad (abia atunci există contextul OpenGL).
    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();   // pentru tasta R

    // Starea aplicației (se modifică în OnUpdateFrame, se folosește în OnRenderFrame).
    private int _selected;            // indexul obiectului selectat în _scene (tasta Tab)
    private bool _wireframe;          // contur (true) sau plin (false)

    // T4 (Opțional): Punctul pivot pentru rotație
    private Vector2? _pivotPoint = null;

    // Contoare afișate în titlul ferestrei.
    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),          // cerem OpenGL 3.3 ...
                   Profile = ContextProfile.Core,           // ... profil Core (fără modul imediat)
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;   // swap-ul bufferelor așteaptă reîmprospătarea monitorului
    }

    /// <summary>Apelată O SINGURĂ DATĂ, după crearea ferestrei și a contextului OpenGL.</summary>
    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        // Scena: aceleași forme din laboratorul 3, acum plasate în lume prin Transform2D.
        // Depth (z) contează doar în perspectivă (tasta O, secțiunea 7.2).
        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });
    }

    /// <summary>Adaugă un obiect în scenă și reține transformarea lui inițială (pentru tasta R).</summary>
    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    /// <summary>Apelată când se schimbă dimensiunea ferestrei.</summary>
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);   // zona ferestrei în care se desenează
        _camera.Resize(e.Width, e.Height);      // proiecția se adaptează la noul raport de aspect
    }

    /// <summary>Apelată la fiecare iterație a buclei: LOGICA aplicației (intrări, mișcare, timp).</summary>
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        // --- Obiectul selectat: tastatura (secțiunea 8) ------------------------------

        float dt = (float)args.Time;
        SceneObject sel = _scene[_selected];

        const float moveSpeed = 2.0f;   // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.Left)) sel.Transform.Position.X -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Right)) sel.Transform.Position.X += moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Down)) sel.Transform.Position.Y -= moveSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.Up)) sel.Transform.Position.Y += moveSpeed * dt;

        // Rezolvare T2: Q / E rotesc obiectul, Z / X îl scalează
        float rotSpeed = 90.0f; // grade pe secundă

        // T4 (Opțional): Rotație în jurul pivotului dacă tasta P este ținută apăsată și avem un pivot setat
        if (KeyboardState.IsKeyDown(Keys.P) && _pivotPoint.HasValue)
        {
            float deltaAngle = 0.0f;
            if (KeyboardState.IsKeyDown(Keys.Q)) deltaAngle += rotSpeed * dt;
            if (KeyboardState.IsKeyDown(Keys.E)) deltaAngle -= rotSpeed * dt;

            if (deltaAngle != 0.0f)
            {
                float rad = MathHelper.DegreesToRadians(deltaAngle);
                Vector2 pivot = _pivotPoint.Value;
                Vector2 relativePos = sel.Transform.Position - pivot;

                // Rotim poziția obiectului față de pivot
                float newX = relativePos.X * MathF.Cos(rad) - relativePos.Y * MathF.Sin(rad);
                float newY = relativePos.X * MathF.Sin(rad) + relativePos.Y * MathF.Cos(rad);

                sel.Transform.Position = pivot + new Vector2(newX, newY);
                sel.Transform.Rotation += deltaAngle;
            }
        }
        else
        {
            // Rotație normală în jurul centrului propriu
            if (KeyboardState.IsKeyDown(Keys.Q)) sel.Transform.Rotation += rotSpeed * dt;
            if (KeyboardState.IsKeyDown(Keys.E)) sel.Transform.Rotation -= rotSpeed * dt;
        }

        float scaleSpeed = 1.5f;
        if (KeyboardState.IsKeyDown(Keys.Z)) sel.Transform.Scale /= MathF.Pow(scaleSpeed, dt);
        if (KeyboardState.IsKeyDown(Keys.X)) sel.Transform.Scale *= MathF.Pow(scaleSpeed, dt);

        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O)) _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F)) _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R)) ResetScene();
        if (KeyboardState.IsKeyPressed(Keys.C)) { _pivotPoint = null; _titleDirty = true; } // Tasta C șterge pivotul

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        // --- Obiectul selectat: mouse-ul (secțiunea 9) -------------------------------

        // Rezolvare T3: la clic stânga, obiectul selectat se mută în punctul din lume aflat sub cursor
        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            sel.Transform.Position = ScreenToWorld(MouseState.Position);
        }

        // Rezolvare T4 (Opțional): la clic dreapta se fixează punctul pivot
        if (MouseState.IsButtonPressed(MouseButton.Right))
        {
            _pivotPoint = ScreenToWorld(MouseState.Position);
            _titleDirty = true;
        }

        // --- Camera: tastatura și rotița (secțiunea 6.2) -----------------------------

        float camSpeed = 3.0f / _camera.Zoom;                    // unități de lume pe secundă
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);   // un pas al rotiței = ±10 %
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        // --- Titlul ferestrei: obiectul selectat, proiecția, cadre pe secundă --------

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            string pivotInfo = _pivotPoint.HasValue
                ? $" | Pivot: ({_pivotPoint.Value.X:F1}, {_pivotPoint.Value.Y:F1})"
                : " | Pivot: Nicio selecție";

            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection}{pivotInfo} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    /// <summary>Apelată la fiecare iterație a buclei: DESENAREA unui cadru.</summary>
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        // 1. Ștergem imaginea veche din back buffer.
        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // 2. Starea de rasterizare și programul shader.
        SetWireframe(_wireframe);
        _shader.Use();

        // 3. View și projection sunt aceleași pentru toate obiectele: le trimitem o singură dată.
        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        // 4. Fiecare obiect: matricea lui model, nuanța, apoi desenarea.
        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        // 5. Afișăm cadrul terminat: back buffer <-> front buffer.
        SwapBuffers();
    }

    /// <summary>Apelată O SINGURĂ DATĂ, la închiderea ferestrei: eliberăm resursele GPU.</summary>
    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();
    }

    /// <summary>Transformă poziția mouse-ului (pixeli, origine stânga-sus) în coordonate de lume.</summary>
    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        // Rezolvare T3: pasul 1 (pixel → NDC), apoi pasul 2 (NDC → lume)
        float ndcX = (2.0f * mousePixels.X / ClientSize.X) - 1.0f;
        float ndcY = 1.0f - (2.0f * mousePixels.Y / ClientSize.Y);

        Vector4 ndcPoint = new Vector4(ndcX, ndcY, 0.0f, 1.0f);

        Matrix4 invProjView = Matrix4.Invert(_camera.ViewMatrix * _camera.ProjectionMatrix);
        Vector4 worldPoint = ndcPoint * invProjView;

        return worldPoint.Xy;
    }

    /// <summary>Tasta R: obiectele revin la transformările inițiale, camera la starea inițială.</summary>
    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _pivotPoint = null;
        _camera.Reset();
    }

    /// <summary>
    /// Contur / plin. Dacă Visual Studio semnalează o eroare la primul parametru,
    /// folosiți aici tipul acceptat de versiunea voastră (vezi laboratorul 2, Problema 3):
    /// TriangleFace.FrontAndBack sau MaterialFace.FrontAndBack.
    /// </summary>
    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}