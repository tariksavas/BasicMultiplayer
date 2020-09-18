using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int heartCount = 3;
    [SerializeField] private int maxItemCount = 10;

    [SerializeField] private int confettiShowingTime = 3;

    [SerializeField] private Text finishCompletionTimeText = null;
    [SerializeField] private Text finishRemainingHeartText = null;

    [SerializeField] private Text distanceText = null;
    [SerializeField] private Text heartText = null;

    [SerializeField] private Transform[] itemPositions = null;
    [SerializeField] private GameObject coinPrefab = null;
    [SerializeField] private GameObject crystalPrefab = null;
    [SerializeField] private GameObject itemsParentObject = null;

    [SerializeField] private GameObject computerObject = null;

    [SerializeField] private GameObject finishObject = null;
    [SerializeField] private GameObject MapBottomCollider = null;

    [SerializeField] private GameObject finishMenuSingleplayer = null;
    [SerializeField] private GameObject finishMenuMultiplayer = null;
    [SerializeField] private GameObject gameOverMenu = null;
    [SerializeField] private GameObject escapeMenu = null;
    [SerializeField] private GameObject confettiObject = null;

    [SerializeField] private AudioClip coinClip = null;
    [SerializeField] private AudioClip jumpClip = null;
    [SerializeField] private AudioSource playerAudio = null;

    private int earnedCrystalCount = 0;
    private int earnedCoinCount = 0;
    private float completionTimer = 0;
    private float particleTimer = 0;
    private bool showParticle = false;

    public static GameManager gameManagerClass;
    private void Start()
    {
        gameManagerClass = this;
        if (!RoomManager.roomManagerClass.inRoom)
            GetComponent<Rigidbody>().useGravity = true;
        else
            SpawnItems();

        heartText.text = "Heart: " + heartCount.ToString();
    }
    private void Update()
    {
        if (!RoomManager.roomManagerClass.inRoom && !finishMenuSingleplayer.activeSelf)
            completionTimer += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Escape))
            escapeMenu.SetActive(true);

        distanceText.text = (int)Vector3.Distance(transform.position, finishObject.transform.position) + " m";

        if (showParticle)
        {
            particleTimer += Time.deltaTime;
            if(particleTimer >= confettiShowingTime)
            {
                showParticle = false;
                particleTimer = 0;
                if (!RoomManager.roomManagerClass.inRoom)
                {
                    EnGameStatement(heartCount);
                    finishMenuSingleplayer.SetActive(true);
                }
                else
                {
                    MultiplayerManager.multiplayerManagerClass.EndGameStatement(heartCount, earnedCoinCount, earnedCrystalCount);
                    finishMenuMultiplayer.SetActive(true);
                }
            }
        }
    }
    private void SpawnItems()
    {
        for(int i = 0; i < maxItemCount; i++)
        {
            int randForItem = Random.Range(0, 3);
            int randForPosition = Random.Range(0, 4);

            int randX = Random.Range(Mathf.RoundToInt(itemPositions[randForPosition].position.x), Mathf.RoundToInt(itemPositions[randForPosition + 1].position.x));
            int randZ = Random.Range(Mathf.RoundToInt(itemPositions[randForPosition].position.z), Mathf.RoundToInt(itemPositions[randForPosition + 1].position.z));

            if (randForItem == 0)
                continue;
            else if(randForItem == 1)
            {
                GameObject coinCopy = Instantiate(coinPrefab, itemsParentObject.transform);
                coinCopy.transform.position = new Vector3(randX, -7, randZ);
            }
            else
            {
                GameObject energyCopy = Instantiate(crystalPrefab, itemsParentObject.transform);
                energyCopy.transform.position = new Vector3(randX, -7, randZ);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == finishObject)
        {
            StopBall();
            Destroy(gameObject.GetComponent<PlayerControl>());
            confettiObject.SetActive(true);
            Destroy(confettiObject, confettiShowingTime);
            showParticle = true;

            if (!RoomManager.roomManagerClass.inRoom)
            {
                Destroy(computerObject);
            }
        }
        else if(other.gameObject == MapBottomCollider)
        {
            StopBall();
            heartCount--;
            if(heartCount > 0)
            {
                transform.position = new Vector3(0, 0, 0);
                heartText.text = "Heart: " + heartCount.ToString();
            }
            else
            {
                gameOverMenu.SetActive(true);
            }
        }
        else if(other.tag == "coin")
        {
            playerAudio.clip = coinClip;
            playerAudio.Play();
            Destroy(other.gameObject);
            earnedCoinCount++;
        }
        else if(other.tag == "crystal")
        {
            playerAudio.clip = coinClip;
            playerAudio.Play();
            Destroy(other.gameObject);
            earnedCrystalCount++;
        }
    }
    public void JumpAudio()
    {
        playerAudio.clip = jumpClip;
        playerAudio.Play();
    }
    public void LeaveMenu()
    {
        if (RoomManager.roomManagerClass.inRoom)
            RoomManager.roomManagerClass.LeaveRoomWhenPlaying();

        SceneManager.LoadScene("MainMenu");
    }
    public void ContinueGame()
    {
        escapeMenu.SetActive(false);
    }
    private void StopBall()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    private void EnGameStatement(int heartCount)
    {
        finishCompletionTimeText.text = completionTimer + " seconds" ;
        finishRemainingHeartText.text = heartCount.ToString();
    }
}