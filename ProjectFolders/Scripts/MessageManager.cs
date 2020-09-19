using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Unity.Editor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MessageManager : MonoBehaviour
{
    [SerializeField] private int messageLimit = 20;
    [SerializeField] private float refreshChatDelay = 0.1f;

    [SerializeField] private GameObject chatPanel = null;
    [SerializeField] private GameObject textObject = null;
    [SerializeField] private InputField chatInput = null;

    private DatabaseReference roomsRef;

    private float refreshChatTimer = 0;
    private bool refreshedChat = false;

    public static bool leftRoom = false;

    //Bu script MainMenu sahnesindeki ScriptObject 'e atanmıştır.
    private void Start()
    {
        //Database linki düzenlenir ve "Rooms" ağacı referans alınır.
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");
    }
    private void FixedUpdate()
    {
        if (RoomManager.roomManagerClass.inRoom)
        {
            //Eğer ki oyuncu odada ise bir değişiklik olup olmadığı listener vasıtasıyla dinlenir.
            roomsRef.ChildAdded += HandleChatChanged;
            roomsRef.ChildChanged += HandleChatChanged;
            roomsRef.ChildRemoved += HandleChatChanged;
            roomsRef.ChildMoved += HandleChatChanged;
        }
        if (!RoomManager.roomManagerClass.inRoom && leftRoom)
        {
            //Oyuncu odadan ayrıldıysa messages bölümü temizlenir.
            leftRoom = false;
            ClearChat();
        }
        if (refreshedChat)
        {
            //Odada bir değişiklik varsa belirli bir delay sonra chat güncellenir.
            refreshChatTimer += Time.deltaTime;
            if(refreshChatTimer >= refreshChatDelay)
            {
                refreshChatTimer = 0;
                refreshedChat = false;
                GetMessagesFromDatabase();
            }
        }
    }
    private void HandleChatChanged(object sender, ChildChangedEventArgs args)
    {
        refreshedChat = true;
    }
    private void GetMessagesFromDatabase()
    {
        //Mesajlar Database'den çekilir.
        roomsRef.Child(RoomManager.roomManagerClass.roomName).Child("Messages").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                //"Messages" ağacının altındaki liste ilgili metoda gönderilir.
                Messages message = JsonUtility.FromJson<Messages>(snapshot.GetRawJsonValue());
                SendMessagesToChat(message);
            }
        });
    }
    private void ClearChat()
    {
        //Messages kısmı arayüzde temizlenir.
        for (int i = 0; i < chatPanel.transform.childCount; i++)
            Destroy(chatPanel.transform.GetChild(i).gameObject);
    }
    private void SendMessagesToChat(Messages message)
    {
        //Referans alınan nesne ve bu nesneye ait lis değişkeni tek tek arayüzdeki messages kısmına yazdırılır.
        ClearChat();
        for (int i = 0; i < message.messageList.Count; i++)
        {
            GameObject textObjectClone = Instantiate(textObject, chatPanel.transform);
            textObjectClone.GetComponent<Text>().text = message.messageList[i];
        }
    }
    public void SendButton()
    {
        //Messages bölümündeki "Send" butonuna tanımlanmıştır.
        if (chatInput.text == null || chatInput.text == "")
            return;

        SendMessageToDatabes(chatInput.text);
        chatInput.text = "";
    }
    public void SendMessageToDatabes(string text)
    {
        //Referans alınan texte, cihazdaki aktif oyuncunun nicki eklenerek Database'e eklenir.
        roomsRef.Child(RoomManager.roomManagerClass.roomName).Child("Messages").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            Messages message = new Messages();
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
                message = JsonUtility.FromJson<Messages>(snapshot.GetRawJsonValue());

            text = DatabaseManager.nick + ": " + text;
            message.messageList.Add(text);

            //Database'deki mesaj sayısı limiti aştıysa ilk gönderilen mesaj listeden silinir.
            if (message.messageList.Count >= messageLimit)
                message.messageList.Remove(message.messageList[0]);

            roomsRef.Child(RoomManager.roomManagerClass.roomName).Child("Messages").SetRawJsonValueAsync(JsonUtility.ToJson(message));
        });
    }
}
public class Messages
{
    public List<string> messageList = new List<string>();
}