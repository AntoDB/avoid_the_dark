using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnhancedLightsManager : MonoBehaviour
{
    [System.Serializable]
    public class LightConfig
    {
        public Light targetLight;                // R�f�rence � la lumi�re
        public float duration = 5f;              // Dur�e d'illumination
        public float fadeOutTime = 1.5f;         // Temps d'extinction
        public float delayBeforeNext = 0f;       // D�lai avant d'allumer la prochaine lumi�re
        [HideInInspector] public float initialIntensity;  // Intensit� initiale (sera automatiquement enregistr�e)
    }

    public enum StartMode
    {
        Sequential,       // Allumage une par une
        Simultaneous      // Allumage de toutes les lumi�res en m�me temps
    }

    public enum EndMode
    {
        Sequential,       // Extinction une par une
        Simultaneous      // Extinction de toutes les lumi�res en m�me temps
    }

    [Header("Lights Configuration")]
    public List<LightConfig> lightSequence = new List<LightConfig>();  // Liste des lumi�res � g�rer

    [Header("Sequence Settings")]
    public bool autoStartOnAwake = true;         // D�marrer automatiquement la s�quence
    public bool loopSequence = false;            // R�p�ter la s�quence ind�finiment
    public float initialDelay = 0f;              // D�lai avant de commencer la s�quence

    [Header("Light Behavior")]
    public StartMode lightStartMode = StartMode.Sequential;  // Mode d'allumage
    public EndMode lightEndMode = EndMode.Sequential;        // Mode d'extinction
    public float simultaneousStartTime = 1.0f;    // Temps d'allumage progressif (mode simultan�)
    public float simultaneousEndTime = 2.0f;      // Temps d'extinction progressif (mode simultan�)
    public bool useProgressiveStart = true;      // Allumer les lumi�res progressivement en mode s�quentiel
    public float progressiveStartTime = 0.5f;    // Temps pour allumer progressivement en mode s�quentiel

    private int currentLightIndex = -1;          // Index de la lumi�re actuelle
    private bool isSequenceActive = false;       // �tat de la s�quence
    private Coroutine sequenceCoroutine;         // R�f�rence � la coroutine de s�quence

    void Start()
    {
        // V�rifier et enregistrer l'intensit� initiale de chaque lumi�re
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.initialIntensity = config.targetLight.intensity;
                // �teindre toutes les lumi�res au d�part
                config.targetLight.intensity = 0;
            }
            else
            {
                Debug.LogWarning("Une lumi�re de la s�quence n'est pas assign�e!");
            }
        }

        // D�marrer automatiquement si configur�
        if (autoStartOnAwake && lightSequence.Count > 0)
        {
            StartSequence();
        }
    }

    /// <summary>
    /// D�marre la s�quence de lumi�res
    /// </summary>
    public void StartSequence()
    {
        if (isSequenceActive)
        {
            StopSequence();
        }

        isSequenceActive = true;
        currentLightIndex = -1;

        if (lightStartMode == StartMode.Sequential)
        {
            sequenceCoroutine = StartCoroutine(RunSequentialLightSequence());
        }
        else
        {
            sequenceCoroutine = StartCoroutine(RunSimultaneousLightSequence());
        }
    }

    /// <summary>
    /// Arr�te la s�quence de lumi�res et �teint toutes les lumi�res
    /// </summary>
    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        isSequenceActive = false;

        // �teindre toutes les lumi�res
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = 0;
            }
        }
    }

    /// <summary>
    /// G�re la s�quence s�quentielle des lumi�res
    /// </summary>
    private IEnumerator RunSequentialLightSequence()
    {
        // Attendre le d�lai initial
        if (initialDelay > 0)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        do
        {
            // Parcourir chaque lumi�re dans la s�quence
            for (int i = 0; i < lightSequence.Count; i++)
            {
                currentLightIndex = i;
                LightConfig currentLight = lightSequence[i];

                if (currentLight.targetLight != null)
                {
                    // Allumer progressivement la lumi�re si l'option est activ�e
                    if (useProgressiveStart)
                    {
                        yield return StartCoroutine(FadeInLight(currentLight));
                    }
                    else
                    {
                        currentLight.targetLight.intensity = currentLight.initialIntensity;
                    }

                    // Attendre la dur�e configur�e
                    yield return new WaitForSeconds(currentLight.duration);

                    // �teindre progressivement
                    yield return StartCoroutine(FadeOutLight(currentLight));

                    // Attendre le d�lai avant la prochaine lumi�re
                    if (currentLight.delayBeforeNext > 0)
                    {
                        yield return new WaitForSeconds(currentLight.delayBeforeNext);
                    }
                }
            }
        } while (loopSequence && isSequenceActive);

        isSequenceActive = false;
    }

    /// <summary>
    /// G�re la s�quence simultan�e des lumi�res
    /// </summary>
    private IEnumerator RunSimultaneousLightSequence()
    {
        // Attendre le d�lai initial
        if (initialDelay > 0)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        do
        {
            // Allumer toutes les lumi�res en m�me temps
            yield return StartCoroutine(TurnOnAllLights());

            // Trouver la dur�e maximale parmi toutes les lumi�res
            float maxDuration = 0;
            foreach (LightConfig config in lightSequence)
            {
                if (config.duration > maxDuration)
                {
                    maxDuration = config.duration;
                }
            }

            // Attendre la dur�e maximale
            yield return new WaitForSeconds(maxDuration);

            // �teindre les lumi�res selon le mode choisi
            if (lightEndMode == EndMode.Sequential)
            {
                // �teindre les lumi�res une par une
                for (int i = 0; i < lightSequence.Count; i++)
                {
                    currentLightIndex = i;
                    LightConfig currentLight = lightSequence[i];

                    if (currentLight.targetLight != null)
                    {
                        yield return StartCoroutine(FadeOutLight(currentLight));

                        if (currentLight.delayBeforeNext > 0)
                        {
                            yield return new WaitForSeconds(currentLight.delayBeforeNext);
                        }
                    }
                }
            }
            else
            {
                // �teindre toutes les lumi�res en m�me temps
                yield return StartCoroutine(TurnOffAllLights());
            }

            // Attendre un d�lai avant de recommencer (si en boucle)
            if (loopSequence)
            {
                yield return new WaitForSeconds(1.0f);
            }

        } while (loopSequence && isSequenceActive);

        isSequenceActive = false;
    }

    /// <summary>
    /// Allume progressivement une lumi�re individuelle
    /// </summary>
    private IEnumerator FadeInLight(LightConfig lightConfig)
    {
        float elapsedTime = 0;
        lightConfig.targetLight.intensity = 0;

        while (elapsedTime < progressiveStartTime)
        {
            elapsedTime += Time.deltaTime;
            float ratio = elapsedTime / progressiveStartTime;
            lightConfig.targetLight.intensity = Mathf.Lerp(0, lightConfig.initialIntensity, ratio);
            yield return null;
        }

        lightConfig.targetLight.intensity = lightConfig.initialIntensity;
    }

    /// <summary>
    /// �teint progressivement une lumi�re individuelle
    /// </summary>
    private IEnumerator FadeOutLight(LightConfig lightConfig)
    {
        float elapsedTime = 0;
        float startIntensity = lightConfig.targetLight.intensity;

        while (elapsedTime < lightConfig.fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float ratio = 1 - (elapsedTime / lightConfig.fadeOutTime);
            lightConfig.targetLight.intensity = Mathf.Lerp(0, startIntensity, ratio);
            yield return null;
        }

        lightConfig.targetLight.intensity = 0;
    }

    /// <summary>
    /// Allume toutes les lumi�res en m�me temps
    /// </summary>
    private IEnumerator TurnOnAllLights()
    {
        float elapsedTime = 0;

        // Initialiser toutes les lumi�res � 0
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = 0;
            }
        }

        // Transition progressive de toutes les lumi�res
        while (elapsedTime < simultaneousStartTime)
        {
            elapsedTime += Time.deltaTime;
            float ratio = elapsedTime / simultaneousStartTime;

            foreach (LightConfig config in lightSequence)
            {
                if (config.targetLight != null)
                {
                    config.targetLight.intensity = Mathf.Lerp(0, config.initialIntensity, ratio);
                }
            }

            yield return null;
        }

        // S'assurer que toutes les lumi�res sont � leur intensit� maximale
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = config.initialIntensity;
            }
        }
    }

    /// <summary>
    /// �teint toutes les lumi�res en m�me temps
    /// </summary>
    private IEnumerator TurnOffAllLights()
    {
        float elapsedTime = 0;
        Dictionary<Light, float> startIntensities = new Dictionary<Light, float>();

        // Enregistrer l'intensit� initiale de chaque lumi�re
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                startIntensities[config.targetLight] = config.targetLight.intensity;
            }
        }

        // Transition progressive de toutes les lumi�res
        while (elapsedTime < simultaneousEndTime)
        {
            elapsedTime += Time.deltaTime;
            float ratio = 1 - (elapsedTime / simultaneousEndTime);

            foreach (LightConfig config in lightSequence)
            {
                if (config.targetLight != null && startIntensities.ContainsKey(config.targetLight))
                {
                    config.targetLight.intensity = Mathf.Lerp(0, startIntensities[config.targetLight], ratio);
                }
            }

            yield return null;
        }

        // S'assurer que toutes les lumi�res sont �teintes
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = 0;
            }
        }
    }

    /// <summary>
    /// Affiche l'�tat actuel de la s�quence dans l'inspecteur (utile pour le d�bogage)
    /// </summary>
    void OnGUI()
    {
        if (isSequenceActive)
        {
            string modeInfo = "Mode d'allumage: " + lightStartMode.ToString() +
                              " | Mode d'extinction: " + lightEndMode.ToString();

            GUI.Label(new Rect(10, 10, 400, 20), modeInfo);

            if (currentLightIndex >= 0 && currentLightIndex < lightSequence.Count)
            {
                GUI.Label(new Rect(10, 30, 300, 20), "Lumi�re active: " + currentLightIndex +
                    " (" + (lightSequence[currentLightIndex].targetLight ? lightSequence[currentLightIndex].targetLight.name : "null") + ")");
            }
        }
    }
}