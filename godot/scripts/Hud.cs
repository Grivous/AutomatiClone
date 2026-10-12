using Godot;
using Automaticlone.Core;

namespace Automaticlone.Game;

// =====================================================================================
//  INTERFACE MINIMALE DU PROTOTYPE
// -------------------------------------------------------------------------------------
//  Simplification propre au prototype (§9) : l'oxygène est un simple texte à l'écran
//  en attendant l'affichage sur la combinaison. En haut à gauche : vie, oxygène, objet
//  porté, objectif. Au centre : le viseur et les messages. En bas : l'aide de visée.
// =====================================================================================

public partial class Hud : CanvasLayer
{
    private Label _status = null!;
    private Label _aim = null!;
    private Label _message = null!;
    private double _messageTimeLeft;

    public override void _Ready()
    {
        _status = AddLabel(new Vector2(16, 12), 22);

        var help = AddLabel(Vector2.Zero, 16);
        help.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        help.GrowHorizontal = Control.GrowDirection.Begin;
        help.HorizontalAlignment = HorizontalAlignment.Right;
        help.OffsetRight = -16;
        help.OffsetTop = 12;
        help.Modulate = new Color(1, 1, 1, 0.8f);
        help.Text =
            "ZQSD se déplacer · souris regarder\n" +
            "clic gauche ramasser / déposer\n" +
            "clic droit maintenu travailler\n" +
            "1 chantier de four · 2 chantier d'établi\n" +
            "Suppr supprimer le clone visé\n" +
            "Échap libérer la souris";

        var crosshair = AddLabel(Vector2.Zero, 28);
        crosshair.Text = "+";
        crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        crosshair.HorizontalAlignment = HorizontalAlignment.Center;
        crosshair.VerticalAlignment = VerticalAlignment.Center;

        _aim = AddLabel(Vector2.Zero, 20);
        _aim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
        _aim.GrowHorizontal = Control.GrowDirection.Both;
        _aim.GrowVertical = Control.GrowDirection.Begin;
        _aim.HorizontalAlignment = HorizontalAlignment.Center;
        _aim.OffsetBottom = -24;

        _message = AddLabel(Vector2.Zero, 26);
        _message.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _message.GrowHorizontal = Control.GrowDirection.Both;
        _message.HorizontalAlignment = HorizontalAlignment.Center;
        _message.OffsetTop = 40;
        _message.Modulate = new Color(1, 0.85f, 0.4f);
    }

    public override void _Process(double delta)
    {
        if (_messageTimeLeft <= 0) return;
        _messageTimeLeft -= delta;
        if (_messageTimeLeft <= 0) _message.Text = "";
    }

    public void UpdateStatus(Core.Game game, string aimHint)
    {
        int secondsLeft = Mathf.CeilToInt((game.Rules.LifeSteps - game.Step) / (float)game.Rules.StepsPerSecond);
        string carried = game.Player.Carried is { } item ? Conv.Name(item) : "rien";
        _status.Text =
            $"Vie n° {game.LifeNumber} — clones : {game.Clones.Count}\n" +
            $"O₂ {secondsLeft} s\n" +
            $"Porté : {carried}\n" +
            $"Engrenages : {game.GearsDeliveredThisLoop}/{game.Rules.GoalGears}";
        _aim.Text = aimHint;
    }

    public void ShowMessage(string text, double seconds = 2)
    {
        _message.Text = text;
        _messageTimeLeft = seconds;
    }

    private Label AddLabel(Vector2 position, int fontSize)
    {
        var label = new Label { Position = position };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeConstantOverride("outline_size", 6);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        AddChild(label);
        return label;
    }
}
