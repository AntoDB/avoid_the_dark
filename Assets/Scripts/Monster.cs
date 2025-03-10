using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Vitesse de déplacement du monstre")]
    public float moveSpeed = 3.0f;

    [Tooltip("Distance à laquelle le monstre considère qu'il a atteint sa destination")]
    public float stoppingDistance = 0.5f;

    [Tooltip("Point de destination fixe (ignoré si followTarget est activé)")]
    public Transform targetDestination;

    [Tooltip("Cible à suivre en continu (joueur ou autre objet)")]
    public Transform targetToFollow;

    [Tooltip("Activer/désactiver le suivi de cible")]
    public bool followTarget = false;

    [Tooltip("Fréquence de mise à jour du chemin lors du suivi (en secondes)")]
    public float pathUpdateFrequency = 0.2f;

    // Composants
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    // Variables d'état
    private bool hasReachedDestination = false;
    private float pathUpdateTimer = 0f;

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

            // Si le suivi n'est pas activé et qu'une destination fixe est définie
            if (!followTarget && targetDestination != null)
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
        if (navMeshAgent == null || !navMeshAgent.enabled)
            return;

        // Mode suivi de cible
        if (followTarget && targetToFollow != null)
        {
            // Mise à jour périodique du chemin pour suivre la cible
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathUpdateFrequency)
            {
                pathUpdateTimer = 0f;
                navMeshAgent.SetDestination(targetToFollow.position);
            }

            // On considère que la destination est atteinte si on est assez proche
            if (Vector3.Distance(transform.position, targetToFollow.position) <= stoppingDistance)
            {
                if (!hasReachedDestination)
                {
                    hasReachedDestination = true;
                    OnReachTarget();
                }
            }
            else
            {
                hasReachedDestination = false;
            }
        }
        // Mode destination fixe
        else if (targetDestination != null)
        {
            if (!hasReachedDestination && navMeshAgent.remainingDistance <= stoppingDistance)
            {
                OnReachDestination();
            }
        }

        // Animation de déplacement (si un Animator est présent)
        if (animator != null)
        {
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
        }
    }

    /// <summary>
    /// Définit une nouvelle destination fixe pour le monstre
    /// </summary>
    /// <param name="destination">Position de destination</param>
    public void SetDestination(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            followTarget = false; // Désactive le mode suivi
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
    /// Active ou désactive le suivi de cible
    /// </summary>
    /// <param name="target">Cible à suivre (null pour désactiver)</param>
    /// <param name="follow">Activer/désactiver le suivi</param>
    public void SetTargetFollow(Transform target, bool follow)
    {
        targetToFollow = target;
        followTarget = follow && (target != null);

        // Réinitialisation du timer pour mise à jour immédiate
        pathUpdateTimer = pathUpdateFrequency;

        if (followTarget && animator != null)
        {
            animator.SetTrigger("StartMoving");
        }
    }

    /// <summary>
    /// Appelé lorsque le monstre atteint sa destination fixe
    /// </summary>
    private void OnReachDestination()
    {
        hasReachedDestination = true;

        // Animation optionnelle d'arrivée
        if (animator != null)
        {
            animator.SetTrigger("ReachedDestination");
        }

        Debug.Log("Le monstre a atteint sa destination fixe.");
    }

    /// <summary>
    /// Appelé lorsque le monstre atteint sa cible en mouvement
    /// </summary>
    private void OnReachTarget()
    {
        // Animation optionnelle d'arrivée à la cible
        if (animator != null)
        {
            animator.SetTrigger("ReachedTarget");
        }

        Debug.Log("Le monstre a atteint sa cible mobile.");

        // Ici vous pouvez ajouter des comportements comme attaquer le joueur
        // Par exemple: Attack();
    }

    /// <summary>
    /// Exemple de méthode d'attaque (à implémenter selon vos besoins)
    /// </summary>
    private void Attack()
    {
        // Logique d'attaque
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Autres effets d'attaque...
    }
}