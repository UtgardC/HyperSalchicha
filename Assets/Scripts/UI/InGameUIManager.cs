using UnityEngine;
using DG.Tweening;
using TMPro;

public class InGameUIManager : MonoBehaviour
{
    [Header("Round Display")]
    [SerializeField] private TextMeshProUGUI currentRoundDisplay;
    [SerializeField] private float squashDuration = 0.15f;
    [SerializeField] private Ease squashEase = Ease.InOutSine;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = currentRoundDisplay.transform.localScale;
    }

    public void UpdateCurrentRoundDisplay(int round)
    {
        var target = currentRoundDisplay.transform;
        target.DOKill();
        target.localScale = originalScale;

        DOTween.Sequence()
            .Append(target.DOScale(new Vector3(0f, originalScale.y, originalScale.z), squashDuration).SetEase(squashEase))
            .AppendCallback(() => currentRoundDisplay.text = round.ToString())
            .Append(target.DOScale(originalScale, squashDuration).SetEase(squashEase));
    }
}
