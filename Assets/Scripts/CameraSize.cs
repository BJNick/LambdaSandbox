using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSize : MonoBehaviour
{
    private Camera cam;

    public float scale = 6.75f;

    public float aspectRatio = 16f / 9f;

    // Start is called before the first frame update
    void Start()
    {
        cam = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        // Keep width consistent
        cam.orthographicSize = scale * aspectRatio / cam.aspect;
    }
}
