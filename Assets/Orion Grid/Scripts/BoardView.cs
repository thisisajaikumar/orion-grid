using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardView : MonoBehaviour
{
    [SerializeField] GridLayoutGroup grid;
    [SerializeField] RectTransform container;

    [SerializeField] CardView cardPrefab;

    [SerializeField] float maxCellSize = 280f;

    readonly List<CardView> active = new();
    readonly Queue<CardView> pool = new();
    readonly List<CardView> peekCandidates = new();

    Coroutine fitRoutine;
    Coroutine peekRoutine;

    public IReadOnlyList<CardView> Cards => active;

    void Awake()
    {
        if (grid == null || container == null || cardPrefab == null)
        {
            Debug.LogError($"{nameof(BoardView)} has missing references.");
            enabled = false;
            return;
        }
        var csf = grid.GetComponent<ContentSizeFitter>();
        if (csf != null)
        {
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.enabled = false;
            Debug.LogWarning(
                $"[{nameof(BoardView)}] ContentSizeFitter on the GridLayoutGroup " +
                "was disabled at runtime — please remove it in the Inspector.");
        }

        var gridRT = grid.GetComponent<RectTransform>();
        gridRT.anchorMin = Vector2.zero;
        gridRT.anchorMax = Vector2.one;
        gridRT.offsetMin = Vector2.zero;
        gridRT.offsetMax = Vector2.zero;
    }

    void OnEnable() => GameEvents.OnStateChanged += HandleStateChange;
    void OnDisable() => GameEvents.OnStateChanged -= HandleStateChange;

    void HandleStateChange(GameState s)
    {
        if (s == GameState.Playing) BeginRandomPeeks();
        else StopRandomPeeks();
    }

    public List<CardView> Build(
        List<(int pairIndex, Sprite sprite)> data,
        int columns, int rows,
        Action onReady = null)
    {
        StopRandomPeeks();
        ReturnAllToPool();

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;

        foreach (var (idx, sprite) in data)
        {
            var card = Rent();
            card.transform.SetParent(grid.transform, false);
            card.Setup(idx, sprite);
            active.Add(card);
        }

        if (fitRoutine != null) StopCoroutine(fitRoutine);
        fitRoutine = StartCoroutine(FitAfterLayout(columns, rows, onReady));

        return active;
    }

    public void RevealAll() { foreach (var c in active) c.RevealInstant(); }
    public void HideAll() { foreach (var c in active) c.HideInstant(); }
    public void StartIdleAnimations() { for (int i = 0; i < active.Count; i++) active[i].StartIdleAnimation(i); }
    public void StopIdleAnimations() { foreach (var c in active) c.StopIdleAnimation(); }

    IEnumerator FitAfterLayout(int columns, int rows, Action onReady)
    {
        yield return new WaitForEndOfFrame();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(container);

        yield return new WaitForEndOfFrame();

        FitGrid(columns, rows);
        fitRoutine = null;
        onReady?.Invoke();
    }
    void FitGrid(int columns, int rows)
    {
        float width = container.rect.width;
        float height = container.rect.height;

        if (width <= 0f || height <= 0f)
        {
            Debug.LogError(
                $"[{nameof(BoardView)}] container rect is invalid ({width}×{height}). " +
                "Ensure the container RectTransform is anchored and not zero-sized.");
            return;
        }

        float usableW = width - (grid.padding.left + grid.padding.right);
        float usableH = height - (grid.padding.top + grid.padding.bottom);

        float gapW = grid.spacing.x * (columns - 1);
        float gapH = grid.spacing.y * (rows - 1);

        float cellW = (usableW - gapW) / columns;
        float cellH = (usableH - gapH) / rows;

        float size = Mathf.Min(cellW, cellH);

        if (maxCellSize > 0f)
            size = Mathf.Min(size, maxCellSize);

        size = Mathf.Max(size, 1f);

        grid.cellSize = new Vector2(size, size);

        Debug.Log(
            $"[{nameof(BoardView)}] FitGrid {columns}x{rows} | " +
            $"container={width:F1}x{height:F1} | " +
            $"usable={usableW:F1}x{usableH:F1} | " +
            $"cell={size:F1}px");
    }

    void BeginRandomPeeks()
    {
        StopRandomPeeks();
        peekRoutine = StartCoroutine(RandomPeekRoutine());
    }

    void StopRandomPeeks()
    {
        if (peekRoutine == null) return;
        StopCoroutine(peekRoutine);
        peekRoutine = null;
    }

    IEnumerator RandomPeekRoutine()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(2.0f, 3.5f));

        while (true)
        {
            peekCandidates.Clear();

            for (int i = 0; i < active.Count; i++)
            {
                var card = active[i];
                if (!card.IsOpen && !card.IsLocked)
                    peekCandidates.Add(card);
            }

            if (peekCandidates.Count > 0)
            {
                var pick = peekCandidates[UnityEngine.Random.Range(0, peekCandidates.Count)];
                pick.PlayPeekAnimation();
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(1.8f, 4.2f));
        }
    }

    CardView Rent()
    {
        if (pool.Count > 0)
        {
            var c = pool.Dequeue();
            c.gameObject.SetActive(true);
            return c;
        }
        return Instantiate(cardPrefab);
    }

    void ReturnAllToPool()
    {
        foreach (var card in active)
        {
            card.StopIdleAnimation();
            card.Reset();
            card.transform.SetParent(transform, false);
            card.gameObject.SetActive(false);
            pool.Enqueue(card);
        }
        active.Clear();
    }

    void OnDestroy()
    {
        StopRandomPeeks();
        while (pool.Count > 0)
        {
            var c = pool.Dequeue();
            if (c) Destroy(c.gameObject);
        }
    }
}