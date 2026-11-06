#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TimeTravellerRunner.Editor
{
    /// <summary>
    /// Editor utility for Future City prefabs and materials.
    /// Organizes standard PBR materials in Assets/_Game/Materials/ and modular prefabs in Assets/_Game/Prefabs/CyberCity/.
    /// </summary>
    public static class CityPrefabGenerator
    {
        private const string MaterialsDir = "Assets/_Game/Materials";
        private const string PrefabsDir = "Assets/_Game/Prefabs/CyberCity";

        [MenuItem("Tools/Generate City Prefabs & Materials")]
        public static void GenerateAll()
        {
            EnsureDirectories();
            var mats = GenerateMaterials();
            GenerateObstaclePrefabs(mats);
            GenerateEnvironmentPrefabs(mats);
            GenerateTrackAndCoinPrefabs(mats);
            GeneratePlayerPrefab(mats);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CityPrefabGenerator] Successfully generated all Future City Prefabs & Materials in Assets/_Game/!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(MaterialsDir)) Directory.CreateDirectory(MaterialsDir);
            if (!Directory.Exists(PrefabsDir)) Directory.CreateDirectory(PrefabsDir);
        }

        public struct Materials
        {
            public Material Titanium;
            public Material DarkMetal;
            public Material NeonCyan;
            public Material NeonPink;
            public Material NeonYellow;
            public Material Road;
            public Material GlassHolo;
            public Material GlassDark;
            public Material GoldCoin;
            public Material HoverBlue;
        }

        private static Material GetOrCreateMaterial(string name, Color albedo, Color emission, float smoothness = 0.5f, float metallic = 0.0f)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = albedo;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (emission != Color.black)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
            return mat;
        }

        private static Materials GenerateMaterials()
        {
            var m = new Materials();
            m.Titanium = GetOrCreateMaterial("Mat_CyberTitanium", new Color(0.65f, 0.72f, 0.80f), Color.black, 0.8f, 0.6f);
            m.DarkMetal = GetOrCreateMaterial("Mat_DarkMetal", new Color(0.10f, 0.13f, 0.17f), Color.black, 0.5f, 0.8f);
            m.NeonCyan = GetOrCreateMaterial("Mat_NeonCyan", new Color(0.1f, 0.85f, 1f), new Color(0.0f, 0.9f, 1f) * 2.5f, 0.9f, 0.1f);
            m.NeonPink = GetOrCreateMaterial("Mat_NeonPink", new Color(1f, 0.18f, 0.55f), new Color(1f, 0.18f, 0.55f) * 2.5f, 0.9f, 0.1f);
            m.NeonYellow = GetOrCreateMaterial("Mat_NeonYellow", new Color(1f, 0.85f, 0.1f), new Color(1f, 0.85f, 0.1f) * 2.2f, 0.8f, 0.1f);
            m.Road = GetOrCreateMaterial("Mat_RoadComposite", new Color(0.09f, 0.12f, 0.16f), Color.black, 0.35f, 0.2f);
            m.GlassHolo = GetOrCreateMaterial("Mat_GlassHolo", new Color(0.2f, 0.8f, 1f, 0.6f), new Color(0.0f, 0.7f, 1f) * 1.5f, 0.95f, 0.1f);
            m.GlassDark = GetOrCreateMaterial("Mat_GlassDark", new Color(0.05f, 0.08f, 0.12f), Color.black, 0.9f, 0.3f);
            m.GoldCoin = GetOrCreateMaterial("Mat_GoldCoin", new Color(1f, 0.78f, 0.12f), new Color(1f, 0.75f, 0.15f) * 1.8f, 0.85f, 0.7f);
            m.HoverBlue = GetOrCreateMaterial("Mat_HoverBlue", new Color(0.2f, 0.5f, 1f), new Color(0.2f, 0.6f, 1f) * 2.0f, 0.8f, 0.1f);
            return m;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = localScale;
            if (mat != null) obj.GetComponent<Renderer>().sharedMaterial = mat;
            Collider col = obj.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            return obj;
        }

        private static void SavePrefab(GameObject root, string filename)
        {
            string path = $"{PrefabsDir}/{filename}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void GenerateObstaclePrefabs(Materials m)
        {
            // 1. Maglev Bullet Train
            {
                GameObject root = new GameObject("MaglevTrain_Obstacle");
                var obs = root.AddComponent<Obstacle>();
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(2.18f, 2.5f, 7.0f);
                box.center = new Vector3(0, 1.4f, 0);

                CreatePrimitive(PrimitiveType.Cube, "Undercarriage", root.transform, new Vector3(0, 0.40f, 0), new Vector3(1.92f, 0.30f, 7.0f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "IonField", root.transform, new Vector3(0, 0.15f, 0), new Vector3(1.64f, 0.08f, 6.44f), m.HoverBlue);
                CreatePrimitive(PrimitiveType.Cube, "MainBody", root.transform, new Vector3(0, 1.62f, 0), new Vector3(2.18f, 2.05f, 7.0f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "RoofShell", root.transform, new Vector3(0, 2.72f, 0), new Vector3(2.05f, 0.18f, 7.12f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "NoseWedge", root.transform, new Vector3(0, 1.55f, -3.72f), new Vector3(2.07f, 1.85f, 0.45f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "CockpitVisor", root.transform, new Vector3(0, 1.95f, -3.92f), new Vector3(1.83f, 0.75f, 0.12f), m.GlassDark);
                CreatePrimitive(PrimitiveType.Cube, "FrontBumper", root.transform, new Vector3(0, 0.48f, -3.85f), new Vector3(2.01f, 0.28f, 0.25f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "FrontNeon", root.transform, new Vector3(0, 0.62f, -3.96f), new Vector3(1.79f, 0.08f, 0.04f), m.NeonCyan);

                // Rear incline ramp
                CreatePrimitive(PrimitiveType.Cube, "RearRamp", root.transform, new Vector3(0, 1.48f, 4.10f), new Vector3(1.87f, 0.14f, 1.45f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "RampNeonGuide", root.transform, new Vector3(0, 1.55f, 4.10f), new Vector3(0.12f, 0.04f, 1.40f), m.NeonCyan);

                SavePrefab(root, "MaglevTrain_Obstacle");
            }

            // 2. Holo Jump Barrier
            {
                GameObject root = new GameObject("HoloJumpBarrier_Obstacle");
                root.AddComponent<Obstacle>();
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(2.15f, 0.85f, 0.24f);
                box.center = new Vector3(0, 0.45f, 0);

                CreatePrimitive(PrimitiveType.Cube, "PylonL", root.transform, new Vector3(-0.98f, 0.48f, 0), new Vector3(0.22f, 0.96f, 0.24f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "PylonR", root.transform, new Vector3(0.98f, 0.48f, 0), new Vector3(0.22f, 0.96f, 0.24f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "BaseBlock", root.transform, new Vector3(0, 0.35f, 0), new Vector3(2.15f, 0.45f, 0.16f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "NeonBeam", root.transform, new Vector3(0, 0.68f, 0), new Vector3(2.10f, 0.12f, 0.12f), m.NeonCyan);
                for (int i = -2; i <= 2; i++)
                {
                    CreatePrimitive(PrimitiveType.Cube, $"Chevron_{i}", root.transform, new Vector3(i * 0.38f, 0.90f, 0), new Vector3(0.18f, 0.22f, 0.05f), m.NeonCyan);
                }

                SavePrefab(root, "HoloJumpBarrier_Obstacle");
            }

            // 3. Overhead Cyber Scan Gantry
            {
                GameObject root = new GameObject("CyberScanGantry_Obstacle");
                root.AddComponent<Obstacle>();
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(2.40f, 0.65f, 0.36f);
                box.center = new Vector3(0, 2.25f, 0);

                CreatePrimitive(PrimitiveType.Cube, "PillarL", root.transform, new Vector3(-1.15f, 1.35f, 0), new Vector3(0.18f, 2.70f, 0.24f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "PillarR", root.transform, new Vector3(1.15f, 1.35f, 0), new Vector3(0.18f, 2.70f, 0.24f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "OverheadBeam", root.transform, new Vector3(0, 2.20f, 0), new Vector3(2.40f, 0.38f, 0.32f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "ScannerUnit", root.transform, new Vector3(0, 2.45f, -0.10f), new Vector3(1.20f, 0.24f, 0.22f), m.Titanium);
                for (int i = -3; i <= 3; i++)
                {
                    CreatePrimitive(PrimitiveType.Cube, $"LaserEmitter_{i}", root.transform, new Vector3(i * 0.30f, 1.90f, 0), new Vector3(0.14f, 0.22f, 0.06f), m.NeonYellow);
                }

                SavePrefab(root, "CyberScanGantry_Obstacle");
            }

            // 4. Induction Pit
            {
                GameObject root = new GameObject("InductionPit_Obstacle");
                root.AddComponent<Obstacle>();
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(2.24f, 0.12f, 2.20f);
                box.center = new Vector3(0, 0.02f, 0);

                CreatePrimitive(PrimitiveType.Cube, "PitFrame", root.transform, new Vector3(0, 0.03f, 0), new Vector3(2.24f, 0.06f, 2.20f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "PitTrench", root.transform, new Vector3(0, -0.04f, 0), new Vector3(1.95f, 0.06f, 1.95f), m.DarkMetal);
                for (int i = -2; i <= 2; i++)
                {
                    CreatePrimitive(PrimitiveType.Cube, $"Coil_{i}", root.transform, new Vector3(0, 0.01f, i * 0.38f), new Vector3(1.80f, 0.02f, 0.12f), m.NeonPink);
                }

                SavePrefab(root, "InductionPit_Obstacle");
            }

            // 5. Anti-Gravity Launch Pad
            {
                GameObject root = new GameObject("LaunchPad_Obstacle");
                root.AddComponent<Obstacle>();
                var box = root.AddComponent<BoxCollider>();
                box.size = new Vector3(2.10f, 0.15f, 1.90f);
                box.center = new Vector3(0, 0.06f, 0);

                CreatePrimitive(PrimitiveType.Cube, "Base", root.transform, new Vector3(0, 0.04f, 0), new Vector3(2.10f, 0.08f, 1.90f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "Plate", root.transform, new Vector3(0, 0.08f, 0), new Vector3(1.75f, 0.04f, 1.60f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "IonCore", root.transform, new Vector3(0, 0.11f, 0), new Vector3(1.20f, 0.03f, 1.10f), m.NeonCyan);
                CreatePrimitive(PrimitiveType.Cube, "UpChevron", root.transform, new Vector3(0, 0.35f, 0), new Vector3(0.45f, 0.45f, 0.05f), m.NeonCyan);

                SavePrefab(root, "LaunchPad_Obstacle");
            }
        }

        private static void GenerateEnvironmentPrefabs(Materials m)
        {
            // 1. Skyscraper Type A (Glass & Titanium High-Rise)
            {
                GameObject root = new GameObject("CyberSkyscraper_TypeA");
                float height = 24f;
                float width = 8.2f;
                float depth = 8.5f;

                CreatePrimitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0, height * 0.5f, 0), new Vector3(width, height, depth), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "GlassFacade", root.transform, new Vector3(-width * 0.5f - 0.04f, height * 0.5f, 0), new Vector3(0.08f, height - 1.5f, depth * 0.88f), m.GlassDark);

                for (int f = 1; f < 8; f++)
                {
                    CreatePrimitive(PrimitiveType.Cube, $"FloorNeon_{f}", root.transform, new Vector3(-width * 0.5f - 0.07f, f * 3.2f, 0), new Vector3(0.06f, 0.12f, depth * 0.82f), m.NeonCyan);
                }

                CreatePrimitive(PrimitiveType.Cube, "Crown", root.transform, new Vector3(0, height + 0.6f, 0), new Vector3(width * 0.8f, 1.2f, depth * 0.8f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "Spire", root.transform, new Vector3(0, height + 1.8f, 0), new Vector3(0.35f, 2.5f, 0.35f), m.NeonPink);

                SavePrefab(root, "CyberSkyscraper_TypeA");
            }

            // 2. Skyscraper Type B (Aerodynamic Megatower)
            {
                GameObject root = new GameObject("CyberSkyscraper_TypeB");
                float height = 36f;
                float width = 10.5f;
                float depth = 9.0f;

                CreatePrimitive(PrimitiveType.Cube, "Body", root.transform, new Vector3(0, height * 0.5f, 0), new Vector3(width, height, depth), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "RecessedSide", root.transform, new Vector3(width * 0.5f + 0.04f, height * 0.5f, 0), new Vector3(0.08f, height - 2f, depth * 0.85f), m.GlassHolo);

                for (int f = 1; f < 11; f++)
                {
                    CreatePrimitive(PrimitiveType.Cube, $"FloorBand_{f}", root.transform, new Vector3(width * 0.5f + 0.06f, f * 3.2f, 0), new Vector3(0.06f, 0.10f, depth * 0.80f), m.NeonPink);
                }

                CreatePrimitive(PrimitiveType.Cube, "SpireBase", root.transform, new Vector3(0, height + 0.8f, 0), new Vector3(width * 0.7f, 1.6f, depth * 0.7f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cylinder, "TwinSpire1", root.transform, new Vector3(-1.2f, height + 2.5f, 0), new Vector3(0.3f, 2.0f, 0.3f), m.NeonCyan);
                CreatePrimitive(PrimitiveType.Cylinder, "TwinSpire2", root.transform, new Vector3(1.2f, height + 2.5f, 0), new Vector3(0.3f, 2.0f, 0.3f), m.NeonCyan);

                SavePrefab(root, "CyberSkyscraper_TypeB");
            }

            // 3. Holo Billboard
            {
                GameObject root = new GameObject("HoloBillboard");
                CreatePrimitive(PrimitiveType.Cube, "Pylon", root.transform, new Vector3(0, 2.6f, 0), new Vector3(0.22f, 5.2f, 0.22f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, "HorizontalArm", root.transform, new Vector3(-0.6f, 5.2f, 0), new Vector3(1.4f, 0.18f, 0.18f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "Screen", root.transform, new Vector3(-1.5f, 6.0f, 0), new Vector3(0.05f, 2.2f, 4.6f), m.NeonCyan);
                CreatePrimitive(PrimitiveType.Cube, "Bezel", root.transform, new Vector3(-1.5f, 6.0f, 0), new Vector3(0.12f, 2.35f, 4.75f), m.Titanium);

                SavePrefab(root, "HoloBillboard");
            }

            // 4. Street Luminaire
            {
                GameObject root = new GameObject("StreetLuminaire");
                CreatePrimitive(PrimitiveType.Cube, "Pole", root.transform, new Vector3(0, 2.6f, 0), new Vector3(0.14f, 5.2f, 0.14f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "Arm", root.transform, new Vector3(-0.6f, 5.2f, 0), new Vector3(1.3f, 0.12f, 0.14f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "LampHead", root.transform, new Vector3(-1.1f, 5.08f, 0), new Vector3(0.45f, 0.10f, 0.25f), m.NeonYellow);

                SavePrefab(root, "StreetLuminaire");
            }

            // 5. Overhead Skybridge
            {
                GameObject root = new GameObject("OverheadSkybridge");
                CreatePrimitive(PrimitiveType.Cube, "BridgeTube", root.transform, new Vector3(0, 6.2f, 0), new Vector3(13.5f, 1.4f, 3.2f), m.Titanium);
                CreatePrimitive(PrimitiveType.Cube, "WindowFront", root.transform, new Vector3(0, 6.2f, -1.62f), new Vector3(12.8f, 0.9f, 0.06f), m.GlassHolo);
                CreatePrimitive(PrimitiveType.Cube, "WindowBack", root.transform, new Vector3(0, 6.2f, 1.62f), new Vector3(12.8f, 0.9f, 0.06f), m.GlassHolo);
                CreatePrimitive(PrimitiveType.Cube, "UnderGlow", root.transform, new Vector3(0, 5.45f, 0), new Vector3(13.2f, 0.08f, 3.1f), m.NeonCyan);

                SavePrefab(root, "OverheadSkybridge");
            }
        }

        private static void GenerateTrackAndCoinPrefabs(Materials m)
        {
            // 1. Cyber Coin
            {
                GameObject root = new GameObject("CyberCoin");
                root.AddComponent<CoinItem>();
                var col = root.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.55f;

                var coin = CreatePrimitive(PrimitiveType.Cylinder, "CoinDisk", root.transform, Vector3.zero, new Vector3(0.85f, 0.10f, 0.85f), m.GoldCoin);
                coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                CreatePrimitive(PrimitiveType.Sphere, "CoinCore", root.transform, new Vector3(0, 0, -0.06f), new Vector3(0.55f, 0.55f, 0.12f), m.GoldCoin);

                SavePrefab(root, "CyberCoin");
            }

            // 2. Cyber Track Tile (Full 3-lane Maglev Segment)
            {
                GameObject root = new GameObject("CyberTrackTile");
                float tileLength = 28f;

                // Collision road slab
                GameObject roadSlab = new GameObject("Roadbed_CollisionSlab");
                roadSlab.transform.SetParent(root.transform, false);
                roadSlab.transform.localPosition = new Vector3(0, -0.23f, tileLength * 0.5f);
                var slabCol = roadSlab.AddComponent<BoxCollider>();
                slabCol.size = new Vector3(9.3f, 0.45f, tileLength);

                // Road visual
                CreatePrimitive(PrimitiveType.Cube, "Roadbed_Visual", root.transform, new Vector3(0, -0.10f, tileLength * 0.5f), new Vector3(9.2f, 0.14f, tileLength), m.Road);

                // Glowing energy lane dividers (-1.325 and +1.325)
                for (int d = 0; d < 7; d++)
                {
                    float zPos = 2f + d * 4f;
                    CreatePrimitive(PrimitiveType.Cube, $"LaneDividerL_{d}", root.transform, new Vector3(-1.325f, 0.005f, zPos), new Vector3(0.10f, 0.015f, 2.2f), m.NeonCyan);
                    CreatePrimitive(PrimitiveType.Cube, $"LaneDividerR_{d}", root.transform, new Vector3(1.325f, 0.005f, zPos), new Vector3(0.10f, 0.015f, 2.2f), m.NeonCyan);
                }

                // Curbs & Holographic Guardrails
                for (int side = -1; side <= 1; side += 2)
                {
                    float s = side;
                    CreatePrimitive(PrimitiveType.Cube, $"Curb_{s}", root.transform, new Vector3(s * 4.65f, 0.10f, tileLength * 0.5f), new Vector3(0.30f, 0.22f, tileLength), m.Titanium);
                    CreatePrimitive(PrimitiveType.Cube, $"HoloGuardrail_{s}", root.transform, new Vector3(s * 4.65f, 0.38f, tileLength * 0.5f), new Vector3(0.08f, 0.36f, tileLength), m.GlassHolo);
                    CreatePrimitive(PrimitiveType.Cube, $"SafetyCap_{s}", root.transform, new Vector3(s * 4.65f, 0.58f, tileLength * 0.5f), new Vector3(0.10f, 0.05f, tileLength), m.NeonPink);
                    CreatePrimitive(PrimitiveType.Cube, $"Walkway_{s}", root.transform, new Vector3(s * 6.2f, 0.02f, tileLength * 0.5f), new Vector3(2.6f, 0.12f, tileLength), m.DarkMetal);
                }

                SavePrefab(root, "CyberTrackTile");
            }
        }

        private static void GeneratePlayerPrefab(Materials m)
        {
            GameObject root = new GameObject("Player_Runner");
            root.tag = "Player";
            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.15f, 0);
            col.radius = 0.45f;
            col.height = 2.3f;

            // Torso & Upper Body
            CreatePrimitive(PrimitiveType.Capsule, "Body_Torso", root.transform, new Vector3(0, 1.35f, 0), new Vector3(0.95f, 1.15f, 0.57f), m.DarkMetal);
            CreatePrimitive(PrimitiveType.Cube, "Jacket_Stripe", root.transform, new Vector3(0, 1.35f, 0.29f), new Vector3(0.14f, 0.90f, 0.05f), m.NeonCyan);
            CreatePrimitive(PrimitiveType.Cylinder, "Chest_ReactorCore", root.transform, new Vector3(0, 1.50f, 0.28f), new Vector3(0.24f, 0.05f, 0.24f), m.NeonCyan);

            // Head & Cyber Visor
            CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0, 2.29f, 0.05f), new Vector3(0.72f, 0.74f, 0.68f), m.DarkMetal);
            CreatePrimitive(PrimitiveType.Cube, "Cyber_Visor", root.transform, new Vector3(0, 2.32f, 0.36f), new Vector3(0.58f, 0.20f, 0.12f), m.NeonCyan);
            CreatePrimitive(PrimitiveType.Sphere, "HairCap", root.transform, new Vector3(0, 2.61f, -0.02f), new Vector3(0.76f, 0.31f, 0.76f), m.Titanium);

            // Backpack & Thrusters
            CreatePrimitive(PrimitiveType.Cube, "Backpack", root.transform, new Vector3(0, 1.34f, -0.41f), new Vector3(0.80f, 0.96f, 0.36f), m.Titanium);
            for (int side = -1; side <= 1; side += 2)
            {
                CreatePrimitive(PrimitiveType.Cylinder, $"Thruster_{(side < 0 ? "L" : "R")}", root.transform, new Vector3(side * 0.35f, 1.30f, -0.55f), new Vector3(0.20f, 0.60f, 0.20f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Sphere, $"Exhaust_{(side < 0 ? "L" : "R")}", root.transform, new Vector3(side * 0.35f, 0.92f, -0.55f), new Vector3(0.18f, 0.22f, 0.18f), m.NeonPink);
            }

            // Arms
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                CreatePrimitive(PrimitiveType.Capsule, $"Arm_{s}", root.transform, new Vector3(side * 0.55f, 1.48f, 0), new Vector3(0.30f, 0.62f, 0.32f), m.Titanium);
                CreatePrimitive(PrimitiveType.Sphere, $"Hand_{s}", root.transform, new Vector3(side * 0.55f, 1.10f, 0.05f), new Vector3(0.24f, 0.26f, 0.26f), m.DarkMetal);
            }

            // Legs & Sneakers
            for (int side = -1; side <= 1; side += 2)
            {
                string s = side < 0 ? "L" : "R";
                Material shoeMat = side < 0 ? m.NeonCyan : m.NeonPink;
                CreatePrimitive(PrimitiveType.Capsule, $"Leg_{s}", root.transform, new Vector3(side * 0.25f, 0.65f, 0), new Vector3(0.34f, 0.70f, 0.38f), m.DarkMetal);
                CreatePrimitive(PrimitiveType.Cube, $"Sneaker_{s}", root.transform, new Vector3(side * 0.25f, 0.16f, 0.10f), new Vector3(0.36f, 0.22f, 0.62f), shoeMat);
            }

            SavePrefab(root, "Player_Runner");
        }
    }
}
#endif
