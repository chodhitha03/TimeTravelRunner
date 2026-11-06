using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Real Unity Rigidbody explosion debris with gravity, angular momentum and road collisions.
    /// Keeps physics separate from the existing reliable arcade hitbox/lane controller.
    /// </summary>
    public static class CyberPhysicsFx
    {
        public static void Burst(Transform parent, Vector3 position, string color, int count)
        {
            GameObject group = new GameObject("PHYSICS / exploding fragments (auto cleanup)");
            group.transform.SetParent(parent, false);
            group.transform.position = position;
            for (int i = 0; i < count; i++)
            {
                GameObject fragment = RunnerArt.Box("Rigidbody shard", group.transform,
                    Vector3.zero, Vector3.one * Random.Range(.15f, .37f),
                    i % 3 == 0 ? "#fcffff" : color, i % 2 == 0);
                fragment.transform.localPosition = Random.insideUnitSphere * .3f;
                fragment.AddComponent<BoxCollider>();
                Rigidbody body = fragment.AddComponent<Rigidbody>();
                body.mass = .12f;
                body.linearDamping = .15f;
                body.angularDamping = .12f;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.AddForce(new Vector3(Random.Range(-2f, 2f), Random.Range(2f, 5f),
                    Random.Range(-2f, 4f)), ForceMode.Impulse);
                body.AddTorque(Random.insideUnitSphere * 2.2f, ForceMode.Impulse);
            }
            Object.Destroy(group, 3.5f);
        }

        public static void SlideSparks(Transform parent, Vector3 position, string color)
        {
            if (parent == null) return;
            GameObject group = new GameObject("SPARKS / slide friction");
            group.transform.SetParent(parent, false);
            group.transform.position = position;
            for (int i = 0; i < 3; i++)
            {
                GameObject spark = RunnerArt.Box("Spark", group.transform,
                    Vector3.zero, new Vector3(0.04f, 0.04f, Random.Range(0.12f, 0.28f)),
                    color, true);
                spark.transform.localPosition = new Vector3(Random.Range(-0.15f, 0.15f), 0.02f, Random.Range(-0.3f, 0.1f));
                Rigidbody body = spark.AddComponent<Rigidbody>();
                body.mass = 0.05f;
                body.linearDamping = 0.5f;
                body.AddForce(new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(0.5f, 2.5f), -Random.Range(4f, 10f)), ForceMode.Impulse);
            }
            Object.Destroy(group, 0.35f);
        }
    }
}
