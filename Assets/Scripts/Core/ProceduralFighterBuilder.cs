using UnityEngine;
using Rival.Combat;

namespace Rival.Core
{
    /// <summary>
    /// Builds a procedural fighter model from primitive meshes. Each weapon
    /// type produces a distinct body shape: hulking hammer user, lithe dagger
    /// fighter, long spear-wielder, etc. The default capsule is replaced with
    /// assembled body parts (torso, limbs, head, weapon mesh).
    /// </summary>
    public class ProceduralFighterBuilder : MonoBehaviour
    {
        [SerializeField] private Weapon weapon;
        [SerializeField] private Color bodyColor = Color.white;
        [SerializeField] private Color accentColor = Color.red;

        private Transform modelRoot;
        private readonly System.Collections.Generic.List<Material> accentMaterials = new System.Collections.Generic.List<Material>();

        /// <summary>Root transform holding every model part. Visual only; safe to animate.</summary>
        public Transform ModelRoot => modelRoot;

        /// <summary>Recolors every part created with the accent color (belts, blades, capes...).</summary>
        public void SetAccentColor(Color color)
        {
            if (accentColor == color) return;
            accentColor = color;
            foreach (var mat in accentMaterials)
            {
                if (mat != null && mat.HasProperty("_Color")) mat.color = color;
            }
        }

        public void Build(Weapon weaponRef, Color body, Color accent)
        {
            weapon = weaponRef;
            bodyColor = body;
            accentColor = accent;
            Rebuild();
        }

        private void Awake() => Rebuild();

        private void Rebuild()
        {
            if (modelRoot != null) DestroyImmediate(modelRoot.gameObject);
            accentMaterials.Clear();
            modelRoot = new GameObject("FighterModel").transform;
            modelRoot.SetParent(transform, false);
            modelRoot.localPosition = Vector3.zero;

            var type = weapon != null ? weapon.WeaponType : WeaponType.Sword;
            switch (type)
            {
                case WeaponType.Sword: BuildSwordsman(); break;
                case WeaponType.Axe: BuildBerserker(); break;
                case WeaponType.Spear: BuildSpearman(); break;
                case WeaponType.Dagger: BuildAssassin(); break;
                case WeaponType.Hammer: BuildHeavy(); break;
                case WeaponType.Fist: BuildBrawler(); break;
                default: BuildSwordsman(); break;
            }

            // Hide the actor's original capsule mesh so only the assembled
            // fighter model is visible (the capsule collider stays for physics).
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.transform.IsChildOf(modelRoot)) renderer.enabled = false;
            }
        }

        private void BuildSwordsman()
        {
            CreatePart("Torso", new Vector3(0, 1.0f, 0), new Vector3(0.45f, 0.55f, 0.3f), bodyColor);
            CreatePart("Head", new Vector3(0, 1.7f, 0), new Vector3(0.25f, 0.25f, 0.25f), bodyColor);
            CreatePart("Arm_L", new Vector3(-0.35f, 1.0f, 0), new Vector3(0.12f, 0.4f, 0.12f), bodyColor);
            CreatePart("Arm_R", new Vector3(0.35f, 1.0f, 0.15f), new Vector3(0.12f, 0.4f, 0.12f), bodyColor);
            CreatePart("Leg_L", new Vector3(-0.15f, 0.3f, 0), new Vector3(0.14f, 0.5f, 0.14f), bodyColor);
            CreatePart("Leg_R", new Vector3(0.15f, 0.3f, 0), new Vector3(0.14f, 0.5f, 0.14f), bodyColor);
            CreatePart("Sword", new Vector3(0.45f, 0.8f, 0.2f), new Vector3(0.04f, 0.5f, 0.04f), accentColor);
            CreatePart("SwordGuard", new Vector3(0.45f, 0.6f, 0.2f), new Vector3(0.12f, 0.04f, 0.06f), accentColor);
            CreatePart("Cape", new Vector3(0, 1.1f, -0.2f), new Vector3(0.4f, 0.5f, 0.05f), accentColor);
        }

        private void BuildBerserker()
        {
            CreatePart("Torso", new Vector3(0, 1.0f, 0), new Vector3(0.55f, 0.6f, 0.35f), bodyColor);
            CreatePart("Head", new Vector3(0, 1.75f, 0), new Vector3(0.28f, 0.28f, 0.28f), bodyColor);
            CreatePart("Arm_L", new Vector3(-0.4f, 1.0f, 0), new Vector3(0.15f, 0.45f, 0.15f), bodyColor);
            CreatePart("Arm_R", new Vector3(0.4f, 1.0f, 0.15f), new Vector3(0.15f, 0.45f, 0.15f), bodyColor);
            CreatePart("Leg_L", new Vector3(-0.18f, 0.28f, 0), new Vector3(0.16f, 0.5f, 0.16f), bodyColor);
            CreatePart("Leg_R", new Vector3(0.18f, 0.28f, 0), new Vector3(0.16f, 0.5f, 0.16f), bodyColor);
            CreatePart("AxeBlade", new Vector3(0.5f, 0.75f, 0.2f), new Vector3(0.2f, 0.25f, 0.06f), accentColor);
            CreatePart("AxeHandle", new Vector3(0.5f, 0.5f, 0.2f), new Vector3(0.04f, 0.5f, 0.04f), new Color(0.4f, 0.25f, 0.1f));
            CreatePart("ShoulderPad", new Vector3(-0.35f, 1.3f, 0), new Vector3(0.18f, 0.1f, 0.18f), accentColor);
        }

        private void BuildSpearman()
        {
            CreatePart("Torso", new Vector3(0, 1.0f, 0), new Vector3(0.4f, 0.5f, 0.28f), bodyColor);
            CreatePart("Head", new Vector3(0, 1.65f, 0), new Vector3(0.24f, 0.24f, 0.24f), bodyColor);
            CreatePart("Arm_L", new Vector3(-0.3f, 1.0f, 0), new Vector3(0.1f, 0.38f, 0.1f), bodyColor);
            CreatePart("Arm_R", new Vector3(0.3f, 1.0f, 0.15f), new Vector3(0.1f, 0.38f, 0.1f), bodyColor);
            CreatePart("Leg_L", new Vector3(-0.12f, 0.3f, 0), new Vector3(0.12f, 0.5f, 0.12f), bodyColor);
            CreatePart("Leg_R", new Vector3(0.12f, 0.3f, 0), new Vector3(0.12f, 0.5f, 0.12f), bodyColor);
            CreatePart("SpearShaft", new Vector3(0.4f, 1.2f, 0.25f), new Vector3(0.03f, 1.2f, 0.03f), new Color(0.5f, 0.35f, 0.15f));
            CreatePart("SpearTip", new Vector3(0.4f, 1.85f, 0.25f), new Vector3(0.06f, 0.12f, 0.06f), accentColor);
            CreatePart("Cloak", new Vector3(0, 1.05f, -0.18f), new Vector3(0.35f, 0.45f, 0.04f), accentColor);
        }

        private void BuildAssassin()
        {
            CreatePart("Torso", new Vector3(0, 0.95f, 0), new Vector3(0.35f, 0.45f, 0.25f), bodyColor);
            CreatePart("Head", new Vector3(0, 1.55f, 0), new Vector3(0.22f, 0.22f, 0.22f), bodyColor);
            CreatePart("Hood", new Vector3(0, 1.65f, -0.05f), new Vector3(0.28f, 0.2f, 0.28f), accentColor);
            CreatePart("Arm_L", new Vector3(-0.25f, 0.95f, 0), new Vector3(0.09f, 0.35f, 0.09f), bodyColor);
            CreatePart("Arm_R", new Vector3(0.25f, 0.95f, 0.12f), new Vector3(0.09f, 0.35f, 0.09f), bodyColor);
            CreatePart("Leg_L", new Vector3(-0.1f, 0.28f, 0), new Vector3(0.1f, 0.48f, 0.1f), bodyColor);
            CreatePart("Leg_R", new Vector3(0.1f, 0.28f, 0), new Vector3(0.1f, 0.48f, 0.1f), bodyColor);
            CreatePart("Dagger_R", new Vector3(0.3f, 0.75f, 0.15f), new Vector3(0.03f, 0.25f, 0.03f), accentColor);
            CreatePart("Dagger_L", new Vector3(-0.3f, 0.75f, 0.1f), new Vector3(0.03f, 0.22f, 0.03f), accentColor);
            CreatePart("Belt", new Vector3(0, 0.75f, 0), new Vector3(0.38f, 0.06f, 0.28f), accentColor);
        }

        private void BuildHeavy()
        {
            CreatePart("Torso", new Vector3(0, 1.05f, 0), new Vector3(0.6f, 0.65f, 0.4f), bodyColor);
            CreatePart("Head", new Vector3(0, 1.8f, 0), new Vector3(0.3f, 0.3f, 0.3f), bodyColor);
            CreatePart("Arm_L", new Vector3(-0.45f, 1.05f, 0), new Vector3(0.17f, 0.5f, 0.17f), bodyColor);
            CreatePart("Arm_R", new Vector3(0.45f, 1.05f, 0.15f), new Vector3(0.17f, 0.5f, 0.17f), bodyColor);
            CreatePart("Leg_L", new Vector3(-0.2f, 0.3f, 0), new Vector3(0.18f, 0.55f, 0.18f), bodyColor);
            CreatePart("Leg_R", new Vector3(0.2f, 0.3f, 0), new Vector3(0.18f, 0.55f, 0.18f), bodyColor);
            CreatePart("HammerHead", new Vector3(0.55f, 0.85f, 0.2f), new Vector3(0.25f, 0.15f, 0.15f), accentColor);
            CreatePart("HammerHandle", new Vector3(0.55f, 0.5f, 0.2f), new Vector3(0.05f, 0.6f, 0.05f), new Color(0.35f, 0.2f, 0.1f));
            CreatePart("ShoulderPad_L", new Vector3(-0.4f, 1.35f, 0), new Vector3(0.2f, 0.12f, 0.2f), accentColor);
            CreatePart("ShoulderPad_R", new Vector3(0.4f, 1.35f, 0), new Vector3(0.2f, 0.12f, 0.2f), accentColor);
            CreatePart("Belt", new Vector3(0, 0.75f, 0), new Vector3(0.62f, 0.08f, 0.42f), accentColor);
        }

        private void BuildBrawler()
        {
            CreatePart("Torso", new Vector3(0, 1.0f, 0), new Vector3(0.42f, 0.5f, 0.3f), bodyColor);
            CreatePart("Head", new Vector3(0, 1.65f, 0), new Vector3(0.24f, 0.24f, 0.24f), bodyColor);
            CreatePart("Arm_L", new Vector3(-0.32f, 1.0f, 0), new Vector3(0.13f, 0.42f, 0.13f), bodyColor);
            CreatePart("Arm_R", new Vector3(0.32f, 1.0f, 0.12f), new Vector3(0.13f, 0.42f, 0.13f), bodyColor);
            CreatePart("Fist_L", new Vector3(-0.35f, 0.72f, 0.05f), new Vector3(0.1f, 0.1f, 0.1f), accentColor);
            CreatePart("Fist_R", new Vector3(0.35f, 0.72f, 0.17f), new Vector3(0.1f, 0.1f, 0.1f), accentColor);
            CreatePart("Leg_L", new Vector3(-0.14f, 0.3f, 0), new Vector3(0.13f, 0.5f, 0.13f), bodyColor);
            CreatePart("Leg_R", new Vector3(0.14f, 0.3f, 0), new Vector3(0.13f, 0.5f, 0.13f), bodyColor);
            CreatePart("Belt", new Vector3(0, 0.78f, 0), new Vector3(0.44f, 0.06f, 0.32f), accentColor);
            CreatePart("Bandage_L", new Vector3(-0.32f, 0.8f, 0), new Vector3(0.14f, 0.08f, 0.14f), accentColor);
        }

        private void CreatePart(string name, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(modelRoot, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.SetFloat("_Glossiness", 0.3f);
                renderer.material = mat;
                if (color == accentColor) accentMaterials.Add(mat);
            }

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }
    }
}
