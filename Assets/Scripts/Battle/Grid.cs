using UnityEngine;

public class Grid
{
    public int width;
    public int height;
    private bool[,] occupiedCells;
    private float cellSize;
    private Vector3 originPosition;

    public Grid(int width, int height, float cellSize, Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        occupiedCells = new bool[width, height];
    }

    public void DrawGrid()
    {
        // Dibujar líneas del grid (para debug)
        for (int x = 0; x <= width; x++)
        {
            Vector3 start = new Vector3(originPosition.x + x * cellSize, originPosition.y, 0);
            Vector3 end = new Vector3(originPosition.x + x * cellSize, originPosition.y + height * cellSize, 0);
            Debug.DrawLine(start, end, Color.white);
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 start = new Vector3(originPosition.x, originPosition.y + y * cellSize, 0);
            Vector3 end = new Vector3(originPosition.x + width * cellSize, originPosition.y + y * cellSize, 0);
            Debug.DrawLine(start, end, Color.white);
        }
    }

    public Vector3 GetCellCenterPosition(Vector3Int cellPosition)
    {
        return new Vector3(originPosition.x + (cellPosition.x + 0.5f) * cellSize, originPosition.y + (cellPosition.y + 0.5f) * cellSize, 0);
    }

    public bool IsCellOccupied(Vector3 worldPosition)
    {
        Vector3Int cell = WorldToCell(worldPosition);
        return IsCellOccupied(cell);
    }

    public bool IsCellOccupied(Vector3Int cell)
    {
        if (!IsWithinBounds(cell))
        {
            return true; // Fuera de límites = ocupado
        }
        return occupiedCells[cell.x, cell.y];
    }

    public void SetCellOccupied(Vector3 worldPosition, bool occupied)
    {
        Vector3Int cell = WorldToCell(worldPosition);
        SetCellOccupied(cell, occupied);
    }

    public void SetCellOccupied(Vector3Int cell, bool occupied)
    {
        if (IsWithinBounds(cell))
        {
            occupiedCells[cell.x, cell.y] = occupied;
        }
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - originPosition.y) / cellSize);
        Vector3Int cellPosition = new Vector3Int(x, y, 0);
        return cellPosition;
    }

    private bool IsWithinBounds(Vector3Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }
}