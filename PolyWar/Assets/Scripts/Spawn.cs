using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawn : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject enems;
    public GameObject Player;
    public Transform[] spawn;
    [Range(5f,0.001f)]
    public float timerangemax = 3f;
    [Range(1f, 10f)]
    int randomspwan;
    public static bool playerlive;
    
    void Start()
    {
        playerlive = true;
        StartCoroutine(SpawnRandomEnemy());
        
    }
   
    private IEnumerator SpawnRandomEnemy()
    {
        while(playerlive)       // untill player is alive enemy keeps spawning
        {
            randomspwan = Random.Range(0, spawn.Length);
            Debug.Log(randomspwan);
            Instantiate(enems, spawn[randomspwan].position, Quaternion.identity);
            yield return new WaitForSeconds(timerangemax);
        }

    }
    

}
