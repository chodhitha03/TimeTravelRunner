#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace TimeTravellerRunner.Editor
{
    /// <summary>
    /// Editor utility that builds and configures the complete 3D Game Scene.
    /// Places Main Camera, Directional Light, Post-Processing Volume, EventSystem,
    /// Player character, and the modular Future City environment (tracks, buildings, obstacles, coins)
    /// into the scene hierarchy so everything is visible and inspectable in the Unity Editor before Play.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoBuildGameScene
    {
        private const string ScenePath = "Assets/_Game/Scenes/TimeTravellerRunner.unity";
        private const string PrefabsDir = "Assets/_Game/Prefabs/CyberCity";

        static AutoBuildGameScene()
        {
            EditorApplication.delayCall += EnsureScenePopulated;
        }

        private static void EnsureScenePopulated()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            if (!File.Exists(ScenePath))
            {
                SetupCompleteScene();
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled")
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                return;
            }

            if (activeScene.path == ScenePath)
            {
                if (GameObject.Find("Environment") == null || GameObject.Find("Player") == null)
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                    if (GameObject.Find("Environment") == null || GameObject.Find("Player") == null)
                    {
                        SetupCompleteScene();
                    }
                }
            }
        }

        [MenuItem("Tools/Setup Complete Game Scene", priority = 10)]
        [MenuItem("Time Traveller Runner/Setup Complete Game Scene", priority = 10)]
        public static void SetupCompleteScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            // 1. Ensure all materials and modular prefabs exist in Assets/_Game/
            CityPrefabGenerator.GenerateAll();

            // 2. Open or create the scene
            string directory = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Create Main Camera with cinematic perspective
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(0, 5.7f, -9.8f);
            camObj.transform.rotation = Quaternion.Euler(18f, 0, 0);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 1f);
            cam.fieldOfView = 64f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            camObj.AddComponent<AudioListener>();
            var camData = camObj.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // 4. Create Directional Light (Cyber Sunlight)
            GameObject lightObj = new GameObject("Directional Light");
            lightObj.transform.position = new Vector3(0, 20f, 0);
            lightObj.transform.rotation = Quaternion.Euler(40f, 22f, 0);
            Light sun = lightObj.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.90f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;

            // 5. Create Global Post-Processing Volume
            GameObject volObj = new GameObject("Global Volume");
            Volume volume = volObj.AddComponent<Volume>();
            volume.isGlobal = true;
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "CyberScenePostProfile";
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(1.2f);
            bloom.threshold.Override(0.85f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);
            volume.profile = profile;

            string profilePath = "Assets/_Game/Materials/CyberScenePostProfile.asset";
            if (!File.Exists(profilePath)) AssetDatabase.CreateAsset(profile, profilePath);

            // 6. Create UI EventSystem
            GameObject events = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            var module = events.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            events.AddComponent<StandaloneInputModule>();
#endif

            // 7. Create GameManager
            GameObject director = new GameObject("GameManager");
            var runnerGame = director.AddComponent<RunnerGame>();

            // 8. Instantiate Player Runner at origin (Lane 0)
            GameObject playerInstance = null;
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/Player_Runner.prefab");
            if (playerPrefab != null)
            {
                playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerInstance.name = "Player";
                playerInstance.transform.position = Vector3.zero;
                playerInstance.transform.rotation = Quaternion.identity;
            }

            // 9. Instantiate Environment hierarchy with Tracks, Skyscrapers, Props, Obstacles, and Coins
            GameObject env = new GameObject("Environment");

            // Tracks (Spanning from Z = -28 to Z = 84)
            GameObject tracksFolder = new GameObject("Tracks");
            tracksFolder.transform.SetParent(env.transform, false);
            GameObject trackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/CyberTrackTile.prefab");
            if (trackPrefab != null)
            {
                for (int i = -1; i <= 3; i++)
                {
                    GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(trackPrefab, tracksFolder.transform);
                    tile.name = $"TrackTile_{i + 1}";
                    tile.transform.localPosition = new Vector3(0, 0, i * 28f);
                }
            }

            // Skyscrapers Flanking the Avenue
            GameObject buildingsFolder = new GameObject("Skyscrapers");
            buildingsFolder.transform.SetParent(env.transform, false);
            GameObject towerA = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/CyberSkyscraper_TypeA.prefab");
            GameObject towerB = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/CyberSkyscraper_TypeB.prefab");
            if (towerA != null && towerB != null)
            {
                InstantiateAt(towerA, buildingsFolder.transform, "Skyscraper_L1", new Vector3(-12.5f, 0, 14f));
                InstantiateAt(towerB, buildingsFolder.transform, "Skyscraper_R1", new Vector3(12.5f, 0, 14f));
                InstantiateAt(towerB, buildingsFolder.transform, "Skyscraper_L2", new Vector3(-13.0f, 0, 42f));
                InstantiateAt(towerA, buildingsFolder.transform, "Skyscraper_R2", new Vector3(13.0f, 0, 42f));
                InstantiateAt(towerA, buildingsFolder.transform, "Skyscraper_L3", new Vector3(-12.5f, 0, 70f));
                InstantiateAt(towerB, buildingsFolder.transform, "Skyscraper_R3", new Vector3(12.5f, 0, 70f));
            }

            // Props & Lighting
            GameObject propsFolder = new GameObject("Props & Streetlights");
            propsFolder.transform.SetParent(env.transform, false);
            GameObject billboard = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/HoloBillboard.prefab");
            GameObject lamp = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/StreetLuminaire.prefab");
            GameObject skybridge = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/OverheadSkybridge.prefab");

            if (billboard != null)
            {
                InstantiateAt(billboard, propsFolder.transform, "HoloBillboard_L", new Vector3(-7.5f, 5.2f, 22f));
                InstantiateAt(billboard, propsFolder.transform, "HoloBillboard_R", new Vector3(7.5f, 5.2f, 50f));
            }
            if (lamp != null)
            {
                InstantiateAt(lamp, propsFolder.transform, "StreetLuminaire_L1", new Vector3(-4.85f, 0, 8f));
                InstantiateAt(lamp, propsFolder.transform, "StreetLuminaire_R1", new Vector3(4.85f, 0, 8f));
                InstantiateAt(lamp, propsFolder.transform, "StreetLuminaire_L2", new Vector3(-4.85f, 0, 36f));
                InstantiateAt(lamp, propsFolder.transform, "StreetLuminaire_R2", new Vector3(4.85f, 0, 36f));
            }
            if (skybridge != null)
            {
                InstantiateAt(skybridge, propsFolder.transform, "OverheadSkybridge", new Vector3(0, 0, 42f));
            }

            // Obstacles & Collectibles
            GameObject obsFolder = new GameObject("Obstacles & Collectibles");
            obsFolder.transform.SetParent(env.transform, false);
            GameObject train = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/MaglevTrain_Obstacle.prefab");
            GameObject barrier = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/HoloJumpBarrier_Obstacle.prefab");
            GameObject gantry = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/CyberScanGantry_Obstacle.prefab");
            GameObject coin = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/CyberCoin.prefab");

            if (barrier != null) InstantiateAt(barrier, obsFolder.transform, "HoloJumpBarrier_CenterLane", new Vector3(0, 0, 18f));
            if (gantry != null) InstantiateAt(gantry, obsFolder.transform, "CyberScanGantry_LeftLane", new Vector3(-2.65f, 0, 26f));
            if (train != null) InstantiateAt(train, obsFolder.transform, "MaglevBulletTrain_RightLane", new Vector3(2.65f, 0, 38f));

            if (coin != null)
            {
                for (int c = 0; c < 4; c++)
                {
                    InstantiateAt(coin, obsFolder.transform, $"CyberCoin_{c + 1}", new Vector3(0, 0.85f, 6f + c * 2.8f));
                }
            }

            GameObject pit = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/InductionPit_Obstacle.prefab");
            GameObject launchPad = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsDir}/LaunchPad_Obstacle.prefab");

            // Wire scene and prefab references directly into GameManager in the Inspector
            SerializedObject so = new SerializedObject(runnerGame);
            var camProp = so.FindProperty("followCamera");
            if (camProp != null) camProp.objectReferenceValue = cam;

            var sunProp = so.FindProperty("sun");
            if (sunProp != null) sunProp.objectReferenceValue = sun;

            var envProp = so.FindProperty("sceneEnvironment");
            if (envProp != null) envProp.objectReferenceValue = env;

            var plrProp = so.FindProperty("scenePlayer");
            if (plrProp != null) plrProp.objectReferenceValue = playerInstance;

            var prefabsProp = so.FindProperty("cyberPrefabs");
            if (prefabsProp != null)
            {
                var pTrack = prefabsProp.FindPropertyRelative("trackTile");
                if (pTrack != null) pTrack.objectReferenceValue = trackPrefab;

                var pTrain = prefabsProp.FindPropertyRelative("maglevTrain");
                if (pTrain != null) pTrain.objectReferenceValue = train;

                var pBarrier = prefabsProp.FindPropertyRelative("jumpBarrier");
                if (pBarrier != null) pBarrier.objectReferenceValue = barrier;

                var pGantry = prefabsProp.FindPropertyRelative("scanGantry");
                if (pGantry != null) pGantry.objectReferenceValue = gantry;

                var pPit = prefabsProp.FindPropertyRelative("inductionPit");
                if (pPit != null) pPit.objectReferenceValue = pit;

                var pPad = prefabsProp.FindPropertyRelative("launchPad");
                if (pPad != null) pPad.objectReferenceValue = launchPad;

                var pCoin = prefabsProp.FindPropertyRelative("coin");
                if (pCoin != null) pCoin.objectReferenceValue = coin;

                var pA = prefabsProp.FindPropertyRelative("skyscraperA");
                if (pA != null) pA.objectReferenceValue = towerA;

                var pB = prefabsProp.FindPropertyRelative("skyscraperB");
                if (pB != null) pB.objectReferenceValue = towerB;

                var pBill = prefabsProp.FindPropertyRelative("billboard");
                if (pBill != null) pBill.objectReferenceValue = billboard;

                var pLum = prefabsProp.FindPropertyRelative("luminaire");
                if (pLum != null) pLum.objectReferenceValue = lamp;

                var pSky = prefabsProp.FindPropertyRelative("skybridge");
                if (pSky != null) pSky.objectReferenceValue = skybridge;
            }
            so.ApplyModifiedProperties();

            // 10. Save and Refresh Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Time Traveller Runner] Successfully built complete 3D scene with camera, lighting, environment, and player!");
        }

        private static GameObject InstantiateAt(GameObject prefab, Transform parent, string name, Vector3 pos)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            obj.name = name;
            obj.transform.localPosition = pos;
            return obj;
        }

        [MenuItem("Tools/Open Game Scene", priority = 11)]
        [MenuItem("Time Traveller Runner/Open Game Scene", priority = 11)]
        public static void OpenGame()
        {
            if (!File.Exists(ScenePath)) SetupCompleteScene();
            if (File.Exists(ScenePath)) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Tools/Regenerate Game Scene", priority = 12)]
        [MenuItem("Time Traveller Runner/Regenerate Scene", priority = 12)]
        public static void RegenerateScene()
        {
            SetupCompleteScene();
        }
    }
}
#endif
