using UnityEngine;
using System.Collections;

public class TutorialGuideController : MonoBehaviour
{
    [SerializeField] private GameObject guideImage;

    private void Awake()
    {
        Screen.SetResolution(720, 1280, false);
    }

    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // InputManager가 생성될 때까지 대기
        while (InputManager.Instance == null)
            yield return null;

        InputManager.Instance.OnBecameIdle += ShowGuide;
        InputManager.Instance.OnExitIdle += HideGuide;
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnBecameIdle -= ShowGuide;
            InputManager.Instance.OnExitIdle -= HideGuide;
        }
    }

    private void ShowGuide()
    {
        guideImage.SetActive(true);
    }

    private void HideGuide()
    {
        guideImage.SetActive(false);
    }
}