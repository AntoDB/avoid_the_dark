#include <WiFi.h>
#include <PubSubClient.h>
#include "credentials.h"  // Inclure le fichier de credentials (reprendre "credentials_example.h", le renommer "credentials.h" et changer les informations de connexion)

// Configuration MQTT device-specific
const char* mqtt_client_id = "ESP32-1";    // ID client unique
const char* mqtt_topic = "ESP32-1";        // Topic sur lequel publier

// Variables
WiFiClient espClient;
PubSubClient client(espClient);
unsigned long lastMsg = 0;
int value = 0;

void setup_wifi() {
  delay(10);
  Serial.println();
  Serial.print("Connexion à ");
  Serial.println(ssid);

  WiFi.begin(ssid, password);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }

  Serial.println("");
  Serial.println("WiFi connecté");
  Serial.print("Adresse IP: ");
  Serial.println(WiFi.localIP());
}

void reconnect() {
  // Boucle jusqu'à reconnexion
  while (!client.connected()) {
    Serial.print("Tentative de connexion MQTT...");
    
    // Tentative de connexion
    bool connected;
    if (mqtt_user != "" && mqtt_password != "") {
      connected = client.connect(mqtt_client_id, mqtt_user, mqtt_password);
    } else {
      connected = client.connect(mqtt_client_id);
    }
    
    if (connected) {
      Serial.println("connecté");
      
      // Une fois connecté, publier un message d'annonce...
      client.publish(mqtt_topic, "ESP32 connecté");
      
      // ... et s'abonner si nécessaire
      // client.subscribe("commandes");  // Décommentez pour vous abonner à un topic
    } else {
      Serial.print("échec, rc=");
      Serial.print(client.state());
      Serial.println(" nouvelle tentative dans 5 secondes");
      delay(5000);
    }
  }
}

void setup() {
  Serial.begin(115200);
  setup_wifi();
  client.setServer(mqtt_server, mqtt_port);
  // client.setCallback(callback);  // Décommentez si vous voulez recevoir des messages
}

/*
// Décommentez cette fonction si vous souhaitez recevoir des messages
void callback(char* topic, byte* payload, unsigned int length) {
  Serial.print("Message reçu [");
  Serial.print(topic);
  Serial.print("] ");
  
  String message;
  for (int i = 0; i < length; i++) {
    message += (char)payload[i];
  }
  Serial.println(message);

  // Traiter le message reçu ici
}
*/

void loop() {
  // Vérifier la connexion MQTT
  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  // Publier une valeur toutes les 2 secondes
  unsigned long now = millis();
  if (now - lastMsg > 2000) {
    lastMsg = now;
    
    // Incrémenter la valeur
    value++;
    
    // Convertir la valeur en chaîne de caractères
    char msg[10];
    snprintf(msg, 10, "%d", value);
    
    Serial.print("Publication du message: ");
    Serial.println(msg);
    
    // Publier sur le topic ESP32-1
    client.publish(mqtt_topic, msg);
  }
}
