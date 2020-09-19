using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target = null;
    [SerializeField] private Vector3 offset = new Vector3();
    [SerializeField] private float smoothSpeed = 0.125f;

    //Bu scirpt oyun sahnesindeki Camera'ya atanmıştır.
    private void FixedUpdate()
    {
        //Editörden alınan değerlere göre Player'ı takip etmektedir.
        transform.position = Vector3.Lerp(transform.position, (target.position + offset), smoothSpeed);
    }
}