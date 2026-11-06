using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TimeTravellerRunner
{
    public enum GameMode { MainMenu, Playing, Paused, GameOver, CharacterSelect }

    [System.Serializable]
    public class CyberPrefabSet
    {
        public GameObject trackTile;
        public GameObject maglevTrain;
        public GameObject jumpBarrier;
        public GameObject scanGantry;
        public GameObject inductionPit;
        public GameObject launchPad;
        public GameObject coin;
        public GameObject skyscraperA;
        public GameObject skyscraperB;
        public GameObject billboard;
        public GameObject luminaire;
        public GameObject skybridge;
    }

    /// <summary>
    /// Core Game Manager coordinating the game loop, states, player movement, camera follow, and scoring.
    /// Connects to scene camera, lighting, and procedural track generation.
    /// </summary>
    public sealed class RunnerGame : MonoBehaviour
    {
        public GameMode Mode { get; private set; }
        public Era CurrentEra { get; private set; }
        public RunSelection SelectedRun { get; private set; }
        public bool CyberRun { get { return SelectedRun == RunSelection.CyberCity; } }
        public bool CyberHighDetail { get { return CyberCityBuilder.HighDetail; } }

        // Designer-facing settings: safely tune proportions during Play Mode in Inspector.
        [Header("Cyber City Dynamic Camera Framing")]
        public bool UseRecommendedFraming = true;
        [Range(6.0f, 14f)] public float CyberFollowDistance = 9.8f;
        [Range(3.5f, 7.5f)] public float CyberCameraHeight = 5.7f;
        [Range(50f, 75f)] public float CyberCameraFov = 64f;
        [Range(.60f, 1.4f)] public float CyberCharacterScale = 1.0f;
        [Range(6.0f, 14f)] public float CyberPreviewDistance = 10.0f;
        public int CyberCharacter { get { return CyberCharacterCatalog.Selected; } }
        public float Distance { get; private set; }
        public float Speed { get; private set; }
        public float EraBannerTime { get; private set; }
        public int Coins { get; private set; }
        public int BestScore { get; private set; }
        public int Score { get { return Mathf.FloorToInt(Distance * 4f) + Coins * 25; } }
        public bool ShieldActive { get; private set; }
        public bool CanRevive { get { return !reviveUsed && Coins >= 40; } }
        public string ActivePower { get; private set; }
        public float PowerTime { get; private set; }
        public CyberGadget SelectedCyberGadget { get { return CyberGadgetLibrary.Selected; } }
        public CyberGadget ActiveCyberGadget { get; private set; }
        public float CyberGadgetTime { get; private set; }
        public float CyberGadgetDuration { get; private set; }

        private RunnerWorld world;
        private RunnerAvatar avatar;
        private CyberGadgetVisual gadgetVisual;
        private readonly CyberRideMotion cyberRide = new CyberRideMotion();
        private CyberRideStageFX cyberRideEffects;
        // Public values make ride transitions easy to verify in the Inspector.
        public float CyberRideHeight { get { return cyberRide.Lift; } }
        public CyberGadget VisibleCyberGadget { get { return cyberRide.VisibleGadget; } }
        public float CyberLandingPose { get { return cyberRide.LandingPose; } }
        public float CyberTakeoffPose { get { return cyberRide.TakeoffPose; } }
        public bool CyberRideLanding { get { return cyberRide.Descending; } }
        public string CyberRidePhase { get { return cyberRide.Descending ? "DESCENDING" :
            cyberRide.VisibleGadget != CyberGadget.None ? "FLYING / RIDING" : "ON FOOT"; } }
        private RunnerAudio audioFx;
        private RunnerUI runnerUI;
        private CyberAtmosphere cyberAtmosphere;
        private float cameraKick;
        private Vector3 cyberCameraVelocity;
        [Header("Scene References")]
        [SerializeField] private Camera followCamera;
        [SerializeField] private Light sun;
        [SerializeField] private GameObject sceneEnvironment;
        [SerializeField] private GameObject scenePlayer;
        [Header("Modular Cyber City Prefabs")]
        [SerializeField] private CyberPrefabSet cyberPrefabs = new CyberPrefabSet();
        public CyberPrefabSet CyberPrefabs => cyberPrefabs;
        private Transform runtimeRoot;
        private float x, z, y, velocityY, slideTime, invulnerableTime;
        private int lane;
        private float elapsed;
        // Cyber arcade movement state: independent of the selected mesh / visual model.
        private float cyberLaneVelocity;
        private float cyberJumpBuffer;
        private float cyberGroundGrace;
        private float cyberLandingPulse;
        private int cyberJumpCount;
        private float robotDecisionCooldown;
        private int previousRobotCountdown;
        private float robotRecoveryTime;
        public bool RobotAutopilotActive { get { return CyberRun && ActiveCyberGadget == CyberGadget.Robot; } }
        public int RobotHandoverCountdown { get { return RobotAutopilotActive && CyberGadgetTime <= 5f ?
            Mathf.Clamp(Mathf.CeilToInt(CyberGadgetTime), 1, 5) : 0; } }
        public float RobotRecoveryTime { get { return robotRecoveryTime; } }
        public string RobotAction { get; private set; }
        public bool CyberJetFlight { get { return CyberRun && cyberRide.VisibleGadget == CyberGadget.Jetpack &&
            !cyberRide.Descending; } }
        public bool CyberBoardDoubleJumpReady { get { return CyberRun &&
            ActiveCyberGadget == CyberGadget.Hoverboard && cyberJumpCount == 1 && y > .1f; } }
        private bool cyberStarterReady;
        public bool CyberStarterReady { get { return CyberRun && cyberStarterReady; } }
        private bool reviveUsed;
        private Vector2 swipeStart;
        private bool swipeStarted;

        private void Awake()
        {
            // Set optimal default camera and character framing on launch.
            if (UseRecommendedFraming)
            {
                CyberFollowDistance = 9.8f;
                CyberCameraHeight = 5.7f;
                CyberCameraFov = 64f;
                CyberCharacterScale = 1.0f;
                CyberPreviewDistance = 10.0f;
            }
            Application.targetFrameRate = 60;
            BestScore = PlayerPrefs.GetInt("TTR_BEST_SCORE", 0);

            // Connect to pre-existing scene camera or create if missing
            if (followCamera == null) followCamera = Camera.main;
            if (followCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                followCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            var camData = followCamera.GetComponent<UniversalAdditionalCameraData>();
            if (camData == null) camData = followCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            followCamera.fieldOfView = CyberCameraFov;
            followCamera.nearClipPlane = .1f;
            followCamera.farClipPlane = 320;
            followCamera.clearFlags = CameraClearFlags.SolidColor;
            followCamera.backgroundColor = RunnerArt.Hex("#0B1320");

            // Connect to pre-existing scene light or create if missing
            if (sun == null) sun = FindFirstObjectByType<Light>();
            if (sun == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                sun = lightObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.6f;
                sun.transform.rotation = Quaternion.Euler(40f, 22f, 0f);
            }

            // Hide static scene preview elements during runtime gameplay to avoid collision overlaps
            if (sceneEnvironment == null) sceneEnvironment = GameObject.Find("Environment");
            if (sceneEnvironment != null) sceneEnvironment.SetActive(false);

            if (scenePlayer == null) scenePlayer = GameObject.Find("Player");
            if (scenePlayer != null) scenePlayer.SetActive(false);

            runtimeRoot = new GameObject("GameWorld").transform;
            runtimeRoot.SetParent(transform, false);

            CyberPostProcessing.Ensure(runtimeRoot);
            audioFx = new RunnerAudio(runtimeRoot);
            runnerUI = gameObject.GetComponent<RunnerUI>();
            if (runnerUI == null) runnerUI = gameObject.AddComponent<RunnerUI>();
            SelectedRun = RunSelection.CyberCity;
            EnsurePrefabsLoaded();
            ResetToMenu();
        }

        private void EnsurePrefabsLoaded()
        {
            if (cyberPrefabs.trackTile == null) cyberPrefabs.trackTile = Resources.Load<GameObject>("CyberCity/CyberTrackTile");
            if (cyberPrefabs.maglevTrain == null) cyberPrefabs.maglevTrain = Resources.Load<GameObject>("CyberCity/MaglevTrain_Obstacle");
            if (cyberPrefabs.jumpBarrier == null) cyberPrefabs.jumpBarrier = Resources.Load<GameObject>("CyberCity/HoloJumpBarrier_Obstacle");
            if (cyberPrefabs.scanGantry == null) cyberPrefabs.scanGantry = Resources.Load<GameObject>("CyberCity/CyberScanGantry_Obstacle");
            if (cyberPrefabs.inductionPit == null) cyberPrefabs.inductionPit = Resources.Load<GameObject>("CyberCity/InductionPit_Obstacle");
            if (cyberPrefabs.launchPad == null) cyberPrefabs.launchPad = Resources.Load<GameObject>("CyberCity/LaunchPad_Obstacle");
            if (cyberPrefabs.coin == null) cyberPrefabs.coin = Resources.Load<GameObject>("CyberCity/CyberCoin");
            if (cyberPrefabs.skyscraperA == null) cyberPrefabs.skyscraperA = Resources.Load<GameObject>("CyberCity/CyberSkyscraper_TypeA");
            if (cyberPrefabs.skyscraperB == null) cyberPrefabs.skyscraperB = Resources.Load<GameObject>("CyberCity/CyberSkyscraper_TypeB");
            if (cyberPrefabs.billboard == null) cyberPrefabs.billboard = Resources.Load<GameObject>("CyberCity/HoloBillboard");
            if (cyberPrefabs.luminaire == null) cyberPrefabs.luminaire = Resources.Load<GameObject>("CyberCity/StreetLuminaire");
            if (cyberPrefabs.skybridge == null) cyberPrefabs.skybridge = Resources.Load<GameObject>("CyberCity/OverheadSkybridge");
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            RenderSettings.fog = false;
            if (sceneEnvironment != null) sceneEnvironment.SetActive(true);
            if (scenePlayer != null) scenePlayer.SetActive(true);
        }

        public bool SoundEnabled { get { return audioFx != null && audioFx.Enabled; } }
        public void ToggleSound() { if (audioFx != null) audioFx.SetSoundEnabled(!audioFx.Enabled); }

        public void ToggleCyberQuality()
        {
            if (Mode != GameMode.MainMenu) return;
            CyberCityBuilder.HighDetail = !CyberCityBuilder.HighDetail;
            ResetToMenu();
        }

        public void ResetToMenu()
        {
            ResetWorld();
            Mode = GameMode.MainMenu;
        }

        // These are the only publicly playable modes until later development.
        public static bool IsModeEnabled(RunSelection selection)
        {
            return selection == RunSelection.CyberCity || selection == RunSelection.Demo || selection == RunSelection.AncientEgypt;
        }

        public void OpenCyberCharacterSelect()
        {
            if (Mode != GameMode.MainMenu) return;
            SelectedRun = RunSelection.CyberCity;
            ResetWorld();
            Mode = GameMode.CharacterSelect;
            audioFx.Confirm();
        }

        public void ChangeCyberCharacter(int direction)
        {
            if (Mode != GameMode.CharacterSelect) return;
            CyberCharacterCatalog.Selected = (CyberCharacter + direction + CyberCharacterCatalog.Count)
                % CyberCharacterCatalog.Count;
            if (avatar != null)
            {
                avatar.SetCyberCharacter(CyberCharacter);
                avatar.SetCyberModelScale(CyberCharacterScale);
            }
            audioFx.Confirm();
        }

        // Direct character card selection is more convenient than cycling with arrows.
        public void ChooseCyberCharacter(int index)
        {
            if (Mode != GameMode.CharacterSelect) return;
            int selected = Mathf.Clamp(index, 0, CyberCharacterCatalog.Count - 1);
            if (CyberCharacter == selected) return;
            CyberCharacterCatalog.Selected = selected;
            if (avatar != null)
            {
                avatar.SetCyberCharacter(selected);
                avatar.SetCyberModelScale(CyberCharacterScale);
            }
            audioFx.Confirm();
        }

        public void ChooseCyberGadget(CyberGadget gadget)
        {
            if (Mode != GameMode.CharacterSelect || gadget == CyberGadget.None) return;
            CyberGadgetLibrary.Selected = gadget;
            if (gadgetVisual != null) gadgetVisual.Show(gadget);
            audioFx.Confirm();
        }

        public void ActivateStartingCyberGadget()
        {
            if (!CyberRun || Mode != GameMode.Playing || !cyberStarterReady ||
                ActiveCyberGadget != CyberGadget.None || cyberRide.VisibleGadget != CyberGadget.None) return;
            cyberStarterReady = false;
            ActivateCyberGadget(SelectedCyberGadget, true);
        }

        public void StartRun(RunSelection selection)
        {
            if (!IsModeEnabled(selection)) return;
            SelectedRun = selection;
            StartRun();
        }

        public void StartRun()
        {
            if (!IsModeEnabled(SelectedRun)) return;
            ResetWorld();
            Mode = GameMode.Playing;
            audioFx.CyberAmbience(CyberRun);
            cyberStarterReady = CyberRun; // Gadget is equipped but only activates on G / HUD button.
            audioFx.Confirm();
        }

        private void ResetWorld()
        {
            Time.timeScale = 1f;
            if (audioFx != null) audioFx.CyberAmbience(false);
            if (world != null) Destroy(world.Root.gameObject);
            if (avatar != null) Destroy(avatar.Root.gameObject);
            gadgetVisual = null;
            if (cyberRideEffects != null && cyberRideEffects.Root != null)
                Destroy(cyberRideEffects.Root);
            cyberRideEffects = null;
            cyberRide.Reset();
            ActiveCyberGadget = CyberGadget.None;
            cyberStarterReady = false;
            CyberGadgetTime = 0f;
            CyberGadgetDuration = 0f;
            if (cyberAtmosphere != null) Destroy(cyberAtmosphere.gameObject);
            cyberAtmosphere = null;
            cameraKick = 0f;
            cyberCameraVelocity = Vector3.zero;
            cyberLaneVelocity = 0f; cyberJumpBuffer = 0f; cyberGroundGrace = 0f;
            cyberLandingPulse = 0f;
            cyberJumpCount = 0;
            robotDecisionCooldown = 0f;
            previousRobotCountdown = 0;
            robotRecoveryTime = 0f;
            RobotAction = "AUTOPILOT";
            BestScore = PlayerPrefs.GetInt(SelectedRun == RunSelection.Demo ?
                "TTR_BEST_SCORE" : "TTR_BEST_SCORE_" + SelectedRun, 0);
            lane = 1;
            x = 0; z = 0; y = 0; velocityY = 0; slideTime = 0;
            elapsed = 0; invulnerableTime = 0; Distance = 0; Coins = 0;
            Speed = 12; PowerTime = 0; ActivePower = "";
            ShieldActive = false; reviveUsed = false;
            CurrentEra = RunnerWorld.GetEra(0, SelectedRun);
            EraBannerTime = 0;
            world = new RunnerWorld(runtimeRoot, SelectedRun, cyberPrefabs);
            avatar = new RunnerAvatar(runtimeRoot, CyberCharacter);
            avatar.EnableCyberSuit(CyberRun);
            if (CyberRun) avatar.SetCyberModelScale(CyberCharacterScale);
            if (CyberRun)
            {
                gadgetVisual = new CyberGadgetVisual(avatar.Root);
                gadgetVisual.Show(CyberGadget.None);
                cyberRideEffects = new CyberRideStageFX(runtimeRoot);
            }
            if (CyberRun)
            {
                cyberAtmosphere = CyberAtmosphere.Create(runtimeRoot);
                cyberAtmosphere.Follow(0);
                CyberPostProcessing.Ensure(runtimeRoot);
            }
            avatar.Pose(0, 0, 0, false, false, false, 0);
            ApplyEra(CurrentEra);
            followCamera.transform.position = CyberRun ? new Vector3(0, CyberCameraHeight, -CyberFollowDistance) : new Vector3(0, 4.5f, -7.2f);
        }

        public void TogglePause()
        {
            if (Mode == GameMode.Playing)
            {
                Mode = GameMode.Paused;
                Time.timeScale = 0f;
                audioFx.Confirm();
            }
            else if (Mode == GameMode.Paused)
            {
                Mode = GameMode.Playing;
                Time.timeScale = 1f;
                audioFx.Confirm();
            }
        }

        private bool recoveryAttempted;

        private void Update()
        {
            // Safety guard: initialize world if needed.
            if (world == null || avatar == null || avatar.Root == null || followCamera == null)
            {
                if (!recoveryAttempted)
                {
                    recoveryAttempted = true;
                    Debug.LogWarning("World/character did not initialize. Attempting recovery.");
                    try { ResetWorld(); }
                    catch (System.Exception ex) { Debug.LogError("Recovery failed: " + ex); }
                }
                if (world == null || avatar == null || avatar.Root == null || followCamera == null) return;
            }

            if (RunnerInput.PausePressed() && (Mode == GameMode.Playing || Mode == GameMode.Paused))
                TogglePause();
            if (Mode == GameMode.MainMenu && RunnerInput.StartPressed()) OpenCyberCharacterSelect();
            else if (Mode == GameMode.CharacterSelect && RunnerInput.StartPressed()) StartRun(RunSelection.CyberCity);
            else if (Mode == GameMode.GameOver && RunnerInput.StartPressed()) StartRun();
            else if (Mode == GameMode.Playing) UpdatePlaying(Time.deltaTime);
            // Direct scale adjustment in Inspector, without touching the gameplay hitbox.
            if (CyberRun && avatar != null) avatar.SetCyberModelScale(CyberCharacterScale);
            if (Mode == GameMode.MainMenu || Mode == GameMode.CharacterSelect)
            {
                avatar.Pose(x, z, 0, false, false, false, Time.deltaTime);
                if (CyberRun && gadgetVisual != null)
                {
                    // Garage display only; gameplay always begins on the track and lifts smoothly.
                    CyberGadget preview = CyberGadgetLibrary.Selected;
                    gadgetVisual.Show(preview);
                    avatar.Root.localPosition += Vector3.up *
                        (preview == CyberGadget.Robot ? .62f :
                         preview == CyberGadget.Hoverboard ? .52f : .78f);
                    gadgetVisual.Animate(Time.time);
                }
            }
            UpdateCamera(Time.deltaTime);
        }

        private void UpdatePlaying(float dt)
        {
            elapsed += dt;
            if (CyberRun && RunnerInput.GadgetPressed()) ActivateStartingCyberGadget();
            bool autoRobot = RobotAutopilotActive;
            // During autopilot, the robot owns lane/vertical actions; the player
            // regains controls when the final 5-second handover ends.
            if (!autoRobot && RunnerInput.LeftPressed()) lane = Mathf.Max(0, lane - 1);
            if (!autoRobot && RunnerInput.RightPressed()) lane = Mathf.Min(2, lane + 1);
            bool wantsJump = !autoRobot && RunnerInput.JumpPressed();
            bool wantsSlide = !autoRobot && RunnerInput.SlidePressed();
            if (!autoRobot) HandleTouch(ref wantsJump, ref wantsSlide);
            if (autoRobot) UpdateCyberAutopilot(dt);
            if (CyberRun)
            {
                if (CyberJetFlight || cyberRide.Descending || autoRobot)
                {
                    // Jetpack = left/right only. During descent, also block jumps/slides.
                    wantsJump = false; wantsSlide = false;
                    cyberJumpBuffer = 0f;
                    if (!autoRobot) slideTime = 0f;
                }
                cyberJumpBuffer = wantsJump ? .14f : Mathf.Max(0f, cyberJumpBuffer - dt);
                cyberGroundGrace = y <= .025f ? .10f : Mathf.Max(0f, cyberGroundGrace - dt);
                if (y <= .025f && velocityY <= 0f) cyberJumpCount = 0;
                if (cyberJumpBuffer > 0f && slideTime <= .01f)
                {
                    if (cyberGroundGrace > 0f)
                    {
                        velocityY = 11.8f;
                        cyberJumpCount = 1;
                        cyberGroundGrace = 0f;
                        cyberJumpBuffer = 0f;
                        audioFx.Jump();
                    }
                    else if (CyberBoardDoubleJumpReady)
                    {
                        // Mid-air second jump: fresh upward impulse, no teleport.
                        velocityY = 11.0f;
                        cyberJumpCount = 2;
                        cyberJumpBuffer = 0f;
                        audioFx.Jump();
                        CyberPhysicsFx.Burst(runtimeRoot, new Vector3(x, y + cyberRide.Lift, z),
                            "#68EDFF", 6);
                    }
                }
                if (wantsSlide)
                {
                    if (y > .15f)
                    {
                        // Subway Surfers air dive-roll cancel
                        velocityY = -18.5f;
                        slideTime = .75f;
                        cyberJumpBuffer = 0;
                        audioFx.Slide();
                    }
                    else if (slideTime <= .01f)
                    {
                        slideTime = .82f;
                        audioFx.Slide();
                    }
                }
            }
            else
            {
                // Preserve Demo controls exactly as they were.
                if (wantsJump && y <= .015f && slideTime <= 0)
                { velocityY = 11.8f; audioFx.Jump(); }
                if (wantsSlide && slideTime <= 0 && y < .2f)
                { slideTime = .85f; audioFx.Slide(); }
            }
            if (slideTime > 0) slideTime = Mathf.Max(0f, slideTime - dt);
            if (invulnerableTime > 0) invulnerableTime = Mathf.Max(0f, invulnerableTime - dt);
            if (robotRecoveryTime > 0f) robotRecoveryTime = Mathf.Max(0f, robotRecoveryTime - dt);
            if (PowerTime > 0)
            {
                PowerTime -= dt;
                if (PowerTime <= 0) { ActivePower = ""; PowerTime = 0; }
            }
            if (EraBannerTime > 0) EraBannerTime -= dt;
            if (CyberRun && CyberGadgetTime > 0f)
            {
                CyberGadgetTime = Mathf.Max(0f, CyberGadgetTime - dt);
                if (CyberGadgetTime <= 0f) EndCyberGadget(false);
                else if (RobotAutopilotActive)
                {
                    int countdown = RobotHandoverCountdown;
                    if (countdown > 0 && countdown != previousRobotCountdown)
                        audioFx.RobotCountdown();
                    previousRobotCountdown = countdown;
                }
            }
            if (CyberRun) cyberRide.Tick(dt, elapsed);

            Speed = Mathf.Min(22f, 12f + Distance * .009f);
            if (ActivePower == "SPEED BOOST") Speed *= 1.42f;
            else if (ActivePower == "TIME FREEZE") Speed *= .62f;
            z += Speed * dt;
            Distance = z;
            if (CyberRun)
            {
                x = Mathf.SmoothDamp(x, RunnerWorld.LaneX(lane), ref cyberLaneVelocity,
                    .115f, 35f, Mathf.Max(.00001f, dt));
            }
            else x = Mathf.Lerp(x, RunnerWorld.LaneX(lane), Mathf.Clamp01(dt * 14f));
            if (y > 0 || velocityY > 0)
            {
                velocityY -= (CyberRun ? (velocityY < 0 ? 40f : 29f) : 27f) * dt;
                y += velocityY * dt;
                if (y < 0) { y = 0; velocityY = 0; if (CyberRun) cyberLandingPulse = .15f; }
            }
            Era encountered = RunnerWorld.GetEra(z, SelectedRun);
            if (encountered != CurrentEra)
            {
                CurrentEra = encountered;
                EraBannerTime = 3.5f;
                ApplyEra(CurrentEra);
                avatar.EnableCyberSuit(CyberRun);
                if (cyberAtmosphere != null) Destroy(cyberAtmosphere.gameObject);
                cyberAtmosphere = CyberRun ? CyberAtmosphere.Create(runtimeRoot) : null;
                audioFx.Portal();
            }
            world.EnsureAhead(z);
            if (CyberRun)
            {
                // Sky rewards are visible only during the jetpack sequence.
                // This stops inaccessible airborne coins confusing the track
                // when the player is on foot, riding the board or using robot.
                bool revealSkyTrail = cyberRide.VisibleGadget == CyberGadget.Jetpack;
                foreach (TrackThing sky in world.Things)
                    if (sky.Kind == ThingKind.AirCoin && !sky.Used && sky.Model != null &&
                        sky.Model.activeSelf != revealSkyTrail)
                        sky.Model.SetActive(revealSkyTrail);
            }
            if (cyberAtmosphere != null) cyberAtmosphere.Follow(z);
            if (cyberLandingPulse > 0) cyberLandingPulse = Mathf.Max(0f, cyberLandingPulse - dt);
            CheckWorld();
            avatar.Pose(x, z, y, velocityY, slideTime > 0,
                !CyberRun || (ActiveCyberGadget == CyberGadget.None && !cyberRide.Descending),
                ShieldActive, dt, CyberRun ? cyberRide.Lift : 0f,
                CyberRun ? Mathf.Max(cyberRide.LandingPose,
                    cyberLandingPulse / .4f) : 0f,
                CyberRun && cyberRide.Descending);
            if (CyberRun && gadgetVisual != null)
            {
                // Mesh, gear, and shadow use the same visible position each frame.
                avatar.Root.localPosition += Vector3.up * cyberRide.Lift;
                gadgetVisual.Show(cyberRide.VisibleGadget);
                gadgetVisual.Animate(elapsed, cyberRide.Lift, cyberRide.LandingPose);
                if (cyberRideEffects != null)
                    cyberRideEffects.Tick(x, z, cyberRide.Lift, cyberRide.LandedThisFrame, dt);
                if (cyberRide.LandedThisFrame)
                {
                    cyberLandingPulse = .4f;
                    cameraKick = Mathf.Max(cameraKick, .10f);
                    CyberPhysicsFx.Burst(runtimeRoot, new Vector3(x, .12f, z), "#62DBFF", 7);
                }
            }
        }

        // Route-aware cyber mech autopilot. This runs while Robot power-up is active.
        // Scans oncoming TrackThing hazards and steers into clear lanes.
        private void UpdateCyberAutopilot(float dt)
        {
            if (!RobotAutopilotActive || world == null) return;
            robotDecisionCooldown = Mathf.Max(0f, robotDecisionCooldown - dt);
            RobotAction = "SCANNING TRACK";
            TrackThing nearest = null;
            float nearestEdge = float.MaxValue;
            foreach (TrackThing t in world.Things)
            {
                if (t.Used || !CyberRobotAI.IsHazard(t.Kind)) continue;
                float edge = t.Z - t.HalfLength - z;
                if (edge < -.3f || edge > 27f) continue;
                if (edge < nearestEdge) { nearest = t; nearestEdge = edge; }
            }
            if (nearest != null)
            {
                // One coherent row at a time. Mark lanes occupied around the
                // earliest obstacle's centre, not unrelated hazards farther away.
                bool[] blocked = new bool[3];
                foreach (TrackThing t in world.Things)
                    if (!t.Used && CyberRobotAI.IsHazard(t.Kind) &&
                        Mathf.Abs(t.Z - nearest.Z) < 2.8f && t.Lane >= 0 && t.Lane < 3)
                        blocked[t.Lane] = true;
                int safeLane = CyberRobotAI.BestClearLane(lane, blocked);
                // For low hazards the robot visibly jumps or slides. It only
                // steers away from a full-sized train or a mixed dangerous row.
                bool fullBlock = false;
                foreach (TrackThing t in world.Things)
                    if (!t.Used && t.Lane == lane &&
                        Mathf.Abs(t.Z - nearest.Z) < 2.8f && t.Kind == ThingKind.Dodge)
                        fullBlock = true;
                if (nearestEdge < 22f && blocked[lane] && fullBlock && safeLane != lane &&
                    robotDecisionCooldown <= 0f)
                {
                    lane += safeLane > lane ? 1 : -1;
                    robotDecisionCooldown = .17f;
                    RobotAction = "EVADING TRAIN";
                }
                // If already within the event window, perform context-aware
                // jumps/slides. A blocked train lane should be evaded instead.
                if (nearestEdge < Mathf.Max(5.0f, Speed * .57f))
                {
                    foreach (TrackThing t in world.Things)
                    {
                        if (t.Used || t.Lane != lane ||
                            Mathf.Abs(t.Z - nearest.Z) > 2.8f ||
                            !CyberRobotAI.IsHazard(t.Kind)) continue;
                        if ((t.Kind == ThingKind.Jump || t.Kind == ThingKind.Pit) &&
                            y < .10f && velocityY <= .05f)
                        {
                            velocityY = 12.0f; cyberJumpCount = 1;
                            RobotAction = "AUTO JUMP";
                            audioFx.Jump();
                        }
                        else if (t.Kind == ThingKind.Slide && y < .12f && slideTime < .15f)
                        {
                            slideTime = .95f;
                            RobotAction = "AUTO SLIDE";
                            audioFx.Slide();
                        }
                    }
                }
            }
            // When the road ahead is clear, collect rewards intelligently.
            if (nearest == null || nearestEdge > 15f)
            {
                TrackThing coin = null;
                float best = float.MaxValue;
                foreach (TrackThing t in world.Things)
                {
                    if (t.Kind != ThingKind.Coin || t.Used) continue;
                    float ahead = t.Z - z;
                    if (ahead < 2f || ahead > 12f) continue;
                    float score = ahead + Mathf.Abs(t.Lane - lane) * 2.8f;
                    if (score < best) { best = score; coin = t; }
                }
                if (coin != null && coin.Lane != lane && robotDecisionCooldown <= 0f)
                {
                    lane += coin.Lane > lane ? 1 : -1;
                    robotDecisionCooldown = .27f;
                    RobotAction = "COIN ROUTE";
                }
            }
        }

        private void HandleTouch(ref bool wantsJump, ref bool wantsSlide)
        {
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    swipeStart = touch.position;
                    swipeStarted = true;
                }
                else if (touch.phase == TouchPhase.Ended && swipeStarted)
                {
                    Vector2 delta = touch.position - swipeStart;
                    swipeStarted = false;
                    if (delta.magnitude < 35) return;
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        lane = Mathf.Clamp(lane + (delta.x > 0 ? 1 : -1), 0, 2);
                    else if (delta.y > 0) wantsJump = true;
                    else wantsSlide = true;
                }
            }
#endif
        }

        /// <summary>Visible contact-edge distance for the NEXT hazard in the
        /// current lane. No fake predicted trigger based on decorative graphics.</summary>
        public bool TryGetCyberApproach(out ThingKind kind, out float meters, out float seconds)
        {
            kind = ThingKind.Dodge;
            meters = float.MaxValue;
            seconds = float.MaxValue;
            if (!CyberRun || Mode != GameMode.Playing || world == null ||
                RobotAutopilotActive ||
                (ActiveCyberGadget == CyberGadget.Jetpack ||
                 cyberRide.Lift > 1.5f && cyberRide.VisibleGadget == CyberGadget.Jetpack)) return false;
            foreach (TrackThing item in world.Things)
            {
                if (item == null || item.Used ||
                    Mathf.Abs(RunnerWorld.LaneX(item.Lane) - x) > 1.05f) continue;
                if (item.Kind != ThingKind.Jump && item.Kind != ThingKind.Dodge &&
                    item.Kind != ThingKind.Slide && item.Kind != ThingKind.Pit) continue;
                float edgeDistance = item.Z - item.HalfLength - .22f - z;
                if (edgeDistance < -.1f || edgeDistance >= meters) continue;
                meters = edgeDistance;
                kind = item.Kind;
            }
            if (meters == float.MaxValue) return false;
            seconds = meters / Mathf.Max(1f, Speed);
            return seconds <= 1.8f;
        }

        private void CheckWorld()
        {
            foreach (TrackThing item in world.Things)
            {
                // Pair with the visible obstacle half-length for responsive, precise hit registration.
                float reach = item.HalfLength + (CyberRun ? .22f : 0f);
                if (item.Used || Mathf.Abs(item.Z - z) > reach) continue;
                if (item.Model == null || Mathf.Abs(item.Model.transform.position.x - x) > 1.05f) continue;
                if (item.Kind == ThingKind.AirCoin)
                {
                    // Only collectible by an actively flying jetpack, at flight altitude.
                    // Ground pickup code below can never collect air coins.
                    if (CyberJetFlight && cyberRide.Lift >= 3.15f)
                    {
                        item.Used = true;
                        item.Model.SetActive(false);
                        Coins++;
                        audioFx.Coin();
                    }
                    continue;
                }
                // Jetpack excludes EVERY ground pickup for its full visible flight,
                // including takeoff and controlled descent (no coin magnet exception).
                if (CyberRun && cyberRide.VisibleGadget == CyberGadget.Jetpack &&
                    (item.Kind == ThingKind.Coin || item.Kind == ThingKind.Jetpack ||
                     item.Kind == ThingKind.Hoverboard || item.Kind == ThingKind.Robot ||
                     item.Kind == ThingKind.Freeze || item.Kind == ThingKind.Boost ||
                     item.Kind == ThingKind.Shield || item.Kind == ThingKind.Magnet ||
                     item.Kind == ThingKind.LaunchPad)) continue;
                // Ground collectibles cannot magically be collected five metres up.
                if (CyberRun && cyberRide.VisibleGadget == CyberGadget.Jetpack &&
                    cyberRide.Lift > 1.5f &&
                    (item.Kind == ThingKind.Coin || item.Kind == ThingKind.Jetpack ||
                     item.Kind == ThingKind.Hoverboard || item.Kind == ThingKind.Robot ||
                     item.Kind == ThingKind.Freeze || item.Kind == ThingKind.Boost ||
                     item.Kind == ThingKind.Shield || item.Kind == ThingKind.Magnet ||
                     item.Kind == ThingKind.LaunchPad)) continue;
                if (item.Kind == ThingKind.LaunchPad)
                {
                    item.Used = true;
                    if (RobotAutopilotActive) continue;
                    if (y < .25f)
                    {
                        velocityY = 15.3f;
                        slideTime = 0;
                        cameraKick = .24f;
                        audioFx.PowerUp();
                        CyberPhysicsFx.Burst(runtimeRoot, new Vector3(x, .1f, z), "#4cefff", 9);
                    }
                    continue;
                }
                if (item.Kind == ThingKind.Coin)
                {
                    Coins++;
                    item.Used = true;
                    item.Model.SetActive(false);
                    audioFx.Coin();
                    if (runnerUI != null) runnerUI.OnCoinCollected();
                    CyberPhysicsFx.Burst(runtimeRoot, new Vector3(x, y + 1.2f, z), "#FFD700", 7);
                }
                else if (CyberRun && (item.Kind == ThingKind.Jetpack ||
                    item.Kind == ThingKind.Hoverboard || item.Kind == ThingKind.Robot))
                {
                    // Never cancel a promised 20s robot run by driving into
                    // another optional gadget pickup.
                    if (RobotAutopilotActive) continue;
                    item.Used = true;
                    item.Model.SetActive(false);
                    ActivateCyberGadget(item.Kind == ThingKind.Jetpack ? CyberGadget.Jetpack :
                        item.Kind == ThingKind.Hoverboard ? CyberGadget.Hoverboard : CyberGadget.Robot, false);
                }
                else if (item.Kind == ThingKind.Freeze || item.Kind == ThingKind.Boost ||
                    item.Kind == ThingKind.Shield || item.Kind == ThingKind.Magnet)
                {
                    item.Used = true;
                    item.Model.SetActive(false);
                    CollectPower(item.Kind);
                }
                else if (CyberRun && cyberRide.VisibleGadget == CyberGadget.Jetpack)
                {
                    // Jet is above ground hazards. Also guard its ascent/descent
                    // while its visual collider has not cleared a tall train yet.
                    item.Used = true;
                }
                else if (RobotAutopilotActive)
                {
                    // Active mech dismantles hazards on contact, even when the AI
                    // has to cross a blocked row to protect the runner.
                    item.Used = true;
                    if (item.Model != null) item.Model.SetActive(false);
                    CyberPhysicsFx.Burst(runtimeRoot, new Vector3(x, 1.5f, z + .3f),
                        "#8CFFD0", 14);
                    RobotAction = "THREAT DISABLED";
                    audioFx.ShieldHit();
                }
                else if (invulnerableTime <= 0)
                {
                    bool escaped = (item.Kind == ThingKind.Jump && y > .95f)
                        || (item.Kind == ThingKind.Pit && y > .75f)
                        || (item.Kind == ThingKind.Slide && slideTime > 0);
                    if (!escaped)
                    {
                        item.Used = true;
                        if (CyberRun)
                        {
                            CyberPhysicsFx.Burst(runtimeRoot, new Vector3(x, 1.3f, z + .4f),
                                ShieldActive ? "#4ffbff" : "#ff5aaa", ShieldActive ? 13 : 18);
                            cameraKick = ShieldActive ? .35f : .9f;
                        }
                        if (CyberRun && (ActiveCyberGadget == CyberGadget.Hoverboard ||
                            ActiveCyberGadget == CyberGadget.Robot))
                        {
                            // The board/mech absorbs exactly one hazard impact then breaks.
                            EndCyberGadget(true);
                            invulnerableTime = 1.85f;
                            audioFx.ShieldHit();
                        }
                        else if (ShieldActive)
                        {
                            ShieldActive = false;
                            invulnerableTime = 1.8f;
                            audioFx.ShieldHit();
                        }
                        else { GameOver(); return; }
                    }
                    else item.Used = true;
                }
            }
        }

        private void ActivateCyberGadget(CyberGadget gadget, bool starter)
        {
            if (!CyberRun || gadget == CyberGadget.None) return;
            ActiveCyberGadget = gadget;
            CyberGadgetDuration = CyberGadgetLibrary.Duration(gadget);
            // Robot always supplies a full 20 seconds, including when selected
            // as starter equipment. Other starters keep their shorter demo period.
            CyberGadgetTime = gadget == CyberGadget.Robot ? CyberGadgetDuration :
                starter ? Mathf.Min(9f, CyberGadgetDuration) : CyberGadgetDuration;
            robotDecisionCooldown = 0f;
            cyberJumpCount = 0;
            // Never teleport the player: CyberRideMotion eases from their current height.
            y = 0; velocityY = 0; slideTime = 0;
            cyberRide.Equip(gadget);
            previousRobotCountdown = 0;
            if (gadget == CyberGadget.Robot) audioFx.RobotStart();
            else if (gadget == CyberGadget.Jetpack) audioFx.JetStart();
            else if (gadget == CyberGadget.Hoverboard) audioFx.HoverboardStart();
            invulnerableTime = Mathf.Max(invulnerableTime, .65f);
            if (gadgetVisual != null) gadgetVisual.Show(gadget);
            audioFx.PowerUp();
            CyberPhysicsFx.Burst(runtimeRoot, new Vector3(x, 1.0f, z),
                gadget == CyberGadget.Jetpack ? "#59EFFF" :
                gadget == CyberGadget.Hoverboard ? "#FFDF76" : "#A4FF9F", 12);
        }

        private void EndCyberGadget(bool damaged)
        {
            if (ActiveCyberGadget == CyberGadget.None) return;
            // On handover: four seconds for the player to reacquire control,
            // immune to hazards while the mech visually dismounts.
            if (ActiveCyberGadget == CyberGadget.Robot && !damaged)
            {
                robotRecoveryTime = 4f;
                audioFx.RobotRelease();
                invulnerableTime = Mathf.Max(invulnerableTime, 4f);
                cyberJumpBuffer = 0f;
            }
            ActiveCyberGadget = CyberGadget.None;
            CyberGadgetTime = 0;
            CyberGadgetDuration = 0;
            // Keep device attached while descending; damage gets a short dismount.
            cyberRide.Release(damaged);
            if (!damaged) invulnerableTime = Mathf.Max(invulnerableTime, 1.75f);
            // Visual removal happens inside CyberRideMotion after touchdown.
        }

        private void CollectPower(ThingKind power)
        {
            audioFx.PowerUp();
            if (power == ThingKind.Shield) { ShieldActive = true; return; }
            if (power == ThingKind.Freeze) { ActivePower = "TIME FREEZE"; PowerTime = 7f; }
            else if (power == ThingKind.Boost) { ActivePower = "SPEED BOOST"; PowerTime = 7f; }
            else if (power == ThingKind.Magnet)
            {
                ActivePower = "COIN MAGNET";
                PowerTime = 10f;
            }
        }

        private void LateUpdate()
        {
            if (Mode != GameMode.Playing || world == null) return;
            // Magnets and robot coin scanners never collect ground coins while flying.
            if (CyberRun && cyberRide.VisibleGadget == CyberGadget.Jetpack) return;
            bool traditionalMagnet = ActivePower == "COIN MAGNET";
            bool robotScan = CyberRun && ActiveCyberGadget == CyberGadget.Robot;
            if (!traditionalMagnet && !robotScan) return;
            foreach (TrackThing item in world.Things)
            {
                if (item.Kind != ThingKind.Coin || item.Used) continue;
                if (Mathf.Abs(item.Z - z) < (robotScan ? 9f : 6.3f) &&
                    Mathf.Abs(item.Lane - lane) <= 1)
                {
                    item.Used = true;
                    if (item.Model != null) item.Model.SetActive(false);
                    Coins++;
                    audioFx.Coin();
                }
            }
        }

        private void GameOver()
        {
            Mode = GameMode.GameOver;
            audioFx.Hit();
            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt(SelectedRun == RunSelection.Demo ?
                    "TTR_BEST_SCORE" : "TTR_BEST_SCORE_" + SelectedRun, BestScore);
                PlayerPrefs.Save();
            }
        }

        public void Revive()
        {
            if (Mode != GameMode.GameOver || !CanRevive) return;
            Coins -= 40;
            reviveUsed = true;
            invulnerableTime = 3f;
            velocityY = 0; y = 0; slideTime = 0;
            foreach (TrackThing item in world.Things)
                if (Mathf.Abs(item.Z - z) < 4) item.Used = true;
            Mode = GameMode.Playing;
            audioFx.PowerUp();
        }

        private void UpdateCamera(float dt)
        {
            if (followCamera == null) return;
            cameraKick = Mathf.MoveTowards(cameraKick, 0, dt * 1.5f);
            if (Mode == GameMode.CharacterSelect)
            {
                // Present the character face-on. No manual preview camera or RenderTexture needed.
                // Full-body stage preview, framed in the right side of the character garage.
                // Framing uses the same field of view regardless of the selected outfit.
                Vector3 preview = new Vector3(0f, 2.35f, z + CyberPreviewDistance);
                followCamera.transform.position = Vector3.Lerp(followCamera.transform.position,
                    preview, Mathf.Clamp01(dt * 4.5f));
                Quaternion face = Quaternion.LookRotation(new Vector3(2.55f, 1.25f, z) - followCamera.transform.position);
                followCamera.transform.rotation = Quaternion.Slerp(followCamera.transform.rotation,
                    face, Mathf.Clamp01(dt * 6.5f));
                followCamera.fieldOfView = Mathf.Lerp(followCamera.fieldOfView, 50f, dt * 4f);
                return;
            }
            // Camera distance and pitch are tuned for Subway Surfers-style hero framing.
            // Generous view distance down the tracks matching the popular Earth Demo perspective.
            Vector3 intended = CyberRun
                ? new Vector3(x * 0.22f, CyberCameraHeight + y * 0.15f +
                    cyberRide.Lift * 0.20f, z - CyberFollowDistance)
                : new Vector3(x * .22f, 5.7f + y * .15f, z - 9.8f);
            if (cameraKick > .001f)
                intended += new Vector3(Mathf.Sin(Time.time * 65f), Mathf.Cos(Time.time * 47f), 0) * cameraKick * .27f;
            if (CyberRun)
                followCamera.transform.position = Vector3.SmoothDamp(followCamera.transform.position,
                    intended, ref cyberCameraVelocity, .12f, 90f, Mathf.Max(.0001f, dt));
            else
                followCamera.transform.position = Vector3.Lerp(followCamera.transform.position,
                    intended, Mathf.Clamp01(dt * 5.5f));
            Vector3 focus = CyberRun ? new Vector3(x * 0.10f,
                1.50f + cyberRide.Lift * 0.20f, z + 13.0f)
                : new Vector3(x * .1f, 1.5f, z + 13f);
            Quaternion rot = Quaternion.LookRotation(focus - followCamera.transform.position);
            if (CyberRun)
            {
                float camLean = Mathf.Clamp(-cyberLaneVelocity * 0.12f, -3.5f, 3.5f);
                rot *= Quaternion.Euler(0, 0, camLean);
            }
            followCamera.transform.rotation = Quaternion.Slerp(followCamera.transform.rotation, rot,
                Mathf.Clamp01(dt * 8f));
            float targetFov = Mode == GameMode.Playing && ActivePower == "SPEED BOOST" ? 78 :
                CyberRun ? Mathf.Lerp(CyberCameraFov, 75f, Mathf.Clamp01((Speed - 12f) / 10f)) : 64f;
            followCamera.fieldOfView = Mathf.Lerp(followCamera.fieldOfView, targetFov, dt * 2.8f);
        }

        private void ApplyEra(Era era)
        {
            // A sunny, natural Earth look for Demo. No cyber lighting, dinosaur jungle
            // palette or ancient-desert haze can leak into this run.
            if (SelectedRun == RunSelection.Demo)
            {
                followCamera.backgroundColor = RunnerArt.Hex("#9FD4EE");
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = .0032f;
                RenderSettings.fogColor = RunnerArt.Hex("#B9DFEC");
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = RunnerArt.Hex("#C7CFCA");
                sun.color = RunnerArt.Hex("#FFF0D8");
                sun.intensity = 1.35f;
                return;
            }
            string sky = era == Era.DinosaurAge ? "#8cc9cf" : era == Era.AncientEgypt ? "#ebbd84" : "#5AA9E6";
            if (CyberRun) sky = "#5AA9E6";
            followCamera.backgroundColor = RunnerArt.Hex(sky);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = CyberRun ? .0018f : era == Era.CyberCity ? .009f : era == Era.AncientEgypt ? .0035f : .0065f;
            RenderSettings.fogColor = RunnerArt.Hex(CyberRun ? "#A3DCFD" : sky);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = RunnerArt.Hex(CyberRun ? "#8899B0" : era == Era.CyberCity ? "#737dc4" : era == Era.AncientEgypt ? "#e0c5a0" : "#ddddc5");
            sun.color = RunnerArt.Hex(CyberRun ? "#FFF8EE" : era == Era.DinosaurAge ? "#fff0d2" : era == Era.AncientEgypt ? "#fff1d0" : "#94baff");
            sun.intensity = CyberRun ? 1.55f : era == Era.CyberCity ? .82f : 1.65f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .35f;
        }

        // UI is rendered through an interactive Unity Canvas.
    }

    internal static class RunnerInput
    {
        public static bool GadgetPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) return true;
#endif
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.G);
#else
            return false;
#endif
        }
        public static bool LeftPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.aKey.wasPressedThisFrame ||
                Keyboard.current.leftArrowKey.wasPressedThisFrame)) return true;
#endif
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
#else
            return false;
#endif
        }
        public static bool RightPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.dKey.wasPressedThisFrame ||
                Keyboard.current.rightArrowKey.wasPressedThisFrame)) return true;
#endif
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
#else
            return false;
#endif
        }
        public static bool JumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame ||
                Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)) return true;
#endif
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.UpArrow);
#else
            return false;
#endif
        }
        public static bool SlidePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.sKey.wasPressedThisFrame ||
                Keyboard.current.downArrowKey.wasPressedThisFrame)) return true;
#endif
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
#else
            return false;
#endif
        }
        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.pKey.wasPressedThisFrame ||
                Keyboard.current.escapeKey.wasPressedThisFrame)) return true;
#endif
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
        public static bool StartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) return true;
#endif
#if !ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Return);
#else
            return false;
#endif
        }
    }
}
