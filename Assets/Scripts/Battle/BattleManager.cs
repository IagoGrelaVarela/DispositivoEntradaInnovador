using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Collections;

[System.Serializable]
public class SubWaveElement
{
    public GameObject enemyPrefab;
    public int count;
    public float spawnRate;
}

[System.Serializable]
public class WaveElement
{
    public SubWaveElement[] subWaves;
    public float timeBetweenSubWaves = 20f;
}

[System.Serializable]
public class Wave
{
    public WaveElement[] elements;
}

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    [SerializeField] Transform spawnPoint;

    [SerializeField] TextMeshProUGUI moneyText;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] TextMeshProUGUI livesText;
    [SerializeField] GameObject wavePanel;
    [SerializeField] TextMeshProUGUI waveMessageText;
    [SerializeField] GameObject waveStartButton;

    public int money = 0;
    [SerializeField] int lives = 20;

    [Header("Audio")]
    [SerializeField] private AudioClip lifeLostClip;
    AudioSource sfxSource;

    private int currentWaveIndex = 0;
    private bool waveInProgress = false;
    private List<GameObject> activeEnemies = new List<GameObject>();

    private List<Wave> waves = new List<Wave>();

    private GridManager gridManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        gridManager = FindFirstObjectByType<GridManager>();

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        UpdateMoneyText();
        UpdateLivesText();
        InitializeWaves();
        InitializeWavePanel("Presiona Start para comenzar", true);
    }

    public void DestroySelf()
    {
        Instance = null;
        Destroy(gameObject);
    }

    void InitializeWaves()
    {
        // Configurar las oleadas de enemigos manualmente
        Wave wave0 = new Wave
        {
            elements = new WaveElement[]
            {
                new WaveElement
                {
                    subWaves = new SubWaveElement[]
                    {
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[0], count = 5, spawnRate = 1f },
                    },
                    timeBetweenSubWaves = 10f
                }
            }
        };

        Wave wave1 = new Wave
        {
            elements = new WaveElement[]
            {
                new WaveElement
                {
                    subWaves = new SubWaveElement[]
                    {
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[0], count = 5, spawnRate = 1f },
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[1], count = 3, spawnRate = 0.5f }
                    },
                    timeBetweenSubWaves = 15f
                }
            }
        };

        Wave wave2 = new Wave
        {
            elements = new WaveElement[]
            {
                new WaveElement
                {
                    subWaves = new SubWaveElement[]
                    {
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[1], count = 4, spawnRate = 1f },
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[0], count = 6, spawnRate = 0.9f },
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[3], count = 2, spawnRate = 0.75f },
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[2], count = 2, spawnRate = 0.25f }
                    },
                    timeBetweenSubWaves = 15f
                }
            }
        };

        Wave wave3 = new Wave
        {
            elements = new WaveElement[]
            {
                new WaveElement
                {
                    subWaves = new SubWaveElement[]
                    {
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[1], count = 6, spawnRate = 1.5f },
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[0], count = 4, spawnRate = 1f },
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[3], count = 4, spawnRate = 0.75f },
                        new SubWaveElement { enemyPrefab = EnemyPool.Instance.enemyPrefabs[4], count = 1, spawnRate = 0.1f },
                    },
                    timeBetweenSubWaves = 15f
                }
            }
        };

        waves.Add(wave0);
        waves.Add(wave1);
        waves.Add(wave2);
        waves.Add(wave3);
    }

    public void StartGame()
    {
        if (!waveInProgress)
        {
            wavePanel.SetActive(false);
            gridManager.SetInteractionEnabled(true);
            StartCoroutine(SpawnWave());
        }
    }

    public void StartNextWave()
    {
        if (!waveInProgress)
        {
            wavePanel.SetActive(false);
            gridManager.SetInteractionEnabled(true);
            StartCoroutine(SpawnWave());
        }
    }

    IEnumerator SpawnWave()
    {
        waveInProgress = true;

        Wave currentWave = waves[currentWaveIndex];

        foreach (WaveElement element in currentWave.elements)
        {
            yield return StartCoroutine(SpawnSubWaves(element));
        }

        // Esperar hasta que no queden enemigos activos
        yield return new WaitUntil(() => activeEnemies.Count == 0);

        waveInProgress = false;
        currentWaveIndex++;

        if (currentWaveIndex < waves.Count)
        {
            waveMessageText.text = "Wave " + (currentWaveIndex + 1) + " Completed";
            InitializeWavePanel("Presiona Start para la siguiente oleada", true);
        }
        else
        {
            waveMessageText.text = "All Waves Completed!";
            InitializeWavePanel("Juego Completado", false);
        }

        gridManager.SetInteractionEnabled(false);
    }

    IEnumerator SpawnSubWaves(WaveElement element)
    {
        foreach (SubWaveElement subWave in element.subWaves)
        {
            for (int i = 0; i < subWave.count; i++)
            {
                Vector3 spawnPosition = spawnPoint.position;

                GameObject enemy = EnemyPool.Instance.GetEnemy(subWave.enemyPrefab);
                if (enemy != null)
                {
                    enemy.transform.position = spawnPosition;
                    enemy.SetActive(true);
                    activeEnemies.Add(enemy);

                    Enemy enemyScript = enemy.GetComponent<Enemy>();
                    if (enemyScript != null)
                    {
                        enemyScript.OnDeath -= HandleEnemyDeath;
                        enemyScript.OnDeath += HandleEnemyDeath;
                    }
                }
                else
                {
                    Debug.LogWarning("No hay enemigos disponibles en el pool. Aumenta el tamaño del pool.");
                    yield break;
                }

                yield return new WaitForSeconds(1f / subWave.spawnRate);
            }

            yield return new WaitForSeconds(element.timeBetweenSubWaves);
        }
    }

    void HandleEnemyDeath(GameObject enemy)
    {
        // Evitar duplicados en las suscripciones
        if (enemy != null)
        {
            Enemy e = enemy.GetComponent<Enemy>();
            if (e != null)
                e.OnDeath -= HandleEnemyDeath;
        }

        activeEnemies.Remove(enemy);
        EnemyPool.Instance.ReturnEnemy(enemy);
    }

    void Update()
    {
        UpdateMoneyText();
        UpdateLivesText();
        if (!waveInProgress)
        {
            gridManager.SetInteractionEnabled(true);
        }
    }

    public void AddMoney(int amount)
    {
        money += amount;
    }

    private void UpdateMoneyText()
    {
        moneyText.text = "Monedas: " + money;
    }
    public void UpdatePriceText(int price)
    {
        priceText.text = "Torre: 50\r\nReembolso: 40\r\nMejora: " + price;
    }
    private void UpdateLivesText()
    {
        livesText.text = "Vidas: " + lives;
    }

    public void LoseLife(int amount)
    {
        lives -= amount;
        if (lives < 0) lives = 0;
        UpdateLivesText();

        Debug.Log($"[BattleManager] Vida perdida: {amount}. Vidas restantes: {lives}");

        sfxSource.PlayOneShot(lifeLostClip);

        if (lives <= 0)
        {
            waveInProgress = false;
            gridManager.SetInteractionEnabled(false);
            InitializeWavePanel("Juego Finalizado", false);
        }
    }

    void InitializeWavePanel(string message, bool showButton)
    {
        waveMessageText.text = message;
        waveStartButton.SetActive(showButton);
        wavePanel.SetActive(true);
    }
}