using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyTimer : MonoBehaviour
{
    public float timeToDestroy = 0.3f;

    float t = 0;
    Vector3 startingSize;
    
    void Start()
    {
        startingSize = transform.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        transform.localScale = Vector3.Lerp(startingSize, Vector3.zero, t / timeToDestroy);
        if(t >= timeToDestroy) Destroy(gameObject);
    }
}
