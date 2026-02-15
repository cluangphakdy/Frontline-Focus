using UnityEngine;

public class AlienSpawner : MonoBehaviour
{
    public GameObject[] alienPrefabs; // Slot 0: Green, 1: Yellow, 2: Blue
    public float spawnRate = 1.5f;
    public float spawnWidth = 15f;

    void Start()
    {
        InvokeRepeating("Spawn", 2f, spawnRate);
    }

    void Spawn()
    {
        // Random horizontal position
        Vector3 spawnPos = transform.position + new Vector3(Random.Range(-spawnWidth, spawnWidth), 0, Random.Range(-5, 5));
        
        // Logic to make Normal common and Rare... rare.
        float roll = Random.value;
        int index = (roll > 0.9f) ? 2 : (roll > 0.7f) ? 1 : 0;

        Instantiate(alienPrefabs[index], spawnPos, Quaternion.identity);
    }
}