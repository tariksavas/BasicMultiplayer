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
    private void Awake()
    {
        shopManagerClass = this;

        auth = FirebaseAuth.DefaultInstance;
        FirebaseApp.DefaultInstance.SetEditorDatabaseUrl("https://caycomtech-d31a3.firebaseio.com/");
        userDataRef = FirebaseDatabase.DefaultInstance.GetReference("UserDatas");
    }
    private void Start()
    {
        redBallPriceText.text = redBallPrice.ToString();
        blueBallPriceText.text = blueBallPrice.ToString();
        greenBallPriceText.text = greenBallPrice.ToString();
        yellowBallPriceText.text = yellowBallPrice.ToString();

        SetBallPriceImage(redBallPriceImage, redBallMoneyType);
        SetBallPriceImage(blueBallPriceImage, blueBallMoneyType);
        SetBallPriceImage(greenBallPriceImage, greenBallMoneyType);
        SetBallPriceImage(yellowBallPriceImage, yellowBallMoneyType);
    }
    private void SetBallPriceImage(Image refImage, MoneyType moneyType)
    {
        if (moneyType == MoneyType.Coin)
            refImage.sprite = coinSprite;
        else
            refImage.sprite = crystalSprite;
    }
    private void BuyBall(int price, MoneyType moneyType, Color ballColor)
    {
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
        for (int i = 0; i < inventoryLength; i++)
        {
            if (inventory[i] == Color.red)
                redBallButton.GetComponentInChildren<Text>().text = "Use";
            else if (inventory[i] == Color.blue)
                blueBallButton.GetComponentInChildren<Text>().text = "Use";
            else if (inventory[i] == Color.green)
                greenBallButton.GetComponentInChildren<Text>().text = "Use";
            else if (inventory[i] == Color.yellow)
                yellowBallButton.GetComponentInChildren<Text>().text = "Use";
        }
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
        userDataRef.Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                UserData user = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                user.skin = ballColor;
                userDataRef.Child(auth.CurrentUser.UserId).SetRawJsonValueAsync(JsonUtility.ToJson(user));
                UpdateButtons(user.inventory,user.inventoryLength,user.skin);
            }
        });
    }
    public void RedBall()
    {
        if (redBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(redBallPrice, redBallMoneyType, Color.red);
        else
            UseBall(Color.red);
    }
    public void BlueBall()
    {
        if(blueBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(blueBallPrice, blueBallMoneyType, Color.blue);
        else
            UseBall(Color.blue);
    }
    public void GreenBall()
    {
        if(greenBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(greenBallPrice, greenBallMoneyType, Color.green);
        else
            UseBall(Color.green);
    }
    public void YellowBall()
    {
        if(yellowBallButton.GetComponentInChildren<Text>().text == "Buy")
            BuyBall(yellowBallPrice, yellowBallMoneyType, Color.yellow);
        else
            UseBall(Color.yellow);
    }
    public void Yes()
    {
        userDataRef.Child(auth.CurrentUser.UserId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            DataSnapshot snapshot = task.Result;
            if (snapshot.GetRawJsonValue() != null)
            {
                UserData user = JsonUtility.FromJson<UserData>(snapshot.GetRawJsonValue());
                user.inventory[user.inventoryLength] = buyingColor;
                user.inventoryLength++;

                if (buyingMoneyType == MoneyType.Coin)
                    user.coin -= buyingPrice;
                else
                    user.crystal -= buyingPrice;

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
        buyingColor = Color.white;
        SureMenu.SetActive(false);
    }
}