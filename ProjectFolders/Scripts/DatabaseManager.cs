using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Unity.Editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class DatabaseManager : MonoBehaviour
{
    [SerializeField] private InputField nickInput = null;
    [SerializeField] private GameObject createNick = null;
    [SerializeField] private GameObject gameName = null;
    [SerializeField] private GameObject hud = null;
    [SerializeField] private GameObject menuButtons = null;

    [SerializeField] private Text crystalText = null;
    [SerializeField] private Text coinText = null;
    [SerializeField] private Text nickText = null;
    [SerializeField] private Color defaultColor = new Color();

    private FirebaseAuth auth;
    private DatabaseReference referance;

    public static string nick = null;

    //Bu script MainMenu sahnesindeki ScriptObject 'e atanmıştır.
    private void Awake()
    {
        //Temel kullanıcı değişkeni atanır.
        auth = FirebaseAuth.DefaultInstance;
    }
    private void Start()
    {
        //Kullanıcı girişi yok ise LoginScene sahnesine yönlendirilir.
        if (auth.CurrentUser == null)
            SceneManager.LoadScene("LoginScene");
        else
        {
            //Kullanıcı girişi var ise Firebase'e ait Realtime Database linki atanır.
            FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
            referance = FirebaseDatabase.DefaultInstance.RootReference;
            GetDatas();
        }
    }
    private void GetDatas()
    {
        referance.Child("UserDatas").Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
       {
           //Aktif olan kullanıcının ID'sine göre Database'den veriler alınır.
           if (task.IsFaulted)
           {
               Debug.Log("Unknown error");
           }
           else if (task.IsCompleted)
           {
               DataSnapshot snapshot = task.Result;
               if (snapshot.GetRawJsonValue() == null)
               {
                   //Eğer ki kullanıcı yeni oluşturulduysa createNick menüsü aktif edilir.
                   gameName.SetActive(false);
                   createNick.SetActive(true);
               }
               else
               {
                   //Daha önceden oluşturulan bir kullanıcı ise ilgili menüler aktif edilir.
                   createNick.SetActive(false);
                   menuButtons.SetActive(true);
                   hud.SetActive(true);
                   gameName.SetActive(true);
                   UserData normalObject = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());

                   //Database'den alınan kullanıcı değişkenler ana menüdeki textlere yazdırılır.
                   nick = normalObject.nick;
                   nickText.text = normalObject.nick;
                   crystalText.text = normalObject.crystal.ToString();
                   coinText.text = normalObject.coin.ToString();

                   //Shoptaki menü düzenlemeleri ilgili kullanıcının envanterine göre düzenlenir.
                   ShopManager.shopManagerClass.UpdateButtons(normalObject.inventory,normalObject.inventoryLength,normalObject.skin);
               }
           }
       });
    }
    private void CreateUserDatas(string nick)
    {
        //Oluşturulan nick referans alınarak kullanıcı, Database'de oluşturulur.
        UserData emptyObject = new UserData
        {
            nick = nick,
            skin = defaultColor,
            crystal = 50,
            coin = 50,
            inventoryLength = 1
        };
        emptyObject.inventory[0] = defaultColor;

        //UserData class'ına göre oluşturulan kullanıcı Database'de yazdırılır.
        string emptyJson = JsonUtility.ToJson(emptyObject);
        referance.Child("UserDatas").Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(emptyJson);
    }
    public void CreateNick()
    {
        //Bu metot createNick menüsündeki nick oluştur butonuna tanımlanmıştır.
        if(nickInput.text != null && nickInput.text != "")
        {
            CreateUserDatas(nickInput.text);
            GetDatas();
        }
        else
            return;
    }
    public void LogOut()
    {
        //Cihazda aktif olan kullanıcının çıkışı yapılır.
        auth.SignOut();
        SceneManager.LoadScene("LoginScene");
    }
}
public class UserData
{
    public string nick;
    public int crystal;
    public int coin;
    public int inventoryLength;
    public Color skin;
    public Color[] inventory = new Color[5];
}