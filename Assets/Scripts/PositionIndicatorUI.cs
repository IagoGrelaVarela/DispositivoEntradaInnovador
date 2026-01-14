using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PositionIndicatorUI : MonoBehaviour
{
    public MagnetometerGestureDetector gestureDetector;

    [Tooltip("Paneles que se muestran cuando se detecta la posición.")]
    public GameObject leftPanel;
    public GameObject rightPanel;
    public GameObject upPanel;
    public GameObject downPanel;
    public GameObject centerPanel;
    public GameObject approachPanel;

    [Tooltip("Duración en segundos que permanece visible el indicador.")]
    public float displayDuration = 1.0f;

    Coroutine hideCoroutine;

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

    void HandleGesture(string gesture)
    {
        // Mapear gesto a panel correspondiente
        switch (gesture)
        {
            case "Izquierda":
                ShowPanel(leftPanel);
                break;
            case "Derecha":
                ShowPanel(rightPanel);
                break;
            case "Arriba":
                ShowPanel(upPanel);
                break;
            case "Abajo":
                ShowPanel(downPanel);
                break;
            case "Centro":
                ShowPanel(centerPanel);
                break;
            case "Acercar":
            case "Approach":
                ShowPanel(approachPanel);
                break;
            default:
                break;
        }
    }

    void ShowPanel(GameObject panel)
    {
        if (panel == null) return;

        // Cancelar ocultado previo
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);

        // Ocultar todos y mostrar solo el actual
        HideAllPanels();
        panel.SetActive(true);

        hideCoroutine = StartCoroutine(HideAfter(panel, displayDuration));
    }

    IEnumerator HideAfter(GameObject panel, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (panel != null) panel.SetActive(false);
        hideCoroutine = null;
    }

    void HideAllPanels()
    {
        if (leftPanel != null) leftPanel.SetActive(false);
        if (rightPanel != null) rightPanel.SetActive(false);
        if (upPanel != null) upPanel.SetActive(false);
        if (downPanel != null) downPanel.SetActive(false);
        if (centerPanel != null) centerPanel.SetActive(false);
        if (approachPanel != null) approachPanel.SetActive(false);
    }
}