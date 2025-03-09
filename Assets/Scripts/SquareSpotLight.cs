using System.Collections;
using UnityEngine;

public class SquareSpotLight : MonoBehaviour
{
    public Light spotLight;
    public Texture2D cookieTexture;
    public float intensity = 1f;
    public Color lightColor = Color.white;
    public float range = 10f;
    public float angle = 30f;
    public float side = 5f;

    [Header("Paramètres de clignotement")]
    [Tooltip("Active la lumière clignotante")]
    public bool isFlickering;
    [Tooltip("Durée minimale pendant laquelle la lumière reste allumée")]
    public float minOnTime = 0.1f;
    [Tooltip("Durée maximale pendant laquelle la lumière reste allumée")]
    public float maxOnTime = 2.0f;

    [Tooltip("Durée minimale pendant laquelle la lumière reste éteinte")]
    public float minOffTime = 0.05f;
    [Tooltip("Durée maximale pendant laquelle la lumière reste éteinte")]
    public float maxOffTime = 0.5f;

    [Tooltip("Probabilité que la lumière clignote rapidement plusieurs fois")]
    [Range(0f, 1f)]
    public float flickerProbability = 0.3f;

    void Start()
    {
        SetupSquareSpotLight();
        CreateSquareCollider();
        if (isFlickering)
            StartCoroutine(FlickerRoutine());
    }

    void SetupSquareSpotLight()
    {
        // Si aucune lumière n'est assignée, créer une nouvelle
        if (spotLight == null)
        {
            spotLight = gameObject.GetComponent<Light>();
            if (spotLight == null)
            {
                spotLight = gameObject.AddComponent<Light>();
            }
        }

        // Configurer comme spot
        spotLight.type = LightType.Spot;
        spotLight.intensity = intensity;
        spotLight.color = lightColor;
        spotLight.range = range;
        spotLight.spotAngle = angle;

        // Si aucune texture cookie n'est fournie, créer une texture carrée
        if (cookieTexture == null)
        {
            cookieTexture = CreateSquareCookie(64);
        }

        // Appliquer la texture cookie
        spotLight.cookie = cookieTexture;
    }

    Texture2D CreateSquareCookie(int resolution)
    {
        Texture2D texture = new Texture2D(resolution, resolution);
        Color[] colors = new Color[resolution * resolution];

        // Calculer les couleurs pour chaque pixel
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // Normaliser les coordonnées de -1 à 1
                float nx = (2.0f * x / resolution) - 1.0f;
                float ny = (2.0f * y / resolution) - 1.0f;

                // Un carré parfait (utiliser le max de |x| et |y|)
                float dist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                // 0 à l'intérieur du carré, 1 à l'extérieur
                float value = dist <= 0.8f ? 1f : 0f;

                // Option: ajouter un léger adoucissement des bords
                if (dist > 0.75f && dist <= 0.85f)
                {
                    value = 1f - (dist - 0.75f) / 0.1f;
                }

                colors[y * resolution + x] = new Color(value, value, value, 1);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    void CreateSquareCollider()
    {
        BoxCollider collider = gameObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(side, side, spotLight.range);
        collider.center = new Vector3(0, 0, spotLight.range / 2);
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // Allumer la lumière
            spotLight.enabled = true;

            // Attendre pendant un temps aléatoire
            yield return new WaitForSeconds(Random.Range(minOnTime, maxOnTime));

            // Éteindre la lumière
            spotLight.enabled = false;

            // Attendre pendant un temps aléatoire
            yield return new WaitForSeconds(Random.Range(minOffTime, maxOffTime));

            // Possibilité de faire un clignotement rapide et nerveux
            if (Random.value < flickerProbability)
            {
                // Séquence de clignotements rapides
                int flickerCount = Random.Range(2, 6);
                for (int i = 0; i < flickerCount; i++)
                {
                    spotLight.enabled = true;
                    yield return new WaitForSeconds(Random.Range(0.01f, 0.1f));
                    spotLight.enabled = false;
                    yield return new WaitForSeconds(Random.Range(0.01f, 0.1f));
                }
            }
        }
    }
}