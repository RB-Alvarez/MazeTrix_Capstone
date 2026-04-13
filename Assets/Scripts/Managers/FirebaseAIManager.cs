using UnityEngine;
using Firebase;
using Firebase.AI;
using System.Threading.Tasks;
using System.Collections;

public class FirebaseAIManager : MonoBehaviour
{
    private FirebaseAI ai;
    private GenerativeModel model;
    
    [Header("UI Reference")]
    public ShowAIResponse responseDisplay;

    [Header("AI Model Configuration")]
    [TextArea(3, 10)]
    [Tooltip("Persistent system instructions for the AI model")]
    public string systemInstructions = 
        "You are Dr. 9524, a scientist who created robots to test their survival in an infinite maze. " +
        "Return dialog only. Stick to a few sentences at a time. " +
        "Stay in character as a detached scientist who can't keep track of all the robot units you've created. At least this one you remember is Unit AD14";

    public string playerlog = "Recent activity: ";

    [Header("Periodic Update Settings")]
    [Tooltip("Interval in seconds between automatic AI updates")] // how often will the AI respond to playerlog
    public float updateInterval = 10f;
    
    [Tooltip("Enable/disable automatic periodic AI updates")]
    public bool enablePeriodicUpdates = true;

    private Coroutine periodicUpdateCoroutine;
    private bool isProcessingUpdate = false;

    public static FirebaseAIManager Instance { get; private set; }

    void Awake()
    {
        // Set singleton instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ai = FirebaseAI.GetInstance(FirebaseAI.Backend.GoogleAI());
        model = ai.GetGenerativeModel(modelName: "gemini-2.5-flash-lite");
    }

    async void Start()
    {
        var prompt = "This may be an existing or a new robot unit. Greet your creation.";

        try
        {
            var response = await model.GenerateContentAsync(BuildPromptWithInstructions(prompt));
            string aiText = response.Text ?? "No text in response.";
            
            if (responseDisplay != null)
            {
                responseDisplay.SetAndDisplayResponse(aiText);
            }
            
            Debug.Log(aiText);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating AI response: {e.Message}");
        }

        if (enablePeriodicUpdates)
        {
            StartPeriodicUpdates();
        }
    }

    void OnDestroy()
    {
        StopPeriodicUpdates();
    }

    private string BuildPromptWithInstructions(string userPrompt)
    {
        return $"{systemInstructions}\n\n{userPrompt}";
    }

    public async Task<string> SendPromptAsync(string prompt)
    {
        try
        {
            var response = await model.GenerateContentAsync(BuildPromptWithInstructions(prompt));
            string aiText = response.Text ?? "No text in response.";
            
            if (responseDisplay != null)
            {
                responseDisplay.SetAndDisplayResponse(aiText);
            }
            
            Debug.Log($"AI Response: {aiText}");
            return aiText;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating AI response: {e.Message}");
            return null;
        }
    }

    public async Task<string> SendPromptWithLogAsync(string additionalContext = "")
    {
        string prompt = $"Based on the following recent activity log, respond to the player:\n{playerlog}";
        
        if (!string.IsNullOrEmpty(additionalContext))
        {
            prompt += $"\n\nAdditional context: {additionalContext}";
        }
        
        return await SendPromptAsync(prompt);
    }

    public void UpdatePlayerLog(string newEntry)
    {
        playerlog += newEntry + ". ";
        
        // Keep log manageable (last 500 characters to avoid token limits)
        if (playerlog.Length > 500)
        {
            int cutIndex = playerlog.Length - 500;
            int periodIndex = playerlog.IndexOf(". ", cutIndex);
            if (periodIndex > 0)
            {
                cutIndex = periodIndex + 2;
            }
            playerlog = "Recent activity: " + playerlog.Substring(cutIndex);
        }
    }

    public void ResetPlayerLog()
    {
        playerlog = "Recent activity: ";
    }

    public void StartPeriodicUpdates()
    {
        StopPeriodicUpdates();
        periodicUpdateCoroutine = StartCoroutine(PeriodicUpdateCoroutine());
        Debug.Log($"Started periodic AI updates every {updateInterval} seconds");
    }

    public void StopPeriodicUpdates()
    {
        if (periodicUpdateCoroutine != null)
        {
            StopCoroutine(periodicUpdateCoroutine);
            periodicUpdateCoroutine = null;
            Debug.Log("Stopped periodic AI updates");
        }
    }

    private IEnumerator PeriodicUpdateCoroutine()
    {
        while (true)
        {
            // Wait for the full interval before checking anything
            yield return new WaitForSeconds(updateInterval);

            if (isProcessingUpdate)
            {
                Debug.Log("Skipping periodic AI update - previous update still processing");
                continue;
            }

            if (playerlog.Length > "Recent activity: ".Length)
            {
                isProcessingUpdate = true;
                Debug.Log("Sending periodic AI update with player log");
                
                Task<string> responseTask = SendPromptWithLogAsync();
                
                while (!responseTask.IsCompleted)
                {
                    yield return null;
                }
                
                if (responseTask.Exception != null)
                {
                    Debug.LogError($"Error during periodic AI update: {responseTask.Exception.GetBaseException().Message}");
                }
                
                ResetPlayerLog();
                isProcessingUpdate = false;
            }
            else
            {
                Debug.Log("Skipping periodic AI update - no activity logged");
            }
        }
    }
}



