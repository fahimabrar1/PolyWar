using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFollowPlayer : MonoBehaviour
{
    [Range(1f,10f)]
    public float speed = 5f;
    private GameObject findplayer;
    // Start is called before the first frame update
    void Start()
    {
        findplayer = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(Spawn.playerlive)
        {
             transform.position = Vector3.MoveTowards(transform.position, findplayer.transform.position, speed * Time.deltaTime);
        }
    }
    

}
