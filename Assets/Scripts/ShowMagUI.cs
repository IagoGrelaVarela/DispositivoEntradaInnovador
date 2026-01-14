using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowMagUI : MonoBehaviour
{
    public AndroidMagnetometer mag;
    public MagnetometerGestureDetector gestureDetector;
    public RingSpellController ringController;
    public GridManager gridManager;

    public TMP_Text mx, my, mz, status, gestureText, selectedSpellText, castResultText, calibrationStatusText, modeText;
    public Slider chargeSlider;
    public float uiUpdateHz = 10f;

    float uiTimer;
    bool localCharging = false;
    float localChargeStart = 0f;

    void OnEnable()
    {
        if (gestureDetector != null)
        {
            gestureDetector.OnGesture += OnGestureReceived;
            gestureDetector.OnCalibratedUnity.AddListener(OnCalibrationCompleted);
        }

        if (ringController != null)
        {
            if (ringController.OnSpellSelected != null)
                ringController.OnSpellSelected.AddListener(OnSpellSelected);
            if (ringController.OnSpellCast != null)
                ringController.OnSpellCast.AddListener(OnSpellCast);
        }
    }

    void OnDisable()
    {
        if (gestureDetector != null)
        {
            gestureDetector.OnGesture -= OnGestureReceived;
            gestureDetector.OnCalibratedUnity.RemoveListener(OnCalibrationCompleted);
        }

        if (ringController != null)
        {
            if (ringController.OnSpellSelected != null)
                ringController.OnSpellSelected.RemoveListener(OnSpellSelected);
            if (ringController.OnSpellCast != null)
                ringController.OnSpellCast.RemoveListener(OnSpellCast);
        }
    }

    void Start()
    {
        if (ringController != null && selectedSpellText != null)
            selectedSpellText.text = $"Hechizo: {ringController.CurrentSpell}";

        if (chargeSlider != null)
        {
            chargeSlider.minValue = 0f;
            chargeSlider.maxValue = 1f;
            chargeSlider.value = 0f;
        }

        if (calibrationStatusText != null)
            calibrationStatusText.text = "Listo para calibrar";

        if (castResultText != null)
            castResultText.text = string.Empty;

        UpdateModeTextLocal();
    }

    void Update()
    {
        uiTimer += Time.unscaledDeltaTime;
        if (uiTimer < 1f / Mathf.Max(1f, uiUpdateHz)) return;
        uiTimer = 0f;

        if (mag != null)
        {
            var v = mag.LatestVector;
            if (mx != null) mx.text = $"mx: {v.x:F2}";
            if (my != null) my.text = $"my: {v.y:F2}";
            if (mz != null) mz.text = $"mz: {v.z:F2}";
            if (status != null) status.text = mag.IsRunning ? "Sensor: activo" : "Sensor: inactivo";
        }

        if (localCharging && chargeSlider != null && ringController != null)
        {
            float elapsed = Time.unscaledTime - localChargeStart;
            float progress = Mathf.Clamp01(elapsed / ringController.maxChargeTime);
            chargeSlider.value = progress;
        }
    }

    void OnGestureReceived(string g)
    {
        if (gestureText != null) gestureText.text = $"Gesto: {g}";

        if (g == "Acercar")
        {
            if (!localCharging)
            {
                localCharging = true;
                localChargeStart = Time.unscaledTime;
                if (castResultText != null) castResultText.text = "Cargando...";
            }
            else
            {
                localCharging = false;
            }
        }

        if (g == "Arriba")
        {
            localCharging = false;
        }
    }

    void OnCalibrationCompleted(string calibrationType)
    {
        if (calibrationStatusText != null)
            calibrationStatusText.text = $"Calibrado: {calibrationType}";
    }

    void OnSpellSelected(string spellName)
    {
        if (selectedSpellText != null) selectedSpellText.text = $"Hechizo: {spellName}";
    }

    void OnSpellCast(string spellName, float power)
    {
        if (castResultText != null) castResultText.text = $"Lanzado: {spellName} (p={power:F2})";
        if (chargeSlider != null) chargeSlider.value = power;
        localCharging = false;
    }

    void OnModeChanged(string modeLabel)
    {
        if (modeText != null) modeText.text = $"MODO: {modeLabel}";
    }

    void UpdateModeTextLocal()
    {
        if (modeText == null || gridManager == null) return;
        var m = gridManager.GetCurrentMode();
        modeText.text = m == GridManager.ControlMode.GridPlacement ? "MODO: COLOCACIÓN DE TORRES" : "MODO: LANZAMIENTO DE HECHIZOS";
    }

    // Handlers de botones UI
    public void OnCalibrateAmbientButton() => gestureDetector?.CalibrateAmbient();
    public void OnCalibrateCenterButton() => gestureDetector?.CalibratePositionCenter();
    public void OnCalibrateLeftButton() => gestureDetector?.CalibratePositionLeft();
    public void OnCalibrateRightButton() => gestureDetector?.CalibratePositionRight();
    public void OnCalibrateUpButton() => gestureDetector?.CalibratePositionUp();
    public void OnCalibrateDownButton() => gestureDetector?.CalibratePositionDown();
    public void OnCalibrateApproachButton() => gestureDetector?.CalibratePositionApproach();
    public void OnClearCalibrations() => gestureDetector?.ClearAllCalibrations();

    public void EnableGameplayControl() => ringController?.EnableControl();
    public void DisableGameplayControl() => ringController?.DisableControl();
}