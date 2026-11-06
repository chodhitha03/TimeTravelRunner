using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Subway Surfers-style clean daytime atmosphere.
    /// Completely removes blinding speed-lines and harsh particles.
    /// Provides gentle floating summer dust specks and distant suburban city backdrop.
    /// </summary>
    public sealed class CyberAtmosphere : MonoBehaviour
    {
        private ParticleSystem ambientSpecks;
        private GameObject horizonBackdrop;

        public static CyberAtmosphere Create(Transform parent)
        {
            GameObject go = new GameObject("ATMOSPHERE / Subway Daylight Ambience");
            go.transform.SetParent(parent, false);
            CyberAtmosphere atmosphere = go.AddComponent<CyberAtmosphere>();
            atmosphere.Setup();
            return atmosphere;
        }

        private void Setup()
        {
            // 1. Gentle floating sun specks (NO harsh speed lines in the player's face)
            GameObject specksObj = new GameObject("Sunny Specks");
            specksObj.transform.SetParent(transform, false);
            ambientSpecks = specksObj.AddComponent<ParticleSystem>();
            ambientSpecks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var main = ambientSpecks.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 4f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.8f, 0.6f), new Color(1f, 1f, 1f, 0.4f));

            var emission = ambientSpecks.emission;
            emission.enabled = true;
            emission.rateOverTime = 12f;

            var shape = ambientSpecks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 6f, 30f);

            var renderer = specksObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = RunnerArt.Mat("#FFF8D6", false);

            specksObj.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
            ambientSpecks.Play(true);

            // 2. Distant Clean Cartoon City Skyline & Clouds
            CreateSubwayBackdrop();
        }

        private void CreateSubwayBackdrop()
        {
            horizonBackdrop = new GameObject("SUBWAY HORIZON / Distant City & Clouds");
            horizonBackdrop.transform.SetParent(transform, false);

            // Fluffy white cartoon clouds in the blue sky
            for (int c = -6; c <= 6; c += 2)
            {
                float cloudX = c * 24f + (c % 3) * 6f;
                float cloudY = 32f + Mathf.Abs(c * 3 % 8);
                float cloudZ = 160f + Mathf.Abs(c * 11 % 40);

                GameObject cloud = new GameObject("Cloud_" + c);
                cloud.transform.SetParent(horizonBackdrop.transform, false);
                cloud.transform.localPosition = new Vector3(cloudX, cloudY, cloudZ);

                RunnerArt.Ball("Puff1", cloud.transform, Vector3.zero, new Vector3(18f, 7f, 6f), "#FFFFFF");
                RunnerArt.Ball("Puff2", cloud.transform, new Vector3(-6f, -1.2f, 0), new Vector3(12f, 5.5f, 5f), "#F0F8FF");
                RunnerArt.Ball("Puff3", cloud.transform, new Vector3(6f, -1f, 0), new Vector3(14f, 6f, 5f), "#FFFFFF");
            }

            // Distant colorful brick buildings & water towers
            for (int i = -9; i <= 9; i++)
            {
                if (Mathf.Abs(i) <= 1) continue;
                float x = i * 15f + (i > 0 ? 10f : -10f);
                float h = 28f + Mathf.Abs(i * 13 % 26);
                float w = 11f + Mathf.Abs(i * 5 % 6);
                
                string[] bldgColors = { "#D9534F", "#337AB7", "#F0AD4E", "#5CB85C", "#E87A5D", "#C49A7A" };
                string bldgColor = bldgColors[Mathf.Abs(i) % bldgColors.Length];

                GameObject bldg = RunnerArt.Box("Subway Skyline Bldg " + i, horizonBackdrop.transform,
                    new Vector3(x, h * 0.5f - 2f, 175f),
                    new Vector3(w, h, 14f), bldgColor);

                // Rooftop water tower or antenna
                if (Mathf.Abs(i) % 2 == 0)
                {
                    RunnerArt.Cylinder("WaterTower", bldg.transform,
                        new Vector3(0, 0.53f, 0),
                        new Vector3(0.25f, 0.08f, 0.25f), "#8B6543");
                }
            }
        }

        public void Follow(float playerZ)
        {
            transform.position = new Vector3(0, 0, playerZ);
            if (horizonBackdrop != null)
            {
                horizonBackdrop.transform.localPosition = new Vector3(0, 0, 75f);
            }
        }
    }
}
