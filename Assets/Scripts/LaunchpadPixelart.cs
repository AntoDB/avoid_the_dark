using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RtMidi.LowLevel;

public class LaunchpadPixelArtCreator : MonoBehaviour
{
    // Paramètres MIDI
    private MidiProbe _inProbe;
    private MidiProbe _outProbe;
    private List<MidiInPort> _inPorts = new List<MidiInPort>();
    private List<MidiOutPort> _outPorts = new List<MidiOutPort>();
    private MidiInPort _launchpadIn;
    private MidiOutPort _launchpadOut;
    [SerializeField] private string deviceNameContains = "Launchpad";
    private bool deviceConnected = false;

    // Mapping des touches du Launchpad MK2
    private Dictionary<int, Vector2Int> padPositions = new Dictionary<int, Vector2Int>();
    private Dictionary<Vector2Int, int> positionToPad = new Dictionary<Vector2Int, int>();

    // Tableau des boutons de droite (Volume, Pan, Send A, etc.)
    private int[] rightButtons = new int[] { 89, 79, 69, 59, 49, 39, 29, 19 };

    // Images disponibles
    public enum PixelArtImage
    {
        Alien,
        Ghost
    }

    [Header("Configuration")]
    public PixelArtImage targetImage = PixelArtImage.Alien;

    // Variable entière pour contrôler l'affichage
    [Header("Contrôle d'Affichage")]
    [Tooltip("0 = Éteint, 1 = Alien, 2 = Ghost")]
    [Range(0, 2)]
    public int displayMode = 0;

    [Header("Animation")]
    [SerializeField] private float blinkSpeed = 0.3f;

    // Grille de création de l'utilisateur
    private int[,] userGrid = new int[8, 8];

    // Couleur sélectionnée actuellement
    private int selectedColor = 0;
    private int selectedColorIndex = 0;

    // Couleurs disponibles selon l'image
    private int[] alienColors = new int[] { 0, 3, 16, 17, 25, 29, 37 };
    private int[] ghostColors = new int[] { 0, 3, 36, 41, 45 };

    private int previousDisplayMode = -1; // Pour détecter les changements

    // Variables pour le clignotement
    private bool isBlinking = false;
    private Coroutine blinkCoroutine = null;

    // Active/désactive la vérification automatique
    private bool autoVerification = true;

    void Start()
    {
        // Initialiser le mapping des touches
        InitializePadMapping();

        // Initialiser la connexion MIDI
        InitializeMIDI();

        // Initialiser la grille utilisateur
        ClearUserGrid();

        // Afficher la palette de couleurs
        DisplayColorPalette();
    }

    void Update()
    {
        // Vérifier si les ports ont changé
        if (_inProbe != null && _inPorts.Count != _inProbe.PortCount)
        {
            DisposePorts();
            ScanPorts();

            if (deviceConnected)
            {
                UpdateDisplay();
            }
        }

        // Vérifier si le mode d'affichage a changé
        if (displayMode != previousDisplayMode)
        {
            previousDisplayMode = displayMode;
            UpdateDisplay();
        }

        // Traiter les messages MIDI entrants
        foreach (var port in _inPorts)
        {
            port?.ProcessMessages();
        }
    }

    void OnDestroy()
    {
        StopBlinking();
        // Nettoyage des connexions MIDI
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
                int noteNumber = 11 + x + (y * 10);
                padPositions.Add(noteNumber, new Vector2Int(x, 7 - y));
                positionToPad.Add(new Vector2Int(x, 7 - y), noteNumber);
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
        if (velocity == 0) return; // Ignorer les messages Note On avec vélocité 0 (équivalent à Note Off)

        // Vérifier si c'est un bouton latéral (pour sélectionner une couleur)
        bool isRightButton = false;
        int buttonIndex = -1;

        for (int i = 0; i < rightButtons.Length; i++)
        {
            if (note == rightButtons[i])
            {
                isRightButton = true;
                buttonIndex = i;
                break;
            }
        }

        if (isRightButton)
        {
            // C'est un bouton latéral - sélectionner la couleur
            int[] availableColors = targetImage == PixelArtImage.Alien ? alienColors : ghostColors;

            if (buttonIndex < availableColors.Length)
            {
                // Arrêter l'ancien clignotement s'il existe
                StopBlinking();

                selectedColor = availableColors[buttonIndex];
                selectedColorIndex = buttonIndex;
                Debug.Log($"Couleur sélectionnée: {selectedColor}");

                // Démarrer le clignotement pour le bouton sélectionné
                StartBlinking(rightButtons[buttonIndex], selectedColor);
            }
        }
        else if (padPositions.ContainsKey(note))
        {
            // C'est un bouton de la grille principale - appliquer la couleur sélectionnée
            Vector2Int position = padPositions[note];

            // Coordonnées inversées car padPositions utilise (7-y)
            int gridX = position.x;
            int gridY = position.y;

            // Mettre à jour la grille utilisateur
            userGrid[gridX, gridY] = selectedColor;

            // Envoyer la couleur au Launchpad
            _launchpadOut.SendNoteOn(channel, note, (byte)selectedColor);

            Debug.Log($"Couleur {selectedColor} appliquée à la position ({gridX}, {gridY})");

            // Vérification automatique après chaque changement
            if (autoVerification)
            {
                CheckCompletionStatus();
            }
        }
    }

    void HandleNoteOff(byte channel, byte note)
    {
        // Nous n'avons pas besoin de faire quoi que ce soit ici pour notre application
    }

    void HandleControlChange(byte channel, byte number, byte value)
    {
        Debug.Log($"Contrôleur changé: {number} avec valeur {value}");
        // Traiter les messages de contrôle si nécessaire
    }

    // Démarrer le clignotement d'un bouton
    private void StartBlinking(int noteNumber, int color)
    {
        StopBlinking(); // S'assurer qu'aucun autre clignotement n'est en cours

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkButton(noteNumber, color));
    }

    // Arrêter le clignotement
    private void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        isBlinking = false;

        // Restaurer l'affichage normal de la palette
        DisplayColorPalette();
    }

    // Coroutine pour faire clignoter un bouton
    private IEnumerator BlinkButton(int noteNumber, int color)
    {
        int highIntensityColor = color;
        int lowIntensityColor = 0; // Éteint

        // Si c'est le noir (0), utiliser le blanc avec faible vélocité pour contraster
        if (color == 0)
        {
            highIntensityColor = 1; // Blanc faible intensité (1 au lieu de 3)
        }

        while (isBlinking)
        {
            // Allumer le bouton (couleur originale)
            _launchpadOut.SendNoteOn(0, noteNumber, (byte)highIntensityColor);
            yield return new WaitForSeconds(blinkSpeed);

            // Éteindre le bouton
            _launchpadOut.SendNoteOn(0, noteNumber, (byte)lowIntensityColor);
            yield return new WaitForSeconds(blinkSpeed);
        }
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

        _inProbe?.Dispose();
        _outProbe?.Dispose();

        Debug.Log("Connexions MIDI fermées");
    }

    // Éteindre toutes les LED
    public void ClearGrid()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Éteindre la grille principale 8x8
        foreach (var kvp in padPositions)
        {
            _launchpadOut.SendNoteOn(0, (byte)kvp.Key, 0); // 0 = OFF (éteint)
        }

        // Éteindre aussi les boutons de contrôle sur la droite
        foreach (int note in rightButtons)
        {
            _launchpadOut.SendNoteOn(0, (byte)note, 0);
        }
    }

    // Réinitialiser la grille de l'utilisateur
    public void ClearUserGrid()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                userGrid[x, y] = 0; // 0 = OFF (éteint)
            }
        }

        DisplayUserGrid();
    }

    // Afficher un pixel avec une couleur spécifique
    public void SetPixel(int x, int y, int color)
    {
        if (!deviceConnected || _launchpadOut == null) return;

        Vector2Int pos = new Vector2Int(x, y);
        if (positionToPad.ContainsKey(pos))
        {
            int note = positionToPad[pos];
            _launchpadOut.SendNoteOn(0, (byte)note, (byte)color);
        }
    }

    // Mettre à jour l'affichage selon le mode sélectionné
    private void UpdateDisplay()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Arrêter tout clignotement en cours
        StopBlinking();

        // Réinitialiser la grille et la couleur sélectionnée
        selectedColor = 0;
        selectedColorIndex = 0;

        // Ensuite afficher selon le mode
        switch (displayMode)
        {
            case 0: // Éteint
                ClearGrid();
                ClearUserGrid();
                break;
            case 1: // Alien
                targetImage = PixelArtImage.Alien;
                ClearUserGrid();
                // Afficher seulement la palette de couleurs pour le dessin
                DisplayColorPalette();
                break;
            case 2: // Ghost
                targetImage = PixelArtImage.Ghost;
                ClearUserGrid();
                // Afficher seulement la palette de couleurs pour le dessin
                DisplayColorPalette();
                break;
        }
    }

    // Afficher la grille utilisateur
    public void DisplayUserGrid()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, userGrid[x, y]);
            }
        }
    }

    // Afficher la palette de couleurs sur les boutons de droite
    private void DisplayColorPalette()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Si le clignotement est actif, ne pas interrompre
        if (isBlinking) return;

        // D'abord, éteindre tous les boutons latéraux
        foreach (int note in rightButtons)
        {
            _launchpadOut.SendNoteOn(0, (byte)note, 0);
        }

        // Ensuite, afficher les couleurs disponibles
        int[] availableColors = targetImage == PixelArtImage.Alien ? alienColors : ghostColors;

        for (int i = 0; i < availableColors.Length && i < rightButtons.Length; i++)
        {
            _launchpadOut.SendNoteOn(0, (byte)rightButtons[i], (byte)availableColors[i]);
        }
    }

    private IEnumerator ShowTargetImagePreview(float previewDuration)
    {
        // Récupérer les données de l'image cible
        int[,] targetImageData = GetImageData(targetImage);

        // Afficher l'image cible
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, targetImageData[x, y]);
            }
        }

        // Attendre la durée spécifiée
        yield return new WaitForSeconds(previewDuration);

        // Revenir à la grille vide
        ClearUserGrid();
        DisplayColorPalette();
    }

    // Fonction pour changer l'image cible
    public void SetTargetImage(PixelArtImage image)
    {
        StopBlinking();
        targetImage = image;
        selectedColor = 0; // Réinitialiser la couleur sélectionnée
        selectedColorIndex = 0;
        DisplayColorPalette();
    }

    // Méthode pour vérifier la création avec l'image cible
    public void VerifyPixelArt()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        int[,] targetImageData = GetImageData(targetImage);
        bool isCorrect = true;

        // Comparer pixel par pixel
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (userGrid[x, y] != targetImageData[x, y])
                {
                    // Marquer en rouge les pixels incorrects
                    SetPixel(x, y, 5); // 5 = rouge vif
                    isCorrect = false;
                }
                else
                {
                    // Marquer en vert les pixels corrects
                    SetPixel(x, y, 48); // 48 = vert vif
                }
            }
        }

        // Afficher un feedback global (clignotement de toute la grille en vert si correct)
        if (isCorrect)
        {
            StartCoroutine(FlashEntireGrid(48, 3)); // Flash vert puis blanc pour célébrer
            Debug.Log("Bravo! L'image est correcte!");
        }
        else
        {
            Debug.Log("Il y a des erreurs dans l'image. Les pixels incorrects sont en rouge.");
        }
    }

    // Flash de la grille entière dans une couleur
    private IEnumerator FlashEntireGrid(int color1, int color2)
    {
        for (int i = 0; i < 3; i++)
        {
            // Premier flash
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, color1);
                }
            }
            yield return new WaitForSeconds(0.3f);

            // Deuxième flash
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, color2);
                }
            }
            yield return new WaitForSeconds(0.3f);
        }

        // Retour à l'affichage normal
        DisplayUserGrid();
    }

    // Récupérer les données de l'image cible
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
    private void CheckCompletionStatus()
    {
        // Récupérer les données de l'image cible
        int[,] targetImageData = GetImageData(targetImage);
        bool isCorrect = true;

        // Comparer pixel par pixel
        for (int y = 0; y < 8 && isCorrect; y++)
        {
            for (int x = 0; x < 8 && isCorrect; x++)
            {
                if (userGrid[x, y] != targetImageData[x, y])
                {
                    isCorrect = false;
                }
            }
        }

        // Si tout correspond, afficher un message et célébrer
        if (isCorrect)
        {
            Debug.Log("BRAVO! Vous avez parfaitement reproduit l'image " + targetImage.ToString() + "!");
            StartCoroutine(CelebrateCompletion());
        }
    }

    private IEnumerator CelebrateCompletion()
    {
        // Sauvegarder l'état actuel de la grille
        int[,] savedGrid = new int[8, 8];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                savedGrid[x, y] = userGrid[x, y];
            }
        }

        // Animation de célébration : faire clignoter la grille en vert
        for (int i = 0; i < 3; i++)
        {
            // Colorer tout en vert
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, 21); // Vert vif
                }
            }
            yield return new WaitForSeconds(0.2f);

            // Restaurer l'image originale
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, savedGrid[x, y]);
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void SetAutoVerification(bool enabled)
    {
        autoVerification = enabled;
        Debug.Log("Vérification automatique " + (enabled ? "activée" : "désactivée"));
    }

    // Gestion publique de l'affichage
    public void SetDisplayMode(int mode)
    {
        if (mode >= 0 && mode <= 2)
        {
            displayMode = mode;
            UpdateDisplay();
        }
    }

    // Fonctions utilitaires pour faciliter l'intégration
    public void TurnOff()
    {
        SetDisplayMode(0);
    }

    public void ShowAlien()
    {
        SetDisplayMode(1);
    }

    public void ShowGhost()
    {
        SetDisplayMode(2);
    }

    // Basculer entre les modes
    public void ToggleNextMode()
    {
        displayMode = (displayMode + 1) % 3; // Cycle entre 0, 1, 2
        UpdateDisplay();
    }
}