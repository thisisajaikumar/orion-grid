using UnityEngine;

public class AudioService : MonoBehaviour
{
    [SerializeField] AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] AudioClip clipFlip;
    [SerializeField] AudioClip clipMatch;
    [SerializeField] AudioClip clipMismatch;
    [SerializeField] AudioClip clipGameOver;

    void Awake()
    {
        if (sfxSource == null)
        {
            Debug.LogError($"{nameof(AudioService)} requires an AudioSource.");
            enabled = false;
        }
    }

    void OnEnable()
    {
        GameEvents.OnCardFlipped += OnFlip;
        GameEvents.OnPairEvaluated += OnPairEvaluated;
        GameEvents.OnStateChanged += OnStateChanged;
    }

    void OnDisable()
    {
        GameEvents.OnCardFlipped -= OnFlip;
        GameEvents.OnPairEvaluated -= OnPairEvaluated;
        GameEvents.OnStateChanged -= OnStateChanged;
    }

    void OnFlip() => sfxSource.PlayOneShot(clipFlip);
    void OnPairEvaluated(bool isMatch) => sfxSource.PlayOneShot(isMatch ? clipMatch : clipMismatch);

    void OnStateChanged(GameState s)
    {
        if (s == GameState.GameOver) sfxSource.PlayOneShot(clipGameOver);
    }
}