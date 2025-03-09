import socket
import threading
import time
import logging
import queue
import paho.mqtt.client as mqtt

# Configuration du logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')
logger = logging.getLogger("paho-mqtt-broker")

class SimpleMQTTBroker:
    def __init__(self, host="0.0.0.0", port=1883):
        self.host = host
        self.port = port
        self.clients = {}  # Dictionnaire pour stocker les connexions clients
        self.topics = {}   # Dictionnaire pour stocker les abonnements aux topics
        self.message_queue = queue.Queue()  # File d'attente pour les messages
        self.running = False
        self.server_socket = None
    
    def start(self):
        """Démarre le broker MQTT"""
        self.running = True
        
        # Créer un socket serveur
        self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        self.server_socket.bind((self.host, self.port))
        self.server_socket.listen(5)
        
        logger.info(f"Broker MQTT démarré sur {self.host}:{self.port}")
        
        # Démarrer le thread pour traiter les messages
        message_processor = threading.Thread(target=self._process_messages)
        message_processor.daemon = True
        message_processor.start()
        
        # Accepter les connexions entrantes
        try:
            while self.running:
                try:
                    client_socket, address = self.server_socket.accept()
                    logger.info(f"Nouvelle connexion depuis {address}")
                    
                    # Créer un thread pour gérer ce client
                    client_handler = threading.Thread(
                        target=self._handle_client,
                        args=(client_socket, address)
                    )
                    client_handler.daemon = True
                    client_handler.start()
                except socket.error as e:
                    if self.running:
                        logger.error(f"Erreur de socket: {e}")
                    break
        finally:
            self.stop()
    
    def stop(self):
        """Arrête le broker MQTT"""
        logger.info("Arrêt du broker MQTT...")
        self.running = False
        
        # Fermer toutes les connexions clients
        for client_id, client_info in list(self.clients.items()):
            try:
                client_info['socket'].close()
            except:
                pass
        
        # Fermer le socket serveur
        if self.server_socket:
            try:
                self.server_socket.close()
            except:
                pass
        
        logger.info("Broker MQTT arrêté")
    
    def _handle_client(self, client_socket, address):
        """Gère une connexion client"""
        client_id = f"{address[0]}:{address[1]}"
        
        # Ajouter le client à notre dictionnaire
        self.clients[client_id] = {
            'socket': client_socket,
            'address': address,
            'subscriptions': set()
        }
        
        # Configurer un client Paho pour traiter les messages entrants
        client = mqtt.Client(client_id=client_id, protocol=mqtt.MQTTv311)
        
        # Configurer les callbacks
        client.on_connect = lambda client, userdata, flags, rc: logger.info(f"Client {client_id} connecté")
        client.on_disconnect = lambda client, userdata, rc: logger.info(f"Client {client_id} déconnecté")
        
        client.on_subscribe = lambda client, userdata, mid, granted_qos: self._on_client_subscribe(client_id, userdata['topic'])
        client.on_message = lambda client, userdata, msg: self._on_client_message(client_id, msg.topic, msg.payload)
        
        # Boucle pour recevoir et traiter les messages
        try:
            while self.running:
                # Lire les données du socket
                data = client_socket.recv(1024)
                if not data:
                    break  # Le client s'est déconnecté
                
                # Traiter les données MQTT avec Paho
                client._packet_read(data)
        except Exception as e:
            logger.error(f"Erreur avec le client {client_id}: {e}")
        finally:
            # Nettoyer les ressources
            self._remove_client(client_id)
    
    def _on_client_subscribe(self, client_id, topic):
        """Gère l'abonnement d'un client à un topic"""
        logger.info(f"Client {client_id} s'abonne au topic {topic}")
        
        # Ajouter ce topic aux abonnements du client
        if client_id in self.clients:
            self.clients[client_id]['subscriptions'].add(topic)
        
        # Ajouter ce client aux abonnés du topic
        if topic not in self.topics:
            self.topics[topic] = set()
        self.topics[topic].add(client_id)
    
    def _on_client_message(self, client_id, topic, payload):
        """Gère un message publié par un client"""
        logger.info(f"Message reçu du client {client_id} sur le topic {topic}")
        
        # Ajouter le message à la file d'attente pour traitement
        self.message_queue.put({
            'sender': client_id,
            'topic': topic,
            'payload': payload
        })
    
    def _process_messages(self):
        """Traite les messages dans la file d'attente et les distribue aux abonnés"""
        while self.running:
            try:
                # Attendre un message dans la file
                message = self.message_queue.get(timeout=1.0)
                
                topic = message['topic']
                sender = message['sender']
                payload = message['payload']
                
                # Trouver tous les clients abonnés à ce topic
                for subscribed_topic, subscribers in self.topics.items():
                    # Vérifier si le topic correspond (gestion basique des wildcards)
                    if self._topic_matches(subscribed_topic, topic):
                        # Envoyer le message à tous les abonnés sauf l'expéditeur
                        for subscriber_id in subscribers:
                            if subscriber_id != sender and subscriber_id in self.clients:
                                try:
                                    client_socket = self.clients[subscriber_id]['socket']
                                    # Construire et envoyer un paquet MQTT PUBLISH
                                    # (Simplification - en réalité, il faudrait utiliser la bibliothèque Paho correctement)
                                    client_socket.send(payload)
                                except Exception as e:
                                    logger.error(f"Erreur en envoyant au client {subscriber_id}: {e}")
            
            except queue.Empty:
                # Timeout de la file d'attente - continuer la boucle
                pass
            except Exception as e:
                logger.error(f"Erreur lors du traitement des messages: {e}")
    
    def _topic_matches(self, subscription, publish_topic):
        """Vérifie si un topic de publication correspond à un abonnement (gestion simplifiée des wildcards)"""
        # Implémentation très basique - à améliorer pour supporter les wildcards MQTT correctement
        if subscription == publish_topic:
            return True
        if subscription == "#":
            return True
        if subscription.endswith("/#") and publish_topic.startswith(subscription[:-2]):
            return True
        if "+" in subscription:
            # Conversion en expression régulière simple pour la correspondance
            import re
            pattern = subscription.replace("+", "[^/]+").replace("#", ".*")
            return re.match(f"^{pattern}$", publish_topic) is not None
        return False
    
    def _remove_client(self, client_id):
        """Supprime un client lorsqu'il se déconnecte"""
        if client_id in self.clients:
            # Supprimer les abonnements aux topics
            for topic in list(self.topics.keys()):
                if client_id in self.topics[topic]:
                    self.topics[topic].remove(client_id)
                if not self.topics[topic]:
                    del self.topics[topic]
            
            # Supprimer le client
            del self.clients[client_id]
            logger.info(f"Client {client_id} supprimé")

# Exemple d'utilisation
if __name__ == "__main__":
    broker = SimpleMQTTBroker(host="0.0.0.0", port=1883)
    try:
        broker.start()
    except KeyboardInterrupt:
        broker.stop()
