using UnityEngine;
using UnityEngine.UI;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance;

    [Tooltip("Relación de aspecto objetivo (16:9)")]
    public float targetAspectRatio = 16f / 9f;

    [Tooltip("Permitir solo landscape (izquierda/derecha)")]
    public bool landscapeOnly = true;

    [Tooltip("Color de las barras")]
    public Color barColor = Color.black;

    Camera mainCamera;
    int lastWidth = 0;
    int lastHeight = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Permitir rotación automática SOLO en landscape
        if (landscapeOnly)
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }

        Screen.fullScreen = true;
    }

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[ScreenManager] No se encontró la cámara principal.");
            return;
        }

        // Asegurar background negro
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = barColor;

        // Inicializar dimensiones
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        // Ajustar canvas y viewport
        ApplyScreenSettings();
    }

    void Update()
    {
        // Aplicar sólo si cambia resolución/orientación
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            ApplyScreenSettings();
        }
    }

    void ApplyScreenSettings()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float currentAspect = screenWidth / screenHeight;

        float viewportWidth = 1f;
        float viewportHeight = 1f;
        float viewportX = 0f;
        float viewportY = 0f;

        if (currentAspect > targetAspectRatio)
        {
            // Pantalla muy ancha: barras laterales
            viewportWidth = targetAspectRatio / currentAspect;
            viewportX = (1f - viewportWidth) / 2f;
        }
        else if (currentAspect < targetAspectRatio)
        {
            // Pantalla muy alta: barras arriba/abajo
            viewportHeight = currentAspect / targetAspectRatio;
            viewportY = (1f - viewportHeight) / 2f;
        }

        mainCamera.rect = new Rect(viewportX, viewportY, viewportWidth, viewportHeight);
        mainCamera.backgroundColor = barColor;

        // Mover canvases Overlay a ScreenSpaceCamera para que respeten el viewport
        EnsureCanvasesRenderInCamera(mainCamera);

        Debug.Log($"[ScreenManager] Viewport X={viewportX:F3} Y={viewportY:F3} W={viewportWidth:F3} H={viewportHeight:F3} (Screen {screenWidth}x{screenHeight})");
    }

    void EnsureCanvasesRenderInCamera(Camera cam)
    {
        // Encontrar todos los canvases activos
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            // Sólo convertir los que son ScreenSpaceOverlay
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
            }
            else if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null)
            {
                c.worldCamera = cam;
            }
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
            ApplyScreenSettings();
    }
}