using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameOverOverlayController : MonoBehaviour
{
    [Header("Restart")]
    public string fallbackScene = "Town";

    [Header("Optional Prompt")]
    public GameObject pressSpacePrompt;
    public float promptDelay = 0.5f; // delay before showing prompt

    private bool canRestart;

    private void OnEnable()
    {
        canRestart = false;

        if (pressSpacePrompt != null)
            pressSpacePrompt.SetActive(false);

        StartCoroutine(EnableRestartAfterDelay());
    }

    private IEnumerator EnableRestartAfterDelay()
    {
        // IMPORTANT: realtime, works even when Time.timeScale = 0
        yield return new WaitForSecondsRealtime(promptDelay);

        if (pressSpacePrompt != null)
            pressSpacePrompt.SetActive(true);

        canRestart = true;
    }

    private void Update()
    {
        if (!canRestart) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Time.timeScale = 1f;

            string targetScene = string.IsNullOrEmpty(GameOverState.lastGameplayScene)
                ? fallbackScene
                : GameOverState.lastGameplayScene;

            SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
        }
    }
}