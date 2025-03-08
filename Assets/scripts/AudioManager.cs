using UnityEngine;
using FMODUnity;

public class AudioManager : MonoBehaviour
{
    [SerializeField] EventReference player_footsteps;
    //[SerializeField] EventReference;
    [SerializeField] float rate;
    //[SerializeField FirstPersonController controller;

    float time;

    public void PlayFootstep()
    {
        RuntimeManager.PlayOneShotAttached(player_footsteps, player);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created


    // Update is called once per frame
    void Update()
    {
        time += Time.deltaTime;
        if (controller.isWalking)
        {
            if (time >= rate)
            {
                PlayFootstep();
                time = 0;
            }
        }
    }
}
