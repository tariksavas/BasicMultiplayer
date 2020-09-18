using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target = null;
    [SerializeField] private Vector3 offset = new Vector3();
    [SerializeField] private float smoothSpeed = 0.125f;
    private void FixedUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, (target.position + offset), smoothSpeed);
    }
}