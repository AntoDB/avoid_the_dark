#include <WiFi.h>
#include <PubSubClient.h>
#include "credentials.h"  // Inclure le fichier de credentials (reprendre "credentials_example.h", le renommer "credentials.h" et changer les informations de connexion)

// Configuration MQTT device-specific
const char* mqtt_client_id = "ESP32-1";    // ID client unique
const char* mqtt_topic = "ESP32-1";        // Topic sur lequel publier

// Configuration des boutons
const int bouton1 = 12;  // Bouton 1 sur GPIO 12
const int bouton2 = 14;  // Bouton 2 sur GPIO 14

// Variables pour la détection de front montant
int etatBouton1 = LOW;
int etatPrecedentBouton1 = LOW;
int etatBouton2 = LOW;
int etatPrecedentBouton2 = LOW;

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
  
  // Configuration des broches des boutons en entrée
  pinMode(bouton1, INPUT);
  pinMode(bouton2, INPUT);
  
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

// Fonction pour publier un message sur le topic MQTT
void publierMessage(const char* message) {
  Serial.print("Publication du message: ");
  Serial.println(message);
  
  // Publier sur le topic ESP32-1
  client.publish(mqtt_topic, message);
}

void loop() {
  // Vérifier la connexion MQTT
  if (!client.connected()) {
    reconnect();
  }
  client.loop();

  // Lire l'état actuel des boutons
  etatBouton1 = digitalRead(bouton1);
  etatBouton2 = digitalRead(bouton2);

  // Détecter le front montant pour le bouton 1
  if (etatBouton1 == HIGH && etatPrecedentBouton1 == LOW) {
    publierMessage("1");  // Envoyer "1" quand le bouton 1 est pressé
    delay(50);  // Petit délai pour éviter les rebonds
  }
  
  // Détecter le front montant pour le bouton 2
  if (etatBouton2 == HIGH && etatPrecedentBouton2 == LOW) {
    publierMessage("2");  // Envoyer "2" quand le bouton 2 est pressé
    delay(50);  // Petit délai pour éviter les rebonds
  }

  // Sauvegarder l'état précédent des boutons
  etatPrecedentBouton1 = etatBouton1;
  etatPrecedentBouton2 = etatBouton2;

  // Délai court pour stabiliser la lecture des boutons
  delay(10);
}
