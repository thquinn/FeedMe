using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StemScript : MonoBehaviour
{
    static int BASE_TIMER = 30 * 60;
    static int TIMER_INCREMENT = 15 * 60;

    public GameObject fruitPrefab;

    public float spawnChance = 1;
    public bool randomRotation;

    FruitColor color;
    FruitScript fruit;
    int numSpawned = 0;
    int spawnTimer;

    void Start() {
        if (Random.value > spawnChance) {
            Destroy(gameObject);
            return;
        }
        if (randomRotation) {
            transform.Rotate(Vector3.up, Random.Range(0, 360f), Space.Self);
        }
        color = (FruitColor)Random.Range(1, System.Enum.GetNames(typeof(FruitColor)).Length);
    }
    void Update()
    {
        if (spawnTimer > 0) {
            spawnTimer--;
        }else if (fruit == null) {
            fruit = Instantiate(fruitPrefab).GetComponent<FruitScript>();
            fruit.Spawn(this, color);
            numSpawned++;
        }
    }

    public void Pick() {
        fruit = null;
        spawnTimer = BASE_TIMER + TIMER_INCREMENT * numSpawned;
    }
}
