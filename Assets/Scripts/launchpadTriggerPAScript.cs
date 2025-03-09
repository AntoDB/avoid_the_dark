using UnityEngine;

// Ce script doit être attaché au même objet que LaunchpadPixelArtCreator
// et qui possède un BoxCollider avec Is Trigger activé
public class LaunchpadTriggerController : MonoBehaviour
{
    // Référence au script LaunchpadPixelArtCreator
    private LaunchpadPixelArtCreator launchpadController;
    
    // Référence au collider de l'objet
    private BoxCollider boxCollider;
    
    // Mode d'affichage à définir lors du trigger (2 = Ghost)
    [SerializeField] private int triggerDisplayMode = 2;
    
    // Désactiver le trigger après la première activation?
    [SerializeField] private bool disableTriggerAfterActivation = true;
    
    void Start()
    {
        // Récupérer les références aux composants
        launchpadController = GetComponent<LaunchpadPixelArtCreator>();
        boxCollider = GetComponent<BoxCollider>();
        
        // Vérifier que les composants requis sont présents
        if (launchpadController == null)
        {
            Debug.LogError("LaunchpadPixelArtCreator non trouvé sur cet objet!");
        }
        
        if (boxCollider == null)
        {
            Debug.LogError("BoxCollider non trouvé sur cet objet!");
        }
        else if (!boxCollider.isTrigger)
        {
            Debug.LogWarning("Le BoxCollider n'est pas configuré comme trigger. Il est recommandé d'activer Is Trigger.");
        }
    }
    
    // Cette méthode est appelée lorsqu'un objet entre dans le trigger
    void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur ou l'objet avec lequel nous voulons interagir
        // Vous pouvez adapter cette condition selon vos besoins (tag, layer, etc.)
        if (other.CompareTag("Player") || other.CompareTag("MainCamera"))
        {
            ActivateLaunchpad();
        }
    }
    
    // Fonction pour activer le Launchpad avec l'image souhaitée
    private void ActivateLaunchpad()
    {
        if (launchpadController != null)
        {
            // Définir le mode d'affichage (2 = Ghost)
            launchpadController.SetDisplayMode(triggerDisplayMode);
            
            // Afficher un log pour confirmer l'activation
            Debug.Log("Launchpad activé en mode " + triggerDisplayMode);
            
            // Si configuré, désactiver le trigger après activation
            if (disableTriggerAfterActivation && boxCollider != null)
            {
                boxCollider.isTrigger = false;
                Debug.Log("Trigger désactivé après activation");
            }
        }
    }
    
    // Méthode publique pour réinitialiser le trigger (utile pour tester)
    public void ResetTrigger()
    {
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            Debug.Log("Trigger réinitialisé");
        }
    }
}
