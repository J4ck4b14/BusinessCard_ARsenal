using UnityEngine;
using UnityEngine.Animations;

public class GetCamera : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var rotationConstraint = GetComponent<RotationConstraint>();
        if (rotationConstraint.sourceCount <= 0)
        {
            var source = new ConstraintSource
            {
                sourceTransform = Camera.main.transform,
                weight = 1f
            };
            rotationConstraint.AddSource(source);
        }
    }
}
