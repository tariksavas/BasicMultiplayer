using UnityEngine;
using UnityEngine.UI;
public class SelectRoomManager : MonoBehaviour
{
    public void OnMouseDown()
    {
        RoomManager.roomManagerClass.selectedRoomName = transform.GetChild(0).GetComponent<Text>().text;
    }
}