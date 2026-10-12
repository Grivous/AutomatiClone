using System;
using Godot;
using Automaticlone.Core;

namespace Automaticlone.Game;

// =====================================================================================
//  AFFICHAGE D'UNE STATION
// -------------------------------------------------------------------------------------
//  Une boîte de couleur par type de station, un corps physique pour la visée (calque 1,
//  métadonnée « station_id »), et un texte 3D qui montre son contenu.
//  Quand un chantier devient un bâtiment, Main jette la vue et en crée une nouvelle.
// =====================================================================================

public partial class StationView : Node3D
{
    public const string StationIdMeta = "station_id";

    private Label3D _label = null!;

    public int StationId { get; private set; }

    /// <summary>Le type de station affiché (pour savoir quand recréer la vue).</summary>
    public Type StationType { get; private set; } = null!;

    public static StationView Create(Station station)
    {
        var view = new StationView { StationId = station.Id, StationType = station.GetType() };
        view.Position = station.Position.ToGodot();
        var (size, color) = Look(station);

        var body = new StaticBody3D { CollisionLayer = 1 };
        body.SetMeta(StationIdMeta, station.Id);
        body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size }, Position = new Vector3(0, size.Y / 2, 0) });
        view.AddChild(body);

        var material = new StandardMaterial3D { AlbedoColor = color };
        if (station is BuildSite)
        {
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            material.AlbedoColor = color with { A = 0.35f };
        }
        view.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = size, Material = material },
            Position = new Vector3(0, size.Y / 2, 0),
        });

        view._label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            Position = new Vector3(0, size.Y + 0.6f, 0),
            FontSize = 40,
            PixelSize = 0.006f,
            OutlineSize = 8,
            NoDepthTest = true,
        };
        view.AddChild(view._label);
        return view;
    }

    public void Sync(Station station, Rules rules) => _label.Text = Describe(station, rules);

    /// <summary>Le texte affiché au-dessus de la station (et dans l'aide de visée).</summary>
    public static string Describe(Station station, Rules rules) => station switch
    {
        ResourceDeposit d =>
            $"{Capitalize(Conv.Name(d.Resource))}\nau sol {d.Pile}/{rules.PileLimit} — {Percent(d.ProgressSteps, rules.StepsFor(rules.MineTicks))}",
        Furnace f =>
            $"Four — {(f.IsLit ? $"feu {Mathf.CeilToInt(f.BurnStepsLeft / (float)rules.StepsPerSecond)} s" : "éteint")}\n" +
            $"fer {f.Iron} · charbon {f.Coal} · lingots {f.Ingots} — {Percent(f.ProgressSteps, rules.StepsFor(rules.SmeltTicks))}",
        Workbench w =>
            $"Établi — engrenage\nlingots {w.Ingots} · engrenages {w.Gears} — {Percent(w.ProgressSteps, rules.StepsFor(rules.GearTicks))}",
        BuildSite s =>
            $"Chantier {Conv.Name(s.Kind)}\n{Conv.Name(s.Material)} {s.Delivered}/{s.Needed}",
        DeliveryPoint p =>
            $"Livraison\nengrenages {p.GearsDelivered}/{rules.GoalGears}",
        _ => "",
    };

    private static (Vector3 size, Color color) Look(Station station) => station switch
    {
        ResourceDeposit { Resource: Item.Fer } => (new Vector3(2, 1.5f, 2), new Color(0.45f, 0.5f, 0.6f)),
        ResourceDeposit { Resource: Item.Pierre } => (new Vector3(2, 1.5f, 2), new Color(0.8f, 0.72f, 0.55f)),
        ResourceDeposit => (new Vector3(2, 1.5f, 2), new Color(0.12f, 0.12f, 0.12f)),
        Furnace => (new Vector3(1.5f, 1.5f, 1.5f), new Color(0.65f, 0.25f, 0.15f)),
        Workbench => (new Vector3(2, 1, 1), new Color(0.6f, 0.42f, 0.25f)),
        BuildSite { Kind: BuildingKind.Four } => (new Vector3(1.5f, 1.5f, 1.5f), new Color(0.65f, 0.25f, 0.15f)),
        BuildSite => (new Vector3(2, 1, 1), new Color(0.6f, 0.42f, 0.25f)),
        _ => (new Vector3(1.5f, 0.3f, 1.5f), new Color(0.2f, 0.7f, 0.3f)),
    };

    private static string Percent(int progress, int cost) => $"{100 * progress / Math.Max(1, cost)} %";

    private static string Capitalize(string s) => char.ToUpper(s[0]) + s[1..];
}
