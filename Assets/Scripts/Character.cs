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

    private int nbInSpot = 0;

    private Mood mood;

    private float mentalHealth = 1;

    float MentalHealth
    {
        set
        {
            mentalHealth = value;
            GameManager.instance.step = (FearStep)((mentalHealth / 0.2) + 1);

        }
        get { return mentalHealth; }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mood = Mood.Nice;
    }

    private void Update()
    {
        Velocity = rb.linearVelocity.y;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    public void movementAxis(float horizontalInput, float verticalInput)
    {
        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput);
        Speed = movement.magnitude;

        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            float rotationSpeed = 10f; 
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        transform.Translate(movement * moveSpeed * Time.deltaTime, Space.World);
    }

    public void jump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    public void UpdateMentalHealth()
    {
        Mood localMood = Mood.None;
        if (nbInSpot > 0)
        {
            localMood = Mood.Nice;
            if(mentalHealth <= 0.98f)
                MentalHealth += 0.02f;
        }
        else if(nbInSpot == 0)
        {
            localMood = Mood.Fear;
            if(mentalHealth > 0)
                MentalHealth -= 0.01f;
        }
        changeAnimationMood(localMood);
    }

    private void changeAnimationMood(Mood localMood)
    {
        if (!localMood.Equals(mood))
        {
            mood = localMood;
            switch (localMood)
            {
                case Mood.Nice:
                    animator.SetTrigger("Replay");
                    break;
                case Mood.Fear:
                    animator.SetTrigger("Scared");
                    break;
            }
        }
    } 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Light") && other.GetComponent<Light>().intensity > 0)
        {
            nbInSpot++;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Light") && nbInSpot>0)
        {
            nbInSpot--;
        }
    }

}
