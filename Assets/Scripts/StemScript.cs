using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StemScript : MonoBehaviour
{
    public GameObject fruitPrefab;

    FruitScript fruit;
    int spawnTimer;

    // Update is called once per frame
    void Update()
    {
        spawnTimer--;
        if (fruit == null && spawnTimer <= 0) {
            fruit = Instantiate(fruitPrefab).GetComponent<FruitScript>();
            fruit.Spawn(this);
        }
    }

    public void Pick() {
        fruit = null;
        spawnTimer = 240;
    }
}
