using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
    public float timeToDestroy = 0.3f;

    float t = 0;
    
    void Update()
    {
        t += Time.deltaTime;
        if(t >= timeToDestroy) Destroy(gameObject);
    }
}
