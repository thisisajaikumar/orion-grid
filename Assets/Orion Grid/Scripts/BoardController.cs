using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    readonly List<CardView> pendingCards = new(2);
    Coroutine evalRoutine;
    GameLevelConfig config;
    int totalPairs;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MatchesFound { get; private set; }

    public System.Action OnAllMatched;


    public void Initialise(
        GameLevelConfig cfg,
        int pairs,
        int startScore = 0,
        int startCombo = 0,
        int startMatches = 0)
    {
        config = cfg;
        totalPairs = pairs;
        Score = startScore;
        Combo = startCombo;
        MatchesFound = startMatches;

        CancelPending();
        pendingCards.Clear();
    }

    public void HandleCardClicked(CardView card)
    {
        if (pendingCards.Count == 2)
        {
            CancelPending();
            Evaluate();
        }

        if (card.IsLocked || card.IsOpen || pendingCards.Contains(card)) return;

        card.FlipOpen();
        pendingCards.Add(card);

        if (pendingCards.Count == 2)
            evalRoutine = StartCoroutine(EvalAfterDelay());
    }

    public int[] GetMatchedPairIndices(IReadOnlyList<CardView> allCards)
    {
        HashSet<int> set = new HashSet<int>();

        for (int i = 0; i < allCards.Count; i++)
        {
            var card = allCards[i];
            if (card.IsLocked)
                set.Add(card.PairIndex);
        }

        int[] result = new int[set.Count];
        set.CopyTo(result);

        return result;
    }

    IEnumerator EvalAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);
        Evaluate();
        evalRoutine = null;
    }

    void Evaluate()
    {
        if (pendingCards.Count < 2) return;

        var a = pendingCards[0];
        var b = pendingCards[1];
        pendingCards.Clear();

        if (a.PairIndex == b.PairIndex)
            OnMatch(a, b);
        else
            OnMismatch(a, b);
    }

    void OnMatch(CardView a, CardView b)
    {
        Combo++;
        int earned = Mathf.RoundToInt(config.baseScore * (1f + Combo * config.comboBonus));
        Score += earned;
        MatchesFound++;

        a.PlayMatchAnimation();
        b.PlayMatchAnimation();

        GameEvents.PairEvaluated(true);
        GameEvents.ScoreUpdated(Score);
        GameEvents.ComboUpdated(Combo);
        GameEvents.ProgressUpdated((float)MatchesFound / totalPairs);

        if (MatchesFound >= totalPairs)
            OnAllMatched?.Invoke();
    }

    void OnMismatch(CardView a, CardView b)
    {
        Combo = 0;
        a.PlayMismatchAnimation();
        b.PlayMismatchAnimation();

        GameEvents.PairEvaluated(false);
        GameEvents.ComboUpdated(Combo);
    }

    void CancelPending()
    {
        if (evalRoutine == null) return;
        StopCoroutine(evalRoutine);
        evalRoutine = null;
    }
}