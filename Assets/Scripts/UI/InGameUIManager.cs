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

    [Header("Weapons (optional)")]
    [SerializeField] private TextMeshProUGUI magazineDisplay;
    [SerializeField] private TextMeshProUGUI reserveDisplay;
    [SerializeField] private TextMeshProUGUI weaponNameDisplay;
    [SerializeField] private Color ammoZeroColor = new Color(1f, 0.45f, 0.45f, 1f);
    [SerializeField] private float weaponNameVisibleSeconds = 3f;
    [SerializeField] private float weaponNameFadeSeconds = 0.35f;
    [SerializeField] private Ease weaponNameFadeEase = Ease.InOutSine;

    private Vector3 originalScale;
    private Tween weaponNameFadeTween;
    private Color magazineDefaultColor = Color.white;
    private Color reserveDefaultColor = Color.white;

    private void Awake()
    {
        originalScale = currentRoundDisplay.transform.localScale;
        if (magazineDisplay != null)
            magazineDefaultColor = magazineDisplay.color;
        if (reserveDisplay != null)
            reserveDefaultColor = reserveDisplay.color;
        if (weaponNameDisplay != null)
            weaponNameDisplay.alpha = 0f;
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

    public void UpdateAmmoDisplay(int magazine, int reserve, bool reserveIsInfinite = false)
    {
        if (magazineDisplay != null)
        {
            magazineDisplay.text = magazine.ToString();
            magazineDisplay.color = magazine <= 0 ? ammoZeroColor : magazineDefaultColor;
        }
        if (reserveDisplay != null)
        {
            reserveDisplay.text = reserveIsInfinite ? "\u221E" : reserve.ToString();
            reserveDisplay.color = reserveIsInfinite ? reserveDefaultColor : reserve <= 0 ? ammoZeroColor : reserveDefaultColor;
        }
    }

    public void UpdateWeaponNameDisplay(string displayName)
    {
        if (weaponNameDisplay == null)
            return;

        if (weaponNameFadeTween != null)
        {
            weaponNameFadeTween.Kill();
            weaponNameFadeTween = null;
        }

        if (string.IsNullOrEmpty(displayName))
        {
            weaponNameDisplay.text = string.Empty;
            weaponNameDisplay.alpha = 0f;
            return;
        }

        weaponNameDisplay.text = displayName;
        weaponNameDisplay.alpha = 1f;
        weaponNameFadeTween = DOTween.To(
                () => weaponNameDisplay.alpha,
                value => weaponNameDisplay.alpha = value,
                0f,
                weaponNameFadeSeconds)
            .SetDelay(Mathf.Max(0f, weaponNameVisibleSeconds))
            .SetEase(weaponNameFadeEase);
    }
}
