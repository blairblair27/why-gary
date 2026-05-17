using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioManager : MonoBehaviour
{
    [Tooltip("World-space Canvas that shows win/lose text")]
    public GameObject outcomeCanvas;
    public TextMeshProUGUI outcomeText;
    [Tooltip("Player's HMD transform — used to position the outcome canvas in view")]
    public Transform playerHMD;

    bool _ended;

    public void ShowOutcome(bool won, string message)
    {
        if (_ended) return;
        _ended = true;

        if (outcomeCanvas != null)
        {
            outcomeCanvas.SetActive(true);
            if (playerHMD != null)
            {
                outcomeCanvas.transform.position = playerHMD.position + playerHMD.forward * 1.5f;
                outcomeCanvas.transform.LookAt(playerHMD.position);
                outcomeCanvas.transform.Rotate(0, 180, 0);
            }
        }
        if (outcomeText != null) outcomeText.text = message;

#if UNITY_EDITOR
        if (UnityEditor.EditorBuildSettings.scenes.Length == 0)
            Debug.LogWarning("[ScenarioManager] No scenes in Build Settings — reload will fail in a build.");
#endif

        Invoke(nameof(ReloadScene), 4f);
    }

    void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
}
