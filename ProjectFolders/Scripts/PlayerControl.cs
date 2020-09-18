using UnityEngine;
using UnityEngine.UI;
public class PlayerControl : MonoBehaviour
{
    [SerializeField] private float playerSpeed = 2;
    [SerializeField] private float JumpForce = 10;
    [SerializeField] private bool canJump = false;
    [SerializeField] private bool jump = false;
    [SerializeField] private Joystick joystick = null;
    private Rigidbody rb;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        float movementJump = 0;
        if (jump && canJump)
        {
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
        if (other.tag == "ground")
            canJump = true;
    }
    public void JumpButton()
    {
        jump = true;
    }
}