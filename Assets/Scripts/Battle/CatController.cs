using UnityEngine;

public class CatController : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private float baseFireRate = 1f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private AudioSource scratchSfx;

    [SerializeField] private GameObject spellFirePrefab;
    [SerializeField] private GameObject spellIcePrefab;
    [SerializeField] private GameObject spellWindPrefab;

    public float range = 3.5f;

    private Animator animator;
    private float fireRate;
    private float nextFireTime;
    private Transform target;
    private Transform finalPoint;
    private bool isAttacking = false;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        fireRate = baseFireRate;
        nextFireTime = Time.time;

        if (Path.points != null && Path.points.Length > 0)
            finalPoint = Path.points[Path.points.Length - 1];

        if (CompareTag("Pelusa") && animator != null)
            animator.Play("C_Idle");
    }

    void Update()
    {
        FindTarget();

        if (CompareTag("Pelusa"))
            scratchSfx.pitch = Random.Range(0.55f, 0.95f);
        else
            scratchSfx.pitch = Random.Range(1.05f, 1.45f);

        if (target != null)
        {
            isAttacking = true;
            if (Time.time >= nextFireTime)
            {
                Fire(target);
                nextFireTime = Time.time + 1f / fireRate;
            }
        }
        else
        {
            isAttacking = false;
        }

        UpdateAnimatorState();
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

    private void UpdateAnimatorState()
    {
        if (animator != null)
            animator.SetBool("Attacking", isAttacking);
    }

    private void Fire(Transform target)
    {
        if (BulletPool.Instance == null)
        {
            Debug.LogWarning("[CatController] BulletPool.Instance es null.");
        }
        else
        {
            GameObject bulletObject = BulletPool.Instance.GetBullet("scratch");
            if (bulletObject == null)
            {
                Debug.LogWarning("[CatController] No se pudo obtener bala del pool.");
            }
            else
            {
                scratchSfx?.Play();

                bulletObject.transform.position = firePoint.position;
                bulletObject.transform.rotation = firePoint.rotation;

                Bullet bulletComponent = bulletObject.GetComponent<Bullet>();
                if (bulletComponent != null)
                    bulletComponent.Setup(target, bulletSpeed);

                return;
            }
        }

        Debug.LogWarning("[CatController] Fallback: no se pudo obtener bala del pool.");
    }

    // Lanzar un hechizo desde esta torre
    public void CastSpell(string spellName, float power)
    {
        if (target == null)
        {
            Debug.Log($"[CatController] No hay enemigo en rango para {spellName}");
            return;
        }

        if (BulletPool.Instance == null)
        {
            Debug.LogWarning("[CatController] BulletPool.Instance es null.");
        }
        else
        {
            GameObject pooled = BulletPool.Instance.GetBullet(spellName);
            if (pooled != null)
            {
                pooled.transform.position = firePoint.position;
                pooled.SetActive(true);
                SpellProjectile projectile = pooled.GetComponent<SpellProjectile>();
                if (projectile != null)
                {
                    projectile.Setup(target, power, bulletSpeed);
                    return;
                }
                else
                {
                    Debug.LogError($"[CatController] Prefab en pool '{spellName}' no tiene SpellProjectile");
                    pooled.SetActive(false);
                }
            }
            else
            {
                Debug.LogWarning($"[CatController] BulletPool.GetBullet('{spellName}') devolvió null.");
            }
        }

        // Fallback: instanciar prefab si no hay pool
        GameObject spellPrefab = GetSpellPrefab(spellName);
        if (spellPrefab == null)
        {
            Debug.LogError($"[CatController] No hay prefab asignado para: {spellName}");
            return;
        }

        GameObject spellObject = Instantiate(spellPrefab, firePoint.position, Quaternion.identity);
        SpellProjectile projectileNew = spellObject.GetComponent<SpellProjectile>();
        if (projectileNew != null)
            projectileNew.Setup(target, power, bulletSpeed);
        else
            Destroy(spellObject);
    }

    private GameObject GetSpellPrefab(string spellName)
    {
        return spellName switch
        {
            "Fire" => spellFirePrefab,
            "Ice" => spellIcePrefab,
            "Wind" => spellWindPrefab,
            _ => null
        };
    }
}