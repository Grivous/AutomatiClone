using System;
using Godot;
using GameState = Automaticlone.Core.Game;

namespace Automaticlone.Game;

// =====================================================================================
//  ÉCRAN ENTRE DEUX VIES (§3.3, §9)
// -------------------------------------------------------------------------------------
//  À la mort : bilan de la boucle, liste des clones (chacun supprimable), et le choix
//  de garder ou de rejeter la vie qui vient de se terminer.
// =====================================================================================

public partial class LifeEndPanel : CanvasLayer
{
    private GameState _game = null!;
    private Label _title = null!;
    private Label _gears = null!;
    private VBoxContainer _cloneList = null!;

    /// <summary>Le joueur a choisi : <c>true</c> = garder la vie comme clone, <c>false</c> = la rejeter.</summary>
    public event Action<bool>? Decided;

    public override void _Ready()
    {
        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(460, 0) };
        center.AddChild(panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 20);
        panel.AddChild(margin);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 12);
        margin.AddChild(box);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        _title.AddThemeFontSizeOverride("font_size", 28);
        box.AddChild(_title);

        _gears = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        box.AddChild(_gears);

        box.AddChild(new Label { Text = "Clones :" });
        _cloneList = new VBoxContainer();
        box.AddChild(_cloneList);

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 16);
        box.AddChild(buttons);
        AddButton(buttons, "Garder cette vie", () => Decide(true));
        AddButton(buttons, "Rejeter cette vie", () => Decide(false));

        Visible = false;
    }

    public void Show(GameState game)
    {
        _game = game;
        _title.Text = $"Vie n° {game.LifeNumber} terminée";
        _gears.Text = $"Engrenages livrés : {game.GearsDeliveredThisLoop}/{game.Rules.GoalGears}";
        RefreshClones();
        Visible = true;
    }

    private void RefreshClones()
    {
        foreach (var child in _cloneList.GetChildren()) child.QueueFree();

        if (_game.Clones.Count == 0)
        {
            _cloneList.AddChild(new Label { Text = "(aucun)" });
            return;
        }

        foreach (var clone in _game.Clones)
        {
            int life = clone.Recording.LifeNumber;
            string state = clone.Actor.HasFailed ? " — a échoué" : clone.Actor.HasWaited ? " — a attendu" : "";
            var row = new HBoxContainer();
            row.AddChild(new Label { Text = $"Vie {life}{state}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
            AddButton(row, "Supprimer", () =>
            {
                _game.DeleteClone(life);
                RefreshClones();
            });
            _cloneList.AddChild(row);
        }
    }

    private void Decide(bool keep)
    {
        Visible = false;
        Decided?.Invoke(keep);
    }

    private static void AddButton(Container parent, string text, Action onPressed)
    {
        var button = new Button { Text = text };
        button.Pressed += onPressed;
        parent.AddChild(button);
    }
}
