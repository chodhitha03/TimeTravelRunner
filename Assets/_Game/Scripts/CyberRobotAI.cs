namespace TimeTravellerRunner
{
    /// <summary>Small pure routing helper for 3-lane robot autopilot.</summary>
    public static class CyberRobotAI
    {
        public static bool IsHazard(ThingKind kind)
        {
            return kind == ThingKind.Jump || kind == ThingKind.Slide ||
                kind == ThingKind.Dodge || kind == ThingKind.Pit;
        }
        public static int BestClearLane(int currentLane, bool[] blocked)
        {
            if (blocked == null || blocked.Length != 3) return currentLane;
            if (!blocked[currentLane]) return currentLane;
            int chosen = currentLane;
            int best = 10;
            for (int i = 0; i < 3; i++)
            {
                if (blocked[i]) continue;
                int distance = System.Math.Abs(i - currentLane);
                if (distance < best) { best = distance; chosen = i; }
            }
            return chosen;
        }
    }
}
