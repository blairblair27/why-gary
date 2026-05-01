using UnityEngine;

public class WhyGaryScenario : ScenarioBase
{
    [Header("Scenario Actors")]
    public NPCHealth targetNPC;
    public EscortAgent[] escorts;
    public NPCGroupPatrol patrolGroup;
    [Tooltip("Player's HMD — passed to escorts so they know where to tackle")]
    public Transform playerHMD;

    bool _ended;

    protected override void OnScenarioStart()
    {
        if (patrolGroup != null) patrolGroup.enabled = true;
    }

    void Update()
    {
        if (_ended) return;
        if (targetNPC != null && targetNPC.IsDead)
        {
            Win();
        }
    }

    // Called by EscortAgent when any escort is struck by a cork
    public void OnEscortHit()
    {
        if (_ended) return;
        _ended = true;
        if (patrolGroup != null) patrolGroup.enabled = false;
        foreach (var escort in escorts)
            escort?.StartTackle(playerHMD);
        EndScenario(won: false, loseReason: "escort hit");
    }

    // Called by NPCGroupPatrol when the group walks off the far end
    public void OnEscortReachedEnd()
    {
        if (_ended) return;
        _ended = true;
        EndScenario(won: false, loseReason: "target escaped");
    }

    void Win()
    {
        _ended = true;
        if (patrolGroup != null) patrolGroup.enabled = false;
        EndScenario(won: true);
    }
}
