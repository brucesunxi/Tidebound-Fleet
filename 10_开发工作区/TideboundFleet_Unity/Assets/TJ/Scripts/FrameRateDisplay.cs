using UnityEngine;
using TMPro;

public class FrameRateDisplay : MonoBehaviour
{
    public bool showFramerate = true;
    public float updateInterval = 0.5f;

    private float lastInterval;
    private int frames = 0;
    private float framesPerSecond;

    [SerializeField] private TextMeshProUGUI textMeshPro;

    void Start()
    {
        lastInterval = Time.realtimeSinceStartup;
        frames = 0;

        // Assuming the TextMeshPro component is on the same GameObject
        //textMeshPro = GetComponent<TextMeshProUGUI>();
        //if (textMeshPro == null)
        //{
        //    Debug.LogError("TextMeshProUGUI component not found. Make sure it's attached to the same GameObject.");
        //    enabled = false; // Disable the script to avoid errors
        //}
    }

    void Update()
    {
        frames++;

        float timeNow = Time.realtimeSinceStartup;
        if (timeNow > lastInterval + updateInterval)
        {
            framesPerSecond = frames / (timeNow - lastInterval);
            frames = 0;
            lastInterval = timeNow;

            // Update the TextMeshPro text
            if (showFramerate && textMeshPro != null)
            {
                textMeshPro.text = "FPS: " + framesPerSecond.ToString("f2");
            }
        }
    }
}