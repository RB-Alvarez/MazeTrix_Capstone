using UnityEngine;
using UnityEngine.UI;

public class HungerBar : MonoBehaviour
{
    public Slider hungerSlider;

    float maxHunger = 100f;
    public float currentHunger;
    public float hungerDecreaseRate = 0.75f; // Hunger decrease rate per second

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHunger = maxHunger;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log($"Current Hunger: {currentHunger}"); // Debug log to check hunger value

        hungerSlider.value = currentHunger / maxHunger;

        if (currentHunger > 0)
            currentHunger -= hungerDecreaseRate * Time.deltaTime;
        else if (currentHunger <= 0)
        {
            Debug.Log("Player is starving!"); //change with HP bar decrease or death logic
        }
    }
}
