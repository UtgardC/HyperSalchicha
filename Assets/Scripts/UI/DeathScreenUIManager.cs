using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class DeathScreenUIManager : MonoBehaviour
{
    [Header("Referencia al GameManager")]
    [SerializeField] private GameManager gameManager;

    [Header("Textos (TextMeshProUGUI)")]
    [SerializeField] private TextMeshProUGUI currentRoundText;
    [SerializeField] private TextMeshProUGUI maxRoundRecordText;
    [SerializeField] private TextMeshProUGUI cuajosText;

    [Header("Bounce 'Nuevo récord'")]
    [SerializeField] private GameObject bounceTarget; // cartel "NEW RECORD!"
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

    private Vector3 _bounceOriginalScale;
    private Vector2 _moveOriginalAnchoredPos;

    private void Awake()
    {
        _bounceOriginalScale = bounceTarget.transform.localScale;
        _moveOriginalAnchoredPos = moveTarget.anchoredPosition;
    }

    private void OnEnable()
    {
        UpdateTexts();
        HandleNewRecordBounce();
        StartCoroutine(MoveUpCoroutine());
        PlayFadeIn();
    }

    private void UpdateTexts()
    {
        currentRoundText.text   = gameManager.currentRound.ToString();
        maxRoundRecordText.text = gameManager.maxRoundRecord.ToString();
        cuajosText.text         = gameManager.cuajosActuales.ToString();
    }

    private void HandleNewRecordBounce()
    {
        if (gameManager.newRecord)
        {
            bounceTarget.SetActive(true);
            bounceTarget.transform.localScale = _bounceOriginalScale;

            bounceTarget.transform
                .DOScale(_bounceOriginalScale * bounceScaleMultiplier, bounceDuration)
                .SetEase(bounceEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true); // ignora Time.timeScale (por el pause)
        }
        else
        {
            bounceTarget.SetActive(false);
        }
    }

    private IEnumerator MoveUpCoroutine()
    {
        moveTarget.anchoredPosition = _moveOriginalAnchoredPos;

        if (moveDelay > 0f)
            yield return new WaitForSecondsRealtime(moveDelay);

        moveTarget
            .DOAnchorPos(_moveOriginalAnchoredPos + new Vector2(0f, moveOffsetY), moveDuration)
            .SetEase(moveEase)
            .SetUpdate(true);
    }

    private void PlayFadeIn()
    {
        Color c = fadeImage.color;
        fadeImage.color = new Color(c.r, c.g, c.b, 0f);

        fadeImage
            .DOFade(1f, fadeDuration)
            .SetEase(fadeEase)
            .SetUpdate(true);
    }

}
