// credentials.h - Fichier contenant toutes les informations d'identification

#ifndef CREDENTIALS_H
#define CREDENTIALS_H

// Configuration WiFi
const char* ssid = "VOTRE_SSID";          // Remplacez par le nom de votre réseau WiFi
const char* password = "VOTRE_PASSWORD";  // Remplacez par le mot de passe de votre réseau WiFi

// Configuration MQTT
const char* mqtt_server = "192.168.1.100"; // Remplacez par l'adresse IP de votre broker MQTT
const int mqtt_port = 1883;                // Port standard MQTT
const char* mqtt_user = "";                // Nom d'utilisateur MQTT (laissez vide si pas d'authentification)
const char* mqtt_password = "";            // Mot de passe MQTT (laissez vide si pas d'authentification)

#endif // CREDENTIALS_H
