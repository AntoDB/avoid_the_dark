using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

public class Dialogue3DText : MonoBehaviour
{
    [Header("Dialogue Settings")]
    public GameObject[] characterPrefabs;
    public float characterSpacing = 0.5f;
    public float wordSpacing = 0.5f;
    public float lineSpacing = 1f;
    public float revealSpeed = 0.1f;
    public float dropForce = 2f;
    public float characterLifetime = 2f;

    [Header("Animation Settings")]
    public float animationSpeed = 1f;

    [Header("Dialogue Texts")]
    public string[] dialogueTexts;
    private int currentDialogueIndex = 0;

    private List<GameObject> currentCharacters = new List<GameObject>();

    // Structure pour stocker les informations de caractère (vitesse et animation)
    private class CharacterInfo
    {
        public float SpeedModifier { get; set; } = 1f;  // Par défaut = 1 (vitesse normale)
        public float AnimAmplitude { get; set; } = 0f;  // Par défaut = 0 (pas d'animation)
    }

    // Dictionnaire de mappage pour les caractères spéciaux
    private Dictionary<char, string> specialCharacterMap = new Dictionary<char, string>
    {
        { '/', "Slash" },
        { '?', "QuestionMark" },
        { ':', "Colon" },
        { '"', "QuotationMarks" },
        { '.', "Period" }
    };

    void Start()
    {
        StartDialogue();
    }

    string GetPrefabNameForCharacter(char character)
    {
        // Vérifier d'abord les caractères spéciaux
        if (specialCharacterMap.ContainsKey(character))
        {
            return specialCharacterMap[character];
        }

        // Gestion des majuscules avec préfixe M_
        if (char.IsUpper(character))
        {
            return $"M_{character}";
        }

        // Pour les lettres minuscules et chiffres, utiliser le caractère directement
        return character.ToString();
    }

    GameObject FindCharacterPrefab(string prefabName)
    {
        // Rechercher le prefab correspondant au nom
        foreach (GameObject prefab in characterPrefabs)
        {
            if (prefab.name == prefabName)
            {
                return prefab;
            }
        }

        // Fallback si aucun prefab trouvé
        Debug.LogWarning($"Pas de prefab trouvé pour le caractère {prefabName}");
        return null;
    }

    void StartDialogue()
    {
        ClearPreviousCharacters();
        StartCoroutine(RevealText(dialogueTexts[currentDialogueIndex]));
    }

    void ClearPreviousCharacters()
    {
        foreach (GameObject character in currentCharacters)
        {
            DropCharacter(character);
        }
        currentCharacters.Clear();
    }

    void DropCharacter(GameObject character)
    {
        Rigidbody rb = character.AddComponent<Rigidbody>();
        rb.useGravity = true;
        rb.AddForce(Vector3.down * dropForce, ForceMode.Impulse);

        // Supprime tout script d'animation qui pourrait exister
        BobAnimation bobAnim = character.GetComponent<BobAnimation>();
        if (bobAnim != null)
        {
            Destroy(bobAnim);
        }

        Destroy(character, characterLifetime);
    }

    (string cleanText, CharacterInfo[] charInfos) ProcessAllModifiers(string text)
    {
        // Traiter d'abord les sauts de ligne pour simplifier le traitement des balises
        string workingText = text.Replace("/n", "\n");

        // Structure temporaire pour stocker les infos de modification
        Dictionary<int, CharacterInfo> charInfoMap = new Dictionary<int, CharacterInfo>();

        // Traiter les balises de manière récursive
        while (true)
        {
            string beforeProcessing = workingText;
            workingText = ProcessSingleTagLevel(workingText, charInfoMap);

            // Si aucun changement n'a été fait, on sort de la boucle
            if (beforeProcessing == workingText)
                break;
        }

        // Re-convertir les \n en /n pour le reste du traitement
        workingText = workingText.Replace("\n", "/n");

        // Créer un tableau de CharacterInfo final
        CharacterInfo[] charInfoArray = new CharacterInfo[workingText.Length];
        for (int i = 0; i < workingText.Length; i++)
        {
            if (charInfoMap.ContainsKey(i))
            {
                charInfoArray[i] = charInfoMap[i];
            }
            else
            {
                charInfoArray[i] = new CharacterInfo();
            }
        }

        return (workingText, charInfoArray);
    }

    string ProcessSingleTagLevel(string text, Dictionary<int, CharacterInfo> charInfoMap)
    {
        // Chercher toutes les balises de vitesse ou d'animation qui n'ont pas de balises à l'intérieur
        string pattern = @"/(speed|anim)\[(\d+(?:\.\d+)?)\]\(([^\(\)]*)\)";
        Match match = Regex.Match(text, pattern);

        if (!match.Success)
        {
            return text; // Aucune balise trouvée, retourner le texte tel quel
        }

        string tagType = match.Groups[1].Value;
        float value = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        string content = match.Groups[3].Value;
        int startIndex = match.Index;
        int endIndex = startIndex + match.Length;

        // Le texte sera modifié par le remplacement de la balise par son contenu
        string newText = text.Substring(0, startIndex) + content + text.Substring(endIndex);

        // Appliquer la modification aux caractères dans la plage
        for (int i = 0; i < content.Length; i++)
        {
            int charIndex = startIndex + i;

            if (!charInfoMap.ContainsKey(charIndex))
            {
                charInfoMap[charIndex] = new CharacterInfo();
            }

            if (tagType == "speed")
            {
                charInfoMap[charIndex].SpeedModifier = value;
            }
            else if (tagType == "anim")
            {
                charInfoMap[charIndex].AnimAmplitude = value;
            }
        }

        // Décaler les infos de caractères après la balise remplacée
        int difference = match.Length - content.Length;
        Dictionary<int, CharacterInfo> updatedMap = new Dictionary<int, CharacterInfo>();

        foreach (var entry in charInfoMap)
        {
            int newIndex = entry.Key;
            // Mettre à jour seulement les index qui sont après la fin du contenu remplacé
            if (newIndex >= startIndex + content.Length)
            {
                newIndex -= difference;
            }
            updatedMap[newIndex] = entry.Value;
        }

        charInfoMap.Clear();
        foreach (var entry in updatedMap)
        {
            charInfoMap[entry.Key] = entry.Value;
        }

        return newText;
    }

    IEnumerator RevealText(string text)
    {
        // Traiter toutes les balises et obtenir les infos pour chaque caractère
        var (processedText, charInfos) = ProcessAllModifiers(text);

        float currentXPosition = 0f;
        float currentYPosition = 0f;
        int charIndex = 0;

        // Séparer le texte en lignes
        string[] lines = processedText.Split("/n");

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            // Réinitialiser la position X pour chaque nouvelle ligne
            currentXPosition = 0f;

            string line = lines[lineIndex];
            string[] words = line.Split(' ');

            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                string word = words[wordIndex];

                for (int i = 0; i < word.Length; i++)
                {
                    char character = word[i];

                    // Créer le caractère 3D si ce n'est pas un espace
                    string prefabName = GetPrefabNameForCharacter(character);
                    GameObject characterPrefab = FindCharacterPrefab(prefabName);

                    if (characterPrefab != null)
                    {
                        // Récupérer les infos de caractère
                        CharacterInfo charInfo = (charIndex < charInfos.Length) ?
                                               charInfos[charIndex] :
                                               new CharacterInfo();

                        GameObject characterObject = Instantiate(
                            characterPrefab,
                            transform.position + new Vector3(currentXPosition, -currentYPosition, 0),
                            Quaternion.Euler(0, 180, 0),
                            transform
                        );

                        currentCharacters.Add(characterObject);

                        // Appliquer l'animation si nécessaire
                        if (charInfo.AnimAmplitude > 0)
                        {
                            BobAnimation bobAnim = characterObject.AddComponent<BobAnimation>();
                            bobAnim.amplitude = charInfo.AnimAmplitude;
                            bobAnim.speed = animationSpeed;
                        }

                        // Incrémenter la position X
                        currentXPosition += characterSpacing;

                        // Attendre selon la vitesse spécifique pour ce caractère
                        float waitTime = revealSpeed;
                        if (charInfo.SpeedModifier != 1f)
                        {
                            waitTime = revealSpeed / charInfo.SpeedModifier;
                        }

                        yield return new WaitForSeconds(waitTime);
                    }

                    charIndex++;
                }

                // Ajouter l'espacement entre les mots (sauf pour le dernier mot)
                if (wordIndex < words.Length - 1)
                {
                    currentXPosition += wordSpacing;
                    charIndex++; // Pour l'espace entre les mots
                }
            }

            // Passer à la ligne suivante
            if (lineIndex < lines.Length - 1)
            {
                currentYPosition += lineSpacing;
                // Ne pas incrémenter charIndex ici car /n a déjà été compté lors du split
            }
        }

        yield return new WaitForSeconds(2f);
        NextDialogue();
    }

    void NextDialogue()
    {
        currentDialogueIndex++;
        if(currentDialogueIndex< dialogueTexts.Length)
            StartDialogue();
        else
            ClearPreviousCharacters();
        //currentDialogueIndex = (currentDialogueIndex + 1) % dialogueTexts.Length;
        //StartDialogue();
    }

    public void AddDialogue(string newDialogue)
    {
        System.Array.Resize(ref dialogueTexts, dialogueTexts.Length + 1);
        dialogueTexts[dialogueTexts.Length - 1] = newDialogue;
    }
}

// Classe pour l'animation de mouvement haut/bas
public class BobAnimation : MonoBehaviour
{
    public float amplitude = 1.0f; // Amplitude du mouvement
    public float speed = 1.0f;     // Vitesse du mouvement

    private Vector3 startPosition;
    private float randomOffset;    // Décalage aléatoire pour éviter que tous les caractères bougent synchronisés

    void Start()
    {
        startPosition = transform.localPosition;
        randomOffset = Random.Range(0f, 2f * Mathf.PI); // Décalage aléatoire pour un mouvement plus naturel
    }

    void Update()
    {
        // Calculer le déplacement vertical avec une fonction sinus
        float yOffset = amplitude * Mathf.Sin((Time.time * speed) + randomOffset);

        // Appliquer la position
        transform.localPosition = new Vector3(
            startPosition.x,
            startPosition.y + yOffset,
            startPosition.z
        );
    }
}