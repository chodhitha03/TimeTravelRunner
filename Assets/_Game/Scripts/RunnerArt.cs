using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TimeTravellerRunner
{
    /// <summary>Procedural mesh and material factory utility for runner objects and visual effects.</summary>
    public static class RunnerArt
    {
        private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMaterialCache() { MaterialCache.Clear(); }

        private static Shader CompatibleShader()
        {
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            string[] choices = urp
                ? new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Sprites/Default" }
                : new[] { "Standard", "Unlit/Color", "Sprites/Default" };
            foreach (string name in choices)
            {
                Shader candidate = Shader.Find(name);
                if (candidate != null && candidate.isSupported) return candidate;
            }
            return Shader.Find("Sprites/Default");
        }

        public static Color Hex(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString(hex, out color) ? color : Color.white;
        }

        public static Material Mat(string hex, bool glow = false)
        {
            string key = hex + (glow ? "G" : "N");
            Material result;
            if (MaterialCache.TryGetValue(key, out result))
            {
                if (result != null && result.shader != null && result.shader.isSupported) return result;
                MaterialCache.Remove(key);
            }
            Shader shader = CompatibleShader();
            if (shader == null) throw new InvalidOperationException("Cannot locate a supported rendering shader for generated game art.");
            result = new Material(shader);
            Color color = Hex(hex);
            if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", color);
            if (result.HasProperty("_Color")) result.SetColor("_Color", color);
            if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", glow ? 0.82f : 0.45f);
            if (result.HasProperty("_Metallic")) result.SetFloat("_Metallic", glow ? 0.35f : 0.05f);
            if (glow && result.HasProperty("_EmissionColor"))
            {
                result.EnableKeyword("_EMISSION");
                result.SetColor("_EmissionColor", color * 3.4f);
            }
            MaterialCache.Add(key, result);
            return result;
        }

        private static GameObject Make(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 scale, string color, bool glow)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            Renderer renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = Mat(color, glow);
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);
            return obj;
        }

        public static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 size, string hex, bool glow = false)
            => Make(PrimitiveType.Cube, name, parent, pos, size, hex, glow);

        public static GameObject Ball(string name, Transform parent, Vector3 pos, Vector3 size, string hex, bool glow = false)
            => Make(PrimitiveType.Sphere, name, parent, pos, size, hex, glow);

        public static GameObject Cylinder(string name, Transform parent, Vector3 pos, Vector3 scale, string hex, bool glow = false)
            => Make(PrimitiveType.Cylinder, name, parent, pos, scale, hex, glow);

        public static GameObject Pyramid(string name, Transform parent, Vector3 pos, float size, float height, string hex)
        {
            GameObject obj = new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            Vector3[] vertices = {
                new Vector3(-size, 0, -size), new Vector3(size, 0, -size), new Vector3(0, height, 0),
                new Vector3(size, 0, -size), new Vector3(size, 0, size), new Vector3(0, height, 0),
                new Vector3(size, 0, size), new Vector3(-size, 0, size), new Vector3(0, height, 0),
                new Vector3(-size, 0, size), new Vector3(-size, 0, -size), new Vector3(0, height, 0),
                new Vector3(-size, 0, -size), new Vector3(-size, 0, size), new Vector3(size, 0, size),
                new Vector3(-size, 0, -size), new Vector3(size, 0, size), new Vector3(size, 0, -size)
            };
            Mesh mesh = new Mesh();
            mesh.name = "Generated pyramid";
            mesh.vertices = vertices;
            mesh.triangles = new[] {0,2,1, 3,5,4, 6,8,7, 9,11,10, 12,14,13, 15,17,16};
            mesh.RecalculateNormals();
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = Mat(hex);
            return obj;
        }

        public static GameObject Ring(string name, Transform parent, Vector3 pos, float radius, float tube,
            string hex, bool glow = true)
        {
            const int segments = 44, sides = 8;
            Vector3[] vertices = new Vector3[segments * sides];
            int[] triangles = new int[segments * sides * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                float cx = Mathf.Cos(a), cy = Mathf.Sin(a);
                for (int j = 0; j < sides; j++)
                {
                    float b = j * Mathf.PI * 2f / sides;
                    vertices[i * sides + j] = new Vector3(
                        (radius + Mathf.Cos(b) * tube) * cx,
                        (radius + Mathf.Cos(b) * tube) * cy,
                        Mathf.Sin(b) * tube);
                    int nextI = ((i + 1) % segments) * sides;
                    int nextJ = (j + 1) % sides;
                    int ix = (i * sides + j) * 6;
                    triangles[ix] = i * sides + j;
                    triangles[ix + 1] = nextI + j;
                    triangles[ix + 2] = i * sides + nextJ;
                    triangles[ix + 3] = nextI + j;
                    triangles[ix + 4] = nextI + nextJ;
                    triangles[ix + 5] = i * sides + nextJ;
                }
            }
            Mesh mesh = new Mesh();
            mesh.name = "Generated time portal ring";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            GameObject obj = new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = pos;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = Mat(hex, glow);
            return obj;
        }

        public static void Tree(Transform parent, Vector3 pos, float scale)
        {
            Transform pivot = new GameObject("Ancient jungle tree").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = pos;
            pivot.localScale = Vector3.one * scale;
            Cylinder("Trunk", pivot, new Vector3(0, 1.8f, 0), new Vector3(.42f, 1.8f, .42f), "#675039");
            Ball("Canopy 1", pivot, new Vector3(0, 4.1f, 0), new Vector3(3, 2.4f, 3), "#265e46");
            Ball("Canopy 2", pivot, new Vector3(-1f, 4.7f, 0), new Vector3(2.1f, 1.7f, 2.1f), "#3d8753");
            Ball("Canopy 3", pivot, new Vector3(.9f, 4.9f, .6f), new Vector3(2, 1.5f, 2), "#377248");
        }

        public static void Palm(Transform parent, Vector3 pos, float scale)
        {
            Transform pivot = new GameObject("Egyptian palm").transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = pos;
            pivot.localScale = Vector3.one * scale;
            Cylinder("Palm trunk", pivot, new Vector3(0, 2.4f, 0), new Vector3(.22f, 2.4f, .22f), "#8e5e36");
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2f / 7;
                GameObject leaf = Ball("Palm frond", pivot,
                    new Vector3(Mathf.Cos(angle) * 1.4f, 5f, Mathf.Sin(angle) * 1.4f),
                    new Vector3(.65f, .15f, 3.1f), i % 2 == 0 ? "#579655" : "#36784b");
                leaf.transform.localRotation = Quaternion.Euler(14, -angle * Mathf.Rad2Deg, 0);
            }
        }

        public static void NeonBuilding(Transform parent, Vector3 pos, float height, string accent)
        {
            float width = 3.3f;
            Box("Cyber tower", parent, pos + new Vector3(0, height / 2f, 0),
                new Vector3(width, height, 3.8f), "#1d2749");
            Box("Rooftop", parent, pos + new Vector3(0, height + .15f, 0),
                new Vector3(width + .4f, .3f, 4.1f), accent, true);
            for (int floor = 1; floor < (int)height - 1; floor += 2)
            {
                for (int col = -1; col <= 1; col++)
                    Box("Lit window", parent, pos + new Vector3(col * .95f, floor + .35f, -1.96f),
                        new Vector3(.5f, .7f, .06f), (floor + col) % 3 == 0 ? "#f057cd" : "#43d9ff", true);
            }
        }

        public static GameObject CyberCoin(Transform parent, Vector3 pos)
        {
            GameObject coinRoot = new GameObject("CYBER COIN / 3D Spinning Token");
            if (parent != null) coinRoot.transform.SetParent(parent, false);
            coinRoot.transform.localPosition = pos;

            // Classic Subway Surfers Gold Coin
            GameObject disc = Cylinder("Coin Rim", coinRoot.transform, Vector3.zero,
                new Vector3(0.52f, 0.055f, 0.52f), "#FFB800", false);
            disc.transform.localRotation = Quaternion.Euler(90f, 0, 0);

            // Embossed star / inner core
            Ball("Coin Core Front", coinRoot.transform, new Vector3(0, 0, -0.032f),
                new Vector3(0.34f, 0.34f, 0.05f), "#FFF59D", false);
            Ball("Coin Core Back", coinRoot.transform, new Vector3(0, 0, 0.032f),
                new Vector3(0.34f, 0.34f, 0.05f), "#FFF59D", false);

            coinRoot.AddComponent<CyberCoinAnimation>();
            return coinRoot;
        }
    }

    /// <summary>Subway Surfers-style continuous coin rotation and gentle vertical hover bob.</summary>
    public sealed class CyberCoinAnimation : MonoBehaviour
    {
        private Vector3 startPos;
        private float offset;

        private void Start()
        {
            startPos = transform.localPosition;
            offset = (transform.position.z * 0.35f) % (Mathf.PI * 2f);
        }

        private void Update()
        {
            transform.Rotate(0, 160f * Time.deltaTime, 0, Space.Self);
            float bob = Mathf.Sin(Time.time * 4.5f + offset) * 0.07f;
            transform.localPosition = startPos + new Vector3(0, bob, 0);
        }
    }
}
