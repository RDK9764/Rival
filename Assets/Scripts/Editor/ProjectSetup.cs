#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Rival.Actors;
using Rival.Camera;
using Rival.Combat;
using Rival.Core;
using Rival.Enemy;
using Rival.Player;
using Rival.UI;

namespace Rival.Editor
{
    /// <summary>
    /// One-click project setup. Creates all ScriptableObject assets, three scenes
    /// (Boot, MainMenu, Gameplay) with full GameObject wiring, and registers
    /// them in Build Settings. Run via Tools → Rival → Setup Project.
    /// </summary>
    public static class ProjectSetup
    {
        private const string Root = "Assets";
        private const string ArtRoot = "Assets/Art";
        private const string ScenesRoot = "Assets/Scenes";
        private const string ScriptableRoot = "Assets/ScriptableObjects";

        [MenuItem("Tools/Rival/Setup Project")]
        public static void SetupProject()
        {
            if (!EditorUtility.DisplayDialog("Rival Project Setup",
                "This will create all required assets and scenes.\n\nExisting files with the same names will NOT be overwritten.\n\nProceed?",
                "Setup", "Cancel")) return;

            CreateDirectories();
            CreateAssets();
            CreateBootScene();
            CreateMainMenuScene();
            CreateGameplayScene();
            ApplyPackagingDefaults();
            UpdateBuildSettings();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Setup Complete",
                "All assets and scenes created.\n\nOpen the Boot scene to play.", "OK");
        }

        private static void CreateDirectories()
        {
            var dirs = new[] { ArtRoot, ScenesRoot, ScriptableRoot,
                $"{ScriptableRoot}/Weapons", $"{ScriptableRoot}/Attacks",
                $"{ScriptableRoot}/Configs", $"{ScriptableRoot}/Archetypes",
                $"{ScriptableRoot}/Audio" };
            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
        }

        // ─────────────────────── Assets ───────────────────────

        private static void ApplyPackagingDefaults()
        {
            PlayerSettings.productName = "RIVAL";
            PlayerSettings.companyName = "Rival Games";
            PlayerSettings.bundleVersion = "0.9.0";
            var splash = PlayerSettings.SplashScreen;
            splash.show = true;
            splash.backgroundColor = new Color(0.06f, 0.08f, 0.10f);
        }

        private static void CreateAssets()
        {
            CreateMovementConfig();
            CreateCameraConfig();
            WeaponFactory.CreateAll();
            CreateArchetypes();
            CreateRoster();
        }

        private static CharacterMovementConfig CreateMovementConfig()
        {
            var path = $"{ScriptableRoot}/Configs/PlayerMovement.asset";
            if (AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>(path) != null)
                return AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>(path);

            var config = ScriptableObject.CreateInstance<CharacterMovementConfig>();
            config.moveSpeed = 6f;
            config.sprintMultiplier = 1.6f;
            config.turnSpeed = 12f;
            config.jumpHeight = 1.2f;
            config.dashSpeed = 20f;
            config.dashDuration = 0.2f;
            config.dashCooldown = 1f;
            config.dashStaminaCost = 20f;
            config.dodgeSpeed = 12f;
            config.dodgeDuration = 0.35f;
            config.dodgeCooldown = 1.2f;
            config.dodgeStaminaCost = 25f;
            config.sprintStaminaCostPerSecond = 12f;
            config.groundCheckRadius = 0.35f;
            config.groundCheckDistance = 0.35f;
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static CameraConfig CreateCameraConfig()
        {
            var path = $"{ScriptableRoot}/Configs/MainCamera.asset";
            if (AssetDatabase.LoadAssetAtPath<CameraConfig>(path) != null)
                return AssetDatabase.LoadAssetAtPath<CameraConfig>(path);

            var config = ScriptableObject.CreateInstance<CameraConfig>();
            config.sensitivityX = 3f;
            config.sensitivityY = 3f;
            config.startPitch = 12f;
            config.pitchMin = -25f;
            config.pitchMax = 60f;
            config.startDistance = 5f;
            config.minDistance = 2f;
            config.maxDistance = 8f;
            config.zoomStep = 0.5f;
            config.positionDamping = 0.15f;
            config.lookDamping = 0.08f;
            config.collisionRadius = 0.15f;
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        private static void CreateArchetypes()
        {
            RivalArchetypeFactory.CreateAll();
        }

        private static void CreateArchetype(string assetName, string displayName,
            float aggression, float blockFreq, float reaction, float speed,
            float learning, float minDist, float maxDist)
        {
            var path = $"{ScriptableRoot}/Archetypes/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<RivalArchetype>(path) != null) return;

            var archetype = ScriptableObject.CreateInstance<RivalArchetype>();
            archetype.DisplayName = displayName;
            archetype.ActorName = displayName;
            archetype.LearningBlend = learning;
            archetype.MovementSpeedMultiplier = speed;
            archetype.MaxReactionDelay = 0.6f;
            archetype.AttackCooldown = 0.9f;
            archetype.MinEngageDistance = minDist;
            archetype.MaxEngageDistance = maxDist;
            archetype.AccentColor = Color.white;
            AssetDatabase.CreateAsset(archetype, path);
        }

        private static void CreateRoster()
        {
            var path = $"{ScriptableRoot}/DefaultRoster.asset";
            if (AssetDatabase.LoadAssetAtPath<RivalRoster>(path) != null) return;

            var roster = ScriptableObject.CreateInstance<RivalRoster>();
            var archetypeNames = new[] { "Berserker", "Defender", "Vanguard", "Phantom", "Sentinel", "Tempest", "Warlord", "Trickster" };
            foreach (var name in archetypeNames)
            {
                var archetype = AssetDatabase.LoadAssetAtPath<RivalArchetype>($"{ScriptableRoot}/Archetypes/{name}.asset");
                if (archetype != null) roster.Archetypes.Add(archetype);
            }
            AssetDatabase.CreateAsset(roster, path);
        }

        // ─────────────────────── Scenes ───────────────────────

        private static void CreateBootScene()
        {
            var path = $"{ScenesRoot}/Boot.unity";
            if (File.Exists(path)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Remove default Main Camera (PlayerCameraController will be on the gameplay camera)
            var defaultCam = GameObject.FindObjectOfType<UnityEngine.Camera>();
            if (defaultCam != null) Object.DestroyImmediate(defaultCam.gameObject);

            // BootSystems
            var bootSystems = new GameObject("BootSystems");
            bootSystems.AddComponent<GameManager>();
            bootSystems.AddComponent<SceneLoader>();
            bootSystems.AddComponent<Bootstrapper>();
            bootSystems.AddComponent<PauseHandler>();
            bootSystems.AddComponent<TimeScaleService>();
            bootSystems.AddComponent<MatchConfig>();
            bootSystems.AddComponent<AudioService>();
            bootSystems.AddComponent<ProceduralAudioGenerator>();
            bootSystems.AddComponent<MusicService>();
            bootSystems.AddComponent<GameSettings>();
            bootSystems.AddComponent<StatsTracker>();

            // InputReader (persistent, cross-cutting)
            var inputReaderGo = new GameObject("InputReader");
            inputReaderGo.AddComponent<InputReader>();

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateMainMenuScene()
        {
            var path = $"{ScenesRoot}/MainMenu.unity";
            if (File.Exists(path)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var defaultCam = GameObject.FindObjectOfType<UnityEngine.Camera>();
            if (defaultCam != null) Object.DestroyImmediate(defaultCam.gameObject);

            // Camera (background)
            var camGo = new GameObject("MainCamera");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            camGo.transform.position = new Vector3(0, 1, -5);

            // Directional Light
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.5f;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Menu Root
            var menuRoot = new GameObject("MainMenu");
            menuRoot.AddComponent<MainMenuUI>();

            var statsDisplay = new GameObject("StatsDisplay");
            statsDisplay.AddComponent<MainMenuStatsDisplay>();

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateGameplayScene()
        {
            var path = $"{ScenesRoot}/Gameplay.unity";
            if (File.Exists(path)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var defaultCam = GameObject.FindObjectOfType<UnityEngine.Camera>();
            if (defaultCam != null) Object.DestroyImmediate(defaultCam.gameObject);

            var movementConfig = AssetDatabase.LoadAssetAtPath<CharacterMovementConfig>($"{ScriptableRoot}/Configs/PlayerMovement.asset");
            var cameraConfig = AssetDatabase.LoadAssetAtPath<CameraConfig>($"{ScriptableRoot}/Configs/MainCamera.asset");
            var weapon = AssetDatabase.LoadAssetAtPath<Weapon>($"{ScriptableRoot}/Weapons/Sword.asset");
            var lightSlash = AssetDatabase.LoadAssetAtPath<AttackDefinition>($"{ScriptableRoot}/Attacks/Sword_Light.asset");
            var heavySlash = AssetDatabase.LoadAssetAtPath<AttackDefinition>($"{ScriptableRoot}/Attacks/Sword_Heavy.asset");
            var roster = AssetDatabase.LoadAssetAtPath<RivalRoster>($"{ScriptableRoot}/DefaultRoster.asset");

            AttackDefinition[] comboChain = lightSlash != null && heavySlash != null
                ? new[] { lightSlash, heavySlash }
                : null;

            // ── Ground ──
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5, 1, 5);
            var groundRenderer = ground.GetComponent<MeshRenderer>();
            if (groundRenderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.25f, 0.28f, 0.3f);
                groundRenderer.material = mat;
            }

            // ── Directional Light ──
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            // ── Camera ──
            var camGo = new GameObject("MainCamera");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            var camController = camGo.AddComponent<PlayerCameraController>();

            // ── Player ──
            var player = CreateCapsule("Player", new Vector3(0, 1, 0), new Color(0.2f, 0.5f, 0.9f));
            player.tag = "Player";
            var playerMotor = player.AddComponent<CharacterMotor>();
            var playerStamina = player.AddComponent<StaminaComponent>();
            var playerDodge = player.AddComponent<DodgeInvulnerability>();
            var playerController = player.AddComponent<PlayerController>();
            var playerCombat = player.AddComponent<CombatController>();
            var playerDamageReceiver = player.AddComponent<DamageReceiver>();
            var playerInputBridge = player.AddComponent<CombatInputBridge>();
            var playerHitbox = CreateChildHitbox(player.transform, "PlayerHitbox");
            var playerHealth = player.GetComponent<HealthComponent>();
            if (playerHealth == null) playerHealth = player.AddComponent<HealthComponent>();
            var playerLockOn = player.AddComponent<LockOnController>();
            var playerHUD = player.AddComponent<PlayerHUD>();
            var playerTelemetry = player.AddComponent<CombatTelemetryRecorder>();

            // LookTarget
            var lookTarget = new GameObject("LookTarget");
            lookTarget.transform.SetParent(player.transform, false);
            lookTarget.transform.localPosition = new Vector3(0, 1.5f, 0);

            // Camera references
            SetField(camController, "followTarget", player.transform);
            SetField(camController, "lookTarget", lookTarget.transform);
            SetField(camController, "cameraConfig", cameraConfig);
            camGo.AddComponent<CameraZoomEffect>();

            // PlayerController references
            SetField(playerController, "movementConfig", movementConfig);
            SetField(playerController, "combat", playerCombat);
            SetField(playerController, "lockOn", playerLockOn);

            // ── Rival ──
            var rival = CreateCapsule("Rival", new Vector3(3, 1, 0), new Color(0.9f, 0.2f, 0.2f));
            var rivalMotor = rival.AddComponent<CharacterMotor>();
            var rivalStamina = rival.AddComponent<StaminaComponent>();
            var rivalDodge = rival.AddComponent<DodgeInvulnerability>();
            var rivalCombat = rival.AddComponent<CombatController>();
            var rivalDamageReceiver = rival.AddComponent<DamageReceiver>();
            var rivalInputBridge = rival.AddComponent<CombatInputBridge>();
            var rivalHitbox = CreateChildHitbox(rival.transform, "RivalHitbox");
            var rivalHealth = rival.GetComponent<HealthComponent>();
            if (rivalHealth == null) rivalHealth = rival.AddComponent<HealthComponent>();
            var rivalLockOn = rival.AddComponent<LockOnController>();
            var rivalBrain = rival.AddComponent<RivalBrain>();
            var rivalControllerComp = rival.AddComponent<RivalController>();
            var rivalHUD = rival.AddComponent<RivalHUD>();
            var rivalTelemetry = rival.AddComponent<CombatTelemetryRecorder>();
            var rivalMatchSetup = rival.AddComponent<RivalMatchSetup>();

            // Rival look target (faces player)
            var rivalLookTarget = new GameObject("LookTarget");
            rivalLookTarget.transform.SetParent(rival.transform, false);
            rivalLookTarget.transform.localPosition = new Vector3(0, 1.5f, 0);

            // RivalController references
            SetField(rivalControllerComp, "movementConfig", movementConfig);
            SetField(rivalControllerComp, "brain", rivalBrain);
            SetField(rivalControllerComp, "combat", rivalCombat);
            SetField(rivalControllerComp, "lockOn", rivalLockOn);

            // RivalBrain references
            SetField(rivalBrain, "opponent", player.transform);
            SetField(rivalBrain, "combat", rivalCombat);
            SetField(rivalBrain, "motor", rivalMotor);

            // LockOn references
            SetField(playerLockOn, "Target", rival.transform);
            SetField(rivalLockOn, "Target", player.transform);

            // RivalMatchSetup references
            SetField(rivalMatchSetup, "roster", roster);
            SetField(rivalMatchSetup, "brain", rivalBrain);
            SetField(rivalMatchSetup, "controller", rivalControllerComp);
            SetField(rivalMatchSetup, "telemetry", rivalTelemetry);
            SetField(rivalMatchSetup, "playerHealth", playerHealth);
            SetField(rivalMatchSetup, "rivalHealth", rivalHealth);

            // ── Scene-level systems ──
            var matchControllerGo = new GameObject("MatchController");
            var matchController = matchControllerGo.AddComponent<MatchController>();
            matchControllerGo.AddComponent<RoundManager>();

            var characterSelectGo = new GameObject("CharacterSelectMenu");
            characterSelectGo.AddComponent<CharacterSelectMenu>();

            var loadoutGo = new GameObject("PlayerLoadoutManager");
            var loadout = loadoutGo.AddComponent<PlayerLoadoutManager>();
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>($"{ScriptableRoot}/Weapons/WeaponCatalog.asset");
            SetField(loadout, "catalog", catalog);
            SetField(loadout, "playerCombat", playerCombat);
            SetField(loadout, "rivalCombat", rivalCombat);
            SetField(loadout, "rivalBodyColor", new Color(0.25f, 0.08f, 0.08f));
            SetField(loadout, "rivalAccentColor", new Color(0.9f, 0.2f, 0.2f));
            SetField(loadout, "unlockCosts", new[]
            {
                new PlayerLoadoutManager.UnlockCost { weaponId = "Sword", cost = 0 },
                new PlayerLoadoutManager.UnlockCost { weaponId = "Axe", cost = 60 },
                new PlayerLoadoutManager.UnlockCost { weaponId = "Spear", cost = 80 },
                new PlayerLoadoutManager.UnlockCost { weaponId = "Dagger", cost = 100 },
                new PlayerLoadoutManager.UnlockCost { weaponId = "Hammer", cost = 140 },
                new PlayerLoadoutManager.UnlockCost { weaponId = "Fist", cost = 120 }
            });

            var gameOverGo = new GameObject("GameOverUI");
            gameOverGo.AddComponent<GameOverUI>();

            var pauseMenuGo = new GameObject("PauseMenuUI");
            pauseMenuGo.AddComponent<PauseMenuUI>();

            var roundAnnounceGo = new GameObject("RoundAnnouncementUI");
            roundAnnounceGo.AddComponent<RoundAnnouncementUI>();

            var loadingScreenGo = new GameObject("LoadingScreenUI");
            loadingScreenGo.AddComponent<LoadingScreenUI>();

            var hitStopGo = new GameObject("HitStop");
            hitStopGo.AddComponent<HitStop>();

            var gameFeelGo = new GameObject("GameFeelService");
            gameFeelGo.AddComponent<GameFeelService>();

            // ── Combat Feel Wiring ──
            var combatFeelGo = new GameObject("CombatFeelConnector");
            var combatFeel = combatFeelGo.AddComponent<CombatFeelConnector>();
            SetField(combatFeel, "playerHealth", playerHealth);
            SetField(combatFeel, "rivalHealth", rivalHealth);
            SetField(combatFeel, "playerCombat", playerCombat);
            SetField(combatFeel, "rivalCombat", rivalCombat);
            SetField(combatFeel, "playerDamageReceiver", playerDamageReceiver);
            SetField(combatFeel, "rivalDamageReceiver", rivalDamageReceiver);
            SetField(combatFeel, "hitStop", hitStopGo.GetComponent<HitStop>());
            SetField(combatFeel, "gameFeel", gameFeelGo.GetComponent<GameFeelService>());

            // ── Per-actor feel components ──
            var playerAnimatorAdapter = player.AddComponent<AnimatorAdapter>();
            SetField(playerAnimatorAdapter, "combat", playerCombat);
            SetField(playerAnimatorAdapter, "health", playerHealth);
            SetField(playerAnimatorAdapter, "motor", playerMotor);

            var rivalAnimatorAdapter = rival.AddComponent<AnimatorAdapter>();
            SetField(rivalAnimatorAdapter, "combat", rivalCombat);
            SetField(rivalAnimatorAdapter, "health", rivalHealth);
            SetField(rivalAnimatorAdapter, "motor", rivalMotor);

            player.AddComponent<HitFlash>();
            rival.AddComponent<HitFlash>();

            player.AddComponent<DamageNumbers>();
            rival.AddComponent<DamageNumbers>();

            player.AddComponent<ComboCounter>();

            var playerFighterBuilder = player.AddComponent<ProceduralFighterBuilder>();
            SetField(playerFighterBuilder, "weapon", weapon);
            SetField(playerFighterBuilder, "bodyColor", new Color(0.2f, 0.5f, 0.9f));
            SetField(playerFighterBuilder, "accentColor", new Color(0.3f, 0.7f, 1f));
            SetField(loadout, "playerBuilder", playerFighterBuilder);

            player.AddComponent<ParryIndicator>();
            rival.AddComponent<ParryIndicator>();

            var rivalFighterBuilder = rival.AddComponent<ProceduralFighterBuilder>();
            SetField(loadout, "rivalBuilder", rivalFighterBuilder);

            // Proc-animate the primitive bodies (no animation assets needed).
            player.AddComponent<ProceduralBodyAnimator>();
            rival.AddComponent<ProceduralBodyAnimator>();

            // ── Telemetry & Profiling ──
            var profilerGo = new GameObject("BehaviorProfiler");
            profilerGo.AddComponent<BehaviorProfiler>();

            var consoleSinkGo = new GameObject("ConsoleTelemetrySink");
            consoleSinkGo.AddComponent<ConsoleTelemetrySink>();

            var profileStoreGo = new GameObject("ProfileStore");
            profileStoreGo.AddComponent<ProfileStore>();

            var loadoutStoreGo = new GameObject("LoadoutStore");
            loadoutStoreGo.AddComponent<LoadoutStore>();

            var persisterGo = new GameObject("ProfilePersister");
            var persister = persisterGo.AddComponent<ProfilePersister>();
            SetField(persister, "profiler", profilerGo.GetComponent<BehaviorProfiler>());

            var jsonSinkGo = new GameObject("JsonTelemetrySink");
            jsonSinkGo.AddComponent<JsonTelemetrySink>();

            // ── VFX ──
            var vfxServiceGo = new GameObject("VfxService");
            vfxServiceGo.AddComponent<VfxService>();

            var hitVfxGo = new GameObject("HitVfxSpawner");
            var hitVfx = hitVfxGo.AddComponent<HitVfxSpawner>();
            SetField(hitVfx, "playerHitbox", playerHitbox.GetComponent<MeleeHitbox>());
            SetField(hitVfx, "rivalHitbox", rivalHitbox.GetComponent<MeleeHitbox>());
            SetField(hitVfx, "playerCombat", playerCombat);
            SetField(hitVfx, "rivalCombat", rivalCombat);

            // ── Arena ──
            var arenaGo = new GameObject("ArenaEnvironment");
            arenaGo.AddComponent<ArenaEnvironment>();

            // ── Endless Mode ──
            var endlessGo = new GameObject("EndlessModeManager");
            var endless = endlessGo.AddComponent<EndlessModeManager>();
            SetField(endless, "roster", roster);
            SetField(endless, "matchSetup", rivalMatchSetup);

            var waveAnnounceGo = new GameObject("WaveAnnouncementUI");
            waveAnnounceGo.AddComponent<WaveAnnouncementUI>();

            var tutorialGo = new GameObject("TutorialManager");
            tutorialGo.AddComponent<TutorialManager>();

            // ── Animator Controllers ──
            AnimatorControllerFactory.CreateAll();

            EditorSceneManager.SaveScene(scene, path);
        }

        // ─────────────────────── Helpers ───────────────────────

        private static GameObject CreateCapsule(string name, Vector3 position, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = position;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
            return go;
        }

        private static GameObject CreateChildHitbox(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0, 1f, 1.2f);
            go.AddComponent<MeleeHitbox>();
            var trailRenderer = go.AddComponent<TrailRenderer>();
            trailRenderer.startWidth = 0.08f;
            trailRenderer.endWidth = 0f;
            trailRenderer.time = 0.15f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.startColor = new Color(1f, 1f, 1f, 0.6f);
            trailRenderer.endColor = new Color(1f, 1f, 1f, 0f);
            go.AddComponent<WeaponTrail>();
            return go;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            if (value == null) return;
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (field != null) field.SetValue(target, value);
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new[] {
                $"{ScenesRoot}/Boot.unity",
                $"{ScenesRoot}/MainMenu.unity",
                $"{ScenesRoot}/Gameplay.unity"
            };

            var scenesEnabled = new EditorBuildSettingsScene[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                scenesEnabled[i] = new EditorBuildSettingsScene(scenes[i], true);
            }

            EditorBuildSettings.scenes = scenesEnabled;
        }
    }
}
#endif
