using UnityEngine;

public class TriggerMonsterFollow : MonoBehaviour
{
    public float distanceApparition = 3;
    public GameObject Monster;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 pos = other.transform.position;
            GameObject go = Instantiate(Monster, new Vector3(pos.x, pos.y, pos.z - distanceApparition), Quaternion.identity);
            go.GetComponent<Animator>().SetBool("Lava", true);
            go.GetComponent<Monster>().followTarget = true;
            go.GetComponent<Monster>().targetToFollow = other.transform;
            Destroy(gameObject);
        }
    }
        
}
