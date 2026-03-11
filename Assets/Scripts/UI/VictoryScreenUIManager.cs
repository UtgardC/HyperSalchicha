using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class VictoryScreenUIManager : MonoBehaviour
{
    [Header("Textos (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI currentRoundText;
    [SerializeField] private TextMeshProUGUI maxRoundRecordText;
    [SerializeField] private TextMeshProUGUI cuajosText;

    [Header("Bounce 'Nuevo record'")]
    [SerializeField] private GameObject bounceTarget;
    [SerializeField] private float bounceScaleMultiplier = 1.2f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private Ease bounceEase = Ease.InOutSine;

    [Header("Movimiento hacia arriba")]
    [SerializeField] private RectTransform moveTarget;
    [SerializeField] private float moveOffsetY = 80f;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private float moveDelay = 0.5f;

    [Header("Fade in de sprite / grupo")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.Linear;

    private GameManager Game => GameManager.Instance;
    private Vector3 bounceOriginalScale;
    private Vector2 moveOriginalAnchoredPos;

    private void Awake()
    {
        if (bounceTarget != null)
            bounceOriginalScale = bounceTarget.transform.localScale;
        if (moveTarget != null)
            moveOriginalAnchoredPos = moveTarget.anchoredPosition;
    }

    private void OnEnable()
    {
        UpdateTexts();
        HandleNewRecordBounce();
        StartCoroutine(MoveUpCoroutine());
        PlayFadeIn();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (bounceTarget != null)
            bounceTarget.transform.DOKill();
        if (moveTarget != null)
            moveTarget.DOKill();
        if (fadeImage != null)
            fadeImage.DOKill();
    }

    private void UpdateTexts()
    {
        if (Game == null)
            return;

        if (currentRoundText != null)
            currentRoundText.text = Game.currentRound.ToString();
        if (maxRoundRecordText != null)
            maxRoundRecordText.text = Game.maxRoundRecord.ToString();
        if (cuajosText != null)
            cuajosText.text = Game.cuajosActuales.ToString();
    }

    private void HandleNewRecordBounce()
    {
        if (bounceTarget == null || Game == null)
            return;

        bounceTarget.transform.DOKill();

        if (Game.newRecord)
        {
            bounceTarget.SetActive(true);
            bounceTarget.transform.localScale = bounceOriginalScale;
            bounceTarget.transform
                .DOScale(bounceOriginalScale * bounceScaleMultiplier, bounceDuration)
                .SetEase(bounceEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(UpdateType.Normal, true);
        }
        else
        {
            bounceTarget.SetActive(false);
        }
    }

    private IEnumerator MoveUpCoroutine()
    {
        if (moveTarget == null)
            yield break;

        moveTarget.DOKill();
        moveTarget.anchoredPosition = moveOriginalAnchoredPos;

        if (moveDelay > 0f)
            yield return new WaitForSecondsRealtime(moveDelay);

        moveTarget
            .DOAnchorPos(moveOriginalAnchoredPos + new Vector2(0f, moveOffsetY), moveDuration)
            .SetEase(moveEase)
            .SetUpdate(UpdateType.Normal, true);
    }

    private void PlayFadeIn()
    {
        if (fadeImage == null)
            return;

        fadeImage.DOKill();

        Color color = fadeImage.color;
        fadeImage.color = new Color(color.r, color.g, color.b, 0f);
        fadeImage
            .DOFade(1f, fadeDuration)
            .SetEase(fadeEase)
            .SetUpdate(UpdateType.Normal, true);
    }
}
