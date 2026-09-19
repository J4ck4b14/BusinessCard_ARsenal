using UnityEngine;

public class TankMotor : MonoBehaviour
{

    public void Thrust(float speed)
    {
        gameObject.transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    public void Rotate(float rotationSpeed)
    {
        gameObject.transform.Rotate(Vector3.up * Time.deltaTime * 360f / rotationSpeed);
    }
}
