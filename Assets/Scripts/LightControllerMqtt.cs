using UnityEngine;
using System.Collections;

/// <summary>
/// Contrôleur de lumières avec minuterie qui s'intègre avec le système MQTT existant
/// </summary>
public class TimedLightController : MonoBehaviour
{
    // Référence au client MQTT
    private DirectMQTTClient mqttClient;

    // Lumières à contrôler
    [Header("Lights Configuration")]
    [Tooltip("Toutes les lumières à contrôler")]
    public Light[] directionalLights;

    [Header("Timing")]
    [Tooltip("Durée pendant laquelle une lumière reste allumée (en secondes)")]
    public float lightDuration = 5.0f;

    // Minuteries pour chaque bouton
    private float button1Timer = 0f;
    private float button2Timer = 0f;

    // Lumières actuellement activées
    private int button1LightIndex = -1;
    private int button2LightIndex = -1;

    void Awake()
    {
        // S'assurer que toutes les lumières sont éteintes dès le démarrage
        TurnOffAllLights();
    }

    void Start()
    {
        Debug.Log("[LightCtrl] Starting TimedLightController");

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

    void Update()
    {
        // Gérer la minuterie pour le bouton 1
        if (button1Timer > 0)
        {
            button1Timer -= Time.deltaTime;
            if (button1Timer <= 0)
            {
                // Le temps est écoulé, éteindre la lumière
                if (button1LightIndex >= 0 && button1LightIndex < directionalLights.Length && directionalLights[button1LightIndex] != null)
                {
                    directionalLights[button1LightIndex].enabled = false;
                    Debug.Log($"[LightCtrl] Button 1 timer expired, turned off light {button1LightIndex}");
                }
                button1LightIndex = -1;
            }
        }

        // Gérer la minuterie pour le bouton 2
        if (button2Timer > 0)
        {
            button2Timer -= Time.deltaTime;
            if (button2Timer <= 0)
            {
                // Le temps est écoulé, éteindre la lumière
                if (button2LightIndex >= 0 && button2LightIndex < directionalLights.Length && directionalLights[button2LightIndex] != null)
                {
                    directionalLights[button2LightIndex].enabled = false;
                    Debug.Log($"[LightCtrl] Button 2 timer expired, turned off light {button2LightIndex}");
                }
                button2LightIndex = -1;
            }
        }
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
        Debug.Log("[LightCtrl] Button 1 event received");

        // Vérifier si le bouton est déjà actif (minuterie en cours)
        if (button1Timer > 0)
        {
            Debug.Log("[LightCtrl] Button 1 is blocked, ignoring press");
            return; // Le bouton est bloqué
        }

        // Activer une nouvelle lumière pour le bouton 1
        ActivateRandomLightForButton(1);
    }

    /// <summary>
    /// Gestionnaire pour l'événement du bouton 2 pressé
    /// </summary>
    private void HandleButton2Pressed()
    {
        Debug.Log("[LightCtrl] Button 2 event received");

        // Vérifier si le bouton est déjà actif (minuterie en cours)
        if (button2Timer > 0)
        {
            Debug.Log("[LightCtrl] Button 2 is blocked, ignoring press");
            return; // Le bouton est bloqué
        }

        // Activer une nouvelle lumière pour le bouton 2
        ActivateRandomLightForButton(2);
    }

    /// <summary>
    /// Active une lumière aléatoire pour un bouton spécifique
    /// </summary>
    private void ActivateRandomLightForButton(int buttonNumber)
    {
        if (directionalLights == null || directionalLights.Length == 0)
        {
            Debug.LogWarning("[LightCtrl] Cannot activate light - no lights assigned!");
            return;
        }

        // Trouver une lumière qui n'est pas déjà activée par l'autre bouton
        int otherLightIndex = (buttonNumber == 1) ? button2LightIndex : button1LightIndex;
        int newLightIndex;
        int attempts = 0;
        int maxAttempts = 10; // Éviter une boucle infinie

        do
        {
            newLightIndex = Random.Range(0, directionalLights.Length);
            attempts++;

            // Si toutes les lumières sont nulles ou si une seule lumière est disponible et occupée
            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("[LightCtrl] Could not find available light after multiple attempts");
                return;
            }
        }
        while (directionalLights[newLightIndex] == null || newLightIndex == otherLightIndex);

        // Définir la nouvelle lumière active pour ce bouton
        if (buttonNumber == 1)
        {
            // Si le bouton 1 avait déjà une lumière active, l'éteindre
            if (button1LightIndex >= 0 && button1LightIndex < directionalLights.Length && directionalLights[button1LightIndex] != null)
            {
                directionalLights[button1LightIndex].enabled = false;
            }

            button1LightIndex = newLightIndex;
            button1Timer = lightDuration;
        }
        else // bouton 2
        {
            // Si le bouton 2 avait déjà une lumière active, l'éteindre
            if (button2LightIndex >= 0 && button2LightIndex < directionalLights.Length && directionalLights[button2LightIndex] != null)
            {
                directionalLights[button2LightIndex].enabled = false;
            }

            button2LightIndex = newLightIndex;
            button2Timer = lightDuration;
        }

        // Allumer la nouvelle lumière
        directionalLights[newLightIndex].enabled = true;
        Debug.Log($"[LightCtrl] Button {buttonNumber} activated light {newLightIndex} for {lightDuration} seconds");
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