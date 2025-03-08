using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MidiJack;

public class LaunchpadMK2Controller : MonoBehaviour
{
    // Mapping des touches du Launchpad MK2
    private Dictionary<int, Vector2> padPositions = new Dictionary<int, Vector2>();

    // Couleurs pour le retour visuel sur le Launchpad
    private enum LaunchpadColor
    {
        OFF = 0,
        RED_LOW = 1,
        RED_FULL = 3,
        AMBER_LOW = 17,
        AMBER_FULL = 19,
        YELLOW = 35,
        GREEN_LOW = 16,
        GREEN_FULL = 48
    }

    void Start()
    {
        // Initialiser le mapping des positions du pad
        InitializePadPositions();

        // Initialiser la connexion MIDI
        InitializeMIDI();

        // Debug pour vérifier si MidiJack fonctionne
        Debug.Log("MidiJack initialisé - vérifiez la console pour les événements MIDI");
    }

    void InitializePadPositions()
    {
        // Le Launchpad MK2 a une grille 8x8
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                // Calculer la note MIDI (0-based)
                int noteNumber = 11 + x + (y * 10);
                padPositions.Add(noteNumber, new Vector2(x, 7 - y));
            }
        }
    }

    void InitializeMIDI()
    {
        // Configuration spécifique à MidiJack
        MidiMaster.noteOnDelegate += OnNoteOn;
        MidiMaster.noteOffDelegate += OnNoteOff;
        MidiMaster.knobDelegate += OnKnobChange;

        Debug.Log("Abonnement aux événements MIDI effectué");
    }

    void OnNoteOn(MidiChannel channel, int note, float velocity)
    {
        if (padPositions.ContainsKey(note))
        {
            Vector2 position = padPositions[note];
            Debug.Log($"Pad pressé à la position ({position.x}, {position.y}) avec vélocité {velocity}");

            // Ici, vous pouvez déclencher vos actions Unity en fonction du pad pressé
            TriggerAction((int)position.x, (int)position.y);

            // Pour l'instant, nous ne pouvons pas envoyer de couleurs directement
            // avec cette version de MidiJack, voir les commentaires ci-dessous
        }
    }

    void OnNoteOff(MidiChannel channel, int note)
    {
        if (padPositions.ContainsKey(note))
        {
            Vector2 position = padPositions[note];
            Debug.Log($"Pad relâché à la position ({position.x}, {position.y})");
        }
    }

    // Fonction pour réagir aux changements de contrôleurs
    void OnKnobChange(MidiChannel channel, int knobNumber, float knobValue)
    {
        Debug.Log($"Contrôleur changé: {knobNumber} avec valeur {knobValue}");
        // Traiter les boutons de fonction ici si nécessaire
    }

    // Note: La version standard de MidiJack ne dispose pas d'une méthode SendNoteOn
    // Pour envoyer des messages MIDI au Launchpad, nous devrons étendre MidiJack
    // ou utiliser une autre bibliothèque comme RtMidi.Core

    void TriggerAction(int x, int y)
    {
        // Implémentez ici ce que vous voulez que chaque pad fasse dans Unity
        // Exemple:
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

    // Fonction Update pour tester l'entrée MIDI en temps réel
    void Update()
    {
        // Vérifier si des touches MIDI sont actuellement pressées
        for (int i = 0; i < 128; i++)
        {
            if (MidiMaster.GetKey(i) > 0)
            {
                Debug.Log($"Note MIDI {i} active avec vélocité {MidiMaster.GetKey(i)}");
            }
        }
    }

    void OnApplicationQuit()
    {
        // Avec MidiJack, vous devez vous désabonner des événements
        MidiMaster.noteOnDelegate -= OnNoteOn;
        MidiMaster.noteOffDelegate -= OnNoteOff;
        MidiMaster.knobDelegate -= OnKnobChange;
    }
}