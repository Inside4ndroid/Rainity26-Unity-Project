using UnityEngine;

public class FpsCounter : MonoBehaviour
{
    [Tooltip("How quickly the displayed FPS value smooths toward the real value.")]
    [Range(0.01f, 1f)]
    public float smoothing = 0.1f;

    [Tooltip("Font size for the FPS label.")]
    public int fontSize = 18;

    [Tooltip("Horizontal padding from the right edge of the screen, in pixels.")]
    public int paddingRight = 12;

    [Tooltip("Vertical padding from the top edge of the screen, in pixels.")]
    public int paddingTop = 8;

    private float _smoothedFps;
    private GUIStyle _style;

    private void Start()
    {
        _smoothedFps = 1f / Time.deltaTime;
    }

    private void Update()
    {
        float currentFps = 1f / Time.unscaledDeltaTime;
        _smoothedFps = Mathf.Lerp(_smoothedFps, currentFps, smoothing);
    }

    private void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize  = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperRight
            };
            _style.normal.textColor = Color.white;
        }

        string label = Mathf.RoundToInt(_smoothedFps) + " FPS";
        const int w = 100, h = 28;
        Rect rect = new Rect(Screen.width - w - paddingRight, paddingTop, w, h);

        // Dark shadow pass for legibility over any background.
        _style.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), label, _style);

        // Foreground — green above 50 fps, yellow above 30, red below.
        _style.normal.textColor = _smoothedFps >= 50f ? Color.green
                                : _smoothedFps >= 30f ? Color.yellow
                                : Color.red;
        GUI.Label(rect, label, _style);
    }
}
