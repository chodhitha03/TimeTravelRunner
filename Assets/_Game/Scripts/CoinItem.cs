using UnityEngine;

namespace TimeTravellerRunner
{
    /// <summary>
    /// Attached to coin pickups in the project.
    /// Handles continuous spinning rotation and trigger collision detection.
    /// </summary>
    [SelectionBase]
    public class CoinItem : MonoBehaviour
    {
        [Header("Coin Settings")]
        [SerializeField] private int coinValue = 1;
        [SerializeField] private float rotationSpeed = 160f;

        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        }

        public int Value => coinValue;
    }
}
