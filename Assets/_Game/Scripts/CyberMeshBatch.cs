using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Bakes many architectural shapes into a few meshes per streamed block.
    /// This is deliberately static geometry: no primitive-per-window overhead.
    /// </summary>
    public sealed class CyberMeshBatch
    {
        private sealed class Bucket
        {
            public readonly List<Vector3> positions = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> indices = new List<int>();
        }

        private readonly Dictionary<string, Bucket> buckets = new Dictionary<string, Bucket>();
        private readonly Transform parent;
        private readonly string label;

        public CyberMeshBatch(Transform parent, string label)
        {
            this.parent = parent;
            this.label = label;
        }

        public void Box(string material, Vector3 position, Vector3 size)
        {
            if (size.x <= 0 || size.y <= 0 || size.z <= 0) return;
            Bucket bucket;
            if (!buckets.TryGetValue(material, out bucket))
            {
                bucket = new Bucket();
                buckets.Add(material, bucket);
            }
            Vector3 h = size * .5f;
            // Each face owns its vertices so lighting uses hard architectural edges.
            Face(bucket, position, new Vector3(-h.x, -h.y, -h.z), new Vector3(-h.x, h.y, -h.z),
                new Vector3(h.x, h.y, -h.z), new Vector3(h.x, -h.y, -h.z), Vector3.back, size.x, size.y);
            Face(bucket, position, new Vector3(h.x, -h.y, h.z), new Vector3(h.x, h.y, h.z),
                new Vector3(-h.x, h.y, h.z), new Vector3(-h.x, -h.y, h.z), Vector3.forward, size.x, size.y);
            Face(bucket, position, new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, h.y, h.z),
                new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, -h.y, -h.z), Vector3.left, size.z, size.y);
            Face(bucket, position, new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, h.y, -h.z),
                new Vector3(h.x, h.y, h.z), new Vector3(h.x, -h.y, h.z), Vector3.right, size.z, size.y);
            Face(bucket, position, new Vector3(-h.x, h.y, -h.z), new Vector3(-h.x, h.y, h.z),
                new Vector3(h.x, h.y, h.z), new Vector3(h.x, h.y, -h.z), Vector3.up, size.x, size.z);
            Face(bucket, position, new Vector3(-h.x, -h.y, h.z), new Vector3(-h.x, -h.y, -h.z),
                new Vector3(h.x, -h.y, -h.z), new Vector3(h.x, -h.y, h.z), Vector3.down, size.x, size.z);
        }

        private static void Face(Bucket b, Vector3 p, Vector3 a, Vector3 c, Vector3 d, Vector3 e,
            Vector3 normal, float u, float v)
        {
            int s = b.positions.Count;
            b.positions.Add(p + a); b.positions.Add(p + c);
            b.positions.Add(p + d); b.positions.Add(p + e);
            for (int i = 0; i < 4; i++) b.normals.Add(normal);
            b.uv.Add(Vector2.zero);
            b.uv.Add(new Vector2(0, v));
            b.uv.Add(new Vector2(u, v));
            b.uv.Add(new Vector2(u, 0));
            // The corners are ordered clockwise when viewed from outside.
            b.indices.Add(s); b.indices.Add(s + 1); b.indices.Add(s + 2);
            b.indices.Add(s); b.indices.Add(s + 2); b.indices.Add(s + 3);
        }

        public void Bake()
        {
            foreach (KeyValuePair<string, Bucket> entry in buckets)
            {
                Bucket b = entry.Value;
                if (b.positions.Count == 0) continue;
                GameObject obj = new GameObject(label + " / " + entry.Key);
                obj.transform.SetParent(parent, false);
                Mesh mesh = new Mesh();
                mesh.name = "Static city geometry - " + entry.Key;
                mesh.indexFormat = b.positions.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(b.positions);
                mesh.SetNormals(b.normals);
                mesh.SetUVs(0, b.uv);
                mesh.SetTriangles(b.indices, 0);
                mesh.RecalculateBounds();
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                obj.AddComponent<CyberGeneratedMeshOwner>().OwnedMesh = mesh;
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = CyberSurface.Get(entry.Key);
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            buckets.Clear();
        }
    }

    /// <summary>Textured, lit URP/Standard materials, with runtime Resources loading.</summary>
    public static class CyberSurface
    {
        public static bool NightMode { get; private set; }
        public static void SetNightMode(bool night)
        {
            if (NightMode == night) return;
            NightMode = night;
            foreach (KeyValuePair<string, Material> entry in cache)
                if (entry.Value != null) ApplyTint(entry.Value, entry.Key);
        }
        private static void ApplyTint(Material material, string kind)
        {
            if (material == null) return;
            Color normal = RunnerArt.Hex(colors.ContainsKey(kind) ? colors[kind] : "#aab3b8");
            Color tint = normal;
            bool accent = kind == "light" || kind == "signal" || kind == "mint" ||
                kind == "teal" || kind == "magenta" || kind == "glasswarm";
            if (NightMode && !accent)
                tint = Color.Lerp(normal, RunnerArt.Hex("#142844"), .47f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (accent && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", tint * (NightMode ? 1.8f : .48f));
            }
        }
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSurfaceCache()
        {
            cache.Clear();
            NightMode = false;
        }
        private static readonly Dictionary<string, string> colors = new Dictionary<string, string>()
        {
            {"road", "#1A2232"}, {"rail", "#00E5FF"}, {"roadpaint", "#FFFFFF"},
            {"walk", "#283548"}, {"curb", "#475569"}, {"darkmetal", "#1E293B"},
            {"steel", "#64748B"}, {"glass", "#0284C7"}, {"glassdark", "#0B132B"},
            {"glasswarm", "#F59E0B"}, {"concrete", "#94A3B8"}, {"white", "#FFFFFF"},
            {"navy", "#0F172A"}, {"teal", "#14B8A6"}, {"orange", "#F97316"},
            {"magenta", "#EC4899"}, {"mint", "#10B981"}, {"planter", "#334155"},
            {"leaf", "#22C55E"}, {"leaf2", "#15803D"}, {"light", "#FEF08A"},
            {"signal", "#EF4444"}, {"shadow", "#0F172A"}, {"ad", "#3B82F6"},
            {"arcadeBlue", "#2563EB"}, {"arcadePink", "#DB2777"},
            {"arcadeYellow", "#EAB308"}, {"arcadeCream", "#F3F4F6"},
            {"arcadeGreen", "#16A34A"}, {"neonCyan", "#00E5FF"},
            {"neonPink", "#FF007F"}, {"neonGold", "#FFD600"},
            {"neonPurple", "#9333EA"},
            // Next-Gen Future Cyber City Palette
            {"energyCyan", "#00E5FF"}, {"energyPink", "#FF007F"}, {"energyYellow", "#FFD600"},
            {"futureNavy", "#0F172A"}, {"futureTitanium", "#475569"}, {"futureWhite", "#F8FAFC"},
            {"hoverBlue", "#00B4D8"},
            // Authentic High-Speed Maglev Train & Obstacle Palette
            {"trainRed", "#FF007F"}, {"trainBlue", "#00E5FF"}, {"trainGreen", "#10B981"},
            {"trainYellow", "#FFD600"}, {"trainRoof", "#F8FAFC"}, {"trainGlass", "#0B132B"},
            {"ballast", "#1E2532"}, {"woodTie", "#334155"}, {"steelRail", "#64748B"},
            {"subwayWall", "#1E293B"}, {"brick", "#334155"}, {"graffitiPink", "#FF007F"},
            {"graffitiCyan", "#00E5FF"}, {"graffitiYellow", "#FFD600"}
        };
        public static Material Get(string kind)
        {
            Material material;
            if (cache.TryGetValue(kind, out material))
            {
                if (material != null && material.shader != null && material.shader.isSupported) return material;
                cache.Remove(kind);
            }
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            string[] candidates = urp
                ? new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Sprites/Default" }
                : new[] { "Standard", "Unlit/Color", "Sprites/Default" };
            Shader shader = null;
            foreach (string name in candidates)
            {
                Shader choice = Shader.Find(name);
                if (choice != null && choice.isSupported) { shader = choice; break; }
            }
            if (shader == null) throw new System.InvalidOperationException("No supported Cyber City shader was found.");
            material = new Material(shader);
            material.name = "CyberCity / " + kind;
            Color tint = RunnerArt.Hex(colors.ContainsKey(kind) ? colors[kind] : "#aab3b8");
            Texture2D texture = null;
            if (kind == "road" || kind == "ballast") texture = Resources.Load<Texture2D>("CyberTextures/cyber_asphalt");
            else if (kind == "concrete" || kind == "walk" || kind == "curb" || kind == "subwayWall" || kind == "brick" || kind == "futureWhite")
                texture = Resources.Load<Texture2D>("CyberTextures/cyber_concrete");
            else if (kind == "glass" || kind == "glassdark" || kind == "glasswarm" || kind == "trainGlass")
                texture = Resources.Load<Texture2D>("CyberTextures/cyber_glass");
            else if (kind == "steel" || kind == "darkmetal" || kind == "rail" || kind == "steelRail" || kind == "futureTitanium" || kind == "futureNavy")
                texture = Resources.Load<Texture2D>("CyberTextures/cyber_metal");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (texture != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            }
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", kind.Contains("glass") || kind == "trainGlass" ? .85f :
                    kind == "steelRail" || kind == "futureTitanium" ? .70f : kind == "road" ? .25f : .35f);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", kind == "steelRail" || kind == "steel" || kind == "futureTitanium" ? .75f :
                    kind.StartsWith("train") || kind == "futureNavy" ? .35f : .04f);
            
            // Subtle arcade neon & luminaire emission (balanced, never blinding)
            bool isEmissive = kind == "light" || kind == "signal" || kind == "energyCyan" || kind == "energyPink" || kind == "neonCyan" || kind == "neonPink";
            if (isEmissive)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    float factor = (kind == "light" || kind == "signal") ? 1.35f : 1.15f;
                    material.SetColor("_EmissionColor", tint * factor);
                }
            }
            ApplyTint(material, kind);
            cache.Add(kind, material);
            return material;
        }
    }
}

namespace TimeTravellerRunner
{
    /// <summary>Release generated meshes as streamed road tiles retire.</summary>
    public sealed class CyberGeneratedMeshOwner : MonoBehaviour
    {
        public Mesh OwnedMesh;
        private void OnDestroy()
        {
            if (OwnedMesh != null) Destroy(OwnedMesh);
        }
    }
}
