#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TimeTravellerRunner.Editor
{
    public static class OneClickBuild
    {
        [MenuItem("Time Traveller Runner/Build/Linux Desktop Game")]
        public static void BuildLinux()
        {
            const string scene = "Assets/_Game/Scenes/TimeTravellerRunner.unity";
            if (!File.Exists(scene)) AutoBuildGameScene.OpenGame();
            Directory.CreateDirectory("Builds/Linux");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = "Builds/Linux/TimeTravellerRunner.x86_64",
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None
            };
            BuildPipeline.BuildPlayer(options);
            Debug.Log("Time Traveller Runner: desktop build requested in Builds/Linux.");
        }

        [MenuItem("Time Traveller Runner/Build/Windows Desktop Game")]
        public static void BuildWindows()
        {
            const string scene = "Assets/_Game/Scenes/TimeTravellerRunner.unity";
            if (!File.Exists(scene)) AutoBuildGameScene.OpenGame();
            Directory.CreateDirectory("Builds/Windows");
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = "Builds/Windows/TimeTravellerRunner.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildPipeline.BuildPlayer(options);
            Debug.Log("Time Traveller Runner: Windows build requested in Builds/Windows.");
        }
    }
}
#endif
