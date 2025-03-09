using UnityEngine;

/// <summary>
/// Contrôleur de lumières très simple qui s'intègre avec le système MQTT existant
/// </summary>
public class SimpleLightController : MonoBehaviour
{
    // Référence au client MQTT
    private DirectMQTTClient mqttClient;

    // Lumières à contrôler
    [Header("Lights Configuration")]
    [Tooltip("Toutes les lumières à contrôler")]
    public Light[] directionalLights;

    // Index de la lumière actuelle
    private int currentLightIndex = -1;

    void Awake()
    {
        // S'assurer que toutes les lumières sont éteintes dès le démarrage
        TurnOffAllLights();
    }

    void Start()
    {
        Debug.Log("[LightCtrl] Starting SimpleLightController");

        // Récupérer le client MQTT (qui doit être déjà dans la scène)
        mqttClient = FindObjectOfType<DirectMQTTClient>();

        if (mqttClient == null)
        {
            Debug.LogError("[LightCtrl] DirectMQTTClient component not found in the scene!");
            return;
        }

        // S'abonner aux événements
        mqttClient.OnButton1Pressed += HandleButton1Pressed;
        mqttClient.OnButton2Pressed += HandleButton2Pressed;

        Debug.Log("[LightCtrl] MQTT events subscribed");

        // Vérifier les lumières
        CheckLights();
    }

    /// <summary>
    /// Vérifie que les lumières sont correctement configurées
    /// </summary>
    private void CheckLights()
    {
        if (directionalLights == null || directionalLights.Length == 0)
        {
            Debug.LogWarning("[LightCtrl] No lights assigned! Please assign lights in the inspector.");
            return;
        }

        // Vérifier si des références sont nulles
        int validLightsCount = 0;
        for (int i = 0; i < directionalLights.Length; i++)
        {
            if (directionalLights[i] != null)
            {
                validLightsCount++;
            }
            else
            {
                Debug.LogWarning($"[LightCtrl] Light at index {i} is null!");
            }
        }

        Debug.Log($"[LightCtrl] Found {validLightsCount} valid lights out of {directionalLights.Length}");
    }

    /// <summary>
    /// Gestionnaire pour l'événement du bouton 1 pressé
    /// </summary>
    private void HandleButton1Pressed()
    {
        Debug.Log("[LightCtrl] Button 1 event received - Cycling lights");
        ActivateRandomLight();
    }

    /// <summary>
    /// Gestionnaire pour l'événement du bouton 2 pressé
    /// </summary>
    private void HandleButton2Pressed()
    {
        Debug.Log("[LightCtrl] Button 2 event received - Cycling lights");
        ActivateRandomLight();
    }

    /// <summary>
    /// Active une lumière aléatoire
    /// </summary>
    private void ActivateRandomLight()
    {
        if (directionalLights == null || directionalLights.Length == 0)
        {
            Debug.LogWarning("[LightCtrl] Cannot activate light - no lights assigned!");
            return;
        }

        // Éteindre toutes les lumières d'abord
        TurnOffAllLights();

        // Sélectionner une nouvelle lumière aléatoire
        int newIndex;
        int maxAttempts = 10; // Éviter une boucle infinie si toutes les lumières sont nulles
        int attempts = 0;

        do
        {
            newIndex = Random.Range(0, directionalLights.Length);
            attempts++;
        } while (directionalLights[newIndex] == null && attempts < maxAttempts);

        // Si on a trouvé une lumière valide
        if (directionalLights[newIndex] != null)
        {
            currentLightIndex = newIndex;
            directionalLights[currentLightIndex].enabled = true;
            Debug.Log($"[LightCtrl] Activated light {currentLightIndex}: {directionalLights[currentLightIndex].name}");
        }
        else
        {
            Debug.LogWarning("[LightCtrl] Could not find a valid light to activate!");
        }
    }

    /// <summary>
    /// Éteint toutes les lumières
    /// </summary>
    private void TurnOffAllLights()
    {
        if (directionalLights == null) return;

        foreach (Light light in directionalLights)
        {
            if (light != null)
            {
                light.enabled = false;
            }
        }
    }

    /// <summary>
    /// Méthode appelée lors de la destruction de l'objet
    /// </summary>
    private void OnDestroy()
    {
        // Désabonner des événements
        if (mqttClient != null)
        {
            mqttClient.OnButton1Pressed -= HandleButton1Pressed;
            mqttClient.OnButton2Pressed -= HandleButton2Pressed;
        }
    }
}