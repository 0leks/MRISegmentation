using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TwoDDisplay : MonoBehaviour {


    [SerializeField] private Slider m_mainSlider;

    [SerializeField]
    private DataContainer data;

    private Texture2D displayedTexture;

    private int guiWidth;
    private int guiHeight;

    // Use this for initialization
    void Start () {

        ValueChangeCheck();

        m_mainSlider.onValueChanged.AddListener(delegate { ValueChangeCheck(); });
    }
    
    void OnGUI() {
        guiWidth = Mathf.Max(Screen.height - 100, data.getWidth());
        guiHeight = Mathf.Max(Screen.height - 100, data.getHeight());
        GUI.DrawTexture(new Rect(0, 0, guiWidth, guiHeight), displayedTexture, ScaleMode.ScaleToFit, true, 1.0F);
    }

    // Update is called once per frame
    void Update () {
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))) { // if left button pressed...
            int x = (int)((guiWidth - Input.mousePosition.x) * data.getWidth() / guiWidth);
            int y = (int)((Screen.height - Input.mousePosition.y + 3) * data.getHeight() / guiHeight);
            if (x >= 0 && x < data.getWidth() && y >= 0 && y < data.getHeight()) {
                if (Input.GetMouseButtonDown(0)) {
                    Debug.LogErrorFormat("Added point to Object {0}, {1}", x, y);
                    partOfObject.Add(new Point(x, y, selectedScan, 0));
                }
                else {
                    Debug.LogErrorFormat("Adding point to Background {0}, {1}", x, y);
                    partOfBackground.Add(new Point(x, y, selectedScan, 0));
                }
            }
        }
    }


    public void ValueChangeCheck() {
        displayedTexture = data.getSelectedTexture(m_mainSlider.value);
    }
}
