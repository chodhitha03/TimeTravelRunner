using UnityEngine;

namespace TimeTravellerRunner
{
    // Power-up identity stays independent from hazard ThingKind and the character skin.
    public enum CyberGadget { None = 0, Jetpack = 1, Hoverboard = 2, Robot = 3 }

    public static class CyberGadgetLibrary
    {
        private const string SaveKey = "TTR_CYBER_START_GADGET";
        public static CyberGadget Selected
        {
            get { return (CyberGadget)Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 1), 1, 3); }
            set
            {
                PlayerPrefs.SetInt(SaveKey, Mathf.Clamp((int)value, 1, 3));
                PlayerPrefs.Save();
            }
        }
        public static string Name(CyberGadget gadget)
        {
            switch (gadget)
            {
                case CyberGadget.Jetpack: return "SKY JET";
                case CyberGadget.Hoverboard: return "PHASE BOARD";
                case CyberGadget.Robot: return "GUARDIAN MECH";
                default: return "ON FOOT";
            }
        }
        public static string Description(CyberGadget gadget)
        {
            switch (gadget)
            {
                case CyberGadget.Jetpack: return "SKY-ONLY COINS + LANE FLIGHT";
                case CyberGadget.Hoverboard: return "DOUBLE JUMP + ONE-HIT SHIELD";
                case CyberGadget.Robot: return "20s AUTOPILOT + 4s SAFE HANDOVER";
                default: return "RUN";
            }
        }
        public static float Duration(CyberGadget gadget)
        {
            switch (gadget)
            {
                case CyberGadget.Jetpack: return 11f;
                case CyberGadget.Hoverboard: return 17f;
                case CyberGadget.Robot: return 20f;
                default: return 0f;
            }
        }
        public static ThingKind PickupKind(CyberGadget gadget)
        {
            return gadget == CyberGadget.Jetpack ? ThingKind.Jetpack :
                gadget == CyberGadget.Hoverboard ? ThingKind.Hoverboard : ThingKind.Robot;
        }
    }

    // Original, lightweight, geometric science-fiction gadget art.
    // All pickup silhouettes sit ABOVE the track inside a colored circular beacon.
    public static class CyberGadgetArt
    {
        private static void AddBox(Transform p, string name, Vector3 pos, Vector3 size, string color, bool glow = false)
        { RunnerArt.Box(name, p, pos, size, color, glow); }
        private static void AddBall(Transform p, string name, Vector3 pos, Vector3 size, string color, bool glow = false)
        { RunnerArt.Ball(name, p, pos, size, color, glow); }
        private static string Color(CyberGadget gadget)
        { return gadget == CyberGadget.Jetpack ? "#58E7FF" : gadget == CyberGadget.Hoverboard ? "#FFD35B" : "#B3FF81"; }
        public static void MakePickup(Transform parent, CyberGadget gadget)
        {
            string accent = Color(gadget);
            Transform beacon = new GameObject("SAFE COLLECTIBLE / " + CyberGadgetLibrary.Name(gadget)).transform;
            beacon.SetParent(parent, false);
            beacon.localPosition = new Vector3(0, 1.55f, 0);
            // Pickups have a clear circular aura, not the rectangular red/orange warning footprint of hazards.
            RunnerArt.Ring("PICKUP / floating ring", beacon, Vector3.zero, .86f, .075f, accent);
            RunnerArt.Ring("PICKUP / double ring", beacon, new Vector3(0, 0, .02f), .68f, .045f, "#FFFFFF");
            AddBall(beacon, "PICKUP / luminous center", Vector3.zero,
                new Vector3(.37f, .37f, .37f), "#0C2C4A");
            if (gadget == CyberGadget.Jetpack)
            {
                AddBox(beacon,"JET / frame", new Vector3(0, 0, .12f),new Vector3(.53f,.67f,.20f),"#1D4168");
                for (int side = -1; side <= 1; side += 2)
                {
                    AddBox(beacon,"JET / dual thruster",new Vector3(side*.33f, -.03f, .17f),
                        new Vector3(.23f,.60f,.27f),"#E3F7FF");
                    AddBall(beacon,"JET / blue exhaust",new Vector3(side*.33f,-.43f,.17f),
                        new Vector3(.20f,.26f,.20f),accent,true);
                }
            }
            else if (gadget == CyberGadget.Hoverboard)
            {
                AddBox(beacon,"BOARD / floating carbon deck",new Vector3(0,-.04f,.16f),
                    new Vector3(1.14f,.12f,.35f),"#21304D");
                for (int s = -1; s <= 1; s += 2)
                {
                    AddBox(beacon,"BOARD / bright edge",new Vector3(s*.46f,.025f,.18f),
                        new Vector3(.17f,.08f,.41f),accent,true);
                    AddBall(beacon,"BOARD / ion drive",new Vector3(s*.40f,-.15f,.12f),
                        new Vector3(.28f,.16f,.18f),"#49E7FF",true);
                }
            }
            else
            {
                AddBox(beacon,"MECH / head",new Vector3(0,.18f,.18f),
                    new Vector3(.54f,.38f,.42f),"#EBF8FF");
                AddBox(beacon,"MECH / eyes",new Vector3(0,.22f,-.045f),
                    new Vector3(.40f,.11f,.03f),accent,true);
                AddBox(beacon,"MECH / chest",new Vector3(0,-.23f,.20f),
                    new Vector3(.67f,.42f,.48f),"#305F71");
                for (int s=-1;s<=1;s+=2)
                    AddBall(beacon,"MECH / manipulator",new Vector3(s*.42f,-.25f,.20f),
                        new Vector3(.20f,.35f,.23f),accent);
            }
            TextMesh name = new GameObject("PICKUP LABEL / " + CyberGadgetLibrary.Name(gadget))
                .AddComponent<TextMesh>();
            name.transform.SetParent(beacon, false);
            name.transform.localPosition = new Vector3(0,1.05f,0);
            name.transform.localRotation = Quaternion.identity;
            name.text = CyberGadgetLibrary.Name(gadget);
            name.characterSize = .15f;
            name.fontSize = 52;
            name.anchor = TextAnchor.MiddleCenter;
            name.alignment = TextAlignment.Center;
            name.color = RunnerArt.Hex(accent);
            beacon.gameObject.AddComponent<CyberMotion>().Configure(CyberMotion.MotionType.Bob,.14f,2.7f);
        }
        public static void MakeLegacyPickup(Transform parent, ThingKind kind)
        {
            string accent = kind == ThingKind.Shield ? "#65F4FF" :
                kind == ThingKind.Magnet ? "#E7AAFF" :
                kind == ThingKind.Freeze ? "#C1EBFF" : "#FFE27D";
            Transform beacon = new GameObject("SAFE PICKUP / " + kind).transform;
            beacon.SetParent(parent,false);
            beacon.localPosition = new Vector3(0,1.55f,0);
            RunnerArt.Ring("round collectible frame",beacon,Vector3.zero,.70f,.10f,accent);
            AddBall(beacon,"power core",Vector3.zero,new Vector3(.48f,.48f,.37f),accent,true);
            AddBox(beacon,"symbol",new Vector3(0,0,-.27f),new Vector3(.29f,.13f,.07f),"#162844");
            TextMesh label = new GameObject("power label / " + kind).AddComponent<TextMesh>();
            label.transform.SetParent(beacon,false);
            label.transform.localPosition = new Vector3(0,.95f,0);
            label.transform.localRotation=Quaternion.identity;
            label.text = kind.ToString().ToUpperInvariant();
            label.characterSize=.14f; label.fontSize=48;
            label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
            label.color=RunnerArt.Hex(accent);
            beacon.gameObject.AddComponent<CyberMotion>().Configure(CyberMotion.MotionType.Bob,.12f,2.4f);
        }
    }

    // Mounted devices are visual children of the existing player root. No new project, prefabs or setup needed.
    public sealed class CyberGadgetVisual
    {
        private readonly GameObject jet, board, mech;
        private readonly Transform exhaust;
        private readonly Transform mechScanner;
        private readonly Transform mechLeftPod;
        private readonly Transform mechRightPod;
        public CyberGadgetVisual(Transform avatarRoot)
        {
            jet = new GameObject("SKY JET / twin thrusters"); jet.transform.SetParent(avatarRoot,false);
            BuildJetpack(jet.transform);
            board = new GameObject("PHASE BOARD / futuristic ride"); board.transform.SetParent(avatarRoot,false);
            BuildHoverboard(board.transform);
            mech = new GameObject("GUARDIAN / rideable protection mech"); mech.transform.SetParent(avatarRoot,false);
            BuildMech(mech.transform);
            exhaust = jet.transform.Find("Exhaust light");
            mechScanner = mech.transform.Find("GUARDIAN / AI scanner halo");
            mechLeftPod = mech.transform.Find("GUARDIAN / LEFT countermeasure pod");
            mechRightPod = mech.transform.Find("GUARDIAN / RIGHT countermeasure pod");
            Show(CyberGadget.None);
        }
        private static void AddBox(Transform p, string n, Vector3 pos, Vector3 scale, string col, bool glow=false)
        { RunnerArt.Box(n,p,pos,scale,col,glow); }
        private static void AddBall(Transform p, string n, Vector3 pos, Vector3 scale, string col, bool glow=false)
        { RunnerArt.Ball(n,p,pos,scale,col,glow); }
        private void BuildJetpack(Transform t)
        {
            AddBox(t, "JET / fitted back plate", new Vector3(0f, 1.53f, -.36f),
                new Vector3(.68f, .80f, .18f), "#26364C");
            AddBox(t, "JET / brushed alloy centre", new Vector3(0f, 1.58f, -.48f),
                new Vector3(.35f, .56f, .11f), "#A4B8C6");
            AddBall(t, "JET / cyan battery core", new Vector3(0f, 1.47f, -.56f),
                new Vector3(.20f, .25f, .10f), "#6BF5EF", true);
            for (int side = -1; side <= 1; side += 2)
            {
                AddBall(t, "JET / rounded nacelle", new Vector3(side * .38f, 1.33f, -.53f),
                    new Vector3(.35f, .86f, .40f), "#CDDDE5");
                AddBox(t, "JET / nacelle grille", new Vector3(side * .38f, 1.62f, -.76f),
                    new Vector3(.23f, .16f, .06f), "#183648");
                AddBall(t, "JET / plasma nozzle", new Vector3(side * .38f, .86f, -.53f),
                    new Vector3(.29f, .18f, .29f), "#1A2C48");
                AddBall(t, "JET / translucent energy flame", new Vector3(side * .38f, .47f, -.53f),
                    new Vector3(.19f, .65f, .20f), "#33D8FF", true);
                AddBox(t, "JET / shoulder harness", new Vector3(side * .29f, 1.77f, -.19f),
                    new Vector3(.11f, .35f, .28f), "#33455A");
            }
            AddBall(t, "Exhaust light", new Vector3(0f, .34f, -.51f),
                new Vector3(.44f, .42f, .20f), "#A8FCFF", true);
        }

        private void BuildHoverboard(Transform t)
        {
            // Rounded silhouette, turned-up nose and working-looking underside drives.
            AddBall(t, "BOARD / elongated carbon body", new Vector3(0f, -.12f, 0f),
                new Vector3(1.11f, .23f, 2.18f), "#183147");
            AddBox(t, "BOARD / anti slip deck", new Vector3(0f, -.005f, 0f),
                new Vector3(.70f, .025f, 1.44f), "#516476");
            AddBall(t, "BOARD / raised front aerofoil", new Vector3(0f, -.085f, .93f),
                new Vector3(.66f, .12f, .37f), "#D8F0F5");
            AddBall(t, "BOARD / tapered rear bumper", new Vector3(0f, -.10f, -.94f),
                new Vector3(.61f, .14f, .27f), "#D8F0F5");
            for (int side = -1; side <= 1; side += 2)
            {
                AddBox(t, "BOARD / neon lower edge", new Vector3(side * .46f, -.12f, 0f),
                    new Vector3(.07f, .08f, 1.71f), "#4CEFFF", true);
                AddBox(t, "BOARD / foot mount", new Vector3(side * .22f, .027f, -.13f),
                    new Vector3(.30f, .045f, .43f), "#273745");
                for (int front = -1; front <= 1; front += 2)
                {
                    AddBall(t, "BOARD / vector turbine housing",
                        new Vector3(side * .43f, -.30f, front * .64f),
                        new Vector3(.30f, .20f, .39f), "#A8CFD9");
                    AddBall(t, "BOARD / ion thrust core",
                        new Vector3(side * .43f, -.405f, front * .64f),
                        new Vector3(.20f, .075f, .24f), "#58ECFF", true);
                }
            }
            AddBox(t, "BOARD / flight indicator", new Vector3(0f, .021f, .62f),
                new Vector3(.24f, .026f, .11f), "#FFE16A", true);
        }

        private void BuildMech(Transform t)
        {
            AddBox(t, "GUARDIAN / rider saddle", new Vector3(0f, -.36f, 0f),
                new Vector3(1.40f, .19f, 1.46f), "#243F55");
            AddBall(t, "GUARDIAN / rounded front armour", new Vector3(0f, .10f, .56f),
                new Vector3(.88f, .62f, .45f), "#EAF5F8");
            AddBox(t, "GUARDIAN / glowing sensor panel", new Vector3(0f, .18f, .795f),
                new Vector3(.54f, .13f, .055f), "#8CF7FF", true);
            for (int side = -1; side <= 1; side += 2)
            {
                AddBall(t, "GUARDIAN / shock absorber", new Vector3(side * .55f, -.67f, -.13f),
                    new Vector3(.35f, .70f, .37f), "#9FBAC6");
                AddBox(t, "GUARDIAN / foot", new Vector3(side * .55f, -1.0f, .14f),
                    new Vector3(.53f, .18f, .72f), "#243C54");
                AddBox(t, "GUARDIAN / rider handrail", new Vector3(side * .72f, .17f, -.09f),
                    new Vector3(.12f, .74f, .77f), "#CADFE6");
                AddBall(t, "GUARDIAN / scanning pod", new Vector3(side * .68f, .40f, .45f),
                    new Vector3(.30f, .26f, .32f), "#A7FCD3", true);
            }
            // Guardian has recognizable high-mounted scanner and response pods;
            // sensors actively sweep during autonomous steering.
            RunnerArt.Ring("GUARDIAN / AI scanner halo", t,
                new Vector3(0f, .97f, .38f), .52f, .065f, "#89FFD9");
            AddBox(t, "GUARDIAN / LEFT countermeasure pod",
                new Vector3(-.84f,.49f,.18f), new Vector3(.24f,.34f,.64f),"#87CAE0");
            AddBox(t, "GUARDIAN / RIGHT countermeasure pod",
                new Vector3(.84f,.49f,.18f), new Vector3(.24f,.34f,.64f),"#87CAE0");
            AddBox(t, "GUARDIAN / reactor plating", new Vector3(0f, .10f, -.35f),
                new Vector3(.76f, .62f, .21f), "#315771");
            AddBall(t, "GUARDIAN / power node", new Vector3(0f, .15f, -.47f),
                new Vector3(.29f, .29f, .13f), "#91FFA6", true);
        }
        public void Show(CyberGadget power)
        {
            jet.SetActive(power == CyberGadget.Jetpack);
            board.SetActive(power == CyberGadget.Hoverboard);
            mech.SetActive(power == CyberGadget.Robot);
        }
        public void Animate(float t)
        {
            Animate(t, 0f, 0f);
        }

        public void Animate(float t, float altitude, float landingPose)
        {
            if (jet.activeSelf && exhaust != null)
                exhaust.localScale = new Vector3(.32f,
                    .54f + .16f * Mathf.Sin(t * 23f), .22f);
            if (board.activeSelf)
            {
                // Antigravity deck tilts subtly rather than looking glued to the feet.
                board.transform.localPosition = new Vector3(0f,
                    .028f * Mathf.Sin(t * 5f), 0f);
                board.transform.localRotation = Quaternion.Euler(1.6f * Mathf.Sin(t * 4f),
                    0f, 3.0f * Mathf.Sin(t * 3f) * (1f - landingPose));
            }
            if (mech.activeSelf)
            {
                mech.transform.localRotation = Quaternion.Euler(0f, 0f,
                    Mathf.Sin(t * 4f) * 1.5f * (1f - landingPose));
                if (mechScanner != null)
                    mechScanner.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t*2.8f)*24f);
                if (mechLeftPod != null)
                    mechLeftPod.localRotation = Quaternion.Euler(0f, 8f*Mathf.Sin(t*3.0f), -6f);
                if (mechRightPod != null)
                    mechRightPod.localRotation = Quaternion.Euler(0f, -8f*Mathf.Sin(t*3.0f), 6f);
            }
        }
    }
}
