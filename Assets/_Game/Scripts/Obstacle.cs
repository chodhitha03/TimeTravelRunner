using UnityEngine;

namespace TimeTravellerRunner
{
    public enum ObstacleType
    {
        Dodge,      // Requires lane switch (e.g., Maglev Bullet Train)
        Jump,       // Requires jumping over (e.g., Holo Barrier, hurdle)
        Slide,      // Requires ducking/sliding under (e.g., Overhead Gantry)
        Pit,        // Requires jumping across hazard gap
        LaunchPad   // Launches runner high into the air
    }

    /// <summary>
    /// Attached to all obstacle prefabs in the project.
    /// Defines collision bounds, hazard interaction type, and gameplay behaviors.
    /// </summary>
    [SelectionBase]
    public class Obstacle : MonoBehaviour
    {
        [Header("Obstacle Configuration")]
        [Tooltip("The required player action to clear this obstacle.")]
        [SerializeField] private ObstacleType obstacleType = ObstacleType.Dodge;

        [Tooltip("Approximate forward half-length of this obstacle for distance checks.")]
        [SerializeField] private float halfLength = 1.0f;

        [Header("Collision Bounds")]
        [SerializeField] private BoxCollider hitCollider;

        public ObstacleType Type => obstacleType;
        public float HalfLength => halfLength;

        private void Reset()
        {
            hitCollider = GetComponent<BoxCollider>();
            if (hitCollider == null)
            {
                hitCollider = gameObject.AddComponent<BoxCollider>();
            }
        }
    }
}
