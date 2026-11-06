using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Present-day Earth art for the standalone Demo run. Intentionally contains
    /// no portals, futuristic city geometry, ancient scenery or dinosaurs.
    /// All decorations are generated automatically outside the runner lanes.
    /// </summary>
    public static class DemoEarthBuilder
    {
        private const float L = RunnerWorld.TileLength;

        public static void Build(Transform tile, int index)
        {
            // Daylight town: a normal asphalt street bordered by grass,
            // footpaths, planted trees and ordinary modern buildings.
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                RunnerArt.Box("Green verge", tile,
                    new Vector3(s * 17.5f, -.43f, L * .5f),
                    new Vector3(26f, .74f, L), "#779F63");
                RunnerArt.Box("Concrete sidewalk", tile,
                    new Vector3(s * 6.45f, -.045f, L * .5f),
                    new Vector3(3.4f, .14f, L), "#C9C9C1");
                RunnerArt.Box("Roadside curb", tile,
                    new Vector3(s * 4.82f, .12f, L * .5f),
                    new Vector3(.21f, .25f, L), "#F1EEE5");
                RunnerArt.Box("Garden strip", tile,
                    new Vector3(s * 8.35f, -.04f, L * .5f),
                    new Vector3(.85f, .12f, L), "#5F8A56");

                // Each repeating tile keeps recognizable variation but
                // architectural elements align consistently between tiles.
                for (int i = 0; i < 3; i++)
                {
                    float zz = 4.2f + i * 9.2f;
                    Tree(tile, s * 8.4f, zz, (index + i) % 2 == 0 ? 1.0f : .85f);
                }
                Lamppost(tile, s * 5.48f, 7.6f);
                Lamppost(tile, s * 5.48f, 23f);

                if ((index + (side == 1 ? 1 : 0)) % 3 == 0)
                    Bench(tile, s * 6.9f, 15.2f);

                float buildingX = s * (13.2f + ((index + 3) % 3) * 1.9f);
                MakeBuilding(tile, buildingX, 14f, index + (side + 1) / 2);
                // Small village-like house/shop in the background adds depth.
                House(tile, s * 23f, 19f, index);
            }
            if (index % 4 == 1)
            {
                // Familiar crosswalk stripes, never a portal or sci-fi track.
                for (int i = -3; i <= 3; i++)
                    RunnerArt.Box("Pedestrian-crossing marking", tile,
                        new Vector3(i * 1.22f, .018f, 5.1f),
                        new Vector3(.66f, .023f, 1.35f), "#F4F2EB");
            }
            if (index % 3 == 2)
            {
                RunnerArt.Box("Bus stop roof", tile, new Vector3(7.05f, 3.12f, 15.2f),
                    new Vector3(2.35f, .17f, 4.2f), "#376A83");
                for (int k = -1; k <= 1; k += 2)
                    RunnerArt.Box("Bus stop post", tile,
                        new Vector3(7.05f, 1.54f, 15.2f + k * 1.85f),
                        new Vector3(.14f, 3.08f, .14f), "#555D63");
                RunnerArt.Box("Bus stop sign", tile,
                    new Vector3(7.05f, 2.25f, 15.2f),
                    new Vector3(1.9f, .36f, .1f), "#E8F0E9");
            }
        }

        private static void MakeBuilding(Transform parent, float x, float z, int variant)
        {
            Transform b = new GameObject("Present-day apartment or shop").transform;
            b.SetParent(parent, false);
            b.localPosition = new Vector3(x, 0, z);
            float height = 9f + (variant % 4) * 2.1f;
            float width = 5.8f;
            string body = variant % 3 == 0 ? "#DEDBCD" : variant % 3 == 1 ? "#A7BBC1" : "#C6B2A0";
            RunnerArt.Box("Concrete and brick facade", b,
                new Vector3(0, height / 2, 0), new Vector3(width, height, 7f), body);
            RunnerArt.Box("Ground floor storefront", b,
                new Vector3(0, 1.55f, -3.53f), new Vector3(4.7f, 2.7f, .16f), "#496779");
            RunnerArt.Box("Shop canopy", b,
                new Vector3(0, 3.05f, -3.85f), new Vector3(5.25f, .2f, 1.1f), "#59646C");
            RunnerArt.Box("Front door", b,
                new Vector3(0, 1.15f, -3.64f), new Vector3(1.1f, 2.15f, .1f), "#D7E6E8");
            int floors = Mathf.FloorToInt((height - 3f) / 2.5f);
            for (int floor = 0; floor < floors; floor++)
            {
                float y = 4.2f + floor * 2.45f;
                for (int column = -1; column <= 1; column++)
                {
                    float xx = column * 1.65f;
                    RunnerArt.Box("Window dark frame", b,
                        new Vector3(xx, y, -3.54f),
                        new Vector3(1.23f, 1.55f, .13f), "#606B72");
                    RunnerArt.Box("Daylight glass pane", b,
                        new Vector3(xx, y, -3.63f),
                        new Vector3(.99f, 1.27f, .07f), "#8FAFBD");
                }
            }
            RunnerArt.Box("Roof parapet", b, new Vector3(0, height + .26f, -3.42f),
                new Vector3(width + .3f, .52f, .3f), "#B1B0AA");
            RunnerArt.Box("Flat rooftop structure", b,
                new Vector3(1.2f, height + .49f, .8f), new Vector3(2.1f, .98f, 2.4f), "#8A9493");
        }

        private static void House(Transform tile, float x, float z, int index)
        {
            Transform b = new GameObject("Ordinary distant house").transform;
            b.SetParent(tile, false);
            b.localPosition = new Vector3(x, 0, z);
            RunnerArt.Box("House walls", b,
                new Vector3(0, 2.3f, 0), new Vector3(7f, 4.6f, 7f),
                index % 2 == 0 ? "#E5D5C0" : "#E0E8DE");
            RunnerArt.Box("Roof", b, new Vector3(0, 4.8f, 0),
                new Vector3(7.6f, .6f, 7.5f), "#8B7770");
            RunnerArt.Box("Door", b,
                new Vector3(0, 1.22f, -3.55f), new Vector3(1.25f, 2.4f, .12f), "#715F50");
            for (int col = -1; col <= 1; col += 2)
                RunnerArt.Box("Window", b,
                    new Vector3(col * 2.25f, 2.3f, -3.56f),
                    new Vector3(1.3f, 1.3f, .1f), "#86A6B8");
        }

        private static void Tree(Transform tile, float x, float z, float scale)
        {
            RunnerArt.Cylinder("Street tree trunk", tile,
                new Vector3(x, 1.25f * scale, z),
                new Vector3(.18f * scale, 1.25f * scale, .18f * scale), "#80654C");
            RunnerArt.Ball("Street tree leaves", tile,
                new Vector3(x, 3.15f * scale, z),
                new Vector3(2f * scale, 2.45f * scale, 2f * scale), "#4D8D55");
            RunnerArt.Ball("Tree crown detail", tile,
                new Vector3(x + .43f * scale, 3.7f * scale, z + .26f),
                new Vector3(1.4f * scale, 1.35f * scale, 1.4f * scale), "#66A16A");
        }

        private static void Lamppost(Transform tile, float x, float z)
        {
            RunnerArt.Cylinder("Streetlight steel pole", tile,
                new Vector3(x, 2.35f, z), new Vector3(.092f, 2.35f, .092f), "#657079");
            RunnerArt.Box("Streetlight arm", tile,
                new Vector3(x - Mathf.Sign(x) * .47f, 4.66f, z),
                new Vector3(.95f, .11f, .16f), "#657079");
            RunnerArt.Box("Warm-white lamp", tile,
                new Vector3(x - Mathf.Sign(x) * .82f, 4.58f, z),
                new Vector3(.42f, .09f, .3f), "#F4E6C1");
        }

        private static void Bench(Transform tile, float x, float z)
        {
            RunnerArt.Box("Public park bench seat", tile,
                new Vector3(x, .68f, z), new Vector3(.72f, .14f, 1.95f), "#8C6A4A");
            RunnerArt.Box("Bench back", tile,
                new Vector3(x + Mathf.Sign(x) * .36f, 1.05f, z),
                new Vector3(.1f, .85f, 1.95f), "#8C6A4A");
            for (int i = -1; i <= 1; i += 2)
                RunnerArt.Box("Bench leg", tile, new Vector3(x, .3f, z + i * .75f),
                    new Vector3(.13f, .6f, .12f), "#535B60");
        }

        /// <summary>
        /// Everyday obstacles, aligned with existing lane and jump/slide rules.
        /// Returns half-length in running direction for the scripted collision system.
        /// </summary>
        public static float PopulateObstacle(Transform root, ThingKind kind)
        {
            switch (kind)
            {
                case ThingKind.Jump:
                    // A waist-height orange worksite barrier: clear it by jumping.
                    RunnerArt.Box("Construction barrier frame", root,
                        new Vector3(0, .48f, 0), new Vector3(2.15f, .75f, .26f), "#E37B35");
                    RunnerArt.Box("White caution stripe", root,
                        new Vector3(0, .52f, -.149f), new Vector3(1.9f, .21f, .045f), "#F8F3E8");
                    for (int side = -1; side <= 1; side += 2)
                        RunnerArt.Box("Barrier leg", root,
                            new Vector3(side * .85f, .23f, 0),
                            new Vector3(.16f, .46f, .42f), "#5B6064");
                    return .72f;
                case ThingKind.Slide:
                    // Low clearance sign: slide under the horizontal board.
                    for (int side = -1; side <= 1; side += 2)
                        RunnerArt.Box("Roadwork overhead support", root,
                            new Vector3(side * 1.13f, 1f, 0),
                            new Vector3(.12f, 2f, .28f), "#D6A44F");
                    RunnerArt.Box("LOW CLEARANCE - DUCK", root,
                        new Vector3(0, 1.92f, 0),
                        new Vector3(2.4f, .36f, .37f), "#F0C15A");
                    RunnerArt.Box("Warning marking", root,
                        new Vector3(0, 1.94f, -.20f),
                        new Vector3(1.85f, .11f, .05f), "#3A454B");
                    return .73f;
                case ThingKind.Pit:
                    // Roadworks opening is represented visually; jumping clears it.
                    RunnerArt.Box("Asphalt repair boundary", root,
                        new Vector3(0, .018f, 0),
                        new Vector3(2.35f, .048f, 2.25f), "#D5A955");
                    RunnerArt.Box("Open roadwork pit", root,
                        new Vector3(0, .05f, 0),
                        new Vector3(2.05f, .065f, 1.95f), "#252A2D");
                    for (int side = -1; side <= 1; side += 2)
                        RunnerArt.Box("Roadwork reflective edge", root,
                            new Vector3(side * 1.04f, .094f, 0),
                            new Vector3(.11f, .05f, 2f), "#F3D26C");
                    return 1.05f;
                case ThingKind.Dodge:
                    // Ordinary delivery van — change lanes to avoid it.
                    RunnerArt.Box("City delivery van body", root,
                        new Vector3(0, 1.13f, 0),
                        new Vector3(1.95f, 2.06f, 2.7f), "#E5E9E6");
                    RunnerArt.Box("Van windscreen", root,
                        new Vector3(0, 1.78f, -1.37f),
                        new Vector3(1.45f, .65f, .08f), "#6F9EAE");
                    RunnerArt.Box("Vehicle bumper", root,
                        new Vector3(0, .53f, -1.39f),
                        new Vector3(2.05f, .2f, .16f), "#59656C");
                    for (int side = -1; side <= 1; side += 2)
                        for (int zz = -1; zz <= 1; zz += 2)
                        {
                            GameObject wheel = RunnerArt.Cylinder("Van rubber tire", root,
                                new Vector3(side * 1.01f, .42f, zz * .83f),
                                new Vector3(.35f, .1f, .35f), "#303338");
                            wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
                        }
                    return 1.40f;
                default: return .73f;
            }
        }
    }
}
