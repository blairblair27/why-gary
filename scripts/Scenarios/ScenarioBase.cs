using UnityEngine;

// Every scenario inherits from this. Override OnScenarioStart, OnScenarioWin, OnScenarioLose.
// Call EndScenario(true/false) when the outcome is decided.
public abstract class ScenarioBase : MonoBehaviour
{
    protected ScenarioManager Manager { get; private set; }

    void Start()
    {
        Manager = FindObjectOfType<ScenarioManager>();
        OnScenarioStart();
    }

    protected virtual void OnScenarioStart() { }
    protected virtual void OnScenarioWin()   { }
    protected virtual void OnScenarioLose(string reason) { }

    protected void EndScenario(bool won, string loseReason = "")
    {
        if (won)
        {
            OnScenarioWin();
            Manager?.ShowOutcome(won: true, message: "TARGET DOWN");
        }
        else
        {
            OnScenarioLose(loseReason);
            Manager?.ShowOutcome(won: false, message: "WHY GARY WHY");
        }
    }
}
