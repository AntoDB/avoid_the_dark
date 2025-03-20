using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script d'exemple pour utiliser le client MQTT direct dans Unity
/// </summary>
public class MQTTExample : MonoBehaviour
{
    // Référence au client MQTT
    private DirectMQTTClient mqttClient;

    // UI Elements (à assigner dans l'inspecteur)
    public Text messageText;
    public Text statusText;
    public Image button1Indicator;
    public Image button2Indicator;

    // Couleurs pour les indicateurs
    private Color defaultColor = Color.gray;
    private Color button1Color = Color.green;
    private Color button2Color = Color.blue;

    // Variables pour la réinitialisation des indicateurs
    private float resetTime = 0.5f;
    private float button1Timer = 0f;
    private float button2Timer = 0f;

    // Variable pour vérifier la connexion
    private float connectionCheckTimer = 0f;
    private float connectionCheckInterval = 2f;

    void Start()
    {
        Debug.Log("[UI] Starting MQTTExample");

        // S'assurer que le dispatcher de thread principal est disponible
        UnityMainThreadDispatcher.Instance();

        // Récupérer le client MQTT
        mqttClient = GetComponent<DirectMQTTClient>();

        if (mqttClient == null)
        {
            Debug.LogError("[UI] DirectMQTTClient component not found!");
            if (statusText != null)
                statusText.text = "ERREUR: Client MQTT non trouvé";
            return;
        }

        // S'abonner aux événements
        mqttClient.OnButton1Pressed += HandleButton1Pressed;
        mqttClient.OnButton2Pressed += HandleButton2Pressed;

        Debug.Log("[UI] Events subscribed");

        // Initialiser les indicateurs
        if (button1Indicator != null)
            button1Indicator.color = defaultColor;
        if (button2Indicator != null)
            button2Indicator.color = defaultColor;

        if (statusText != null)
            statusText.text = "Démarrage...";

        Debug.Log("[UI] UI initialized");
    }

    void Update()
    {
        // Mettre à jour les textes d'informations
        if (messageText != null)
        {
            string msg = mqttClient.GetLastMessage();
            messageText.text = string.IsNullOrEmpty(msg) ?
                "Aucun message reçu" :
                $"Dernier message: {msg}";
        }

        // Gérer les timers pour les indicateurs
        if (button1Timer > 0)
        {
            button1Timer -= Time.deltaTime;
            if (button1Timer <= 0 && button1Indicator != null)
            {
                button1Indicator.color = defaultColor;
            }
        }

        if (button2Timer > 0)
        {
            button2Timer -= Time.deltaTime;
            if (button2Timer <= 0 && button2Indicator != null)
            {
                button2Indicator.color = defaultColor;
            }
        }

        // Vérifier périodiquement la connexion
        connectionCheckTimer += Time.deltaTime;
        if (connectionCheckTimer >= connectionCheckInterval)
        {
            connectionCheckTimer = 0f;
            UpdateConnectionStatus();
        }
    }

    /// <summary>
    /// Met à jour le texte de statut avec l'état de la connexion
    /// </summary>
    private void UpdateConnectionStatus()
    {
        if (statusText != null && mqttClient != null)
        {
            if (mqttClient.IsConnected())
            {
                statusText.text = "Connecté au broker MQTT";
                statusText.color = Color.green;
            }
            else
            {
                statusText.text = "Déconnecté - Tentative de reconnexion...";
                statusText.color = Color.red;
            }
        }
    }

    /// <summary>
    /// Gestionnaire pour l'événement du bouton 1 pressé
    /// </summary>
    private void HandleButton1Pressed()
    {
        Debug.Log("[UI] Button 1 event received!");

        // Changer la couleur de l'indicateur
        if (button1Indicator != null)
        {
            button1Indicator.color = button1Color;
            button1Timer = resetTime;
        }
    }

    /// <summary>
    /// Gestionnaire pour l'événement du bouton 2 pressé
    /// </summary>
    private void HandleButton2Pressed()
    {
        Debug.Log("[UI] Button 2 event received!");

        // Changer la couleur de l'indicateur
        if (button2Indicator != null)
        {
            button2Indicator.color = button2Color;
            button2Timer = resetTime;
        }
    }

    /// <summary>
    /// Méthode appelée lors de la destruction de l'objet
    /// </summary>
    private void OnDestroy()
    {
        Debug.Log("[UI] MQTTExample - OnDestroy");

        // Désabonner des événements
        if (mqttClient != null)
        {
            mqttClient.OnButton1Pressed -= HandleButton1Pressed;
            mqttClient.OnButton2Pressed -= HandleButton2Pressed;
        }
    }
}