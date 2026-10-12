# Prototype A d'Automaticlone — Plan de développement

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal :** un prototype FPS jouable où le joueur orchestre ses vies passées (clones fantômes) pour livrer 5 engrenages en une boucle de 60 s.

**Architecture :** toute la simulation (gisements, four, chantiers, établi, ticks, enregistrement et rejeu des clones, priorités, statuts rouge et orange) vit dans une bibliothèque C# pure, `Automaticlone.Core`, testée avec xUnit sans Godot. Le projet Godot ne fait qu'afficher l'état du Core, déplacer le joueur et lui transmettre les commandes. C'est la même organisation que le projet `seed7`. La simulation avance à pas fixe : **1 pas = 1 frame physique, 60 pas par seconde**. Un « tick » du cahier des charges vaut donc 60 pas de travail d'un personnage.

**Tech Stack :** Godot 4.7.2 mono (`%USERPROFILE%\tools\Godot_v4.7.2-stable_mono_win64\`), C# .NET 8 (`net8.0`), xUnit 2.5.3, `System.Numerics.Vector3` dans le Core.

**Spec :** `cahier-des-charges.md` (racine du dépôt), sections 3, 4, 7 et 9.

## Global Constraints

- Le Core ne référence jamais Godot : seulement `System.*`. Le projet Godot référence le Core.
- Toutes les valeurs d'équilibrage sont dans `Rules` (un record avec des valeurs par défaut), jamais en dur ailleurs.
- Valeurs par défaut (spec §9) : vie 60 s ; 1 tick par seconde et par personnage ; extraction 20 ticks ; 1 ressource par extraction ; limite de 10 au pied d'un gisement ; fonte 5 ticks ; 1 charbon = 10 s ; four = 3 pierres ; établi = 2 lingots ; engrenage = 2 lingots, 10 ticks ; 1 objet porté ; objectif 5 engrenages ; environ 5 s de marche entre les gisements et la zone de production.
- Valeurs choisies par ce plan, absentes du cahier des charges (réglables) : 60 pas par seconde ; vitesse de marche 4 m/s ; espacement minimal entre bâtiments 2,5 m ; four : 5 fers, 5 charbons et 5 lingots au plus ; établi : 4 lingots et 2 engrenages au plus ; portée d'interaction 3 m ; flash rouge de 1,5 s.
- Commentaires et textes affichés en français, comme dans `seed7`. Touches physiques (ZQSD sur un clavier AZERTY).
- Commandes : `dotnet test Automaticlone.Core.Tests` pour les tests, `dotnet build godot/Automaticlone.csproj` pour compiler le jeu, `lancer.bat` pour jouer.

## Review Focus

1. **Les clics ratés du joueur ne sont pas enregistrés.** Seules les commandes qui réussissent entrent dans l'enregistrement, sinon le clone serait rouge à cause d'une maladresse du joueur. Test `PlayerFailedCommandsAreNotRecorded` (tâche 5).
2. **Échec en cascade.** Un clone qui rate son ramassage a les mains vides, donc son dépôt suivant échoue aussi, et il reste marqué rouge jusqu'à la fin de la boucle. Test `FailedPickupCascadesIntoFailedDeposit` (tâche 5).
3. **Supprimer un clone en plein travail.** Son apport de ticks s'arrête au pas suivant, mais la progression déjà acquise sur le gisement reste. Test `DeletingWorkingCloneKeepsStationProgress` (tâche 5).
4. **Une vie rejetée laisse ses constructions**, y compris les matériaux d'un chantier inachevé, alors que le contenu des fours est vidé à la boucle suivante. Test `RejectedLifeKeepsBuildingsAndSiteMaterials` (tâche 5).
5. **Le mineur bloqué repart dès qu'une place se libère** : il redevient non-orange, mais garde la teinte orange permanente. Test `BlockedMinerResumesWhenPileFreed` (tâche 2).

---

## Structure des fichiers

```
Automaticlone/
  Automaticlone.slnx
  .gitignore
  lancer.bat
  cahier-des-charges.md
  Automaticlone.Core/
    Automaticlone.Core.csproj
    Rules.cs          réglages d'équilibrage (record Rules)
    Items.cs          enum Item, enum BuildingKind, enum ActionResult
    Commands.cs       commandes (PickUp, Deposit, StartWork, StopWork, PlaceBuilding) + TimedCommand
    Actor.cs          un personnage (joueur ou clone) : objet porté, travail en cours, statuts
    Stations.cs       Station et ses sous-classes : ResourceDeposit, Furnace, Workbench, BuildSite, DeliveryPoint
    World.cs          liste des stations, Apply(commande), Advance(pas de travail), ResetForNewLoop
    Recording.cs      Pose, LifeRecording, Clone
    Game.cs           boucle de vie : Tick, FinishLife(keep), DeleteClone, objectif
    GhostLook.cs      apparence d'un clone (normal, teinte orange/rouge, attente, flash)
    PrototypeMap.cs   carte du prototype A (positions des gisements et de la livraison)
  Automaticlone.Core.Tests/
    Automaticlone.Core.Tests.csproj
    TestHelpers.cs    petits mondes et Run(steps)
    RulesTests.cs, MiningTests.cs, FurnaceTests.cs, BuildingTests.cs, GameTests.cs, GhostLookTests.cs, PrototypeMapTests.cs
  godot/
    project.godot
    Automaticlone.csproj
    scenes/Main.tscn
    scripts/Main.cs         lien joueur ↔ Core ↔ affichage, pas fixe, fin de vie
    scripts/Player.cs       contrôleur FPS (CharacterBody3D + Camera3D)
    scripts/StationView.cs  affichage d'une station (boîte + texte 3D)
    scripts/CloneView.cs    affichage d'un clone fantôme
    scripts/Hud.cs          oxygène, objet porté, objectif, visée, messages
    scripts/LifeEndPanel.cs écran entre deux vies (garder/rejeter, liste des clones)
```

---

### Task 1 : squelette de la solution et règles

**Files :**
- Create : `.gitignore` (contenu identique à `seed7` : `.godot/`, `bin/`, `obj/`, `*.user`, `.vs/`, `export/`), `Automaticlone.slnx`, `Automaticlone.Core/Automaticlone.Core.csproj`, `Automaticlone.Core/Rules.cs`, `Automaticlone.Core/Items.cs`
- Create : `Automaticlone.Core.Tests/Automaticlone.Core.Tests.csproj` (mêmes paquets que `seed7/Seed7.Core.Tests` : xunit 2.5.3, xunit.runner.visualstudio 2.5.3, Microsoft.NET.Test.Sdk 17.8.0, coverlet.collector 6.0.0, `<Using Include="Xunit" />`)
- Test : `Automaticlone.Core.Tests/RulesTests.cs`

**Interfaces :**
- Produces :
  - `public sealed record Rules(int StepsPerSecond = 60, int LifeSeconds = 60, int MineTicks = 20, int ResourcesPerExtraction = 1, int PileLimit = 10, int SmeltTicks = 5, int CoalBurnSeconds = 10, int GearTicks = 10, int GearIngots = 2, int FurnaceStoneCost = 3, int WorkbenchIngotCost = 2, int FurnaceInputCapacity = 5, int FurnaceOutputCapacity = 5, int WorkbenchInputCapacity = 4, int WorkbenchOutputCapacity = 2, int GoalGears = 5, float MinBuildSpacing = 2.5f, float WalkSpeed = 4f, float InteractRange = 3f, float FailFlashSeconds = 1.5f)` avec les propriétés calculées `int LifeSteps => LifeSeconds * StepsPerSecond` et `int StepsFor(int ticks) => ticks * StepsPerSecond`.
  - `public enum Item { Fer, Pierre, Charbon, Lingot, Engrenage }`
  - `public enum BuildingKind { Four, Etabli }`
  - `public enum ActionResult { Ok, UnknownStation, HandsFull, HandsEmpty, NothingToTake, WrongItem, StationFull, CannotWorkHere, PlacementBlocked }`

- [ ] **Step 1 : initialiser le dépôt.** `git init` à la racine d'`Automaticlone`, puis créer `.gitignore`, les deux `.csproj` (`net8.0`, `Nullable` et `ImplicitUsings` activés), et `Automaticlone.slnx` qui liste les deux projets (même format que `seed7/Seed7.slnx`).

- [ ] **Step 2 : écrire le test qui échoue**

```csharp
public class RulesTests
{
    [Fact]
    public void DefaultsMatchSpecPrototypeA()
    {
        var r = new Rules();
        Assert.Equal(3600, r.LifeSteps);          // 60 s à 60 pas/s
        Assert.Equal(1200, r.StepsFor(r.MineTicks)); // 20 ticks
        Assert.Equal(10, r.PileLimit);
        Assert.Equal(5, r.SmeltTicks);
        Assert.Equal(10, r.CoalBurnSeconds);
        Assert.Equal(3, r.FurnaceStoneCost);
        Assert.Equal(2, r.WorkbenchIngotCost);
        Assert.Equal((2, 10), (r.GearIngots, r.GearTicks));
        Assert.Equal(5, r.GoalGears);
    }
}
```

- [ ] **Step 3 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : ÉCHEC de compilation (`Rules` introuvable).

- [ ] **Step 4 : écrire `Rules.cs` et `Items.cs`** selon le bloc Interfaces, avec un en-tête de commentaire en français qui renvoie au §9 du cahier des charges.

- [ ] **Step 5 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : 1 test réussi.

- [ ] **Step 6 : commit**

```bash
git add .gitignore Automaticlone.slnx Automaticlone.Core Automaticlone.Core.Tests cahier-des-charges.md docs
git commit -m "Squelette de la solution et règles du prototype A"
```

---

### Task 2 : personnages, gisements, ramassage et minage

**Files :**
- Create : `Automaticlone.Core/Commands.cs`, `Automaticlone.Core/Actor.cs`, `Automaticlone.Core/Stations.cs` (classe de base et `ResourceDeposit`, `DeliveryPoint`), `Automaticlone.Core/World.cs`
- Test : `Automaticlone.Core.Tests/TestHelpers.cs`, `Automaticlone.Core.Tests/MiningTests.cs`

**Interfaces :**
- Consumes : `Rules`, `Item`, `ActionResult` (tâche 1).
- Produces :
  - `public abstract record Command;` avec `PickUp(int StationId)`, `Deposit(int StationId)`, `StartWork(int StationId)`, `StopWork()`, `PlaceBuilding(BuildingKind Kind, Vector3 Position)` ; `public sealed record TimedCommand(int Step, Command Command);`
  - `public sealed class Actor` : `Actor(int lifeNumber)`, `int LifeNumber`, `Item? Carried`, `int? WorkStationId`, `bool IsWaiting` (attente en cours), `bool HasWaited` (teinte orange permanente), `bool HasFailed` (teinte rouge permanente), `int? LastFailStep`, `void ResetForNewLoop()` (vide tout).
  - `public abstract class Station` : `int Id`, `Vector3 Position`, `virtual void ResetForNewLoop()`.
  - `public sealed class ResourceDeposit : Station` : `Item Resource`, `int Pile`, `int ProgressSteps`.
  - `public sealed class DeliveryPoint : Station` : `int GearsDelivered`.
  - `public sealed class World` : `World(Rules rules)`, `Rules Rules`, `IReadOnlyList<Station> Stations`, `ResourceDeposit AddDeposit(Item resource, Vector3 position)`, `DeliveryPoint AddDeliveryPoint(Vector3 position)`, `Station? Find(int id)`, `ActionResult Apply(Actor actor, Command command)`, `void Advance(IReadOnlyList<Actor> actors)` (un pas de travail), `void ResetForNewLoop()`. Les identifiants viennent d'un compteur qui ne revient jamais en arrière.

**Règles à implémenter (spec 3.5, 3.6) :**
- `PickUp` sur un gisement : mains pleines → `HandsFull` ; tas vide → `NothingToTake` ; sinon `Pile--` et l'objet passe en main.
- `Deposit` sur un gisement → `WrongItem`. `Deposit` avec les mains vides → `HandsEmpty`. Station inconnue → `UnknownStation`.
- `StartWork` sur un gisement → `Ok` et `WorkStationId` est défini (le minage ne demande pas d'avoir les mains vides). `StartWork` sur une `DeliveryPoint` → `CannotWorkHere`. `StopWork` → toujours `Ok`.
- Un échec ne change jamais l'objet porté.
- `Advance` : pour chaque gisement, compter les acteurs qui y travaillent. Si `Pile >= PileLimit`, ces acteurs ont `IsWaiting = true` et `HasWaited = true`, et rien n'avance. Sinon, `IsWaiting = false`, `ProgressSteps += nombre de travailleurs`, et quand `ProgressSteps >= StepsFor(MineTicks)` : `Pile += ResourcesPerExtraction` (sans dépasser `PileLimit`) et `ProgressSteps -= StepsFor(MineTicks)`. Les acteurs qui ne travaillent nulle part ont `IsWaiting = false`.
- `ResetForNewLoop` du gisement : `Pile = 0`, `ProgressSteps = 0`.

- [ ] **Step 1 : écrire les tests qui échouent** (`TestHelpers.Run(this World w, IReadOnlyList<Actor> actors, int steps)` appelle `Advance` en boucle).

```csharp
[Fact] public void OneMinerProducesOneOreAfter20Seconds()
{   // 1199 pas : Pile == 0 ; 1200 pas : Pile == 1
}
[Fact] public void TwoMinersStackTicks()          // 600 pas à deux → Pile == 1
[Fact] public void ProgressIsKeptWhenMinerLeaves() // 720 pas seul, StopWork, puis un autre acteur 480 pas → Pile == 1
[Fact] public void PickUpTakesFromPileAndFailsWhenEmptyOrHandsFull()
{   // Pile 1 → PickUp Ok, Carried == Fer, Pile 0 ; un 2e PickUp → HandsFull ; un autre acteur → NothingToTake
}
[Fact] public void MinerBlocksAtPileLimit()       // règles de test avec PileLimit = 3 : après 3 × 1200 pas, Pile == 3, IsWaiting, HasWaited ; 1200 pas de plus → Pile toujours 3
[Fact] public void BlockedMinerResumesWhenPileFreed()
{   // Pile pleine et mineur en attente → PickUp par un autre acteur → Advance 1 pas :
    // (PileLimit = 3) IsWaiting == false, HasWaited == true ; 1200 pas plus tard, Pile == 3
}
[Fact] public void ResetForNewLoopEmptiesPilesAndProgress()
```

- [ ] **Step 2 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : ÉCHEC de compilation.
- [ ] **Step 3 : implémenter** `Commands.cs`, `Actor.cs`, `Stations.cs` (base, `ResourceDeposit`, `DeliveryPoint`) et `World.cs` selon les règles ci-dessus. `Apply` aiguille selon le type de commande, puis selon le type de station.
- [ ] **Step 4 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : tous les tests réussis.
- [ ] **Step 5 : commit** — `git commit -m "Gisements : ramassage, minage en ticks, limite au sol et attente"`

---

### Task 3 : le four

**Files :**
- Modify : `Automaticlone.Core/Stations.cs` (ajouter `Furnace`), `Automaticlone.Core/World.cs` (`Apply` et `Advance` pour le four)
- Test : `Automaticlone.Core.Tests/FurnaceTests.cs`

**Interfaces :**
- Consumes : `World`, `Actor`, `Station` (tâche 2).
- Produces : `public sealed class Furnace : Station` : `int Iron`, `int Coal`, `int Ingots`, `int BurnStepsLeft`, `int ProgressSteps`, `bool IsLit => BurnStepsLeft > 0`. `World.AddFurnace(Vector3 position)`, qui sert dans les tests ; la tâche 4 crée les fours à partir des chantiers.

**Règles (spec §9, fonctionnement du four) :**
- `Deposit` : `Fer` → `Iron++`, `Charbon` → `Coal++`, chacun limité à `FurnaceInputCapacity` (au-delà : `StationFull`). Un autre objet → `WrongItem`.
- `PickUp` : `Ingots > 0` → un `Lingot` en main, sinon `NothingToTake`.
- `StartWork` sur un four → `Ok`.
- `Advance`, dans cet ordre, à chaque pas :
  1. si `BurnStepsLeft == 0` et `Coal > 0` : `Coal--`, `BurnStepsLeft = CoalBurnSeconds * StepsPerSecond` (allumage automatique dès qu'il y a du charbon) ;
  2. si le four est allumé : `BurnStepsLeft--` (il brûle même à vide) ;
  3. le four « peut fondre » s'il était allumé à ce pas, si `Iron > 0` et si `Ingots < FurnaceOutputCapacity`. Dans ce cas, `ProgressSteps += 1 + travailleurs` et, à `StepsFor(SmeltTicks)` : `Iron--`, `Ingots++`, `ProgressSteps -= StepsFor(SmeltTicks)` ;
  4. les travailleurs au four sont en attente (`IsWaiting`, `HasWaited`) quand le four ne peut pas fondre.
- `ResetForNewLoop` : tout revient à zéro.

- [ ] **Step 1 : écrire les tests qui échouent**

```csharp
[Fact] public void LitFurnaceSmeltsAloneOneIngotEvery5Seconds()   // 1 charbon, 3 fers, 600 pas → Ingots == 2, Iron == 1
[Fact] public void HelperDoublesSmeltingSpeed()                    // 1 charbon, 3 fers, 1 travailleur, 300 pas → Ingots == 2
[Fact] public void CoalBurnsEvenWithoutIron()                      // 1 charbon, 0 fer, 600 pas → Coal == 0, IsLit == false
[Fact] public void NextCoalIsLitAutomatically()                    // 2 charbons, 601 pas → Coal == 0, IsLit
[Fact] public void WorkerWaitsWhenFurnaceUnlitOrEmpty()            // pas de charbon : travailleur IsWaiting, HasWaited
[Fact] public void DepositRejectsWrongItemAndFullInput()           // Pierre → WrongItem, 6e fer → StationFull, l'objet reste en main
[Fact] public void FullOutputStopsSmeltingButCoalStillBurns()
```

- [ ] **Step 2 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : ÉCHEC.
- [ ] **Step 3 : implémenter `Furnace`** et ses branches dans `World.Apply` et `World.Advance`.
- [ ] **Step 4 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : tous les tests réussis.
- [ ] **Step 5 : commit** — `git commit -m "Four : charbon, fonte autonome et aide des personnages"`

---

### Task 4 : chantiers, établi et livraison

**Files :**
- Modify : `Automaticlone.Core/Stations.cs` (ajouter `BuildSite`, `Workbench`), `Automaticlone.Core/World.cs`
- Test : `Automaticlone.Core.Tests/BuildingTests.cs`

**Interfaces :**
- Consumes : `World`, `Furnace` (tâches 2 et 3).
- Produces :
  - `public sealed class BuildSite : Station` : `BuildingKind Kind`, `Item Material` (`Pierre` pour un four, `Lingot` pour un établi), `int Needed`, `int Delivered`. Son `ResetForNewLoop` ne fait **rien** (les matériaux persistent, spec §4).
  - `public sealed class Workbench : Station` : `int Ingots`, `int Gears`, `int ProgressSteps`.
  - `World.AddWorkbench(Vector3 position)` pour les tests.

**Règles (spec 3.4, §4, §9) :**
- `PlaceBuilding(kind, pos)` : si une station est à moins de `MinBuildSpacing` de `pos` (distance horizontale X/Z) → `PlacementBlocked`. Sinon, un `BuildSite` est créé avec un nouvel identifiant. Cette règle couvre aussi le clone qui rejoue la pose d'un bâtiment qui existe déjà : il échoue, comme le veut la spec 3.4.
- `Deposit` sur un `BuildSite` : le bon matériau → `Delivered++` ; quand `Delivered == Needed`, le chantier est **remplacé** dans `Stations`, au même index, par un `Furnace` ou un `Workbench` qui garde le même `Id` et la même `Position`. Un autre objet → `WrongItem`. `PickUp` et `StartWork` sur un chantier → `NothingToTake` et `CannotWorkHere`.
- Un apport à un chantier déjà terminé vise un four ou un établi (même identifiant) : une pierre déposée dans un four donne `WrongItem` et la pierre reste en main.
- `Workbench` : `Deposit(Lingot)` → `Ingots++` (limité à `WorkbenchInputCapacity`, sinon `StationFull`), un autre objet → `WrongItem`. `PickUp` → un `Engrenage` si `Gears > 0`. `StartWork` → `Ok`. `Advance` : l'établi n'apporte aucun tick lui-même. Si `Ingots >= GearIngots` et `Gears < WorkbenchOutputCapacity`, alors `ProgressSteps += travailleurs`, et à `StepsFor(GearTicks)` : `Ingots -= GearIngots`, `Gears++`. Sinon, les travailleurs sont en attente. `ResetForNewLoop` : tout à zéro.
- `DeliveryPoint` : `Deposit(Engrenage)` → `GearsDelivered++`, un autre objet → `WrongItem`. `ResetForNewLoop` : `GearsDelivered = 0`.

- [ ] **Step 1 : écrire les tests qui échouent**

```csharp
[Fact] public void PlacingCreatesSiteAndThreeStonesMakeAFurnaceWithSameId()
[Fact] public void PlacementTooCloseIsBlocked()                    // 2,4 m d'une station → PlacementBlocked ; 2,6 m → Ok
[Fact] public void SiteMaterialsSurviveLoopReset()                 // 2 pierres livrées, ResetForNewLoop → Delivered == 2
[Fact] public void StoneToFinishedFurnaceFailsAndStaysInHand()     // WrongItem, Carried == Pierre
[Fact] public void WorkbenchMakesGearFromTwoIngotsIn10Ticks()      // 2 lingots, 1 travailleur, 599 pas → 0 engrenage, 600 → 1
[Fact] public void WorkbenchDoesNotWorkAlone()                     // 2 lingots, aucun travailleur, 1200 pas → 0 engrenage
[Fact] public void DeliveryCountsGearsOnly()
```

- [ ] **Step 2 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : ÉCHEC.
- [ ] **Step 3 : implémenter** `BuildSite` et `Workbench`, les branches de `Apply` et `Advance`, et le remplacement du chantier par le bâtiment terminé.
- [ ] **Step 4 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : tous les tests réussis.
- [ ] **Step 5 : commit** — `git commit -m "Chantiers persistants, établi et livraison des engrenages"`

---

### Task 5 : boucle de vie, enregistrement et rejeu des clones

**Files :**
- Create : `Automaticlone.Core/Recording.cs`, `Automaticlone.Core/Game.cs`, `Automaticlone.Core/GhostLook.cs`, `Automaticlone.Core/PrototypeMap.cs`
- Test : `Automaticlone.Core.Tests/GameTests.cs`, `Automaticlone.Core.Tests/GhostLookTests.cs`, `Automaticlone.Core.Tests/PrototypeMapTests.cs`

**Interfaces :**
- Consumes : tout le Core des tâches 1 à 4.
- Produces :
  - `public readonly record struct Pose(Vector3 Position, float Yaw, float Pitch);`
  - `public sealed record LifeRecording(int LifeNumber, IReadOnlyList<Pose> Poses, IReadOnlyList<TimedCommand> Commands);`
  - `public sealed class Clone` : `LifeRecording Recording`, `Actor Actor`, `Pose PoseAt(int step)` (la dernière pose si `step` dépasse la fin).
  - `public sealed class Game` : `Game(Rules rules, Action<World> buildMap)`, `Rules Rules`, `World World`, `int LifeNumber` (commence à 1), `int Step`, `bool IsLifeOver => Step >= Rules.LifeSteps`, `Actor Player`, `IReadOnlyList<Clone> Clones` (triés par `LifeNumber` croissant), `int GearsDeliveredThisLoop`, `bool GoalReached`, `void SetPlayerPose(Pose pose)`, `IReadOnlyList<ActionResult> Tick(IReadOnlyList<Command> playerCommands)`, `LifeRecording FinishLife(bool keep)`, `bool DeleteClone(int lifeNumber)`.
  - `public enum GhostLook { Normal, TintOrange, TintRed, Waiting, FailFlash }` et `public static GhostLook GhostLooks.For(Actor actor, int step, Rules rules)`.
  - `public static class PrototypeMap` : `Vector3 Spawn`, `void Build(World world)`.

**Règles (spec 3.1 à 3.3, 3.7, §4) :**
- `Tick`, dans cet ordre :
  1. **Clones**, par ordre de vie croissant : appliquer chaque `TimedCommand` dont `Step == Step` courant. En cas d'échec : `HasFailed = true`, `LastFailStep = Step`.
  2. **Joueur** (en dernier, spec 3.7) : appliquer `playerCommands` dans l'ordre et renvoyer leurs résultats. **Seules les commandes `Ok` sont enregistrées**, avec le `Step` courant. La pose donnée par `SetPlayerPose` est ajoutée à l'enregistrement en cours.
  3. `World.Advance(clones puis joueur)`.
  4. `Step++`.
  - Si `IsLifeOver`, `Tick` ne fait rien et renvoie une liste vide.
- `FinishLife(keep)` : construit le `LifeRecording` de la vie en cours. Si `keep`, ajoute un `Clone`. Puis `World.ResetForNewLoop()`, `Step = 0`, `LifeNumber++`, nouveau `Player`, et `ResetForNewLoop()` de chaque acteur de clone (le curseur de commandes revient au début).
- `DeleteClone` : retire le clone tout de suite, même en pleine vie. Ses effets sur le monde restent.
- `GearsDeliveredThisLoop` : la somme des `DeliveryPoint.GearsDelivered`. `GoalReached` : cette somme `>= GoalGears`.
- `GhostLooks.For`, par priorité décroissante : `FailFlash` si `LastFailStep` date de moins de `FailFlashSeconds` ; `Waiting` si `IsWaiting` ; `TintRed` si `HasFailed` ; `TintOrange` si `HasWaited` ; sinon `Normal`.
- `PrototypeMap.Build` : point de livraison en (0, 0, -3) ; fer en (-8, 0, -20) ; pierre en (0, 0, -22) ; charbon en (8, 0, -20). `Spawn = (0, 0, 0)`. Les gisements sont donc à environ 5 s de marche (4 m/s) de la livraison.

- [ ] **Step 1 : écrire les tests qui échouent.** `GameTests` utilise une petite carte de test (un gisement de fer avec `Pile` préréglé, un four, une livraison) et un helper `PlayLife(Game g, params (int step, Command cmd)[] script)` qui fait des `Tick` jusqu'à `IsLifeOver` en injectant les commandes aux pas indiqués.

```csharp
[Fact] public void KeptLifeReplaysAsCloneWithSameCommandsAndPoses()
[Fact] public void RejectedLifeAddsNoClone()
[Fact] public void PlayerFailedCommandsAreNotRecorded()
{   // le joueur tente PickUp sur un tas vide (NothingToTake) puis réussit plus tard ;
    // la vie gardée contient 1 seule commande, et le clone n'a pas HasFailed à la boucle suivante
}
[Fact] public void CloneFailsWhenPlayerTookTheOreFirst()
{   // vie 1 : PickUp au pas 100 (Pile = 1 au départ) ; vie 2 : le joueur ramasse au pas 50
    // → au pas 100, le clone 1 a HasFailed == true, LastFailStep == 100 et Carried == null
}
[Fact] public void FailedPickupCascadesIntoFailedDeposit()
[Fact] public void OlderLifeWinsTheLastOreOnSameStep()
{   // clones 1 et 2 ramassent au même pas, un seul minerai : le clone 1 l'a, le clone 2 échoue
}
[Fact] public void DeletingWorkingCloneKeepsStationProgress()
[Fact] public void RejectedLifeKeepsBuildingsAndSiteMaterials()
{   // vie 1 : pose un four et livre 2 pierres ; FinishLife(false) → le chantier est toujours là avec Delivered == 2
}
[Fact] public void NewLoopEmptiesFurnaceAndDeliveries()
[Fact] public void GoalReachedAtFiveGearsInOneLoop()
[Fact] public void TickDoesNothingAfterLifeIsOver()
// GhostLookTests : une assertion par niveau de priorité, plus le passage de FailFlash à TintRed après 1,5 s (90 pas)
// PrototypeMapTests : chaque gisement est entre 16 et 24 m du point de livraison (environ 5 s à 4 m/s)
```

- [ ] **Step 2 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : ÉCHEC.
- [ ] **Step 3 : implémenter** `Recording.cs`, `Game.cs`, `GhostLook.cs` et `PrototypeMap.cs`.
- [ ] **Step 4 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : tous les tests réussis.
- [ ] **Step 5 : commit** — `git commit -m "Boucle de vie : enregistrement, rejeu, priorité des vies, statuts des clones"`

---

### Task 6 : projet Godot, joueur FPS et interactions

**Files :**
- Create : `godot/project.godot`, `godot/Automaticlone.csproj`, `godot/scenes/Main.tscn`, `godot/scripts/Main.cs`, `godot/scripts/Player.cs`, `godot/scripts/StationView.cs`, `godot/scripts/Hud.cs`, `lancer.bat`

**Interfaces :**
- Consumes : `Game`, `PrototypeMap`, `Command`, `ActionResult`, les stations (tâche 5).
- Produces : `Main` (Node3D) expose `Game Game` et `int CurrentStep`, et c'est lui qui appelle `Tick`. `StationView` porte `int StationId` sur son `StaticBody3D` (métadonnée `station_id`), ce qui permet au rayon de visée de retrouver la station.

**Détails :**
- `godot/Automaticlone.csproj` : `Godot.NET.Sdk/4.7.2`, `net8.0`, `RootNamespace` `Automaticlone.Game`, référence à `..\Automaticlone.Core\Automaticlone.Core.csproj`. Ajouter ce projet à `Automaticlone.slnx`.
- `project.godot` : `config/name="Automaticlone"`, scène principale `res://scenes/Main.tscn`, `physics/common/physics_ticks_per_second=60`, rendu `gl_compatibility`, assembly `Automaticlone`.
- Les actions clavier et souris sont créées dans `Main._Ready` avec des touches physiques : `avancer` W, `reculer` S, `gauche` A, `droite` D, `agir` clic gauche, `travailler` clic droit maintenu, `poser_four` 1, `poser_etabli` 2, `supprimer_clone` Suppr, `liberer_souris` Échap.
- `Player` (CharacterBody3D, capsule et Camera3D) : vitesse `Rules.WalkSpeed`, souris capturée, pas de saut. Dans `_PhysicsProcess` : rayon depuis la caméra (`InteractRange` pour les stations, 30 m pour le sol). Les commandes produites :
  - clic gauche sur une station : `Deposit` si on porte un objet, sinon `PickUp` ;
  - clic droit maintenu sur une station : `StartWork` au moment de l'appui ; `StopWork` au relâchement ou si la visée quitte la station ;
  - touche 1 ou 2 en visant le sol : `PlaceBuilding`.
- `Main._PhysicsProcess` : `Game.SetPlayerPose(...)`, puis `Tick(commandes de la frame)`, puis transmission des résultats non `Ok` au `Hud` (message en français pendant 2 s, par exemple « Rien à ramasser », « Mains pleines », « Trop près d'un autre bâtiment »). Quand `IsLifeOver`, la simulation s'arrête (la tâche 8 branche l'écran de fin).
- `StationView` : une boîte de couleur par type (fer gris-bleu, pierre beige, charbon noir, four rouge brique, établi bois, chantier en fil de fer, livraison vert), plus un `Label3D` mis à jour chaque frame (par exemple « Fer — au sol 2/10 — 45 % », « Four — feu 6 s — fer 1, charbon 2, lingots 0 », « Chantier four — pierres 2/3 »). `Main` crée et retire les vues en comparant les `Id` du Core, et recrée la vue quand un chantier devient un bâtiment.
- `Hud` : oxygène restant (« O₂ 42 s »), objet porté, « Engrenages : 2/5 », numéro de vie, aide contextuelle sur la cible visée.
- `lancer.bat` : copie du `lancer.bat` de `seed7`, sans l'option `autodemo`, avec `godot\Automaticlone.csproj`.

- [ ] **Step 1 : créer le projet Godot et lancer `dotnet build godot/Automaticlone.csproj`.** Résultat attendu : compilation réussie, 0 erreur.
- [ ] **Step 2 : implémenter** `Main`, `Player`, `StationView` et `Hud`.
- [ ] **Step 3 : vérification manuelle avec `lancer.bat`.** Attendu :
  - on se déplace en ZQSD ;
  - miner le fer au clic droit pendant 20 s fait tomber 1 minerai (texte « au sol 1/10 »), qu'on ramasse au clic gauche ;
  - la touche 1 pose un chantier de four, qui devient un four après 3 pierres ;
  - l'oxygène descend de 60 à 0, puis la simulation s'arrête.
- [ ] **Step 4 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : tout réussit encore.
- [ ] **Step 5 : commit** — `git commit -m "Godot : joueur FPS, stations et interface minimale"`

---

### Task 7 : clones fantômes

**Files :**
- Create : `godot/scripts/CloneView.cs`
- Modify : `godot/scripts/Main.cs`, `godot/scripts/Player.cs`

**Interfaces :**
- Consumes : `Game.Clones`, `Clone.PoseAt`, `GhostLooks.For` (tâche 5).
- Produces : `CloneView` (Node3D) avec `int LifeNumber` et `void Sync(Clone clone, int step, Rules rules)`.

**Détails :**
- Chaque clone est une capsule avec un `StandardMaterial3D` transparent et émissif. Couleurs selon `GhostLook` :
  - `Normal` : cyan, alpha 0,35 ;
  - `TintOrange` : orange léger, alpha 0,35 ;
  - `TintRed` : rouge léger, alpha 0,35 ;
  - `Waiting` : orange vif, alpha 0,7 ;
  - `FailFlash` : rouge vif, alpha 0,8.
  
  Un `Label3D` au-dessus affiche « Vie 3 » et l'objet porté.
- Position et orientation viennent de `PoseAt(Step)`. Le joueur traverse les clones : ils sont détectés par un `Area3D` sur le calque 2, et n'ont aucun corps physique.
- Touche Suppr en visant un clone (rayon de 30 m sur le calque 2) : `Game.DeleteClone(lifeNumber)` et la vue est retirée.

- [ ] **Step 1 : implémenter** `CloneView` et la synchronisation dans `Main` (création et retrait par `LifeNumber`).
- [ ] **Step 2 : lancer `dotnet build godot/Automaticlone.csproj`.** Résultat attendu : 0 erreur.
- [ ] **Step 3 : vérification manuelle**, avec une fin de vie provisoire (touche F9 = `FinishLife(true)`, retirée à la tâche 8). Attendu :
  - la vie 1 est rejouée en cyan ;
  - si le joueur ramasse le minerai avant le clone, celui-ci flashe en rouge, puis garde une teinte rouge ;
  - un mineur bloqué par un tas plein devient orange vif, puis garde une teinte orange ;
  - Suppr en visant un clone le fait disparaître.
- [ ] **Step 4 : commit** — `git commit -m "Godot : clones fantômes, couleurs d'état et suppression par visée"`

---

### Task 8 : écran entre deux vies et victoire

**Files :**
- Create : `godot/scripts/LifeEndPanel.cs`
- Modify : `godot/scripts/Main.cs` (retirer F9), `godot/scripts/Hud.cs`

**Interfaces :**
- Consumes : `Game.FinishLife`, `Game.DeleteClone`, `Game.Clones`, `Game.GearsDeliveredThisLoop`, `Game.GoalReached`.
- Produces : `LifeEndPanel` (Control) avec `void Show(Game game)` et le signal `Decided(bool keep)`.

**Détails :**
- Quand `IsLifeOver` : la souris est libérée et le panneau affiche :
  - « Vie n° N terminée » et « Engrenages livrés : x/5 » ;
  - la liste des clones (« Vie 1 », avec un bouton « Supprimer » chacun) ;
  - deux boutons, « Garder cette vie » et « Rejeter cette vie ».
  
  Le choix appelle `FinishLife(keep)`, replace le joueur sur `PrototypeMap.Spawn`, recapture la souris et relance la simulation.
- Quand `GoalReached` devient vrai pour la première fois dans une boucle : le message « Objectif atteint : 5 engrenages livrés ! » s'affiche au centre pendant 5 s. On peut continuer à jouer.

- [ ] **Step 1 : implémenter** `LifeEndPanel` et le branchement dans `Main`.
- [ ] **Step 2 : lancer `dotnet build godot/Automaticlone.csproj`.** Résultat attendu : 0 erreur.
- [ ] **Step 3 : vérification manuelle de la boucle complète avec `lancer.bat`.** Attendu :
  - à 0 d'oxygène, le panneau apparaît ;
  - « Rejeter » n'ajoute pas de clone, mais le chantier commencé reste ;
  - « Garder » ajoute un clone qui rejoue la vie ;
  - le bouton « Supprimer » du panneau retire le clone visé ;
  - livrer 5 engrenages en une boucle affiche le message de victoire.
- [ ] **Step 4 : lancer `dotnet test Automaticlone.Core.Tests`.** Résultat attendu : tout réussit.
- [ ] **Step 5 : commit** — `git commit -m "Écran entre deux vies (garder, rejeter, supprimer) et victoire"`
