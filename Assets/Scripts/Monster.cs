using UnityEngine;
using UnityEngine.AI;

public class Monster : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Vitesse de d�placement du monstre")]
    public float moveSpeed = 3.0f;

    [Tooltip("Distance � laquelle le monstre consid�re qu'il a atteint sa destination")]
    public float stoppingDistance = 0.5f;

    [Tooltip("Point de destination fixe (ignor� si followTarget est activ�)")]
    public Transform targetDestination;

    [Tooltip("Cible � suivre en continu (joueur ou autre objet)")]
    public Transform targetToFollow;

    [Tooltip("Activer/d�sactiver le suivi de cible")]
    public bool followTarget = false;

    [Tooltip("Fr�quence de mise � jour du chemin lors du suivi (en secondes)")]
    public float pathUpdateFrequency = 0.2f;

    // Composants
    private NavMeshAgent navMeshAgent;
    private Animator animator;

    // Variables d'�tat
    private bool hasReachedDestination = false;
    private float pathUpdateTimer = 0f;

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

            // Si le suivi n'est pas activ� et qu'une destination fixe est d�finie
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
            // Mise � jour p�riodique du chemin pour suivre la cible
            pathUpdateTimer += Time.deltaTime;
            if (pathUpdateTimer >= pathUpdateFrequency)
            {
                pathUpdateTimer = 0f;
                navMeshAgent.SetDestination(targetToFollow.position);
            }

            // On consid�re que la destination est atteinte si on est assez proche
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

        // Animation de d�placement (si un Animator est pr�sent)
        if (animator != null)
        {
            animator.SetFloat("Speed", navMeshAgent.velocity.magnitude);
        }
    }

    /// <summary>
    /// D�finit une nouvelle destination fixe pour le monstre
    /// </summary>
    /// <param name="destination">Position de destination</param>
    public void SetDestination(Vector3 destination)
    {
        if (navMeshAgent != null && navMeshAgent.enabled)
        {
            followTarget = false; // D�sactive le mode suivi
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
    /// Active ou d�sactive le suivi de cible
    /// </summary>
    /// <param name="target">Cible � suivre (null pour d�sactiver)</param>
    /// <param name="follow">Activer/d�sactiver le suivi</param>
    public void SetTargetFollow(Transform target, bool follow)
    {
        targetToFollow = target;
        followTarget = follow && (target != null);

        // R�initialisation du timer pour mise � jour imm�diate
        pathUpdateTimer = pathUpdateFrequency;

        if (followTarget && animator != null)
        {
            animator.SetTrigger("StartMoving");
        }
    }

    /// <summary>
    /// Appel� lorsque le monstre atteint sa destination fixe
    /// </summary>
    private void OnReachDestination()
    {
        hasReachedDestination = true;

        // Animation optionnelle d'arriv�e
        if (animator != null)
        {
            animator.SetTrigger("ReachedDestination");
        }

        Debug.Log("Le monstre a atteint sa destination fixe.");
    }

    /// <summary>
    /// Appel� lorsque le monstre atteint sa cible en mouvement
    /// </summary>
    private void OnReachTarget()
    {
        // Animation optionnelle d'arriv�e � la cible
        if (animator != null)
        {
            animator.SetTrigger("ReachedTarget");
        }

        Debug.Log("Le monstre a atteint sa cible mobile.");

        // Ici vous pouvez ajouter des comportements comme attaquer le joueur
        // Par exemple: Attack();
    }

    /// <summary>
    /// Exemple de m�thode d'attaque (� impl�menter selon vos besoins)
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