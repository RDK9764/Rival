# RIVAL

A 3D action game where the player fights progressively smarter copies of
themselves. The enemy is not a recording of button presses — it is a behavioral
model distilled from your combat style and replayed through real AI.

**Status: Milestone 7 — Persistent loadout & progression; the Rival mirrors you.**
Weapon choice is now part of the meta-game. `WeaponCatalog` (one ScriptableObject
over the 6 weapons + attack chains) lets the player swap equipment at runtime.
`LoadoutStore` (SaveSystem) persists the equipped weapon, unlocked set, and
currency via JSON. `PlayerLoadoutManager` (Enemy) owns runtime progression,
applies the equipped weapon to the player's CombatController via a new `Equip()`
API, awards currency on match results (win 25 / loss 10, plus 10 per endless
wave and a 25 bonus at 10+ waves), and unlocks weapons for a spend. The runtime
`LoadoutSelectMenu` (UI) lists every weapon with locked/unlocked state and cost,
equips/unlocks directly, and is reachable from the pause menu's new LOADOUT
option. `PlayerHUD` shows the equipped weapon name; `RivalHUD` shows "MIRROR".
`ProceduralFighterBuilder` now hides the original capsule mesh (model parts only)
and the Rival visibly wields the same weapon as you — switching weapons mid-match
(via the pause menu) re-equips and re-builds the rival live. `GameOverUI` reports
currency earned this match, your balance, and the next unlock.

**Polish batch (ready-to-play fixes).** The main menu now offers two game modes
via horizontal browsing: **MATCH** (best-of-N rounds) and **ENDLESS** (escalating
waves) — endless finally has an entry point and wires into the existing
`EndlessModeManager` (death handling, wave rewards, best-wave record, endless
game-over screen). All nested menus now support **Back** (Esc / gamepad East)
via a new `IMenuInput.BackPressed`; Esc returns through Settings and Loadout to
the pause menu, and the game-over screen back to the main menu.
`ProceduralBodyAnimator` (Combat) brings the primitive fighter models to life
with zero animation assets — idle breathing, walk/run limb swings, attack
lunges, dodge rolls, hit flinches, block/stun poses and a death fall, plus a
"get back up" cue whenever health resets between rounds/waves. Equipping any
weapon now rebuilds the *player's* model too (not just the rival's).
`CameraZoomEffect` finally subscribes to the right combat controller (the
player's) — it previously attached to a camera rig with no parent combat rig, so
heavy-hit zoom never fired. Rival archetype accent colors now paint the
procedural model's belts/blades via `ProceduralFighterBuilder.SetAccentColor`
instead of the hidden capsule.

**Meta-comfort batch.** The main menu now lists SETTINGS and QUIT rows beneath
the roster (vertical navigation) — Settings opens in-place and returns on Esc;
QUIT / Esc exits the game. The pause menu gained a 5th QUIT option.
`TutorialManager` only runs in MATCH mode (Endless starts straight into the
action). `WaveAnnouncementUI` now reports "WAVE X CLEARED / next wave
approaching" between waves instead of doing nothing. `ProjectSetup` seeds
product/company/version and a dark splash background so builds carry sane
packaging defaults (an icon asset is the only thing left for shipping).

**Difficulty & stats fixes.** `EndlessModeManager` no longer mutates the shared
`RivalArchetype` ScriptableObjects in place: pristine base values are captured
at run start and every wave *recomputes* from them, and the assets are restored
when the run ends — so endless waves never compound and MATCH mode is never
affected by a previous endless run. A death in endless now also records a
lifetime **Loss** (previously endless deaths were invisible to the stats
tracker).

## Requirements

- Unity 6 (6000.0.x) recommended; any 2022.3+ works.
- New Input System (already declared in `Packages/manifest.json`).

If you use a different editor version, edit `ProjectSettings/ProjectVersion.txt`.

## How to open

1. Unity Hub → **Add** → select this folder.
2. Let Unity import and generate `.meta` files.
3. Run **Tools → Rival → Setup Project** from the menu bar. This creates all
   ScriptableObject assets, three scenes (Boot, MainMenu, Gameplay), full
   GameObject wiring, and registers scenes in Build Settings.
4. Open the **Boot** scene and press Play.

## Folder layout

```
Assets/
  Scripts/
    Core/            game state, GameManager, scene loading, boot, time control, IMatchConfig,
                     AudioService, SfxLibrary, HitStop, GameFeelService, CombatFeelConnector, HitFlash,
                     GameSettings, StatsTracker, VfxService, HitVfxSpawner, ArenaEnvironment,
                     MusicService, ProceduralAudioGenerator, ProceduralFighterBuilder
    Input/           IPlayerInput + IMenuInput contracts + InputReader (Input System)
    Actors/          shared actor primitives: stamina, CharacterMotor, movement config, WeaponTrail
    Player/          player controller (drives the shared motor from IPlayerInput)
    Camera/          orbit camera + ICameraRig contract + CameraZoomEffect
    Utilities/       ServiceLocator (DI) + DirectionMapper (pure math)
    Combat/          damage models, state machine, hitbox, lock-on, training dummy, AnimatorAdapter,
                     WeaponCatalog, ProceduralBodyAnimator
    Weapons/         weapon + attack definition assets (Sword, Axe, Spear, Dagger, Hammer, Fist)
    Telemetry/       combat event bus, per-actor recorder, console sink
    BehaviorAnalysis/ profile traits + analyzer + profiler
    SaveSystem/      JSON telemetry sink, IProfileStore, ILoadoutStore, profile/loadout persisters
    Enemy/           RivalBrain + RivalController + RivalArchetype/Roster/MatchSetup + MatchController +
                     RoundManager + EndlessModeManager + PlayerLoadoutManager
    UI/              PlayerHUD + RivalHUD + CharacterSelectMenu + MainMenuUI + PauseMenuUI +
                     GameOverUI + LoadingScreenUI + DamageNumbers + ComboCounter +
                     RoundAnnouncementUI + SettingsMenuUI + WaveAnnouncementUI +
                     TutorialManager + TutorialUI + MainMenuStatsDisplay + LoadoutSelectMenu
    Editor/          ProjectSetup (one-click scene + asset generation)
    AI/ Animation/ Managers/ Data/   (empty — later milestones)
  ScriptableObjects/ Prefabs/ Scenes/ Animations/ Audio/ Models/
```

Each `Scripts/` layer folder carries its own `.asmdef` assembly
(`Rival.<Layer>`) whose declared references *enforce* the dependency direction
below — a cross-layer reference the graph doesn't allow is a compile error, not
a convention.

## Assembly boundaries

```
Rival.Utilities (none)        ← no dependencies, usable by everyone
Rival.Actors (none)           ← stamina, CharacterMotor, movement config
Rival.Weapons (none)          ← weapon data
Rival.Combat   ← Utilities, Actors, Weapons
Rival.Input    ← Utilities, Combat, Unity.InputSystem
Rival.Camera   ← Utilities, Input
Rival.Core     ← Utilities, Input, Combat
Rival.Telemetry← Combat, Actors
Rival.BehaviorAnalysis ← Telemetry
Rival.SaveSystem ← Telemetry, BehaviorAnalysis, Utilities
Rival.Enemy    ← Actors, Combat, BehaviorAnalysis, Core, Utilities, SaveSystem, Telemetry
Rival.UI       ← Actors, Combat, Core, Input, Enemy, Utilities
Rival.Player   ← Actors, Camera, Combat, Core, Input, Utilities
Rival.Editor   ← all game assemblies + UnityEditor (Editor-only)
```

Rules this enforces: gameplay never references input hardware (Input can be
replaced or removed without touching Combat); Telemetry depends downward on
Combat, never upward; SaveSystem only reads what analysis and telemetry already
produced (it never touches producers); Enemy sits on top of every analysis and
persistence layer; no cycle between any two layers.

## Boot flow

```
Boot scene (persistent)                 MainMenu scene               Gameplay scene
  GameManager ──state changes──────────► MainMenuUI ──loads──────► Player (Motor, Controller, Stamina, Combat, HUD)
  SceneLoader ──loads scenes───────────►  (writes IMatchConfig)    Camera (PlayerCameraController)
  Bootstrapper ──kicks off MainMenu     InputReader (menu map)     Rival (Motor, Combat, Brain, Controller)
  TimeScaleService ──pause → time scale 0                          CharacterSelectMenu (optional)
  PauseHandler ──Esc → GameManager.TogglePause
  InputReader ──sceneLoaded→ resets to gameplay mode
  MatchConfig ──IMatchConfig (persistent)

  Player & Rival emit CombatEvents ──► Telemetry bus ──► BehaviorProfiler
                                                            │
                                     RivalBrain ◄──copies──┘ (live profile of the Player)
                                                            │
   JsonTelemetrySink ◄─────── bus (JSON log per session) ────┘
   ProfilePersister ──checkpoints──► IProfileStore ──► RivalSaves/*.json
   RivalBrain.Start ──seeds from──► stored profile (remembers the last session)
```

Services register themselves in `ServiceLocator` under interfaces:

| Interface      | Implementation        | Registered by      |
|----------------|-----------------------|--------------------|
| `ISceneLoader` | `SceneLoader`         | its own Awake      |
| `IPlayerInput` | `InputReader`         | its own Awake      |
| `IMenuInput`   | `InputReader`         | its own Awake      |
| `ICameraRig`   | `PlayerCameraController` | its own Awake   |
| `ITimeService` | `TimeScaleService`    | its own Awake      |
| `IMatchConfig` | `MatchConfig`         | its own Awake      |
| `IAudioService`| `AudioService`        | its own Awake      |
| `IGameFeelService` | `GameFeelService` | its own Awake      |
| `IProfileStore`| `ProfileStore`        | its own Awake      |
| `GameManager`  | `GameManager` (concrete) | its own Awake   |
| `RivalMatchSetup` | `RivalMatchSetup` (concrete) | its own Awake |

No gameplay code references a concrete service directly.

Combat is **per-actor**, not a global service: each fighting actor owns its
`CombatController`, `CombatInputBridge`, `LockOnController`, `MeleeHitbox` and
a combat input source (`InputReader` for the player, `TrainingDummyBrain` for
the dummy). The bridge and lock-on resolve their input source as: inspector
override → component on the actor → global `ServiceLocator`.

## Scene setup

Create three scenes — **Boot**, **MainMenu**, **Gameplay** — then add all three
to File → Build Settings. Names must match `SceneRegistry`.

### Boot scene

1. Create an empty GameObject `BootSystems`.
2. Add components: `GameManager`, `SceneLoader`, `Bootstrapper`,
   `PauseHandler`, `TimeScaleService`, `MatchConfig`.
3. `InputReader` lives here (persistent across scenes; resets to gameplay mode
   via `sceneLoaded` after every transition). The main menu re-activates menu
   mode in its `Start()`.

### MainMenu scene

1. Create an empty GameObject `MainMenu`.
2. Add `MainMenuUI`. Assign a `RivalRoster` asset (the same one used in the
   Gameplay scene) so the roster is visible on the title screen.
3. On Start it resolves `IMenuInput`, `ISceneLoader`, `IMatchConfig`,
   `ITimeService` and `GameManager` from the ServiceLocator. It activates menu
   mode, freezes time, and sets `GameManager` state to `MainMenu`.
4. Confirming writes the selected index into `IMatchConfig.SelectedIndex`,
   loads the Gameplay scene asynchronously, and sets `GameManager` to
   `Loading` → `Playing` on completion.

### Gameplay scene

Player (root `Player`):

1. `CapsuleCollider` — center `(0, 1, 0)`, height `2`, radius `0.5`.
2. `Rigidbody` — mass `1` (interpolation and collision mode are set in code).
3. `CharacterMotor`, `PlayerController`, `StaminaComponent`,
   `DodgeInvulnerability` (the last three are auto-added by
   `RequireComponent`).
4. Child empty `LookTarget` at `(0, 1.5, 0)`.
5. Create a `CharacterMovementConfig` (Assets → right-click →
   Create → Rival/Actors/Character Movement Config) and assign it to
   `PlayerController.Movement Config`. It holds movement values and stamina
   *costs*; the stamina resource itself is tuned on `StaminaComponent`.
6. Add `HealthComponent` and `CombatController`. `RequireComponent`
   auto-adds `StaminaComponent`, `DamageReceiver` and `CombatInputBridge`.
7. Add `LockOnController` and `PlayerHUD`.
8. Child empty `Hitbox` near the front of the player (e.g. `(0, 1, 1.2)`)
   with a `MeleeHitbox` component. Its world position plus the weapon's
   `Attack Range` define where swings connect.
9. Create a `Weapon` and `AttackDefinition`s (Assets → right-click →
   Create → Rival/Combat/…) and build a combo chain. Assign both to
   `CombatController`.
10. On `LockOnController`, set `Target` = the Rival.
11. Add `CombatTelemetryRecorder`; set `Actor Name` = Player and
    `Opponent` = Rival.

> `InputReader` is **not** on the Player anymore — it lives in the Boot scene
> (persistent, cross-cutting). `IPlayerInput` and `IMenuInput` are resolved
> from the ServiceLocator.

> Guarding disables stamina regeneration, so sustained blocking drains stamina
> and eventually breaks the guard — the block-breaking mechanic is meaningful
> by design, not by accident of regen tuning.

Camera:

1. GameObject `MainCamera` with a `Camera` component.
2. Add `PlayerCameraController`.
3. Assign `Follow Target` = Player, `Look Target` = Player/LookTarget,
   `Camera Config` = the `CameraConfig` asset
   (Create → Rival/Camera/Camera Config).
4. Set your environment layers on `Occlusion Layers` so walls hide the camera.

Floor and light:

1. Add a `Plane` named `Ground` on the `Ground` layer.
2. On the `CharacterMovementConfig` assets, set `Ground Layers` to `Ground`
   (defaults to Everything, which works but wastes queries).
3. Add a `Directional Light`.

Training dummy (root `Dummy`) — the Milestone 2 sparring target, still useful
for testing hits. The Rival below is the real behavioral copy:

1. Same body setup as the player: `CapsuleCollider` + `Rigidbody` (no motor).
2. `HealthComponent`, `StaminaComponent`, `CombatController`,
   `DamageReceiver`, `CombatInputBridge` (leave `Input Source` empty — it
   resolves the `TrainingDummyBrain` on the root), `LockOnController`,
   `TrainingDummyBrain`.
3. Child `Hitbox` in front of the dummy with a `MeleeHitbox`; assign a
   `Weapon` and combo chain on `CombatController`.
4. On `LockOnController`, set `Target` = Player. The dummy holds its lock
   (`IsTargetLockHeld => true`), so it constantly faces the player and its
   attacks reach toward you.

Rival (root `Rival`) — the first behavioral copy of the player:

1. Same body setup as the player: `CapsuleCollider` + `Rigidbody`.
2. `CharacterMotor`, `StaminaComponent`, `CombatController` (auto-adds
   `DamageReceiver`, `CombatInputBridge` and `DodgeInvulnerability`),
   `LockOnController`, `RivalBrain`, `RivalController`,
   `CombatTelemetryRecorder`.
3. Assign the same `CharacterMovementConfig` to `RivalController` (it
   shares the player's locomotion feel; `movementSpeedMultiplier` defaults
   to 0.85).
4. Child `Hitbox` in front of the rival with a `MeleeHitbox`; assign a
   `Weapon` and combo chain on `CombatController`.
5. On `RivalBrain`: `Opponent` = Player, `Profiler` = the `BehaviorProfiler`
   in the scene.
6. On `LockOnController`, set `Target` = Player. The brain holds its lock
   permanently, so the rival always faces the player.
7. On `CombatTelemetryRecorder`, set `Actor Name` = Rival and
   `Opponent` = Player.

Rival roster (optional, replaces the manual wiring above):

1. Create a `RivalArchetype` per identity (Assets → right-click → Create →
   Rival/Enemy/Rival Archetype): a unique `Actor Name`, an intrinsic
   `Base Style` (or leave it unset to use the fair default), `Learning Blend`,
   and body tuning (`Movement Speed Multiplier`, reaction, cooldown,
   engagement band).
2. Create a `RivalRoster` (Create → Rival/Enemy/Rival Roster) and add the
   archetypes in menu order.
3. Add `RivalMatchSetup` to the Rival root and assign the `Roster` (its
   `Brain`, `Controller` and `Telemetry` fields auto-resolve on the same
   object). On Awake it reads `IMatchConfig.SelectedIndex` (set by the main
   menu) and falls back to the serialized `startingIndex` if no config is
   available. Call `SetArchetype(int)` at runtime to swap identity. With the
   roster left empty, the Rival behaves exactly like the Milestone 4 single
   rival above.

Character select (optional overlay that picks the roster identity in-scene):

1. Add `CharacterSelectMenu` to any GameObject in the Gameplay scene. It
   resolves `RivalMatchSetup`, `IMenuInput` and `ITimeService` from the
   ServiceLocator (no inspector wiring needed).
2. On Start it builds a screen-space menu listing every archetype, freezes
   game time while open, and applies the highlighted identity via
   `SetArchetype(int)` when the player confirms (Enter / Space / gamepad
   South). Without a roster in the scene it stays dormant and the match
   starts immediately.

Telemetry & behavior analysis & persistence (scene-level):

1. Empty GameObject `BehaviorProfiler` with the `BehaviorProfiler` component.
   It subscribes to every `Telemetry` event and builds a live profile per
   actor.
2. Empty GameObject `TelemetrySink` with `ConsoleTelemetrySink` — prints
   each combat event to the console so the pipeline is visible while it runs.
3. Empty GameObject `ProfileStore` with the `ProfileStore` component —
   registers `IProfileStore`, writes one JSON profile per actor under
   `persistentDataPath/RivalSaves/profiles/`.
4. Empty GameObject `ProfilePersister` with the `ProfilePersister` component
   — every 5s (and on disable/quit) it checkpoints each profile with
   `SampleCount > 0` through the store. Assign `Profiler` = the
   `BehaviorProfiler` above.
5. Empty GameObject `JsonTelemetrySink` with the `JsonTelemetrySink`
   component — writes one versioned JSON log per session under
   `persistentDataPath/RivalSaves/telemetry/`.

Round system (scene-level):

1. Empty GameObject `MatchController` with `MatchController` and
   `RoundManager` components. `MatchController` detects death events,
   `RoundManager` manages best-of-N rounds, resets health/stamina/combat
   between rounds, and tracks score.
2. Empty GameObject `RoundAnnouncementUI` with the `RoundAnnouncementUI`
   component — shows timed "ROUND N" / "FIGHT!" / round-result overlays.
3. Empty GameObject `GameOverUI` with the `GameOverUI` component — displays
   match score and lifetime stats from `StatsTracker`.

Settings (persistent via BootSystems):

1. `GameSettings` and `StatsTracker` are on BootSystems (persistent
   DontDestroyOnLoad). `GameSettings` reads/writes PlayerPrefs-backed
   volume, sensitivity, quality, and fullscreen. `StatsTracker` persists
   lifetime wins, losses, and win streak.
2. `PauseMenuUI` includes a SETTINGS option that opens `SettingsMenuUI`
   (audio volume, camera sensitivity, quality, fullscreen toggle).

With this wiring, the Rival's `useStoredProfile` (on by default) loads the
player's saved profile on start, so a fresh match opens with the rival already
playing like your last session.

## Controls

| Action     | Keyboard    | Gamepad            |
|------------|-------------|--------------------|
| Move       | WASD        | Left stick / D-pad |
| Look       | Mouse       | Right stick        |
| Jump       | Space       | South button       |
| Sprint     | Left Shift  | Left stick press   |
| Dash       | Q           | West button        |
| Dodge/Roll | Left Ctrl   | East button        |
| Light attack | LMB / F   | Right shoulder     |
| Heavy attack | RMB / G   | Right trigger      |
| Block      | R            | Left trigger       |
| Parry      | E            | North button       |
| Target lock | Tab        | Right stick press  |
| Zoom       | Mouse wheel | —                  |
| Pause      | Esc         | Start              |

## Why these choices (Milestone 1)

1. **Interfaces, not types.** `IPlayerInput` keeps the player controller and
   camera completely ignorant of the Input System. Later, a bot brain can
   implement the same contract — or drive the shared `CharacterMotor` directly —
   so the Rival enemy never replays button presses by construction.
2. **ServiceLocator for DI.** Systems register themselves as interfaces;
   consumers ask for the interface. No scattered singletons, no fights with
   Unity's lifecycle.
3. **Motor / Controller split.** `CharacterMotor` is pure physics (velocity,
   gravity, dash bursts) with no game rules. `PlayerController` reads input,
   manages stamina costs and facing, and converts intent into motor calls.
   Any actor — human or AI — can share the motor.
4. **Single owners for shared concerns.** `TimeScaleService` is the only place
   that writes `Time.timeScale` (pause → 0, future slow-mo via `SetScale`).
   `StaminaComponent` owns the stamina resource so movement *and* future
   combat share one instance.
5. **ScriptableObjects for tuning.** Movement and camera numbers live in
   assets, so game feel is a data change, and future enemy archetypes are new
   data assets reusing the same code.
6. **Pure math where possible.** `DirectionMapper` has no engine state, so the
   camera-relative movement math is unit-testable and reusable by AI later.
7. **Events for communication.** Stamina and movement emit events; UI and
   telemetry subscribe instead of polling.
8. **Async scene loading** behind `ISceneLoader` makes loading screens and
   progress reporting a drop-in addition later.

## Why these choices (Milestone 2)

1. **Actor-agnostic combat.** The player and every future rival run the exact
   same `CombatController`, `DamageReceiver`, `MeleeHitbox` and bridge. Adding
   the Rival later means wiring a brain, not forking the combat code.
2. **Damage resolution is inverted.** The attacker only supplies a
   `DamageContext`; the defender's `DamageReceiver` asks its `IDefenseState`
   (the `CombatController`) what it is doing. A new defense mechanic = one new
   case in one place, and dodge/block/parry rules never leak into the attacker.
3. **Input is buffered intent.** `CombatInputBridge` turns presses into
   short-lived buffered intents, giving forgiving combo windows. Brains
   implement `ICombatInput` — intent, never button presses — so the Rival
   learns behavior by construction.
4. **Telemetry-ready from day one.** Every hit produces a `DamageContext` and
   `DamageResult` with an outcome (hit/blocked/block-broken/parried/evaded),
   broadcast through `AttackResolved` and `HitTaken`. That is the exact feed
   the Behavior Analyzer consumes in later milestones.
5. **Explicit timing windows.** Windup / active / recovery are separate
   numbers per `AttackDefinition`, so animation, hit-stop and AI reaction
   times all attach to well-defined sync points instead of magic constants.
6. **State machine over coroutines.** `CombatController` steps its state with
   timers, making each phase inspectable and the whole flow unit-testable.

## Why these choices (Milestone 3)

1. **Behavior, not buttons.** Telemetry records combat events — attacks,
   blocks, parries, dodges, hits taken, distances, outcomes. Nothing records
   which key was pressed, so the Rival is a behavioral copy by construction.
2. **A shared event bus.** Producers, sinks and the analyzer only know the
   `Telemetry.Recorded` event. JSON persistence (the SaveSystem milestone)
   becomes one new subscriber, not a change to any producer.
3. **Pure, unit-testable analysis.** `BehaviorAnalyzer` is plain C# with no
   engine state. Traits are normalized 0..1, so brains and designers reason
   about the same numbers.
4. **One shared motor.** `PlayerMotor` became `CharacterMotor` the moment a
   second consumer appeared — the exact trigger the README predicted. The
   Rival runs the same locomotion, combat and stamina systems as the player.
5. **Live adaptation.** `RivalBrain` continuously copies the player's profile,
   blended toward a fair default, so a mirror match visibly turns into
   "fighting yourself" without a single replayed input.
6. **Reaction as a measured trait.** Reaction speed is inferred from the gap
   between an opponent's attack and the fighter's response — telemetry-derived,
   not scripted.
7. **Brain vs. body.** `RivalBrain` is pure `ICombatInput` (decisions only);
   `RivalController` executes them on the shared `CharacterMotor`. Mirroring
   `PlayerController`'s split keeps both actors on identical systems.

## Why these choices (Milestone 4)

1. **Persistence is just another subscriber.** Telemetry's promise was that a
   JSON sink arrives "without touching any producer" — `JsonTelemetrySink`
   keeps that promise: it subscribes to `Telemetry.Recorded` and nothing in the
   Combat/Telemetry code changed.
2. **DTOs decouple the disk format from runtime types.** `TelemetryRecord` and
   `ProfileRecord` are versioned (`schemaVersion`) and mapped explicitly, so
   the saved format can evolve (new traits, new fields) without changing
   combat, telemetry or the analyzer. JsonUtility serializes plain
   `[Serializable]` DTOs — no third-party package, no extra risk.
3. **Storage behind an interface.** Brains and persisters depend on
   `IProfileStore`, never on file I/O or `ProfileStore`. Swapping to a cloud
   or binary store later is a new implementation, not a code change upstream.
4. **Checkpoint instead of a match-end hook.** There is no GameOver transition
   yet, so `ProfilePersister` snapshots on a cadence and on disable/quit. When
   a match-end state arrives, the persister can subscribe to it without a
   redesign.
5. **Downward-only save layer.** `Rival.SaveSystem` depends on Telemetry,
   BehaviorAnalysis and Utilities — it consumes what analysis produced and
   never reaches back into gameplay. The Rival brain sits *above* persistence
   (Enemy → SaveSystem) and asks for a seed through the interface.

## Why these choices (Milestone 5)

1. **Identity is data, not code.** An archetype is a ScriptableObject: identity,
   intrinsic style, learning and body tuning all live in assets. Adding a rival
   is creating a new asset — no new class, no new wiring.
2. **A memory per identity.** Each archetype's unique `ActorName` keys its own
   stored profile. The player has one profile; every roster rival has its own,
   so rival A remembers fighting aggressively while rival B remembers your
   parries — separate memories under one persistence layer.
3. **The roster is the menu contract.** `RivalMatchSetup.SetArchetype(int)`
   swaps the whole identity (brain tuning, speed, telemetry identity) in one
   call, so a future character-select menu only picks an index.
4. **Backward compatible by construction.** With no roster assigned,
   `RivalMatchSetup` does nothing and the Rival is the Milestone 4 single rival.
   The archetype path is additive: same brain, same controller, same recorder.
5. **Behavior model unchanged.** The rival still fights as
   `Lerp(seed, livePlayerProfile, learningBlend)`; an archetype only changes
   *what the seed is* (its own memory → its intrinsic style → default) and the
   tuning constants. Every rival still mirrors the player live.
6. **Telemetry identity follows identity.** `SetActorName` makes the recorder
   report under the archetype's name, so analysis and persistence automatically
   separate one rival from the next with zero analyzer changes.
7. **A real input mode for menus.** `IMenuInput` is a second contract on the
   same InputReader with its own action map, so a menu suppresses gameplay
   input while open (no accidental attacks during character select) and the
   "only InputReader touches InputSystem" rule still holds.
8. **The menu is disposable UI.** `CharacterSelectMenu` is runtime-built and
   talks to `RivalMatchSetup` through the ServiceLocator — nothing in the
   roster or combat path knows a menu exists. It freezes time via `ITimeService`
   (the single owner of game time) and restores it on confirm.

## Why these choices (Main Menu + Match Flow)

1. **IMatchConfig is a pure interface.** The main menu only knows `int
   SelectedIndex`; it has no reference to `RivalMatchSetup`, `RivalBrain` or
   anything in the Enemy layer. The gameplay scene reads the same interface.
   This keeps the two scenes decoupled through a data contract in Core.
2. **MatchConfig is persistent.** It lives on the BootSystems GameObject
   (`DontDestroyOnLoad`) so the selected index survives every scene transition
   without static state or singletons.
3. **InputReader is a cross-cutting concern.** Like time and scene loading,
   input lives in the Boot scene and persists across all scenes. The
   `sceneLoaded` callback resets to gameplay mode after every transition;
   `MainMenuUI` re-activates menu mode in its `Start()`. This avoids
   duplicating InputReader in every scene or relying on scene-specific wiring.
4. **GameManager state drives the flow.** `MainMenuUI` transitions
   `MainMenu → Loading → Playing` explicitly, so pause, time scale, and future
   loading screens all key off the same state machine. The Bootstrapper only
   loads the first scene; it does not manage state.
5. **Roster duplication is intentional.** `MainMenuUI` holds its own serialized
   `RivalRoster` for display (it cannot reach `RivalMatchSetup` which lives in
   the Gameplay scene). Both reference the same asset — one roster, two
   read-only references. The actual match wiring happens once in Gameplay.
## Known trade-offs (addressed in later milestones)

- No animation yet. Attack timing is pure numbers — `AnimatorAdapter`
  writes parameters; actual Animator controllers are created in the editor.
- The combo chain is a scripted sequence; light vs. heavy currently only
  matters through the attack definitions, not through branch selection.
- The dummy's parry/block timing is scripted randomness. The Rival, by
  contrast, responds through the telemetry-derived profile.
- Rival decisions are trait-weighted randomness with a simple threat-response
  model (block/parry/dodge when threatened), not yet a full utility or FSM
  hierarchy.
- Reaction speed is inferred from a single event gap (opponent attack → own
  response); there is no model of opponent intent (windup reading) yet.
- No character models yet — player and rival are colored capsules.
- No environment art — the arena is a flat gray plane.
- The HUD is minimal runtime-built placeholders; a designed UI comes later.
- Buffered combat input is only consumed from Idle and Recovery — pressing
  parry during an attack is ignored.
- `IPlayerInput` and `IMenuInput` are global services on the persistent
  `InputReader` in the Boot scene — fine while one human drives; bot brains
  will use the motor directly, not this interface.
- `GameManager` is a concrete service (registered in ServiceLocator) and a
  pragmatic Unity singleton; systems resolve it lazily so the class stays
  simple.
- Round announcements are runtime-built Text overlays — no art-directed
  typography or animation yet.
- `GameSettings` uses PlayerPrefs — fine for local; a save-file approach
  would be needed for cloud or multi-device.
- No animation yet. Attack timing is pure numbers — `AnimatorAdapter`
  writes parameters; actual Animator controllers are created in the editor.
- Done (Lead Engineer review, Milestone 3): each layer folder now has an
  `.asmdef` that enforces the assembly boundaries above; dodge i-frames are a
  shared `DodgeInvulnerability` component instead of duplicated per controller;
  `StaminaComponent` owns a `Changed` event; the `MeleeHitbox` victim layer
  default is `~0` — assign a dedicated `Damageable` layer in the scene so
  swings don't test the ground.
- `CombatController` is one cohesive "rules engine" (attacker state machine +
  defender `IDefenseState`). Flagged as the next split candidate once combat
  grows (animation sync, more defense mechanics) — deliberately not split now.
- `ITimeService` is consumed by `HitStop`, `PauseMenuUI`, `CharacterSelectMenu`
  and `MainMenuUI` for time-scale control. Still the intended owner of future
  slow-mo effects.
- Done in Milestone 3: the Rival was the second motor consumer, so the motor is
  now the shared `CharacterMotor` (the rename this README promised).
