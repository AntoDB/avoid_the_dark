using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnhancedLightsManager : MonoBehaviour
{
    [System.Serializable]
    public class LightConfig
    {
        public Light targetLight;                // Référence à la lumière
        public float duration = 5f;              // Durée d'illumination
        public float fadeOutTime = 1.5f;         // Temps d'extinction
        public float delayBeforeNext = 0f;       // Délai avant d'allumer la prochaine lumière
        [HideInInspector] public float initialIntensity;  // Intensité initiale (sera automatiquement enregistrée)
    }

    public enum StartMode
    {
        Sequential,       // Allumage une par une
        Simultaneous      // Allumage de toutes les lumières en même temps
    }

    public enum EndMode
    {
        Sequential,       // Extinction une par une
        Simultaneous      // Extinction de toutes les lumières en même temps
    }

    [Header("Lights Configuration")]
    public List<LightConfig> lightSequence = new List<LightConfig>();  // Liste des lumières à gérer

    [Header("Sequence Settings")]
    public bool autoStartOnAwake = true;         // Démarrer automatiquement la séquence
    public bool loopSequence = false;            // Répéter la séquence indéfiniment
    public float initialDelay = 0f;              // Délai avant de commencer la séquence

    [Header("Light Behavior")]
    public StartMode lightStartMode = StartMode.Sequential;  // Mode d'allumage
    public EndMode lightEndMode = EndMode.Sequential;        // Mode d'extinction
    public float simultaneousStartTime = 1.0f;    // Temps d'allumage progressif (mode simultané)
    public float simultaneousEndTime = 2.0f;      // Temps d'extinction progressif (mode simultané)
    public bool useProgressiveStart = true;      // Allumer les lumières progressivement en mode séquentiel
    public float progressiveStartTime = 0.5f;    // Temps pour allumer progressivement en mode séquentiel

    private int currentLightIndex = -1;          // Index de la lumière actuelle
    private bool isSequenceActive = false;       // État de la séquence
    private Coroutine sequenceCoroutine;         // Référence à la coroutine de séquence

    void Start()
    {
        // Vérifier et enregistrer l'intensité initiale de chaque lumière
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.initialIntensity = config.targetLight.intensity;
                // Éteindre toutes les lumières au départ
                config.targetLight.intensity = 0;
            }
            else
            {
                Debug.LogWarning("Une lumière de la séquence n'est pas assignée!");
            }
        }

        // Démarrer automatiquement si configuré
        if (autoStartOnAwake && lightSequence.Count > 0)
        {
            StartSequence();
        }
    }

    /// <summary>
    /// Démarre la séquence de lumières
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
    /// Arrête la séquence de lumières et éteint toutes les lumières
    /// </summary>
    public void StopSequence()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        isSequenceActive = false;

        // Éteindre toutes les lumières
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = 0;
            }
        }
    }

    /// <summary>
    /// Gère la séquence séquentielle des lumières
    /// </summary>
    private IEnumerator RunSequentialLightSequence()
    {
        // Attendre le délai initial
        if (initialDelay > 0)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        do
        {
            // Parcourir chaque lumière dans la séquence
            for (int i = 0; i < lightSequence.Count; i++)
            {
                currentLightIndex = i;
                LightConfig currentLight = lightSequence[i];

                if (currentLight.targetLight != null)
                {
                    // Allumer progressivement la lumière si l'option est activée
                    if (useProgressiveStart)
                    {
                        yield return StartCoroutine(FadeInLight(currentLight));
                    }
                    else
                    {
                        currentLight.targetLight.intensity = currentLight.initialIntensity;
                    }

                    // Attendre la durée configurée
                    yield return new WaitForSeconds(currentLight.duration);

                    // Éteindre progressivement
                    yield return StartCoroutine(FadeOutLight(currentLight));

                    // Attendre le délai avant la prochaine lumière
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
    /// Gère la séquence simultanée des lumières
    /// </summary>
    private IEnumerator RunSimultaneousLightSequence()
    {
        // Attendre le délai initial
        if (initialDelay > 0)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        do
        {
            // Allumer toutes les lumières en même temps
            yield return StartCoroutine(TurnOnAllLights());

            // Trouver la durée maximale parmi toutes les lumières
            float maxDuration = 0;
            foreach (LightConfig config in lightSequence)
            {
                if (config.duration > maxDuration)
                {
                    maxDuration = config.duration;
                }
            }

            // Attendre la durée maximale
            yield return new WaitForSeconds(maxDuration);

            // Éteindre les lumières selon le mode choisi
            if (lightEndMode == EndMode.Sequential)
            {
                // Éteindre les lumières une par une
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
                // Éteindre toutes les lumières en même temps
                yield return StartCoroutine(TurnOffAllLights());
            }

            // Attendre un délai avant de recommencer (si en boucle)
            if (loopSequence)
            {
                yield return new WaitForSeconds(1.0f);
            }

        } while (loopSequence && isSequenceActive);

        isSequenceActive = false;
    }

    /// <summary>
    /// Allume progressivement une lumière individuelle
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
    /// Éteint progressivement une lumière individuelle
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
    /// Allume toutes les lumières en même temps
    /// </summary>
    private IEnumerator TurnOnAllLights()
    {
        float elapsedTime = 0;

        // Initialiser toutes les lumières à 0
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = 0;
            }
        }

        // Transition progressive de toutes les lumières
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

        // S'assurer que toutes les lumières sont à leur intensité maximale
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = config.initialIntensity;
            }
        }
    }

    /// <summary>
    /// Éteint toutes les lumières en même temps
    /// </summary>
    private IEnumerator TurnOffAllLights()
    {
        float elapsedTime = 0;
        Dictionary<Light, float> startIntensities = new Dictionary<Light, float>();

        // Enregistrer l'intensité initiale de chaque lumière
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                startIntensities[config.targetLight] = config.targetLight.intensity;
            }
        }

        // Transition progressive de toutes les lumières
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

        // S'assurer que toutes les lumières sont éteintes
        foreach (LightConfig config in lightSequence)
        {
            if (config.targetLight != null)
            {
                config.targetLight.intensity = 0;
            }
        }
    }

    /// <summary>
    /// Affiche l'état actuel de la séquence dans l'inspecteur (utile pour le débogage)
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
                GUI.Label(new Rect(10, 30, 300, 20), "Lumière active: " + currentLightIndex +
                    " (" + (lightSequence[currentLightIndex].targetLight ? lightSequence[currentLightIndex].targetLight.name : "null") + ")");
            }
        }
    }
}