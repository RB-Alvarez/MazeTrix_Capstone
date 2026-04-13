using UnityEngine;
using UnityEngine.UI;

public class XPBar : MonoBehaviour
{
    public Slider XPSlider;

    private void Awake()
    {
        if (XPSlider == null)
        {
            XPSlider = GetComponent<Slider>();
        }
    }

    private void Start()
    {
        UpdateBar();
    }

    public void UpdateBar()
    {
        if (PlayerXP.Instance == null || XPSlider == null) return;

        int current = PlayerXP.Instance.CurrentXP;
        int max = PlayerXP.Instance.XPToNextLevel;

        XPSlider.value = max > 0 ? (float)current / max : 0f;
    }
}
