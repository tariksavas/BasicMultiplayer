using UnityEngine;
public class ComputerControl : MonoBehaviour
{
    [SerializeField] private int heartCount = 3;
    [SerializeField] private float jumpForce = 3.2f;
    [SerializeField] private float playerSpeed = 10;

    [SerializeField] private Transform[] targetPos = null;
    [SerializeField] private GameObject MapBottomCollider = null;
    [SerializeField] private GameObject finishObject = null;
    [SerializeField] private GameObject gameOverMenu = null;

    private Rigidbody rb = null;
    private int index = 0;
    private bool jump = false;

    //Bu script Singleplayer oyundaki bota aktarılmıştır.
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        float movementJump = 0;
        if (jump)
        {
            //Botun zıplaması tetiklendiğinde hedef pozisyona olan uzaklığına göre botun zıplama gücü değişmektedir.
            jump = false;
            float distance = Vector3.Distance(targetPos[index].position, transform.position);
            movementJump = distance * jumpForce;
        }

        //Botun sonraki hedefe olan uzaklığı ilgili değişkenlere tanımlanmaktadır.
        float xDistance = Mathf.Round(targetPos[index].position.x) - Mathf.Round(transform.position.x);
        float zDistance = Mathf.Round(targetPos[index].position.z) - Mathf.Round(transform.position.z);

        //Eğer ki bot hedefe ulaştıysa üzerindeki etkiler kaldırılır ve sonraki indis hedef alınır.
        if (xDistance == 0 && zDistance == 0)
        {
            StopBall();
            if (index != targetPos.Length - 1)
                index++;
        }
        //Botun hedefe olan x ve z pozisyonlarındaki mesafeler hesaplanmaktadır.
        float horizontalDistance = Mathf.Abs(targetPos[index].position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(targetPos[index].position.z - transform.position.z);

        //Botun x ve z yönlerindeki kuvvet toplamı 1 olmalıdır. (Playerdaki Joystick gibi)
        float movementHorizontal = horizontalDistance / (horizontalDistance + verticalDistance);
        float movementVertical = verticalDistance / (horizontalDistance + verticalDistance);

        //Botun hedefe olan mesafesindeki yönüne göre hareket kuvveti işaret değiştirmektedir.
        if (targetPos[index].position.x - transform.position.x < 0)
            movementHorizontal = -movementHorizontal;
        if (targetPos[index].position.z - transform.position.z < 0)
            movementVertical = -movementVertical;

        //Botun x, y ve z yönlerindeki kuvveti hesaplandıktan sonra uygulanmaktadır.
        Vector3 movement = new Vector3(movementHorizontal, movementJump, movementVertical);
        rb.AddForce(movement * playerSpeed);
    }
    private void StopBall()
    {
        //Bota ait fiziksel kuvvetler sıfırlanmaktadır.
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    private void OnTriggerEnter(Collider other)
    {
        //Singleplayer sahnede gerekli yerlerinde botun zıplamasını tetikleyecek triggerlar bulunmaktadır.
        if(other.tag == "jumper")
            jump = true;

        //Bot sahneden aşağı düştüğünde hakkı varsa tekrar baştan başlatılmaktadır.
        else if(other.gameObject == MapBottomCollider)
        {
            StopBall();
            heartCount--;
            if (heartCount > 0)
            {
                index = 0;
                transform.position = new Vector3(0, -7.25f, 0);
            }
            else
                Destroy(gameObject);
        }

        //Bot oyunu bitirdiğinde aktif oyuncu oyunu kaybetmektedir.
        else if(other.gameObject == finishObject)
        {
            Destroy(gameObject);
            gameOverMenu.SetActive(true);
        }
    }
}