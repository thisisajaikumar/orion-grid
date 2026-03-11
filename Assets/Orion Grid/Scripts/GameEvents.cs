using System;

public static class GameEvents
{
    public static event Action<GameState> OnStateChanged;
    public static event Action<int> OnScoreUpdated;
    public static event Action<float> OnTimerUpdated;
    public static event Action<float> OnProgressUpdated;
    public static event Action<int> OnComboUpdated;
    public static event Action<bool> OnPairEvaluated;
    public static event Action OnCardFlipped;

    public static void StateChanged(GameState s) => OnStateChanged?.Invoke(s);
    public static void ScoreUpdated(int v) => OnScoreUpdated?.Invoke(v);
    public static void TimerUpdated(float v) => OnTimerUpdated?.Invoke(v);
    public static void ProgressUpdated(float v) => OnProgressUpdated?.Invoke(v);
    public static void ComboUpdated(int v) => OnComboUpdated?.Invoke(v);
    public static void PairEvaluated(bool match) => OnPairEvaluated?.Invoke(match);
    public static void CardFlipped() => OnCardFlipped?.Invoke();
}