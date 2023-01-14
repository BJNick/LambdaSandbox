using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickablePause : MonoBehaviour
{
    bool isPaused = false;

    public bool startPaused = false;

    public Sprite pauseSprite;
    public Sprite playSprite;

    // Start is called before the first frame update
    void Start()
    {
        if (startPaused) OnMouseDown();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnMouseDown() {
        // Pause/unpause all physics
        Debug.Log("Clicked");
        isPaused = !isPaused;
        if (isPaused) {
            Time.timeScale = 0;
            GetComponent<SpriteRenderer>().sprite = playSprite;
        } else {
            Time.timeScale = 1;
            GetComponent<SpriteRenderer>().sprite = pauseSprite;
        }
    }


}
