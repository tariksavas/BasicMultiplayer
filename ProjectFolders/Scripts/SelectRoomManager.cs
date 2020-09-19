using UnityEngine;
using UnityEngine.UI;
public class SelectRoomManager : MonoBehaviour
{
    //Bu script MainMenu sahnesindeki Join room menüsü altında listelenen her bir oda için bu metot geçerlidir.
    public void OnMouseDown()
    {
        //RoomManager classındaki static bir değişkene ilgili odanın ismi atanmaktadır.
        RoomManager.roomManagerClass.selectedRoomName = transform.GetChild(0).GetComponent<Text>().text;
    }
}