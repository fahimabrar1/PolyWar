using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMotion : MonoBehaviour
{
    private CharacterController character;
    private float gravity =30f;
    private float playergrav;
    private Animator animator;
    private Transform playertransform;
    private Quaternion pr;
    Vector3 movevec = Vector3.zero;
   


    void Start()
    {
        character = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playertransform = GetComponent<Transform>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(!character.isGrounded)
        {
            playergrav = - gravity * Time.deltaTime;
        }
        else
        {
            playergrav -= gravity * Time.deltaTime;

        }

        movevec = new Vector3(0, playergrav, 0);
        character.Move(movevec*Time.deltaTime);

        if (Input.GetKey(KeyCode.W))
        {
            animator.SetBool("Run", true);
            pr.eulerAngles = new Vector3(0, 0, 0);
            playertransform.rotation = pr;
            
        }
        else if (Input.GetKey(KeyCode.S))
        {
            animator.SetBool("Run", true);
            pr.eulerAngles = new Vector3(0, 180, 0);
            playertransform.rotation = pr;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            animator.SetBool("Run", true);
            pr.eulerAngles = new Vector3(0, -90, 0);
            playertransform.rotation = pr;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            animator.SetBool("Run", true);
            pr.eulerAngles = new Vector3(0, 90, 0);
            playertransform.rotation = pr;
        }else
        {
            animator.SetBool("Run", false);
        }

    }
}
