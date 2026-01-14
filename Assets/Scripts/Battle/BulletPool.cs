using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance;

    [System.Serializable]
    public struct BulletPrefab
    {
        public string bulletType;
        public GameObject prefab;
    }

    [SerializeField] private BulletPrefab[] bulletPrefabs;
    [SerializeField] private int poolSize = 10;

    // Diccionarios para gestionar el pool
    private Dictionary<string, List<GameObject>> bulletPools;
    private Dictionary<string, GameObject> prototypeMap;
    private Transform container;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[BulletPool] Múltiples instancias detectadas.");

        Instance = this;
        bulletPools = new Dictionary<string, List<GameObject>>();
        prototypeMap = new Dictionary<string, GameObject>();
        container = this.transform;

        foreach (var bulletPrefab in bulletPrefabs)
        {
            if (bulletPrefab.prefab == null || string.IsNullOrWhiteSpace(bulletPrefab.bulletType))
            {
                Debug.LogWarning("[BulletPool] Prefab o tipo de bala inválido.");
                continue;
            }

            string key = NormalizeKey(bulletPrefab.bulletType);

            if (bulletPools.ContainsKey(key))
            {
                Debug.LogWarning($"[BulletPool] Ya existe una entrada para '{bulletPrefab.bulletType}'.");
                continue;
            }

            prototypeMap[key] = bulletPrefab.prefab;
            var list = new List<GameObject>(poolSize);
            for (int i = 0; i < poolSize; i++)
            {
                GameObject bullet = Instantiate(bulletPrefab.prefab, container);
                bullet.SetActive(false);
                list.Add(bullet);
            }
            bulletPools[key] = list;
        }
    }

    // Obtener una bala del pool (no sensible a mayúsculas)
    public GameObject GetBullet(string bulletType)
    {
        if (string.IsNullOrWhiteSpace(bulletType))
        {
            Debug.LogWarning("[BulletPool] Tipo de bala vacío.");
            return null;
        }

        string key = NormalizeKey(bulletType);

        if (!bulletPools.ContainsKey(key))
        {
            Debug.LogWarning($"[BulletPool] Tipo '{bulletType}' no encontrado.");
            return null;
        }

        foreach (var bullet in bulletPools[key])
        {
            if (!bullet.activeInHierarchy)
            {
                bullet.SetActive(true);
                if (bullet.TryGetComponent<Rigidbody2D>(out var rb2d)) rb2d.linearVelocity = Vector2.zero;
                if (bullet.TryGetComponent<Rigidbody>(out var rb3d)) rb3d.linearVelocity = Vector3.zero;

                Debug.Log($"[BulletPool] Reusando bala '{bulletType}' del pool.");
                return bullet;
            }
        }

        // Si no hay disponibles, crear una más
        if (prototypeMap.TryGetValue(key, out var proto) && proto != null)
        {
            GameObject extra = Instantiate(proto, container);
            extra.SetActive(true);
            bulletPools[key].Add(extra);

            if (extra.TryGetComponent<Rigidbody2D>(out var rb2)) rb2.linearVelocity = Vector2.zero;
            if (extra.TryGetComponent<Rigidbody>(out var rb3)) rb3.linearVelocity = Vector3.zero;

            Debug.LogWarning($"[BulletPool] Pool agotado para '{bulletType}'. Creando extra.");
            return extra;
        }

        Debug.LogWarning($"[BulletPool] No se encontró prototipo para '{bulletType}'.");
        return null;
    }

    // Devolver la bala al pool
    public void ReturnBullet(string bulletType, GameObject bullet)
    {
        if (bullet == null) return;

        string key = NormalizeKey(bulletType);
        if (bulletPools.ContainsKey(key))
        {
            if (bullet.TryGetComponent<Rigidbody2D>(out var rb2d)) rb2d.linearVelocity = Vector2.zero;
            if (bullet.TryGetComponent<Rigidbody>(out var rb3d)) rb3d.linearVelocity = Vector3.zero;

            bullet.SetActive(false);
            bullet.transform.SetParent(container);

            Debug.Log($"[BulletPool] Bala '{bulletType}' devuelta al pool.");
        }
        else
        {
            Debug.LogWarning($"[BulletPool] Tipo '{bulletType}' no encontrado. Destruyendo objeto.");
            Destroy(bullet);
        }
    }

    private string NormalizeKey(string s) => s.Trim().ToLowerInvariant();
}