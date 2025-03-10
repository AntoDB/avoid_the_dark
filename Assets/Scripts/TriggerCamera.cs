using UnityEngine;
using static CameraFollowWithAxisLock;

public class TriggerCamera : MonoBehaviour
{
    public CameraViewMode followMode;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Camera.main.GetComponent<CameraFollowWithAxisLock>().currentViewMode = followMode;
        }
    }
}
