using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIView : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] CanvasGroup menuPanel;
    [SerializeField] Button playButton;

    [Header("Game HUD")]
    [SerializeField] CanvasGroup hudPanel;
    [SerializeField] TMP_Text hudLevelText;
    [SerializeField] TMP_Text hudScoreText;
    [SerializeField] TMP_Text comboText;
    [SerializeField] TMP_Text timerText;
    [SerializeField] Image progressBar;

    [Header("Result Panel")]
    [SerializeField] CanvasGroup resultPanel;
    [SerializeField] TMP_Text resultTitleText;
    [SerializeField] TMP_Text matchScoreText;
    [SerializeField] TMP_Text timeBonusText;
    [SerializeField] TMP_Text totalScoreText;
    [SerializeField] TMP_Text bestScoreText;
    [SerializeField] Button nextLevelButton;
    [SerializeField] Button retryButton;

    public event Action OnPlayPressed;
    public event Action OnNextLevelPressed;
    public event Action OnRetryPressed;

    Tween pulseTween;
    Tween progressTween;

    int cachedLevelNumber;
    int cachedScore;

    void Awake()
    {
        WireButton(playButton, () => OnPlayPressed?.Invoke());
        WireButton(nextLevelButton, () => OnNextLevelPressed?.Invoke());
        WireButton(retryButton, () => OnRetryPressed?.Invoke());
    }

    void OnEnable()
    {
        GameEvents.OnStateChanged += HandleState;
        GameEvents.OnComboUpdated += UpdateCombo;
        GameEvents.OnTimerUpdated += UpdateTimer;
        GameEvents.OnProgressUpdated += UpdateProgress;
        GameEvents.OnScoreUpdated += UpdateScore;
    }

    void OnDisable()
    {
        GameEvents.OnStateChanged -= HandleState;
        GameEvents.OnComboUpdated -= UpdateCombo;
        GameEvents.OnTimerUpdated -= UpdateTimer;
        GameEvents.OnProgressUpdated -= UpdateProgress;
        GameEvents.OnScoreUpdated -= UpdateScore;
    }

    public void SetLevelInfo(int levelNumber, int score)
    {
        cachedLevelNumber = levelNumber;
        cachedScore = score;

        ApplyHudTexts();
    }

    public void ShowLevelComplete(int matchScore, int timeBonus, int total, int best)
    {
        resultTitleText.text = "Level Complete!";

        AnimateScoreLabel(matchScoreText, "Match  ", matchScore, delay: 0.3f);
        AnimateScoreLabel(timeBonusText, "Bonus  ", timeBonus, delay: 0.8f);
        AnimateScoreLabel(totalScoreText, "Total  ", total, delay: 1.4f,
            onDone: () =>
            {
                totalScoreText.transform
                    .DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.5f);
            });

        bestScoreText.text = $"Best   {best:N0}";
    }

    public void ShowGameOver(int score)
    {
        resultTitleText.text = "Time's Up";
        AnimateScoreLabel(matchScoreText, "Score  ", score, delay: 0.3f);
        timeBonusText.text = "";
        totalScoreText.text = "";
        bestScoreText.text = "";
    }

    void HandleState(GameState state)
    {
        bool showMenu = state == GameState.Idle;
        bool showHud = state == GameState.Playing || state == GameState.Preview;
        bool showResult = state == GameState.LevelComplete || state == GameState.GameOver;

        Fade(menuPanel, showMenu);
        Fade(hudPanel, showHud);
        Fade(resultPanel, showResult);

        if (showHud)
            ApplyHudTexts();

        if (showMenu)
            StartPulse();
        else
            StopPulse();
    }

    void ApplyHudTexts()
    {
        hudLevelText.text = $"Level {cachedLevelNumber}";

        if (hudScoreText != null)
            hudScoreText.text = $"Score: {cachedScore.ToString("N0")}";
    }

    void UpdateScore(int score)
    {
        cachedScore = score;

        if (hudScoreText != null)
            hudScoreText.text = $"Score: {score.ToString("N0")}";
    }

    void UpdateCombo(int combo)
    {
        if (combo < 2)
        {
            comboText.text = "";
            return;
        }

        comboText.text = $"COMBO x{combo}!";
        comboText.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.6f);
    }

    void UpdateTimer(float t)
    {
        int total = Mathf.CeilToInt(t);
        int minutes = total / 60;
        int seconds = total % 60;
        timerText.text = $"{minutes:00}:{seconds:00}";
        timerText.color = (t <= 10f)
            ? Color.Lerp(Color.white, new Color(1f, 0.28f, 0.28f), (10f - t) / 10f)
            : Color.white;
    }

    void UpdateProgress(float value)
    {
        progressTween?.Kill();

        progressTween = progressBar.DOFillAmount(value, 0.35f)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                progressBar.transform
                    .DOPunchScale(new Vector3(0.06f, 0.2f, 0f), 0.3f, 5, 0.4f);
            });
    }

    void AnimateScoreLabel(
        TMP_Text label, string prefix, int value,
        float delay = 0f, Action onDone = null)
    {
        label.text = $"{prefix}0";
        int display = 0;

        DOTween.To(() => display, x =>
        {
            display = x;
            label.text = $"{prefix}{x:N0}";
        }, value, 0.9f)
        .SetDelay(delay)
        .SetEase(Ease.OutCubic)
        .OnComplete(() => onDone?.Invoke());
    }

    static void WireButton(Button btn, Action action)
    {
        btn.onClick.AddListener(() =>
        {
            btn.transform
                .DOScale(0.88f, 0.08f)
                .SetEase(Ease.OutQuad)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => btn.transform.localScale = Vector3.one);
        });
        btn.onClick.AddListener(() => action?.Invoke());
    }

    void StartPulse()
    {
        StopPulse();
        pulseTween = playButton.transform
            .DOScale(1.1f, 0.8f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    void StopPulse()
    {
        pulseTween?.Kill();
        playButton.transform.localScale = Vector3.one;
    }

    void Fade(CanvasGroup cg, bool show)
    {
        cg.interactable = show;
        cg.blocksRaycasts = show;
        cg.DOKill();
        cg.DOFade(show ? 1f : 0f, 0.25f);
    }
}