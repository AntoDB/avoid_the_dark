#include <Arduino.h>

// Configuration des pins
#define DMX_TX_PIN 17       // TX pin
#define DMX_DIR_PIN 5       // Direction pin

// Configuration DMX
#define DMX_SERIAL_PORT Serial2
#define DMX_UNIVERSE_SIZE 512
#define DMX_BREAK_MICROS 180
#define DMX_MAB_MICROS 20

// Buffer DMX
uint8_t dmxBuffer[DMX_UNIVERSE_SIZE + 1];

// Variables pour le test des canaux
int currentChannel = 0;  // Commence à 0 pour la phase d'initialisation
#define FIRST_CHANNEL 1
#define LAST_CHANNEL 50  // Dernier canal à tester
#define CHANNEL_TEST_DURATION 1000  // Durée du test pour chaque canal (ms)
#define TEST_VALUE 100  // Valeur à 100%

unsigned long lastChannelChange = 0;
bool initializationDone = false;

void setup() {
  // Initialiser le port série pour le debugging
  Serial.begin(115200);
  Serial.println("\n\nESP32 DMX Test Séquentiel - Canal par Canal");
  
  // Configuration de la pin de direction
  pinMode(DMX_DIR_PIN, OUTPUT);
  digitalWrite(DMX_DIR_PIN, HIGH);  // HIGH pour émettre
  
  // Initialiser le buffer DMX avec des zéros
  memset(dmxBuffer, 0, DMX_UNIVERSE_SIZE + 1);
  dmxBuffer[0] = 0;  // START code
  
  // Initialiser le port série DMX
  DMX_SERIAL_PORT.begin(250000, SERIAL_8N2);
  
  // Attendre que le port série soit prêt
  delay(1000);
  
  Serial.println("Phase 1: Mise à zéro de tous les canaux...");
  
  // Envoyer quelques trames avec tous les canaux à zéro
  for (int i = 0; i < 20; i++) {
    sendDMXUniverse();
    delay(50);
  }
  
  Serial.println("Phase 2: Test canal par canal...");
  
  // Commencer le test séquentiel
  lastChannelChange = millis();
  currentChannel = FIRST_CHANNEL - 1;  // On incrémente au premier cycle
}

void loop() {
  // Vérifier s'il est temps de passer au canal suivant
  if (millis() - lastChannelChange >= CHANNEL_TEST_DURATION) {
    // Si on était sur un canal actif, le remettre à zéro
    if (currentChannel >= FIRST_CHANNEL) {
      setChannelValue(currentChannel, 0);
    }
    
    // Passer au canal suivant
    currentChannel++;
    
    // Si nous avons dépassé le dernier canal à tester
    if (currentChannel > LAST_CHANNEL) {
      Serial.println("Fin du cycle de test. Redémarrage...");
      // Remettre tous les canaux à zéro pendant quelques secondes
      memset(dmxBuffer + 1, 0, DMX_UNIVERSE_SIZE);
      
      // Envoyer quelques trames avec tous les canaux à zéro
      for (int i = 0; i < 20; i++) {
        sendDMXUniverse();
        delay(50);
      }
      
      // Recommencer au premier canal
      currentChannel = FIRST_CHANNEL;
    }
    
    // Mettre le canal actuel à 100%
    setChannelValue(currentChannel, TEST_VALUE);
    
    // Afficher l'information sur le canal actuel
    Serial.print("Test du canal DMX #");
    Serial.print(currentChannel);
    Serial.print(" à 100% (valeur 255) pendant ");
    Serial.print(CHANNEL_TEST_DURATION / 1000);
    Serial.println(" secondes...");
    
    // Enregistrer le moment du changement
    lastChannelChange = millis();
  }
  
  // Envoyer l'univers DMX plusieurs fois par cycle
  for (int i = 0; i < 3; i++) {
    sendDMXUniverse();
    delay(10);
  }
  
  // Un petit délai pour la stabilité
  delay(30);
}

// Fonction pour définir la valeur d'un canal DMX
void setChannelValue(uint16_t channel, uint8_t value) {
  if (channel > 0 && channel <= DMX_UNIVERSE_SIZE) {
    dmxBuffer[channel] = value;
  }
}

// Fonction pour envoyer l'univers DMX complet
void sendDMXUniverse() {
  // S'assurer que la pin de direction est en mode émission
  digitalWrite(DMX_DIR_PIN, HIGH);
  
  // 1. Break
  DMX_SERIAL_PORT.flush();
  DMX_SERIAL_PORT.end();
  
  // Générer le signal BREAK en mettant TX à LOW
  pinMode(DMX_TX_PIN, OUTPUT);
  digitalWrite(DMX_TX_PIN, LOW);
  delayMicroseconds(DMX_BREAK_MICROS);
  
  // 2. Mark After Break (MAB)
  digitalWrite(DMX_TX_PIN, HIGH);
  delayMicroseconds(DMX_MAB_MICROS);
  
  // 3. Redémarrer la communication série
  DMX_SERIAL_PORT.begin(250000, SERIAL_8N2);
  
  // 4. Envoyer toutes les données DMX
  DMX_SERIAL_PORT.write(dmxBuffer, DMX_UNIVERSE_SIZE + 1);
}
