using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Transform target;
    private float speed;
    private Rigidbody2D rb;
    private Transform spriteTransform; // Referencia al sprite hijo

    // Configurar la bala antes de disparar
    public void Setup(Transform target, float speed)
    {
        this.target = target;
        this.speed = speed;
        rb = GetComponent<Rigidbody2D>();
        spriteTransform = transform.GetChild(0); // El sprite es el primer hijo
    }

    void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
            return;
        }

        // Dirección hacia el objetivo
        Vector2 direction = (target.position - new Vector3(0, 0.5f, 0) - transform.position).normalized;
        rb.linearVelocity = direction * speed;

        // Rotar el sprite para que apunte en la dirección de movimiento
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        spriteTransform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (target != null && target.GetComponent<Enemy>() != null)
        {
            target.GetComponent<Enemy>().TakeDamage(1);
        }

        gameObject.SetActive(false);
    }
}