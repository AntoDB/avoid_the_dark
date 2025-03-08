using UnityEngine;

public class TriggerCamera : MonoBehaviour
{
    public bool followXAxis;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Camera.main.GetComponent<CameraFollowWithAxisLock>().followXAxis = followXAxis;
        }
    }
}
