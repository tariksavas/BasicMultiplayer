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

    private List<string> placeList = new List<string>();

    private int playingPlayerCount = 0;
    private long connectedPlayerCount = 0;
    private float waitTimer = 0;
    private float startTimer = 0;
    private bool waiting = true;
    private bool starting = false;

    private GameObject[] otherPlayers;
    private string[] otherPlayersNick;

    private FirebaseAuth auth;
    private DatabaseReference userDataRef;
    private DatabaseReference roomRef;

    public static MultiplayerManager multiplayerManagerClass;
    private void Awake()
    {
        auth = FirebaseAuth.DefaultInstance;
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        userDataRef = FirebaseDatabase.DefaultInstance.GetReference("UserDatas");
        roomRef = FirebaseDatabase.DefaultInstance.GetReference("Rooms").Child(RoomManager.roomManagerClass.roomName);
    }
    private void Start()
    {
        multiplayerManagerClass = this;
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
        roomRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                RoomData room = JsonUtility.FromJson<RoomData>(snapshot.GetRawJsonValue());
                playingPlayerCount = room.playingPlayerCount;

                roomRef.Child("playingPlayerCount").SetValueAsync(0);
            }
        });
    }
    private void Update()
    {
        if (waiting)
        {
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
                        otherPlayersNick = new string[connectedPlayerCount - 1];
                    }
                }
            });
        }        
        if (starting)
        {
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
                            Player otherPlayer = JsonUtility.FromJson<Player>(ds.GetRawJsonValue());
                            placeList.Add((i + 1) + ". " + otherPlayer.nick);
                            if (otherPlayer.userId == auth.CurrentUser.UserId)
                                continue;

                            otherPlayersNick[i] = otherPlayer.nick;
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

                GameScenePlaces playerPlaces = new GameScenePlaces();
                playerPlaces.finishedPlayerCount = 0;
                roomRef.Child("GameScenePlaces").SetRawJsonValueAsync(JsonUtility.ToJson(playerPlaces));
            }
        }
        
    }
    private void FixedUpdate()
    {
        SendMeToDatabase();
        GetPlayerFromDatabase();
        OrderByDistance();
    }
    public void EndGameStatement(int heartCount, int earnedCoinCount, int earnedCrystalCount)
    {
        roomRef.Child("GameScenePlaces").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                GameScenePlaces playerPlaces = JsonUtility.FromJson<GameScenePlaces>(snapshot.GetRawJsonValue());
                playerPlaces.finishedPlayerCount++;
                int place = playerPlaces.finishedPlayerCount;
                roomRef.Child("GameScenePlaces").SetRawJsonValueAsync(JsonUtility.ToJson(playerPlaces));

                int placeResultPoint = (int)(connectedPlayerCount + 1) - place;
                int remainingHeartPoint = heartCount * placeResultPoint;
                int earnedCoin = remainingHeartPoint * earnedCoinCount;
                int earnedCrystal = remainingHeartPoint * earnedCrystalCount;

                UpdateUserData(earnedCoin, earnedCrystal);

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
        Player.PlayerPosition myPosition = new Player.PlayerPosition();
        myPosition.position = player.position;
        roomRef.Child("GameScene").Child(auth.CurrentUser.UserId).Child("Transform").SetRawJsonValueAsync(JsonUtility.ToJson(myPosition));

        GameSceneDistances myDistance = new GameSceneDistances();
        myDistance.nick = DatabaseManager.nick;
        myDistance.distance = Vector3.Distance(player.position, finishObject.position);
        roomRef.Child("GameSceneDistances").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(myDistance));
    }
    private void GetPlayerFromDatabase()
    {
        roomRef.Child("GameScene").GetValueAsync().ContinueWith(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                int i = 0;
                foreach (DataSnapshot ds in snapshot.Children)
                {
                    Player otherPlayer = JsonUtility.FromJson<Player>(ds.GetRawJsonValue());
                    if (otherPlayer.userId == auth.CurrentUser.UserId)
                        continue;

                    Player.PlayerPosition otherPlayerTransform = JsonUtility.FromJson<Player.PlayerPosition>(ds.Child("Transform").GetRawJsonValue());
                    
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
        roomRef.Child("GameSceneDistances").OrderByChild("distance").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                int i = 0;
                foreach (DataSnapshot ds in snapshot.Children)
                {
                    GameSceneDistances playerDistance = JsonUtility.FromJson<GameSceneDistances>(ds.GetRawJsonValue());
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
    public string userId;
    public string nick;
    public float distance;
}
public class GameScenePlaces
{
    public int finishedPlayerCount;
}