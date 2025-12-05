using UnityEngine;
using DG.Tweening;
using TMPro;

public class InGameUIManager : MonoBehaviour
{
    [Header("Round Display")]
    [SerializeField] private TextMeshProUGUI currentRoundDisplay;
    [SerializeField] private float squashDuration = 0.15f;
    [SerializeField] private Ease squashEase = Ease.InOutSine;

    [Header("Cuajos")]
    [SerializeField] private TextMeshProUGUI cuajosDisplay;
    [SerializeField] private RectTransform cuajosTextAnchor;
    [SerializeField] private GameObject operationTextPrefab;

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

    public void UpdateCuajosDisplay(int total)
    {
        cuajosDisplay.text = total.ToString();
    }

    public void ShowCuajosChange(int amount)
    {

        var go = Instantiate(operationTextPrefab, cuajosTextAnchor);
        var rt = go.transform as RectTransform;
        if (rt != null) rt.anchoredPosition = Vector2.zero;

        var text = go.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            if (amount > 0)
            {
                text.text = $"+ {amount}";
            }
            else
            {
                text.text = $"- {Mathf.Abs(amount)}";
                text.color = Color.red;
            }
        }
    }
}
