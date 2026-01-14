using UnityEngine;

public class TowerPrefab : MonoBehaviour
{
    [SerializeField] Transform firePoint;
    [SerializeField] protected float fireRate = 0.5f;
    [SerializeField] float bulletSpeed = 10f;
    [SerializeField] private AudioSource bulletSfx;

    public float range = 4.5f;
    [SerializeField] GameObject rangeIndicator;
    [SerializeField] Canvas towerUI;
    private float spriteDiameter = 44f; // Diámetro del rango en píxeles
    private float PPU = 16f; // Pixels Per Unit configurado en Unity
    public int upgradeCost = 20;
    public int sellValue = 15;

    private Animator animator;
    private float nextFireTime;
    private Transform target;
    private Transform finalPoint;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        nextFireTime = Time.time;
        if (rangeIndicator != null)
        {
            towerUI.enabled = false;
            rangeIndicator.SetActive(false);
            UpdateRangeIndicator();
        }
        finalPoint = Path.points[Path.points.Length - 1]; // El último punto del camino
    }

    void Update()
    {
        bulletSfx.pitch = Random.Range(0.95f, 1.25f);
        FindTarget();

        if (target != null)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            UpdateAnimatorParameters(direction);

            if (Time.time >= nextFireTime)
            {
                Fire(target);
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
    }

    void FindTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        target = null;
        float closestDistanceToFinalPoint = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distanceToFinalPoint = Vector3.Distance(enemy.transform.position, finalPoint.position);
            float distanceToTower = Vector3.Distance(transform.position, enemy.transform.position);

            if (distanceToTower <= range && distanceToFinalPoint < closestDistanceToFinalPoint)
            {
                closestDistanceToFinalPoint = distanceToFinalPoint;
                target = enemy.transform;
            }
        }
    }

    void UpdateAnimatorParameters(Vector2 direction)
    {
        animator.SetFloat("moveX", direction.x);
        animator.SetFloat("moveY", direction.y);
    }

    void Fire(Transform target)
    {
        GameObject bulletObject = BulletPool.Instance.GetBullet("bullet");
        bulletSfx.Play();

        if (bulletObject != null)
        {
            bulletObject.transform.position = firePoint.position;

            Bullet bulletComponent = bulletObject.GetComponent<Bullet>();
            if (bulletComponent != null)
            {
                bulletComponent.Setup(target, bulletSpeed);
            }

            bulletObject.SetActive(true);
        }
    }

    public void ShowRangeIndicator(bool show)
    {
        if (rangeIndicator != null)
        {
            rangeIndicator.SetActive(show);
        }
    }

    public void ShowUI(bool show)
    {
        if (towerUI != null)
        {
            towerUI.enabled = show;
        }
    }

    void UpdateRangeIndicator()
    {
        if (rangeIndicator != null)
        {
            // La escala del indicador debe ser el doble del rango
            float scale = (range * 2f) / (spriteDiameter / PPU);
            rangeIndicator.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void UpgradeTower()
    {
        if (BattleManager.Instance.money >= upgradeCost)
        {
            BattleManager.Instance.AddMoney(-upgradeCost);
            range += 2f; // Aumentar rango
            fireRate += 1f; // Aumentar tasa de disparo
            UpdateRangeIndicator();
            upgradeCost += 30;
            BattleManager.Instance.UpdatePriceText(upgradeCost);
        }
        else
        {
            Debug.Log("No hay suficiente dinero para mejorar la torre.");
        }
    }

    public void SellTower()
    {
        Vector3Int cellPosition = GridManager.Instance.grid.WorldToCell(transform.position);
        GridManager gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            BattleManager.Instance.AddMoney(sellValue);
            gridManager.SellTower(cellPosition);
        }
    }
}