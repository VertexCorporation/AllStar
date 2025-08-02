using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public static event System.Action PlayerDied;
    public SpawnCubes spawner;
    public GameObject theWall;
    public GameObject theBallSprite;
    public GameObject movingBall;
    public AnimationClip jumpAnim;

    [HideInInspector] public float speedY;
    [HideInInspector] public bool isJumping = false;
    [HideInInspector] public bool isreadyForBigJump = false;
    [HideInInspector] public bool isCheckingCollision = false;

    private Animator ballAnimator;
    private SpriteRenderer ballSpriteRenderer;

    private float direction = -1;
    private float jumpDirection;

    private Vector2 newWavePosition;
    public Button[] specificButtons;
    private float t = 0;
    private float jumpTime;
    private const float threeFourthTheLengthOfTheSprite = 0.74f;
    private float temporaryXposition = 0;

    // --- REVERTED LOGIC ---
    // State flags for the original, automatic jump system.
    // The 'canJump' boolean is no longer necessary.
    private bool firstClick = true;
    private bool isReadyToPlay = false;
    private bool isReadyForNextJump = false;
    private bool isReadyForNextLoop = false;
    private bool isReadyToLeap = false;
    public float maxGameSpeed = 3.0f;
    public float acceleration = 0.75f;
    public float deceleration = 1.5f;

    void OnEnable()
    {
        GameManager.GameStateChanged += GameManager_GameStateChanged;
    }

    void OnDisable()
    {
        GameManager.GameStateChanged -= GameManager_GameStateChanged;
    }

    // --- REVERTED LOGIC ---
    // This now makes the very first jump automatic as soon as the game starts.
    void GameManager_GameStateChanged(GameState newState, GameState oldState)
    {
        if (newState == GameState.Playing && oldState == GameState.Prepare)
        {
            if (firstClick)
            {
                firstClick = false;
                isReadyToPlay = true;

                // When the first jump begins, ensure the character turns
                // to face the direction of movement (which is left, as direction = -1).
                if (ballSpriteRenderer != null)
                {
                    ballSpriteRenderer.flipX = (direction == -1);
                }

                isReadyForNextJump = true; // This makes the first jump automatic.
            }
        }
    }

    void Start()
    {
        ballAnimator = theBallSprite.GetComponent<Animator>();
        ballSpriteRenderer = theBallSprite.GetComponent<SpriteRenderer>();
        jumpTime = jumpAnim.length;
        newWavePosition = theWall.transform.position;

        if (CharacterManager.Instance != null)
        {
            ballSpriteRenderer.sprite = CharacterManager.Instance.character;
        }

        // --- CHANGE ---
        // Character now starts by visually facing right.
        ballSpriteRenderer.flipX = false;
    }


    void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (GameManager.Instance.GameState == GameState.Playing)
        {
            if (Input.GetMouseButtonDown(0) && !IsPointerOverSpecificButtons())
            {
                ChangeDirection();
            }

            HandleSpeedBoost();

            if (isReadyToPlay)
            {
                Leap();
            }
        }
    }


    // --- YENİ: Hızlanma ve yavaşlamayı yöneten yeni fonksiyon ---
    private void HandleSpeedBoost()
    {
        float targetSpeed;

        // Ekrana basılı tutuluyorsa (ve UI butonu üzerinde değilse) hedef hızı maksimuma ayarla
        if (Input.GetMouseButton(0) && !IsPointerOverSpecificButtons())
        {
            targetSpeed = maxGameSpeed;
        }
        else
        {
            // Parmak çekildiyse, hedef hızı GameManager'daki mevcut normal hıza ayarla
            targetSpeed = GameManager.Instance.gameSpeed;
        }

        // Oyunun hızını (Time.timeScale) hedef hıza doğru yumuşak bir şekilde ayarla
        // Hızlanma ve yavaşlama oranları farklı olabilir
        float currentRate = (Time.timeScale < targetSpeed) ? acceleration : deceleration;
        Time.timeScale = Mathf.MoveTowards(Time.timeScale, targetSpeed, currentRate * Time.unscaledDeltaTime);
        // ÖNEMLİ: Time.timeScale'i değiştirdiğimiz için Time.unscaledDeltaTime kullanıyoruz.
        // Bu, oyun yavaşladığında bile yavaşlamanın akıcı olmasını sağlar.
    }

    private void ChangeDirection()
    {
        direction *= -1;
        if (ballSpriteRenderer != null)
        {
            ballSpriteRenderer.flipX = (direction == -1);
        }
    }

    private bool IsPointerOverSpecificButtons()
    {
        foreach (var button in specificButtons)
        {
            if (button != null && button.gameObject.activeInHierarchy)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(button.GetComponent<RectTransform>(), Input.mousePosition, null))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // The Leap method now reads the automatically set 'isReadyForNextJump' flag.
    public void Leap()
    {
        if (!isCheckingCollision && isReadyForNextJump)
        {
            PrepareForJump();
        }

        if (isReadyForNextLoop)
        {
            PerformJump();
        }
        else if (isReadyToLeap)
        {
            PerformLeap();
        }
    }

    private void PrepareForJump()
    {
        temporaryXposition = transform.position.x;
        jumpDirection = direction;

        isJumping = true;
        isReadyForNextJump = false; // Consume the jump signal.

        if (isreadyForBigJump)
        {
            isReadyToLeap = true;
            ballAnimator.SetTrigger(jumpDirection == 1 ? "LeapRight" : "LeapLeft");
        }
        else
        {
            isReadyForNextLoop = true;
            ballAnimator.SetTrigger(jumpDirection == 1 ? "JumpRight" : "JumpLeft");
        }

        movingBall.transform.position = new Vector2(temporaryXposition, movingBall.transform.position.y);
    }

    private void PerformJump()
    {
        speedY = (GameManager.Instance.boundsY * threeFourthTheLengthOfTheSprite) / jumpTime;
        newWavePosition.y += speedY * Time.deltaTime;
        theWall.transform.position = newWavePosition;
        t += Time.deltaTime;

        if (t >= jumpTime)
        {
            FinishJump();
        }
    }

    private void PerformLeap()
    {
        isreadyForBigJump = false;

        speedY = (GameManager.Instance.boundsY * threeFourthTheLengthOfTheSprite) / jumpTime;
        newWavePosition.y += speedY * Time.deltaTime;
        theWall.transform.position = newWavePosition;
        t += Time.deltaTime;

        if (t >= jumpTime * 2)
        {
            FinishLeap();
        }
    }

    private void FinishJump()
    {
        ResetJumpState();
        transform.position = new Vector2(movingBall.transform.position.x + jumpDirection * (GameManager.Instance.boundsX / 2), transform.position.y);
        ballAnimator.ResetTrigger("JumpRight");
        ballAnimator.ResetTrigger("JumpLeft");
        movingBall.transform.position = transform.position;
    }

    private void FinishLeap()
    {
        ResetJumpState();
        transform.position = new Vector2(movingBall.transform.position.x + jumpDirection * (GameManager.Instance.boundsX / 2) * 2, transform.position.y);
        ballAnimator.ResetTrigger("LeapRight");
        ballAnimator.ResetTrigger("LeapLeft");
        movingBall.transform.position = transform.position;
    }

    // --- REVERTED LOGIC ---
    // This is the key change. Upon landing, the character is immediately ready for the next jump.
    private void ResetJumpState()
    {
        isJumping = false;
        isReadyForNextLoop = false;
        isReadyToLeap = false;
        isCheckingCollision = true;
        t = 0;

        isReadyForNextJump = true;
    }

    public void Die()
    {
        if (GameManager.Instance.GameState != GameState.GameOver)
        {
            PlayerDied?.Invoke();
        }
    }
}