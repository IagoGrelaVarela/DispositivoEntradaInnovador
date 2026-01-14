using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] int maxHealth = 50;
    [SerializeField] int value = 10;
    [SerializeField] float moveInterval = 0.5f;
    [SerializeField] float moveSpeed = 1f;

    private int currentHealth;
    private Transform target;
    private int targetIndex = 0;
    private BattleManager battleManager;

    public delegate void DeathHandler(GameObject enemy);
    public event DeathHandler OnDeath;

    void Awake()
    {
        battleManager = FindFirstObjectByType<BattleManager>();
    }

    void OnEnable()
    {
        ResetHealth();
        InitializeMovement();
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    void InitializeMovement()
    {
        if (Path.points == null || Path.points.Length == 0)
        {
            Debug.LogError("[Enemy] Path points no inicializados.");
            return;
        }

        targetIndex = 0;
        target = Path.points[targetIndex];
        StartCoroutine(MoveTowardsTarget());
    }

    IEnumerator MoveTowardsTarget()
    {
        const float arriveThreshold = 0.05f;

        while (true)
        {
            if (target == null)
                yield break;

            float stepDistance = moveSpeed * Mathf.Max(0.0001f, moveInterval);
            float dist = Vector3.Distance(transform.position, target.position);

            if (dist <= arriveThreshold)
            {
                if (targetIndex >= Path.points.Length - 1)
                {
                    // Llegó al final: restar vida
                    if (battleManager != null)
                    {
                        battleManager.LoseLife(1);
                    }

                    OnDeath?.Invoke(gameObject);
                    yield break;
                }

                targetIndex++;
                target = Path.points[targetIndex];
                yield return null;
                continue;
            }

            if (stepDistance >= dist)
            {
                transform.position = target.position;
            }
            else
            {
                Vector3 dir = (target.position - transform.position).normalized;
                transform.position += dir * stepDistance;
            }

            yield return new WaitForSeconds(moveInterval);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log($"[Enemy] Recibió {damageAmount} de daño. Salud: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (battleManager != null)
        {
            battleManager.AddMoney(value);
        }

        OnDeath?.Invoke(gameObject);
    }
}