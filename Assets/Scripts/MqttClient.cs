using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

/// <summary>
/// Client MQTT direct pour Unity, sans hériter de M2MqttUnityClient
/// </summary>
public class DirectMQTTClient : MonoBehaviour
{
    [Header("MQTT Configuration")]
    public string brokerAddress = "192.168.1.100";
    public int brokerPort = 1883;
    public string mqttUsername = "";
    public string mqttPassword = "";
    public string clientId = "UnityClient";
    public string topicSubscribe = "ESP32-1";

    [Header("Debug Options")]
    public bool verboseLogging = true;

    // Variables pour stocker les données reçues
    private string lastMessageReceived = "";
    private bool button1Pressed = false;
    private bool button2Pressed = false;

    // Événements que vous pouvez utiliser pour déclencher des actions dans Unity
    public event Action OnButton1Pressed;
    public event Action OnButton2Pressed;

    // Client MQTT
    private MqttClient mqttClient;
    private bool isConnected = false;
    private float reconnectTimer = 0f;
    private float reconnectInterval = 5f;

    // File d'attente pour les messages MQTT
    private Queue<MqttEvent> messageQueue = new Queue<MqttEvent>();
    private object queueLock = new object();

    // Structure pour stocker les infos des messages MQTT
    private struct MqttEvent
    {
        public string Topic;
        public string Message;
    }

    void Start()
    {
        Log("Starting MQTT client...");

        // Générer un ID client unique s'il n'est pas spécifié
        if (string.IsNullOrEmpty(clientId))
        {
            clientId = "UnityClient_" + Guid.NewGuid().ToString().Substring(0, 8);
        }

        ConnectToMqttBroker();
    }

    /// <summary>
    /// Se connecte au broker MQTT
    /// </summary>
    private void ConnectToMqttBroker()
    {
        try
        {
            Log($"Connecting to MQTT broker at {brokerAddress}:{brokerPort}...");

            // Créer le client MQTT
            mqttClient = new MqttClient(brokerAddress, brokerPort, false, null, null, MqttSslProtocols.None);

            // Définir le gestionnaire de messages
            mqttClient.MqttMsgPublishReceived += OnMqttMessageReceived;

            // Définir le gestionnaire de connexion/déconnexion
            mqttClient.ConnectionClosed += OnMqttConnectionClosed;

            // Se connecter au broker
            string clientIdToUse = clientId + "_" + DateTime.Now.Ticks;
            Log($"Using client ID: {clientIdToUse}");

            byte connectResult;
            if (!string.IsNullOrEmpty(mqttUsername) && !string.IsNullOrEmpty(mqttPassword))
            {
                connectResult = mqttClient.Connect(clientIdToUse, mqttUsername, mqttPassword);
            }
            else
            {
                connectResult = mqttClient.Connect(clientIdToUse);
            }

            // Vérifier le résultat de la connexion
            isConnected = mqttClient.IsConnected;

            if (isConnected)
            {
                Log("Connected to MQTT broker successfully!");

                // S'abonner au topic
                mqttClient.Subscribe(new string[] { topicSubscribe }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
                Log($"Subscribed to topic: {topicSubscribe}");
            }
            else
            {
                LogError($"Failed to connect to MQTT broker. Result code: {connectResult}");
            }
        }
        catch (Exception e)
        {
            LogError($"Exception while connecting to MQTT broker: {e.Message}");
            isConnected = false;
        }
    }

    /// <summary>
    /// Gestionnaire d'événement appelé lorsqu'un message MQTT est reçu
    /// </summary>
    private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        try
        {
            string topic = e.Topic;
            string message = Encoding.UTF8.GetString(e.Message);

            // Au lieu d'utiliser le dispatcher, on ajoute simplement le message à une file d'attente
            lock (queueLock)
            {
                messageQueue.Enqueue(new MqttEvent { Topic = topic, Message = message });
            }
        }
        catch (Exception ex)
        {
            // On ne peut pas appeler LogError directement ici car on est sur un thread secondaire
            // On garde la chaîne d'erreur pour la logger plus tard
            string errorMessage = ex.Message;
            lock (queueLock)
            {
                messageQueue.Enqueue(new MqttEvent { Topic = "ERROR", Message = errorMessage });
            }
        }
    }

    /// <summary>
    /// Traite les messages MQTT en attente sur le thread principal
    /// </summary>
    private void ProcessMessageQueue()
    {
        // N'exécutez cette méthode que sur le thread principal (Update)
        if (messageQueue.Count == 0) return;

        MqttEvent[] events;
        lock (queueLock)
        {
            events = messageQueue.ToArray();
            messageQueue.Clear();
        }

        foreach (var mqttEvent in events)
        {
            if (mqttEvent.Topic == "ERROR")
            {
                LogError($"Error processing MQTT message: {mqttEvent.Message}");
                continue;
            }

            Log($"Message received: '{mqttEvent.Message}' from topic: {mqttEvent.Topic}");

            // Stocker le message
            lastMessageReceived = mqttEvent.Message;

            // Vérifier le contenu du message
            if (mqttEvent.Message == "1")
            {
                button1Pressed = true;
                Log("Button 1 pressed!");
                OnButton1Pressed?.Invoke();
            }
            else if (mqttEvent.Message == "2")
            {
                button2Pressed = true;
                Log("Button 2 pressed!");
                OnButton2Pressed?.Invoke();
            }
        }
    }

    /// <summary>
    /// Gestionnaire d'événement appelé lorsque la connexion MQTT est fermée
    /// </summary>
    private void OnMqttConnectionClosed(object sender, EventArgs e)
    {
        // On ne peut pas appeler LogWarning directement, on passe par la file d'attente
        lock (queueLock)
        {
            messageQueue.Enqueue(new MqttEvent { Topic = "CONNECTION_CLOSED", Message = "" });
        }
        isConnected = false;
    }

    void Update()
    {
        // Traiter les messages MQTT en attente
        ProcessMessageQueue();

        // Vérifier la connexion et tenter de se reconnecter si nécessaire
        if (!isConnected)
        {
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer >= reconnectInterval)
            {
                reconnectTimer = 0f;
                ConnectToMqttBroker();
            }
        }

        // Logs périodiques de statut si en mode verbeux
        if (verboseLogging && Time.frameCount % 300 == 0)
        {
            Log($"Connection status: {(isConnected ? "Connected" : "Disconnected")}");
        }
    }

    /// <summary>
    /// Méthode pour récupérer l'état du bouton 1 et réinitialiser
    /// </summary>
    public bool GetButton1PressedAndReset()
    {
        if (button1Pressed)
        {
            button1Pressed = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Méthode pour récupérer l'état du bouton 2 et réinitialiser
    /// </summary>
    public bool GetButton2PressedAndReset()
    {
        if (button2Pressed)
        {
            button2Pressed = false;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Méthode pour récupérer le dernier message reçu
    /// </summary>
    public string GetLastMessage()
    {
        return lastMessageReceived;
    }

    /// <summary>
    /// Méthode pour vérifier si le client est connecté
    /// </summary>
    public bool IsConnected()
    {
        return isConnected;
    }

    /// <summary>
    /// Méthode pour se déconnecter proprement
    /// </summary>
    private void Disconnect()
    {
        if (mqttClient != null && mqttClient.IsConnected)
        {
            try
            {
                mqttClient.Disconnect();
                Log("Disconnected from MQTT broker");
            }
            catch (Exception e)
            {
                LogError($"Error disconnecting from MQTT broker: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Méthode appelée lors de la destruction de l'objet
    /// </summary>
    private void OnDestroy()
    {
        Disconnect();
    }

    /// <summary>
    /// Méthode appelée lors de la fermeture de l'application
    /// </summary>
    private void OnApplicationQuit()
    {
        Disconnect();
    }

    /// <summary>
    /// Méthodes de logging avec préfixe MQTT pour faciliter le filtrage
    /// </summary>
    private void Log(string message)
    {
        Debug.Log($"[MQTT] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MQTT] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MQTT] {message}");
    }
}