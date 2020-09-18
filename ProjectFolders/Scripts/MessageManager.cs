using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Unity.Editor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MessageManager : MonoBehaviour
{
    [SerializeField] private GameObject chatPanel = null;
    [SerializeField] private GameObject textObject = null;
    [SerializeField] private InputField chatInput = null;
    [SerializeField] private int messageLimit = 20;
    [SerializeField] private float refreshChatDelay = 0.1f;
    private float refreshChatTimer = 0;
    private bool refreshedChat = false;

    private FirebaseAuth auth;
    private DatabaseReference roomsRef;

    public static bool leftRoom = false;
    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
    }
    private void Start()
    {
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");
    }
    private void FixedUpdate()
    {
        if (RoomManager.roomManagerClass.inRoom)
        {
            roomsRef.ChildAdded += HandleChatChanged;
            roomsRef.ChildChanged += HandleChatChanged;
            roomsRef.ChildRemoved += HandleChatChanged;
            roomsRef.ChildMoved += HandleChatChanged;
        }
        if (!RoomManager.roomManagerClass.inRoom && leftRoom)
        {
            leftRoom = false;
            ClearChat();
        }
        if (refreshedChat)
        {
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
        roomsRef.Child(RoomManager.roomManagerClass.roomName).Child("Messages").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                Messages message = JsonUtility.FromJson<Messages>(snapshot.GetRawJsonValue());
                SendMessagesToChat(message);
            }
        });
    }
    private void ClearChat()
    {
        for (int i = 0; i < chatPanel.transform.childCount; i++)
            Destroy(chatPanel.transform.GetChild(i).gameObject);
    }
    private void SendMessagesToChat(Messages message)
    {
        ClearChat();
        for (int i = 0; i < message.messageList.Count; i++)
        {
            GameObject textObjectClone = Instantiate(textObject, chatPanel.transform);
            textObjectClone.GetComponent<Text>().text = message.messageList[i];
        }
    }
    public void SendButton()
    {
        if (chatInput.text == null || chatInput.text == "")
            return;

        SendMessageToDatabes(chatInput.text);
        chatInput.text = "";
    }
    public void SendMessageToDatabes(string text)
    {
        roomsRef.Child(RoomManager.roomManagerClass.roomName).Child("Messages").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            Messages message = new Messages();
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
                message = JsonUtility.FromJson<Messages>(snapshot.GetRawJsonValue());

            text = DatabaseManager.nick + ": " + text;
            message.messageList.Add(text);

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