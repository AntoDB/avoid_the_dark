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

    // Configuration du jeu
    [Header("Configuration du jeu")]
    [SerializeField] private int nbTouchesJeu = 4; // Nombre de touches utilisées pour le jeu
    [SerializeField] private int tempsAffichage = 500; // Temps d'affichage d'une touche en ms
    [SerializeField] private int tempsEntreTouches = 300; // Temps entre l'affichage de deux touches en ms
    [SerializeField] public int nombreMaxEtapes = 6; // Nombre maximum d'étapes (niveaux) à atteindre
    
    // Variable pour activer/désactiver le jeu
    [Header("Activation du jeu")]
    [SerializeField] public bool jeuEnabled = true; // Permet d'activer/désactiver le jeu

    // Couleurs attribuées au jeu
    private int[] couleursJeu = new int[] {
        5,    // Rouge vif (rangée supérieure)
        21,   // Jaune vif 
        52,   // Bleu ciel
        21,   // Vert vif
        95,   // Violet/rose
        9,    // Orange
        73,   // Vert fluo
        46    // Bleu
    };

    // Variables du jeu
    private List<int> touchesJeu = new List<int>(); // Les touches utilisées pour le jeu
    private Dictionary<int, int> couleursTouches = new Dictionary<int, int>(); // Mapping note -> couleur
    private List<int> sequenceActuelle = new List<int>(); // Séquence actuelle à reproduire
    private int positionReproduction = 0; // Position dans la reproduction de la séquence
    private bool enAttenteSaisie = false; // Si le joueur est en train de saisir la séquence
    private bool jeuActif = false; // Si une partie est en cours
    private int niveau = 0; // Niveau actuel (= longueur de la séquence)
    private bool previousJeuEnabled = true; // Pour détecter les changements de la variable jeuEnabled

    // Mapping des touches pour les boutons latéraux
    private Dictionary<string, int> boutonsLateraux = new Dictionary<string, int>();

    // Ajout d'une référence au Box Collider
    [Header("Trigger Configuration")]
    [SerializeField] private BoxCollider triggerCollider;

    void Start()
    {
        // S'assurer que le jeu est désactivé au démarrage
        jeuEnabled = false;
        previousJeuEnabled = false;

        // Initialiser le mapping des boutons latéraux
        InitialiserBoutonsLateraux();

        // Initialiser le mapping des touches
        InitializePadPositions();

        // Initialiser la connexion MIDI
        InitializeMIDI();

        // S'assurer que tout est éteint au démarrage
        Invoke("ResetAllLEDs", 0.2f);
        Invoke("ResetAllLEDs", 0.5f); // Double reset pour être sûr
    }

    // Initialiser les boutons latéraux
    void InitialiserBoutonsLateraux()
    {
        // Pour le Launchpad MK2, les boutons latéraux utilisent des messages CC (Control Change)
        // Boutons du haut
        boutonsLateraux.Add("Up", 104);     // CC 104
        boutonsLateraux.Add("Down", 105);   // CC 105
        boutonsLateraux.Add("Left", 106);   // CC 106
        boutonsLateraux.Add("Right", 107);  // CC 107
        boutonsLateraux.Add("Session", 108); // CC 108
        boutonsLateraux.Add("User1", 109);  // CC 109
        boutonsLateraux.Add("User2", 110);  // CC 110
        boutonsLateraux.Add("Mixer", 111);  // CC 111
        
        // Boutons de droite - sur le Launchpad MK2 ils sont mappés comme des notes MIDI
        // avec un décalage de 8 par rangée
        boutonsLateraux.Add("Volume", 89);  // Note 89
        boutonsLateraux.Add("Pan", 79);     // Note 79
        boutonsLateraux.Add("SendA", 69);   // Note 69
        boutonsLateraux.Add("SendB", 59);   // Note 59
        boutonsLateraux.Add("Stop", 49);    // Note 49
        boutonsLateraux.Add("Mute", 39);    // Note 39
        boutonsLateraux.Add("Solo", 29);    // Note 29
        boutonsLateraux.Add("RecordArm", 19); // Note 19
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

        // Détecter les changements de la variable jeuEnabled
        if (previousJeuEnabled != jeuEnabled)
        {
            if (jeuEnabled)
            {
                // Le jeu vient d'être activé
                ArreterJeu();
                Invoke("AfficherMenuPrincipal", 0.5f);
            }
            else
            {
                // Le jeu vient d'être désactivé
                ArreterJeu();
            }
            previousJeuEnabled = jeuEnabled;
        }

        // Traiter les messages MIDI entrants seulement si le jeu est activé
        if (jeuEnabled)
        {
            foreach (var port in _inPorts)
            {
                port?.ProcessMessages();
            }
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

        // Si le launchpad est connecté et le jeu est activé, initialiser le jeu
        if (deviceConnected && _launchpadOut != null && _launchpadIn != null && jeuEnabled)
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
        // Si le jeu n'est pas activé, ignorer tous les événements
        if (!jeuEnabled) return;
        
        if (velocity == 0) return; // Ignorer les NoteOn avec vélocité 0 (équivalent à NoteOff)

        // Si le jeu est inactif, vérifier si c'est un des boutons START (boutons centraux)
        if (!jeuActif)
        {
            if (padPositions.ContainsKey(note))
            {
                Vector2 pos = padPositions[note];
                // Les 4 boutons centraux (3,3), (4,3), (3,4), (4,4)
                if ((pos.x == 3 || pos.x == 4) && (pos.y == 3 || pos.y == 4))
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
        // Si le jeu n'est pas activé, ignorer tous les événements
        if (!jeuEnabled) return;
        
        // Si le jeu est actif et c'est une touche du jeu, l'éteindre
        if (jeuActif && touchesJeu.Contains(note))
        {
            SendColorToLaunchpad(note, 0); // 0 = OFF
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

        // Nettoyage agressif pour s'assurer que toutes les LED sont éteintes

        // 1. Éteindre toutes les notes possibles (0-127)
        for (int note = 0; note < 128; note++)
        {
            try { _launchpadOut.SendNoteOn(0, (byte)note, 0); } catch { }
        }

        // 2. Éteindre tous les messages de contrôle possibles
        for (int cc = 0; cc < 128; cc++)
        {
            try { _launchpadOut.SendControlChange(0, (byte)cc, 0); } catch { }
        }

        // 3. Approche spécifique pour le Launchpad
        for (byte y = 0; y < 9; y++)
        {
            for (byte x = 0; x < 9; x++)
            {
                byte note = (byte)(16 * y + x);
                try { _launchpadOut.SendNoteOn(0, note, 0); } catch { }
            }
        }
    }

    // Afficher le menu principal
    void AfficherMenuPrincipal()
    {
        // Si le jeu n'est pas activé, ne rien faire
        if (!jeuEnabled) return;
        
        // Éteindre seulement la grille centrale, préserver l'état des indicateurs de niveau
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, 0); // Éteindre
            }
        }

        // Attendre un moment pour s'assurer que tout est éteint
        StartCoroutine(AfficherBoutonsStart());
    }
    
    // Réinitialiser les LED de la grille principale sans affecter les boutons de progression
    void ResetMainGridLEDs()
    {
        if (!deviceConnected || _launchpadOut == null) return;
        
        // Éteindre uniquement la grille centrale 8x8
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, 0); // Éteindre
            }
        }
    }

    // Afficher les boutons de démarrage
    IEnumerator AfficherBoutonsStart()
    {
        yield return new WaitForSeconds(0.3f);

        // Si pendant l'attente le jeu a été désactivé, ne pas afficher les boutons
        if (!jeuEnabled) yield break;
        
        // Allumer les 4 boutons centraux en vert comme boutons START
        SetPixel(3, 3, 21); // Vert vif
        SetPixel(4, 3, 21); // Vert vif
        SetPixel(3, 4, 21); // Vert vif
        SetPixel(4, 4, 21); // Vert vif
    }

    // Afficher un pixel avec une couleur spécifique par sa position x,y
    void SetPixel(int x, int y, int color)
    {
        if (!deviceConnected || _launchpadOut == null) return;

        // Convertir la position x,y en numéro de note
        if (positionToNote.TryGetValue(new Vector2(x, y), out int note))
        {
            SendColorToLaunchpad(note, (byte)color);
        }
    }

    // Méthode pour arrêter complètement le jeu et nettoyer l'affichage
    public void ArreterJeu()
    {
        // Réinitialiser les variables du jeu
        jeuActif = false;
        enAttenteSaisie = false;
        
        // Arrêter toutes les coroutines en cours
        StopAllCoroutines();
        
        // Effacer l'écran
        ResetAllLEDs();
    }

    // Démarrer une nouvelle partie
    void DemarrerPartie()
    {
        if (!deviceConnected || !jeuEnabled) return;

        jeuActif = true;
        niveau = 0;

        // Nettoyer seulement la grille principale
        ResetMainGridLEDs();
        
        // Initialiser l'affichage de la progression
        AfficherProgressionNiveaux();

        // Choisir les touches qui seront utilisées pour le jeu
        ChoisirTouchesJeu();
    }

    // Choisir aléatoirement les touches qui seront utilisées pour le jeu
    void ChoisirTouchesJeu()
    {
        touchesJeu.Clear();
        couleursTouches.Clear();

        // S'assurer que la grille centrale est vide, mais préserver les indicateurs de progression
        ResetMainGridLEDs();

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
        List<int> couleurs = new List<int>(couleursJeu);
        ShuffleList(couleurs);

        // Attribuer une couleur aléatoire à chaque touche
        for (int i = 0; i < touchesJeu.Count; i++)
        {
            couleursTouches[touchesJeu[i]] = couleurs[i % couleurs.Count];
        }

        // Commencer directement à jouer
        StartCoroutine(PasserAuNiveauSuivant());
    }

    // Passer au niveau suivant
    IEnumerator PasserAuNiveauSuivant()
    {
        // Vérifier si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }
        
        niveau++;

        // Vérifier si le joueur a atteint le nombre maximum d'étapes
        if (niveau > nombreMaxEtapes)
        {
            yield return StartCoroutine(AfficherVictoire());
            yield break;
        }
        
        // Mettre à jour l'affichage des niveaux
        AfficherProgressionNiveaux();

        // Attendre un court instant
        yield return new WaitForSeconds(0.5f);
        
        // Vérifier à nouveau si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }

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

        // Afficher directement la séquence sans montrer le niveau
        yield return StartCoroutine(AfficherSequence());
        
        // Vérifier une dernière fois si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }

        // Attendre la reproduction par le joueur
        positionReproduction = 0;
        enAttenteSaisie = true;
    }
    
    // Afficher la progression des niveaux sur les boutons du haut et de droite
    void AfficherProgressionNiveaux()
    {
        if (!deviceConnected || _launchpadOut == null) return;
        
        // D'abord, éteindre les indicateurs précédents
        // Buttons du haut (Up à Mixer) - utiliser Control Change pour les boutons du haut
        foreach (var bouton in new string[] {"Up", "Down", "Left", "Right", "Session", "User1", "User2", "Mixer"})
        {
            if (boutonsLateraux.TryGetValue(bouton, out int cc))
            {
                _launchpadOut.SendControlChange(0, (byte)cc, 0); // Éteindre avec CC
            }
        }
        
        // Buttons de droite (Volume à RecordArm) - utiliser NoteOn pour les boutons de droite
        foreach (var bouton in new string[] {"Volume", "Pan", "SendA", "SendB", "Stop", "Mute", "Solo", "RecordArm"})
        {
            if (boutonsLateraux.TryGetValue(bouton, out int note))
            {
                SendColorToLaunchpad(note, 0); // Éteindre avec NoteOn
            }
        }
        
        // Afficher le nombre total d'étapes en amber en haut
        int boutonsAfficher = Mathf.Min(nombreMaxEtapes, 8); // Maximum 8 boutons en haut
        string[] boutonsHaut = new string[] {"Up", "Down", "Left", "Right", "Session", "User1", "User2", "Mixer"};
        
        for (int i = 0; i < boutonsAfficher; i++)
        {
            if (boutonsLateraux.TryGetValue(boutonsHaut[i], out int cc))
            {
                _launchpadOut.SendControlChange(0, (byte)cc, 96); // 96 = Amber full (jaune/orange)
            }
        }
        
        // Si plus de 8 étapes, utiliser les boutons de droite pour le reste
        if (nombreMaxEtapes > 8)
        {
            int etapesRestantes = nombreMaxEtapes - 8;
            int boutonsRestantsAfficher = Mathf.Min(etapesRestantes, 8); // Maximum 8 boutons sur le côté
            string[] boutonsDroite = new string[] {"Volume", "Pan", "SendA", "SendB", "Stop", "Mute", "Solo", "RecordArm"};
            
            for (int i = 0; i < boutonsRestantsAfficher; i++)
            {
                if (boutonsLateraux.TryGetValue(boutonsDroite[i], out int note))
                {
                    SendColorToLaunchpad(note, 96); // 96 = Amber full (jaune/orange)
                }
            }
        }
        
        // Maintenant, marquer les étapes complétées en vert
        int etapesCompletees = niveau - 1; // Le niveau actuel moins 1 représente les étapes complétées
        
        // Marquer les étapes complétées en haut
        int etapesHautCompletees = Mathf.Min(etapesCompletees, 8);
        for (int i = 0; i < etapesHautCompletees; i++)
        {
            if (boutonsLateraux.TryGetValue(boutonsHaut[i], out int cc))
            {
                _launchpadOut.SendControlChange(0, (byte)cc, 87); // 87 = GREEN_FULL, vert intense
            }
        }
        
        // Si plus de 8 étapes complétées, marquer également sur la droite
        if (etapesCompletees > 8)
        {
            int etapesDroiteCompletees = Mathf.Min(etapesCompletees - 8, 8);
            string[] boutonsDroite = new string[] {"Volume", "Pan", "SendA", "SendB", "Stop", "Mute", "Solo", "RecordArm"};
            
            for (int i = 0; i < etapesDroiteCompletees; i++)
            {
                if (boutonsLateraux.TryGetValue(boutonsDroite[i], out int note))
                {
                    SendColorToLaunchpad(note, 87); // 87 = GREEN_FULL, vert intense
                }
            }
        }
    }
    
    // Afficher l'animation de victoire
    IEnumerator AfficherVictoire()
    {
        // Vérifier si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }
        
        // Annonce dans la console
        Debug.Log("VICTOIRE ! Le joueur a complété toutes les " + nombreMaxEtapes + " étapes !");
        
        // Mettre à jour l'affichage des niveaux pour montrer toutes les étapes en vert
        AfficherToutesEtapesCompletees();
        
        // Animation de victoire: remplir tout le Launchpad de vert
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                // Allumer chaque LED en vert vif
                SetPixel(x, y, 21); // Vert vif
                
                // Petit délai pour une animation de remplissage
                yield return new WaitForSeconds(0.01f);
                
                // Vérifier si le jeu est toujours activé
                if (!jeuEnabled)
                {
                    yield break;
                }
            }
        }
        
        // Attendre un moment pour que le joueur puisse voir l'écran de victoire
        yield return new WaitForSeconds(3.0f);
        
        // Faire clignoter l'écran en vert pour une animation festive
        for (int i = 0; i < 5; i++)
        {
            // Vérifier si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }
            
            // Éteindre toutes les LED de la grille centrale (garder les indicateurs de niveau)
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, 0); // Éteindre
                }
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // Vérifier si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }
            
            // Rallumer toutes les LED en vert
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, 21); // Vert vif
                }
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // Vérifier si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }
        }
        
        // Attendre un peu avant de désactiver le jeu
        yield return new WaitForSeconds(1.0f);
        
        // Désactiver le jeu
        jeuEnabled = false;
        previousJeuEnabled = false; // Important pour éviter de réactiver automatiquement
        ArreterJeu();
    }
    
    // Afficher toutes les étapes comme complétées (en vert)
    void AfficherToutesEtapesCompletees()
    {
        if (!deviceConnected || _launchpadOut == null) return;
        
        // Marquer toutes les étapes comme complétées
        string[] boutonsHaut = new string[] {"Up", "Down", "Left", "Right", "Session", "User1", "User2", "Mixer"};
        string[] boutonsDroite = new string[] {"Volume", "Pan", "SendA", "SendB", "Stop", "Mute", "Solo", "RecordArm"};
        
        // Afficher les boutons du haut en vert (jusqu'à 8 étapes) - utiliser Control Change
        int etapesHaut = Mathf.Min(nombreMaxEtapes, 8);
        for (int i = 0; i < etapesHaut; i++)
        {
            if (boutonsLateraux.TryGetValue(boutonsHaut[i], out int cc))
            {
                _launchpadOut.SendControlChange(0, (byte)cc, 87); // 87 = GREEN_FULL, vert intense
            }
        }
        
        // Afficher les boutons de droite en vert (si plus de 8 étapes) - utiliser NoteOn
        if (nombreMaxEtapes > 8)
        {
            int etapesDroite = Mathf.Min(nombreMaxEtapes - 8, 8);
            for (int i = 0; i < etapesDroite; i++)
            {
                if (boutonsLateraux.TryGetValue(boutonsDroite[i], out int note))
                {
                    SendColorToLaunchpad(note, 87); // 87 = GREEN_FULL, vert intense
                }
            }
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
        // Vérifier si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }
        
        // Éteindre seulement la grille principale, mais garder les indicateurs de progression
        // Cela signifie ne pas utiliser ResetAllLEDs() qui éteint tout
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                SetPixel(x, y, 0); // Éteindre seulement les LED de la grille
            }
        }

        // Attendre un court instant avant de commencer
        yield return new WaitForSeconds(0.3f);
        
        // Vérifier à nouveau si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }

        // Afficher chaque touche de la séquence
        foreach (int note in sequenceActuelle)
        {
            // Si le jeu a été désactivé entre-temps, arrêter
            if (!jeuEnabled)
            {
                yield break;
            }
            
            // Allumer la touche avec sa couleur
            SendColorToLaunchpad(note, (byte)couleursTouches[note]);

            // Attendre le temps d'affichage
            yield return new WaitForSeconds(tempsAffichage / 1000f);
            
            // Vérifier à nouveau si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }

            // Éteindre la touche
            SendColorToLaunchpad(note, 0); // 0 = OFF

            // Attendre entre les touches
            yield return new WaitForSeconds(tempsEntreTouches / 1000f);
            
            // Vérifier à nouveau si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }
        }

        // Signal visuel que c'est au tour du joueur
        for (int i = 0; i < 3; i++)
        {
            // Si le jeu a été désactivé entre-temps, arrêter
            if (!jeuEnabled)
            {
                yield break;
            }
            
            // Allumer brièvement les 4 coins en vert pour indiquer que c'est au joueur de jouer
            SetPixel(0, 0, 21); // Vert vif
            SetPixel(7, 0, 21); // Vert vif
            SetPixel(0, 7, 21); // Vert vif
            SetPixel(7, 7, 21); // Vert vif

            yield return new WaitForSeconds(0.08f);
            
            // Vérifier à nouveau si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }

            // Éteindre les coins
            SetPixel(0, 0, 0); // OFF
            SetPixel(7, 0, 0); // OFF
            SetPixel(0, 7, 0); // OFF
            SetPixel(7, 7, 0); // OFF

            yield return new WaitForSeconds(0.08f);
            
            // Vérifier à nouveau si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }
        }
    }

    // Afficher une animation d'erreur
    IEnumerator AfficherErreur()
    {
        // Vérifier si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }
        
        enAttenteSaisie = false;

        // Animation d'erreur: clignoter en rouge
        for (int i = 0; i < 3; i++)
        {
            // Si le jeu a été désactivé entre-temps, arrêter
            if (!jeuEnabled)
            {
                yield break;
            }
            
            // Remplir la grille de rouge
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    SetPixel(x, y, 5); // Rouge vif
                }
            }

            yield return new WaitForSeconds(0.2f);
            
            // Vérifier à nouveau si le jeu est toujours activé
            if (!jeuEnabled)
            {
                yield break;
            }
        }

        // Petit délai avant de revenir au menu principal
        yield return new WaitForSeconds(0.5f);
        
        // Vérifier une dernière fois si le jeu est toujours activé
        if (!jeuEnabled)
        {
            yield break;
        }

        // Retour au menu principal
        jeuActif = false;
        AfficherMenuPrincipal();
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

    // Fonction appelée quand un autre collider entre dans notre trigger
    void OnTriggerEnter(Collider other)
    {
        // Vérifier si c'est le joueur qui entre
        if (other.CompareTag("Player"))
        {
            // Activer le jeu
            jeuEnabled = true;

            // Afficher le menu principal après un court délai
            Invoke("AfficherMenuPrincipal", 0.5f);

            Debug.Log("Joueur détecté - Jeu Simon activé!");

            // Détruire l'objet après un délai pour permettre au jeu de s'initialiser
            Invoke("DestroyTrigger", 0.6f);
        }
    }

    // Fonction appelée quand un autre collider sort de notre trigger
    /*void OnTriggerExit(Collider other)
    {
        // Vérifier si c'est le joueur qui sort
        if (other.CompareTag("Player"))
        {
            // Désactiver le jeu
            jeuEnabled = false;

            // Arrêter le jeu proprement
            ArreterJeu();

            Debug.Log("Joueur sorti - Jeu Simon désactivé!");
        }
    }*/

    // Méthode pour détruire uniquement le trigger et non le jeu entier
    void DestroyTrigger()
    {
        // Si on veut détruire uniquement le collider trigger
        if (triggerCollider != null)
        {
            Destroy(triggerCollider);
        }
        else
        {
            // Si on veut détruire l'objet entier qui contient le trigger
            // Assurez-vous que ce n'est pas l'objet qui contient le script LaunchpadSimon
            Transform triggerObject = transform.Find("TriggerObject");
            if (triggerObject != null)
            {
                Destroy(triggerObject.gameObject);
            }
        }
    }
}