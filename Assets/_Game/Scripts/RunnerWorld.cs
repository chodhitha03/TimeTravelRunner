using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeTravellerRunner
{
    public enum Era { DinosaurAge = 0, AncientEgypt = 1, CyberCity = 2, NormalEarth = 3 }
    // Demo is an ordinary present-day Earth run; Cyber City is a separate mode.
    public enum RunSelection { Demo, CyberCity, AncientEgypt, DinosaurAge }
    public enum ThingKind { Coin, Jump, Slide, Dodge, Pit, Freeze, Boost, Shield, Magnet, LaunchPad, Jetpack, Hoverboard, Robot, AirCoin }

    public sealed class TrackThing
    {
        public ThingKind Kind;
        public int Lane;
        public float Z;
        public GameObject Model;
        public float HalfLength = .73f; // Large cyber vehicles collide before their nose reaches the player.
        public bool Used;
    }

    /// <summary>Streams endless-runner road chunks. Demo remains on present-day Earth.</summary>
    public sealed class RunnerWorld
    {
        public const float LaneSpacing = 2.65f;
        public const float TileLength = 28f;
        public const int SegmentsPerEra = 9;
        public const float EraDistance = TileLength * SegmentsPerEra;
        private readonly Transform root;
        private readonly RunSelection selection;
        private readonly CyberPrefabSet prefabs;
        private readonly Dictionary<int, GameObject> tiles = new Dictionary<int, GameObject>();
        private readonly System.Random random = new System.Random(314159);
        private readonly List<TrackThing> things = new List<TrackThing>();
        private int nextTile;

        public List<TrackThing> Things { get { return things; } }
        public Transform Root { get { return root; } }
        public static float LaneX(int lane) { return (lane - 1) * LaneSpacing; }
        // Normal Earth is the only Demo environment, regardless of distance.
        // Distances do NOT cause a time-period change or spawn a time portal.
        public static Era GetEra(float z) { return Era.NormalEarth; }
        public static Era GetEra(float z, RunSelection selection)
        {
            switch (selection)
            {
                case RunSelection.CyberCity: return Era.CyberCity;
                case RunSelection.AncientEgypt: return Era.AncientEgypt;
                case RunSelection.DinosaurAge: return Era.DinosaurAge; // Locked in game controller
                default: return Era.NormalEarth;
            }
        }
        public static string SelectionName(RunSelection selection)
        {
            return selection == RunSelection.Demo ? "NORMAL EARTH DEMO" :
                selection == RunSelection.CyberCity ? "FUTURE CYBER CITY" :
                selection == RunSelection.AncientEgypt ? "ANCIENT EGYPT" : "DINOSAUR AGE";
        }
        public static string EraName(Era era)
        {
            switch (era)
            {
                case Era.NormalEarth: return "NORMAL EARTH";
                case Era.DinosaurAge: return "DINOSAUR AGE";
                case Era.AncientEgypt: return "ANCIENT EGYPT";
                default: return "FUTURE CYBER CITY";
            }
        }

        public RunnerWorld(Transform parent, RunSelection runSelection, CyberPrefabSet cyberPrefabs = null)
        {
            selection = runSelection;
            prefabs = cyberPrefabs;
            root = new GameObject("TrackWorld - " + SelectionName(selection)).transform;
            root.SetParent(parent, false);
            EnsureAhead(0);
        }

        public void EnsureAhead(float playerZ)
        {
            int needed = Mathf.FloorToInt((playerZ + 235f) / TileLength);
            while (nextTile <= needed)
            {
                BuildTile(nextTile);
                nextTile++;
            }
            List<int> expired = new List<int>();
            foreach (KeyValuePair<int, GameObject> tile in tiles)
            {
                if ((tile.Key + 1) * TileLength < playerZ - 38f)
                {
                    UnityEngine.Object.Destroy(tile.Value);
                    expired.Add(tile.Key);
                }
            }
            foreach (int tileIndex in expired) tiles.Remove(tileIndex);
            things.RemoveAll(t => t.Used || t.Z < playerZ - 32f || t.Model == null);
        }

        private int Next(int upper) { return random.Next(upper); }
        private float R(float low, float high) { return low + (float)random.NextDouble() * (high - low); }

        private void BuildTile(int index)
        {
            Era era = GetEra(index * TileLength, selection);
            Transform tile = new GameObject("Track " + index.ToString("00") + " / " + EraName(era)).transform;
            tile.SetParent(root, false);
            float startZ = index * TileLength;
            tile.localPosition = new Vector3(0, 0, startZ);
            tiles[index] = tile.gameObject;
            string road = era == Era.NormalEarth ? "#4F5962" : era == Era.DinosaurAge ? "#8e795c" : era == Era.AncientEgypt ? "#bc9662" : "#202441";
            string edging = era == Era.NormalEarth ? "#E1E1D7" : era == Era.DinosaurAge ? "#5f6652" : era == Era.AncientEgypt ? "#cfae78" : "#879096";
            if (selection == RunSelection.CyberCity)
            {
                // One collision slab per tile supports the physically simulated debris.
                GameObject collisionRoad = new GameObject("PHYSICS / 3-lane road collision slab");
                collisionRoad.transform.SetParent(tile, false);
                collisionRoad.transform.localPosition = new Vector3(0, -.23f, TileLength * .5f);
                BoxCollider slab = collisionRoad.AddComponent<BoxCollider>();
                slab.size = new Vector3(9.3f, .45f, TileLength);
            }
            else
            {
                // Conventional asphalt track for Demo, using the existing 3-lane physics.
                RunnerArt.Box("Main running surface", tile, new Vector3(0, -.3f, TileLength / 2),
                    new Vector3(9.3f, .6f, TileLength), road);
                RunnerArt.Box("Left edge", tile, new Vector3(-4.65f, .12f, TileLength / 2),
                    new Vector3(.15f, .25f, TileLength), edging, era == Era.CyberCity);
                RunnerArt.Box("Right edge", tile, new Vector3(4.65f, .12f, TileLength / 2),
                    new Vector3(.15f, .25f, TileLength), edging, era == Era.CyberCity);
                string divider = era == Era.NormalEarth ? "#FCFAEA" : era == Era.CyberCity ? "#a0aaa8" : era == Era.AncientEgypt ? "#e8cf9e" : "#c4ac86";
                for (int laneLine = -1; laneLine <= 1; laneLine += 2)
                    for (int dash = 0; dash < 7; dash++)
                        RunnerArt.Box("Lane dash", tile,
                            new Vector3(laneLine * LaneSpacing * .5f, .012f, dash * 4f + 1.5f),
                            new Vector3(.085f, .025f, 1.85f), divider, false);
            }

            if (selection == RunSelection.Demo) DemoEarthBuilder.Build(tile, index);
            else if (era == Era.DinosaurAge) MakeJungle(tile, index);
            else if (era == Era.AncientEgypt) DesertThemeBuilder.Build(tile, index);
            else MakeCyber(tile, index);
            MakeContent(tile, index, startZ, era);
        }

        private void MakeJungle(Transform tile, int index)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                RunnerArt.Box("Jungle ground", tile, new Vector3(side * 13f, -.45f, 14),
                    new Vector3(17f, .8f, 28f), "#4f7844");
                for (int i = 0; i < 3; i++)
                {
                    float x = side * R(7.6f, 16.5f);
                    float z = (i + .5f) * 8.2f;
                    RunnerArt.Tree(tile, new Vector3(x, 0, z), R(.72f, 1.2f));
                    RunnerArt.Ball("Mossy stone", tile,
                        new Vector3(side * R(5.6f, 7.1f), .35f, z + 2),
                        new Vector3(R(.8f, 1.6f), .75f, 1.15f), "#69785b");
                }
            }
            if (index % 3 == 1)
            {
                // Stylized ancient ruin, kept outside the playable lanes.
                RunnerArt.Box("Temple post L", tile, new Vector3(-5.9f, 1.75f, 15),
                    new Vector3(1.4f, 3.5f, 1.4f), "#8c8971");
                RunnerArt.Box("Temple post R", tile, new Vector3(5.9f, 1.75f, 15),
                    new Vector3(1.4f, 3.5f, 1.4f), "#8c8971");
                RunnerArt.Box("Temple bridge", tile, new Vector3(0, 3.7f, 15),
                    new Vector3(13.2f, 1.1f, 1.35f), "#aaa18a");
            }
            if (index % 4 == 2) MakeDinosaur(tile, new Vector3(-11.5f, 0, 20));
        }

        private void MakeDinosaur(Transform tile, Vector3 pos)
        {
            Transform d = new GameObject("Decorative friendly dinosaur").transform;
            d.SetParent(tile, false);
            d.localPosition = pos;
            RunnerArt.Ball("Body", d, new Vector3(0, 1.7f, 0), new Vector3(2.7f, 2.1f, 1.7f), "#52866a");
            RunnerArt.Ball("Neck", d, new Vector3(.8f, 2.7f, .3f), new Vector3(.95f, 2.1f, .9f), "#609779");
            RunnerArt.Ball("Head", d, new Vector3(1.05f, 3.65f, .45f), new Vector3(1.25f, .9f, 1.05f), "#609779");
            RunnerArt.Ball("Snout", d, new Vector3(1.5f, 3.36f, .78f), new Vector3(.75f, .4f, .7f), "#81b28d");
            RunnerArt.Ball("Eye", d, new Vector3(1.38f, 3.88f, 1f), new Vector3(.13f, .13f, .13f), "#1b2e2b");
            for (int i = 0; i < 3; i++)
            {
                RunnerArt.Ball("Tail", d, new Vector3(-1.3f - i * .67f, 1.85f + i * .23f, -.1f),
                    new Vector3(1.25f - i * .23f, .8f - i * .15f, .8f - i * .1f), "#4d8167");
            }
            for (int i = -1; i <= 1; i += 2)
                RunnerArt.Cylinder("Leg", d, new Vector3(i * .7f, .7f, 0),
                    new Vector3(.35f, .7f, .35f), "#396f5b");
        }

        private void MakeDesert(Transform tile, int index)
        {
            RunnerArt.Box("Sand left", tile, new Vector3(-15, -.46f, 14),
                new Vector3(21, .8f, 28), "#d6b274");
            RunnerArt.Box("Sand right", tile, new Vector3(15, -.46f, 14),
                new Vector3(21, .8f, 28), "#d6b274");
            for (int side = -1; side <= 1; side += 2)
            {
                RunnerArt.Palm(tile, new Vector3(side * R(7.5f, 10f), 0, 6), R(.7f, 1.05f));
                RunnerArt.Pyramid("Ancient pyramid", tile,
                    new Vector3(side * R(18f, 24f), -.12f, 17), R(4f, 6f), R(9f, 13f), "#cfad73");
                if (index % 2 == 0)
                {
                    RunnerArt.Cylinder("Stone column", tile,
                        new Vector3(side * 6f, 2.2f, 20), new Vector3(.66f, 2.2f, .66f), "#b88c59");
                    RunnerArt.Box("Column top", tile,
                        new Vector3(side * 6f, 4.47f, 20), new Vector3(1.8f, .5f, 1.8f), "#edce97");
                }
            }
        }

        private void MakeCyber(Transform tile, int index)
        {
            if (selection == RunSelection.CyberCity)
            {
                if (prefabs != null && prefabs.trackTile != null)
                {
                    // 1. Modular 3-lane Maglev Track Tile Prefab
                    GameObject trackObj = UnityEngine.Object.Instantiate(prefabs.trackTile, tile);
                    trackObj.name = "CyberTrackTile";
                    trackObj.transform.localPosition = Vector3.zero;

                    // 2. Modular Skyscraper Prefabs flanking the avenue
                    if (prefabs.skyscraperA != null && prefabs.skyscraperB != null)
                    {
                        GameObject leftTower = (index % 2 == 0) ? prefabs.skyscraperA : prefabs.skyscraperB;
                        GameObject rightTower = (index % 2 == 0) ? prefabs.skyscraperB : prefabs.skyscraperA;

                        GameObject tL = UnityEngine.Object.Instantiate(leftTower, tile);
                        tL.transform.localPosition = new Vector3(-12.5f, 0, 14f);

                        GameObject tR = UnityEngine.Object.Instantiate(rightTower, tile);
                        tR.transform.localPosition = new Vector3(12.5f, 0, 14f);
                    }

                    // 3. Floating Holographic Billboards
                    if (prefabs.billboard != null && index % 3 == 1)
                    {
                        float side = (index % 2 == 0) ? 1f : -1f;
                        GameObject b = UnityEngine.Object.Instantiate(prefabs.billboard, tile);
                        b.transform.localPosition = new Vector3(side * 7.5f, 5.2f, 14f);
                    }

                    // 4. Street Lighting Luminaires
                    if (prefabs.luminaire != null && index % 2 == 0)
                    {
                        GameObject lL = UnityEngine.Object.Instantiate(prefabs.luminaire, tile);
                        lL.transform.localPosition = new Vector3(-4.85f, 0, 8f);
                        GameObject lR = UnityEngine.Object.Instantiate(prefabs.luminaire, tile);
                        lR.transform.localPosition = new Vector3(4.85f, 0, 8f);
                    }

                    // 5. Overhead Skybridge crossing the avenue
                    if (prefabs.skybridge != null && index % 4 == 2)
                    {
                        GameObject sb = UnityEngine.Object.Instantiate(prefabs.skybridge, tile);
                        sb.transform.localPosition = new Vector3(0, 0, 14f);
                    }
                    return;
                }
                CyberCityBuilder.Build(tile, index);
                return;
            }
            // Demo mode preserves the ORIGINAL simpler future-city scenery.
            RunnerArt.Box("City block left", tile, new Vector3(-16, -.48f, 14),
                new Vector3(22f, .84f, 28), "#11172d");
            RunnerArt.Box("City block right", tile, new Vector3(16, -.48f, 14),
                new Vector3(22f, .84f, 28), "#11172d");
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 3; i++)
                    RunnerArt.NeonBuilding(tile, new Vector3(side * R(9f, 16f), 0, i * 9.5f + 3),
                        R(8, 19), i % 2 == 0 ? "#d152f4" : "#28d9ff");
                RunnerArt.Cylinder("Cyber street light", tile, new Vector3(side * 5.5f, 2.6f, 13),
                    new Vector3(.11f, 2.6f, .11f), "#a4d8f5", true);
                RunnerArt.Ball("Streetlight orb", tile, new Vector3(side * 5.5f, 5.3f, 13),
                    new Vector3(.6f, .6f, .6f), "#41e4ff", true);
            }
        }

        private void MakeContent(Transform tile, int index, float startZ, Era era)
        {
            if (selection == RunSelection.CyberCity)
            {
                MakeCyberContent(tile, index, startZ, era);
                return;
            }
            // Level 0 has no hazards: players can first understand the controls.
            if (index == 0)
            {
                for (int i = 0; i < 7; i++)
                    SpawnThing(tile, ThingKind.Coin, 1, 8f + i * 2.5f, startZ, era);
                return;
            }
            for (int row = 0; row < 2; row++)
            {
                float localZ = row == 0 ? 8f : 20.5f;
                int hazardLane = Next(3);
                ThingKind hazard;
                int option = Next(4);
                hazard = option == 0 ? ThingKind.Slide : option == 1 ? ThingKind.Dodge
                    : option == 2 ? ThingKind.Pit : ThingKind.Jump;
                SpawnThing(tile, hazard, hazardLane, localZ, startZ, era);
                int secondLane = -1;
                if (index > 2 && index % 4 == 0 && row == 1)
                {
                    secondLane = (hazardLane + 1 + Next(2)) % 3;
                    SpawnThing(tile, ThingKind.Jump, secondLane, localZ, startZ, era);
                }
                // In a two-hazard cyber row, coins must lead through the genuinely open lane.
                int safeLane = selection == RunSelection.CyberCity && secondLane >= 0
                    ? 3 - hazardLane - secondLane : (hazardLane + 1 + Next(2)) % 3;
                for (int i = 0; i < 4; i++)
                    SpawnThing(tile, ThingKind.Coin, safeLane, localZ - 4f + i * 1.5f, startZ, era);
            }
            if (era == Era.CyberCity && selection == RunSelection.CyberCity && index % 5 == 3)
            {
                // Cyber-only magnetic jump pad: physically launches the runner, no key required.
                SpawnThing(tile, ThingKind.LaunchPad, Next(3), 13.4f, startZ, era);
            }
            else if (index % 3 == 0)
            {
                ThingKind[] powers = { ThingKind.Shield, ThingKind.Magnet, ThingKind.Freeze, ThingKind.Boost };
                SpawnThing(tile, powers[(index / 3) % powers.Length], Next(3), 14f, startZ, era);
            }
        }

        private void MakeCyberContent(Transform tile, int index, float startZ, Era era)
        {
            if (index == 0)
            {
                for (int i = 0; i < 6; i++)
                    SpawnThing(tile, ThingKind.Coin, 1, 7f + i * 2.8f, startZ, era);
                for (int i = 0; i < 3; i++)
                    SpawnThing(tile, ThingKind.AirCoin, 1, 9f + i * 6f, startZ, era);
                return;
            }

            // One well-separated hazard row per 28-metre block.
            // Predictable approach time ensures athletic, responsive 3D gameplay.
            float at = 18.0f;
            int hazardLane = Next(3);
            int pattern = (index - 1) % 6;
            ThingKind kind = pattern == 0 ? ThingKind.Dodge :
                pattern == 1 ? ThingKind.Jump :
                pattern == 2 ? ThingKind.Slide :
                pattern == 3 ? ThingKind.Pit :
                pattern == 4 ? ThingKind.Jump : ThingKind.Dodge;
            SpawnThing(tile, kind, hazardLane, at, startZ, era);

            int secondLane = -1;
            // Later challenges can block a second lane, never all three.
            if (index > 12 && index % 8 == 0)
            {
                secondLane = (hazardLane + 1 + Next(2)) % 3;
                SpawnThing(tile, ThingKind.Jump, secondLane, at, startZ, era);
            }
            int clearLane = secondLane >= 0 ? 3 - hazardLane - secondLane :
                (hazardLane + 1 + Next(2)) % 3;
            // Every sixth segment has a visible, optional sci-fi power. The near
            // side of the hazard at 18m remains >12m beyond the pickup; none are
            // placed inside trains, on trap tiles, or in a forced evade lane.
            bool gadgetRow = index % 6 == 1;
            if (gadgetRow)
            {
                int cycle = ((index - 1) / 6) % 3;
                ThingKind power = cycle == 0 ? ThingKind.Jetpack :
                    cycle == 1 ? ThingKind.Hoverboard : ThingKind.Robot;
                SpawnThing(tile, power, clearLane, 4.2f, startZ, era);
            }
            for (int i = 0; i < 4; i++)
                SpawnThing(tile, ThingKind.Coin, clearLane,
                    (gadgetRow ? 8.4f : 5f) + i * 2.3f, startZ, era);
            // Separate sky reward trail at actual flight altitude, not ground coins.
            // Every cyber segment has 3 airborne coins, with alternating lanes.
            // There is no air-coin collision when a jetpack is not flying.
            for (int i = 0; i < 3; i++)
            {
                int skyLane = (index + i) % 3;
                SpawnThing(tile, ThingKind.AirCoin, skyLane,
                    7.5f + i * 7.3f, startZ, era);
            }
            // Keep power-ups away from the collision row and NEVER spawn a
            // boost-pad in the runner's required evade lane.
            if (!gadgetRow && index % 5 == 2)
            {
                ThingKind[] pickups = { ThingKind.Shield, ThingKind.Magnet,
                    ThingKind.Freeze, ThingKind.Boost };
                SpawnThing(tile, pickups[(index / 5) % pickups.Length],
                    clearLane, 25.0f, startZ, era);
            }
        }

        private void SpawnThing(Transform tile, ThingKind kind, int lane, float localZ, float startZ, Era era)
        {
            Transform obj = new GameObject(kind + " / lane " + (lane + 1)).transform;
            obj.SetParent(tile, false);
            obj.localPosition = new Vector3(LaneX(lane),
                selection == RunSelection.CyberCity && kind == ThingKind.AirCoin ? 5.15f : 0f,
                localZ);
            if (selection == RunSelection.CyberCity && kind == ThingKind.AirCoin)
            {
                // The generated coin's centre is local y=1.55; world y=6.70,
                // matching a runner flying at 5.15m with torso at ~1.5m.
                GameObject core = RunnerArt.Cylinder("SKY COIN / Gold Reward", obj,
                    new Vector3(0, 1.55f, 0f), new Vector3(.48f, .065f, .48f), "#FFCB41", true);
                core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                core.AddComponent<CyberMotion>().Configure(CyberMotion.MotionType.Spin, 1f, 85f);
                RunnerArt.Ring("SKY COIN / cyan flight aura", obj,
                    new Vector3(0f, 1.55f, .02f), .65f, .065f, "#4DEAFF");
                RunnerArt.Ball("SKY COIN / gold centre", obj,
                    new Vector3(0f, 1.55f, -.08f), new Vector3(.30f, .30f, .07f), "#FFD34F", true);
                things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ,
                    Model = obj.gameObject, HalfLength = .86f });
                return;
            }
            if (selection == RunSelection.Demo &&
                (kind == ThingKind.Jump || kind == ThingKind.Slide ||
                 kind == ThingKind.Dodge || kind == ThingKind.Pit))
            {
                float halfLength = DemoEarthBuilder.PopulateObstacle(obj, kind);
                things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ,
                    Model = obj.gameObject, HalfLength = halfLength });
                return;
            }
            string obstacle = era == Era.DinosaurAge ? "#795842" : era == Era.AncientEgypt ? "#a9673c" : "#ff4f8a";
            if (selection == RunSelection.CyberCity &&
                (kind == ThingKind.LaunchPad || kind == ThingKind.Dodge || kind == ThingKind.Jump ||
                 kind == ThingKind.Slide || kind == ThingKind.Pit))
            {
                if (kind == ThingKind.Dodge && prefabs != null && prefabs.maglevTrain != null)
                {
                    GameObject train = UnityEngine.Object.Instantiate(prefabs.maglevTrain, obj);
                    train.transform.localPosition = Vector3.zero;
                    var obs = train.GetComponent<Obstacle>();
                    float hLen = obs != null ? obs.HalfLength : 3.5f;
                    things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ, Model = obj.gameObject, HalfLength = hLen });
                    return;
                }
                if (kind == ThingKind.Jump && prefabs != null && prefabs.jumpBarrier != null)
                {
                    GameObject barrier = UnityEngine.Object.Instantiate(prefabs.jumpBarrier, obj);
                    barrier.transform.localPosition = Vector3.zero;
                    var obs = barrier.GetComponent<Obstacle>();
                    float hLen = obs != null ? obs.HalfLength : 0.65f;
                    things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ, Model = obj.gameObject, HalfLength = hLen });
                    return;
                }
                if (kind == ThingKind.Slide && prefabs != null && prefabs.scanGantry != null)
                {
                    GameObject gantry = UnityEngine.Object.Instantiate(prefabs.scanGantry, obj);
                    gantry.transform.localPosition = Vector3.zero;
                    var obs = gantry.GetComponent<Obstacle>();
                    float hLen = obs != null ? obs.HalfLength : 0.45f;
                    things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ, Model = obj.gameObject, HalfLength = hLen });
                    return;
                }
                if (kind == ThingKind.Pit && prefabs != null && prefabs.inductionPit != null)
                {
                    GameObject pit = UnityEngine.Object.Instantiate(prefabs.inductionPit, obj);
                    pit.transform.localPosition = Vector3.zero;
                    var obs = pit.GetComponent<Obstacle>();
                    float hLen = obs != null ? obs.HalfLength : 1.15f;
                    things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ, Model = obj.gameObject, HalfLength = hLen });
                    return;
                }
                if (kind == ThingKind.LaunchPad && prefabs != null && prefabs.launchPad != null)
                {
                    GameObject pad = UnityEngine.Object.Instantiate(prefabs.launchPad, obj);
                    pad.transform.localPosition = Vector3.zero;
                    var obs = pad.GetComponent<Obstacle>();
                    float hLen = obs != null ? obs.HalfLength : 1.10f;
                    things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ, Model = obj.gameObject, HalfLength = hLen });
                    return;
                }
                float halfLength = CyberObstacleFactory.Populate(obj, kind);
                things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ,
                    Model = obj.gameObject, HalfLength = halfLength });
                return;
            }
            if (selection == RunSelection.CyberCity &&
                (kind == ThingKind.Jetpack || kind == ThingKind.Hoverboard || kind == ThingKind.Robot))
            {
                CyberGadget gadget = kind == ThingKind.Jetpack ? CyberGadget.Jetpack :
                    kind == ThingKind.Hoverboard ? CyberGadget.Hoverboard : CyberGadget.Robot;
                CyberGadgetArt.MakePickup(obj, gadget);
                things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ,
                    Model = obj.gameObject, HalfLength = .66f });
                return;
            }
            if (selection == RunSelection.CyberCity &&
                (kind == ThingKind.Shield || kind == ThingKind.Magnet ||
                 kind == ThingKind.Freeze || kind == ThingKind.Boost))
            {
                CyberGadgetArt.MakeLegacyPickup(obj,kind);
                things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ,
                    Model = obj.gameObject, HalfLength = .63f });
                return;
            }
            if (kind == ThingKind.LaunchPad)
            {
                RunnerArt.Box("MAGNETIC LAUNCH PAD", obj, new Vector3(0, .065f, 0),
                    new Vector3(2.12f, .12f, 2.1f), "#123c6c");
                RunnerArt.Box("Launch surface", obj, new Vector3(0, .135f, 0),
                    new Vector3(1.55f, .04f, 1.66f), "#38eaff", true);
                for (int i = -1; i <= 1; i += 2)
                    RunnerArt.Box("Launch ion rail", obj, new Vector3(i * .86f, .21f, 0),
                        new Vector3(.13f, .15f, 1.9f), "#f664ff", true);
                obj.gameObject.AddComponent<CyberMotion>().Configure(CyberMotion.MotionType.Bob, .025f, 2.8f);
            }
            else if (kind == ThingKind.Coin)
            {
                if (selection == RunSelection.CyberCity)
                {
                    if (prefabs != null && prefabs.coin != null)
                    {
                        GameObject coinInst = UnityEngine.Object.Instantiate(prefabs.coin, obj);
                        coinInst.transform.localPosition = new Vector3(0, 1.25f, 0);
                        things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ,
                            Model = obj.gameObject, HalfLength = .5f });
                        return;
                    }
                    RunnerArt.CyberCoin(obj, new Vector3(0, 1.50f, 0));
                }
                else
                {
                    GameObject coin = RunnerArt.Cylinder("GOLD COIN", obj,
                        new Vector3(0, 1.55f, 0), new Vector3(.44f, .07f, .44f), "#ffcb41", true);
                    coin.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    RunnerArt.Ball("Coin centre", obj, new Vector3(0, 1.55f, -.09f),
                        new Vector3(.29f, .29f, .06f), "#f39b21");
                }
            }
            else if (kind == ThingKind.Jump)
            {
                if (era == Era.DinosaurAge)
                {
                    GameObject log = RunnerArt.Cylinder("JUMP LOG", obj, new Vector3(0, .43f, 0),
                        new Vector3(.48f, 1.07f, .48f), obstacle);
                    log.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    for (int i = -1; i <= 1; i += 2)
                        RunnerArt.Ball("Log end", obj, new Vector3(i * 1.07f, .43f, 0),
                            new Vector3(.13f, .95f, .95f), "#b08451");
                }
                else if (era == Era.AncientEgypt)
                {
                    RunnerArt.Cylinder("CLAY POT 1", obj, new Vector3(-.32f, .45f, 0),
                        new Vector3(.60f, .44f, .60f), "#b65e34");
                    RunnerArt.Cylinder("Pot rim 1", obj, new Vector3(-.32f, .91f, 0),
                        new Vector3(.68f, .1f, .68f), "#eaa36a");
                    RunnerArt.Cylinder("Turquoise band 1", obj, new Vector3(-.32f, .65f, 0),
                        new Vector3(.63f, .08f, .63f), "#26A69A");

                    RunnerArt.Cylinder("CLAY POT 2", obj, new Vector3(.35f, .35f, .12f),
                        new Vector3(.48f, .34f, .48f), "#c56c38");
                    RunnerArt.Cylinder("Pot rim 2", obj, new Vector3(.35f, .71f, .12f),
                        new Vector3(.54f, .08f, .54f), "#FFD54F");
                }
                else
                {
                    // Demo's original robot model; Cyber City obstacles are already
                    // handled in CyberObstacleFactory.Populate() and returned above.
                    RunnerArt.Box("Fallen robot", obj, new Vector3(0, .41f, 0),
                        new Vector3(1.7f, .78f, 1f), "#637e9d");
                    RunnerArt.Box("Robot visor", obj, new Vector3(0, .55f, -.54f),
                        new Vector3(1.12f, .21f, .08f), "#36e8ff", true);
                }
            }
            else if (kind == ThingKind.Slide)
            {
                string beam = era == Era.CyberCity ? "#ff4b9b" : era == Era.AncientEgypt ? "#A27341" : "#bc6d3e";
                RunnerArt.Box("LOW BAR - SLIDE", obj, new Vector3(0, 1.92f, 0),
                    new Vector3(2.28f, .4f, .57f), beam, era == Era.CyberCity);
                if (era == Era.AncientEgypt)
                {
                    RunnerArt.Box("Gold Inlay", obj, new Vector3(0, 1.92f, -.30f),
                        new Vector3(2.1f, .12f, .05f), "#FFD54F");
                }
                for (int side = -1; side <= 1; side += 2)
                    RunnerArt.Box("Bar support", obj, new Vector3(side * 1.05f, 1f, 0),
                        new Vector3(.24f, 2f, .65f), era == Era.CyberCity ? "#4ae7f0" : era == Era.AncientEgypt ? "#8F6438" : "#806a4a");
            }
            else if (kind == ThingKind.Pit)
            {
                RunnerArt.Box("DANGEROUS GAP", obj, new Vector3(0, .018f, 0),
                    new Vector3(2.25f, .04f, 2.25f), era == Era.CyberCity ? "#04091b" : "#33271d");
                for (int side = -1; side <= 1; side += 2)
                    RunnerArt.Box("Pit highlight", obj, new Vector3(side * 1.13f, .035f, 0),
                        new Vector3(.11f, .05f, 2.3f), era == Era.CyberCity ? "#ff48a0" : "#f2a94e", era == Era.CyberCity);
            }
            else if (kind == ThingKind.Dodge)
            {
                RunnerArt.Box("BLOCKING OBSTACLE", obj, new Vector3(0, 1.05f, 0),
                    new Vector3(1.87f, 2.1f, 1.7f), obstacle);
                if (era == Era.AncientEgypt)
                {
                    RunnerArt.Ball("Sun Disc", obj, new Vector3(0, 1.6f, -.88f), new Vector3(.44f, .44f, .1f), "#FFD54F");
                    RunnerArt.Box("Wing L", obj, new Vector3(-.48f, 1.6f, -.88f), new Vector3(.55f, .16f, .08f), "#FFD54F");
                    RunnerArt.Box("Wing R", obj, new Vector3(.48f, 1.6f, -.88f), new Vector3(.55f, .16f, .08f), "#FFD54F");
                }
                else
                {
                    RunnerArt.Box("Obstacle warning", obj, new Vector3(0, 1.34f, -.87f),
                        new Vector3(1.42f, .25f, .08f), "#ffcc54", era == Era.CyberCity);
                }
            }
            else
            {
                string color = kind == ThingKind.Shield ? "#40eaff" : kind == ThingKind.Magnet ? "#f35fb3"
                    : kind == ThingKind.Freeze ? "#a6e9ff" : "#ff9651";
                RunnerArt.Ring("POWER UP", obj, new Vector3(0, 1.55f, 0), .57f, .14f, color, true);
                RunnerArt.Ball("Power core", obj, new Vector3(0, 1.55f, 0),
                    new Vector3(.52f, .52f, .42f), color, true);
                if (selection == RunSelection.CyberCity)
                    obj.gameObject.AddComponent<CyberMotion>().Configure(CyberMotion.MotionType.Bob, .18f, 3f);
            }
            things.Add(new TrackThing { Kind = kind, Lane = lane, Z = startZ + localZ, Model = obj.gameObject });
        }
    }
}
