using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TwoDDisplay : MonoBehaviour {


    [SerializeField] private Slider m_mainSlider;
    [SerializeField] private GameObject m_sliderCanvas;

    [SerializeField] private DataContainer data;

    [SerializeField] private ImageSegmentationHandler2 m_segmentationHandler;

    [SerializeField] private HandDepthChooserScript m_handDepthChooserScript;

    private Texture2D displayedTexture;

    private int guiWidth;
    private int guiHeight;

    private bool drawGUI;

    // Use this for initialization
    void Start () {
        drawGUI = true;
        disableTwoDDisplay();
        m_mainSlider.onValueChanged.AddListener( delegate { ValueChangeCheck(); } );
    }
    
    void OnGUI() {
        if( drawGUI ) {
            guiWidth = Mathf.Max(Screen.height - 100, data.getWidth());
            guiHeight = Mathf.Max(Screen.height - 100, data.getHeight());
            GUI.DrawTexture(new Rect(0, 0, guiWidth, guiHeight), displayedTexture, ScaleMode.ScaleToFit, true, 1.0F);
        }
    }

    // Update is called once per frame
    void Update () {
        // if left or right mouse button pressed...
        if( (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && drawGUI ) { 
            float x = (guiWidth - Input.mousePosition.x) / guiWidth;
            float y = (Screen.height - Input.mousePosition.y + 3) / guiHeight;
            if (Input.GetMouseButtonDown(0)) {
                m_segmentationHandler.AddSeedTwoD( x, y, data.getSelectedLayer(m_mainSlider.value), true );
            }
            else {
                m_segmentationHandler.AddSeedTwoD( x, y, data.getSelectedLayer( m_mainSlider.value ), false );
            }
        }
        if( Input.GetKeyDown( "space" ) ) {
            //toggleTwoDDisplay();
            m_handDepthChooserScript.toggleCut();
        }
        if( Input.GetKeyDown( "f" ) ) {
            RegionGrowingButtonPressed();
        }
        if( Input.GetKeyDown( "s" ) ) {
            MaxFlowButtonPressed();
        }
    }

    public void disableTwoDDisplay() {
        if( drawGUI ) {
            toggleTwoDDisplay();
        }
    }
    public void enableTwoDDisplay() {
        if( !drawGUI ) {
            toggleTwoDDisplay();
        }
    }
    private void toggleTwoDDisplay() {
        drawGUI = !drawGUI;
        if( drawGUI ) {
            m_sliderCanvas.SetActive( true );
            ValueChangeCheck();
        }
        else {
            m_sliderCanvas.SetActive( false );
        }
    }

    public void ValueChangeCheck() {
        displayedTexture = data.getSelectedTexture(m_mainSlider.value);
    }

    public void RegionGrowingButtonPressed() {
        m_segmentationHandler.StartFloodFillSegmentationThread();
    }
    public void MaxFlowButtonPressed() {
        m_segmentationHandler.StartMaxFlowSegmentationThread();
    }
    public void SeedPresetOneButtonPressed() {
        m_segmentationHandler.SeedPresetOne();
    }
    public void ClearSeedsButtonPressed() {
        m_segmentationHandler.ClearSeeds();
    }
}
