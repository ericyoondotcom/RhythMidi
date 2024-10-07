using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteSprite : MonoBehaviour
{
    public RectTransform startPosition;
    public RectTransform endPosition;
    public float totalTime;
    float t = 0;
    RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        rt.anchorMin = startPosition.anchorMin;
        rt.anchorMax = startPosition.anchorMax;
    }
    void Update()
    {
        t += Time.deltaTime;
        rt.anchoredPosition = Vector3.Lerp(startPosition.anchoredPosition, endPosition.anchoredPosition, t / totalTime);
        if(t >= totalTime && totalTime > 0)
        {
            Destroy(gameObject);
        }
    }
}
