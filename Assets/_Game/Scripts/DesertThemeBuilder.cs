using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Ancient Egyptian Desert art for the Desert Theme run mode.
    /// Features rolling golden dunes, monumental Great Pyramids with shining
    /// golden pyramidion capstones, sandstone obelisks, temple colonnades,
    /// lush oasis date palms, and ancient terracotta urns.
    /// All decorations are generated procedurally outside the active runner lanes.
    /// </summary>
    public static class DesertThemeBuilder
    {
        private const float L = RunnerWorld.TileLength; // 28f

        public static void Build(Transform tile, int index)
        {
            // 1. Broad Sand Dunes Terrain (Left & Right)
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;

                // Base sand bed
                RunnerArt.Box("Sand floor", tile,
                    new Vector3(s * 18.5f, -.44f, L * .5f),
                    new Vector3(27f, .76f, L), "#D8AB66");

                // Sandstone Roadside Curb
                RunnerArt.Box("Sandstone curb", tile,
                    new Vector3(s * 4.85f, .12f, L * .5f),
                    new Vector3(.24f, .26f, L), "#8F6438");

                // Ancient Causeway Paved Walkway
                RunnerArt.Box("Temple walkway", tile,
                    new Vector3(s * 5.9f, -.04f, L * .5f),
                    new Vector3(1.85f, .14f, L), "#C49A5F");

                // Rolling Sand Dunes (Layered sculpted mounds)
                float dZ1 = ((index * 7) % 11) + 4f;
                float dZ2 = ((index * 13) % 11) + 16f;
                RunnerArt.Ball("Dune swell near", tile,
                    new Vector3(s * 15.5f, -.05f, dZ1),
                    new Vector3(13f, 2.6f, 16f), "#DEB26D");
                RunnerArt.Ball("Dune swell far", tile,
                    new Vector3(s * 24.0f, .35f, dZ2),
                    new Vector3(17f, 3.8f, 22f), "#C89C54");

                // Oasis Date Palms along the causeway
                if ((index + (side == 1 ? 1 : 0)) % 2 == 0)
                {
                    MakeDatePalm(tile, s * 7.4f, 7.5f, 1.0f);
                    MakeDatePalm(tile, s * 8.6f, 21.0f, 0.85f);
                }
                else
                {
                    MakeDatePalm(tile, s * 7.8f, 14.0f, 0.95f);
                }

                // Terracotta Amphoras beside palms
                if (index % 3 == 0)
                {
                    MakeAmphora(tile, s * 6.5f, 8.5f);
                    if (index % 2 == 0) MakeAmphora(tile, s * 6.3f, 9.2f, 0.75f);
                }
            }

            // 2. Monumental Great Pyramids in the Far Desert
            if (index % 3 == 0)
            {
                // Grand Great Pyramid with a gleaming golden pyramidion capstone on the right
                MakeGreatPyramid(tile, 27f, 14f, 15f, 17f, "#D4AB6B", "#FFD54F");
            }
            else if (index % 3 == 1)
            {
                // Twin satellite pyramids on the left
                MakeGreatPyramid(tile, -23f, 9f, 9.5f, 10.5f, "#CDA362", "#FFDF79");
                MakeGreatPyramid(tile, -27f, 22f, 7.5f, 8.5f, "#C49855", null);
            }
            else
            {
                // Distant horizon monument on the left
                MakeGreatPyramid(tile, -31f, 14f, 18f, 20f, "#DEB475", "#FFD54F");
            }

            // 3. Ancient Egyptian Architecture (Alternating features)
            if (index % 2 == 0)
            {
                // Royal Obelisks flanking the causeway
                MakeObelisk(tile, -6.8f, 14f);
                MakeObelisk(tile, 6.8f, 14f);
            }
            else
            {
                // Sandstone Temple Colonnade (Columns with lotus capitals & horizontal lintel beam)
                MakeColonnade(tile, -6.9f);
                MakeColonnade(tile, 6.9f);
            }

            // 4. Ancient Sphinx / Pharaoh Statue Pedestals
            if (index % 4 == 2)
            {
                MakeSphinxMonument(tile, -8.2f, 20f);
                MakeSphinxMonument(tile, 8.2f, 20f);
            }
        }

        private static void MakeGreatPyramid(Transform tile, float x, float z, float baseSize, float height, string sandHex, string capHex)
        {
            Transform pRoot = new GameObject("Great Pyramid of the Desert").transform;
            pRoot.SetParent(tile, false);
            pRoot.localPosition = new Vector3(x, -.15f, z);

            // Main Pyramid Body
            RunnerArt.Pyramid("Pyramid body", pRoot, Vector3.zero, baseSize, height, sandHex);

            // Gleaming Golden Pyramidion Capstone (if specified)
            if (!string.IsNullOrEmpty(capHex))
            {
                float capRatio = 0.22f;
                float capBase = baseSize * capRatio;
                float capHeight = height * capRatio;
                float capY = height * (1f - capRatio);
                RunnerArt.Pyramid("Golden Pyramidion", pRoot, new Vector3(0, capY, 0), capBase, capHeight, capHex);
            }
        }

        private static void MakeObelisk(Transform tile, float x, float z)
        {
            Transform ob = new GameObject("Carved Ancient Obelisk").transform;
            ob.SetParent(tile, false);
            ob.localPosition = new Vector3(x, 0, z);

            // Two-tier Sandstone Pedestal
            RunnerArt.Box("Pedestal Base", ob, new Vector3(0, .25f, 0), new Vector3(1.8f, .5f, 1.8f), "#8B5F33");
            RunnerArt.Box("Pedestal Tier 2", ob, new Vector3(0, .65f, 0), new Vector3(1.4f, .35f, 1.4f), "#A27341");

            // Tapered Main Shaft
            RunnerArt.Box("Shaft Bottom", ob, new Vector3(0, 2.2f, 0), new Vector3(1.05f, 2.8f, 1.05f), "#C5995F");
            RunnerArt.Box("Shaft Mid", ob, new Vector3(0, 4.4f, 0), new Vector3(.90f, 2.4f, .90f), "#C5995F");
            RunnerArt.Box("Shaft Top", ob, new Vector3(0, 6.0f, 0), new Vector3(.76f, 1.8f, .76f), "#C5995F");

            // Golden Sun Inscription Bands
            RunnerArt.Box("Hieroglyph Gold Band 1", ob, new Vector3(0, 3.4f, 0), new Vector3(1.08f, .18f, 1.08f), "#FFD54F");
            RunnerArt.Box("Hieroglyph Gold Band 2", ob, new Vector3(0, 5.2f, 0), new Vector3(.94f, .18f, .94f), "#FFD54F");

            // Golden Pyramidion Point at the Summit
            RunnerArt.Pyramid("Obelisk Gold Tip", ob, new Vector3(0, 6.9f, 0), .50f, 1.25f, "#FFD54F");
        }

        private static void MakeColonnade(Transform tile, float x)
        {
            Transform col = new GameObject("Temple Ruin Colonnade").transform;
            col.SetParent(tile, false);
            col.localPosition = new Vector3(x, 0, 0);

            // Three sandstone pillars with lotus capitals
            for (int i = 0; i < 3; i++)
            {
                float z = 5f + i * 9f;
                // Column Base
                RunnerArt.Cylinder("Column Base", col, new Vector3(0, .3f, z), new Vector3(.58f, .3f, .58f), "#8B5F33");
                // Fluted Shaft
                RunnerArt.Cylinder("Column Shaft", col, new Vector3(0, 2.3f, z), new Vector3(.46f, 2.0f, .46f), "#B88A52");
                // Carved Lotus Capital
                RunnerArt.Cylinder("Lotus Capital", col, new Vector3(0, 4.25f, z), new Vector3(.66f, .35f, .66f), "#D5AD74");
                RunnerArt.Box("Capital Abacus", col, new Vector3(0, 4.6f, z), new Vector3(1.3f, .35f, 1.3f), "#ECC990");
            }

            // Horizontal Temple Architrave (Lintel beam spanning the colonnade)
            RunnerArt.Box("Architrave Beam", col, new Vector3(0, 5.0f, 14f), new Vector3(1.1f, .55f, 22f), "#A27542");
        }

        private static void MakeDatePalm(Transform tile, float x, float z, float scale)
        {
            Transform palm = new GameObject("Egyptian Date Palm").transform;
            palm.SetParent(tile, false);
            palm.localPosition = new Vector3(x, 0, z);
            palm.localScale = Vector3.one * scale;

            // Curved textured trunk segments
            float lean = (x < 0 ? -1f : 1f) * .45f;
            RunnerArt.Cylinder("Trunk Base", palm, new Vector3(0, 1.2f, 0), new Vector3(.30f, 1.2f, .30f), "#7D5230");
            RunnerArt.Cylinder("Trunk Mid", palm, new Vector3(lean * .4f, 3.2f, .2f), new Vector3(.26f, 1.1f, .26f), "#8F5E38");
            RunnerArt.Cylinder("Trunk Top", palm, new Vector3(lean, 5.1f, .4f), new Vector3(.22f, 1.0f, .22f), "#7D5230");

            Vector3 crown = new Vector3(lean * 1.15f, 6.0f, .5f);

            // Golden/Amber ripe date clusters under fronds
            RunnerArt.Ball("Dates L", palm, crown + new Vector3(-.35f, -.45f, 0), new Vector3(.45f, .55f, .45f), "#E68A2E");
            RunnerArt.Ball("Dates R", palm, crown + new Vector3(.35f, -.45f, 0), new Vector3(.45f, .55f, .45f), "#D97820");

            // Spreading Palm Fronds (8 outwards curving fronds)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 frondDir = new Vector3(Mathf.Cos(angle), -.18f, Mathf.Sin(angle));
                GameObject leaf = RunnerArt.Ball("Frond " + i, palm,
                    crown + frondDir * 1.55f,
                    new Vector3(.65f, .12f, 3.2f),
                    i % 2 == 0 ? "#4E8D50" : "#326630");
                leaf.transform.localRotation = Quaternion.Euler(18f, -angle * Mathf.Rad2Deg, 0);
            }
        }

        private static void MakeAmphora(Transform tile, float x, float z, float scale = 1.0f)
        {
            Transform jar = new GameObject("Terracotta Amphora").transform;
            jar.SetParent(tile, false);
            jar.localPosition = new Vector3(x, 0, z);
            jar.localScale = Vector3.one * scale;

            // Bulbous Clay Body
            RunnerArt.Ball("Clay Body", jar, new Vector3(0, .55f, 0), new Vector3(.70f, .85f, .70f), "#BA5C30");
            // Tapered Neck & Rim
            RunnerArt.Cylinder("Jar Neck", jar, new Vector3(0, 1.05f, 0), new Vector3(.28f, .26f, .28f), "#C96D40");
            RunnerArt.Cylinder("Jar Lip", jar, new Vector3(0, 1.25f, 0), new Vector3(.38f, .10f, .38f), "#E49666");

            // Painted Egyptian Turquoise & Gold Bands
            RunnerArt.Cylinder("Turquoise Band", jar, new Vector3(0, .65f, 0), new Vector3(.72f, .08f, .72f), "#26A69A");
            RunnerArt.Cylinder("Gold Band", jar, new Vector3(0, .80f, 0), new Vector3(.58f, .07f, .58f), "#FFD54F");
        }

        private static void MakeSphinxMonument(Transform tile, float x, float z)
        {
            Transform sph = new GameObject("Sphinx Plinth Monument").transform;
            sph.SetParent(tile, false);
            sph.localPosition = new Vector3(x, 0, z);

            // Multi-tiered Sandstone Plinth
            RunnerArt.Box("Base Plinth", sph, new Vector3(0, .3f, 0), new Vector3(2.4f, .6f, 3.8f), "#7D522B");
            RunnerArt.Box("Upper Plinth", sph, new Vector3(0, .8f, 0), new Vector3(1.9f, .5f, 3.2f), "#9C6B3D");

            // Recumbent Lion / Guardian Body
            RunnerArt.Box("Lion Body", sph, new Vector3(0, 1.45f, -.2f), new Vector3(1.3f, .95f, 2.2f), "#BD8B56");
            // Front Paws
            RunnerArt.Box("Paw L", sph, new Vector3(-.42f, 1.15f, 1.15f), new Vector3(.38f, .35f, 1.0f), "#A77744");
            RunnerArt.Box("Paw R", sph, new Vector3(.42f, 1.15f, 1.15f), new Vector3(.38f, .35f, 1.0f), "#A77744");

            // Pharaonic Head with Nemes Headdress
            RunnerArt.Ball("Pharaoh Head", sph, new Vector3(0, 2.3f, .7f), new Vector3(.75f, .85f, .75f), "#D5A56F");
            // Nemes Blue & Gold Headdress Flair
            RunnerArt.Box("Nemes Headdress", sph, new Vector3(0, 2.45f, .6f), new Vector3(1.25f, .75f, .55f), "#FFD54F");
            RunnerArt.Box("Nemes Lapis Stripe", sph, new Vector3(0, 2.55f, .6f), new Vector3(1.30f, .16f, .58f), "#1565C0");
        }
    }
}
