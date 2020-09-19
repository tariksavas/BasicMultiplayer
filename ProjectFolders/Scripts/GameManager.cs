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
    [SerializeField] private GameObject[] itemsPrefab = null;
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

    //Bu script oyun sahnesindeki Player objesine atanmıştır.
    private void Start()
    {
        //Eğer ki oyuncu bir odada değilse direkt yere düşerek başlamalı, değilse itemler sahnede oluşturulmalı.
        gameManagerClass = this;
        if (!RoomManager.roomManagerClass.inRoom)
            GetComponent<Rigidbody>().useGravity = true;
        else
            SpawnItems();

        heartText.text = "Heart: " + heartCount.ToString();
    }
    private void Update()
    {
        //Oyuncu single bir oyunda ve oyun hala devam etmekte ise sayaç saymaya devam eder.
        if (!RoomManager.roomManagerClass.inRoom && !finishMenuSingleplayer.activeSelf)
            completionTimer += Time.deltaTime;

        //Oyunu duraklatma.
        if (Input.GetKeyDown(KeyCode.Escape))
            escapeMenu.SetActive(true);

        //Mesafe ilgili texte sürekli yazdırılmaktadır.
        distanceText.text = (int)Vector3.Distance(transform.position, finishObject.transform.position) + " m";

        if (showParticle)
        {
            //Oyun bittiğinde confetti patlaması sonrasında oyuncu single bir oyunda olup olmadığına göre ilgili menü sahneye getirilir.
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
                    //Harcanan can miktarı, kazanılan coin ve energy miktarı ilgili metoda gönderilir.
                    MultiplayerManager.multiplayerManagerClass.EndGameStatement(heartCount, earnedCoinCount, earnedCrystalCount);
                    finishMenuMultiplayer.SetActive(true);
                }
            }
        }
    }
    private void SpawnItems()
    {
        //Sahneye coin ve energy itemlerinin oluşmasını sağlar.
        for(int i = 0; i < maxItemCount; i++)
        {
            int randForItem = Random.Range(0, itemsPrefab.Length + 1);
            int randForPosition = Random.Range(0, itemPositions.Length - 1);

            int randX = Random.Range(Mathf.RoundToInt(itemPositions[randForPosition].position.x), Mathf.RoundToInt(itemPositions[randForPosition + 1].position.x));
            int randZ = Random.Range(Mathf.RoundToInt(itemPositions[randForPosition].position.z), Mathf.RoundToInt(itemPositions[randForPosition + 1].position.z));

            //"randForItem" değişkeni 0 değerini üretirse  item oluşturulmayacak.
            if (randForItem == 0)
                continue;

            //Bu değişken 1 değerini üretirse random elde edilen x ve z pozisyonlarına göre coin oluşturulacak.
            else if(randForItem == 1)
            {
                GameObject coinCopy = Instantiate(itemsPrefab[0], itemsParentObject.transform);
                coinCopy.transform.position = new Vector3(randX, -7, randZ);
            }

            //Bu değişken 2 değerini üretirse random elde edilen x ve z pozisyonlarına göre energy oluşturulacak.
            else
            {
                GameObject energyCopy = Instantiate(itemsPrefab[1], itemsParentObject.transform);
                energyCopy.transform.position = new Vector3(randX, -7, randZ);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == finishObject)
        {
            //Finish objesine geldiğimizde çalışır.
            confettiObject.SetActive(true);
            showParticle = true;
            StopBall();
            Destroy(gameObject.GetComponent<PlayerControl>());
            Destroy(confettiObject, confettiShowingTime);

            //Oyuncu single bir oyunda ise bot sahneden kaldırılır.
            if (!RoomManager.roomManagerClass.inRoom)
                Destroy(computerObject);
        }
        else if(other.gameObject == MapBottomCollider)
        {
            //Oyuncu yere düştüğünde sahnenin altındaki görünmez bir triggera çarptığında çalışır.
            StopBall();
            heartCount--;

            //Oyuncunun hala canı varsa tekrardan baştan başlatılır.
            if(heartCount > 0)
            {
                transform.position = new Vector3(0, 0, 0);
                heartText.text = "Heart: " + heartCount.ToString();
            }
            else
                gameOverMenu.SetActive(true);
        }
        else if(other.tag == "coin")
        {
            //Oyuncu coin objesine çarptığında ilgili ses ve coin miktarı güncellenir.
            playerAudio.clip = coinClip;
            playerAudio.Play();
            Destroy(other.gameObject);
            earnedCoinCount++;
        }
        else if(other.tag == "crystal")
        {
            //Oyuncu energy objesine çarptığında ilgili ses ve energy miktarı güncellenir.
            playerAudio.clip = coinClip;
            playerAudio.Play();
            Destroy(other.gameObject);
            earnedCrystalCount++;
        }
    }
    public void JumpAudio()
    {
        //Oyuncu zıpladığında "PlayerControl" scriptinden bu metot çağrılır.
        playerAudio.clip = jumpClip;
        playerAudio.Play();
    }
    public void LeaveMenu()
    {
        //LeaveMenu butonuna tanımlanmıştır.
        if (RoomManager.roomManagerClass.inRoom)
            RoomManager.roomManagerClass.LeaveRoomWhenPlaying();

        SceneManager.LoadScene("MainMenu");
    }
    public void ContinueGame()
    {
        //Oyun durdurulduğunda gelen menüde herhangi bir yere dokunduğunda menü kapatılır.
        escapeMenu.SetActive(false);
    }
    private void StopBall()
    {
        //Oyuncunun üzerindeki fiziksel etkiler kaldırılır.
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    private void EnGameStatement(int heartCount)
    {
        //Oyuncu single bir oyundaysa menüye gerekli detaylar yazdırılır.
        finishCompletionTimeText.text = completionTimer + " seconds" ;
        finishRemainingHeartText.text = heartCount.ToString();
    }
}