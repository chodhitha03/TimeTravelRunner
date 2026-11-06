
using UnityEngine;

public class CarController : MonoBehaviour
{
    public float speed = 0f;
    public float maxSpeed = 25f;
    public float acceleration = 10f;
    public float brakePower = 20f;
    public float turnSpeed = 80f;

    void Update()
    {
        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        if (move > 0)
        {
            speed += acceleration * Time.deltaTime;
        }
        else if (move < 0)
        {
            speed -= brakePower * Time.deltaTime;
        }
        else
        {
            speed = Mathf.MoveTowards(speed, 0, 5f * Time.deltaTime);
        }

        speed = Mathf.Clamp(speed, -10f, maxSpeed);

        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Mathf.Abs(speed) > 0.1f)
        {
            transform.Rotate(Vector3.up * turn * turnSpeed
                * Time.deltaTime * Mathf.Sign(speed));
        }
    }
}
