using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RtMidi.LowLevel;

public class LaunchpadSimon : MonoBehaviour
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
    private Dictionary<int, Vector2> padPositions = new Dictionary<int, Vector2>();
    private Dictionary<Vector2, int> positionToNote = new Dictionary<Vector2, int>();

    // Couleurs Launchpad standard
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
        PURPLE = 49,
        PINK = 53,
        ORANGE = 18
    }

    // Configuration du jeu
    [Header("Configuration du jeu")]
    [SerializeField] private int nbTouchesJeu = 4; // Nombre de touches utilisées pour le jeu
    [SerializeField] private int tempsAffichage = 500; // Temps d'affichage d'une touche en ms
    [SerializeField] private int tempsEntreTouches = 300; // Temps entre l'affichage de deux touches en ms

    // Couleurs attribuées au jeu
    private LaunchpadColor[] couleursJeu = new LaunchpadColor[] {
        LaunchpadColor.RED_FULL,
        LaunchpadColor.YELLOW,
        LaunchpadColor.GREEN_FULL,
        LaunchpadColor.BLUE_FULL,
        LaunchpadColor.PURPLE,
        LaunchpadColor.ORANGE,
        LaunchpadColor.PINK,
        LaunchpadColor.AMBER_FULL
    };

    // Variables du jeu
    private List<int> touchesJeu = new List<int>(); // Les touches utilisées pour le jeu
    private Dictionary<int, LaunchpadColor> couleursTouches = new Dictionary<int, LaunchpadColor>(); // Mapping note -> couleur
    private List<int> sequenceActuelle = new List<int>(); // Séquence actuelle à reproduire
    private int positionReproduction = 0; // Position dans la reproduction de la séquence
    private bool enAttenteSaisie = false; // Si le joueur est en train de saisir la séquence
    private bool jeuActif = false; // Si une partie est en cours
    private int niveau = 0; // Niveau actuel (= longueur de la séquence)

    void Start()
    {
        // Initialiser le mapping des touches
        InitializePadPositions();

        // Initialiser la connexion MIDI
        InitializeMIDI();
    }

    void Update()
    {
        // Vérifier si les ports ont changé
        if (_inProbe != null && _inPorts.Count != _inProbe.PortCount ||
            _outProbe != null && _outPorts.Count != _outProbe.PortCount)
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
        CleanupMIDI();
    }

    // Initialiser le mapping des touches du Launchpad
    void InitializePadPositions()
    {
        // Le Launchpad MK2 a une grille 8x8
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                // Calculer la note MIDI (format standard du Launchpad MK2)
                int noteNumber = 11 + x + (y * 10);
                padPositions.Add(noteNumber, new Vector2(x, 7 - y));
                positionToNote.Add(new Vector2(x, 7 - y), noteNumber);
            }
        }
    }

    // Initialiser les connexions MIDI
    void InitializeMIDI()
    {
        _inProbe = new MidiProbe(MidiProbe.Mode.In);
        _outProbe = new MidiProbe(MidiProbe.Mode.Out);
        ScanPorts();
    }

    // Scanner les ports MIDI disponibles
    void ScanPorts()
    {
        // Recherche des ports d'entrée
        for (int i = 0; i < _inProbe.PortCount; i++)
        {
            string portName = _inProbe.GetPortName(i);
            Debug.Log("MIDI-in port trouvé: " + portName);

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
            Debug.Log("MIDI-out port trouvé: " + portName);

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

        // Si le launchpad est connecté, initialiser le jeu
        if (deviceConnected && _launchpadOut != null && _launchpadIn != null)
        {
            ResetAllLEDs();
            // Attendre un instant avant d'afficher le menu
            Invoke("AfficherMenuPrincipal", 0.5f);
        }
    }

    // Libérer les ressources MIDI
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

    // Nettoyer les ressources MIDI
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

    // Gestionnaires d'événements MIDI
    void HandleNoteOn(byte channel, byte note, byte velocity)
    {
        if (velocity == 0) return; // Ignorer les NoteOn avec vélocité 0 (équivalent à NoteOff)

        // Si le jeu est inactif, vérifier si c'est le bouton START
        if (!jeuActif)
        {
            if (padPositions.ContainsKey(note))
            {
                Vector2 pos = padPositions[note];
                if (pos.x == 6 && pos.y == 1) // Bouton START (pos 6,1)
                {
                    DemarrerPartie();
                }
            }
            return;
        }

        // Si en attente de saisie
        if (enAttenteSaisie)
        {
            // Vérifier si c'est une des touches du jeu
            if (touchesJeu.Contains(note))
            {
                // Allumer brièvement la touche avec sa couleur
                SendColorToLaunchpad(note, (byte)couleursTouches[note]);

                // Vérifier si c'est la bonne touche dans la séquence
                if (note == sequenceActuelle[positionReproduction])
                {
                    positionReproduction++;

                    // Si toute la séquence a été reproduite correctement
                    if (positionReproduction >= sequenceActuelle.Count)
                    {
                        enAttenteSaisie = false;
                        StartCoroutine(PasserAuNiveauSuivant());
                    }
                }
                else
                {
                    // Erreur: la touche ne correspond pas à la séquence
                    StartCoroutine(AfficherErreur());
                }
            }
        }
    }

    void HandleNoteOff(byte channel, byte note)
    {
        // Si le jeu est actif et c'est une touche du jeu, l'éteindre
        if (jeuActif && touchesJeu.Contains(note))
        {
            SendColorToLaunchpad(note, (byte)LaunchpadColor.OFF);
        }
    }

    void HandleControlChange(byte channel, byte number, byte value)
    {
        // Non utilisé pour le jeu Simon
    }

    // Envoyer une couleur à une touche du Launchpad
    void SendColorToLaunchpad(int note, byte colorValue)
    {
        if (!deviceConnected || _launchpadOut == null) return;

        try
        {
            _launchpadOut.SendNoteOn(0, (byte)note, colorValue);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Erreur lors de l'envoi du message MIDI: {e.Message}");
        }
    }

    // Éteindre toutes les LED
    void ResetAllLEDs()
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Éteindre la grille principale 8x8
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                if (positionToNote.TryGetValue(new Vector2(x, y), out int note))
                {
                    SendColorToLaunchpad(note, (byte)LaunchpadColor.OFF);
                }
            }
        }

        // Éteindre les boutons de contrôle (round buttons)
        for (int i = 104; i <= 111; i++)
        {
            try
            {
                _launchpadOut.SendControlChange(0, (byte)i, 0);
            }
            catch (System.Exception) { }
        }

        // Éteindre les boutons de droite
        int[] rightButtons = { 19, 29, 39, 49, 59, 69, 79, 89 };
        foreach (int btn in rightButtons)
        {
            try
            {
                _launchpadOut.SendNoteOn(0, (byte)btn, 0);
            }
            catch (System.Exception) { }
        }
    }

    // Afficher le menu principal
    void AfficherMenuPrincipal()
    {
        // D'abord, éteindre toutes les LED
        ResetAllLEDs();

        // Attendre un moment pour s'assurer que tout est éteint
        StartCoroutine(DelayedMenuDisplay());
    }

    // Afficher le menu avec un délai
    IEnumerator DelayedMenuDisplay()
    {
        yield return new WaitForSeconds(0.1f);

        // Afficher le titre "SIMON" au centre
        SetPixel(2, 3, (byte)LaunchpadColor.RED_FULL);     // S (rouge)
        yield return new WaitForSeconds(0.05f);

        SetPixel(3, 3, (byte)LaunchpadColor.YELLOW);       // I (jaune)
        yield return new WaitForSeconds(0.05f);

        SetPixel(4, 3, (byte)LaunchpadColor.BLUE_FULL);    // M (bleu)
        yield return new WaitForSeconds(0.05f);

        SetPixel(5, 3, (byte)LaunchpadColor.GREEN_FULL);   // O (vert)
        yield return new WaitForSeconds(0.05f);

        SetPixel(3, 4, (byte)LaunchpadColor.PURPLE);       // N (violet)
        yield return new WaitForSeconds(0.2f);

        // Bouton pour démarrer la partie (coin bas droit) 
        SetPixel(6, 1, (byte)LaunchpadColor.GREEN_FULL);   // START
    }

    // Afficher un pixel avec une couleur spécifique par sa position x,y
    void SetPixel(int x, int y, byte color)
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Convertir la position x,y en numéro de note
        if (positionToNote.TryGetValue(new Vector2(x, y), out int note))
        {
            SendColorToLaunchpad(note, color);
        }
    }

    // Démarrer une nouvelle partie
    void DemarrerPartie()
    {
        if (!deviceConnected) return;

        jeuActif = true;
        niveau = 0;

        // Nettoyer l'affichage
        ResetAllLEDs();

        // Choisir les touches qui seront utilisées pour le jeu
        ChoisirTouchesJeu();
    }

    // Choisir aléatoirement les touches qui seront utilisées pour le jeu
    void ChoisirTouchesJeu()
    {
        touchesJeu.Clear();
        couleursTouches.Clear();

        // S'assurer que la grille est vide
        ResetAllLEDs();

        // Préparer une liste des notes disponibles (touches centrales)
        List<int> notesDisponibles = new List<int>();
        for (int y = 3; y < 7; y++)
        {
            for (int x = 2; x < 6; x++)
            {
                if (positionToNote.TryGetValue(new Vector2(x, y), out int note))
                {
                    notesDisponibles.Add(note);
                }
            }
        }

        // Mélanger la liste
        ShuffleList(notesDisponibles);

        // Prendre les n premières touches
        for (int i = 0; i < Mathf.Min(nbTouchesJeu, notesDisponibles.Count); i++)
        {
            touchesJeu.Add(notesDisponibles[i]);
        }

        // Mélanger les couleurs
        List<LaunchpadColor> couleurs = new List<LaunchpadColor>(couleursJeu);
        ShuffleList(couleurs);

        // Attribuer une couleur aléatoire à chaque touche
        for (int i = 0; i < touchesJeu.Count; i++)
        {
            couleursTouches[touchesJeu[i]] = couleurs[i % couleurs.Count];
        }

        // Montrer les touches une par une
        StartCoroutine(MontrerTouchesJeuUneParUne());
    }

    // Montrer les touches du jeu une par une
    IEnumerator MontrerTouchesJeuUneParUne()
    {
        // Attendre un moment avant de commencer
        yield return new WaitForSeconds(0.5f);

        // Montrer chaque touche individuellement
        foreach (int note in touchesJeu)
        {
            // Éteindre toutes les touches
            ResetAllLEDs();

            // Allumer la touche actuelle
            SendColorToLaunchpad(note, (byte)couleursTouches[note]);

            // Attendre pour que le joueur puisse voir la couleur
            yield return new WaitForSeconds(0.8f);
        }

        // Montrer toutes les touches ensemble un court instant
        ResetAllLEDs();
        foreach (int note in touchesJeu)
        {
            SendColorToLaunchpad(note, (byte)couleursTouches[note]);
        }

        yield return new WaitForSeconds(1.5f);
        ResetAllLEDs();

        // Passer au niveau suivant
        StartCoroutine(PasserAuNiveauSuivant());
    }

    // Passer au niveau suivant
    IEnumerator PasserAuNiveauSuivant()
    {
        niveau++;

        // Attendre un court instant
        yield return new WaitForSeconds(1.0f);

        // Ajouter une nouvelle touche aléatoire à la séquence
        if (niveau == 1)
        {
            // Première séquence: initialiser avec une touche aléatoire
            sequenceActuelle.Clear();
            sequenceActuelle.Add(touchesJeu[Random.Range(0, touchesJeu.Count)]);
        }
        else
        {
            // Ajouter une touche aléatoire à la séquence existante
            sequenceActuelle.Add(touchesJeu[Random.Range(0, touchesJeu.Count)]);
        }

        // Afficher le niveau actuel
        AfficherNiveau();

        yield return new WaitForSeconds(1.0f);

        // Afficher la séquence
        yield return StartCoroutine(AfficherSequence());

        // Attendre la reproduction par le joueur
        positionReproduction = 0;
        enAttenteSaisie = true;
    }

    // Afficher le niveau actuel
    void AfficherNiveau()
    {
        ResetAllLEDs();

        // Afficher le chiffre du niveau au centre
        if (niveau < 10)
        {
            // Pour les niveaux 1-9, afficher un chiffre simple
            AfficherChiffre(niveau, 3, 3, (byte)LaunchpadColor.YELLOW);
        }
        else
        {
            // Pour les niveaux 10+, afficher juste un symbole
            SetPixel(3, 3, (byte)LaunchpadColor.YELLOW);
            SetPixel(4, 3, (byte)LaunchpadColor.YELLOW);
            SetPixel(3, 4, (byte)LaunchpadColor.YELLOW);
            SetPixel(4, 4, (byte)LaunchpadColor.YELLOW);
        }
    }

    // Afficher un chiffre (1-9) à une position donnée
    void AfficherChiffre(int chiffre, int x, int y, byte couleur)
    {
        switch (chiffre)
        {
            case 1:
                SetPixel(x, y, couleur);
                SetPixel(x, y + 1, couleur);
                break;
            case 2:
                SetPixel(x, y, couleur);
                SetPixel(x + 1, y, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x, y + 2, couleur);
                SetPixel(x + 1, y + 2, couleur);
                break;
            case 3:
                SetPixel(x, y, couleur);
                SetPixel(x + 1, y, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x, y + 2, couleur);
                SetPixel(x + 1, y + 2, couleur);
                break;
            case 4:
                SetPixel(x, y, couleur);
                SetPixel(x, y + 1, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x + 1, y + 2, couleur);
                break;
            case 5:
                SetPixel(x + 1, y, couleur);
                SetPixel(x, y + 1, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x + 1, y + 2, couleur);
                SetPixel(x, y + 2, couleur);
                break;
            case 6:
                SetPixel(x, y, couleur);
                SetPixel(x, y + 1, couleur);
                SetPixel(x, y + 2, couleur);
                SetPixel(x + 1, y, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x + 1, y + 2, couleur);
                break;
            case 7:
                SetPixel(x, y + 2, couleur);
                SetPixel(x + 1, y + 2, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x + 1, y, couleur);
                break;
            case 8:
                SetPixel(x, y, couleur);
                SetPixel(x, y + 1, couleur);
                SetPixel(x, y + 2, couleur);
                SetPixel(x + 1, y, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x + 1, y + 2, couleur);
                break;
            case 9:
                SetPixel(x, y + 1, couleur);
                SetPixel(x, y + 2, couleur);
                SetPixel(x + 1, y, couleur);
                SetPixel(x + 1, y + 1, couleur);
                SetPixel(x + 1, y + 2, couleur);
                break;
            default:
                // Pour le 0 ou autre
                SetPixel(x, y, couleur);
                SetPixel(x, y + 1, couleur);
                SetPixel(x, y + 2, couleur);
                SetPixel(x + 1, y, couleur);
                SetPixel(x + 1, y + 2, couleur);
                break;
        }
    }

    // Afficher la séquence à reproduire
    IEnumerator AfficherSequence()
    {
        ResetAllLEDs();

        // Attendre un court instant avant de commencer
        yield return new WaitForSeconds(0.5f);

        // Afficher chaque touche de la séquence
        foreach (int note in sequenceActuelle)
        {
            // Allumer la touche avec sa couleur
            SendColorToLaunchpad(note, (byte)couleursTouches[note]);

            // Attendre le temps d'affichage
            yield return new WaitForSeconds(tempsAffichage / 1000f);

            // Éteindre la touche
            SendColorToLaunchpad(note, (byte)LaunchpadColor.OFF);

            // Attendre entre les touches
            yield return new WaitForSeconds(tempsEntreTouches / 1000f);
        }
    }

    // Afficher une animation d'erreur
    IEnumerator AfficherErreur()
    {
        enAttenteSaisie = false;

        // Animation d'erreur: clignoter en rouge
        for (int i = 0; i < 3; i++)
        {
            // Remplir la grille de rouge
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, (byte)LaunchpadColor.RED_FULL);
                }
            }

            yield return new WaitForSeconds(0.2f);
            ResetAllLEDs();
            yield return new WaitForSeconds(0.2f);
        }

        // Afficher le score final (nombre de niveaux réussis)
        AfficherScoreFinal();

        yield return new WaitForSeconds(3.0f);

        // Retour au menu principal
        jeuActif = false;
        AfficherMenuPrincipal();
    }

    // Afficher le score final
    void AfficherScoreFinal()
    {
        ResetAllLEDs();

        // Afficher "SCORE" en haut
        SetPixel(1, 2, (byte)LaunchpadColor.YELLOW); // S
        SetPixel(2, 2, (byte)LaunchpadColor.YELLOW); // C
        SetPixel(3, 2, (byte)LaunchpadColor.YELLOW); // O
        SetPixel(4, 2, (byte)LaunchpadColor.YELLOW); // R
        SetPixel(5, 2, (byte)LaunchpadColor.YELLOW); // E

        // Afficher le niveau atteint au centre
        int score = niveau - 1; // Le niveau où on a échoué

        if (score < 10)
        {
            // Afficher un chiffre simple
            AfficherChiffre(score, 3, 4, (byte)LaunchpadColor.GREEN_FULL);
        }
        else
        {
            // Pour les scores 10+, afficher les dizaines et les unités
            int dizaines = score / 10;
            int unites = score % 10;

            AfficherChiffre(dizaines, 2, 4, (byte)LaunchpadColor.GREEN_FULL);
            AfficherChiffre(unites, 4, 4, (byte)LaunchpadColor.GREEN_FULL);
        }
    }

    // Mélanger une liste (Fisher-Yates shuffle)
    void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}