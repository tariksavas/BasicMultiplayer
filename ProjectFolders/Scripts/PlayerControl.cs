using UnityEngine;
public class PlayerControl : MonoBehaviour
{
    [SerializeField] private float playerSpeed = 2;
    [SerializeField] private float JumpForce = 10;

    [SerializeField] private Joystick joystick = null;

    private Rigidbody rb;

    private bool canJump = false;
    private bool jump = false;

    //Bu script oyun sahnesindeki Player'a atanmıştır.
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        float movementJump = 0;
        if (jump && canJump)
        {
            //Oyuncu zıplarken ses oynatılır ve ilgili değişkene değer atanır.
            GameManager.gameManagerClass.JumpAudio();
            movementJump = JumpForce;
            canJump = false;
        }

        float movementHorizontal = joystick.Horizontal;
        float movementVertical = joystick.Vertical;

        Vector3 movement = new Vector3(movementHorizontal, movementJump, movementVertical);
        rb.AddForce(movement * playerSpeed);
        jump = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        //Oyuncu zemine değdiğinde tekrar zıplayabilir.
        if (other.tag == "ground")
            canJump = true;
    }
    public void JumpButton()
    {
        //Oyuncu zıplama butonuna bastığında çalışır.
        jump = true;
    }
}