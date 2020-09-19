using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Unity.Editor;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField]
    private enum MoneyType
    {
        Coin, Crystal
    }
    [SerializeField] private MoneyType redBallMoneyType = new MoneyType();
    [SerializeField] private int redBallPrice = 50;
    [SerializeField] private MoneyType blueBallMoneyType = new MoneyType();
    [SerializeField] private int blueBallPrice = 50;
    [SerializeField] private MoneyType greenBallMoneyType = new MoneyType();
    [SerializeField] private int greenBallPrice = 50;
    [SerializeField] private MoneyType yellowBallMoneyType = new MoneyType();
    [SerializeField] private int yellowBallPrice = 50;

    [SerializeField] private GameObject SureMenu = null;
    [SerializeField] private Sprite coinSprite = null;
    [SerializeField] private Sprite crystalSprite = null;
    [SerializeField] private Image redBallPriceImage = null;
    [SerializeField] private Image blueBallPriceImage = null;
    [SerializeField] private Image greenBallPriceImage = null;
    [SerializeField] private Image yellowBallPriceImage = null;

    [SerializeField] private Text redBallPriceText = null;
    [SerializeField] private Text blueBallPriceText = null;
    [SerializeField] private Text greenBallPriceText = null;
    [SerializeField] private Text yellowBallPriceText = null;

    [SerializeField] private Button redBallButton = null;
    [SerializeField] private Button blueBallButton = null;
    [SerializeField] private Button greenBallButton = null;
    [SerializeField] private Button yellowBallButton = null;

    [SerializeField] private Text exceptionText = null;
    [SerializeField] private Text crystalText = null;
    [SerializeField] private Text coinText = null;

    private Color buyingColor = new Color();
    private MoneyType buyingMoneyType = 0;
    private int buyingPrice = 0;

    private FirebaseAuth auth;
    private DatabaseReference userDataRef;

    public static ShopManager shopManagerClass;
    
    //Bu script MainMenu sahnesindeki ScriptObject 'e atanmıştır.
    private void Awake()
    {
        //Aktif olan kullanıcıya ait Database referans alınır.
        shopManagerClass = this;

        auth = FirebaseAuth.DefaultInstance;
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        userDataRef = FirebaseDatabase.DefaultInstance.GetReference("UserDatas");
    }
    private void Start()
    {
        //Editörden alınan BallPrice'lar ilgili textlere girilir.
        redBallPriceText.text = redBallPrice.ToString();
        blueBallPriceText.text = blueBallPrice.ToString();
        greenBallPriceText.text = greenBallPrice.ToString();
        yellowBallPriceText.text = yellowBallPrice.ToString();

        //Editörden alınan BallMoneyType'a (crystal, coin) ve BallPrice'larına göre ilgili metoda referans gönderilmektedir.
        SetBallPriceImage(redBallPriceImage, redBallMoneyType);
        SetBallPriceImage(blueBallPriceImage, blueBallMoneyType);
        SetBallPriceImage(greenBallPriceImage, greenBallMoneyType);
        SetBallPriceImage(yellowBallPriceImage, yellowBallMoneyType);
    }
    private void SetBallPriceImage(Image refImage, MoneyType moneyType)
    {
        //Referans alınan MoneyType ve refImage'e göre topların spritelerı işlenmektedir.
        if (moneyType == MoneyType.Coin)
            refImage.sprite = coinSprite;
        else
            refImage.sprite = crystalSprite;
    }
    private void BuyBall(int price, MoneyType moneyType, Color ballColor)
    {
        //Referans alınan para birimine, miktarına ve topun rengine göre topun satın alma işlemi başlatılmaktadır.
        userDataRef.Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                buyingMoneyType = moneyType;
                buyingPrice = price;

                UserData user = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                if (moneyType == MoneyType.Coin)
                {
                    //Para coin ise çalışır
                    if (price <= user.coin)
                    {
                        buyingColor = ballColor;
                        SureMenu.SetActive(true);
                    }
                    else
                        exceptionText.text = "You dont have enough coin.";
                }
                else
                {
                    //Para crystal ise çalışır
                    if (price <= user.crystal)
                    {
                        buyingColor = ballColor;
                        SureMenu.SetActive(true);
                    }
                    else
                        exceptionText.text = "You dont have enough crystal.";
                }
            }
        });
    }
    public void UpdateButtons(Color[] inventory, int inventoryLength, Color skin)
    {
        //Satın alma ya da topu kullanma işleminden sonra butonların görünümü düzenlenir.
        for (int i = 0; i < inventoryLength; i++)
        {
            //Kullanıcının envanterinde toplar vardır ve hangi top bulunuyorsa buton "Use" adını almaktadır.
            if (inventory[i] == Color.red)
                redBallButton.GetComponentInChildren<Text>().text = "Use";
            else if (inventory[i] == Color.blue)
                blueBallButton.GetComponentInChildren<Text>().text = "Use";
            else if (inventory[i] == Color.green)
                greenBallButton.GetComponentInChildren<Text>().text = "Use";
            else if (inventory[i] == Color.yellow)
                yellowBallButton.GetComponentInChildren<Text>().text = "Use";
        }
        //Referans alınan skin değişkeni kullanıcının şuan kullandığı skini ifade etmektedir.
        if (skin == Color.red)
            redBallButton.GetComponentInChildren<Text>().text = "Using";
        else if (skin == Color.blue)
            blueBallButton.GetComponentInChildren<Text>().text = "Using";
        else if (skin == Color.green)
            greenBallButton.GetComponentInChildren<Text>().text = "Using";
        else if (skin == Color.yellow)
            yellowBallButton.GetComponentInChildren<Text>().text = "Using";
    }
    private void UseBall(Color ballColor)
    {
        //Referans alınan top rengi kullanıcının skin değişkenine atanır.
        userDataRef.Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                UserData user = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                user.skin = ballColor;
                userDataRef.Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(user));

                //Butonlar tekrardan düzenlenir.
                UpdateButtons(user.inventory,user.inventoryLength,user.skin);
            }
        });
    }
    public void RedBall()
    {
        //Kırmızı topa ait butona tanımlanmıştır.
        if (redBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(redBallPrice, redBallMoneyType, Color.red);
        else
            UseBall(Color.red);
    }
    public void BlueBall()
    {
        //Mavi topa ait butona tanımlanmıştır.
        if (blueBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(blueBallPrice, blueBallMoneyType, Color.blue);
        else
            UseBall(Color.blue);
    }
    public void GreenBall()
    {
        //Yeşil topa ait butona tanımlanmıştır.
        if (greenBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(greenBallPrice, greenBallMoneyType, Color.green);
        else
            UseBall(Color.green);
    }
    public void YellowBall()
    {
        //Sarı topa ait butona tanımlanmıştır.
        if (yellowBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(yellowBallPrice, yellowBallMoneyType, Color.yellow);
        else
            UseBall(Color.yellow);
    }
    public void Yes()
    {
        //Top satın alma sırasında karşımıza çıkan "Are you sure?" menüsündeki "Yes" butouna tanımlanmıştır.
        userDataRef.Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                //Aktif olan kullanıcının envanterine ilgili top dahil edilir.
                UserData user = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                user.inventory[user.inventoryLength] = buyingColor;
                user.inventoryLength++;

                //Aktif olan kullanıcının hesabından BallPrice düşülür.
                if (buyingMoneyType == MoneyType.Coin)
                    user.coin -= buyingPrice;
                else
                    user.crystal -= buyingPrice;

                //Bu işlemlerden sonra kullanıcıya ait değişkenler Database'e ve menüdeki textlere yazılır.
                userDataRef.Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(user));
                UpdateButtons(user.inventory, user.inventoryLength, user.skin);
                crystalText.text = user.crystal.ToString();
                coinText.text = user.coin.ToString();
            }
        });
        SureMenu.SetActive(false);
    }
    public void No()
    {
        //Satın alma işlemi sırasında "Are you sure?" menüsündeki "No" butonuna tanımlanmıştır.
        buyingColor = Color.white;
        SureMenu.SetActive(false);
    }
}