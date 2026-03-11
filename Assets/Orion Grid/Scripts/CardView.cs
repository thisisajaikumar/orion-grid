using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardView : MonoBehaviour
{
    [SerializeField] GameObject backFace;
    [SerializeField] GameObject frontFace;
    [SerializeField] Image      icon;
    [SerializeField] Button     button;

    public System.Action<CardView> OnClick;

    public int  PairIndex { get; private set; }
    public bool IsOpen    { get; private set; }
    public bool IsLocked  { get; private set; }

    Sequence activeSeq;
    Tween    idleTween;

    static readonly Vector3 YEdge       = new(0, 90, 0);

    void Awake()
    {
        button.onClick.AddListener(ButtonSqueezeAnim);
        button.onClick.AddListener(HandleClick);
    }

    public void Setup(int pairIndex, Sprite sprite)
    {
        PairIndex   = pairIndex;
        icon.sprite = sprite;
        Reset();
    }

    void HandleClick()
    {
        if (IsOpen || IsLocked) return;
        OnClick?.Invoke(this);
    }

    public void Reset()
    {
        KillAll();
        IsOpen   = false;
        IsLocked = false;
        ShowBack();
        transform.localScale       = Vector3.one;
        transform.localEulerAngles = Vector3.zero;
        gameObject.SetActive(true);
    }

    public void RevealInstant()
    {
        if (IsLocked) return;
        ShowFront();
        IsOpen = true;
    }

    public void HideInstant()
    {
        if (IsLocked) return;
        ShowBack();
        IsOpen = false;
    }

    public void SetMatchedInstant()
    {
        KillAll();
        IsOpen   = true;
        IsLocked = true;
        gameObject.SetActive(false);
    }

    public void FlipOpen()
    {
        if (IsOpen) return;
        IsOpen = true;
        KillAll();
        ShowBack();
        transform.localScale       = Vector3.one;
        transform.localEulerAngles = Vector3.zero;

        GameEvents.CardFlipped();

        activeSeq = DOTween.Sequence()
            // Anticipation: quick vertical compress
            .Append(transform.DOScale(new Vector3(1.0f, 0.82f, 1f), 0.07f).SetEase(Ease.OutQuad))
            // First half: rotate to edge, squash horizontally as card turns
            .Append(transform.DOLocalRotate(YEdge, 0.13f).SetEase(Ease.InSine))
            .Join(transform.DOScale(new Vector3(0.1f, 1.05f, 1f), 0.13f).SetEase(Ease.InSine))
            .AppendCallback(ShowFront)
            // Second half: spring open with horizontal stretch on landing
            .Append(transform.DOLocalRotate(Vector3.zero, 0.14f).SetEase(Ease.OutBack))
            .Join(transform.DOScale(new Vector3(1.15f, 0.90f, 1f), 0.10f).SetEase(Ease.OutQuad))
            // Settle
            .Append(transform.DOScale(Vector3.one, 0.10f).SetEase(Ease.OutBack));
    }

    public void PlayMismatchAnimation()
    {
        KillAll();
        IsOpen = false;
        ShowFront();
        transform.localEulerAngles = Vector3.zero;
        transform.localScale       = Vector3.one;

        activeSeq = DOTween.Sequence()
            // "No no no" Z-shake (four half-swings)
            .Join(transform.DOLocalRotate(new Vector3(0, 0, -13f), 0.07f).SetEase(Ease.OutQuad))
            .Append(transform.DOLocalRotate(new Vector3(0, 0,  13f), 0.09f).SetEase(Ease.InOutSine))
            .Append(transform.DOLocalRotate(new Vector3(0, 0, -10f), 0.09f).SetEase(Ease.InOutSine))
            .Append(transform.DOLocalRotate(new Vector3(0, 0,   7f), 0.08f).SetEase(Ease.InOutSine))
            .Append(transform.DOLocalRotate(Vector3.zero,            0.07f).SetEase(Ease.OutQuad))
            // Flip back: first half
            .Append(transform.DOLocalRotate(YEdge, 0.13f).SetEase(Ease.InSine))
            .Join(transform.DOScale(new Vector3(0.1f, 1.0f, 1f), 0.13f).SetEase(Ease.InSine))
            .AppendCallback(ShowBack)
            // Flip back: second half — springy landing
            .Append(transform.DOLocalRotate(Vector3.zero, 0.14f).SetEase(Ease.OutBack))
            .Join(transform.DOScale(Vector3.one, 0.14f).SetEase(Ease.OutBack));
    }

    public void PlayMatchAnimation()
    {
        IsLocked = true;
        KillAll();
        ShowFront();
        transform.localEulerAngles = Vector3.zero;
        transform.localScale       = Vector3.one;

        activeSeq = DOTween.Sequence()
            // Pop out with overshoot
            .Append(transform.DOScale(1.32f, 0.15f).SetEase(Ease.OutBack))
            // Bounce back to natural size
            .Append(transform.DOScale(1.00f, 0.10f).SetEase(Ease.InQuad))
            // Hold a beat so the player registers the match
            .AppendInterval(0.08f)
            // Shrink away with InBack for a snappy finish
            .Append(transform.DOScale(0f, 0.26f).SetEase(Ease.InBack));
    }

    public void PlayPeekAnimation()
    {
        if (IsOpen || IsLocked) return;
        KillActive();

        activeSeq = DOTween.Sequence()
            // Lean in with a slight scale squash
            .Append(transform.DOScale(new Vector3(0.92f, 1.06f, 1f), 0.10f).SetEase(Ease.OutQuad))
            .Join(transform.DOLocalRotate(new Vector3(0f, 72f, 0f), 0.22f).SetEase(Ease.OutCubic))
            // Hesitation wobble at the peak
            .Append(transform.DOLocalRotate(new Vector3(0f, 65f, 0f), 0.10f).SetEase(Ease.InOutSine))
            .Append(transform.DOLocalRotate(new Vector3(0f, 74f, 0f), 0.10f).SetEase(Ease.InOutSine))
            // Snap home
            .Append(transform.DOLocalRotate(Vector3.zero, 0.20f).SetEase(Ease.OutBack))
            .Join(transform.DOScale(Vector3.one, 0.20f).SetEase(Ease.OutBack));
    }

    public void StartIdleAnimation(int siblingIndex)
    {
        StopIdle();
        float delay = (siblingIndex % 8) * 0.14f;
        idleTween = transform
            .DOScale(1.07f, 0.9f)
            .SetDelay(delay)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    public void StopIdleAnimation()
    {
        StopIdle();
        transform.localScale       = Vector3.one;
        transform.localEulerAngles = Vector3.zero;
    }

    void ShowFront()
    {
        backFace.SetActive(false);
        frontFace.SetActive(true);
    }

    void ShowBack()
    {
        frontFace.SetActive(false);
        backFace.SetActive(true);
    }

    void ButtonSqueezeAnim()
    {
        if (activeSeq != null && activeSeq.IsActive()) return;
        transform.DOScale(0.82f, 0.07f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .OnComplete(() => transform.localScale = Vector3.one);
    }

    void KillActive()
    {
        activeSeq?.Kill(complete: false);
        activeSeq = null;
    }

    void StopIdle()
    {
        idleTween?.Kill(complete: false);
        idleTween = null;
    }

    void KillAll()
    {
        KillActive();
        StopIdle();
    }

    void OnDestroy() => KillAll();
}