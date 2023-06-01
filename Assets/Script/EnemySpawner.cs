using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
public class EnemySpawner : MonoBehaviour
{


    [Header("References")]
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Attribute")]
    [SerializeField] private int baseEmemies = 8;
    [SerializeField] private float enemiesPerSecond = 0.5f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float difficultyScalingFactor = 0.75f;
    [SerializeField] public int numsOfWave = 5; 

    [Header("Event")]
    public static UnityEvent onEnemyDestroy = new UnityEvent();


    private int currentWave = 1;
    private float timeSinceLastSpawn;
    private int enemiesAlive;
    private int enemiesLeftToSpawn;
    private bool isSpawning = false;

    private void Awake()
    {
        onEnemyDestroy.AddListener(EnemyDestroy);
    }

    private void Start()
    {
        StartCoroutine(StartWave());
    }

    private void Update()
    {
        if (!isSpawning) return;
        timeSinceLastSpawn += Time.deltaTime;

        if(timeSinceLastSpawn >= (1f / enemiesPerSecond) && enemiesLeftToSpawn > 0)
        {
            SpawnEnemy();
            enemiesLeftToSpawn--;
            enemiesAlive++;
            timeSinceLastSpawn = 0f;
        }

        if(enemiesAlive == 0 && enemiesLeftToSpawn == 0)
        {
            EndWave();
            numsOfWave--;
        }
    }

    private void EndWave()
    {
        isSpawning = false;
        timeSinceLastSpawn = 0f;
        currentWave++;
        StartCoroutine(StartWave());
    }

    private void EnemyDestroy()
    {
        enemiesAlive--;
    }

    private IEnumerator StartWave()
    {
        if(numsOfWave > 0)
        {
            yield return new WaitForSeconds(timeBetweenWaves);
            isSpawning = true;
            enemiesLeftToSpawn = EnemiesPerWave();
        }
        
    }

    private void SpawnEnemy()
    {
        int rand = UnityEngine.Random.Range(0, enemyPrefabs.Length);
        GameObject prefabToSpawn = enemyPrefabs[0];
        Instantiate(prefabToSpawn, transform.position, Quaternion.identity);

    }

    private int EnemiesPerWave()
    {
        return Mathf.RoundToInt(baseEmemies * Mathf.Pow(currentWave, difficultyScalingFactor));
    }

}
