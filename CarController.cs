using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// BONUS: mașina. Se conduce prin ACȚIUNI (aceleași ca la cub): înainte / înapoi = accelerație,
/// stânga / dreapta = virare. Totul folosește doar timpul de joc (dt):
///  - viteza crește cu accelerația × dt și scade lin (frecare) când nu se apasă nimic;
///  - virajul depinde de viteză: mașina nu se învârte pe loc, iar în marșarier virează invers;
///  - roțile se rotesc proporțional cu viteza (v / r).
/// Mașina "privește" spre +X când unghiul de orientare (heading) este 0.
/// </summary>
public sealed class CarController
{
    public const float MaxSpeed = 7.0f;        // unități / s, înainte
    public const float MaxReverse = 3.0f;      // unități / s, înapoi
    public const float Acceleration = 9.0f;    // unități / s²
    public const float Brake = 14.0f;          // frânare (tasta opusă mersului)
    public const float Friction = 5.0f;        // încetinire când nu se apasă nimic
    public const float TurnRate = 110.0f;      // grade / s, la viteză mare
    public const float WheelRadius = 0.3f;

    private readonly SceneObject _root;
    private readonly List<SceneObject> _wheels;
    private readonly Vector3 _start;
    private readonly Vector2 _min, _max;

    private Vector3 _position;
    private float _heading;   // grade, în jurul axei Y (pozitiv = viraj la stânga, privit de sus)
    private float _speed;     // pozitivă = înainte

    public CarController(SceneObject root, List<SceneObject> wheels, Vector2 min, Vector2 max)
    {
        _root = root;
        _wheels = wheels;
        _start = root.Transform.Position;
        _position = _start;
        _min = min;
        _max = max;
    }

    /// <summary>Dacă este false, mașina ignoră tastele și încetinește singură până se oprește.</summary>
    public bool Controlled { get; set; } = true;

    public Vector3 Position => _position;
    public float Speed => _speed;
    public float Heading => _heading;

    public void Update(float dt, Input input)
    {
        float throttle = Controlled ? input.Axis(Actions.Back, Actions.Forward) : 0.0f;
        float steer = Controlled ? input.Axis(Actions.Right, Actions.Left) : 0.0f;

        // 1. Viteza: accelerăm / frânăm cât timp se apasă, altfel frecarea aduce viteza spre 0.
        if (throttle != 0.0f)
        {
            bool braking = _speed != 0.0f && MathF.Sign(throttle) != MathF.Sign(_speed);
            _speed += throttle * (braking ? Brake : Acceleration) * dt;
        }
        else
        {
            float drag = Friction * dt;
            _speed = MathF.Abs(_speed) <= drag ? 0.0f : _speed - MathF.Sign(_speed) * drag;
        }
        _speed = Math.Clamp(_speed, -MaxReverse, MaxSpeed);

        // 2. Virajul: proporțional cu viteza (până la 3 u/s), cu semn: în marșarier se inversează.
        float grip = Math.Clamp(_speed / 3.0f, -1.0f, 1.0f);
        _heading = (_heading + steer * TurnRate * grip * dt) % 360.0f;

        // 3. Deplasarea pe direcția în care privește mașina (+X rotit cu heading în jurul lui Y).
        float rad = MathHelper.DegreesToRadians(_heading);
        var forward = new Vector3(MathF.Cos(rad), 0.0f, -MathF.Sin(rad));
        Vector3 p = _position + forward * _speed * dt;

        float x = Math.Clamp(p.X, _min.X, _max.X);
        float z = Math.Clamp(p.Z, _min.Y, _max.Y);
        if (x != p.X || z != p.Z)
        {
            _speed = 0.0f;   // a lovit marginea zonei de joc
        }
        _position = new Vector3(x, p.Y, z);

        // 4. Efecte vizuale: poziția, orientarea și rotirea roților.
        _root.Transform.Position = _position;
        _root.Transform.Rotation = new Vector3(0.0f, _heading, 0.0f);

        float wheelDegrees = MathHelper.RadiansToDegrees(_speed / WheelRadius * dt);
        foreach (SceneObject wheel in _wheels)
        {
            wheel.Transform.Rotation.Z = (wheel.Transform.Rotation.Z - wheelDegrees) % 360.0f;
        }
    }

    public void Reset()
    {
        _position = _start;
        _heading = 0.0f;
        _speed = 0.0f;
        _root.Transform.Position = _start;
        _root.Transform.Rotation = Vector3.Zero;
    }

    public string State => Controlled
        ? (MathF.Abs(_speed) < 0.05f ? "staționează" : _speed > 0 ? "înainte" : "marșarier")
        : "liberă";
}
