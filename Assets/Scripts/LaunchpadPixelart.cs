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
    [System.Serializable]
    public enum LaunchpadColor
    {
        OFF = 0,
        RED_LOW = 1,
        RED_MID = 2,
        RED_FULL = 3,
        ORANGE_LOW = 18,
        ORANGE_FULL = 19,
        YELLOW_LOW = 34,
        YELLOW_FULL = 35,
        LIME_LOW = 50,
        LIME_FULL = 51,
        GREEN_LOW = 16,
        GREEN_MID = 32,
        GREEN_FULL = 48,
        MINT_LOW = 33,
        MINT_FULL = 49,
        CYAN_LOW = 65,
        CYAN_FULL = 66,
        SKY_LOW = 67,
        SKY_FULL = 83,
        BLUE_LOW = 81,
        BLUE_MID = 82,
        BLUE_FULL = 84,
        PURPLE_LOW = 85,
        PURPLE_FULL = 86,
        MAGENTA_LOW = 87,
        MAGENTA_FULL = 88,
        PINK_LOW = 89,
        PINK_FULL = 90,
        WHITE_LOW = 113,
        WHITE_FULL = 127
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
                int noteNumber = 11 + x + (y * 10);
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
        if (!deviceConnected || _launchpadOut == null) return;
        
        Vector2Int pos = new Vector2Int(x, y);
        if (padNotes.ContainsKey(pos))
        {
            _launchpadOut.SendNoteOn(0, padNotes[pos], (int)color);
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
            /*case PixelArtImage.Ghost:
                return CreateGhostImage();
            case PixelArtImage.Mario:
                return CreateMarioImage();
            case PixelArtImage.Mushroom:
                return CreateMushroomImage();
            case PixelArtImage.Space_Invader:
                return CreateSpaceInvaderImage();*/
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
                /*case PixelArtImage.Ghost:
                    yield return AnimateGhost();
                    break;*/
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
    /*private IEnumerator AnimateGhost()
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
    }*/
    
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
        heart[1, 1] = LaunchpadColor.RED_FULL;
        heart[2, 1] = LaunchpadColor.RED_FULL;
        heart[4, 1] = LaunchpadColor.RED_FULL;
        heart[5, 1] = LaunchpadColor.RED_FULL;
        
        heart[0, 2] = LaunchpadColor.RED_FULL;
        heart[1, 2] = LaunchpadColor.RED_FULL;
        heart[2, 2] = LaunchpadColor.RED_FULL;
        heart[3, 2] = LaunchpadColor.RED_FULL;
        heart[4, 2] = LaunchpadColor.RED_FULL;
        heart[5, 2] = LaunchpadColor.RED_FULL;
        heart[6, 2] = LaunchpadColor.RED_FULL;
        
        heart[0, 3] = LaunchpadColor.RED_FULL;
        heart[1, 3] = LaunchpadColor.RED_FULL;
        heart[2, 3] = LaunchpadColor.RED_FULL;
        heart[3, 3] = LaunchpadColor.RED_FULL;
        heart[4, 3] = LaunchpadColor.RED_FULL;
        heart[5, 3] = LaunchpadColor.RED_FULL;
        heart[6, 3] = LaunchpadColor.RED_FULL;
        
        heart[1, 4] = LaunchpadColor.RED_FULL;
        heart[2, 4] = LaunchpadColor.RED_FULL;
        heart[3, 4] = LaunchpadColor.RED_FULL;
        heart[4, 4] = LaunchpadColor.RED_FULL;
        heart[5, 4] = LaunchpadColor.RED_FULL;
        
        heart[2, 5] = LaunchpadColor.RED_FULL;
        heart[3, 5] = LaunchpadColor.RED_FULL;
        heart[4, 5] = LaunchpadColor.RED_FULL;
        
        heart[3, 6] = LaunchpadColor.RED_FULL;
        
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
        heart[2, 2] = LaunchpadColor.RED_FULL;
        heart[4, 2] = LaunchpadColor.RED_FULL;
        
        heart[1, 3] = LaunchpadColor.RED_FULL;
        heart[2, 3] = LaunchpadColor.RED_FULL;
        heart[3, 3] = LaunchpadColor.RED_FULL;
        heart[4, 3] = LaunchpadColor.RED_FULL;
        heart[5, 3] = LaunchpadColor.RED_FULL;
        
        heart[2, 4] = LaunchpadColor.RED_FULL;
        heart[3, 4] = LaunchpadColor.RED_FULL;
        heart[4, 4] = LaunchpadColor.RED_FULL;
        
        heart[3, 5] = LaunchpadColor.RED_FULL;
        
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
        smile[2, 1] = LaunchpadColor.YELLOW_FULL;
        smile[3, 1] = LaunchpadColor.YELLOW_FULL;
        smile[4, 1] = LaunchpadColor.YELLOW_FULL;
        smile[5, 1] = LaunchpadColor.YELLOW_FULL;
        
        smile[1, 2] = LaunchpadColor.YELLOW_FULL;
        smile[2, 2] = LaunchpadColor.YELLOW_FULL;
        smile[3, 2] = LaunchpadColor.YELLOW_FULL;
        smile[4, 2] = LaunchpadColor.YELLOW_FULL;
        smile[5, 2] = LaunchpadColor.YELLOW_FULL;
        smile[6, 2] = LaunchpadColor.YELLOW_FULL;
        
        smile[1, 3] = LaunchpadColor.YELLOW_FULL;
        smile[2, 3] = LaunchpadColor.YELLOW_FULL;
        smile[3, 3] = LaunchpadColor.YELLOW_FULL;
        smile[4, 3] = LaunchpadColor.YELLOW_FULL;
        smile[5, 3] = LaunchpadColor.YELLOW_FULL;
        smile[6, 3] = LaunchpadColor.YELLOW_FULL;
        
        // Yeux
        smile[2, 4] = LaunchpadColor.BLUE_FULL;
        smile[5, 4] = LaunchpadColor.BLUE_FULL;
        
        // Bouche
        smile[1, 5] = LaunchpadColor.RED_FULL;
        smile[2, 6] = LaunchpadColor.RED_FULL;
        smile[3, 6] = LaunchpadColor.RED_FULL;
        smile[4, 6] = LaunchpadColor.RED_FULL;
        smile[5, 6] = LaunchpadColor.RED_FULL;
        smile[6, 5] = LaunchpadColor.RED_FULL;
        
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
        alien[3, 0] = LaunchpadColor.GREEN_FULL;
        alien[4, 0] = LaunchpadColor.GREEN_FULL;
        
        alien[2, 1] = LaunchpadColor.GREEN_FULL;
        alien[3, 1] = LaunchpadColor.GREEN_FULL;
        alien[4, 1] = LaunchpadColor.GREEN_FULL;
        alien[5, 1] = LaunchpadColor.GREEN_FULL;
        
        alien[1, 2] = LaunchpadColor.GREEN_FULL;
        alien[2, 2] = LaunchpadColor.GREEN_FULL;
        alien[3, 2] = LaunchpadColor.GREEN_FULL;
        alien[4, 2] = LaunchpadColor.GREEN_FULL;
        alien[5, 2] = LaunchpadColor.GREEN_FULL;
        alien[6, 2] = LaunchpadColor.GREEN_FULL;
        
        // Yeux
        alien[2, 3] = LaunchpadColor.WHITE_FULL;
        alien[3, 3] = LaunchpadColor.GREEN_FULL;
        alien[4, 3] = LaunchpadColor.GREEN_FULL;
        alien[5, 3] = LaunchpadColor.WHITE_FULL;
        
        alien[2, 4] = LaunchpadColor.GREEN_FULL;
        alien[3, 4] = LaunchpadColor.GREEN_FULL;
        alien[4, 4] = LaunchpadColor.GREEN_FULL;
        alien[5, 4] = LaunchpadColor.GREEN_FULL;
        
        // Antennes
        alien[1, 5] = LaunchpadColor.GREEN_FULL;
        alien[3, 5] = LaunchpadColor.GREEN_FULL;
        alien[4, 5] = LaunchpadColor.GREEN_FULL;
        alien[6, 5] = LaunchpadColor.GREEN_FULL;
        
        alien[0, 6] = LaunchpadColor.GREEN_FULL;
        alien[2, 6] = LaunchpadColor.GREEN_FULL;
        alien[5, 6] = LaunchpadColor.GREEN_FULL;
        alien[7, 6] = LaunchpadColor.GREEN_FULL;
        
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
        pacman[2, 1] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 1] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 1] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[5, 2] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 3] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 3] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 3] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 3] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 4] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 4] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 4] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 5] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 5] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 5] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 5] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[5, 6] = LaunchpadColor.YELLOW_FULL;
        
        pacman[2, 7] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 7] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 7] = LaunchpadColor.YELLOW_FULL;
        
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
        pacman[2, 1] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 1] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 1] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 2] = LaunchpadColor.YELLOW_FULL;
        pacman[5, 2] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 3] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 3] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 3] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 3] = LaunchpadColor.YELLOW_FULL;
        pacman[5, 3] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 4] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 4] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 4] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 4] = LaunchpadColor.YELLOW_FULL;
        pacman[5, 4] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 5] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 5] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 5] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 5] = LaunchpadColor.YELLOW_FULL;
        pacman[5, 5] = LaunchpadColor.YELLOW_FULL;
        
        pacman[1, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[2, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 6] = LaunchpadColor.YELLOW_FULL;
        pacman[5, 6] = LaunchpadColor.YELLOW_FULL;
        
        pacman[2, 7] = LaunchpadColor.YELLOW_FULL;
        pacman[3, 7] = LaunchpadColor.YELLOW_FULL;
        pacman[4, 7] = LaunchpadColor.YELLOW_FULL;
        
        return pacman;
    }

    /*private LaunchpadColor[,] CreateGhostImage()
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
        ghost[2, 1] = LaunchpadColor.SKY_FULL;
        ghost[3, 1] = LaunchpadColor.SKY_FULL;
        ghost[4, 1] = LaunchpadColor.SKY_FULL;
        
        ghost[1, 2] = LaunchpadColor.SKY_FULL;
        ghost[2, 2] = LaunchpadColor.SKY_FULL;
        ghost[3, 2] = LaunchpadColor.SKY_FULL;
        ghost[4, 2] = LaunchpadColor.SKY_FULL;
        ghost[5, 2] = LaunchpadColor.SKY_FULL;
        
        ghost[1, 3] = LaunchpadColor.SKY_FULL;
        ghost[2, 3] = LaunchpadColor.WHITE_FULL;
        ghost[3, 3] = LaunchpadColor.SKY_FULL;
        ghost[4, 3] = LaunchpadColor.WHITE_FULL;
        ghost[5, 3] = LaunchpadColor.SKY_FULL;
        
        ghost[1, 4] = LaunchpadColor.SKY_FULL;
        ghost[2, 4] = LaunchpadColor.BLUE_FULL;
        ghost[3, 4] = LaunchpadColor.SKY_FULL;
        ghost[4, 4] = LaunchpadColor.BLUE_FULL;
        ghost[5, 4] = LaunchpadColor.SKY_FULL;
        
        ghost[1, 5] = LaunchpadColor.SKY_FULL;
        ghost[2, 5] = LaunchpadColor.SKY_FULL;
        ghost[3, 5] = LaunchpadColor.SKY_FULL;
        ghost[4, 5] = LaunchpadColor.SKY_FULL;
        ghost[5, 5] = LaunchpadColor.SKY_FULL;
        
        ghost[1, 6] = LaunchpadColor.SKY_FULL;
        ghost[2, 6] = LaunchpadColor.SKY_FULL;
        ghost[3, 6] = LaunchpadColor.SKY_FULL;
        ghost[4, 6] = LaunchpadColor.SKY_FULL;
        ghost[5, 6] = LaunchpadColor.SKY_FULL;
        
        ghost[1, 7] = LaunchpadColor.SKY_FULL;
        ghost[3, 7] = LaunchpadColor.SKY_FULL;
        ghost[5, 7] = LaunchpadColor.SKY_FULL;
    */
}