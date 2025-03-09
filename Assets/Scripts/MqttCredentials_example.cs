using UnityEngine;

/// <summary>
/// Composant pour stocker les credentials MQTT séparément du client
/// </summary>
public class MQTTCredentials_example : MonoBehaviour // RENAME TO "MQTTCredentials"
{
    [Header("MQTT Broker Configuration")]
    [Tooltip("Adresse IP ou nom d'hôte du broker MQTT")]
    public string brokerAddress = "192.168.1.100";

    [Tooltip("Port du broker MQTT (généralement 1883)")]
    public int brokerPort = 1883;

    [Header("Authentication")]
    [Tooltip("Nom d'utilisateur MQTT (laissez vide si pas d'authentification)")]
    public string mqttUserName = "";

    [Tooltip("Mot de passe MQTT (laissez vide si pas d'authentification)")]
    public string mqttPassword = "";
}
