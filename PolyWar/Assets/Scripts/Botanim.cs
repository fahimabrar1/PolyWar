using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(UnityEngine.AI.NavMesh))]
public class Botanim : MonoBehaviour
{
    Camera camera;
    public GameObject player;
    CharacterController cc;
    float gravity=100f;
    Vector3 lim =new Vector3(0,2,0);
    Vector3 temp;
    // Start is called before the first frame update
    
    void Start()
    {
        cc = GetComponent<CharacterController>();
        camera = Camera.main;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(Vector3.forward);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(Vector3.back);
        }
        else if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(Vector3.left);

        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(Vector3.right);

        }
        if (transform.position.y > lim.y)
        {
            
            transform.Translate(Vector3.down*Time.deltaTime*2);

        }

    }
        
}

