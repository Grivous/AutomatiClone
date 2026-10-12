using Godot;
using Automaticlone.Core;

namespace Automaticlone.Game;

// =====================================================================================
//  AFFICHAGE D'UN CLONE FANTÔME
// -------------------------------------------------------------------------------------
//  Une capsule transparente qui suit la pose enregistrée de sa vie. Sa couleur dit son
//  état (§3.2, §3.6) : cyan normal, rouge = script cassé, orange = il attend.
//  Le joueur le traverse (aucun corps physique, §3.8) ; un Area3D sur le calque 2 permet
//  seulement de le viser pour le supprimer.
// =====================================================================================

public partial class CloneView : Node3D
{
    public const string LifeMeta = "clone_life";
    public const uint AimLayer = 2;

    private StandardMaterial3D _material = null!;
    private Label3D _label = null!;

    public int LifeNumber { get; private set; }

    public static CloneView Create(int lifeNumber)
    {
        var view = new CloneView { LifeNumber = lifeNumber };

        view._material = new StandardMaterial3D
        {
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        view.AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = 0.4f, Height = 1.8f, Material = view._material },
            Position = new Vector3(0, 0.9f, 0),
        });
        // Petit « nez » pour voir où le clone regarde.
        view.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(0.3f, 0.15f, 0.3f), Material = view._material },
            Position = new Vector3(0, Player.EyeHeight, -0.4f),
        });

        var area = new Area3D { CollisionLayer = AimLayer, CollisionMask = 0, Monitoring = false };
        area.SetMeta(LifeMeta, lifeNumber);
        area.AddChild(new CollisionShape3D
        {
            Shape = new CapsuleShape3D { Radius = 0.4f, Height = 1.8f },
            Position = new Vector3(0, 0.9f, 0),
        });
        view.AddChild(area);

        view._label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Position = new Vector3(0, 2.2f, 0),
            FontSize = 36,
            PixelSize = 0.006f,
            OutlineSize = 8,
        };
        view.AddChild(view._label);
        return view;
    }

    public void Sync(Clone clone, int step, Rules rules)
    {
        var pose = clone.PoseAt(step);
        Position = pose.Position.ToGodot();
        Rotation = new Vector3(0, pose.Yaw, 0);

        _material.AlbedoColor = ColorOf(GhostLooks.For(clone.Actor, step, rules));
        string carried = clone.Actor.Carried is { } item ? $"\n{Conv.Name(item)}" : "";
        _label.Text = $"Vie {LifeNumber}{carried}";
    }

    private static Color ColorOf(GhostLook look) => look switch
    {
        GhostLook.FailFlash => new Color(1f, 0.1f, 0.1f, 0.8f),
        GhostLook.Waiting => new Color(1f, 0.55f, 0f, 0.7f),
        GhostLook.TintRed => new Color(0.95f, 0.45f, 0.45f, 0.35f),
        GhostLook.TintOrange => new Color(0.95f, 0.7f, 0.4f, 0.35f),
        _ => new Color(0.3f, 0.9f, 1f, 0.35f),
    };
}
