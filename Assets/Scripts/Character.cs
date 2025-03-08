using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    public float Velocity { set { animator.SetFloat("Velocity", value); } get { return animator.GetFloat("Velocity"); } }
    public float Speed { set { animator.SetFloat("Speed", value); } get { return animator.GetFloat("Speed"); } }

    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        Velocity = rb.linearVelocity.y;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Vérifier si le personnage touche le sol
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    public void movementAxis(float horizontalInput, float verticalInput)
    {
        // Calcul du vecteur de mouvement
        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput);
        Speed = movement.magnitude;
        Debug.Log(Speed);
        // Application du mouvement
        transform.Translate(movement * moveSpeed * Time.deltaTime);
    }

    public void jump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }
}
