using UnityEngine;

public class TriggerFloorIsLava : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.monsterListener(other.transform.position);
        }
    }
}
