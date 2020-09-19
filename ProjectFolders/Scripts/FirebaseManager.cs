using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class FirebaseManager : MonoBehaviour
{
    [SerializeField] private InputField loginEmail = null;
    [SerializeField] private InputField loginPassword = null;

    [SerializeField] private InputField registerEmail = null;
    [SerializeField] private InputField registerPassword = null;
    [SerializeField] private InputField registerPasswordAgain = null;

    [SerializeField] private Text exceptionText = null;
    [SerializeField] private float exceptionTextWaitTime = 3;
    private float timer = 0;

    private FirebaseAuth auth;

    //Bu script Login sahnesindeki ScriptObject 'e atanmıştır.
    private void Awake()
    {
        //Firebasede temel kullanılıcı işlemleri için kullanılan bir değişken
        auth = FirebaseAuth.DefaultInstance;
    }
    private void Start()
    {
        //Cihazda hesaplarla alakalı olaylar listener ile dinleniyor.
        exceptionText.text = null;
        auth.StateChanged += AuthStateChange;

        if (auth.CurrentUser != null)
            SceneManager.LoadScene("MainMenu");
    }
    private void FixedUpdate()
    {
        if(exceptionText.text != "")
        {
            //Uyarı textinde herhangi bir uyarı var ise bir süre sonra silinir.
            timer += Time.deltaTime;
            if (timer >= exceptionTextWaitTime)
            {
                exceptionText.text = "";
                timer = 0;
            }
        }
    }
    private void AuthStateChange(object sender, System.EventArgs eventArgs)
    {
        //Oyun başlatıldığında kullanıcı aktif ise direkt mainmenu sahnesine yönlendirilir.
        if (auth.CurrentUser != null)
            SceneManager.LoadScene("MainMenu");
    }
    public void Register()
    {
        if (CheckRegisterFields())
        {
            //Inputlarda bir sorun yok ise inputlardaki değerler baz alınarak kayıt işlemi gerçekleştirilir.
            auth.CreateUserWithEmailAndPasswordAsync(registerEmail.text, registerPassword.text).ContinueWith(task =>
             {
                 if (task.IsCanceled)
                 {
                     exceptionText.text = "Registration canceled.";
                     return;
                 }
                 else if (task.IsFaulted)
                 {
                     foreach (var exception in task.Exception.Flatten().InnerExceptions)
                         exceptionText.text = exception.Message;

                     return;
                 }
                 FirebaseUser newUser = task.Result;
             });
        }
    }
    private bool CheckRegisterFields()
    {
        //Register işlemi sırasında inputlarda bir sorun var ise false değeri döndürür.
        if (registerEmail.text == null || registerEmail.text == "")
        {
            exceptionText.text = "Please enter email.";
            return false;
        }
        else if (registerPassword.text == null || registerPassword.text == "")
        {
            exceptionText.text = "Please enter password.";
            return false;
        }
        else if(registerPasswordAgain.text == null || registerPasswordAgain.text == "")
        {
            exceptionText.text = "Please enter password again.";
            return false;
        }
        else if (registerPassword.text != registerPasswordAgain.text)
        {
            exceptionText.text = "Passwords are not the same.";
            return false;
        }

        return true;
    }
    public void Login()
    {
        if (CheckLoginFields())
        {
            //Inputlarda bir sorun yok ise inputlardaki değerler baz alınarak giriş işlemi yapılır.
            auth.SignInWithEmailAndPasswordAsync(loginEmail.text, loginPassword.text).ContinueWith(task =>
            {
                if (task.IsCanceled)
                 {
                     exceptionText.text = "Login canceled.";
                     return;
                 }
                 else if (task.IsFaulted)
                 {
                     foreach (var exception in task.Exception.Flatten().InnerExceptions)
                         exceptionText.text = exception.Message;

                     return;
                 }
                 FirebaseUser newUser = task.Result;
             });
        }
    }
    private bool CheckLoginFields()
    {
        //Login işlemi sırasında inputlarda bir sorun var ise false değeri döndürür.
        if (loginEmail.text == null || loginEmail.text == "")
        {
            exceptionText.text = "Please enter email.";
            return false;
        }
        else if (loginPassword.text == null || loginPassword.text == "")
        {
            exceptionText.text = "Please enter password.";
            return false;
        }
        return true;
    }
    public void GuestLogin()
    {
        //Guest login girişi sağlanır.
        FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            FirebaseUser newUser = task.Result;
        });
    }
}