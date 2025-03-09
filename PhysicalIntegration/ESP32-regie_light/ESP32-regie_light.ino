/*
 * ESP32 DMX Controller - Version ultra simple
 * 
 * Configure les canaux:
 * - Canal 1 à 200
 * - Canal 4 à 200
 * - Canal 8 à 255
 * - Canal 12 à 255
 * - Canal 26 à 255
 * - Canal 30 à 255
 */

#include <Arduino.h>

// Configuration des pins
#define DMX_TX_PIN 17
#define DMX_DE_PIN 21

// Tableau DMX (512 canaux + startcode)
uint8_t dmxData[513];

void setup() {
  // Communication série pour monitoring
  Serial.begin(115200);
  delay(1000);
  Serial.println("ESP32 DMX Controller - TX uniquement");
  
  // Configurer le pin DE
  pinMode(DMX_DE_PIN, OUTPUT);
  digitalWrite(DMX_DE_PIN, HIGH);  // Activer transmission
  
  // Initialiser la communication série pour DMX
  Serial2.begin(250000, SERIAL_8N2, -1, DMX_TX_PIN);
  
  // Initialiser le tableau DMX à 0
  for (int i = 0; i < 513; i++) {
    dmxData[i] = 0;
  }
  
  // Configurer les canaux demandés
  dmxData[0] = 0;     // Start code (toujours 0 pour DMX standard)
  dmxData[1] = 200;   // Canal 1 à 200
  dmxData[4] = 200;   // Canal 4 à 200
  dmxData[8] = 255;   // Canal 8 à 255
  dmxData[12] = 255;  // Canal 12 à 255
  dmxData[26] = 255;  // Canal 26 à 255
  dmxData[30] = 255;  // Canal 30 à 255
  
  Serial.println("Configuration DMX terminée");
}

void loop() {
  // Envoyer le paquet DMX complet
  Serial2.write(dmxData, 513);
  
  // Attendre un peu avant de renvoyer
  delay(30);
}
