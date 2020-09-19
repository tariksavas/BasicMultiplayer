using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Unity.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class RoomManager : MonoBehaviour
{
    [SerializeField] private int maxPlayerPerRoom = 10;
    [SerializeField] private int rowsPerTable = 5;
    [SerializeField] private float roomUpdateDelay = 0.1f;
    [SerializeField] private float refreshTableDelay = 3;
    [SerializeField] private float exceptionTextWaitTime = 3;
    [SerializeField] private float gameSceneStartTime = 3;

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

    private FirebaseAuth auth;
    private DatabaseReference roomsRef;

    private float refreshRoomTimer = 0;
    private float refreshTableTimer = 0;
    private float exceptionTextTimer = 0;
    private float gameSceneStartTimer = 0;
    private bool refreshedRoom = false;
    private bool refreshedTable = false;

    private bool canLeave = true;
    private bool played = false;

    public bool inRoom = false;
    public string roomName = "";
    public string selectedRoomName = null;
    public static RoomManager roomManagerClass;

    //Bu script MainMenu sahnesindeki ScriptObject 'e atanmıştır.
    private void Awake()
    {
        //Firebase'e ait aktif kullanıcı değişkeni
        auth = FirebaseAuth.DefaultInstance;
    }
    private void Start()
    {
        //Projenin Database linki ve ilgili Databasedeki "Rooms" ağacı referans alınır.
        roomManagerClass = this;
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        roomsRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms");
        GetRooms();
    }
    private void FixedUpdate()
    {
        if (inRoom)
        {
            //Eğer ki bir odada ise bu if sürekli çalışmaktadır.
            //Odadaki Database değişkenlerinde bir değişiklik olup olmadığı listener ile dinlenir.
            roomsRef.ChildAdded += HandleRoomChanged;
            roomsRef.ChildChanged += HandleRoomChanged;
            roomsRef.ChildRemoved += HandleRoomChanged;
            roomsRef.ChildMoved += HandleRoomChanged;
        }
        if (refreshedRoom)
        {
            //Odada iken listenerlar vasıtasıyla bir değişikliğin olduğu farkedilirse belirli bir delay sonra oda güncellenmektedir.
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
            //Join room menüsündeki odaları tekrar menüye yükleme butonuna basıldığında belirli bir delay sonra odalar yüklenmektedir.
            refreshTableTimer += Time.deltaTime;
            if (refreshTableTimer >= refreshTableDelay)
            {
                refreshedTable = false;
                refreshTableTimer = 0;
            }
        }
        if (exceptionText.text != "")
        {
            //Eğer ki textte bir uyarı var ise bir süre sonra silinmektedir.
            exceptionTextTimer += Time.deltaTime;
            if (exceptionTextTimer >= exceptionTextWaitTime)
            {
                exceptionText.text = "";
                exceptionTextTimer = 0;
            }
        }
        if (played)
        {
            //Eğer ki oyuncu odadayken oyun başlatıldığında hazır dediyse odadaki hazır diyenlerle birlikte bir süre sonra GameScene'e geçiş yapacaktır.
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
        //Odada bir değişiklik var ise odanın güncellenmesi tetiklenir.
        refreshedRoom = true;
    }
    private void OnApplicationPause(bool pause)
    {
        //Uygulama durdurulduğunda eğer odadaysa odadan çıkılır.
        if (inRoom)
        {
            exceptionText.text = "You were taken out of the room because the game was stopped.";
            LeaveRoom();
        }
    }
    private void OnApplicationQuit()
    {
        //Uygulamadan çıkıldığında eğer odadaysa odadan çıkılır.
        if (inRoom)
            LeaveRoom();
    }
    public void RefReshTable()
    {
        //Join room menüsündeki odaları güncelleme butonuna tanımlanmıştır. 
        //Tüm odalar menüden kaldırılır ve tekrar yüklenir.
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
        //Oyuncu odadayken bir değişiklik var ise odanın değişkenkeri güncellenir
        roomRow.transform.GetChild(0).GetComponent<Text>().text = joinedRoom.roomName;
        roomRow.transform.GetChild(1).GetComponent<Text>().text = joinedRoom.roomLeadNick;
        roomRow.transform.GetChild(2).GetComponent<Text>().text = joinedRoom.level.ToString();
        roomRow.transform.GetChild(3).GetComponent<Text>().text = joinedRoom.currentPlayerCount + "/" + joinedRoom.maxPlayer;
        roomRow.transform.GetChild(4).GetComponent<Text>().text = joinedRoom.status;
    }
    private void ClearPlayerRows()
    {
        //Oyuncu odadayken bir değişiklik var ise odada listelenen tüm oyuncu satırları güncellenir.
        roomsRef.Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                for (int i = 0; i < joinedRoom.maxPlayer; i++)
                {
                    //Oyuncunun listelendiği satırların 0. indisinde oyuncuya ait nick, 1. indisinde oyuncunun durumuna ait sprite bulunmaktadır.
                    playerRows.transform.GetChild(i).transform.GetChild(0).GetComponent<Text>().text = "";
                    playerRows.transform.GetChild(i).transform.GetChild(1).transform.gameObject.SetActive(false);
                }
                //İlgili oda referans gönderilerek playerlar tekrar ilgili textlere yazdırılır.
                GetRoomPlayers(joinedRoom);
            }
        });
    }
    public void CreateRoom()
    {
        //Createroom sahnesindeki oda oluşturma butonuna tanımlanmıştır.
        try
        {
            if (CheckCreateRoomInputs())
            {
                //Oda oluşturulurken inputlarda bir hata yok ise alınan input değerlerine göre oda oluşturulur.
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
                    //Eğer ki oluşturulmakta olan odanın adı daha önceden kullanılmış ise uyarı verilir.
                    if (CheckRooms(task.Result))
                    {
                        createRoomMenu.SetActive(false);
                        refreshRoomTimer = -1;
                        string emptyJson = JsonUtility.ToJson(newRoom);
                        roomsRef.Child(newRoom.roomName).SetRawJsonValueAsync(emptyJson);

                        //Oluşturulan oda hem tabloya eklenir. Hem de odaya ilgili userlar getirilir.(Burada sadece bu oyuncu getirilir)
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
            //Oda oluştururken elde edilen hata ilgili texte yazdırılır.
            exceptionText.text = e.Message;
        }
    }
    public void JoinRoom()
    {
        if (selectedRoomName != null && selectedRoomName != "")
        {
            //Bu değişkene ilgili değer listelenen odalarda bulunan butona tıklandığında atanmaydı.
            roomsRef.Child(selectedRoomName).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.GetRawJsonValue() != null)
                {
                    RoomData joiningRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                    
                    //Odanın kapasitesi dolu ise uyarı verilir. Değil ise odaya katılınır ve yeni oyuncu odaya eklenir.
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
        //Katılınan odaya ait Database güncellemeleri yapılır.
        roomName = joiningRoom.roomName;
        joiningRoom.currentPlayerCount++;
        roomsRef.Child(joiningRoom.roomName).Child("currentPlayerCount").SetValueAsync(joiningRoom.currentPlayerCount);

        //Aktif kullanıcıya ait default değişkenler atanır.
        RoomData.RoomUsers roomUser = new RoomData.RoomUsers();
        roomUser.userId = auth.CurrentUser.UserId;
        roomUser.nick = DatabaseManager.nick;
        roomUser.ready = false;
        roomUser.playing = false;

        //Eğer bu oyuncu odayı kuran oyuncu ise hazır durumuna getirilir.
        if (joiningRoom.roomLeadId == auth.CurrentUser.UserId)
            roomUser.ready = true;

        string emptyJson = JsonUtility.ToJson(roomUser);
        roomsRef.Child(joiningRoom.roomName).Child("RoomUsers").Child(roomUser.userId).SetRawJsonValueAsync(emptyJson);

        //"canLeave" değişkeni kullanıcının odadan ayrılabileceğini, inRoom değişkeni de odada bulunduğunu ifade etmektedir.
        canLeave = true;
        inRoom = true;
        roomMenu.SetActive(true);
        GetRoomPlayers(joiningRoom);
    }
    public void LeaveRoom()
    {
        if (canLeave)
        {
            //Oyuncu odadan ayrılabilirse ilgili menüler SetActive edilir.
            roomMenu.SetActive(false);
            joinRoomMenu.SetActive(true);
            readyButtonObject.SetActive(true);
            startButtonObject.SetActive(false);
            inRoom = false;
            MessageManager.leftRoom = true;

            roomsRef.Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.GetRawJsonValue() != null)
                {
                    //Ayrılınan odanın "RoomUsers" ağacından aktif kullanıcı kaldırılır.
                    RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                    roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(auth.CurrentUser.UserId).RemoveValueAsync();
                    RefReshTable();

                    //Eğer ki kullanıcı oda lideri ise Database'deki oda lideri sıfırlanır.
                    if (joinedRoom.roomLeadId == auth.CurrentUser.UserId)
                    {
                        joinedRoom.roomLeadId = null;
                        roomsRef.Child(joinedRoom.roomName).Child("roomLeadId").SetValueAsync(joinedRoom.roomLeadId);
                    }

                    //Odada bulunan oyuncu sayısı 1 azaltılır ve odada oyuncu kalmadıysa oda kapatılır.
                    joinedRoom.currentPlayerCount--;
                    if (joinedRoom.currentPlayerCount > 0)
                        roomsRef.Child(joinedRoom.roomName).Child("currentPlayerCount").SetValueAsync(joinedRoom.currentPlayerCount);
                    else
                        roomsRef.Child(joinedRoom.roomName).RemoveValueAsync();
                }
            });
        }
        //Oyuncu hazır durumunda iken odadan ayrılamaz.
        else
            exceptionText.text = "You can not leave when ready!";
    }
    public void LeaveRoomWhenPlaying()
    {
        //Oyun sahnesinden ayrılırken oyuncu odadan çıkarılır.
        roomsRef.Child(roomName).GetValueAsync().ContinueWith(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                //Oyuncunun odadaki oyun sahnesine ait değişkenleri Databaseden kaldırılır.
                RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(auth.CurrentUser.UserId).RemoveValueAsync();
                roomsRef.Child(joinedRoom.roomName).Child("GameScene").Child(auth.CurrentUser.UserId).RemoveValueAsync();
                roomsRef.Child(joinedRoom.roomName).Child("GameSceneDistances").Child(auth.CurrentUser.UserId).RemoveValueAsync();

                //Oyuncu odadan çıkarıldıktan sonra gerekli kontroller tekrardan yapılır.
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
        //Bu metot odada bulunan "Ready" butonuna tanımlanmıştır.
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
                            //Odada bulunan oyuncular arasından aktif oyuncunun ID'sine göre Database'de oyuncunun "ready" değişkeni güncellenir.
                            RoomData.RoomUsers roomUser = JsonUtility.FromJson<RoomData.RoomUsers>(ds.GetRawJsonValue());
                            if (roomUser.userId == auth.CurrentUser.UserId)
                            {
                                playerRows.transform.GetChild(index).transform.GetChild(1).transform.gameObject.SetActive(!roomUser.ready);
                                roomUser.ready = !roomUser.ready;
                                canLeave = !roomUser.ready;

                                roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(roomUser.userId).Child("ready").SetValueAsync(roomUser.ready);

                                //Eğer ki oyuncu hazır durumuna geldiyse odaya ait hazır oyuncu sayısı ve buton rengi güncellenir.
                                if (roomUser.ready)
                                {
                                    joinedRoom.readyPlayerCount++;
                                    readyButton.GetComponent<Image>().color = Color.green;
                                }
                                //Eğer ki oyuncu hazır durumundan çıktıysa odaya ait hazır oyuncu sayısı ve buton rengi güncellenir.
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
        //Aktif oyuncu oda lideriyken start butonu görünmektedir. Ve bu metot start butonuna tanımlanmıştır.
        roomsRef.Child(roomName).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                RoomData joinedRoom = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                if (joinedRoom.readyPlayerCount >= 1)
                {
                    //Odaya ait "status" değişkeni güncellenmektedir
                    roomsRef.Child(joinedRoom.roomName).Child("status").SetValueAsync("Playing");

                    roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").GetValueAsync().ContinueWithOnMainThread(childTask =>
                    {
                        DataSnapshot childSnapshot = childTask.Result;
                        if (childSnapshot.GetRawJsonValue() != null)
                        {
                            foreach (DataSnapshot ds in childSnapshot.Children)
                            {
                                //Tek tek tüm oyuncular kontrol edilerek ilgili değişkenleri Database'de güncellenmektedir.
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
                //Eğer ki hazır diyen oyuncu yok ise bu uyarı gösterilir.
                else
                    exceptionText.text = "At least 1 people must be ready for the game to start.";
            }
        });
    }
    private bool CheckCreateRoomInputs()
    {
        //Oda kurarken ilgili input değerlirine göre kontroller yapılır.
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
        //Referans alınan odaya göre tüm oyuncular odaya getirilir.
        roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                for(int i = 0; i < joinedRoom.maxPlayer; i++)
                {
                    //Tüm oyuncu textleri ve spriteları silinir.
                    playerRows.transform.GetChild(i).GetChild(1).transform.gameObject.SetActive(false);
                    playerRows.transform.GetChild(i).GetChild(1).GetComponent<Image>().sprite = memberSprite;
                }

                int index = 0;
                foreach (DataSnapshot ds in snapshot.Children)
                {
                    //Tek tek tüm oyuncular ele alınır.
                    RoomData.RoomUsers roomUser = JsonUtility.FromJson<RoomData.RoomUsers>(ds.GetRawJsonValue());

                    //Eğer ki oda lideri yok ise 0. indiste bulunan oyuncu yeni oda lideri olarak atanır.
                    if (index == 0 && joinedRoom.currentPlayerCount > 0 && (joinedRoom.roomLeadId == null || joinedRoom.roomLeadId == ""))
                    {
                        //Cihazdaki aktif oyuncu hazır durumunda ise "canLeave" değişkeni "false" 'dan "true" 'ya çekilir.
                        if (roomUser.ready && roomUser.userId == auth.CurrentUser.UserId)
                            canLeave = true;

                        roomsRef.Child(joinedRoom.roomName).Child("RoomUsers").Child(roomUser.userId).Child("ready").SetValueAsync(true);
                        roomsRef.Child(joinedRoom.roomName).Child("roomLeadId").SetValueAsync(roomUser.userId);
                        roomsRef.Child(joinedRoom.roomName).Child("roomLeadNick").SetValueAsync(roomUser.nick);
                    }
                    //Eğer ki oyuncu oda lideriyse Sprite'ı güncellenir.
                    if (joinedRoom.roomLeadId == roomUser.userId)
                        playerRows.transform.GetChild(index).GetChild(1).GetComponent<Image>().sprite = crownSprite;

                    //Eğer ki oyuncu oyunda değilse hazır durumuna göre Sprite'ı aktif edilir.
                    if(!roomUser.playing)
                        playerRows.transform.GetChild(index).GetChild(1).transform.gameObject.SetActive(roomUser.ready);

                    //Eğer ki oyuncu odada ise Sprite'ı güncellenir ve aktif hale getirilir.
                    else
                    {
                        playerRows.transform.GetChild(index).GetChild(1).GetComponent<Image>().sprite = playingSprite;
                        playerRows.transform.GetChild(index).GetChild(1).transform.gameObject.SetActive(true);
                    }

                    playerRows.transform.GetChild(index).transform.GetChild(0).GetComponent<Text>().text = roomUser.nick;

                    //Tek tek tüm oyuncular kontrol edilirken ilgili oyuncu, cihazdaki aktif oyuncuyla aynı olduğunda oyuncuya ait-
                    //playing değişkeni true ise (oyuna başlıyorsa) odadan ayrılamaz ve bir süre sonra oyun sahnesine yönlendirilir.
                    if(roomUser.userId == auth.CurrentUser.UserId && roomUser.playing)
                    {
                        canLeave = false;
                        played = true;
                    }

                    index++;
                }
                //Aktif oyuncu oda lideriyse ilgili buton düzenlemeleri yapılır.
                if (joinedRoom.roomLeadId == auth.CurrentUser.UserId)
                {
                    readyButton.image.color = Color.white;
                    readyButtonObject.SetActive(false);
                    startButtonObject.SetActive(true);
                }
            }
        });
        //Odaya ait text güncellemelerinin yapıldığı metot çağrılır..
        RefreshRoom(joinedRoom);
    }
    private void GetRooms()
    {
        //Join room menüsündeki odaların Database'den tek tek getirilmesini ardından AddTable metoduna referans gönderilmesini sağlar.
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
        //Eğer ki oda daha önceden kurulmuş ise false değerini döndürür.
        if (snapshot.GetRawJsonValue() == null)
            return true;

        return false;
    }
    private void AddTable(RoomData rooms)
    {
        //Referans alınan oda Join room menüsündeki tabloya eklenir.
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
    //Odaya ait değişkenlerin bulunduğu bir classtır.
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
        //Odadaki oyunculara ait değişkenlerin bulunduğu bir classtır.
        public string userId;
        public string nick;
        public bool ready;
        public bool playing;
    }
}