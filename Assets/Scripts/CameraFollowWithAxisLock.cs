using UnityEngine;

public class CameraFollowWithAxisLock : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                // Le transform du personnage à suivre
    public float smoothSpeed = 10f;         // Vitesse de déplacement fluide

    public enum CameraViewMode { FollowXAxis, FollowZAxis, TopDown }
    public CameraViewMode currentViewMode = CameraViewMode.FollowXAxis;

    [Header("Follow View Settings")]
    public Vector3 sideViewOffset = new Vector3(0f, 5f, -10f);  // Décalage pour les vues latérales

    [Header("Top View Settings")]
    public float topViewHeight = 15f;       // Hauteur de la caméra en vue de dessus
    public float topViewAngle = 70f;        // Angle d'inclinaison (90 = complètement vertical)
    public float topViewDistance = 5f;      // Distance horizontale en vue de dessus

    [Header("Axis Lock Settings")]
    public float fixedZPosition = 0f;       // Position Z fixe quand on suit l'axe X
    public float fixedXPosition = 0f;       // Position X fixe quand on suit l'axe Z

    [Header("Y Axis Settings")]
    public float yOffset = 5f;              // Décalage vertical toujours appliqué
    public bool smoothYFollow = true;       // Suivi fluide sur Y ou instantané

    // Positions calculées
    private Vector3 desiredPosition;
    private Quaternion desiredRotation;
    private Vector3 smoothedPosition;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("Camera: aucun target défini !");
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

        // Déplacement fluide de la caméra
        smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // Rotation fluide pour la caméra
        if (currentViewMode == CameraViewMode.TopDown)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Pour les vues de suivi, faire regarder la caméra vers le personnage
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

        // Calculer le décalage horizontal basé sur l'angle
        float horizontalOffset = topViewDistance * Mathf.Cos(angleRad);

        // Position de la caméra (décalage en Z pour vue de dessus)
        desiredPosition = new Vector3(
            targetPosition.x,
            targetPosition.y + topViewHeight,
            targetPosition.z - horizontalOffset
        );

        // Rotation pour regarder vers le personnage depuis le haut avec un angle
        desiredRotation = Quaternion.Euler(topViewAngle, 0, 0);
    }

    // Méthode pour changer le mode de vue
    public void CycleViewMode()
    {
        // Passer à la vue suivante de manière cyclique
        currentViewMode = (CameraViewMode)(((int)currentViewMode + 1) % 3);
        Debug.Log("Mode de caméra changé pour: " + currentViewMode);
    }

    // Méthode pour définir directement un mode de vue
    public void SetViewMode(CameraViewMode newMode)
    {
        currentViewMode = newMode;
        Debug.Log("Mode de caméra défini à: " + currentViewMode);
    }

    // Méthode pour changer la position du personnage à suivre
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}