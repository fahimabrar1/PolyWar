using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMotion : MonoBehaviour
{
    private CharacterController character;
    private float gravity = 30f;
    private float playergrav;
    Vector3 movevec = Vector3.zero;


    [Range(1f, 10f)]
    public float speed = 5f;
    private GameObject findplayer;
    void Start()
    {
        character = GetComponent<CharacterController>();
        findplayer = GameObject.FindGameObjectWithTag("Player");
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!character.isGrounded)
        {
            playergrav = -gravity * Time.deltaTime;
        }
        else
        {
            playergrav -= gravity * Time.deltaTime;

        }

        movevec = new Vector3(0, playergrav, 0);
        character.Move(movevec * Time.deltaTime);

        if (Spawn.playerlive)
        {
            transform.LookAt(findplayer.transform.position);
        }
    }
}
