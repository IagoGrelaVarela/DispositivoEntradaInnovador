using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SpellCastEvent : UnityEvent<string, float> { }

public class RingSpellController : MonoBehaviour
{
    public MagnetometerGestureDetector gestureDetector;

    [Tooltip("Lista de nombres de hechizos.")]
    public string[] spells = new string[] { "Fire", "Ice", "Wind" };

    [Tooltip("Tiempo máximo de carga en segundos.")]
    public float maxChargeTime = 2.0f;

    [Tooltip("Cooldown entre lanzamientos (s).")]
    public float castCooldown = 0.5f;

    [Tooltip("Referencia a GridManager para saber el modo actual")]
    public GridManager gridManager;

    public UnityEvent<string> OnSpellSelected;
    public SpellCastEvent OnSpellCast;

    private int selectedIndex = 0;
    private bool charging = false;
    private float chargeStart = 0f;
    private float lastCastTime = -10f;

    void Start()
    {
        if (gridManager == null)
            gridManager = FindFirstObjectByType<GridManager>();
    }

    void OnEnable()
    {
        if (gestureDetector != null)
            gestureDetector.OnGesture += HandleGesture;
    }

    void OnDisable()
    {
        if (gestureDetector != null)
            gestureDetector.OnGesture -= HandleGesture;
    }

    void HandleGesture(string g)
    {
        // Solo procesar si estamos en modo SpellCasting
        if (gridManager != null && gridManager.GetCurrentMode() != GridManager.ControlMode.SpellCasting)
            return;

        if (g == "Izquierda")
        {
            selectedIndex = Mathf.Max(0, selectedIndex - 1);
            OnSpellSelected?.Invoke(CurrentSpell);
            Debug.Log("[RingSpellController] Hechizo seleccionado: " + CurrentSpell);
            return;
        }

        if (g == "Derecha")
        {
            selectedIndex = Mathf.Min(spells.Length - 1, selectedIndex + 1);
            OnSpellSelected?.Invoke(CurrentSpell);
            Debug.Log("[RingSpellController] Hechizo seleccionado: " + CurrentSpell);
            return;
        }

        if (g == "Acercar")
        {
            if (!charging)
            {
                charging = true;
                chargeStart = Time.unscaledTime;
                Debug.Log("[RingSpellController] Inicio de carga");
            }
            else
            {
                float power = Mathf.Clamp01((Time.unscaledTime - chargeStart) / maxChargeTime);
                TryCast(CurrentSpell, power);
                charging = false;
                Debug.Log("[RingSpellController] Carga finalizada -> lanzamiento con potencia: " + power);
            }
            return;
        }

        if (g == "Arriba")
        {
            if (charging)
            {
                float power = Mathf.Clamp01((Time.unscaledTime - chargeStart) / maxChargeTime);
                TryCast(CurrentSpell, power);
                charging = false;
                Debug.Log("[RingSpellController] Lanzamiento cargado: " + CurrentSpell + " p=" + power);
            }
            else
            {
                TryCast(CurrentSpell, 0f);
                Debug.Log("[RingSpellController] Lanzamiento normal: " + CurrentSpell);
            }
            return;
        }
    }

    void TryCast(string spellName, float power)
    {
        if (Time.unscaledTime - lastCastTime < castCooldown) return;
        lastCastTime = Time.unscaledTime;
        OnSpellCast?.Invoke(spellName, power);

        // Lanzar desde todas las torres gato
        CatController[] towers = FindObjectsByType<CatController>(FindObjectsSortMode.None);
        foreach (CatController tower in towers)
        {
            tower.CastSpell(spellName, power);
        }
    }

    public string CurrentSpell => spells != null && spells.Length > 0 ? spells[selectedIndex] : string.Empty;

    public void EnableControl()
    {
        if (gestureDetector != null) gestureDetector.OnGesture += HandleGesture;
    }

    public void DisableControl()
    {
        if (gestureDetector != null) gestureDetector.OnGesture -= HandleGesture;
    }
}