using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RtMidi.LowLevel;

public class LaunchpadMK2Controller : MonoBehaviour
{
    // Paramètres MIDI
    private MidiProbe _inProbe;
    private MidiProbe _outProbe;
    private List<MidiInPort> _inPorts = new List<MidiInPort>();
    private List<MidiOutPort> _outPorts = new List<MidiOutPort>();
    private MidiInPort _launchpadIn;
    private MidiOutPort _launchpadOut;
    private string deviceNameContains = "Launchpad";
    private bool deviceConnected = false;

    // Mapping des touches du Launchpad MK2
    private Dictionary<int, Vector2> padPositions = new Dictionary<int, Vector2>();

    // Couleurs pour le retour visuel sur le Launchpad
    [System.Serializable]
    public enum LaunchpadColor
    {
        OFF = 0,
        RED_LOW = 1,
        RED_FULL = 3,
        AMBER_LOW = 17,
        AMBER_FULL = 19,
        YELLOW = 35,
        GREEN_LOW = 16,
        GREEN_FULL = 48,
        BLUE_LOW = 33,
        BLUE_FULL = 51,
        PURPLE = 49
    }

    // Sélection de la couleur active pour les pads
    [Header("Configuration des couleurs")]
    public LaunchpadColor activeColor = LaunchpadColor.GREEN_FULL;
    public LaunchpadColor inactiveColor = LaunchpadColor.OFF;
    public bool runInitialLightTest = true;

    void Start()
    {
        // Initialiser le mapping des positions du pad
        InitializePadPositions();

        // Initialiser les connexions MIDI
        InitializeMIDI();

        // Lancer le test d'éclairage si demandé
        if (runInitialLightTest && deviceConnected)
        {
            StartCoroutine(TestLEDs());
        }
    }

    void Update()
    {
        // Vérifier si les ports ont changé
        if (_inProbe != null && _inPorts.Count != _inProbe.PortCount)
        {
            DisposePorts();
            ScanPorts();
        }

        // Traiter les messages MIDI entrants
        foreach (var port in _inPorts)
        {
            port?.ProcessMessages();
        }
    }

    void OnDestroy()
    {
        // Nettoyage des connexions MIDI
        CleanupMIDI();
    }

    void InitializePadPositions()
    {
        // Le Launchpad MK2 a une grille 8x8
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                // Calculer la note MIDI (0-based)
                // Format standard du Launchpad MK2
                int noteNumber = 11 + x + (y * 10);
                padPositions.Add(noteNumber, new Vector2(x, 7 - y));
            }
        }
    }

    void InitializeMIDI()
    {
        _inProbe = new MidiProbe(MidiProbe.Mode.In);
        _outProbe = new MidiProbe(MidiProbe.Mode.Out);

        ScanPorts();
    }

    // Parcourir et ouvrir tous les ports disponibles
    void ScanPorts()
    {
        // Recherche des ports d'entrée
        for (int i = 0; i < _inProbe.PortCount; i++)
        {
            string portName = _inProbe.GetPortName(i);
            Debug.Log("MIDI-in port found: " + portName);

            if (portName.Contains(deviceNameContains))
            {
                Debug.Log("Launchpad trouvé en entrée: " + portName);

                var port = new MidiInPort(i);

                // Configuration des événements
                port.OnNoteOn = HandleNoteOn;
                port.OnNoteOff = HandleNoteOff;
                port.OnControlChange = HandleControlChange;

                _inPorts.Add(port);
                _launchpadIn = port;

                deviceConnected = true;
            }
            else
            {
                _inPorts.Add(null);
            }
        }

        // Recherche des ports de sortie
        for (int i = 0; i < _outProbe.PortCount; i++)
        {
            string portName = _outProbe.GetPortName(i);
            Debug.Log("MIDI-out port found: " + portName);

            if (portName.Contains(deviceNameContains))
            {
                Debug.Log("Launchpad trouvé en sortie: " + portName);

                var port = new MidiOutPort(i);

                _outPorts.Add(port);
                _launchpadOut = port;

                deviceConnected = true;
            }
            else
            {
                _outPorts.Add(null);
            }
        }
    }

    // Fermer et libérer tous les ports ouverts
    void DisposePorts()
    {
        foreach (var p in _inPorts) p?.Dispose();
        _inPorts.Clear();

        foreach (var p in _outPorts) p?.Dispose();
        _outPorts.Clear();

        _launchpadIn = null;
        _launchpadOut = null;
        deviceConnected = false;
    }

    void HandleNoteOn(byte channel, byte note, byte velocity)
    {
        // Vérifier si la note correspond à un pad
        if (padPositions.ContainsKey(note))
        {
            Vector2 position = padPositions[note];
            Debug.Log($"Pad pressé à la position ({position.x}, {position.y}) avec vélocité {velocity}");

            // Allumer la LED avec la couleur active
            SendColorToLaunchpad(note, (byte)activeColor);

            // Déclencher l'action correspondante
            TriggerAction((int)position.x, (int)position.y);
        }
    }

    void HandleNoteOff(byte channel, byte note)
    {
        // Vérifier si la note correspond à un pad
        if (padPositions.ContainsKey(note))
        {
            Vector2 position = padPositions[note];
            Debug.Log($"Pad relâché à la position ({position.x}, {position.y})");

            // Éteindre la LED ou mettre la couleur inactive
            SendColorToLaunchpad(note, (byte)inactiveColor);
        }
    }

    void HandleControlChange(byte channel, byte number, byte value)
    {
        Debug.Log($"Contrôleur changé: {number} avec valeur {value}");
        // Traiter les messages de contrôle si nécessaire
    }

    void SendColorToLaunchpad(byte note, byte colorVelocity)
    {
        if (!deviceConnected || _launchpadOut == null) return;

        try
        {
            // Envoyer un message Note On avec la vélocité correspondant à la couleur
            _launchpadOut.SendNoteOn(0, note, colorVelocity);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors de l'envoi du message MIDI: {e.Message}");
        }
    }

    IEnumerator TestLEDs()
    {
        if (!deviceConnected) yield break;

        // Éteindre toutes les LED
        ResetAllLEDs();

        yield return new WaitForSeconds(0.5f);

        // Test de pattern sur le Launchpad
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int note = 11 + x + (y * 10);
                SendColorToLaunchpad((byte)note, (byte)LaunchpadColor.RED_LOW);
                yield return new WaitForSeconds(0.02f);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // Changer la couleur
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int note = 11 + x + (y * 10);
                SendColorToLaunchpad((byte)note, (byte)LaunchpadColor.GREEN_LOW);
                yield return new WaitForSeconds(0.02f);
            }
        }

        yield return new WaitForSeconds(0.3f);

        // Une autre couleur
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int note = 11 + x + (y * 10);
                SendColorToLaunchpad((byte)note, (byte)LaunchpadColor.BLUE_LOW);
                yield return new WaitForSeconds(0.02f);
            }
        }

        yield return new WaitForSeconds(0.5f);

        // Éteindre toutes les LED
        ResetAllLEDs();
    }

    void ResetAllLEDs()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                int note = 11 + x + (y * 10);
                SendColorToLaunchpad((byte)note, (byte)LaunchpadColor.OFF);
            }
        }
    }

    void TriggerAction(int x, int y)
    {
        // Implémentez ici ce que vous voulez que chaque pad fasse dans Unity

        // Exemples d'actions par rangée
        switch (y)
        {
            case 0:
                // Première rangée - actions audio
                AudioActions(x);
                break;
            case 1:
                // Deuxième rangée - actions visuelles
                VisualActions(x);
                break;
                // etc.
        }
    }

    void AudioActions(int index)
    {
        // Exemple: déclencher différents sons
        Debug.Log($"Déclenchement action audio {index}");
        // AudioSource.PlayClipAtPoint(audioClips[index], transform.position);
    }

    void VisualActions(int index)
    {
        // Exemple: changer des effets visuels
        Debug.Log($"Déclenchement action visuelle {index}");
        // visualEffects[index].Play();
    }

    void CleanupMIDI()
    {
        // Éteindre toutes les LED
        if (deviceConnected && _launchpadOut != null)
        {
            ResetAllLEDs();
        }

        // Libérer les ressources MIDI
        DisposePorts();

        _inProbe?.Dispose();
        _outProbe?.Dispose();

        Debug.Log("Connexions MIDI fermées");
    }
}