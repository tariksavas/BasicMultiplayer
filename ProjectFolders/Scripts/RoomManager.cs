using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Unity.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class RoomManager : MonoBehaviour
{
    [SerializeField] private Text exceptionText = null;

    [SerializeField] private InputField roomNameInput = null;
    [SerializeField] private InputField maxPlayerInput = null;
    [SerializeField] private Dropdown levelInput = null;

    [SerializeField] private GameObject table = null;
    [SerializeField] private GameObject row = null;

    [SerializeField] private GameObject createRoomMenu = null;
    [SerializeField] private GameObject joinRoomMenu = null;
    [SerializeField] private GameObject roomMenu = null;

    [SerializeField] private GameObject roomRow = null;
    [SerializeField] private GameObject playerRows = null;

    [SerializeField] private Button readyButton = null;
    [SerializeField] private GameObject readyButtonObject = null;
    [SerializeField] private GameObject startButtonObject = null;

    [SerializeField] private Sprite crownSprite = null;
    [SerializeField] private Sprite memberSprite = null;
    [SerializeField] private Sprite playingSprite = null;

    [SerializeField] private int maxPlayerPerRoom = 10;
    [SerializeField] private int rowsPerTable = 5;
    [SerializeField] private float roomUpdateDelay = 0.1f;
    [SerializeField] private float refreshTableDelay = 3;
    [SerializeField] private float exceptionTextWaitTime = 3;
    [SerializeField] private float gameSceneStartTime = 3;

    private float refreshRoomTimer = 0;
    private float refreshTableTimer = 0;
    private float exceptionTextTimer = 0;
    private float gameSceneStartTimer = 0;
    private bool refreshedRoom = false;
    private bool refreshedTable = false;

    private bool ready = false;
    private bool played = false;

    private FirebaseAuth auth;
    private DatabaseReference roomsRef;

    public bool inRoom = false;
    public string roomName = "";
    public string selectedRoomName = null;
    public static RoomManager roomManagerClass;

    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
    }
    private void Start()
    {
        roomManagerClass = this;
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");
        GetRooms();
    }
    private void FixedUpdate()
    {
        if (inRoom)
        {
            roomsRef.ChildAdded += HandleRoomChanged;
            roomsRef.ChildChanged += HandleRoomChanged;
            roomsRef.ChildRemoved += HandleRoomChanged;
            roomsRef.ChildMoved += HandleRoomChanged;
        }
        if (refreshedRoom)
        {
            refreshRoomTimer += Time.deltaTime;
            if (refreshRoomTimer >= roomUpdateDelay)
            {
                refreshRoomTimer = 0;
                refreshedRoom = false;
                ClearPlayerRows();
            }
        }
        if (refreshedTable)
        {
            refreshTableTimer += Time.deltaTime;
            if (refreshTableTimer >= refreshTableDelay)
            {
                refreshedTable = false;
                refreshTableTimer = 0;
            }
        }
        if (exceptionText.text != "")
        {
            exceptionTextTimer += Time.deltaTime;
            if (exceptionTextTimer >= exceptionTextWaitTime)
            {
                exceptionText.text = "";
                exceptionTextTimer = 0;
            }
        }
        if (played)
        {
            gameSceneStartTimer += Time.deltaTime;
            exceptionText.text = "The game will start after " + (int)(gameSceneStartTime - gameSceneStartTimer) + " seconds";
            if (gameSceneStartTimer >= gameSceneStartTime)
            {
                played = false;
                gameSceneStartTimer = 0;
                SceneManager.LoadScene("GameScene");
            }
        }
    }
    private void HandleRoomChanged(object sender, ChildChangedEventArgs args)
    {
        refreshedRoom = true;
    }
    private void OnApplicationPause(bool pause)
    {
        if (inRoom)
        {
            exceptionText.text = "You were taken out of the room because the game was stopped.";
            LeaveRoom();
        }
    }
    private void OnApplicationQuit()
    {
        if(inRoom)
            LeaveRoom();
    }
    public void RefReshTable()
    {
        if (!refreshedTable)
        {
            refreshedTable = true;
            for (int i = 0; i < table.transform.childCount; i++)
                Destroy(table.transform.GetChild(i).gameObject);

            GetRooms();
        }
        else
            exceptionText.text = "You can refresh every " + refreshTableDelay + " seconds.";
    }
    private void RefreshRoom(RoomData joinedRoom)
    {
        roomRow.transform.GetChild(0).GetComponent<Text>().text = joinedRoom.roomName;
        roomRow.transform.GetChild(1).GetComponent<Text>().text = joinedRoom.roomLeadNick;
        roomRow.transform.GetChild(2).GetComponent<Text>().text = joinedRoom.level.ToString();
        roomRow.transform.GetChild(3).GetComponent<Text>().text = joinedRoom.currentPlayerCount + "/" + joinedRoom.maxPlayer;
        roomRow.transform.GetChild(4).GetComponent<Text>().text = joinedRoom.status;
    }
    private void ClearPlayerRows()
    {
        roomsRef.Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                for (int i = 0; i < joinedRoom.maxPlayer; i++)
                {
                    playerRows.transform.GetChild(i).transform.GetChild(0).GetComponent<Text>().text = "";
                    playerRows.transform.GetChild(i).transform.GetChild(1).transform.gameObject.SetActive(false);
                }
                GetRoomPlayers(joinedRoom);
            }
        });
    }
    public void CreateRoom()
    {
        try
        {
            if (CheckCreateRoomInputs())
            {
                RoomData newRoom = new RoomData
                {
                    roomName = roomNameInput.text,
                    level = levelInput.value + 1,
                    maxPlayer = int.Parse(maxPlayerInput.text),
                    currentPlayerCount = 0,
                    status = "Waiting",
                    readyPlayerCount = 0,
                    roomLeadId = auth.CurrentUser.UserId,
                    roomLeadNick = DatabaseManager.nick
                };
                roomsRef.Child(newRoom.roomName).GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    if (CheckRooms(task.Result))
                    {
                        createRoomMenu.SetActive(false);
                        refreshRoomTimer = -1;
                        string emptyJson = JsonUtility.ToJson(newRoom);
                        roomsRef.Child(newRoom.roomName).SetRawJsonValueAsync(emptyJson);

                        NewRoomUser(newRoom);
                        AddTable(newRoom);
                    }
                    else
                        exceptionText.text = "This room name is already exist.";
                });
            }
        }
        catch (System.Exception e)
        {
            exceptionText.text = e.Message;
        }
    }
    public void JoinRoom()
    {
        if (selectedRoomName != null && selectedRoomName != "")
        {
            roomsRef.Child(selectedRoomName).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.GetRawJsonValue() != null)
                {
                    RoomData joiningRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                    if (joiningRoom.currentPlayerCount < joiningRoom.maxPlayer)
                    {
                        joinRoomMenu.SetActive(false);
                        NewRoomUser(joiningRoom);
                    }
                    else
                        exceptionText.text = "This room is full.";
                }
                else
                    exceptionText.text = "This room may have been deleted. Please refresh table.";
            });
        }
    }
    private void NewRoomUser(RoomData joiningRoom)
    {
        roomName = joiningRoom.roomName;
        joiningRoom.currentPlayerCount++;
        roomsRef.Child(joiningRoom.roomName).Child("currentPlayerCount").SetValueAsync(joiningRoom.currentPlayerCount);

        RoomData.RoomUsers roomUser = new RoomData.RoomUsers();
        roomUser.userId = auth.CurrentUser.UserId;
        roomUser.nick = DatabaseManager.nick;
        roomUser.ready = false;
        roomUser.playing = false;
        if (joiningRoom.roomLeadId == auth.CurrentUser.UserId)
            roomUser.ready = true;
        string emptyJson = JsonUtility.ToJson(roomUser);
        roomsRef.Child(joiningRoom.roomName).Child("RoomUsers").Child(roomUser.userId).SetRawJsonValueAsync(emptyJson);

        ready = false;
        inRoom = true;
        roomMenu.SetActive(true);
        GetRoomPlayers(joiningRoom);
    }
    public void LeaveRoom()
    {
        if (!ready)
        {
            roomMenu.SetActive(false);
            joinRoomMenu.SetActive(true);
            readyButtonObject.SetActive(true);
            startButtonObject.SetActive(false);

            roomsRef.Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.GetRawJsonValue() != null)
                {
                    RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                    roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(auth.CurrentUser.UserId).RemoveValueAsync();
                    inRoom = false;
                    MessageManager.leftRoom = true;
                    RefReshTable();
                    if (joinedRoom.roomLeadId == auth.CurrentUser.UserId)
                    {
                        joinedRoom.roomLeadId = null;
                        roomsRef.Child(joinedRoom.roomName).Child("roomLeadId").SetValueAsync(joinedRoom.roomLeadId);
                    }
                    joinedRoom.currentPlayerCount--;
                    if (joinedRoom.currentPlayerCount > 0)
                        roomsRef.Child(joinedRoom.roomName).Child("currentPlayerCount").SetValueAsync(joinedRoom.currentPlayerCount);
                    else
                        roomsRef.Child(joinedRoom.roomName).RemoveValueAsync();
                }
            });
        }
        else
            exceptionText.text = "You can not leave when ready!";
    }
    public void LeaveRoomWhenPlaying()
    {
        roomsRef.Child(roomName).GetValueAsync().ContinueWith(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(auth.CurrentUser.UserId).RemoveValueAsync();
                roomsRef.Child(joinedRoom.roomName).Child("GameScene").Child(auth.CurrentUser.UserId).RemoveValueAsync();
                roomsRef.Child(joinedRoom.roomName).Child("GameSceneDistances").Child(auth.CurrentUser.UserId).RemoveValueAsync();
                if (joinedRoom.roomLeadId == auth.CurrentUser.UserId)
                {
                    joinedRoom.roomLeadId = null;
                    roomsRef.Child(joinedRoom.roomName).Child("roomLeadId").SetValueAsync(joinedRoom.roomLeadId);
                }
                joinedRoom.currentPlayerCount--;
                if (joinedRoom.currentPlayerCount > 0)
                    roomsRef.Child(joinedRoom.roomName).Child("currentPlayerCount").SetValueAsync(joinedRoom.currentPlayerCount);
                else
                    roomsRef.Child(joinedRoom.roomName).RemoveValueAsync();
            }
        });
    }
    public void Ready()
    {
        roomsRef.Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if(snapshot.GetRawJsonValue() != null)
            {
                RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                roomsRef.Child(roomName).Child("RoomUsers").GetValueAsync().ContinueWithOnMainThread(childTask =>
                {
                    DataSnapshot childSnapshot = childTask.Result;
                    if (childSnapshot.GetRawJsonValue() != null)
                    {
                        int index = 0;
                        foreach (DataSnapshot ds in childSnapshot.Children)
                        {
                            RoomData.RoomUsers roomUser = JsonUtility.FromJson<RoomData.RoomUsers>(ds.GetRawJsonValue());
                            if (roomUser.userId == auth.CurrentUser.UserId)
                            {
                                playerRows.transform.GetChild(index).transform.GetChild(1).transform.gameObject.SetActive(!roomUser.ready);
                                roomUser.ready = !roomUser.ready;
                                ready = roomUser.ready;

                                roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(roomUser.userId).Child("ready").SetValueAsync(roomUser.ready);

                                if (roomUser.ready)
                                {
                                    joinedRoom.readyPlayerCount++;
                                    readyButton.GetComponent<Image>().color = Color.green;
                                }
                                else
                                {
                                    joinedRoom.readyPlayerCount--;
                                    readyButton.GetComponent<Image>().color = Color.white;
                                }
                                roomsRef.Child(joinedRoom.roomName).Child("readyPlayerCount").SetValueAsync(joinedRoom.readyPlayerCount);
                                break;
                            }
                            index++;
                        }
                    }
                });
            }
        });
    }
    public void StartGame()
    {
        roomsRef.Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                if (joinedRoom.readyPlayerCount >= 1)
                {
                    roomsRef.Child(joinedRoom.roomName).Child("status").SetValueAsync("Playing");

                    roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").GetValueAsync().ContinueWithOnMainThread(childTask =>
                    {
                        DataSnapshot childSnapshot = childTask.Result;
                        if (childSnapshot.GetRawJsonValue() != null)
                        {
                            foreach (DataSnapshot ds in childSnapshot.Children)
                            {
                                RoomData.RoomUsers roomUser = JsonUtility.FromJson<RoomData.RoomUsers>(ds.GetRawJsonValue());
                                if (roomUser.ready)
                                {
                                    roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(roomUser.userId).Child("ready").SetValueAsync(false);
                                    roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(roomUser.userId).Child("playing").SetValueAsync(true);
                                    roomsRef.Child(joinedRoom.roomName).Child("playingPlayerCount").SetValueAsync(++joinedRoom.playingPlayerCount);
                                }
                            }
                        }
                    });
                }
                else
                    exceptionText.text = "At least 1 people must be ready for the game to start.";
            }
        });
    }
    private bool CheckCreateRoomInputs()
    {
        if (roomNameInput.text == null || roomNameInput.text == "")
        {
            exceptionText.text = "Please enter room name.";
            return false;
        }
        else if (maxPlayerInput.text == null || maxPlayerInput.text == "")
        {
            exceptionText.text = "Please enter max player count.";
            return false;
        }
        else if(int.Parse(maxPlayerInput.text) < 2 || int.Parse(maxPlayerInput.text) > maxPlayerPerRoom)
        {
            exceptionText.text = "Please enter a number between 2 and " + maxPlayerPerRoom + " for the maximum number of players";
            return false;
        }
        return true;
    }
    private void GetRoomPlayers(RoomData joinedRoom)
    {
        roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                for(int i = 0; i < joinedRoom.maxPlayer; i++)
                {
                    playerRows.transform.GetChild(i).GetChild(1).transform.gameObject.SetActive(false);
                    playerRows.transform.GetChild(i).GetChild(1).GetComponent<Image>().sprite = memberSprite;
                }

                int index = 0;
                foreach (DataSnapshot ds in snapshot.Children)
                {
                    RoomData.RoomUsers roomUser = JsonUtility.FromJson<RoomData.RoomUsers>(ds.GetRawJsonValue());

                    if (index == 0 && joinedRoom.currentPlayerCount > 0 && (joinedRoom.roomLeadId == null || joinedRoom.roomLeadId == ""))
                    {
                        if (roomUser.ready && roomUser.userId == auth.CurrentUser.UserId)
                            ready = false;

                        roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(roomUser.userId).Child("ready").SetValueAsync(true);
                        roomsRef.Child(joinedRoom.roomName).Child("roomLeadId").SetValueAsync(roomUser.userId);
                        roomsRef.Child(joinedRoom.roomName).Child("roomLeadNick").SetValueAsync(roomUser.nick);
                    }
                    if (joinedRoom.roomLeadId == roomUser.userId)
                        playerRows.transform.GetChild(index).GetChild(1).GetComponent<Image>().sprite = crownSprite;

                    if(!roomUser.playing)
                        playerRows.transform.GetChild(index).GetChild(1).transform.gameObject.SetActive(roomUser.ready);
                    else
                    {
                        playerRows.transform.GetChild(index).GetChild(1).GetComponent<Image>().sprite = playingSprite;
                        playerRows.transform.GetChild(index).GetChild(1).transform.gameObject.SetActive(true);
                    }

                    playerRows.transform.GetChild(index).transform.GetChild(0).GetComponent<Text>().text = roomUser.nick;

                    if(roomUser.userId == auth.CurrentUser.UserId && roomUser.playing)
                    {
                        ready = true;
                        played = true;
                    }

                    index++;
                }
                if (joinedRoom.roomLeadId == auth.CurrentUser.UserId)
                {
                    readyButton.image.color = Color.white;
                    readyButtonObject.SetActive(false);
                    startButtonObject.SetActive(true);
                }
            }
        });
        RefreshRoom(joinedRoom);
    }
    private void GetRooms()
    {
        roomsRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                foreach(DataSnapshot ds in snapshot.Children)
                {
                    RoomData rooms = JsonUtility.FromJson<RoomData>(ds.GetRawJsonValue());
                    AddTable(rooms);
                }
            }
        });
    }
    private bool CheckRooms(DataSnapshot snapshot)
    {
        if (snapshot.GetRawJsonValue() == null)
            return true;

        return false;
    }
    private void AddTable(RoomData rooms)
    {
        int childCount = table.transform.childCount;
        GameObject rowCopy = Instantiate(row, table.transform);
        RectTransform rect = rowCopy.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, ((1 - ((float)1 / rowsPerTable)) - ((float)childCount / rowsPerTable)));
        rect.anchorMax = new Vector2(1, 1 - ((float)childCount / rowsPerTable));

        rowCopy.transform.GetChild(0).GetComponent<Text>().text = rooms.roomName;
        rowCopy.transform.GetChild(1).GetComponent<Text>().text = rooms.roomLeadNick;
        rowCopy.transform.GetChild(2).GetComponent<Text>().text = rooms.level.ToString();
        rowCopy.transform.GetChild(3).GetComponent<Text>().text = rooms.currentPlayerCount + "/" + rooms.maxPlayer;
        rowCopy.transform.GetChild(4).GetComponent<Text>().text = rooms.status;
    }
}
public class RoomData
{
    public string roomName;
    public int level;
    public int maxPlayer;
    public int currentPlayerCount;
    public int readyPlayerCount;
    public int playingPlayerCount;
    public string status;
    public string roomLeadId;
    public string roomLeadNick;
    public class RoomUsers
    {
        public string userId;
        public string nick;
        public bool ready;
        public bool playing;
    }
}