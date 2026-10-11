#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Rival.Editor
{
    /// <summary>
    /// Creates Player and Rival AnimatorControllers at runtime via the Editor
    /// API. Run once via Tools → Rival → Create Animator Controllers, or let
    /// ProjectSetup call it automatically. Each controller has a Locomotion
    /// Blend Tree (Speed param), attack layer, block, stun, dodge, and death
    /// states matching AnimatorAdapter's parameter names.
    /// </summary>
    public static class AnimatorControllerFactory
    {
        private const string PlayerPath = "Assets/ScriptableObjects/Configs/PlayerAnimator.controller";
        private const string RivalPath = "Assets/ScriptableObjects/Configs/RivalAnimator.controller";

        [MenuItem("Tools/Rival/Create Animator Controllers")]
        public static void CreateAll()
        {
            CreateController(PlayerPath, "Player");
            CreateController(RivalPath, "Rival");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Rival] Animator controllers created.");
        }

        private static void CreateController(string path, string name)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) return;

            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var rootStateMachine = controller.layers[0].stateMachine;

            // ── Parameters ──
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsAttacking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsBlocking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsStunned", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsDodging", AnimatorControllerParameterType.Bool);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Died", AnimatorControllerParameterType.Trigger);

            // ── Layer 0: Base (Locomotion + reactive states) ──
            BuildBaseLayer(rootStateMachine);

            // ── Layer 1: Attacks ──
            var attackLayer = new AnimatorControllerLayer
            {
                name = "Attacks",
                defaultWeight = 1f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine()
            };
            attackLayer.stateMachine.name = "AttackLayer";
            attackLayer.stateMachine.nameHash = Animator.StringToHash("AttackLayer");
            BuildAttackLayer(attackLayer.stateMachine);
            controller.AddLayer(attackLayer);

            // ── Layer 2: Upper Body Additive (block/parry overlay) ──
            var defenseLayer = new AnimatorControllerLayer
            {
                name = "Defense",
                defaultWeight = 0f,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine()
            };
            defenseLayer.stateMachine.name = "DefenseLayer";
            defenseLayer.stateMachine.nameHash = Animator.StringToHash("DefenseLayer");
            BuildDefenseLayer(defenseLayer.stateMachine);
            controller.AddLayer(defenseLayer);

            // Save sub-assets
            AssetDatabase.AddObjectToAsset(rootStateMachine, path);
            AssetDatabase.AddObjectToAsset(attackLayer.stateMachine, path);
            AssetDatabase.AddObjectToAsset(defenseLayer.stateMachine, path);
        }

        private static void BuildBaseLayer(AnimatorStateMachine sm)
        {
            // Idle state (default)
            var idle = sm.AddState("Idle", new Vector3(0, 0, 0));
            idle.motion = CreateClip("Idle");
            sm.defaultState = idle;

            // Locomotion Blend Tree
            var locomotion = sm.AddState("Locomotion", new Vector3(300, 0, 0));
            var blendTree = ScriptableObject.CreateInstance<BlendTree>();
            blendTree.name = "LocomotionBlend";
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "Speed";
            blendTree.useAutomaticThresholds = false;
            blendTree.thresholds = new[] { 0f, 0.5f, 6f };
            blendTree.motions = new Motion[]
            {
                CreateClip("Idle"),
                CreateClip("Walk"),
                CreateClip("Run")
            };
            locomotion.motion = blendTree;
            AssetDatabase.AddObjectToAsset(blendTree, AssetDatabase.GetAssetPath(sm));

            // Stunned state
            var stunned = sm.AddState("Stunned", new Vector3(300, 150, 0));
            stunned.motion = CreateClip("HitReaction");
            var isStunned = sm.AddTransition(idle, stunned);
            isStunned.AddCondition(AnimatorConditionMode.If, 0, "IsStunned");
            var exitStunned = sm.AddTransition(stunned, idle);
            exitStunned.AddCondition(AnimatorConditionMode.IfNot, 0, "IsStunned");
            exitStunned.hasExitTime = true;
            exitStunned.exitTime = 0.8f;

            // Dodge state
            var dodge = sm.AddState("Dodge", new Vector3(300, 300, 0));
            dodge.motion = CreateClip("DodgeRoll");
            var isDodging = sm.AddTransition(idle, dodge);
            isDodging.AddCondition(AnimatorConditionMode.If, 0, "IsDodging");
            var exitDodge = sm.AddTransition(dodge, idle);
            exitDodge.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDodging");
            exitDodge.hasExitTime = true;
            exitDodge.exitTime = 0.9f;

            // Death state
            var death = sm.AddState("Death", new Vector3(600, 75, 0));
            death.motion = CreateClip("Death");
            var toDeath = sm.AddAnyStateTransition(death);
            toDeath.AddCondition(AnimatorConditionMode.If, 0, "Died");

            // Idle → Locomotion when moving
            var toLoco = sm.AddTransition(idle, locomotion);
            toLoco.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");

            // Locomotion → Idle when stopped
            var toIdle = sm.AddTransition(locomotion, idle);
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");

            // Locomotion → Stunned
            var locoToStun = sm.AddTransition(locomotion, stunned);
            locoToStun.AddCondition(AnimatorConditionMode.If, 0, "IsStunned");

            // Locomotion → Dodge
            var locoToDodge = sm.AddTransition(locomotion, dodge);
            locoToDodge.AddCondition(AnimatorConditionMode.If, 0, "IsDodging");
        }

        private static void BuildAttackLayer(AnimatorStateMachine sm)
        {
            var nothing = sm.AddState("Nothing", Vector3.zero);
            sm.defaultState = nothing;

            for (int i = 0; i < 3; i++)
            {
                var attack = sm.AddState($"Attack_{i}", new Vector3(300, i * 120, 0));
                attack.motion = CreateClip($"Attack_{i}");

                var fromNothing = sm.AddTransition(nothing, attack);
                fromNothing.AddCondition(AnimatorConditionMode.If, 0, "IsAttacking");
                fromNothing.AddCondition(AnimatorConditionMode.Equals, i, "AttackIndex");

                var toNothing = sm.AddTransition(attack, nothing);
                toNothing.hasExitTime = true;
                toNothing.exitTime = 0.95f;
                toNothing.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
            }
        }

        private static void BuildDefenseLayer(AnimatorStateMachine sm)
        {
            var nothing = sm.AddState("Nothing", Vector3.zero);
            sm.defaultState = nothing;

            var block = sm.AddState("Block", new Vector3(300, 0, 0));
            block.motion = CreateClip("Block");

            var toBlock = sm.AddTransition(nothing, block);
            toBlock.AddCondition(AnimatorConditionMode.If, 0, "IsBlocking");

            var fromBlock = sm.AddTransition(block, nothing);
            fromBlock.AddCondition(AnimatorConditionMode.IfNot, 0, "IsBlocking");
        }

        private static AnimationClip CreateClip(string name)
        {
            var clip = new AnimationClip
            {
                name = name,
                frameRate = 30
            };
            return clip;
        }
    }
}
#endif
