using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Unity.Editor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MultiplayerManager : MonoBehaviour
{
    [SerializeField] private float maxWaitTimeForPlayer = 10;
    [SerializeField] private float maxWaitTimeForStart = 3;

    [SerializeField] private Text startingText = null;
    [SerializeField] private GameObject waitingMenu = null;
    [SerializeField] private GameObject gamingMenu = null;

    [SerializeField] private GameObject placePanel = null;
    [SerializeField] private GameObject placeObject = null;

    [SerializeField] private Text finishPlaceText = null;
    [SerializeField] private Text placeResultText = null;
    [SerializeField] private Text coinText = null;
    [SerializeField] private Text crystalText = null;
    [SerializeField] private Text remainingHeartText = null;

    [SerializeField] private GameObject playerObject = null;
    [SerializeField] private GameObject otherPlayerPrefab = null;
    [SerializeField] private Transform player = null;
    [SerializeField] private Transform finishObject = null;

    private GameObject[] otherPlayers;

    private List<string> placeList = new List<string>();

    private FirebaseAuth auth;
    private DatabaseReference userDataRef;
    private DatabaseReference roomRef;

    private int playingPlayerCount = 0;
    private long connectedPlayerCount = 0;
    private float waitTimer = 0;
    private float startTimer = 0;
    private bool waiting = true;
    private bool starting = false;

    public static MultiplayerManager multiplayerManagerClass;

    //Bu script Multiplayer oyun sahnesindeki ScriptObject'e aktarılmıştır.
    private void Awake()
    {
        //İlgili referanslar değişkenlere tanımlanır.
        auth = FirebaseAuth.DefaultInstance;
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        userDataRef = FirebaseDatabase.DefaultInstance.GetReference("UserDatas");
        roomRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms").Child(RoomManager.roomManagerClass.roomName);
    }
    private void Start()
    {
        multiplayerManagerClass = this;

        //Aktif oyuncu sahneye bağlandığı sırada Database'e kendi verilerini gönderir ve kostüm değişikliği uygulanır.
        userDataRef.Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                UserData me = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                Player playerMe = new Player();
                playerMe.userId = auth.CurrentUser.UserId;
                playerMe.nick = me.nick;
                playerMe.skin = me.skin;
                roomRef.Child("GameScene").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(playerMe));
                playerObject.GetComponent<Renderer>().material.color = me.skin;
            }
        });

        //Aktif oyuncunun bulunduğu odadaki oyuna bağlanacak olan oyuncu sayısı ilgili değişkene atanır.
        roomRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                RoomData room = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                playingPlayerCount = room.playingPlayerCount;
            }
        });
    }
    private void Update()
    {
        if (waiting)
        {
            //Diğer oyuncular beklenirken, tüm oyuncular bağlandıysa ya da maksimum bekleme süresine ulaşıldıysa oyuna başlanır.
            waitTimer += Time.deltaTime;
            roomRef.Child("GameScene").GetValueAsync().ContinueWithOnMainThread(task =>
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot.GetRawJsonValue() != null)
                {
                    connectedPlayerCount = snapshot.ChildrenCount;
                    if (connectedPlayerCount >= playingPlayerCount || waitTimer >= maxWaitTimeForPlayer)
                    {
                        waiting = false;
                        starting = true;
                        otherPlayers = new GameObject[connectedPlayerCount - 1];
                    }
                }
            });
        }        
        if (starting)
        {
            //Oyunun başlaması tetiklendiğinde başlatma için editörden alınan sayıdan geriye sayılır.
            startingText.text = "The game will start after " + (int)(maxWaitTimeForStart - startTimer) + " seconds";
            startTimer += Time.deltaTime;
            if(startTimer >= maxWaitTimeForStart)
            {
                starting = false;
                roomRef.Child("GameScene").GetValueAsync().ContinueWithOnMainThread(task =>
                {
                    DataSnapshot snapshot = task.Result;
                    if (snapshot.GetRawJsonValue() != null)
                    {
                        int i = 0;
                        foreach (DataSnapshot ds in snapshot.Children)
                        {
                            //Tüm oyuncular listeye eklenir.
                            Player otherPlayer = JsonUtility.FromJson<Player>(ds.GetRawJsonValue());
                            placeList.Add((i + 1) + ". " + otherPlayer.nick);

                            //Eğer ki ilgili oyuncu Aktif oyuncu ile aynı ID'ye sahipse bu oyuncu sahnede tekrar oluşturulmaz.
                            if (otherPlayer.userId == auth.CurrentUser.UserId)
                                continue;

                            //İlgili oyuncu sahnede oluşturulur ve skin değişkeni uygulanır.
                            otherPlayers[i] = Instantiate(otherPlayerPrefab);
                            otherPlayers[i].GetComponent<Renderer>().material.color = otherPlayer.skin;
                            i++;
                        }
                        UpdatePlaceText();
                    }
                });
                waitingMenu.SetActive(false);
                gamingMenu.SetActive(true);
                playerObject.GetComponent<Rigidbody>().useGravity = true;

                //Oyun başlatıldıktan sonra listeye eklenen oyuncular Database'e yazdırılır.
                GameScenePlaces playerPlaces = new GameScenePlaces();
                playerPlaces.finishedPlayerCount = 0;
                roomRef.Child("GameScenePlaces").SetRawJsonValueAsync(JsonUtility.ToJson(playerPlaces));
            }
        }
        
    }
    private void FixedUpdate()
    {
        //Oyunda sürekli konum gönderme ve rakip oyuncuların konumunu alma sağlanır. Oyuncuların mesafe sıraları da alınmaktadır.
        SendMeToDatabase();
        GetPlayerFromDatabase();
        OrderByDistance();
    }
    public void EndGameStatement(int heartCount, int earnedCoinCount, int earnedCrystalCount)
    {
        //GameManager scriptindeki trigger vasıtasıyla oyun bittiğinde bu metot çağırılır. Referanslar alınır.
        roomRef.Child("GameScenePlaces").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                GameScenePlaces playerPlaces = JsonUtility.FromJson<GameScenePlaces>(snapshot.GetRawJsonValue());
                playerPlaces.finishedPlayerCount++;
                int place = playerPlaces.finishedPlayerCount;
                roomRef.Child("GameScenePlaces").SetRawJsonValueAsync(JsonUtility.ToJson(playerPlaces));

                //Oyuncunun oyunu kaçıncı bitirdiğine ve kaç can harcadığına göre bir puan elde edilir.
                //O puana göre oyuncunun elde ettiği coin ve energy miktarları çarpılır.
                int placeResultPoint = (int)(connectedPlayerCount + 1) - place;
                int remainingHeartPoint = heartCount * placeResultPoint;
                int earnedCoin = remainingHeartPoint * earnedCoinCount;
                int earnedCrystal = remainingHeartPoint * earnedCrystalCount;

                UpdateUserData(earnedCoin, earnedCrystal);

                //Bitiş menüsündeki ilgili textlere gerekli bilgiler yazılır.
                finishPlaceText.text = "-" + place + "-";
                placeResultText.text = "+" + placeResultPoint + " point";
                remainingHeartText.text = "+" + remainingHeartPoint + " point";
                coinText.text = "+" + earnedCoin + " coin";
                crystalText.text = "+" + earnedCrystal + " crystal";
            }
        });
    }
    private void UpdateUserData(int earnedCoin, int earnedCrystal)
    {
        //Referans alınan energy ve coin miktarına göre Aktif oyuncu için Database'de güncellenir.
        userDataRef = FirebaseDatabase.DefaultInstance.GetReference("UserDatas").Child(auth.CurrentUser.UserId);
        userDataRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                UserData user = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                user.coin += earnedCoin;
                user.crystal += earnedCrystal;
                userDataRef.SetRawJsonValueAsync(JsonUtility.ToJson(user));
            }
        });
    }
    private void SendMeToDatabase()
    {
        //Aktif oyuncunun position bilgilerini Database'e göndermesini sağlar.
        Player.PlayerPosition myPosition = new Player.PlayerPosition();
        myPosition.position = player.position;
        roomRef.Child("GameScene").Child(auth.CurrentUser.UserId).Child("Transform").SetRawJsonValueAsync(JsonUtility.ToJson(myPosition));

        //Aktif oyuncunun bitiş noktasına olan mesafesini de Database'e gönderir. Böylelikle bu mesafelere göre anlık sıralama yapılır.
        GameSceneDistances myDistance = new GameSceneDistances();
        myDistance.nick = DatabaseManager.nick;
        myDistance.distance = Vector3.Distance(player.position, finishObject.position);
        roomRef.Child("GameSceneDistances").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(myDistance));
    }
    private void GetPlayerFromDatabase()
    {
        //Databaseden diğer oyuncuların anlık olaran position değerlerini almamızı sağlar.
        roomRef.Child("GameScene").GetValueAsync().ContinueWith(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                int i = 0;
                foreach (DataSnapshot ds in snapshot.Children)
                {
                    Player otherPlayer = JsonUtility.FromJson<Player>(ds.GetRawJsonValue());

                    //Eğer ki ilgili oyuncu aktif oyuncu ile aynı ID'ye sahipse bu oyuncuyu atla.
                    if (otherPlayer.userId == auth.CurrentUser.UserId)
                        continue;

                    Player.PlayerPosition otherPlayerTransform = JsonUtility.FromJson<Player.PlayerPosition>(ds.Child("Transform").GetRawJsonValue());
                    
                    //otherPlayers GameObject dizisindeki prefabların position değerleri ilgili oyunculara göre güncellenir.
                    float x = otherPlayerTransform.position.x;
                    float y = otherPlayerTransform.position.y;
                    float z = otherPlayerTransform.position.z;
                    otherPlayers[i].transform.position = new Vector3(x, y, z);
                    i++;
                }
            }
        });
    }
    private void OrderByDistance()
    {
        //Databaseden oyuncuların distance değerine göre sırayla çekilmesini sağlar.
        roomRef.Child("GameSceneDistances").OrderByChild("distance").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                int i = 0;
                foreach (DataSnapshot ds in snapshot.Children)
                {
                    GameSceneDistances playerDistance = JsonUtility.FromJson<GameSceneDistances>(ds.GetRawJsonValue());
                    //Eğer oyuncuların sıralama tablosundaki yeri aynı ise tekrar güncellenmez.
                    if (placeList[i] != ((i + 1) + ". " + playerDistance.nick))
                    {
                        placeList[i] = ((i + 1) + ". " + playerDistance.nick);
                        UpdatePlaceText();
                    }
                    i++;
                }
            }
        });
    }
    private void UpdatePlaceText()
    {
        //Arayüzdeki sıralama texti listeye göre güncellenir.
        for (int i = 0; i < placePanel.transform.childCount; i++)
            Destroy(placePanel.transform.GetChild(i).gameObject);
        for (int i = 0; i < placeList.Count; i++)
        {
            GameObject placeObjectClone = Instantiate(placeObject, placePanel.transform);
            placeObjectClone.GetComponent<Text>().text = placeList[i];
        }
    }
}
public class Player
{
    //Oyuna bağlanan oyunculara ait classtır.
    public string userId;
    public string nick;
    public Color skin;
    public class PlayerPosition
    {
        public Vector3 position;
    }
}
public class GameSceneDistances
{
    //Oyuncuların bitişe olan mesafelerini içeren classtır.
    public string userId;
    public string nick;
    public float distance;
}
public class GameScenePlaces
{
    //Oyunu bitiren oyuncu sayısını içeren classtır. Bu değere göre bitiş sıralaması yapılır.
    public int finishedPlayerCount;
}