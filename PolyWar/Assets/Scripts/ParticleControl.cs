using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleControl : MonoBehaviour
{
    public GameObject Player;
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(control.playerdead)
        {
            ps.Play();
            control.playerdead = false;
        }
        else if(Spawn.playerlive)
        {
            transform.position = Player.transform.position;
        }
    }
}
