using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(0)]
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance;
    public GameObject[] enemyPrefabs;
    public int poolSize = 10;

    private List<GameObject> pool;

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

        pool = new List<GameObject>();

        for (int i = 0; i < poolSize; i++)
        {
            foreach (var prefab in enemyPrefabs)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                pool.Add(obj);
            }
        }
    }

    public GameObject GetEnemy(GameObject prefab)
    {
        foreach (GameObject enemy in pool)
        {
            if (!enemy.activeInHierarchy && enemy.name.Contains(prefab.name))
            {
                return enemy;
            }
        }

        return null;
    }

    public void ReturnEnemy(GameObject enemy)
    {
        enemy.SetActive(false);
    }
}