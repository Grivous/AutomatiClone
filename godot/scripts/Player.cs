using Godot;
using Automaticlone.Core;

namespace Automaticlone.Game;

// =====================================================================================
//  LE JOUEUR (vue à la première personne)
// -------------------------------------------------------------------------------------
//  Déplacement ZQSD (touches physiques), souris pour regarder. Pas de saut.
//  Le déplacement est appelé par Main à chaque frame physique, juste avant le pas de
//  simulation, pour que la pose enregistrée corresponde exactement au pas.
// =====================================================================================

public partial class Player : CharacterBody3D
{
    public const float EyeHeight = 1.6f;
    private const float MouseSensitivity = 0.0025f;
    private const float Gravity = 9.8f;

    private float _speed = 4f;

    public Camera3D Camera { get; private set; } = null!;

    public void Setup(float walkSpeed)
    {
        _speed = walkSpeed;
        AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.8f },
            Position = new Vector3(0, 0.9f, 0),
        });
        Camera = new Camera3D { Position = new Vector3(0, EyeHeight, 0), Current = true, Fov = 80 };
        AddChild(Camera);
    }

    public Pose CurrentPose => new(GlobalPosition.ToCore(), Rotation.Y, Camera.Rotation.X);

    public void Respawn(Vector3 position)
    {
        GlobalPosition = position;
        Rotation = Vector3.Zero;
        Camera.Rotation = Vector3.Zero;
        Velocity = Vector3.Zero;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * MouseSensitivity);
            float pitch = Camera.Rotation.X - motion.Relative.Y * MouseSensitivity;
            Camera.Rotation = new Vector3(Mathf.Clamp(pitch, -1.5f, 1.5f), 0, 0);
        }
    }

    /// <summary>Un pas de déplacement (appelé par Main).</summary>
    public void Move(double delta)
    {
        var input = Input.GetVector("gauche", "droite", "avancer", "reculer");
        var dir = (Transform.Basis * new Vector3(input.X, 0, input.Y)).Normalized();
        var v = Velocity;
        v.X = dir.X * _speed;
        v.Z = dir.Z * _speed;
        v.Y = IsOnFloor() ? 0 : v.Y - Gravity * (float)delta;
        Velocity = v;
        MoveAndSlide();
    }
}
