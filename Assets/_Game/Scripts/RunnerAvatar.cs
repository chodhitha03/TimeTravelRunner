using UnityEngine;

namespace TimeTravellerRunner
{
    public sealed class RunnerAvatar
    {
        public Transform Root { get; private set; }
        private readonly Transform modelRoot;
        private readonly Transform leftArm, rightArm, leftLeg, rightLeg;
        private readonly GameObject shield;
        private readonly GameObject cyberSuit;

        // Renderers for outfit customization
        private readonly Renderer bodyRenderer;
        private readonly Renderer stripeRenderer;
        private readonly Renderer sleeveLRenderer, sleeveRRenderer;
        private readonly Renderer pantsLRenderer, pantsRRenderer;
        private readonly Renderer shoeLRenderer, shoeRRenderer;
        private readonly Renderer visorRenderer;
        private readonly Renderer chestCoreRenderer;
        private readonly Renderer jetpackLRenderer, jetpackRRenderer;
        private readonly Renderer reactorLRenderer, reactorRRenderer;

        private float motionClock;
        private float laneLean;
        private float previousX;
        private float currentPitch;
        private float currentScaleY = 1f;
        private float sparkTimer;
        private bool cyberEnabled;
        private int selectedCyberCharacter;
        private float selectedCyberScale = 1.0f;

        public RunnerAvatar(Transform parent, int cyberCharacter = 0)
        {
            Root = new GameObject("PLAYER - 3D Adventurer").transform;
            Root.SetParent(parent, false);

            modelRoot = new GameObject("ModelRoot").transform;
            modelRoot.SetParent(Root, false);

            // 1. Torso & Upper Body
            bodyRenderer = RunnerArt.Ball("Body / blue hoodie", modelRoot, new Vector3(0, 1.35f, 0),
                new Vector3(.95f, 1.15f, .57f), "#1b83d0").GetComponent<Renderer>();

            stripeRenderer = RunnerArt.Box("Front jacket stripe", modelRoot, new Vector3(0, 1.35f, .29f),
                new Vector3(.13f, .9f, .05f), "#f0ac54").GetComponent<Renderer>();

            RunnerArt.Ball("Neck", modelRoot, new Vector3(0, 1.98f, .04f),
                new Vector3(.29f, .27f, .29f), "#c58a60");

            RunnerArt.Ball("Head", modelRoot, new Vector3(0, 2.29f, .05f),
                new Vector3(.72f, .74f, .68f), "#cf9a6d");

            RunnerArt.Ball("Hair cap", modelRoot, new Vector3(0, 2.61f, -.02f),
                new Vector3(.76f, .31f, .76f), "#3b2830");

            for (int i = 0; i < 4; i++)
            {
                RunnerArt.Ball("Hair tuft", modelRoot, new Vector3((i - 1.5f) * .19f, 2.74f + (i % 2) * .07f, .1f),
                    new Vector3(.26f, .22f, .38f), "#30212b");
            }

            // 2. Backpack
            RunnerArt.Box("Backpack", modelRoot, new Vector3(0, 1.34f, -.41f),
                new Vector3(.8f, .96f, .36f), "#c77a39");
            RunnerArt.Box("Backpack pocket", modelRoot, new Vector3(0, 1.14f, -.63f),
                new Vector3(.64f, .34f, .12f), "#e0a35e");

            // Backpack straps and limbs
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side * .38f;
                RunnerArt.Box("Backpack strap", modelRoot, new Vector3(x, 1.34f, -.2f),
                    new Vector3(.12f, .92f, .17f), "#533828");

                // Arms
                Transform arm = new GameObject(side < 0 ? "LEFT ARM" : "RIGHT ARM").transform;
                arm.SetParent(modelRoot, false);
                arm.localPosition = new Vector3(side * .53f, 1.77f, 0);

                var sleeve = RunnerArt.Ball("Sleeve", arm, new Vector3(side * .04f, -.29f, 0),
                    new Vector3(.31f, .64f, .35f), "#125d9e").GetComponent<Renderer>();
                RunnerArt.Ball("Hand", arm, new Vector3(side * .04f, -.68f, .02f),
                    new Vector3(.24f, .27f, .28f), "#cf9a6d");

                // Legs
                Transform leg = new GameObject(side < 0 ? "LEFT LEG" : "RIGHT LEG").transform;
                leg.SetParent(modelRoot, false);
                leg.localPosition = new Vector3(side * .24f, .91f, 0);

                var pants = RunnerArt.Ball("Pants", leg, new Vector3(0, -.28f, 0),
                    new Vector3(.36f, .7f, .42f), "#233d60").GetComponent<Renderer>();
                var sneaker = RunnerArt.Box("Sneaker", leg, new Vector3(0, -.65f, .1f),
                    new Vector3(.38f, .24f, .67f), side < 0 ? "#f5a14a" : "#f7f5e9").GetComponent<Renderer>();

                if (side < 0)
                {
                    leftArm = arm; leftLeg = leg;
                    sleeveLRenderer = sleeve; pantsLRenderer = pants; shoeLRenderer = sneaker;
                }
                else
                {
                    rightArm = arm; rightLeg = leg;
                    sleeveRRenderer = sleeve; pantsRRenderer = pants; shoeRRenderer = sneaker;
                }
            }

            // Shield bubble
            shield = RunnerArt.Ring("Shield indicator", Root, new Vector3(0, 1.3f, 0), 1.36f, .085f,
                "#50e9ff", true);
            shield.transform.localRotation = Quaternion.Euler(0, 0, 0);
            shield.SetActive(false);

            // 3. Cyber Suit Accessories (high-tech overlays for Cyber City)
            cyberSuit = new GameObject("Cyber suit / visor / boosters / emissive trim");
            cyberSuit.transform.SetParent(modelRoot, false);

            visorRenderer = RunnerArt.Box("AR visor / cyan", cyberSuit.transform, new Vector3(0, 2.33f, .38f),
                new Vector3(.76f, .23f, .12f), "#00E5FF", true).GetComponent<Renderer>();

            chestCoreRenderer = RunnerArt.Box("Chest core", cyberSuit.transform, new Vector3(0, 1.48f, .35f),
                new Vector3(.30f, .26f, .08f), "#00E5FF", true).GetComponent<Renderer>();

            RunnerArt.Box("Suit shoulder L", cyberSuit.transform, new Vector3(-.60f, 1.83f, 0),
                new Vector3(.32f, .22f, .45f), "#334155");
            RunnerArt.Box("Suit shoulder R", cyberSuit.transform, new Vector3(.60f, 1.83f, 0),
                new Vector3(.32f, .22f, .45f), "#334155");

            jetpackLRenderer = RunnerArt.Box("Jetpack left", cyberSuit.transform, new Vector3(-.24f, 1.42f, -.66f),
                new Vector3(.28f, .60f, .24f), "#1E293B").GetComponent<Renderer>();
            jetpackRRenderer = RunnerArt.Box("Jetpack right", cyberSuit.transform, new Vector3(.24f, 1.42f, -.66f),
                new Vector3(.28f, .60f, .24f), "#1E293B").GetComponent<Renderer>();

            reactorLRenderer = RunnerArt.Ball("Reactor left", cyberSuit.transform, new Vector3(-.24f, 1.12f, -.80f),
                new Vector3(.18f, .16f, .14f), "#00E5FF", true).GetComponent<Renderer>();
            reactorRRenderer = RunnerArt.Ball("Reactor right", cyberSuit.transform, new Vector3(.24f, 1.12f, -.80f),
                new Vector3(.18f, .16f, .14f), "#00E5FF", true).GetComponent<Renderer>();

            cyberSuit.SetActive(false);

            selectedCyberCharacter = cyberCharacter;
            ApplyPalette();
        }

        private void ApplyPalette()
        {
            if (!cyberEnabled)
            {
                // Classic Earth Demo Outfit (Jake / Subway Surfers style)
                if (bodyRenderer != null) bodyRenderer.sharedMaterial = RunnerArt.Mat("#1b83d0");
                if (stripeRenderer != null) stripeRenderer.sharedMaterial = RunnerArt.Mat("#f0ac54");
                if (sleeveLRenderer != null) sleeveLRenderer.sharedMaterial = RunnerArt.Mat("#125d9e");
                if (sleeveRRenderer != null) sleeveRRenderer.sharedMaterial = RunnerArt.Mat("#125d9e");
                if (pantsLRenderer != null) pantsLRenderer.sharedMaterial = RunnerArt.Mat("#233d60");
                if (pantsRRenderer != null) pantsRRenderer.sharedMaterial = RunnerArt.Mat("#233d60");
                if (shoeLRenderer != null) shoeLRenderer.sharedMaterial = RunnerArt.Mat("#f5a14a");
                if (shoeRRenderer != null) shoeRRenderer.sharedMaterial = RunnerArt.Mat("#f7f5e9");
                return;
            }

            // Cyber City Themed Stylized Outfits (Axel, Ryder, Kai)
            string jacketHex, stripeHex, pantsHex, shoeHex, accentHex;
            if (selectedCyberCharacter == 1) // RYDER (Urban Courier - Solar Amber & Ivory)
            {
                jacketHex = "#E65100";
                stripeHex = "#FFF3E0";
                pantsHex = "#334155";
                shoeHex = "#FFB300";
                accentHex = "#FFD600";
            }
            else if (selectedCyberCharacter == 2) // KAI (Night Operative - Stealth Slate & Emerald)
            {
                jacketHex = "#263238";
                stripeHex = "#00E676";
                pantsHex = "#0F172A";
                shoeHex = "#00E676";
                accentHex = "#00E676";
            }
            else // AXEL (Transit Runner - Cobalt Blue & Electric Cyan)
            {
                jacketHex = "#1565C0";
                stripeHex = "#E2E8F0";
                pantsHex = "#1E293B";
                shoeHex = "#00E5FF";
                accentHex = "#00E5FF";
            }

            if (bodyRenderer != null) bodyRenderer.sharedMaterial = RunnerArt.Mat(jacketHex);
            if (stripeRenderer != null) stripeRenderer.sharedMaterial = RunnerArt.Mat(stripeHex);
            if (sleeveLRenderer != null) sleeveLRenderer.sharedMaterial = RunnerArt.Mat(jacketHex);
            if (sleeveRRenderer != null) sleeveRRenderer.sharedMaterial = RunnerArt.Mat(jacketHex);
            if (pantsLRenderer != null) pantsLRenderer.sharedMaterial = RunnerArt.Mat(pantsHex);
            if (pantsRRenderer != null) pantsRRenderer.sharedMaterial = RunnerArt.Mat(pantsHex);
            if (shoeLRenderer != null) shoeLRenderer.sharedMaterial = RunnerArt.Mat(shoeHex);
            if (shoeRRenderer != null) shoeRRenderer.sharedMaterial = RunnerArt.Mat(shoeHex);

            if (visorRenderer != null) visorRenderer.sharedMaterial = RunnerArt.Mat(accentHex, true);
            if (chestCoreRenderer != null) chestCoreRenderer.sharedMaterial = RunnerArt.Mat(accentHex, true);
            if (reactorLRenderer != null) reactorLRenderer.sharedMaterial = RunnerArt.Mat(accentHex, true);
            if (reactorRRenderer != null) reactorRRenderer.sharedMaterial = RunnerArt.Mat(accentHex, true);
        }

        public void SetCyberCharacter(int selection)
        {
            selectedCyberCharacter = Mathf.Clamp(selection, 0, CyberCharacterCatalog.Count - 1);
            ApplyPalette();
        }

        public void SetCyberModelScale(float size)
        {
            selectedCyberScale = Mathf.Clamp(size, .50f, 1.40f);
            if (modelRoot != null)
                modelRoot.localScale = new Vector3(selectedCyberScale, selectedCyberScale * currentScaleY, selectedCyberScale);
        }

        public void EnableCyberSuit(bool enabled)
        {
            cyberEnabled = enabled;
            if (cyberSuit != null) cyberSuit.SetActive(enabled);
            ApplyPalette();
        }

        public void Pose(float x, float z, float jumpHeight, bool sliding, bool moving, bool shieldActive,
            float dt, float rideLift = 0f, float landingPose = 0f, bool fallingFromRide = false)
        {
            Pose(x, z, jumpHeight, 0f, sliding, moving, shieldActive, dt, rideLift, landingPose, fallingFromRide);
        }

        public void Pose(float x, float z, float jumpHeight, float velocityY, bool sliding, bool moving, bool shieldActive,
            float dt, float rideLift = 0f, float landingPose = 0f, bool fallingFromRide = false)
        {
            Root.localPosition = new Vector3(x, jumpHeight, z);

            if (moving) motionClock += dt * 13f;
            float swing = moving ? Mathf.Sin(motionClock) * .62f : 0;
            leftLeg.localRotation = Quaternion.Euler(swing * Mathf.Rad2Deg, 0, 0);
            rightLeg.localRotation = Quaternion.Euler(-swing * Mathf.Rad2Deg, 0, 0);
            leftArm.localRotation = Quaternion.Euler(-swing * 42f, 0, 0);
            rightArm.localRotation = Quaternion.Euler(swing * 42f, 0, 0);

            float sidewaysVelocity = dt > .0001f ? (x - previousX) / dt : 0f;
            previousX = x;

            // Smooth athletic lane lean
            float targetLean = Mathf.Clamp(-sidewaysVelocity * 1.6f, -14f, 14f);
            laneLean = Mathf.Lerp(laneLean, targetLean, dt * 9f);

            // Responsive slide crouch and pitch
            float targetPitch = sliding ? -22f : 0f;
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, dt * 14f);
            modelRoot.localRotation = Quaternion.Euler(currentPitch, 0f, laneLean);

            // Vertical scale compression during slide clears low gantries effortlessly
            float targetScaleY = sliding ? 0.65f : 1f;
            currentScaleY = Mathf.Lerp(currentScaleY, targetScaleY, dt * 14f);
            modelRoot.localScale = new Vector3(selectedCyberScale, selectedCyberScale * currentScaleY, selectedCyberScale);

            // Slide ground friction sparks
            if (sliding && moving && dt > 0.0001f)
            {
                sparkTimer += dt;
                if (sparkTimer >= 0.07f)
                {
                    sparkTimer = 0f;
                    Vector3 sparkPos = Root.position + new Vector3(0f, 0.08f, -0.15f);
                    CyberPhysicsFx.SlideSparks(Root.parent != null ? Root.parent : Root, sparkPos, "#FFD600");
                }
            }
            else
            {
                sparkTimer = 0f;
            }

            shield.SetActive(shieldActive);
            if (shieldActive) shield.transform.Rotate(0, 75f * dt, 0, Space.Self);
        }
    }
}
