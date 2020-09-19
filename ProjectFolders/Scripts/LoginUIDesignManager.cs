using UnityEngine;
using UnityEngine.UI;
public class LoginUIDesignManager : MonoBehaviour
{
    [SerializeField] private GameObject registerMenu = null;
    [SerializeField] private GameObject loginMenu = null;
    [SerializeField] private GameObject guestMenu = null;
    [SerializeField] private GameObject gameName = null;
    [SerializeField] private Button registerButton = null;
    [SerializeField] private Button loginButton = null;
    [SerializeField] private Button guestButton = null;
    [SerializeField] private Color normalColor = new Color();
    [SerializeField] private Color selectedColor = new Color();

    //Bu script Login sahnesindeki ScriptObject 'e atanmıştır.
    private void setInactive()
    {
        //Login menüsündeki 3 butondan birine basıldığında bu metot çağrılır.
        //Tüm buton ve menülerin görünümünü resetlemektedir.
        gameName.SetActive(false);

        registerMenu.SetActive(false);
        loginMenu.SetActive(false);
        guestMenu.SetActive(false);

        registerButton.image.color = normalColor;
        loginButton.image.color = normalColor;
        guestButton.image.color = normalColor;
    }
    public void RegisterB()
    {
        //Register butonuna basıldığında çalışır
        setInactive();
        registerButton.image.color = selectedColor;
        registerMenu.SetActive(true);
    }
    public void LoginB()
    {
        //Login butonuna basıldığında çalışır
        setInactive();
        loginButton.image.color = selectedColor;
        loginMenu.SetActive(true);
    }
    public void GuestB()
    {
        //Guest login butonuna basıldığında çalışır
        setInactive();
        guestButton.image.color = selectedColor;
        guestMenu.SetActive(true);
    }
}