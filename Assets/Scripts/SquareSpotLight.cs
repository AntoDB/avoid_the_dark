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

    [Header("Param�tres de clignotement")]
    [Tooltip("Active la lumi�re clignotante")]
    public bool isFlickering;
    [Tooltip("Dur�e minimale pendant laquelle la lumi�re reste allum�e")]
    public float minOnTime = 0.1f;
    [Tooltip("Dur�e maximale pendant laquelle la lumi�re reste allum�e")]
    public float maxOnTime = 2.0f;

    [Tooltip("Dur�e minimale pendant laquelle la lumi�re reste �teinte")]
    public float minOffTime = 0.05f;
    [Tooltip("Dur�e maximale pendant laquelle la lumi�re reste �teinte")]
    public float maxOffTime = 0.5f;

    [Tooltip("Probabilit� que la lumi�re clignote rapidement plusieurs fois")]
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
        // Si aucune lumi�re n'est assign�e, cr�er une nouvelle
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

        // Si aucune texture cookie n'est fournie, cr�er une texture carr�e
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
                // Normaliser les coordonn�es de -1 � 1
                float nx = (2.0f * x / resolution) - 1.0f;
                float ny = (2.0f * y / resolution) - 1.0f;

                // Un carr� parfait (utiliser le max de |x| et |y|)
                float dist = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                // 0 � l'int�rieur du carr�, 1 � l'ext�rieur
                float value = dist <= 0.8f ? 1f : 0f;

                // Option: ajouter un l�ger adoucissement des bords
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
            // Allumer la lumi�re
            spotLight.enabled = true;

            // Attendre pendant un temps al�atoire
            yield return new WaitForSeconds(Random.Range(minOnTime, maxOnTime));

            // �teindre la lumi�re
            spotLight.enabled = false;

            // Attendre pendant un temps al�atoire
            yield return new WaitForSeconds(Random.Range(minOffTime, maxOffTime));

            // Possibilit� de faire un clignotement rapide et nerveux
            if (Random.value < flickerProbability)
            {
                // S�quence de clignotements rapides
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