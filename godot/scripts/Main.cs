using System.Collections.Generic;
using System.Linq;
using Godot;
using Automaticlone.Core;
using GameState = Automaticlone.Core.Game;

namespace Automaticlone.Game;

// =====================================================================================
//  NŒUD PRINCIPAL DU PROTOTYPE A
// -------------------------------------------------------------------------------------
//  Rôle : faire le lien entre le joueur, la simulation (Automaticlone.Core) et l'affichage.
//    - il crée la carte (PrototypeMap), le sol, la lumière, le joueur et l'interface ;
//    - à chaque frame physique (60 par seconde) : déplace le joueur, transforme ses
//      clics en commandes, fait avancer la simulation d'UN pas, met l'affichage à jour.
//
//  COMMANDES :
//    ZQSD                     se déplacer (touches physiques : WASD sur un clavier QWERTY)
//    souris                   regarder
//    clic gauche              sur une station : déposer l'objet porté, ou ramasser si mains vides
//    clic droit (maintenu)    travailler sur la station visée (miner, aider au four, fabriquer)
//    1 / 2                    poser un chantier de four / d'établi à l'endroit visé au sol
//    Suppr                    supprimer le clone visé
//    Échap                    libérer la souris (un clic la recapture)
// =====================================================================================

public partial class Main : Node3D
{
    private const float PlaceRange = 30f;

    private GameState _game = null!;
    private Player _player = null!;
    private Hud _hud = null!;
    private readonly Dictionary<int, StationView> _stationViews = new();
    private readonly Dictionary<int, CloneView> _cloneViews = new();
    private readonly List<Command> _pending = new();
    private int? _workingOn;
    private LifeEndPanel _lifeEnd = null!;
    private bool _goalAnnounced;

    /// <summary>
    /// Option de lancement « -- --test-boucle » : vies de 2 s gardées automatiquement.
    /// Sert seulement à vérifier rapidement l'affichage des clones et la fin de vie.
    /// </summary>
    private bool _testLoop;

    public override void _Ready()
    {
        SetupInput();
        _testLoop = OS.GetCmdlineUserArgs().Contains("--test-boucle");
        var rules = _testLoop ? new Rules(LifeSeconds: 2) : new Rules();
        _game = new GameState(rules, PrototypeMap.Build);

        BuildScenery();
        _player = new Player();
        _player.Setup(_game.Rules.WalkSpeed);
        AddChild(_player);
        _player.Respawn(PrototypeMap.Spawn.ToGodot());

        _hud = new Hud();
        AddChild(_hud);

        _lifeEnd = new LifeEndPanel();
        AddChild(_lifeEnd);
        _lifeEnd.Decided += OnLifeDecided;

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_lifeEnd.Visible) return;
        if (e.IsActionPressed("liberer_souris"))
            Input.MouseMode = Input.MouseModeEnum.Visible;
        else if (e is InputEventMouseButton { Pressed: true } && Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            GetViewport().SetInputAsHandled(); // ce clic sert seulement à recapturer la souris
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_game.IsLifeOver)
        {
            _player.Move(delta);
            ReadPlayerCommands();
            _game.SetPlayerPose(_player.CurrentPose);
            var results = _game.Tick(_pending);
            for (int i = 0; i < results.Count; i++)
                if (results[i] != ActionResult.Ok) OnPlayerCommandFailed(_pending[i], results[i]);
            _pending.Clear();

            if (_game.GoalReached && !_goalAnnounced)
            {
                _goalAnnounced = true;
                _hud.ShowMessage($"Objectif atteint : {_game.Rules.GoalGears} engrenages livrés !", 5);
            }
        }
        else if (!_lifeEnd.Visible)
        {
            OnLifeOver();
        }

        SyncStations();
        SyncClones();
        _hud.UpdateStatus(_game, AimHint());
    }

    // ---------------------------------------------------------------------------------
    //  Fin de vie
    // ---------------------------------------------------------------------------------

    private void OnLifeOver()
    {
        _workingOn = null;
        if (_testLoop)
        {
            GD.Print($"[test-boucle] vie {_game.LifeNumber} terminée, gardée ; clones affichés : {_cloneViews.Count}");
            OnLifeDecided(true);
            return;
        }
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _lifeEnd.Show(_game);
    }

    private void OnLifeDecided(bool keep)
    {
        _game.FinishLife(keep);
        _goalAnnounced = false;
        _player.Respawn(PrototypeMap.Spawn.ToGodot());
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _hud.ShowMessage(keep ? "Vie gardée : un nouveau clone la rejoue" : "Vie rejetée");
    }

    // ---------------------------------------------------------------------------------
    //  Commandes du joueur
    // ---------------------------------------------------------------------------------

    private void ReadPlayerCommands()
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            StopWorking();
            return;
        }

        var aimed = AimedStation();

        if (Input.IsActionJustPressed("agir") && aimed is { } target)
            _pending.Add(_game.Player.Carried != null ? new Deposit(target.Id) : new PickUp(target.Id));

        if (Input.IsActionJustPressed("travailler") && aimed is { } workTarget)
        {
            _pending.Add(new StartWork(workTarget.Id));
            _workingOn = workTarget.Id;
        }
        else if (_workingOn != null && (!Input.IsActionPressed("travailler") || aimed?.Id != _workingOn))
            StopWorking();

        if (Input.IsActionJustPressed("poser_four")) TryPlace(BuildingKind.Four);
        if (Input.IsActionJustPressed("poser_etabli")) TryPlace(BuildingKind.Etabli);
        if (Input.IsActionJustPressed("supprimer_clone")) TryDeleteAimedClone();
    }

    /// <summary>Suppr en visant un clone : il disparaît (prototype : possible à tout moment, §9).</summary>
    private void TryDeleteAimedClone()
    {
        var hit = Raycast(PlaceRange, CloneView.AimLayer, areas: true);
        if (hit == null || !hit.Value.collider.HasMeta(CloneView.LifeMeta)) return;
        int life = hit.Value.collider.GetMeta(CloneView.LifeMeta).AsInt32();
        if (_game.DeleteClone(life)) _hud.ShowMessage($"Clone de la vie {life} supprimé");
    }

    private void StopWorking()
    {
        if (_workingOn == null) return;
        _pending.Add(new StopWork());
        _workingOn = null;
    }

    private void TryPlace(BuildingKind kind)
    {
        var hit = Raycast(PlaceRange, 1);
        if (hit == null || hit.Value.collider.HasMeta(StationView.StationIdMeta))
        {
            _hud.ShowMessage("Visez le sol pour construire");
            return;
        }
        var p = hit.Value.position;
        _pending.Add(new PlaceBuilding(kind, new System.Numerics.Vector3(p.X, 0, p.Z)));
    }

    private void OnPlayerCommandFailed(Command command, ActionResult result)
    {
        if (command is StartWork) _workingOn = null;
        _hud.ShowMessage(Conv.Message(result));
    }

    /// <summary>La station visée à portée d'interaction, ou <c>null</c>.</summary>
    private Station? AimedStation()
    {
        var hit = Raycast(_game.Rules.InteractRange, 1);
        if (hit == null || !hit.Value.collider.HasMeta(StationView.StationIdMeta)) return null;
        return _game.World.Find(hit.Value.collider.GetMeta(StationView.StationIdMeta).AsInt32());
    }

    /// <summary>Lance un rayon depuis la caméra, sur les calques donnés.</summary>
    private (Node collider, Vector3 position)? Raycast(float range, uint mask, bool areas = false)
    {
        var camera = _player.Camera;
        var from = camera.GlobalPosition;
        var to = from - camera.GlobalTransform.Basis.Z * range;
        var query = PhysicsRayQueryParameters3D.Create(from, to, mask);
        query.CollideWithAreas = areas;
        query.CollideWithBodies = !areas;
        var hit = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (hit.Count == 0) return null;
        return ((Node)hit["collider"].AsGodotObject(), hit["position"].AsVector3());
    }

    private string AimHint()
    {
        if (AimedStation() is not { } station) return "";
        string verb = _game.Player.Carried != null ? "[Clic gauche] Déposer" : "[Clic gauche] Ramasser";
        return $"{StationView.Describe(station, _game.Rules).Replace('\n', ' ')}\n{verb} · [Clic droit] Travailler";
    }

    // ---------------------------------------------------------------------------------
    //  Affichage
    // ---------------------------------------------------------------------------------

    private void SyncStations()
    {
        var alive = new HashSet<int>();
        foreach (var station in _game.World.Stations)
        {
            alive.Add(station.Id);
            if (_stationViews.TryGetValue(station.Id, out var view) && view.StationType != station.GetType())
            {
                view.QueueFree();          // un chantier est devenu un bâtiment
                _stationViews.Remove(station.Id);
            }
            if (!_stationViews.TryGetValue(station.Id, out view))
            {
                view = StationView.Create(station);
                AddChild(view);
                _stationViews[station.Id] = view;
            }
            view.Sync(station, _game.Rules);
        }

        foreach (var id in _stationViews.Keys.Where(id => !alive.Contains(id)).ToList())
        {
            _stationViews[id].QueueFree();
            _stationViews.Remove(id);
        }
    }

    private void SyncClones()
    {
        var alive = new HashSet<int>();
        foreach (var clone in _game.Clones)
        {
            int life = clone.Recording.LifeNumber;
            alive.Add(life);
            if (!_cloneViews.TryGetValue(life, out var view))
            {
                view = CloneView.Create(life);
                AddChild(view);
                _cloneViews[life] = view;
            }
            view.Sync(clone, _game.Step, _game.Rules);
        }

        foreach (var life in _cloneViews.Keys.Where(l => !alive.Contains(l)).ToList())
        {
            _cloneViews[life].QueueFree();
            _cloneViews.Remove(life);
        }
    }

    private void BuildScenery()
    {
        var env = new Environment
        {
            BackgroundMode = Environment.BGMode.Sky,
            Sky = new Sky { SkyMaterial = new ProceduralSkyMaterial() },
            AmbientLightSource = Environment.AmbientSource.Sky,
        };
        AddChild(new WorldEnvironment { Environment = env });
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-50, -30, 0), ShadowEnabled = true });

        var ground = new StaticBody3D { CollisionLayer = 1 };
        ground.AddChild(new CollisionShape3D
        {
            Shape = new BoxShape3D { Size = new Vector3(80, 1, 80) },
            Position = new Vector3(0, -0.5f, -10),
        });
        ground.AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(80, 1, 80),
                Material = new StandardMaterial3D { AlbedoColor = new Color(0.55f, 0.35f, 0.25f) },
            },
            Position = new Vector3(0, -0.5f, -10),
        });
        AddChild(ground);
    }

    /// <summary>Touches physiques : ZQSD sur un clavier AZERTY (positions de WASD).</summary>
    private static void SetupInput()
    {
        AddKey("avancer", Key.W);
        AddKey("reculer", Key.S);
        AddKey("gauche", Key.A);
        AddKey("droite", Key.D);
        AddKey("poser_four", Key.Key1);
        AddKey("poser_etabli", Key.Key2);
        AddKey("supprimer_clone", Key.Delete);
        AddKey("liberer_souris", Key.Escape);
        AddMouse("agir", MouseButton.Left);
        AddMouse("travailler", MouseButton.Right);
    }

    private static void AddKey(string action, Key key)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    private static void AddMouse(string action, MouseButton button)
    {
        if (!InputMap.HasAction(action)) InputMap.AddAction(action);
        InputMap.ActionAddEvent(action, new InputEventMouseButton { ButtonIndex = button });
    }
}
