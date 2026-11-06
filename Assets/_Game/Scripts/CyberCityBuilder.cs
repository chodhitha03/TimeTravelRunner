using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Next-Gen Future City Transit Architecture:
    /// - 3-Lane Maglev Hyperloop Track with recessed glowing energy guides
    /// - Translucent holographic guardrails along the track edges
    /// - Sleek aerodynamic skyscrapers, glass towers, and floating holographic billboards
    /// - Overhead suspended hyperloop tubes and skyways spanning the track
    /// - Clear, high-contrast, uncluttered layout engineered for fast, readable gameplay
    /// </summary>
    public static class CyberCityBuilder
    {
        public static bool HighDetail = true;

        private static void AddBlock(CyberMeshBatch mesh, string material, float x, float y, float z,
            float width, float height, float depth)
        {
            mesh.Box(material, new Vector3(x, y, z), new Vector3(width, height, depth));
        }

        public static void Build(Transform tile, int index)
        {
            var blocks = new CyberMeshBatch(tile, "FUTURE CYBER CITY / Maglev Transit Architecture");
            int district = (index / 4) % 4;

            // 1. Sleek Dark Composite Maglev Roadbed
            AddBlock(blocks, "road", 0, -0.10f, 14, 9.2f, 0.14f, 28f);

            // 2. High-Tech Energy Conduits / Lane Divider Strips
            // Separating the 3 gameplay lanes (x = -2.65, 0, +2.65) with crisp, non-blinding guides
            for (int d = 0; d < 7; d++)
            {
                float zPos = 2f + d * 4f;
                // Subtle glowing cyan lane guides (clean & readable, not blinding)
                AddBlock(blocks, "energyCyan", -1.325f, 0.005f, zPos, 0.10f, 0.015f, 2.2f);
                AddBlock(blocks, "energyCyan", 1.325f, 0.005f, zPos, 0.10f, 0.015f, 2.2f);
            }

            // 3. Track Edge Holographic Guardrails & Curbs
            for (int side = -1; side <= 1; side += 2)
            {
                float s = side;
                // Titanium track edge curb
                AddBlock(blocks, "curb", s * 4.65f, 0.10f, 14, 0.30f, 0.22f, 28f);
                // Translucent holographic safety barrier rail
                AddBlock(blocks, "glass", s * 4.65f, 0.38f, 14, 0.08f, 0.36f, 28f);
                // Glowing top safety cap
                AddBlock(blocks, "energyPink", s * 4.65f, 0.58f, 14, 0.10f, 0.05f, 28f);

                // Service walkway & illuminated access strip
                AddBlock(blocks, "walk", s * 6.2f, 0.02f, 14, 2.6f, 0.12f, 28f);
                AddBlock(blocks, "darkmetal", s * 4.95f, 0.04f, 14, 0.12f, 0.08f, 28f);

                // 4. Futuristic City Architecture Flanking the Track
                float frontDist = s * (10.5f + (index % 3) * 1.6f);
                float rearDist = s * (18.5f + (index % 2) * 2.2f);
                float h1 = 16f + ((index * 7 + (side + 1) * 5) % 6) * 3.5f;
                float h2 = 28f + ((index * 11 + (side + 1) * 3) % 7) * 4.2f;

                CyberTower(blocks, frontDist, 14f, 8.2f, h1, index + side, district);
                CyberTower(blocks, rearDist, 14f, 10.5f, h2, index + side + 12, district);

                // Floating Holographic Billboards (Every few blocks)
                if (index % 3 == 1)
                {
                    HoloBillboard(blocks, s * 7.6f, 5.2f, 14f, s);
                }

                // Sleek Street Lighting Luminaire
                if (index % 2 == 0)
                {
                    CyberStreetLuminaire(blocks, s * 4.85f, 8f, s);
                }
            }

            // 5. Overhead Skyway / Elevated Hyperloop Tube crossing the avenue (every 4 tiles)
            if (index % 4 == 2)
            {
                OverheadSkybridge(blocks, 14f);
            }

            blocks.Bake();
        }

        private static void CyberTower(CyberMeshBatch m, float x, float z, float width, float height, int seed, int district)
        {
            float dir = x > 0 ? -1f : 1f;
            float depth = 8.5f;

            // Sleek composite building body
            string bodyColor = (seed % 3 == 0) ? "futureNavy" : (seed % 3 == 1) ? "darkmetal" : "futureTitanium";
            AddBlock(m, bodyColor, x, height * 0.5f, z, width, height, depth);

            // Front glass facade & recessed architectural fins
            float faceX = x + dir * (width * 0.5f + 0.04f);
            AddBlock(m, "glassdark", faceX, height * 0.5f, z, 0.08f, height - 1.5f, depth * 0.88f);

            // Illuminated window floor bands
            int floors = Mathf.Clamp(Mathf.FloorToInt(height / 3.2f), 2, 12);
            for (int f = 1; f < floors; f++)
            {
                float fy = f * 3.2f;
                AddBlock(m, "energyCyan", faceX + dir * 0.03f, fy, z, 0.06f, 0.12f, depth * 0.82f);
            }

            // Rooftop architectural spire / crown
            AddBlock(m, "futureTitanium", x, height + 0.6f, z, width * 0.8f, 1.2f, depth * 0.8f);
            AddBlock(m, "energyPink", x, height + 1.8f, z, 0.35f, 2.5f, 0.35f);
        }

        private static void HoloBillboard(CyberMeshBatch m, float x, float y, float z, float side)
        {
            // Support arm
            AddBlock(m, "darkmetal", x, y * 0.5f, z, 0.22f, y, 0.22f);
            AddBlock(m, "futureTitanium", x - side * 0.6f, y, z, 1.4f, 0.18f, 0.18f);

            // Holographic floating display screen
            string screenColor = (side > 0) ? "neonCyan" : "neonPink";
            AddBlock(m, screenColor, x - side * 1.5f, y + 0.8f, z, 0.05f, 2.2f, 4.6f);
            AddBlock(m, "futureTitanium", x - side * 1.5f, y + 0.8f, z, 0.12f, 2.35f, 0.15f); // bezel
        }

        private static void CyberStreetLuminaire(CyberMeshBatch m, float x, float z, float side)
        {
            // Modern arched light pole
            AddBlock(m, "futureTitanium", x, 2.6f, z, 0.14f, 5.2f, 0.14f);
            AddBlock(m, "futureTitanium", x - side * 0.6f, 5.2f, z, 1.3f, 0.12f, 0.14f);
            // Crisp downward light head
            AddBlock(m, "light", x - side * 1.1f, 5.08f, z, 0.45f, 0.10f, 0.25f);
        }

        private static void OverheadSkybridge(CyberMeshBatch m, float z)
        {
            // Sleek suspended transit tube traversing high above the 3 lanes
            AddBlock(m, "futureTitanium", 0, 6.2f, z, 13.5f, 1.4f, 3.2f);
            AddBlock(m, "glass", 0, 6.2f, z - 1.62f, 12.8f, 0.9f, 0.06f); // viewing window
            AddBlock(m, "glass", 0, 6.2f, z + 1.62f, 12.8f, 0.9f, 0.06f);
            AddBlock(m, "energyCyan", 0, 5.45f, z, 13.2f, 0.08f, 3.1f); // under-glow strip
        }
    }
}
