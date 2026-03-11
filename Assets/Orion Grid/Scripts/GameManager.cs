using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState : byte
{
    Idle,
    Preview,
    Playing,
    LevelComplete,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [Header("Levels")]
    [SerializeField] GameLevelConfig[] levels;

    [Header("References")]
    [SerializeField] BoardView boardView;
    [SerializeField] BoardController boardController;
    [SerializeField] UIView uiView;

    [Header("Debug")]
    [SerializeField] bool resetProgressOnStart = false;

    const float PreviewDuration = 2f;

    SaveData save;
    GameLevelConfig config;
    float timer;
    bool timerActive;
    void Awake()
    {
        if (boardView == null || boardController == null || uiView == null)
        {
            Debug.LogError($"{nameof(GameManager)} missing required references.");
            enabled = false;
            return;
        }

        if (resetProgressOnStart)
        {
            Debug.Log("Resetting save data...");
            SaveSystem.Delete();
            resetProgressOnStart = false;
        }
    }

    void Start()
    {
        save = SaveSystem.Load();
        save.currentLevel = Mathf.Clamp(save.currentLevel, 0, levels.Length - 1);

        uiView.OnPlayPressed += OnPlayPressed;
        uiView.OnRetryPressed += OnRetryPressed;
        uiView.OnNextLevelPressed += OnNextLevelPressed;
        LoadLevel(save.currentLevel, skipIdle: false);
    }

    void OnDestroy()
    {
        uiView.OnPlayPressed -= OnPlayPressed;
        uiView.OnRetryPressed -= OnRetryPressed;
        uiView.OnNextLevelPressed -= OnNextLevelPressed;
    }

    void Update()
    {
        if (!timerActive) return;

        timer = Mathf.Max(0f, timer - Time.deltaTime);
        GameEvents.TimerUpdated(timer);

        if (timer <= 0f) OnTimeUp();
    }

    void OnApplicationPause(bool paused) { if (paused) PersistSession(); }
    void OnApplicationQuit() => PersistSession();

    void LoadLevel(int index, bool skipIdle)
    {
        timerActive = false;
        config = levels[index];

        bool resuming = !skipIdle && save.hasActiveSession && save.currentLevel == index;

        if (resuming)
        {
            boardController.Initialise(
                config, config.TotalPairs,
                save.sessionScore, save.sessionCombo, save.sessionMatchesFound);
            timer = save.sessionTime;
        }
        else
        {
            boardController.Initialise(config, config.TotalPairs);
            timer = config.timeLimit;
        }

        boardController.OnAllMatched = OnAllMatched;

        uiView.SetLevelInfo(index + 1, boardController.Score);
        GameEvents.ProgressUpdated((float)boardController.MatchesFound / config.TotalPairs);
        GameEvents.TimerUpdated(timer);

        var icons = ShuffledIcons(config);

        boardView.Build(icons, config.columns, config.rows, onReady: () =>
        {
            foreach (var card in boardView.Cards)
                card.OnClick = boardController.HandleCardClicked;

            if (resuming)
                RestoreMatchedCards();

            if (skipIdle)
            {
                BeginPreview();
            }
            else
            {
                boardView.StartIdleAnimations();
                SetState(GameState.Idle);
            }
        });
    }

    void RestoreMatchedCards()
    {
        var matched = new HashSet<int>(save.matchedPairIndices ?? Array.Empty<int>());
        var cards = boardView.Cards;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];

            if (matched.Contains(card.PairIndex))
                card.SetMatchedInstant();
        }
    }

    void BeginPreview()
    {
        boardView.StopIdleAnimations();
        boardView.RevealAll();
        SetState(GameState.Preview);
        StartCoroutine(PreviewRoutine());
    }

    IEnumerator PreviewRoutine()
    {
        float remaining = PreviewDuration;

        while (remaining > 0f)
        {
            float step = Mathf.Min(1f, remaining);
            yield return new WaitForSeconds(step);
            remaining -= step;
        }

        boardView.HideAll();
        yield return new WaitForSeconds(0.3f);

        timerActive = true;
        SetState(GameState.Playing);
    }

    void OnPlayPressed() => BeginPreview();

    void OnRetryPressed()
    {
        save.hasActiveSession = false;
        SaveSystem.Save(save);
        LoadLevel(save.currentLevel, skipIdle: true);
    }

    void OnNextLevelPressed()
    {
        save.currentLevel = (save.currentLevel + 1) % levels.Length;
        save.hasActiveSession = false;
        SaveSystem.Save(save);
        LoadLevel(save.currentLevel, skipIdle: true);
    }

    void OnAllMatched()
    {
        timerActive = false;

        int matchScore = boardController.Score;
        int timeBonus = Mathf.RoundToInt(timer * config.timeBonusPerSec);
        int total = matchScore + timeBonus;

        int best = save.bestScores[save.currentLevel];
        if (total > best)
        {
            save.bestScores[save.currentLevel] = total;
            best = total;
        }

        save.hasActiveSession = false;
        SaveSystem.Save(save);

        uiView.ShowLevelComplete(matchScore, timeBonus, total, best);
        SetState(GameState.LevelComplete);
    }

    void OnTimeUp()
    {
        timerActive = false;
        save.hasActiveSession = false;
        SaveSystem.Save(save);

        uiView.ShowGameOver(boardController.Score);
        SetState(GameState.GameOver);
    }

    void SetState(GameState s) => GameEvents.StateChanged(s);

    void PersistSession()
    {
        if (!timerActive) return;

        save.hasActiveSession = true;
        save.sessionTime = timer;
        save.sessionScore = boardController.Score;
        save.sessionCombo = boardController.Combo;
        save.sessionMatchesFound = boardController.MatchesFound;
        save.matchedPairIndices = boardController.GetMatchedPairIndices(boardView.Cards);
        SaveSystem.Save(save);
    }

    static List<(int pairIndex, Sprite sprite)> ShuffledIcons(GameLevelConfig cfg)
    {
        var list = new List<(int, Sprite)>(cfg.TotalCards);

        for (int i = 0; i < cfg.TotalPairs; i++)
        {
            list.Add((i, cfg.cardIcons[i]));
            list.Add((i, cfg.cardIcons[i]));
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }
}