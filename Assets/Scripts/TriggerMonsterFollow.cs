using UnityEngine;

public class TriggerMonsterFollow : MonoBehaviour
{
    public GameObject Monster;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 pos = other.transform.position;
            Instantiate(Monster, new Vector3(pos.x, pos.y, pos.z - 3), Quaternion.identity);
        }
    }
        
}
