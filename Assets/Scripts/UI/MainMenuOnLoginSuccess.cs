using UnityEngine;

public class MainMenuOnLoginSuccess : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowMenuTransitionButton()
    {
        gameObject.SetActive(true);
    }

}
