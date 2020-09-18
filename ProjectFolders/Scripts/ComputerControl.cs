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
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        float movementJump = 0;
        if (jump)
        {
            jump = false;
            float distance = Vector3.Distance(targetPos[index].position, transform.position);
            movementJump = distance * jumpForce;
        }

        float movementHorizontal;
        float movementVertical;

        float xDistance = Mathf.Round(targetPos[index].position.x) - Mathf.Round(transform.position.x);
        float zDistance = Mathf.Round(targetPos[index].position.z) - Mathf.Round(transform.position.z);

        if (xDistance == 0 && zDistance == 0)
        {
            StopBall();
            if (index != targetPos.Length - 1)
                index++;
        }
        float horizontalDistance = Mathf.Abs(targetPos[index].position.x - transform.position.x);
        float verticalDistance = Mathf.Abs(targetPos[index].position.z - transform.position.z);

        movementHorizontal = horizontalDistance / (horizontalDistance + verticalDistance);
        movementVertical = verticalDistance / (horizontalDistance + verticalDistance);

        if (targetPos[index].position.x - transform.position.x < 0)
            movementHorizontal = -movementHorizontal;
        if (targetPos[index].position.z - transform.position.z < 0)
            movementVertical = -movementVertical;

        Debug.Log(movementHorizontal);
        Debug.Log(movementVertical);
        Vector3 movement = new Vector3(movementHorizontal, movementJump, movementVertical);
        rb.AddForce(movement * playerSpeed);
    }
    private void StopBall()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "jumper")
            jump = true;
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
        else if(other.gameObject == finishObject)
        {
            Destroy(gameObject);
            gameOverMenu.SetActive(true);
        }
    }
}