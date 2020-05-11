using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    Cinemachine.CinemachineVirtualCamera cm;
    public GameObject player;
    Vector3 pos,cpos;
    float x, z;
    Camera camera;

    void Start()
    {
        
        camera = GetComponent<Camera>();
        pos = new Vector3(0,18,0);
        Debug.Log(pos.y);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
         cm.transform.position = pos;
          camera.transform.position = pos;
     //   pos = player.transform.position;
     //   pos = player.transform.position- camera.transform.position;
     //   pos.y = 16;
     //   camera.transform.position = pos;
    }
}
