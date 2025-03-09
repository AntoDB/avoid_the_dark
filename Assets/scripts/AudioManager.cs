using UnityEngine;
using FMODUnity;

/*public class AudioManager : MonoBehaviour
{
    [SerializeField] EventReference player_footsteps;
    //[SerializeField] EventReference;
    [SerializeField] float rate;
    [SerializeField] private FirstPersonController controller;
    [SerializeField] private GameObject player; // Ajout de la référence au joueur

    float time;

    public void PlayFootstep()
    {
        RuntimeManager.PlayOneShotAttached(player_footsteps, player);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created


    // Update is called once per frame
    void Update()
    {
        // Vérifier que controller n'est pas null avant d'accéder à ses propriétés
        if (controller != null && controller.isWalking)
        {
            time += Time.deltaTime;
            if (time >= rate)
            {
                PlayFootstep();
                time = 0;
            }
        }
    }
}
*/