using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    public enum ControlMode { GridPlacement, SpellCasting }

    public static GridManager Instance;
    public int width = 3;
    public int height = 1;
    public float cellSize = 1f;
    public Vector3 originPosition = Vector3.zero;
    public GameObject cellPrefab;
    public GameObject towerPrefab;
    public int towerCost = 30;
    public int upgradeCost = 20;

    [SerializeField] private Color selectedCellColor = Color.black;
    [SerializeField] private Color normalCellColor = Color.white;

    public RingSpellController ringSpellController;
    public MagnetometerGestureDetector gestureDetector;

    public Grid grid;
    private Dictionary<Vector3Int, GameObject> towerDictionary = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, GameObject> cellDictionary = new Dictionary<Vector3Int, GameObject>();
    private Dictionary<Vector3Int, SpriteRenderer> cellSpriteRendererDictionary = new Dictionary<Vector3Int, SpriteRenderer>();

    private GameObject lastSelectedTower = null;
    private bool interactionEnabled = true;
    private ControlMode currentMode = ControlMode.GridPlacement;

    private Vector3Int selectedCellPosition = Vector3Int.zero;
    private Vector3Int lastSelectedCellPosition = new Vector3Int(-1, -1, -1);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        grid = new Grid(width, height, cellSize, originPosition);
        CreateGrid();
        SetCellsActive(interactionEnabled);
        UpdateSelectedCellColor();
    }

    void OnEnable()
    {
        if (ringSpellController != null)
        {
            ringSpellController.OnSpellCast.AddListener(OnSpellCast);
        }

        if (gestureDetector != null)
        {
            gestureDetector.OnGesture += OnGestureReceived;
        }
    }

    void OnDisable()
    {
        if (ringSpellController != null)
        {
            ringSpellController.OnSpellCast.RemoveListener(OnSpellCast);
        }

        if (gestureDetector != null)
        {
            gestureDetector.OnGesture -= OnGestureReceived;
        }
    }

    void CreateGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                Vector3 cellCenterPosition = grid.GetCellCenterPosition(cellPosition);
                GameObject cellObject = Instantiate(cellPrefab, cellCenterPosition, Quaternion.identity, transform);
                cellObject.name = $"Cell_{x}_{y}";
                cellDictionary[cellPosition] = cellObject;

                SpriteRenderer spriteRenderer = cellObject.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    cellSpriteRendererDictionary[cellPosition] = spriteRenderer;
                    spriteRenderer.color = normalCellColor;
                }
            }
        }
    }

    void UpdateSelectedCellColor()
    {
        // Restablecer color de la celda anterior
        if (lastSelectedCellPosition != new Vector3Int(-1, -1, -1) &&
            cellSpriteRendererDictionary.TryGetValue(lastSelectedCellPosition, out SpriteRenderer lastRenderer))
        {
            lastRenderer.color = normalCellColor;
        }

        // Cambiar color de la celda seleccionada
        if (currentMode == ControlMode.GridPlacement && interactionEnabled &&
            cellSpriteRendererDictionary.TryGetValue(selectedCellPosition, out SpriteRenderer selectedRenderer))
        {
            selectedRenderer.color = selectedCellColor;
            lastSelectedCellPosition = selectedCellPosition;
        }
    }

    void OnGestureReceived(string gesture)
    {
        if (!interactionEnabled) return;

        if (currentMode == ControlMode.GridPlacement)
        {
            switch (gesture)
            {
                case "Izquierda":
                    MoveSelection(-1, 0);
                    break;
                case "Derecha":
                    MoveSelection(1, 0);
                    break;
                case "Arriba":
                    HandleUpgradeTower();
                    break;
                case "Abajo":
                    HandleTowerPlacement();
                    break;
                case "Centro":
                    SwitchToSpellCastMode();
                    break;
                default:
                    break;
            }
        }
        else if (currentMode == ControlMode.SpellCasting)
        {
            if (gesture == "Centro")
            {
                SwitchToGridMode();
            }
        }
    }

    void OnSpellCast(string spellName, float power)
    {
        if (!interactionEnabled || currentMode != ControlMode.SpellCasting) return;
        Debug.Log($"[GridManager] Hechizo lanzado: {spellName} (poder: {power:F2})");
    }

    void HandleTowerPlacement()
    {
        // Si la celda está vacía, colocar torre
        if (!grid.IsCellOccupied(selectedCellPosition))
        {
            if (BattleManager.Instance.money >= towerCost)
            {
                BattleManager.Instance.AddMoney(-towerCost);
                PlaceTower(selectedCellPosition);
                HideTowerOptions();
            }
            else
            {
                Debug.Log("[GridManager] Dinero insuficiente para torre");
            }
        }
        else
        {
            // Si ya hay torre, mostrar opciones
            GameObject tower = GetTowerAtCell(selectedCellPosition);
            if (tower != null)
            {
                ShowTowerOptions(tower, true);
            }
        }
    }

    void HandleUpgradeTower()
    {
        GameObject tower = GetTowerAtCell(selectedCellPosition);
        if (tower != null && grid.IsCellOccupied(selectedCellPosition))
        {
            TowerPrefab towerPrefab = tower.GetComponent<TowerPrefab>();
            if (towerPrefab != null)
            {
                if (BattleManager.Instance.money >= upgradeCost)
                {
                    BattleManager.Instance.AddMoney(-upgradeCost);
                    towerPrefab.UpgradeTower();
                    Debug.Log($"[GridManager] Torre mejorada en: {selectedCellPosition}");
                }
                else
                {
                    Debug.Log("[GridManager] Dinero insuficiente para mejorar");
                }
            }
            else
            {
                Debug.Log("[GridManager] Esta torre no puede mejorarse");
            }
        }
    }

    void MoveSelection(int dx, int dy)
    {
        Vector3Int newPosition = selectedCellPosition + new Vector3Int(dx, dy, 0);

        if (newPosition.x >= 0 && newPosition.x < width && newPosition.y >= 0 && newPosition.y < height)
        {
            selectedCellPosition = newPosition;
            UpdateSelectedCellColor();
            HideTowerOptions();
            Debug.Log($"[GridManager] Selección movida a: {selectedCellPosition}");
        }
    }

    void SwitchToSpellCastMode()
    {
        currentMode = ControlMode.SpellCasting;
        HideTowerOptions();
        UpdateSelectedCellColor();
        Debug.Log("[GridManager] Modo: LANZAMIENTO DE HECHIZOS");
    }

    void SwitchToGridMode()
    {
        currentMode = ControlMode.GridPlacement;
        UpdateSelectedCellColor();
        Debug.Log("[GridManager] Modo: COLOCACIÓN DE TORRES");
    }

    public void SetInteractionEnabled(bool enabled)
    {
        interactionEnabled = enabled;
        SetCellsActive(enabled);
        UpdateSelectedCellColor();
    }

    void SetCellsActive(bool active)
    {
        foreach (var kvp in cellDictionary)
        {
            kvp.Value.SetActive(active && !grid.IsCellOccupied(kvp.Key));
        }
    }

    void PlaceTower(Vector3Int cellPosition)
    {
        if (towerPrefab != null)
        {
            Vector3 cellCenterPosition = grid.GetCellCenterPosition(cellPosition);
            GameObject newTower = Instantiate(towerPrefab, cellCenterPosition, Quaternion.identity, transform);
            towerDictionary[cellPosition] = newTower;
            grid.SetCellOccupied(cellPosition, true);
            cellDictionary[cellPosition].SetActive(false);
            Debug.Log($"[GridManager] Torre colocada en: {cellPosition}");
        }
    }

    public GameObject GetTowerAtCell(Vector3Int cellPosition)
    {
        towerDictionary.TryGetValue(cellPosition, out GameObject tower);
        return tower;
    }

    void ShowTowerOptions(GameObject towerGo, bool show)
    {
        TowerPrefab towerPrefab = towerGo.GetComponent<TowerPrefab>();
        if (towerPrefab != null && show)
        {
            if (lastSelectedTower != null && lastSelectedTower != towerGo)
            {
                HideTowerOptions();
            }

            lastSelectedTower = towerGo;
            towerPrefab.ShowRangeIndicator(true);
            towerPrefab.ShowUI(true);
        }
        else
        {
            HideTowerOptions();
        }
    }

    void HideTowerOptions()
    {
        if (lastSelectedTower != null)
        {
            TowerPrefab towerPrefab = lastSelectedTower.GetComponent<TowerPrefab>();
            if (towerPrefab != null)
            {
                towerPrefab.ShowRangeIndicator(false);
                towerPrefab.ShowUI(false);
            }
            lastSelectedTower = null;
        }
    }

    public void SellTower(Vector3Int cellPosition)
    {
        if (towerDictionary.TryGetValue(cellPosition, out GameObject towerGo))
        {
            TowerPrefab towerPrefab = towerGo.GetComponent<TowerPrefab>();
            int sellValue = towerPrefab != null ? towerPrefab.sellValue : towerCost / 2;

            grid.SetCellOccupied(cellPosition, false);
            Destroy(towerGo);
            towerDictionary.Remove(cellPosition);
            cellDictionary[cellPosition].SetActive(true);
            BattleManager.Instance.AddMoney(sellValue);
            Debug.Log($"[GridManager] Torre vendida en: {cellPosition} (+{sellValue} monedas)");
        }
        else
        {
            Debug.LogWarning("No hay torre en esa posición para vender.");
        }
    }

    public ControlMode GetCurrentMode() => currentMode;
}