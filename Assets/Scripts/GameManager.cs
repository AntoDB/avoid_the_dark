using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    static GameManager instance;

    public UnityEvent OnPlayerJump;
    public UnityEvent<float, float> OnPlayerMove;
    public UnityEvent OnPlayerHit;

    private void Start()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
    }

    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        OnPlayerMove.Invoke(horizontalInput, verticalInput);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnPlayerJump.Invoke();
        }
    }
}
