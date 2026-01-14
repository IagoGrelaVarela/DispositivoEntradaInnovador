using UnityEngine;

[System.Serializable]
public class SpellConfig
{
    public string spellName;
    public Color color = Color.white;
    public float damageMultiplier = 1f;
    public float speed = 8f;
    public float baseDamage = 10f;
}

public class SpellProjectile : MonoBehaviour
{
    [Tooltip("Configuración específica de este prefab")]
    [SerializeField] private SpellConfig spellConfig;

    private Transform target;
    private float power;
    private float speed;
    private float damage;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer != null && spellConfig != null)
            spriteRenderer.color = spellConfig.color;
    }

    void OnEnable()
    {
        // Limpiar velocidad previa
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // Configurar el proyectil (se usará desde el pool)
    public void Setup(Transform targetEnemy, float powerLevel, float baseProjectileSpeed)
    {
        target = targetEnemy;
        power = Mathf.Clamp01(powerLevel);

        if (spellConfig != null)
        {
            speed = spellConfig.speed > 0f ? spellConfig.speed : baseProjectileSpeed;
            damage = spellConfig.baseDamage * spellConfig.damageMultiplier * (1f + power);

            if (spriteRenderer != null)
                spriteRenderer.color = spellConfig.color;
        }
        else
        {
            speed = baseProjectileSpeed;
            damage = 10f * (1f + power);
        }

        // Ajustar escala si es un cast fuerte
        transform.localScale = Vector3.one * (1f + power * 0.25f);

        // Inicializar velocidad hacia el target
        UpdateVelocityTowardsTarget();
    }

    void Update()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            UpdateVelocityTowardsTarget();
        }
        else
        {
            // Target muerto -> devolver al pool
            ReturnToPool();
        }
    }

    void UpdateVelocityTowardsTarget()
    {
        if (rb == null || target == null) return;
        Vector2 dir = (target.position - transform.position).normalized;
        rb.linearVelocity = dir * speed;
        // Rotar sprite para mirar hacia la dirección
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            int damageToApply = Mathf.RoundToInt(damage);
            enemy.TakeDamage(damageToApply);
        }

        ReturnToPool();
    }

    void ReturnToPool()
    {
        // Reset física
        if (rb != null) rb.linearVelocity = Vector2.zero;
        // Devolver al pool usando la key del spellConfig
        string key = spellConfig != null && !string.IsNullOrEmpty(spellConfig.spellName) ? spellConfig.spellName : "bullet";
        if (BulletPool.Instance != null)
            BulletPool.Instance.ReturnBullet(key, gameObject);
        else
            gameObject.SetActive(false);
    }
}