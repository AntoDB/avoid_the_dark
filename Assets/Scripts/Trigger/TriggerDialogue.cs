using UnityEngine;
using UnityEngine.UIElements;
using static CameraFollowWithAxisLock;

public class TriggerDialogue : MonoBehaviour
{
    public GameObject boxDialogue;
    [Header("Dialogue Settings")]
    public float characterSpacing = 0.5f;
    public float wordSpacing = 0.5f;
    public float lineSpacing = 1f;
    public float revealSpeed = 0.1f;
    public float dropForce = 2f;
    public float characterLifetime = 2f;

    [Header("Animation Settings")]
    public float animationSpeed = 1f;

    [Header("Dialogue Texts")]
    public string[] dialogueTexts;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Transform cameraTransform = Camera.main.transform;
            CameraFollowWithAxisLock cam = Camera.main.GetComponent<CameraFollowWithAxisLock>();
            GameObject go = Instantiate(boxDialogue, new Vector3(other.transform.position.x + (3 * (cam.currentViewMode.Equals(CameraViewMode.FollowXAxis) ? 1 : 0)), other.transform.position.y + 2, other.transform.position.z + (3 * (cam.currentViewMode.Equals(CameraViewMode.FollowZAxis) ? 1 : 0))), Quaternion.identity);
            go.transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                         cameraTransform.rotation * Vector3.up);
            Dialogue3DText dialogue = go.GetComponent<Dialogue3DText>();
            dialogue.characterSpacing = characterSpacing;
            dialogue.wordSpacing = wordSpacing;
            dialogue.lineSpacing = lineSpacing;
            dialogue.revealSpeed = revealSpeed;
            dialogue.dropForce = dropForce;
            dialogue.characterLifetime = characterLifetime;
            dialogue.animationSpeed = animationSpeed;
            dialogue.dialogueTexts = dialogueTexts;
            Destroy(gameObject);
        }
    }
}
