using UnityEngine;

public class CameraFollowWithAxisLock : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                // Le transform du personnage � suivre
    public float smoothSpeed = 10f;         // Vitesse de d�placement fluide
    public Vector3 offset = new Vector3(0f, 5f, -10f);  // D�calage par rapport au personnage

    [Header("Axis Lock Settings")]
    public bool followXAxis = true;         // Si true, suit X; si false, suit Z
    public float fixedZPosition = 0f;       // Position Z fixe quand on suit l'axe X
    public float fixedXPosition = 0f;       // Position X fixe quand on suit l'axe Z

    [Header("Y Axis Settings")]
    public float yOffset = 5f;              // D�calage vertical toujours appliqu�
    public bool smoothYFollow = true;       // Suivi fluide sur Y ou instantan�

    // Positions calcul�es
    private Vector3 desiredPosition;
    private Vector3 smoothedPosition;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera: aucun target d�fini !");
            return;
        }

        // Calculer la position d�sir�e selon l'axe � suivre
        if (followXAxis)
        {
            // Suit les axes X et Y, Z fixe
            desiredPosition = new Vector3(
                target.position.x + offset.x,
                smoothYFollow ? target.position.y + yOffset : target.position.y + offset.y,
                fixedZPosition + offset.z
            );
        }
        else
        {
            // Suit les axes Z et Y, X fixe
            desiredPosition = new Vector3(
                fixedXPosition + offset.x,
                smoothYFollow ? target.position.y + yOffset : target.position.y + offset.y,
                target.position.z + offset.z
            );
        }

        // D�placement fluide de la cam�ra
        smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Faire regarder la cam�ra vers le personnage
        transform.LookAt(target);
    }

    // M�thode pour basculer entre les modes de suivi
    public void ToggleFollowAxis()
    {
        followXAxis = !followXAxis;
        Debug.Log("Camera suivant maintenant l'axe " + (followXAxis ? "X" : "Z"));
    }

    // M�thode pour changer la position du personnage � suivre
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
