using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>Saved, selectable Cyber City avatars. Demo always retains its original player.</summary>
    public static class CyberCharacterCatalog
    {
        public const int Count = 3;
        public static int Selected
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt("TTR_CYBER_CHARACTER", 0), 0, Count - 1); }
            set
            {
                PlayerPrefs.SetInt("TTR_CYBER_CHARACTER", Mathf.Clamp(value, 0, Count - 1));
                PlayerPrefs.Save();
            }
        }
        public static string Name(int index)
        {
            switch (Mathf.Clamp(index, 0, Count - 1))
            {
                case 1: return "RYDER";
                case 2: return "KAI";
                default: return "AXEL";
            }
        }
        public static string Description(int index)
        {
            switch (Mathf.Clamp(index, 0, Count - 1))
            {
                case 1: return "Urban courier  /  ivory & amber";
                case 2: return "Night operative  /  burgundy & blue";
                default: return "Transit runner  /  graphite & teal";
            }
        }
        public static Color Accent(int index)
        {
            switch (Mathf.Clamp(index, 0, Count - 1))
            {
                case 1: return new Color(1f, .71f, .38f);
                case 2: return new Color(.55f, .78f, 1f);
                default: return new Color(.27f, .85f, .79f);
            }
        }
    }
}
