using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public FearStep step;

    public UnityEvent OnPlayerJump;
    public UnityEvent<float, float> OnPlayerMove;
    public UnityEvent OnPlayerFear;
    public UnityEvent<Vector3> OnMonsterListener;

    private void Start()
    {
        if (instance != null)
            Destroy(this);
        else
            instance = this;
        step = FearStep.None;
        StartCoroutine(updateMentalHealth());
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

    private IEnumerator updateMentalHealth()
    {
        OnPlayerFear.Invoke();
        yield return new WaitForSeconds(1);
        StartCoroutine(updateMentalHealth());
    }

    public void monsterListener(Vector3 position)
    {
        OnMonsterListener.Invoke(position);
    }
}
