using UnityEngine;
using System.Collections.Generic;
using RtMidi.LowLevel;

public class LaunchpadPixelArt : MonoBehaviour
{
    // Référence au contrôleur Launchpad
    private MidiProbe _outProbe;
    private List<MidiOutPort> _outPorts = new List<MidiOutPort>();
    private MidiOutPort _launchpadOut;
    [SerializeField] private string deviceNameContains = "Launchpad";
    private bool deviceConnected = false;

    // Mapping des touches du Launchpad MK2
    private Dictionary<Vector2Int, int> padNotes = new Dictionary<Vector2Int, int>();

    // Définitions d'images pixel art préconfigurées
    public enum PixelArtImage
    {
        Alien,
        Ghost
    }

    [Header("Configuration")]
    public PixelArtImage selectedImage = PixelArtImage.Alien;

    void Start()
    {
        // Initialiser le mapping des touches
        InitializePadMapping();

        // Initialiser la connexion MIDI
        InitializeMIDI();

        // Afficher l'image sélectionnée
        if (deviceConnected)
        {
            DisplaySelectedImage();
        }
    }

    void Update()
    {
        // Vérifier si les ports ont changé
        if (_outProbe != null && _outPorts.Count != _outProbe.PortCount)
        {
            DisposePorts();
            ScanPorts();

            // Réafficher l'image si on est reconnecté
            if (deviceConnected)
            {
                DisplaySelectedImage();
            }
        }
    }

    void OnDestroy()
    {
        CleanupMIDI();
    }

    void InitializePadMapping()
    {
        // Le Launchpad MK2 a une grille 8x8
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                // Calculer la note MIDI (format standard du Launchpad MK2)
                // En utilisant (7-y) au lieu de y, nous inversons l'axe Y
                int noteNumber = 11 + x + ((7 - y) * 10);
                padNotes[new Vector2Int(x, y)] = noteNumber;
            }
        }
    }

    void InitializeMIDI()
    {
        _outProbe = new MidiProbe(MidiProbe.Mode.Out);
        ScanPorts();
    }

    void ScanPorts()
    {
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

    void DisposePorts()
    {
        foreach (var p in _outPorts) p?.Dispose();
        _outPorts.Clear();

        _launchpadOut = null;
        deviceConnected = false;
    }

    void CleanupMIDI()
    {
        // Éteindre toutes les LED
        if (deviceConnected && _launchpadOut != null)
        {
            ClearGrid();
        }

        // Libérer les ressources MIDI
        DisposePorts();
        _outProbe?.Dispose();

        Debug.Log("Connexions MIDI fermées");
    }

    // Éteindre toutes les LED
    public void ClearGrid()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Éteindre la grille principale 8x8
        foreach (var kvp in padNotes)
        {
            _launchpadOut.SendNoteOn(0, kvp.Value, 0); // 0 = OFF (éteint)
        }

        // Éteindre aussi les boutons de contrôle sur la droite
        int[] rightButtons = new int[] { 89, 79, 69, 59, 49, 39, 29, 19 };
        foreach (int note in rightButtons)
        {
            _launchpadOut.SendNoteOn(0, note, 0);
        }
    }

    // Afficher un pixel avec une couleur spécifique
    public void SetPixel(int x, int y, int color)
    {
        if (!deviceConnected || _launchpadOut == null) return;

        Vector2Int pos = new Vector2Int(x, y);
        if (padNotes.ContainsKey(pos))
        {
            _launchpadOut.SendNoteOn(0, padNotes[pos], color);
        }
    }

    // Afficher l'image sélectionnée
    public void DisplaySelectedImage()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // D'abord, effacer l'affichage
        ClearGrid();

        // Afficher l'image sélectionnée
        int[,] imageData = GetImageData(selectedImage);

        // Afficher l'image principale sur la grille 8x8
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, imageData[x, y]);
            }
        }

        // Afficher la palette de couleurs sur les boutons de droite
        DisplayColorPalette();
    }

    // Afficher la palette de couleurs sur les boutons de droite
    private void DisplayColorPalette()
    {
        // Ces notes MIDI correspondent aux boutons de droite (Volume, Pan, Send A, Send B, Stop, Mute, Solo, Record Arm)
        // Sur le Launchpad MK2, ces boutons ont les notes MIDI: 89, 79, 69, 59, 49, 39, 29, 19
        int[] rightButtons = new int[] { 89, 79, 69, 59, 49, 39, 29, 19 };

        if (selectedImage == PixelArtImage.Alien)
        {
            // Couleurs pour l'Alien: Off, Blanc, Vert lime clair, Vert lime vif, Vert lumineux, Vert foncé, Vert-bleu
            int[] alienColors = new int[] { 0, 3, 16, 17, 25, 29, 37 };

            // Afficher chaque couleur (on a 6 couleurs + le noir)
            for (int i = 0; i < alienColors.Length && i < rightButtons.Length; i++)
            {
                _launchpadOut.SendNoteOn(0, rightButtons[i], alienColors[i]);
            }
        }
        else if (selectedImage == PixelArtImage.Ghost)
        {
            // Couleurs pour le Ghost: Off, Blanc, Cyan, Bleu clair, Bleu
            int[] ghostColors = new int[] { 0, 3, 36, 41, 45 };

            // Afficher chaque couleur (on a 4 couleurs + le noir)
            for (int i = 0; i < ghostColors.Length && i < rightButtons.Length; i++)
            {
                _launchpadOut.SendNoteOn(0, rightButtons[i], ghostColors[i]);
            }
        }
    }

    // Récupérer les données de l'image sélectionnée
    private int[,] GetImageData(PixelArtImage image)
    {
        switch (image)
        {
            case PixelArtImage.Alien:
                return CreateAlienImage();
            case PixelArtImage.Ghost:
                return CreateGhostImage();
            default:
                return new int[8, 8];
        }
    }

    // Alien avec différentes nuances de vert
    private int[,] CreateAlienImage()
    {
        int[,] alien = new int[8, 8];

        // Initialiser avec 0 (OFF - éteint)
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                alien[x, y] = 0;
            }
        }

        // Dessiner un alien avec différentes nuances de vert
        // Antennes
        alien[3, 0] = 17; // Vert lime vif
        alien[4, 0] = 17; // Vert lime vif

        // Tête (partie supérieure)
        alien[2, 1] = 25; // Vert lumineux
        alien[3, 1] = 25; // Vert lumineux
        alien[4, 1] = 25; // Vert lumineux
        alien[5, 1] = 25; // Vert lumineux

        // Tête (milieu)
        alien[1, 2] = 17; // Vert lime vif
        alien[2, 2] = 16; // Vert clair
        alien[3, 2] = 16; // Vert clair
        alien[4, 2] = 16; // Vert clair
        alien[5, 2] = 16; // Vert clair
        alien[6, 2] = 17; // Vert lime vif

        // Yeux
        alien[2, 3] = 3;  // Blanc lumineux
        alien[3, 3] = 29; // Vert foncé
        alien[4, 3] = 29; // Vert foncé
        alien[5, 3] = 3;  // Blanc lumineux

        // Corps
        alien[2, 4] = 37; // Vert-bleu
        alien[3, 4] = 29; // Vert foncé
        alien[4, 4] = 29; // Vert foncé
        alien[5, 4] = 37; // Vert-bleu

        // Tentacules
        alien[1, 5] = 29; // Vert foncé
        alien[3, 5] = 17; // Vert lime vif
        alien[4, 5] = 17; // Vert lime vif
        alien[6, 5] = 29; // Vert foncé

        alien[0, 6] = 16; // Vert clair
        alien[2, 6] = 29; // Vert foncé
        alien[5, 6] = 29; // Vert foncé
        alien[7, 6] = 16; // Vert clair

        return alien;
    }

    // Fantôme avec différentes nuances de bleu
    private int[,] CreateGhostImage()
    {
        int[,] ghost = new int[8, 8];

        // Initialiser avec 0 (OFF - éteint)
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                ghost[x, y] = 0;
            }
        }

        // Dessiner un fantôme avec plusieurs nuances de bleu
        // Partie supérieure
        ghost[2, 1] = 41; // Bleu clair
        ghost[3, 1] = 41; // Bleu clair
        ghost[4, 1] = 41; // Bleu clair

        // Corps (partie haute)
        ghost[1, 2] = 41; // Bleu clair
        ghost[2, 2] = 36; // Cyan
        ghost[3, 2] = 36; // Cyan
        ghost[4, 2] = 36; // Cyan
        ghost[5, 2] = 41; // Bleu clair

        // Yeux (contour et intérieur)
        ghost[1, 3] = 45; // Bleu
        ghost[2, 3] = 3;  // Blanc lumineux
        ghost[3, 3] = 36; // Cyan
        ghost[4, 3] = 3;  // Blanc lumineux
        ghost[5, 3] = 45; // Bleu

        // Yeux (pupilles) et corps
        ghost[1, 4] = 45; // Bleu
        ghost[2, 4] = 45; // Bleu
        ghost[3, 4] = 45; // Bleu
        ghost[4, 4] = 45; // Bleu
        ghost[5, 4] = 45; // Bleu

        // Corps (milieu)
        ghost[1, 5] = 45; // Bleu
        ghost[2, 5] = 41; // Bleu clair
        ghost[3, 5] = 45; // Bleu
        ghost[4, 5] = 41; // Bleu clair
        ghost[5, 5] = 45; // Bleu

        // Corps (bas)
        ghost[1, 6] = 45; // Bleu
        ghost[2, 6] = 45; // Bleu
        ghost[3, 6] = 45; // Bleu
        ghost[4, 6] = 45; // Bleu
        ghost[5, 6] = 45; // Bleu

        // Tentacules
        ghost[1, 7] = 45; // Bleu
        ghost[3, 7] = 45; // Bleu
        ghost[5, 7] = 45; // Bleu

        return ghost;
    }
}