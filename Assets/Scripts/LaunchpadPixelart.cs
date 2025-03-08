using UnityEngine;
using System.Collections;
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

    // Palette de couleurs disponibles sur le Launchpad MK2
    // Basée sur la carte de couleurs officielle du Launchpad MK2
    [System.Serializable]
    public enum LaunchpadColor
    {
        OFF = 0,            // Éteint

        // Blancs
        WHITE_DIM = 1,      // Blanc faible
        WHITE_MID = 2,      // Blanc moyen
        WHITE_BRIGHT = 3,   // Blanc lumineux

        // Rouges
        RED_LIGHT = 4,      // Rouge clair
        RED_BRIGHT = 5,     // Rouge vif
        RED_DARK = 6,       // Rouge foncé

        // Oranges
        ORANGE = 9,         // Orange
        ORANGE_DARK = 10,   // Orange foncé
        BROWN = 11,         // Marron

        // Jaunes
        YELLOW_LIGHT = 13,  // Jaune clair
        YELLOW_BRIGHT = 14, // Jaune vif
        LIME_DARK = 15,     // Vert lime foncé

        // Verts
        GREEN_LIGHT = 40,   // Vert clair
        GREEN_BRIGHT = 41,  // Vert vif
        GREEN_DARK = 43,    // Vert foncé
        GREEN_BLUE = 45,    // Vert-bleu

        // Bleus
        CYAN_LIGHT = 37,    // Cyan clair
        CYAN = 38,          // Cyan
        BLUE_LIGHT = 45,    // Bleu clair
        BLUE = 46,          // Bleu
        BLUE_DARK = 47,     // Bleu foncé

        // Violets
        PURPLE_LIGHT = 50,  // Violet clair
        PURPLE = 51,        // Violet
        PURPLE_DARK = 52,   // Violet foncé

        // Roses
        PINK_LIGHT = 53,    // Rose clair
        PINK = 57,          // Rose
        PINK_BRIGHT = 89,   // Rose vif

        // Verts spéciaux
        BRIGHT_GREEN = 48,  // Vert lumineux
        BRIGHT_GREEN_2 = 49,// Vert lumineux 2
        LIME_BRIGHT = 74,   // Lime vif
        LIME_LIGHT = 75     // Lime clair
    }

    // Définitions d'images pixel art préconfigurées
    public enum PixelArtImage
    {
        Heart,
        Smile,
        Alien,
        Pacman,
        Ghost,
        Mario,
        Mushroom,
        Space_Invader,
        Custom
    }

    [Header("Configuration")]
    public PixelArtImage selectedImage = PixelArtImage.Heart;

    [Header("Animation")]
    public bool animateImage = false;
    public float frameDelay = 0.5f;

    [Header("Custom Image")]
    [Tooltip("Définir les couleurs pixel par pixel. Utilisé uniquement si l'image est Custom.")]
    public LaunchpadColor[,] customImage = new LaunchpadColor[8, 8];

    private bool isAnimating = false;
    private Coroutine animationCoroutine;

    void Start()
    {
        // Initialiser le mapping des touches
        InitializePadMapping();

        // Initialiser la connexion MIDI
        InitializeMIDI();

        // Par défaut, initialisons le tableau customImage avec des valeurs OFF
        if (customImage.Length == 0)
        {
            customImage = new LaunchpadColor[8, 8];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    customImage[x, y] = LaunchpadColor.OFF;
                }
            }
        }

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

        // Mettre à jour l'animation si nécessaire
        if (animateImage && !isAnimating && deviceConnected)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(AnimateImage());
        }
        else if (!animateImage && isAnimating)
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                isAnimating = false;
            }

            // Réafficher l'image statique
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

        foreach (var kvp in padNotes)
        {
            _launchpadOut.SendNoteOn(0, kvp.Value, (int)LaunchpadColor.OFF);
        }
    }

    // Afficher un pixel avec une couleur spécifique
    public void SetPixel(int x, int y, LaunchpadColor color)
    {
        SetPixel(x, y, (int)color);
    }

    // Surcharge avec int pour le color
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
        LaunchpadColor[,] imageData = GetImageData(selectedImage);

        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, imageData[x, y]);
            }
        }
    }

    // Récupérer les données de l'image sélectionnée
    private LaunchpadColor[,] GetImageData(PixelArtImage image)
    {
        switch (image)
        {
            case PixelArtImage.Heart:
                return CreateHeartImage();
            case PixelArtImage.Smile:
                return CreateSmileImage();
            case PixelArtImage.Alien:
                return CreateAlienImage();
            case PixelArtImage.Pacman:
                return CreatePacmanImage();
            case PixelArtImage.Ghost:
                return CreateGhostImage();
            case PixelArtImage.Mario:
                return CreateMarioImage();
            case PixelArtImage.Mushroom:
                return CreateMushroomImage();
            case PixelArtImage.Space_Invader:
                return CreateSpaceInvaderImage();
            case PixelArtImage.Custom:
                return customImage;
            default:
                return new LaunchpadColor[8, 8];
        }
    }

    // Animation simple
    private IEnumerator AnimateImage()
    {
        isAnimating = true;

        while (animateImage && deviceConnected)
        {
            // Animation selon l'image sélectionnée
            switch (selectedImage)
            {
                case PixelArtImage.Heart:
                    yield return AnimateHeart();
                    break;
                case PixelArtImage.Pacman:
                    yield return AnimatePacman();
                    break;
                case PixelArtImage.Ghost:
                    yield return AnimateGhost();
                    break;
                default:
                    // Pour les autres images, on fait un simple clignotement
                    DisplaySelectedImage();
                    yield return new WaitForSeconds(frameDelay);
                    ClearGrid();
                    yield return new WaitForSeconds(frameDelay);
                    break;
            }
        }

        isAnimating = false;
    }

    // Animation spécifique pour le cœur
    private IEnumerator AnimateHeart()
    {
        // Cœur qui bat: petit -> grand -> petit
        LaunchpadColor[,] smallHeart = CreateSmallHeartImage();
        LaunchpadColor[,] bigHeart = CreateHeartImage();

        // Afficher le petit cœur
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, smallHeart[x, y]);
            }
        }

        yield return new WaitForSeconds(frameDelay);

        // Afficher le grand cœur
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, bigHeart[x, y]);
            }
        }

        yield return new WaitForSeconds(frameDelay);
    }

    // Animation Pacman
    private IEnumerator AnimatePacman()
    {
        LaunchpadColor[,] pacmanOpen = CreatePacmanImage();
        LaunchpadColor[,] pacmanClosed = CreatePacmanClosedImage();

        // Pacman ouvert
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, pacmanOpen[x, y]);
            }
        }

        yield return new WaitForSeconds(frameDelay);

        // Pacman fermé
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, pacmanClosed[x, y]);
            }
        }

        yield return new WaitForSeconds(frameDelay);
    }

    // Animation fantôme
    private IEnumerator AnimateGhost()
    {
        LaunchpadColor[,] ghost1 = CreateGhostImage();
        LaunchpadColor[,] ghost2 = CreateGhostImage2();

        // Premier frame
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, ghost1[x, y]);
            }
        }

        yield return new WaitForSeconds(frameDelay);

        // Deuxième frame
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, ghost2[x, y]);
            }
        }

        yield return new WaitForSeconds(frameDelay);
    }

    // Définitions des images pixel art
    private LaunchpadColor[,] CreateHeartImage()
    {
        LaunchpadColor[,] heart = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                heart[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un cœur rouge
        heart[1, 1] = LaunchpadColor.RED_BRIGHT;
        heart[2, 1] = LaunchpadColor.RED_BRIGHT;
        heart[4, 1] = LaunchpadColor.RED_BRIGHT;
        heart[5, 1] = LaunchpadColor.RED_BRIGHT;

        heart[0, 2] = LaunchpadColor.RED_BRIGHT;
        heart[1, 2] = LaunchpadColor.RED_BRIGHT;
        heart[2, 2] = LaunchpadColor.RED_BRIGHT;
        heart[3, 2] = LaunchpadColor.RED_BRIGHT;
        heart[4, 2] = LaunchpadColor.RED_BRIGHT;
        heart[5, 2] = LaunchpadColor.RED_BRIGHT;
        heart[6, 2] = LaunchpadColor.RED_BRIGHT;

        heart[0, 3] = LaunchpadColor.RED_BRIGHT;
        heart[1, 3] = LaunchpadColor.RED_BRIGHT;
        heart[2, 3] = LaunchpadColor.RED_BRIGHT;
        heart[3, 3] = LaunchpadColor.RED_BRIGHT;
        heart[4, 3] = LaunchpadColor.RED_BRIGHT;
        heart[5, 3] = LaunchpadColor.RED_BRIGHT;
        heart[6, 3] = LaunchpadColor.RED_BRIGHT;

        heart[1, 4] = LaunchpadColor.RED_BRIGHT;
        heart[2, 4] = LaunchpadColor.RED_BRIGHT;
        heart[3, 4] = LaunchpadColor.RED_BRIGHT;
        heart[4, 4] = LaunchpadColor.RED_BRIGHT;
        heart[5, 4] = LaunchpadColor.RED_BRIGHT;

        heart[2, 5] = LaunchpadColor.RED_BRIGHT;
        heart[3, 5] = LaunchpadColor.RED_BRIGHT;
        heart[4, 5] = LaunchpadColor.RED_BRIGHT;

        heart[3, 6] = LaunchpadColor.RED_BRIGHT;

        return heart;
    }

    private LaunchpadColor[,] CreateSmallHeartImage()
    {
        LaunchpadColor[,] heart = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                heart[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un petit cœur rouge
        heart[2, 2] = LaunchpadColor.RED_BRIGHT;
        heart[4, 2] = LaunchpadColor.RED_BRIGHT;

        heart[1, 3] = LaunchpadColor.RED_BRIGHT;
        heart[2, 3] = LaunchpadColor.RED_BRIGHT;
        heart[3, 3] = LaunchpadColor.RED_BRIGHT;
        heart[4, 3] = LaunchpadColor.RED_BRIGHT;
        heart[5, 3] = LaunchpadColor.RED_BRIGHT;

        heart[2, 4] = LaunchpadColor.RED_BRIGHT;
        heart[3, 4] = LaunchpadColor.RED_BRIGHT;
        heart[4, 4] = LaunchpadColor.RED_BRIGHT;

        heart[3, 5] = LaunchpadColor.RED_BRIGHT;

        return heart;
    }

    private LaunchpadColor[,] CreateSmileImage()
    {
        LaunchpadColor[,] smile = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                smile[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un smiley jaune
        smile[2, 1] = LaunchpadColor.YELLOW_BRIGHT;
        smile[3, 1] = LaunchpadColor.YELLOW_BRIGHT;
        smile[4, 1] = LaunchpadColor.YELLOW_BRIGHT;
        smile[5, 1] = LaunchpadColor.YELLOW_BRIGHT;

        smile[1, 2] = LaunchpadColor.YELLOW_BRIGHT;
        smile[2, 2] = LaunchpadColor.YELLOW_BRIGHT;
        smile[3, 2] = LaunchpadColor.YELLOW_BRIGHT;
        smile[4, 2] = LaunchpadColor.YELLOW_BRIGHT;
        smile[5, 2] = LaunchpadColor.YELLOW_BRIGHT;
        smile[6, 2] = LaunchpadColor.YELLOW_BRIGHT;

        smile[1, 3] = LaunchpadColor.YELLOW_BRIGHT;
        smile[2, 3] = LaunchpadColor.YELLOW_BRIGHT;
        smile[3, 3] = LaunchpadColor.YELLOW_BRIGHT;
        smile[4, 3] = LaunchpadColor.YELLOW_BRIGHT;
        smile[5, 3] = LaunchpadColor.YELLOW_BRIGHT;
        smile[6, 3] = LaunchpadColor.YELLOW_BRIGHT;

        // Yeux
        smile[2, 4] = LaunchpadColor.BLUE;
        smile[5, 4] = LaunchpadColor.BLUE;

        // Bouche
        smile[1, 5] = LaunchpadColor.RED_BRIGHT;
        smile[2, 6] = LaunchpadColor.RED_BRIGHT;
        smile[3, 6] = LaunchpadColor.RED_BRIGHT;
        smile[4, 6] = LaunchpadColor.RED_BRIGHT;
        smile[5, 6] = LaunchpadColor.RED_BRIGHT;
        smile[6, 5] = LaunchpadColor.RED_BRIGHT;

        return smile;
    }

    private LaunchpadColor[,] CreateAlienImage()
    {
        LaunchpadColor[,] alien = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                alien[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un alien vert
        alien[3, 0] = LaunchpadColor.GREEN_BRIGHT;
        alien[4, 0] = LaunchpadColor.GREEN_BRIGHT;

        alien[2, 1] = LaunchpadColor.GREEN_BRIGHT;
        alien[3, 1] = LaunchpadColor.GREEN_BRIGHT;
        alien[4, 1] = LaunchpadColor.GREEN_BRIGHT;
        alien[5, 1] = LaunchpadColor.GREEN_BRIGHT;

        alien[1, 2] = LaunchpadColor.GREEN_BRIGHT;
        alien[2, 2] = LaunchpadColor.GREEN_BRIGHT;
        alien[3, 2] = LaunchpadColor.GREEN_BRIGHT;
        alien[4, 2] = LaunchpadColor.GREEN_BRIGHT;
        alien[5, 2] = LaunchpadColor.GREEN_BRIGHT;
        alien[6, 2] = LaunchpadColor.GREEN_BRIGHT;

        // Yeux
        alien[2, 3] = LaunchpadColor.WHITE_BRIGHT;
        alien[3, 3] = LaunchpadColor.GREEN_BRIGHT;
        alien[4, 3] = LaunchpadColor.GREEN_BRIGHT;
        alien[5, 3] = LaunchpadColor.WHITE_BRIGHT;

        alien[2, 4] = LaunchpadColor.GREEN_BRIGHT;
        alien[3, 4] = LaunchpadColor.GREEN_BRIGHT;
        alien[4, 4] = LaunchpadColor.GREEN_BRIGHT;
        alien[5, 4] = LaunchpadColor.GREEN_BRIGHT;

        // Antennes
        alien[1, 5] = LaunchpadColor.GREEN_BRIGHT;
        alien[3, 5] = LaunchpadColor.GREEN_BRIGHT;
        alien[4, 5] = LaunchpadColor.GREEN_BRIGHT;
        alien[6, 5] = LaunchpadColor.GREEN_BRIGHT;

        alien[0, 6] = LaunchpadColor.GREEN_BRIGHT;
        alien[2, 6] = LaunchpadColor.GREEN_BRIGHT;
        alien[5, 6] = LaunchpadColor.GREEN_BRIGHT;
        alien[7, 6] = LaunchpadColor.GREEN_BRIGHT;

        return alien;
    }

    private LaunchpadColor[,] CreatePacmanImage()
    {
        LaunchpadColor[,] pacman = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                pacman[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner Pacman jaune (bouche ouverte)
        pacman[2, 1] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 1] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 1] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[5, 2] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 3] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 3] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 3] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 3] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 4] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 4] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 4] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 5] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 5] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 5] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 5] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[5, 6] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[2, 7] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 7] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 7] = LaunchpadColor.YELLOW_BRIGHT;

        return pacman;
    }

    private LaunchpadColor[,] CreatePacmanClosedImage()
    {
        LaunchpadColor[,] pacman = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                pacman[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner Pacman jaune (bouche fermée)
        pacman[2, 1] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 1] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 1] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 2] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[5, 2] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 3] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 3] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 3] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 3] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[5, 3] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 4] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 4] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 4] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 4] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[5, 4] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 5] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 5] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 5] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 5] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[5, 5] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[1, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[2, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 6] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[5, 6] = LaunchpadColor.YELLOW_BRIGHT;

        pacman[2, 7] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[3, 7] = LaunchpadColor.YELLOW_BRIGHT;
        pacman[4, 7] = LaunchpadColor.YELLOW_BRIGHT;

        return pacman;
    }

    private LaunchpadColor[,] CreateGhostImage()
    {
        LaunchpadColor[,] ghost = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                ghost[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un fantôme bleu clair
        ghost[2, 1] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 1] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 1] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[5, 2] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 3] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 3] = LaunchpadColor.WHITE_BRIGHT;
        ghost[3, 3] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 3] = LaunchpadColor.WHITE_BRIGHT;
        ghost[5, 3] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 4] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 4] = LaunchpadColor.BLUE;
        ghost[3, 4] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 4] = LaunchpadColor.BLUE;
        ghost[5, 4] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[5, 5] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[5, 6] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 7] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 7] = LaunchpadColor.CYAN_LIGHT;
        ghost[5, 7] = LaunchpadColor.CYAN_LIGHT;

        return ghost;
    }

    private LaunchpadColor[,] CreateGhostImage2()
    {
        LaunchpadColor[,] ghost = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                ghost[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un fantôme bleu clair (frame 2)
        ghost[2, 1] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 1] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 1] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 2] = LaunchpadColor.CYAN_LIGHT;
        ghost[5, 2] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 3] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 3] = LaunchpadColor.WHITE_BRIGHT;
        ghost[3, 3] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 3] = LaunchpadColor.WHITE_BRIGHT;
        ghost[5, 3] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 4] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 4] = LaunchpadColor.BLUE;
        ghost[3, 4] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 4] = LaunchpadColor.BLUE;
        ghost[5, 4] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 5] = LaunchpadColor.CYAN_LIGHT;
        ghost[5, 5] = LaunchpadColor.CYAN_LIGHT;

        ghost[1, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[3, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 6] = LaunchpadColor.CYAN_LIGHT;
        ghost[5, 6] = LaunchpadColor.CYAN_LIGHT;

        // Pieds différents pour l'animation
        ghost[0, 7] = LaunchpadColor.CYAN_LIGHT;
        ghost[2, 7] = LaunchpadColor.CYAN_LIGHT;
        ghost[4, 7] = LaunchpadColor.CYAN_LIGHT;
        ghost[6, 7] = LaunchpadColor.CYAN_LIGHT;

        return ghost;
    }

    private LaunchpadColor[,] CreateMarioImage()
    {
        LaunchpadColor[,] mario = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                mario[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner Mario
        // Casquette rouge
        mario[2, 1] = LaunchpadColor.RED_BRIGHT;
        mario[3, 1] = LaunchpadColor.RED_BRIGHT;
        mario[4, 1] = LaunchpadColor.RED_BRIGHT;

        // Tête
        mario[2, 2] = LaunchpadColor.RED_BRIGHT;
        mario[3, 2] = LaunchpadColor.RED_BRIGHT;
        mario[4, 2] = LaunchpadColor.RED_BRIGHT;
        mario[5, 2] = LaunchpadColor.ORANGE;

        mario[1, 3] = LaunchpadColor.ORANGE;
        mario[2, 3] = LaunchpadColor.ORANGE;
        mario[3, 3] = LaunchpadColor.ORANGE;
        mario[4, 3] = LaunchpadColor.ORANGE;
        mario[5, 3] = LaunchpadColor.ORANGE;

        // Moustache et yeux
        mario[2, 4] = LaunchpadColor.ORANGE;
        mario[3, 4] = LaunchpadColor.ORANGE;
        mario[4, 4] = LaunchpadColor.OFF;  // Représente le noir
        mario[5, 4] = LaunchpadColor.ORANGE;

        // Corps
        mario[2, 5] = LaunchpadColor.RED_BRIGHT;
        mario[3, 5] = LaunchpadColor.RED_BRIGHT;
        mario[4, 5] = LaunchpadColor.RED_BRIGHT;
        mario[5, 5] = LaunchpadColor.RED_BRIGHT;

        // Bras et jambes
        mario[1, 5] = LaunchpadColor.ORANGE;
        mario[6, 5] = LaunchpadColor.ORANGE;

        mario[3, 6] = LaunchpadColor.BLUE;
        mario[4, 6] = LaunchpadColor.BLUE;

        mario[2, 7] = LaunchpadColor.ORANGE;
        mario[5, 7] = LaunchpadColor.ORANGE;

        return mario;
    }

    private LaunchpadColor[,] CreateMushroomImage()
    {
        LaunchpadColor[,] mushroom = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                mushroom[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un champignon de Mario
        // Chapeau rouge à pois blancs
        mushroom[2, 1] = LaunchpadColor.RED_BRIGHT;
        mushroom[3, 1] = LaunchpadColor.RED_BRIGHT;
        mushroom[4, 1] = LaunchpadColor.RED_BRIGHT;
        mushroom[5, 1] = LaunchpadColor.RED_BRIGHT;

        mushroom[1, 2] = LaunchpadColor.RED_BRIGHT;
        mushroom[2, 2] = LaunchpadColor.RED_BRIGHT;
        mushroom[3, 2] = LaunchpadColor.WHITE_BRIGHT;
        mushroom[4, 2] = LaunchpadColor.RED_BRIGHT;
        mushroom[5, 2] = LaunchpadColor.RED_BRIGHT;
        mushroom[6, 2] = LaunchpadColor.RED_BRIGHT;

        mushroom[1, 3] = LaunchpadColor.RED_BRIGHT;
        mushroom[2, 3] = LaunchpadColor.RED_BRIGHT;
        mushroom[3, 3] = LaunchpadColor.RED_BRIGHT;
        mushroom[4, 3] = LaunchpadColor.RED_BRIGHT;
        mushroom[5, 3] = LaunchpadColor.WHITE_BRIGHT;
        mushroom[6, 3] = LaunchpadColor.RED_BRIGHT;

        // Visage
        mushroom[2, 4] = LaunchpadColor.ORANGE;
        mushroom[3, 4] = LaunchpadColor.ORANGE;
        mushroom[4, 4] = LaunchpadColor.ORANGE;
        mushroom[5, 4] = LaunchpadColor.ORANGE;

        // Yeux
        mushroom[3, 3] = LaunchpadColor.OFF;  // Noir
        mushroom[4, 3] = LaunchpadColor.OFF;  // Noir

        // Tige
        mushroom[3, 5] = LaunchpadColor.ORANGE;
        mushroom[4, 5] = LaunchpadColor.ORANGE;

        mushroom[3, 6] = LaunchpadColor.ORANGE;
        mushroom[4, 6] = LaunchpadColor.ORANGE;

        return mushroom;
    }

    private LaunchpadColor[,] CreateSpaceInvaderImage()
    {
        LaunchpadColor[,] invader = new LaunchpadColor[8, 8];

        // Initialiser avec OFF
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                invader[x, y] = LaunchpadColor.OFF;
            }
        }

        // Dessiner un Space Invader classique
        invader[1, 1] = LaunchpadColor.GREEN_BRIGHT;
        invader[6, 1] = LaunchpadColor.GREEN_BRIGHT;

        invader[2, 2] = LaunchpadColor.GREEN_BRIGHT;
        invader[5, 2] = LaunchpadColor.GREEN_BRIGHT;

        invader[1, 3] = LaunchpadColor.GREEN_BRIGHT;
        invader[2, 3] = LaunchpadColor.GREEN_BRIGHT;
        invader[3, 3] = LaunchpadColor.GREEN_BRIGHT;
        invader[4, 3] = LaunchpadColor.GREEN_BRIGHT;
        invader[5, 3] = LaunchpadColor.GREEN_BRIGHT;
        invader[6, 3] = LaunchpadColor.GREEN_BRIGHT;

        invader[0, 4] = LaunchpadColor.GREEN_BRIGHT;
        invader[1, 4] = LaunchpadColor.GREEN_BRIGHT;
        invader[3, 4] = LaunchpadColor.GREEN_BRIGHT;
        invader[4, 4] = LaunchpadColor.GREEN_BRIGHT;
        invader[6, 4] = LaunchpadColor.GREEN_BRIGHT;
        invader[7, 4] = LaunchpadColor.GREEN_BRIGHT;

        invader[0, 5] = LaunchpadColor.GREEN_BRIGHT;
        invader[1, 5] = LaunchpadColor.GREEN_BRIGHT;
        invader[2, 5] = LaunchpadColor.GREEN_BRIGHT;
        invader[3, 5] = LaunchpadColor.GREEN_BRIGHT;
        invader[4, 5] = LaunchpadColor.GREEN_BRIGHT;
        invader[5, 5] = LaunchpadColor.GREEN_BRIGHT;
        invader[6, 5] = LaunchpadColor.GREEN_BRIGHT;
        invader[7, 5] = LaunchpadColor.GREEN_BRIGHT;

        invader[2, 6] = LaunchpadColor.GREEN_BRIGHT;
        invader[5, 6] = LaunchpadColor.GREEN_BRIGHT;

        invader[1, 7] = LaunchpadColor.GREEN_BRIGHT;
        invader[6, 7] = LaunchpadColor.GREEN_BRIGHT;

        return invader;
    }

    // Méthode pour créer un motif de test de couleurs
    public void CreateColorTestPattern()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Effacer la grille
        ClearGrid();

        // Créer un motif pour tester différentes couleurs
        // Ligne 1: Couleurs de base
        SetPixel(0, 0, LaunchpadColor.WHITE_BRIGHT);
        SetPixel(1, 0, LaunchpadColor.RED_BRIGHT);
        SetPixel(2, 0, LaunchpadColor.GREEN_BRIGHT);
        SetPixel(3, 0, LaunchpadColor.BLUE);
        SetPixel(4, 0, LaunchpadColor.YELLOW_BRIGHT);
        SetPixel(5, 0, LaunchpadColor.ORANGE);
        SetPixel(6, 0, LaunchpadColor.PURPLE);
        SetPixel(7, 0, LaunchpadColor.PINK);

        // Ligne 2: Variations de rouge
        SetPixel(0, 1, LaunchpadColor.RED_LIGHT);
        SetPixel(1, 1, LaunchpadColor.RED_BRIGHT);
        SetPixel(2, 1, LaunchpadColor.RED_DARK);
        SetPixel(3, 1, LaunchpadColor.ORANGE);
        SetPixel(4, 1, LaunchpadColor.ORANGE_DARK);
        SetPixel(5, 1, LaunchpadColor.PINK_LIGHT);
        SetPixel(6, 1, LaunchpadColor.PINK);
        SetPixel(7, 1, LaunchpadColor.PINK_BRIGHT);

        // Ligne 3: Variations de vert
        SetPixel(0, 2, LaunchpadColor.GREEN_LIGHT);
        SetPixel(1, 2, LaunchpadColor.GREEN_BRIGHT);
        SetPixel(2, 2, LaunchpadColor.GREEN_DARK);
        SetPixel(3, 2, LaunchpadColor.LIME_LIGHT);
        SetPixel(4, 2, LaunchpadColor.LIME_BRIGHT);
        SetPixel(5, 2, LaunchpadColor.LIME_DARK);
        SetPixel(6, 2, LaunchpadColor.BRIGHT_GREEN);
        SetPixel(7, 2, LaunchpadColor.BRIGHT_GREEN_2);

        // Ligne 4: Variations de bleu
        SetPixel(0, 3, LaunchpadColor.BLUE_LIGHT);
        SetPixel(1, 3, LaunchpadColor.BLUE);
        SetPixel(2, 3, LaunchpadColor.BLUE_DARK);
        SetPixel(3, 3, LaunchpadColor.CYAN_LIGHT);
        SetPixel(4, 3, LaunchpadColor.CYAN);
        SetPixel(5, 3, LaunchpadColor.PURPLE_LIGHT);
        SetPixel(6, 3, LaunchpadColor.PURPLE);
        SetPixel(7, 3, LaunchpadColor.PURPLE_DARK);

        // Ligne 5-8: Intensités variables (gris, rouge, vert, bleu)
        for (int x = 0; x < 8; x++)
        {
            int intensity = x * 3;
            if (intensity > 0)
            {
                // Ligne 5: Blancs/Gris d'intensité croissante
                if (x < 3)
                {
                    SetPixel(x, 4, x == 0 ? LaunchpadColor.WHITE_DIM : (x == 1 ? LaunchpadColor.WHITE_MID : LaunchpadColor.WHITE_BRIGHT));
                }

                // Ligne 6: Rouges d'intensité croissante
                if (x < 3)
                {
                    SetPixel(x, 5, x == 0 ? LaunchpadColor.RED_LIGHT : (x == 1 ? LaunchpadColor.RED_BRIGHT : LaunchpadColor.RED_DARK));
                }

                // Ligne 7: Verts d'intensité croissante
                if (x < 3)
                {
                    SetPixel(x, 6, x == 0 ? LaunchpadColor.GREEN_LIGHT : (x == 1 ? LaunchpadColor.GREEN_BRIGHT : LaunchpadColor.GREEN_DARK));
                }

                // Ligne 8: Bleus d'intensité croissante
                if (x < 3)
                {
                    SetPixel(x, 7, x == 0 ? LaunchpadColor.BLUE_LIGHT : (x == 1 ? LaunchpadColor.BLUE : LaunchpadColor.BLUE_DARK));
                }
            }
        }
    }

    // Animation de "tetris" qui fait tomber des pixels du haut
    public IEnumerator AnimateTetris()
    {
        if (!deviceConnected || _launchpadOut == null) yield break;

        // Effacer la grille
        ClearGrid();

        // Créer une grille pour suivre les pixels "fixés"
        LaunchpadColor[,] grid = new LaunchpadColor[8, 8];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                grid[x, y] = LaunchpadColor.OFF;
            }
        }

        // Tableau des couleurs disponibles pour les blocs
        LaunchpadColor[] blockColors = new LaunchpadColor[] {
            LaunchpadColor.RED_BRIGHT,
            LaunchpadColor.BLUE,
            LaunchpadColor.GREEN_BRIGHT,
            LaunchpadColor.YELLOW_BRIGHT,
            LaunchpadColor.PURPLE,
            LaunchpadColor.CYAN,
            LaunchpadColor.ORANGE
        };

        // Continuer jusqu'à ce que la grille soit pleine
        bool gridFull = false;

        while (!gridFull && animateImage)
        {
            // Créer un nouveau bloc qui tombe
            int x = Random.Range(0, 8);
            LaunchpadColor color = blockColors[Random.Range(0, blockColors.Length)];

            // Faire tomber le bloc
            for (int y = 0; y < 8; y++)
            {
                // Effacer la position précédente (sauf première ligne)
                if (y > 0)
                {
                    SetPixel(x, y - 1, grid[x, y - 1]);
                }

                // Dessiner le bloc à sa nouvelle position
                SetPixel(x, y, color);

                yield return new WaitForSeconds(0.1f);

                // Vérifier si on atteint le bas ou un autre bloc
                if (y == 7 || grid[x, y + 1] != LaunchpadColor.OFF)
                {
                    // Fixer le bloc à sa position actuelle
                    grid[x, y] = color;
                    break;
                }
            }

            // Vérifier si une ligne est complète et la faire clignoter
            for (int y = 7; y >= 0; y--)
            {
                bool lineComplete = true;
                for (int lx = 0; lx < 8; lx++)
                {
                    if (grid[lx, y] == LaunchpadColor.OFF)
                    {
                        lineComplete = false;
                        break;
                    }
                }

                if (lineComplete)
                {
                    // Faire clignoter la ligne
                    for (int blink = 0; blink < 3; blink++)
                    {
                        for (int lx = 0; lx < 8; lx++)
                        {
                            SetPixel(lx, y, LaunchpadColor.WHITE_BRIGHT);
                        }
                        yield return new WaitForSeconds(0.1f);

                        for (int lx = 0; lx < 8; lx++)
                        {
                            SetPixel(lx, y, LaunchpadColor.OFF);
                        }
                        yield return new WaitForSeconds(0.1f);
                    }

                    // Faire descendre tous les blocs au-dessus
                    for (int cy = y; cy > 0; cy--)
                    {
                        for (int lx = 0; lx < 8; lx++)
                        {
                            grid[lx, cy] = grid[lx, cy - 1];
                            SetPixel(lx, cy, grid[lx, cy]);
                        }
                    }

                    // Effacer la ligne du haut
                    for (int lx = 0; lx < 8; lx++)
                    {
                        grid[lx, 0] = LaunchpadColor.OFF;
                    }
                }
            }

            // Vérifier si la grille est pleine
            gridFull = true;
            for (int lx = 0; lx < 8; lx++)
            {
                if (grid[lx, 0] == LaunchpadColor.OFF)
                {
                    gridFull = false;
                    break;
                }
            }
        }

        // Animation de fin quand la grille est pleine
        if (gridFull)
        {
            for (int i = 0; i < 3; i++)
            {
                // Remplir d'une couleur vive
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        SetPixel(x, y, LaunchpadColor.WHITE_BRIGHT);
                    }
                }
                yield return new WaitForSeconds(0.2f);

                // Éteindre
                ClearGrid();
                yield return new WaitForSeconds(0.2f);
            }
        }

        // Réinitialiser l'affichage
        ClearGrid();
    }
}