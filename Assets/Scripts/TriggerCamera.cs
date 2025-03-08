using UnityEngine;

public class TriggerCamera : MonoBehaviour
{
    public bool followXAxis;
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            Camera.main.GetComponent<CameraFollowWithAxisLock>().followXAxis = followXAxis;
        }
    }
}
