using System.Collections;          // <-- NECESARIO PARA IEnumerator / Coroutines
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

    [Header("Bounce 'Nuevo récord'")]
    [SerializeField] private RectTransform bounceTarget;
    [SerializeField] private float bounceScaleMultiplier = 1.2f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private Ease bounceEase = Ease.InOutSine;

    [Header("Movimiento hacia arriba")]
    [SerializeField] private RectTransform moveTarget;
    [SerializeField] private float moveOffsetY = 80f;
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private float moveDelay = 0.5f; // <-- delay configurable

    [Header("Fade in de sprite / grupo")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Ease fadeEase = Ease.Linear;

    // Tweens
    private Tween _bounceTween;
    private Tween _moveTween;
    private Tween _fadeTween;

    // Corutina
    private Coroutine _moveCoroutine;

    // Estados originales
    private Vector3 _bounceOriginalScale;
    private Vector2 _moveOriginalAnchoredPos;

    private void Awake()
    {
        // Guardamos estados iniciales
        if (bounceTarget != null)
            _bounceOriginalScale = bounceTarget.localScale;

        if (moveTarget != null)
            _moveOriginalAnchoredPos = moveTarget.anchoredPosition;
    }

    private void OnEnable()
    {
        // 1) Actualizar textos con info del GameManager
        UpdateTexts();

        // 2) Lanzar animaciones
        PlayBounce();
        StartMoveUpWithDelay();   // <-- ahora usamos la versión con delay
        PlayFadeIn();
    }

    private void OnDisable()
    {
        KillTweens();

        // cancelar corutina si estaba corriendo
        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
            _moveCoroutine = null;
        }

        // Resetear estados visuales
        if (bounceTarget != null)
            bounceTarget.localScale = _bounceOriginalScale;

        if (moveTarget != null)
            moveTarget.anchoredPosition = _moveOriginalAnchoredPos;

        if (_fadeTween != null)
        {
            _fadeTween.Kill();
            _fadeTween = null;
        }
        if (fadeImage != null)
        {
            var c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    // --- TEXTOS ---

    private void UpdateTexts()
    {
        if (gameManager == null) return;

        if (currentRoundText != null)
            currentRoundText.text = gameManager.currentRound.ToString();

        if (maxRoundRecordText != null)
            maxRoundRecordText.text = gameManager.maxRoundRecord.ToString();
    }

    // --- BOUNCE ---

    private void PlayBounce()
    {
        if (bounceTarget == null) return;

        _bounceTween?.Kill();

        bounceTarget.localScale = _bounceOriginalScale;

        _bounceTween = bounceTarget
            .DOScale(_bounceOriginalScale * bounceScaleMultiplier, bounceDuration)
            .SetEase(bounceEase)
            .SetLoops(-1, LoopType.Yoyo) // infinito
            .SetUpdate(true)             // ignora Time.timeScale
            .SetAutoKill(false);
    }

    // --- MOVIMIENTO HACIA ARRIBA (con delay) ---

    private void StartMoveUpWithDelay()
    {
        if (moveTarget == null) return;

        // por si ya hubiese una corutina anterior
        if (_moveCoroutine != null)
            StopCoroutine(_moveCoroutine);

        _moveCoroutine = StartCoroutine(MoveUpCoroutine());
    }

    private IEnumerator MoveUpCoroutine()
    {
        // reset pos inicial antes de esperar
        moveTarget.anchoredPosition = _moveOriginalAnchoredPos;

        if (moveDelay > 0f)
        {
            // MUY IMPORTANTE: usamos WaitForSecondsRealtime porque el juego está pausado
            yield return new WaitForSecondsRealtime(moveDelay);
        }

        PlayMoveUp();
        _moveCoroutine = null;
    }

    private void PlayMoveUp()
    {
        if (moveTarget == null) return;

        _moveTween?.Kill();

        moveTarget.anchoredPosition = _moveOriginalAnchoredPos;
        Vector2 endPos = _moveOriginalAnchoredPos + new Vector2(0f, moveOffsetY);

        _moveTween = moveTarget
            .DOAnchorPos(endPos, moveDuration)
            .SetEase(moveEase)
            .SetUpdate(true)     // ignora Time.timeScale
            .SetAutoKill(false);
    }

    // --- FADE IN ---

    private void PlayFadeIn()
    {
        if (fadeImage == null) return;

        _fadeTween?.Kill();

        // alpha a 0
        var c = fadeImage.color;
        fadeImage.color = new Color(c.r, c.g, c.b, 0f);

        // tweeneamos solo el alpha
        _fadeTween = fadeImage
            .DOFade(1f, fadeDuration)
            .SetEase(fadeEase)
            .SetUpdate(true);     // ignora Time.timeScale
    }

    // --- LIMPIEZA ---

    private void KillTweens()
    {
        if (_bounceTween != null)
        {
            _bounceTween.Kill();
            _bounceTween = null;
        }
        if (_moveTween != null)
        {
            _moveTween.Kill();
            _moveTween = null;
        }
        if (_fadeTween != null)
        {
            _fadeTween.Kill();
            _fadeTween = null;
        }
    }

    // Si querés dispararlo manualmente desde código:
    public void RefreshUI()
    {
        UpdateTexts();
        PlayBounce();
        StartMoveUpWithDelay();  // mantiene el mismo comportamiento
        PlayFadeIn();
    }
}
