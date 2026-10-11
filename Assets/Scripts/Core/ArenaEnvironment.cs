using UnityEngine;
using Rival.Utilities;

namespace Rival.Core
{
    /// <summary>
    /// Runtime-built arena. Creates a floor plane with material, perimeter
    /// walls, ambient lighting, fog, and a simple skybox. Runs once on
    /// Awake. Attach to any persistent or scene-level GameObject.
    /// </summary>
    public class ArenaEnvironment : MonoBehaviour
    {
        [SerializeField] private Color floorColor = new(0.18f, 0.20f, 0.22f);
        [SerializeField] private Color wallColor = new(0.12f, 0.13f, 0.15f);
        [SerializeField] private Color ambientColor = new(0.3f, 0.35f, 0.45f);
        [SerializeField] private Color fogColor = new(0.05f, 0.06f, 0.08f);
        [SerializeField, Range(5f, 50f)] private float arenaSize = 20f;
        [SerializeField, Range(1f, 8f)] private float wallHeight = 4f;
        [SerializeField, Range(0.05f, 1f)] private float fogDensity = 0.04f;

        private void Awake()
        {
            CreateFloor();
            CreateWalls();
            ConfigureLighting();
        }

        private void CreateFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(arenaSize / 5f, 1f, arenaSize / 5f);

            var renderer = floor.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = floorColor;
                mat.SetFloat("_Glossiness", 0.3f);
                mat.SetFloat("_Metallic", 0.1f);
                renderer.material = mat;
            }

            var col = floor.GetComponent<Collider>();
            if (col != null) col.gameObject.layer = LayerMask.NameToLayer("Ground");
        }

        private void CreateWalls()
        {
            var half = arenaSize / 2f;
            var wallThickness = 0.3f;

            CreateWall("WallNorth", new Vector3(0, wallHeight / 2f, half), new Vector3(arenaSize, wallHeight, wallThickness));
            CreateWall("WallSouth", new Vector3(0, wallHeight / 2f, -half), new Vector3(arenaSize, wallHeight, wallThickness));
            CreateWall("WallEast", new Vector3(half, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, arenaSize));
            CreateWall("WallWest", new Vector3(-half, wallHeight / 2f, 0), new Vector3(wallThickness, wallHeight, arenaSize));
        }

        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(transform, false);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;

            var renderer = wall.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = wallColor;
                mat.SetFloat("_Glossiness", 0.1f);
                renderer.material = mat;
            }

            var col = wall.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        private void ConfigureLighting()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambientColor;
        }
    }
}
