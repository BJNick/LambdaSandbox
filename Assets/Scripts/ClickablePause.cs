using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickablePause : MonoBehaviour, IPointerClickHandler
{
    bool isPaused = false;

    public bool startPaused = false;

    public bool restartButton = false;

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
        if (Input.GetKeyDown(KeyCode.Space) && !restartButton) OnMouseDown();
    }

    private void OnMouseDown() {
        if (restartButton) {
            Time.timeScale = 1;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            return;
        }

        // Pause/unpause all physics
        isPaused = !isPaused;
        if (isPaused) {
            Time.timeScale = 0;
            try { GetComponent<SpriteRenderer>().sprite = playSprite; } catch {}
            try { GetComponent<Image>().sprite = playSprite; } catch {}
        } else {
            Time.timeScale = 1;
            try { GetComponent<SpriteRenderer>().sprite = pauseSprite; } catch {}
            try { GetComponent<Image>().sprite = pauseSprite; } catch {}
        }
    }

    public void OnPointerClick(PointerEventData eventData) {
        OnMouseDown();
    }


}
