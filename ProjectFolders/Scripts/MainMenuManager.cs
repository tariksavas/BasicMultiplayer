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
    public void QuickPlay()
    {
        SceneManager.LoadScene("GameSceneSingle");
    }
    public void Play()
    {
        playButton.image.color = selectedColor;
        shopButton.image.color = normalColor;
        playMenu.SetActive(true);
        shopMenu.SetActive(false);
        gameName.SetActive(false);
    }
    public void CreateRoom()
    {
        createRoomMenu.SetActive(true);
        joinRoomMenu.SetActive(false);
        playButtons.SetActive(false);
    }
    public void JoinRoom()
    {
        joinRoomMenu.SetActive(true);
        createRoomMenu.SetActive(false);
        playButtons.SetActive(false);
    }
    public void Back()
    {
        joinRoomMenu.SetActive(false);
        createRoomMenu.SetActive(false);
        playButtons.SetActive(true);
    }
    public void Shop()
    {
        shopButton.image.color = selectedColor;
        playButton.image.color = normalColor;
        shopMenu.SetActive(true);
        playMenu.SetActive(false);
        gameName.SetActive(false);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}