using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Vitesse de d�placement du monstre")]
    public float moveSpeed = 3.0f;

    [Tooltip("Distance � laquelle le monstre consid�re qu'il a atteint sa destination")]
    public float stoppingDistance = 0.5f;

    [Tooltip("Point de destination (peut �tre d�fini dans l'inspecteur ou via code)")]
    public Transform targetDestination;

    // Composants
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    // Variables d'�tat
    private bool hasReachedDestination = false;

    void Start()
    {
        // R�cup�ration des composants n�cessaires
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Configuration de base du NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = stoppingDistance;

            // Si une destination est d�finie d�s le d�part, le monstre s'y dirige
            if (targetDestination != null)
            {
                SetDestination(targetDestination.position);
            }
        }
        else
        {
            Debug.LogError("NavMeshAgent manquant sur le monstre. Ajoutez ce composant pour permettre la navigation.");
        }
    }

    void Update()
    {
        // V�rifier si le monstre a atteint sa destination
        if (navMeshAgent != null && navMeshAgent.enabled && targetDestination != null)
        {
            if (!hasReachedDestination && navMeshAgent.remainingDistance <= stoppingDistance)
            {
                OnReachDestination();
            }

            // Animation de d�placement (si un Animator est pr�sent)
            if (animator != null)
            {
                animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            }
        }
    }

    /// <summary>
    /// D�finit une nouvelle destination pour le monstre
    /// </summary>
    /// <param name="destination">Position de destination</param>
    public void SetDestination(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            hasReachedDestination = false;
            navMeshAgent.SetDestination(destination);

            // Animation optionnelle de d�but de d�placement
            if (animator != null)
            {
                animator.SetTrigger("StartMoving");
            }
        }
    }

    /// <summary>
    /// Appel� lorsque le monstre atteint sa destination
    /// </summary>
    private void OnReachDestination()
    {
        hasReachedDestination = true;

        // Animation optionnelle d'arriv�e
        if (animator != null)
        {
            animator.SetTrigger("ReachedDestination");
        }

        // Vous pouvez ajouter ici d'autres comportements � l'arriv�e
        // comme attendre quelques secondes, choisir une nouvelle destination, etc.
        Debug.Log("Le monstre a atteint sa destination.");
    }
}