using UnityEngine;
public class ItemScript : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 5;
    private void Update()
    {
        Vector3 currentRotation = transform.eulerAngles;
        Vector3 rotation = new Vector3(currentRotation.x, currentRotation.y, currentRotation.z + 10); 
        gameObject.transform.eulerAngles = Vector3.Lerp(currentRotation, rotation, Time.deltaTime * rotateSpeed);
    }
}