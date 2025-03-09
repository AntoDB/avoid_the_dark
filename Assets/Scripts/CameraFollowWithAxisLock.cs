using UnityEngine;

public class CameraFollowWithAxisLock : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                // Le transform du personnage � suivre
    public float smoothSpeed = 10f;         // Vitesse de d�placement fluide

    public enum CameraViewMode { FollowXAxis, FollowZAxis, TopDown }
    public CameraViewMode currentViewMode = CameraViewMode.FollowXAxis;

    [Header("Follow View Settings")]
    public Vector3 sideViewOffset = new Vector3(0f, 5f, -10f);  // D�calage pour les vues lat�rales

    [Header("Top View Settings")]
    public float topViewHeight = 15f;       // Hauteur de la cam�ra en vue de dessus
    public float topViewAngle = 70f;        // Angle d'inclinaison (90 = compl�tement vertical)
    public float topViewDistance = 5f;      // Distance horizontale en vue de dessus

    [Header("Axis Lock Settings")]
    public float fixedZPosition = 0f;       // Position Z fixe quand on suit l'axe X
    public float fixedXPosition = 0f;       // Position X fixe quand on suit l'axe Z

    [Header("Y Axis Settings")]
    public float yOffset = 5f;              // D�calage vertical toujours appliqu�
    public bool smoothYFollow = true;       // Suivi fluide sur Y ou instantan�

    // Positions calcul�es
    private Vector3 desiredPosition;
    private Quaternion desiredRotation;
    private Vector3 smoothedPosition;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera: aucun target d�fini !");
            return;
        }

        switch (currentViewMode)
        {
            case CameraViewMode.FollowXAxis:
                FollowXAxisCamera();
                break;
            case CameraViewMode.FollowZAxis:
                FollowZAxisCamera();
                break;
            case CameraViewMode.TopDown:
                TopDownCamera();
                break;
        }

        // D�placement fluide de la cam�ra
        smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Rotation fluide pour la cam�ra
        if (currentViewMode == CameraViewMode.TopDown)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Pour les vues de suivi, faire regarder la cam�ra vers le personnage
            transform.LookAt(target);
        }
    }

    private void FollowXAxisCamera()
    {
        // Suit les axes X et Y, Z fixe
        desiredPosition = new Vector3(
            target.position.x + sideViewOffset.x,
            smoothYFollow ? target.position.y + yOffset : target.position.y + sideViewOffset.y,
            fixedZPosition + sideViewOffset.z
        );
    }

    private void FollowZAxisCamera()
    {
        // Suit les axes Z et Y, X fixe
        desiredPosition = new Vector3(
            fixedXPosition + sideViewOffset.x,
            smoothYFollow ? target.position.y + yOffset : target.position.y + sideViewOffset.y,
            target.position.z + sideViewOffset.z
        );
    }

    private void TopDownCamera()
    {
        // Calcul de la position au-dessus du personnage
        Vector3 targetPosition = target.position;

        // Calculer l'angle en radians
        float angleRad = (90 - topViewAngle) * Mathf.Deg2Rad;

        // Calculer le d�calage horizontal bas� sur l'angle
        float horizontalOffset = topViewDistance * Mathf.Cos(angleRad);

        // Position de la cam�ra (d�calage en Z pour vue de dessus)
        desiredPosition = new Vector3(
            targetPosition.x,
            targetPosition.y + topViewHeight,
            targetPosition.z - horizontalOffset
        );

        // Rotation pour regarder vers le personnage depuis le haut avec un angle
        desiredRotation = Quaternion.Euler(topViewAngle, 0, 0);
    }

    // M�thode pour changer le mode de vue
    public void CycleViewMode()
    {
        // Passer � la vue suivante de mani�re cyclique
        currentViewMode = (CameraViewMode)(((int)currentViewMode + 1) % 3);
        Debug.Log("Mode de cam�ra chang� pour: " + currentViewMode);
    }

    // M�thode pour d�finir directement un mode de vue
    public void SetViewMode(CameraViewMode newMode)
    {
        currentViewMode = newMode;
        Debug.Log("Mode de cam�ra d�fini �: " + currentViewMode);
    }

    // M�thode pour changer la position du personnage � suivre
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}