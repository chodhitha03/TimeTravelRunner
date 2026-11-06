using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Next-Gen Future Cyber City Obstacles:
    /// - Aerodynamic High-Speed Maglev Bullet Trains (hovering, with jumpable rear roof ramps)
    /// - Holographic Jump Barriers with upward arrow chevrons
    /// - Overhead Cyber Scan Gantries (low clearance requiring slide)
    /// - Anti-Gravity Ion Launch Pads
    /// - High-voltage induction hazard pits
    /// </summary>
    public static class CyberObstacleFactory
    {
        private static void Part(Transform p, string name, Vector3 pos, Vector3 size, string material)
        {
            var o = RunnerArt.Box(name, p, pos, size, "#ffffff");
            o.GetComponent<Renderer>().sharedMaterial = CyberSurface.Get(material);
        }

        private static void Cylinder(Transform p, string name, Vector3 pos, Vector3 scale, string material)
        {
            var o = RunnerArt.Cylinder(name, p, pos, scale, "#ffffff");
            o.GetComponent<Renderer>().sharedMaterial = CyberSurface.Get(material);
        }

        public static float Populate(Transform obstacle, ThingKind kind)
        {
            int variety = Mathf.Abs(Mathf.RoundToInt(obstacle.position.z * 0.31f)
                + Mathf.RoundToInt(obstacle.position.x * 0.9f)) % 2;
            float extent;

            if (kind == ThingKind.Dodge)
            {
                if (variety == 0) MaglevBulletTrain(obstacle, "futureWhite", "energyCyan");
                else MaglevBulletTrain(obstacle, "futureNavy", "energyPink");
                extent = 3.5f;
            }
            else if (kind == ThingKind.Jump)
            {
                if (variety == 0) HoloJumpBarrier(obstacle, "energyCyan");
                else HoloJumpBarrier(obstacle, "energyPink");
                extent = 0.65f;
            }
            else if (kind == ThingKind.Slide)
            {
                if (variety == 0) OverheadCyberGantry(obstacle, "energyCyan");
                else OverheadCyberGantry(obstacle, "energyYellow");
                extent = 0.45f;
            }
            else if (kind == ThingKind.Pit)
            {
                MaglevInductionPit(obstacle);
                extent = 1.15f;
            }
            else if (kind == ThingKind.LaunchPad)
            {
                AntiGravLaunchPad(obstacle);
                extent = 1.10f;
            }
            else return 0.73f;

            // The modern 3D maglev trains, holo barriers and overhead gantries speak for themselves.
            // No cluttering floating text billboards ("JUMP", "SWITCH LANE") blocking the camera.
            return extent;
        }

        // --- 1. Aerodynamic Maglev Bullet Train ---
        private static void MaglevBulletTrain(Transform t, string bodyColor, string accentColor)
        {
            float length = 7.0f;
            float width = 2.18f;

            // Hovering chassis / undercarriage (floats at y = 0.35m)
            Part(t, "MAGLEV / undercarriage", new Vector3(0, 0.40f, 0),
                new Vector3(width * 0.88f, 0.30f, length), "darkmetal");

            // Blue ion levitation field underneath
            Part(t, "MAGLEV / ion field", new Vector3(0, 0.15f, 0),
                new Vector3(width * 0.75f, 0.08f, length * 0.92f), "hoverBlue");

            // Main Train Aerodynamic Body
            Part(t, "MAGLEV / main body", new Vector3(0, 1.62f, 0),
                new Vector3(width, 2.05f, length), bodyColor);

            // Sleek Roof Shell
            Part(t, "MAGLEV / roof shell", new Vector3(0, 2.72f, 0),
                new Vector3(width * 0.94f, 0.18f, length + 0.12f), "futureTitanium");

            // Front Aerodynamic Nose Cone
            Part(t, "MAGLEV / nose wedge", new Vector3(0, 1.55f, -length * 0.5f - 0.22f),
                new Vector3(width * 0.95f, 1.85f, 0.45f), bodyColor);

            // Front Cockpit Glass Visor
            Part(t, "MAGLEV / cockpit visor", new Vector3(0, 1.95f, -length * 0.5f - 0.42f),
                new Vector3(width * 0.84f, 0.75f, 0.12f), "glassdark");

            // Front Glow Accent Strip & Bumper
            Part(t, "MAGLEV / front bumper", new Vector3(0, 0.48f, -length * 0.5f - 0.35f),
                new Vector3(width * 0.92f, 0.28f, 0.25f), "futureTitanium");
            Part(t, "MAGLEV / front neon accent", new Vector3(0, 0.62f, -length * 0.5f - 0.46f),
                new Vector3(width * 0.82f, 0.08f, 0.04f), accentColor);

            // High-Tech LED Headlights
            for (int s = -1; s <= 1; s += 2)
            {
                Part(t, "Headlight", new Vector3(s * 0.72f, 0.98f, -length * 0.5f - 0.44f),
                    new Vector3(0.25f, 0.18f, 0.06f), "light");
            }

            // Lateral Neon Speed Strips & Windows along flanks
            for (int side = -1; side <= 1; side += 2)
            {
                float xSide = side * (width * 0.5f + 0.02f);
                // Continuous lateral neon speed stripe
                Part(t, "NeonStripe", new Vector3(xSide, 1.25f, 0),
                    new Vector3(0.04f, 0.08f, length * 0.88f), accentColor);

                // Sleek composite window band
                Part(t, "WindowBand", new Vector3(xSide, 1.88f, 0),
                    new Vector3(0.04f, 0.55f, length * 0.82f), "glassdark");
            }

            // Rear Incline Roof Ramp: allows jumping up and running along train roof!
            Part(t, "MAGLEV / rear incline ramp", new Vector3(0, 1.48f, length * 0.5f + 0.60f),
                new Vector3(width * 0.86f, 0.14f, 1.45f), "futureTitanium");
            Part(t, "MAGLEV / ramp neon guide", new Vector3(0, 1.55f, length * 0.5f + 0.60f),
                new Vector3(0.12f, 0.04f, 1.40f), accentColor);
        }

        // --- 2. Holographic Jump Barrier ---
        private static void HoloJumpBarrier(Transform t, string accentColor)
        {
            // Carbon support pylons on sides
            for (int s = -1; s <= 1; s += 2)
            {
                Part(t, "BARRIER / pylon", new Vector3(s * 0.98f, 0.48f, 0),
                    new Vector3(0.22f, 0.96f, 0.24f), "futureTitanium");
                Part(t, "BARRIER / cap", new Vector3(s * 0.98f, 0.98f, 0),
                    new Vector3(0.26f, 0.06f, 0.28f), accentColor);
            }

            // Lower solid impact block
            Part(t, "BARRIER / base", new Vector3(0, 0.35f, 0),
                new Vector3(2.15f, 0.45f, 0.16f), "darkmetal");

            // Glowing neon crossbar
            Part(t, "BARRIER / neon beam", new Vector3(0, 0.68f, 0),
                new Vector3(2.10f, 0.12f, 0.12f), accentColor);

            // Upward Jump Chevron Holograms (Communicates JUMP clearly)
            for (int i = -2; i <= 2; i++)
            {
                Part(t, "Chevron_" + i, new Vector3(i * 0.38f, 0.90f, 0),
                    new Vector3(0.18f, 0.22f, 0.05f), accentColor);
            }
        }

        // --- 3. Overhead Cyber Scan Gantry (Slide Under) ---
        private static void OverheadCyberGantry(Transform t, string accentColor)
        {
            // Sleek side pillars
            for (int s = -1; s <= 1; s += 2)
            {
                Part(t, "Pillar", new Vector3(s * 1.15f, 1.35f, 0),
                    new Vector3(0.18f, 2.70f, 0.24f), "futureTitanium");
                Part(t, "PillarNeon", new Vector3(s * 1.15f, 1.35f, -0.13f),
                    new Vector3(0.06f, 2.50f, 0.04f), accentColor);
            }

            // Overhead scanner beam at y = 2.15m (Slide room below < 1.15m)
            Part(t, "OverheadBeam", new Vector3(0, 2.20f, 0),
                new Vector3(2.40f, 0.38f, 0.32f), "darkmetal");

            // Overhead display scanner unit
            Part(t, "ScannerUnit", new Vector3(0, 2.45f, -0.10f),
                new Vector3(1.20f, 0.24f, 0.22f), "futureTitanium");

            // Hanging laser scan field / downward hazard teeth
            for (int i = -3; i <= 3; i++)
            {
                Part(t, "LaserEmitter_" + i, new Vector3(i * 0.30f, 1.90f, 0),
                    new Vector3(0.14f, 0.22f, 0.06f), accentColor);
            }

            // Amber / Red status sensor
            Part(t, "SensorLight", new Vector3(0, 2.20f, -0.18f),
                new Vector3(0.24f, 0.14f, 0.06f), "signal");
        }

        // --- 4. Maglev Induction Pit ---
        private static void MaglevInductionPit(Transform t)
        {
            Part(t, "Pit_Frame", new Vector3(0, 0.03f, 0),
                new Vector3(2.24f, 0.06f, 2.20f), "futureTitanium");
            Part(t, "Pit_Trench", new Vector3(0, -0.04f, 0),
                new Vector3(1.95f, 0.06f, 1.95f), "darkmetal");

            // Recessed glowing induction coils
            for (int i = -2; i <= 2; i++)
            {
                Part(t, "Coil_" + i, new Vector3(0, 0.01f, i * 0.38f),
                    new Vector3(1.80f, 0.02f, 0.12f), "energyPink");
            }
        }

        // --- 5. Anti-Gravity Ion Launch Pad ---
        private static void AntiGravLaunchPad(Transform t)
        {
            Part(t, "LaunchPad_Base", new Vector3(0, 0.04f, 0),
                new Vector3(2.10f, 0.08f, 1.90f), "darkmetal");
            Part(t, "LaunchPad_Plate", new Vector3(0, 0.08f, 0),
                new Vector3(1.75f, 0.04f, 1.60f), "futureTitanium");

            // Glowing cyan concentric ion ring
            Part(t, "IonCore", new Vector3(0, 0.11f, 0),
                new Vector3(1.20f, 0.03f, 1.10f), "energyCyan");

            // Floating upward chevron marker
            Part(t, "UpChevron", new Vector3(0, 0.35f, 0),
                new Vector3(0.45f, 0.45f, 0.05f), "energyCyan");
        }
    }
}
