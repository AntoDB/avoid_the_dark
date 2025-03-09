using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Vitesse de déplacement du monstre")]
    public float moveSpeed = 3.0f;

    [Tooltip("Distance à laquelle le monstre considère qu'il a atteint sa destination")]
    public float stoppingDistance = 0.5f;

    [Tooltip("Point de destination (peut être défini dans l'inspecteur ou via code)")]
    public Transform targetDestination;

    // Composants
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    // Variables d'état
    private bool hasReachedDestination = false;

    void Start()
    {
        // Récupération des composants nécessaires
        navMeshAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Configuration de base du NavMeshAgent
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = moveSpeed;
            navMeshAgent.stoppingDistance = stoppingDistance;

            // Si une destination est définie dès le départ, le monstre s'y dirige
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
        // Vérifier si le monstre a atteint sa destination
        if (navMeshAgent != null && navMeshAgent.enabled && targetDestination != null)
        {
            if (!hasReachedDestination && navMeshAgent.remainingDistance <= stoppingDistance)
            {
                OnReachDestination();
            }

            // Animation de déplacement (si un Animator est présent)
            if (animator != null)
            {
                animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
            }
        }
    }

    /// <summary>
    /// Définit une nouvelle destination pour le monstre
    /// </summary>
    /// <param name="destination">Position de destination</param>
    public void SetDestination(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            hasReachedDestination = false;
            navMeshAgent.SetDestination(destination);

            // Animation optionnelle de début de déplacement
            if (animator != null)
            {
                animator.SetTrigger("StartMoving");
            }
        }
    }

    /// <summary>
    /// Appelé lorsque le monstre atteint sa destination
    /// </summary>
    private void OnReachDestination()
    {
        hasReachedDestination = true;

        // Animation optionnelle d'arrivée
        if (animator != null)
        {
            animator.SetTrigger("ReachedDestination");
        }

        // Vous pouvez ajouter ici d'autres comportements à l'arrivée
        // comme attendre quelques secondes, choisir une nouvelle destination, etc.
        Debug.Log("Le monstre a atteint sa destination.");
    }
}