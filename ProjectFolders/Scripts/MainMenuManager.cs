using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject gameName = null;
    [SerializeField] private GameObject shopMenu = null;
    [SerializeField] private GameObject playMenu = null;
    [SerializeField] private GameObject createRoomMenu = null;
    [SerializeField] private GameObject joinRoomMenu = null;
    [SerializeField] private GameObject playButtons = null;
    [SerializeField] private Button shopButton = null;
    [SerializeField] private Button playButton = null;
    [SerializeField] private Color normalColor = new Color();
    [SerializeField] private Color selectedColor = new Color();

    //Bu script MainMenu sahnesindeki ScriptObject 'e atanmıştır.
    public void QuickPlay()
    {
        //Ana menüdeki Quick play butonuna tanımlanmıştır.
        SceneManager.LoadScene("GameSceneSingle");
    }
    public void Play()
    {
        //Ana menüdeki play butonuna tanımlanmıştır.
        playButton.image.color = selectedColor;
        shopButton.image.color = normalColor;
        playMenu.SetActive(true);
        shopMenu.SetActive(false);
        gameName.SetActive(false);
    }
    public void CreateRoom()
    {
        //Ana menüdeki play menüsünde bulunan Create room butonuna tanımlanmıştır.
        createRoomMenu.SetActive(true);
        joinRoomMenu.SetActive(false);
        playButtons.SetActive(false);
    }
    public void JoinRoom()
    {
        //Ana menüdeki play menüsünde bulunan Join room butonuna tanımlanmıştır.
        joinRoomMenu.SetActive(true);
        createRoomMenu.SetActive(false);
        playButtons.SetActive(false);
    }
    public void Back()
    {
        //Ana menüdeki play menüsünde bulunan Back butonlarına tanımlanmıştır.
        joinRoomMenu.SetActive(false);
        createRoomMenu.SetActive(false);
        playButtons.SetActive(true);
    }
    public void Shop()
    {
        //Ana menüdeki Shop butonuna tanımlanmıştır.
        shopButton.image.color = selectedColor;
        playButton.image.color = normalColor;
        shopMenu.SetActive(true);
        playMenu.SetActive(false);
        gameName.SetActive(false);
    }
    public void QuitGame()
    {
        //Ana menüdeki Quit game butonuna tanımlanmıştır.
        Application.Quit();
    }
}