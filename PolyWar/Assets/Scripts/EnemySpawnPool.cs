using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPool : MonoBehaviour
{
    [SerializeField]
    private NewEnemyFollower enemyprefab;
    [SerializeField]
    public static EnemySpawnPool instance;
    public GameObject spawn;
    public static bool playerlive=true;
    public Queue<NewEnemyFollower> enemyMotions = new Queue<NewEnemyFollower>();
    
   
    // Start is called before the first frame update
    void Start()
    {
        playerlive = true;
        var obj = Instantiate(enemyprefab);
        obj.gameObject.SetActive(true);
        enemyMotions.Enqueue(obj);
        for (int a = 1; a <= 10; a++)
        {
            obj = Instantiate(enemyprefab,spawn.transform.position,Quaternion.identity);
            obj.gameObject.SetActive(true);
            enemyMotions.Enqueue(obj);
        }
    }
    private void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public GameObject getgameobjetc()
    {
        var newobj = enemyMotions.Dequeue();
        newobj.gameObject.SetActive(true);
        return newobj.gameObject;
    }
    public void returngameobject()
    {
        var obj = enemyprefab;
        enemyMotions.Enqueue(obj);
        obj.gameObject.SetActive(false);
    }
    
}
