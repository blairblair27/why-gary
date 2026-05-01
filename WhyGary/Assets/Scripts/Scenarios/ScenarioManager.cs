using System.Collections;
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
        StartCoroutine(ShowThenReload(message));
    }

    IEnumerator ShowThenReload(string message)
    {
        if (outcomeCanvas != null)
        {
            outcomeCanvas.SetActive(true);
            // Place canvas 1.5m in front of player at eye height, facing them
            outcomeCanvas.transform.position = playerHMD.position + playerHMD.forward * 1.5f;
            outcomeCanvas.transform.LookAt(playerHMD.position);
            outcomeCanvas.transform.Rotate(0, 180, 0);
        }
        if (outcomeText != null) outcomeText.text = message;

        yield return new WaitForSeconds(4f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
