using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Sources")]
    [SerializeField] private AudioSource[] sources;

    [Header("Settings")]
    [SerializeField] private bool soundOn = true;

    private int index = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // =========================
    // 사운드 재생
    // =========================
    public void Play(AudioClip clip)
    {
        if (!soundOn || clip == null || sources.Length == 0)
            return;

        AudioSource src = GetSource();
        src.PlayOneShot(clip);
    }

    private AudioSource GetSource()
    {
        for (int i = 0; i < sources.Length; i++)
        {
            int idx = (index + i) % sources.Length;
            if (!sources[idx].isPlaying)
            {
                index = (idx + 1) % sources.Length;
                return sources[idx];
            }
        }

        // 다 쓰고 있으면 그냥 덮어쓰기
        AudioSource fallback = sources[index];
        index = (index + 1) % sources.Length;
        return fallback;
    }

    // =========================
    // 전체 ON / OFF
    // =========================
    public void ToggleSound()
    {
        soundOn = !soundOn;
    }

    public void SetSound(bool on)
    {
        soundOn = on;
    }

    public bool IsSoundOn()
    {
        return soundOn;
    }
}