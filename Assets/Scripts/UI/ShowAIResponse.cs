using UnityEngine;
using TMPro;
using System.Collections;

public class ShowAIResponse : MonoBehaviour
{
    public string aiResponse;
    
    [Header("Display Settings")]
    [Tooltip("Characters displayed per second")]
    public float charactersPerSecond = 30f;
    
    [Tooltip("Delay between sentences")]
    public float delayBetweenSentences = 0.8f;
    
    [Tooltip("Delay before starting")]
    public float initialDelay = 0.5f;

    private TextMeshProUGUI textComponent;
    private Coroutine displayCoroutine;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
        {
            Debug.LogError("TextMeshProUGUI component not found on this GameObject.");
        }
    }

    public void DisplayResponse()
    {
        if (textComponent == null) return;

        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        displayCoroutine = StartCoroutine(TypewriterSentenceEffect());
    }

    private IEnumerator TypewriterSentenceEffect()
    {
        textComponent.text = "";

        if (initialDelay > 0)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        // Strip quotation marks from the response before processing
        string cleanedResponse = StripQuotationMarks(aiResponse);

        string[] sentences = SplitIntoSentences(cleanedResponse);
        float delayBetweenChars = 1f / charactersPerSecond;

        string displayedText = "";

        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence)) continue;

            for (int i = 0; i < sentence.Length; i++)
            {
                displayedText += sentence[i];
                textComponent.text = displayedText;
                yield return new WaitForSeconds(delayBetweenChars);
            }

            displayedText += " ";
            textComponent.text = displayedText;

            yield return new WaitForSeconds(delayBetweenSentences);

            textComponent.text = "";
            displayedText = "";
        }
    }


    private string StripQuotationMarks(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text.Replace("\"", "");
    }

    private string[] SplitIntoSentences(string text)
    {
        char[] sentenceDelimiters = new char[] { '.', '!', '?' };
        string[] sentences = text.Split(sentenceDelimiters);

        for (int i = 0; i < sentences.Length - 1; i++)
        {
            sentences[i] = sentences[i].Trim() + text[text.IndexOf(sentences[i]) + sentences[i].Length];
        }

        return sentences;
    }

    public void SkipToEnd()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        if (textComponent != null)
        {
            textComponent.text = StripQuotationMarks(aiResponse);
        }
    }

    public void SetAndDisplayResponse(string newResponse)
    {
        aiResponse = newResponse;
        DisplayResponse();
    }
}
