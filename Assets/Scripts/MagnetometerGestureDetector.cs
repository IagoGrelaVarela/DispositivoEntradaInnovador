using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;

public class MagnetometerGestureDetector : MonoBehaviour
{
    public enum CalibPosition { Ambiente, Centro, Izquierda, Derecha, Arriba, Abajo, Acercar }

    public AndroidMagnetometer mag;

    [Header("Muestreo / Filtrado")]
    [Tooltip("Factor alpha para filtro exponencial. Más alto = más rápido, menos suavizado.")]
    [Range(0.01f, 1f)]
    public float lowPassAlpha = 0.25f;

    [Tooltip("Tamaño del buffer (frames) para cálculo de desviación")]
    public int sampleWindow = 12;

    [Header("Debounce / Histéresis")]
    [Tooltip("Frames consecutivos para confirmar entrada en una posición")]
    public int enterFrames = 3;
    [Tooltip("Frames consecutivos para confirmar salida de una posición")]
    public int exitFrames = 3;

    [Header("Detección calibrada")]
    [Tooltip("Mínimo multiplicador de magnitud relativa para considerar match")]
    [Range(0.1f, 1f)]
    public float calibratedMagnitudeRatio = 0.5f;

    [Tooltip("Distancia máxima absoluta (µT) para considerar match")]
    public float calibratedAbsTolerance = 12f;

    [Header("Fallback heurístico (sin calibración)")]
    public float dirThreshold = 18f;
    public float crossTolerance = 10f;
    public float approachThreshold = 35f;

    [Header("Cooldown")]
    [Tooltip("Cooldown entre emisiones de posición (s)")]
    public float emitCooldown = 0.2f;

    [Header("Calibración")]
    [Tooltip("Segundos de muestreo para calibración")]
    public float calibrationSampleSeconds = 1.0f;

    public UnityEvent<string> OnGestureUnity;
    public UnityEvent<string> OnCalibratedUnity;
    public event Action<string> OnGesture;
    public event Action<CalibPosition> OnCalibrated;

    // Estado interno
    readonly LinkedList<Vector3> samples = new();
    Vector3 ema = Vector3.zero;
    bool emaInitialized = false;

    // Almacenamiento de calibración
    Vector3 ambient = Vector3.zero;
    readonly Dictionary<CalibPosition, Vector3> positionRefs = new();

    const string PREF_AMBIENT = "MagCal_ambient";
    const string PREF_POS_FORMAT = "MagCal_pos_{0}";

    // Contadores debounce por posición
    readonly Dictionary<CalibPosition, int> enterCounters = new();
    readonly Dictionary<CalibPosition, int> exitCounters = new();
    CalibPosition currentPosition = CalibPosition.Centro;
    CalibPosition lastEmittedPosition = CalibPosition.Centro;
    float lastEmitTime = -10f;

    public bool clearCalibrationsOnStart = true;

    void Awake()
    {
        // Inicializar contadores
        foreach (CalibPosition p in Enum.GetValues(typeof(CalibPosition)))
        {
            enterCounters[p] = 0;
            exitCounters[p] = 0;
        }

        if (clearCalibrationsOnStart)
            ClearAllCalibrations();
        else
            LoadCalibration();
    }

    void Update()
    {
        if (mag == null) return;

        // 1) Leer y aplicar filtro paso bajo
        Vector3 raw = mag.LatestVector;
        if (!emaInitialized)
        {
            ema = raw;
            emaInitialized = true;
        }
        else
        {
            ema = Vector3.Lerp(ema, raw, Mathf.Clamp01(lowPassAlpha));
        }

        // 2) Actualizar ventana de muestras
        samples.AddLast(ema);
        while (samples.Count > sampleWindow) samples.RemoveFirst();

        // 3) Elegir referencia base
        Vector3 baseRef = positionRefs.ContainsKey(CalibPosition.Centro) ? positionRefs[CalibPosition.Centro] : ambient;
        Vector3 deltaFromBase = ema - baseRef;
        float deltaMag = deltaFromBase.magnitude;

        // 4) Detección: preferir referencias calibradas si existen
        CalibPosition detected = CalibPosition.Centro; // Por defecto centro

        bool anyCalibrated = positionRefs.Count > 0;

        if (anyCalibrated)
        {
            // Buscar el match con menor distancia normalizada
            CalibPosition best = CalibPosition.Centro;
            float bestScore = float.MaxValue;

            foreach (CalibPosition pos in new[] { CalibPosition.Izquierda, CalibPosition.Derecha, CalibPosition.Arriba, CalibPosition.Abajo, CalibPosition.Acercar })
            {
                if (!positionRefs.ContainsKey(pos)) continue;
                Vector3 refDelta = positionRefs[pos] - baseRef;
                float refMag = refDelta.magnitude;
                if (refMag < 1e-3f) continue;

                // Puntuación: distancia entre delta actual y referencia
                float absDist = (deltaFromBase - refDelta).magnitude;
                float rel = deltaFromBase.magnitude / refMag; // magnitud relativa
                float score = absDist + Mathf.Abs(1f - rel) * refMag * 0.5f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = pos;
                }
            }

            // Decidir aceptación por umbrales (histéresis)
            if (best != CalibPosition.Centro && positionRefs.ContainsKey(best))
            {
                Vector3 refDelta = positionRefs[best] - baseRef;
                float refMag = refDelta.magnitude;
                float absDist = (deltaFromBase - refDelta).magnitude;

                // Condición de entrada
                bool enter = (deltaFromBase.magnitude >= refMag * calibratedMagnitudeRatio && absDist <= Mathf.Max(calibratedAbsTolerance, refMag * 0.5f))
                             || (absDist <= calibratedAbsTolerance);

                if (enter)
                    detected = best;
                else
                    detected = CalibPosition.Centro;
            }
            else
            {
                detected = CalibPosition.Centro;
            }
        }
        else
        {
            // Heurística de fallback usando deltas de ejes
            Vector3 oldest = samples.First.Value;
            Vector3 deltaWindow = ema - oldest;
            // Comprobaciones direccionales en X / Y
            if (Mathf.Abs(deltaWindow.x) >= dirThreshold && Mathf.Abs(deltaWindow.y) <= crossTolerance)
                detected = deltaWindow.x > 0 ? CalibPosition.Derecha : CalibPosition.Izquierda;
            else if (Mathf.Abs(deltaWindow.y) >= dirThreshold && Mathf.Abs(deltaWindow.x) <= crossTolerance)
                detected = deltaWindow.y > 0 ? CalibPosition.Arriba : CalibPosition.Abajo;
            else if (deltaMag >= approachThreshold)
                detected = CalibPosition.Acercar;
            else
                detected = CalibPosition.Centro;
        }

        // 5) Contadores debounce/histéresis
        foreach (CalibPosition pos in Enum.GetValues(typeof(CalibPosition)))
        {
            if (pos == detected)
            {
                enterCounters[pos] = Mathf.Min(enterCounters[pos] + 1, enterFrames);
                exitCounters[pos] = 0;
            }
            else
            {
                exitCounters[pos] = Mathf.Min(exitCounters[pos] + 1, exitFrames);
                enterCounters[pos] = 0;
            }
        }

        // Determinar posición estable: se activa cuando enterCounters >= enterFrames
        CalibPosition stable = CalibPosition.Centro;
        foreach (CalibPosition pos in Enum.GetValues(typeof(CalibPosition)))
        {
            if (enterCounters[pos] >= enterFrames)
            {
                stable = pos;
                break;
            }
        }

        // Si la posición actual estaba activa pero exit counters alcanzó exitFrames, ir a Centro
        if (currentPosition != stable)
        {
            currentPosition = stable;
        }

        // 6) Emitir gesto solo cuando cambia la posición estable y pasó el cooldown
        if (currentPosition != lastEmittedPosition && Time.unscaledTime - lastEmitTime >= emitCooldown)
        {
            string label = PositionToLabel(currentPosition);
            Emit(label, currentPosition == CalibPosition.Acercar);
            lastEmittedPosition = currentPosition;
            lastEmitTime = Time.unscaledTime;
        }
    }

    // Métodos de matching (se mantienen para compatibilidad)
    bool IsMatchWithRange(Vector3 delta, Vector3 refDelta, float rangeFactor)
    {
        float refMag = refDelta.magnitude;
        if (refMag < 1e-3f) return false;

        float magRatio = delta.magnitude / refMag;
        float dot = Vector3.Dot(delta.normalized, refDelta.normalized);

        if (dot >= 0.6f && magRatio >= rangeFactor)
            return true;

        if ((delta - refDelta).magnitude <= Mathf.Max(10f, refMag * 0.5f))
            return true;

        return false;
    }

    bool IsMatch(Vector3 delta, Vector3 refDelta)
    {
        float refMag = refDelta.magnitude;
        if (refMag < 1e-3f) return false;

        float magRatio = delta.magnitude / refMag;
        float dot = Vector3.Dot(delta.normalized, refDelta.normalized);

        if (dot >= 0.7f && magRatio >= 0.5f)
            return true;

        if ((delta - refDelta).magnitude <= Mathf.Max(8f, refMag * 0.4f))
            return true;

        return false;
    }

    void Emit(string gesture, bool isApproach)
    {
        try { OnGestureUnity?.Invoke(gesture); } catch { }
        OnGesture?.Invoke(gesture);
        Debug.Log($"[MagnetometerGestureDetector] Gesto detectado: {gesture} (acercar={isApproach})");
    }

    string PositionToLabel(CalibPosition pos)
    {
        return pos switch
        {
            CalibPosition.Izquierda => "Izquierda",
            CalibPosition.Derecha => "Derecha",
            CalibPosition.Arriba => "Arriba",
            CalibPosition.Abajo => "Abajo",
            CalibPosition.Acercar => "Acercar",
            CalibPosition.Centro => "Centro",
            CalibPosition.Ambiente => "Ambiente",
            _ => pos.ToString()
        };
    }

    // API de Calibración
    public void CalibrateAmbient() => StartCoroutine(CalibrateRoutineAmbient());
    public void CalibratePositionCenter() => StartCoroutine(CalibrateRoutinePosition(CalibPosition.Centro, "Centro"));
    public void CalibratePositionLeft() => StartCoroutine(CalibrateRoutinePosition(CalibPosition.Izquierda, "Izquierda"));
    public void CalibratePositionRight() => StartCoroutine(CalibrateRoutinePosition(CalibPosition.Derecha, "Derecha"));
    public void CalibratePositionUp() => StartCoroutine(CalibrateRoutinePosition(CalibPosition.Arriba, "Arriba"));
    public void CalibratePositionDown() => StartCoroutine(CalibrateRoutinePosition(CalibPosition.Abajo, "Abajo"));
    public void CalibratePositionApproach() => StartCoroutine(CalibrateRoutinePosition(CalibPosition.Acercar, "Acercar"));

    System.Collections.IEnumerator CalibrateRoutineAmbient()
    {
        if (mag == null) yield break;
        Debug.Log("[MagnetometerGestureDetector] Iniciando calibración de Ambiente...");
        float t0 = Time.unscaledTime;
        float end = t0 + calibrationSampleSeconds;
        Vector3 sum = Vector3.zero;
        int count = 0;
        while (Time.unscaledTime < end)
        {
            sum += mag.LatestVector;
            count++;
            yield return null;
        }
        if (count == 0) yield break;
        ambient = sum / count;
        SaveVector(PREF_AMBIENT, ambient);
        Debug.Log($"[MagnetometerGestureDetector] ✓ Ambiente calibrado: {ambient}");
        OnCalibratedUnity?.Invoke("Ambiente");
        OnCalibrated?.Invoke(CalibPosition.Ambiente);
    }

    System.Collections.IEnumerator CalibrateRoutinePosition(CalibPosition pos, string label)
    {
        if (mag == null) yield break;
        Debug.Log($"[MagnetometerGestureDetector] Iniciando calibración de {label}...");
        float t0 = Time.unscaledTime;
        float end = t0 + calibrationSampleSeconds;
        Vector3 sum = Vector3.zero;
        int count = 0;
        while (Time.unscaledTime < end)
        {
            sum += mag.LatestVector;
            count++;
            yield return null;
        }
        if (count == 0) yield break;
        Vector3 avg = sum / count;
        positionRefs[pos] = avg;
        SaveVector(string.Format(PREF_POS_FORMAT, pos.ToString()), avg);
        Debug.Log($"[MagnetometerGestureDetector] {label} calibrado: {avg}");
        OnCalibratedUnity?.Invoke(label);
        OnCalibrated?.Invoke(pos);
    }

    // Ayudantes de persistencia
    void SaveVector(string key, Vector3 v)
    {
        PlayerPrefs.SetString(key, $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}");
        PlayerPrefs.Save();
    }

    bool TryLoadVector(string key, out Vector3 v)
    {
        v = Vector3.zero;
        if (!PlayerPrefs.HasKey(key)) return false;
        var s = PlayerPrefs.GetString(key);
        var parts = s.Split(',');
        if (parts.Length != 3) return false;
        if (float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) &&
            float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            v = new Vector3(x, y, z);
            return true;
        }
        return false;
    }

    void LoadCalibration()
    {
        if (TryLoadVector(PREF_AMBIENT, out Vector3 a)) ambient = a;
        foreach (CalibPosition pos in Enum.GetValues(typeof(CalibPosition)))
        {
            if (pos == CalibPosition.Ambiente) continue;
            string key = string.Format(PREF_POS_FORMAT, pos.ToString());
            if (TryLoadVector(key, out Vector3 p)) positionRefs[pos] = p;
        }
    }

    // Exponer calibración actual para UI
    public Vector3 Ambient => ambient;
    public bool HasCalibration(CalibPosition pos) => positionRefs.ContainsKey(pos);
    public bool HasAmbient => PlayerPrefs.HasKey(PREF_AMBIENT);
    public Vector3 GetPositionRef(CalibPosition pos) => positionRefs.ContainsKey(pos) ? positionRefs[pos] : Vector3.zero;
    public CalibPosition CurrentDetectedPosition => currentPosition;

    public void ClearAllCalibrations()
    {
        PlayerPrefs.DeleteKey(PREF_AMBIENT);
        foreach (CalibPosition pos in Enum.GetValues(typeof(CalibPosition)))
            PlayerPrefs.DeleteKey(string.Format(PREF_POS_FORMAT, pos.ToString()));
        PlayerPrefs.Save();
        positionRefs.Clear();
        ambient = Vector3.zero;
        currentPosition = CalibPosition.Centro;
        lastEmittedPosition = CalibPosition.Centro;
        Debug.Log("[MagnetometerGestureDetector] Calibraciones eliminadas");
    }
}