using UnityEngine;

public class TankMotor : MonoBehaviour
{
    public Transform hull;

    public void Thrust(float speed)
    {
        hull.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    public void Rotate(float rotationSpeed)
    {
        hull.Rotate(Vector3.up * Time.deltaTime * 360f / rotationSpeed);
    }
}
