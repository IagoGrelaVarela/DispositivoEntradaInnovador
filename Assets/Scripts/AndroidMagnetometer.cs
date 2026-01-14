using UnityEngine;
using System;
using System.Threading;

public class AndroidMagnetometer : MonoBehaviour
{
    // Valores más recientes (seguros para uso con hilos)
    volatile float latestX = 0f;
    volatile float latestY = 0f;
    volatile float latestZ = 0f;
    long latestTs = 0L; // timestamp del último dato

    // Usar el sensor de Unity si no estamos en Android
    public bool useUnityFallbackInEditor = true;

    // Frecuencia de actualización para la interfaz
    public float uiUpdateHz = 30f;
    float uiTimer = 0f;

    AndroidJavaObject sensorManager = null;
    AndroidJavaObject sensor = null;
    AndroidJavaProxy listenerProxy = null;
    AndroidJavaObject unityActivity = null;

    bool running = false;

    void Start()
    {
        if (Application.platform != RuntimePlatform.Android)
        {
            Debug.Log("[AndroidMagnetometer] No se ejecuta en Android. Usando Input.compass como fallback.");
            if (useUnityFallbackInEditor)
            {
                Input.compass.enabled = true;
                Input.location?.Start();
            }
            return;
        }

        try
        {
            // Obtener la actividad de Unity actual
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
            string SENSOR_SERVICE = contextClass.GetStatic<string>("SENSOR_SERVICE");

            sensorManager = unityActivity.Call<AndroidJavaObject>("getSystemService", SENSOR_SERVICE);

            AndroidJavaClass sensorClass = new AndroidJavaClass("android.hardware.Sensor");
            int TYPE_MAGNETIC_FIELD = sensorClass.GetStatic<int>("TYPE_MAGNETIC_FIELD");

            sensor = sensorManager.Call<AndroidJavaObject>("getDefaultSensor", TYPE_MAGNETIC_FIELD);

            if (sensor == null)
            {
                Debug.LogWarning("[AndroidMagnetometer] No se encontró sensor MAGNETIC_FIELD en el dispositivo.");
                return;
            }

            // Crear y registrar el listener
            var sensorManagerClass = new AndroidJavaClass("android.hardware.SensorManager");
            int SENSOR_DELAY_GAME = sensorManagerClass.GetStatic<int>("SENSOR_DELAY_GAME");

            listenerProxy = new MagListenerProxy(OnSensorChangedCallback, OnAccuracyChangedCallback);

            bool registered = sensorManager.Call<bool>("registerListener", listenerProxy, sensor, SENSOR_DELAY_GAME);
            Debug.Log("[AndroidMagnetometer] Listener registrado: " + registered);

            running = registered;
        }
        catch (Exception ex)
        {
            Debug.LogError("[AndroidMagnetometer] Error de inicialización: " + ex);
        }
    }

    void Update()
    {
        // Si no es Android (Editor), mostrar valores de Input.compass
        if (Application.platform != RuntimePlatform.Android && useUnityFallbackInEditor)
        {
            Vector3 v = Input.compass.rawVector;
            Volatile.Write(ref latestX, v.x);
            Volatile.Write(ref latestY, v.y);
            Volatile.Write(ref latestZ, v.z);
            Interlocked.Exchange(ref latestTs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        uiTimer += Time.deltaTime;
        if (uiTimer >= (1f / Mathf.Max(1f, uiUpdateHz)))
        {
            uiTimer = 0f;
        }
    }

    void OnDisable()
    {
        UnregisterListener();
    }

    void OnApplicationQuit()
    {
        UnregisterListener();
    }

    void UnregisterListener()
    {
        if (sensorManager != null && listenerProxy != null)
        {
            try
            {
                sensorManager.Call("unregisterListener", listenerProxy);
                Debug.Log("[AndroidMagnetometer] Listener cancelado");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AndroidMagnetometer] Error al cancelar listener: " + ex);
            }
        }
        running = false;
    }

    // Llamado desde el listener de Android (onSensorChanged)
    void OnSensorChangedCallback(float x, float y, float z)
    {
        Volatile.Write(ref latestX, x);
        Volatile.Write(ref latestY, y);
        Volatile.Write(ref latestZ, z);
        Interlocked.Exchange(ref latestTs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    void OnAccuracyChangedCallback(int accuracy)
    {
        // Cambios en la precisión (opcional, se puede ignorar)
    }

    // Propiedades públicas para leer los valores
    public Vector3 LatestVector => new Vector3(Volatile.Read(ref latestX), Volatile.Read(ref latestY), Volatile.Read(ref latestZ));
    public float LatestX => Volatile.Read(ref latestX);
    public float LatestY => Volatile.Read(ref latestY);
    public float LatestZ => Volatile.Read(ref latestZ);
    public long LatestTimestamp => Interlocked.Read(ref latestTs);
    public bool IsRunning => running;

    // Clase interna para manejar el listener de Android
    class MagListenerProxy : AndroidJavaProxy
    {
        readonly Action<float, float, float> onSensorChangedAction;
        readonly Action<int> onAccuracyChangedAction;

        public MagListenerProxy(Action<float, float, float> onSensor, Action<int> onAccuracy)
            : base("android.hardware.SensorEventListener")
        {
            this.onSensorChangedAction = onSensor;
            this.onAccuracyChangedAction = onAccuracy;
        }

        public void onSensorChanged(AndroidJavaObject sensorEvent)
        {
            try
            {
                // sensorEvent.values es un array de floats
                float[] values = sensorEvent.Get<float[]>("values");
                if (values != null && values.Length >= 3)
                {
                    float x = values[0];
                    float y = values[1];
                    float z = values[2];
                    onSensorChangedAction?.Invoke(x, y, z);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MagListenerProxy] Error en onSensorChanged: " + ex);
            }
        }

        public void onAccuracyChanged(AndroidJavaObject sensor, int accuracy)
        {
            try { onAccuracyChangedAction?.Invoke(accuracy); }
            catch (Exception ex) { Debug.LogWarning("[MagListenerProxy] Error en onAccuracyChanged: " + ex); }
        }
    }
}