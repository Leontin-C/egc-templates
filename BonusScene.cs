using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

/// <summary>
/// BONUS: o scenă cu un cub (saltul încărcat din laborator) și o mașină. Tasta C schimbă controlul
/// între ele; cea necomandată rămâne pe loc (mașina încetinește lin, cubul își termină saltul).
/// În jurul mașinii orbitează un cerc (un disc): este copilul unui pivot, care este copilul mașinii,
/// deci cercul urmează mașina oriunde ar merge, iar culoarea lui se schimbă în timp (HSV).
/// Un punct galben plutește deasupra obiectului controlat.
/// </summary>
public sealed class BonusScene : Scene
{
    private const string SwitchControl = "schimbă controlul (cub / mașină)";
    private const string Restart = "repornește scena";

    private PlayerController _player = null!;
    private CarController _car = null!;
    private SceneObject _chassis = null!;
    private SceneObject _circle = null!;
    private SceneObject _indicator = null!;

    // Punctul galben "respiră" în sus și în jos: un Tween dus-întors, cu easing.
    private readonly Tween _bob = new(Vector3.Zero, new Vector3(0.0f, 0.3f, 0.0f), 0.6f, Easing.EaseInOut, pingPong: true);

    private bool _controlCar = true;
    private float _hue;

    public override string Name => "bonus: mașină";

    public override string Status => $"control: {(_controlCar ? "mașină" : "cub")}";

    public override IEnumerable<string> ActionNames => new[]
    {
        Actions.Left, Actions.Right, Actions.Forward, Actions.Back, Actions.Jump, SwitchControl, Restart,
    };

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 13.0f, 11.0f);
        CameraTarget = Vector3.Zero;

        // Aceleași acțiuni ca în scena laboratorului (Bind nu dublează tastele deja legate).
        input.Bind(Actions.Left, Keys.A, Keys.Left);
        input.Bind(Actions.Right, Keys.D, Keys.Right);
        input.Bind(Actions.Forward, Keys.W, Keys.Up);
        input.Bind(Actions.Back, Keys.S, Keys.Down);
        input.Bind(Actions.Jump, Keys.Space);
        input.Bind(SwitchControl, Keys.C);
        input.Bind(Restart, Keys.R);

        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh sphere = AddMesh(Shapes.ShadedSphere(1.0f, 24, 12));
        Mesh wheel = AddMesh(Shapes.ShadedCylinder(CarController.WheelRadius, 0.15f, 20));
        Mesh disk = AddMesh(Shapes.ShadedCylinder(0.5f, 0.02f, 24));

        AddObject("podea", grid);
        BuildPlayer(cube, disk);
        BuildCar(cube, wheel, disk);

        _indicator = AddObject("indicator", sphere);
        _indicator.Transform.Scale = new Vector3(0.2f);
        _indicator.Tint = new Color4(1.0f, 0.9f, 0.2f, 1.0f);

        ApplyControl();
    }

    private void BuildPlayer(Mesh cube, Mesh disk)
    {
        SceneObject shadow = AddObject("umbra jucătorului", disk);
        shadow.Tint = new Color4(0.05f, 0.05f, 0.08f, 1.0f);

        SceneObject body = AddObject("cub", cube);
        body.Transform.Position = new Vector3(-3.0f, 0.5f, 2.0f);

        _player = new PlayerController(body, shadow, new Vector2(-9.0f, -9.0f), new Vector2(9.0f, 9.0f));
    }

    private void BuildCar(Mesh cube, Mesh wheelMesh, Mesh disk)
    {
        // Rădăcina (pivot, fără formă) poartă poziția și orientarea; tot restul sunt copiii ei.
        SceneObject root = AddObject("mașină", null);
        root.Transform.Position = new Vector3(3.0f, 0.0f, -2.0f);

        _chassis = AddObject("caroserie", cube, root);
        _chassis.Transform.Position = new Vector3(0.0f, 0.5f, 0.0f);
        _chassis.Transform.Scale = new Vector3(1.8f, 0.35f, 0.9f);

        SceneObject cabin = AddObject("cabină", cube, root);
        cabin.Transform.Position = new Vector3(-0.1f, 0.82f, 0.0f);
        cabin.Transform.Scale = new Vector3(0.9f, 0.3f, 0.75f);
        cabin.Tint = new Color4(0.6f, 0.8f, 1.0f, 1.0f);

        // Roțile: un pivot (se rotește în jurul axei Z, adică a axei roții) cu doi copii:
        // anvelopa (cilindrul, culcat cu axa pe Z) și o spiță, ca să se vadă că se rotește.
        var wheels = new List<SceneObject>();
        foreach (float x in new[] { 0.6f, -0.6f })
        {
            foreach (float z in new[] { 0.5f, -0.5f })
            {
                SceneObject pivot = AddObject("pivot roată", null, root);
                pivot.Transform.Position = new Vector3(x, CarController.WheelRadius, z);

                SceneObject tire = AddObject("roată", wheelMesh, pivot);
                tire.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
                tire.Tint = new Color4(0.12f, 0.12f, 0.14f, 1.0f);

                SceneObject spoke = AddObject("spiță", cube, pivot);
                spoke.Transform.Scale = new Vector3(0.5f, 0.06f, 0.2f);
                spoke.Tint = new Color4(0.9f, 0.9f, 0.9f, 1.0f);

                wheels.Add(pivot);
            }
        }

        // Cercul care orbitează mașina: pivotul stă deasupra mașinii și se rotește (Spin),
        // iar cercul este așezat la 2 unități de pivot.
        SceneObject orbit = AddObject("orbita cercului", null, root);
        orbit.Transform.Position = new Vector3(0.0f, 1.4f, 0.0f);
        orbit.Spin = new Vector3(0.0f, 90.0f, 0.0f);

        _circle = AddObject("cerc", disk, orbit);
        _circle.Transform.Position = new Vector3(2.0f, 0.0f, 0.0f);
        _circle.Transform.Scale = new Vector3(0.9f, 3.0f, 0.9f);

        _car = new CarController(root, wheels, new Vector2(-9.0f, -9.0f), new Vector2(9.0f, 9.0f));
    }

    /// <summary>Transmite controlul către cub sau mașină și colorează mașina în consecință.</summary>
    private void ApplyControl()
    {
        _car.Controlled = _controlCar;
        _player.Controlled = !_controlCar;
        _chassis.Tint = _controlCar
            ? new Color4(0.95f, 0.2f, 0.2f, 1.0f)    // controlată: roșu aprins
            : new Color4(0.5f, 0.2f, 0.2f, 1.0f);    // necontrolată: roșu stins
    }

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);   // Spin: orbita cercului

        if (input.WasPressed(SwitchControl))
        {
            _controlCar = !_controlCar;
            ApplyControl();
            ConsolePanel.PrintMessage($"controlul: {(_controlCar ? "mașina" : "cubul")}");
        }
        if (input.WasPressed(Restart))
        {
            _player.Reset();
            _car.Reset();
            ConsolePanel.PrintMessage("scena a repornit");
        }

        _player.Update(dt, input);
        _car.Update(dt, input);

        // Cercul își schimbă nuanța în timp (hue crește oricât: FromHsv îl aduce în [0, 1)).
        _hue += 0.25f * dt;
        _circle.Tint = ColorUtil.FromHsv(_hue, 0.8f, 1.0f);

        // Punctul galben urmărește obiectul controlat.
        _bob.Update(dt);
        Vector3 above = _controlCar ? _car.Position + new Vector3(0.0f, 2.1f, 0.0f)
                                    : _player.Position + new Vector3(0.0f, 1.3f, 0.0f);
        _indicator.Transform.Position = above + _bob.Value;
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Control        : {(_controlCar ? "mașină" : "cub")}";
        yield return $"Mașină         : {Fmt(_car.Position)}   {_car.State}   v = {_car.Speed:F2}   orientare {_car.Heading:F0}°";
        yield return $"Cub            : {Fmt(_player.Position)}   {_player.State}";
        yield return $"Cercul orbitei : nuanța {_hue % 1.0f:F2}";
    }

    private static string Fmt(Vector3 v) => $"({v.X:F2}, {v.Y:F2}, {v.Z:F2})";
}
