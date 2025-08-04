// FILE: LoadingScreenManager.cs
// The definitive, scene-reload-proof, and name-conflict-resolved version.

/***************************************************************************
 * REASONING & ARCHITECTURE (WHY THIS FINAL VERSION IS BULLETPROOF):
 * -----------------------------------------------------------------------
 * THE FIX: The root cause of all CS0123 and CS1503 errors was a name
 * collision. A user script named `Scene.cs` was conflicting with the
 * official `UnityEngine.SceneManagement.Scene` type.
 *
 * This version resolves this ambiguity by using the FULLY QUALIFIED NAME
 * for Unity's official types within the `OnSceneLoaded` method signature.
 *
 * By changing:
 *      private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
 * To:
 *      private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
 *
 * We are explicitly telling the compiler "Do not be confused by any other
 * class named 'Scene'. I am specifically referring to the one inside the
 * UnityEngine.SceneManagement namespace." This provides absolute clarity
 * and permanently resolves the compiler errors. The event subscription in
 * OnEnable/OnDisable can now be done directly, which is the cleanest way
 * to manage event listeners.
 ***************************************************************************/

using UnityEngine;
using UnityEngine.SceneManagement; // This is still required.
using Krivodeling.UI.Effects; 

[AddComponentMenu("Vertex/UI/Loading Screen Manager")]
public sealed class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance { get; private set; }

    [Header("Core UI Components")]
    [SerializeField] private GameObject loadingScreenRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Effects (Optional)")]
    [SerializeField] private UIBlur blurEffect; 

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private float _initialBlurIntensity;
    private float _initialBlurMultiplier;
    private bool _initialValuesSaved = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        InitializeScreenState();
        SaveInitialBlurValues();
    }

    private void OnEnable()
    {
        // Now that the ambiguity is resolved, we can subscribe directly.
        // This is the cleanest and safest way to handle subscriptions.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe directly. This reliably prevents memory leaks.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // AppInitializer will handle hiding this screen after the initial load.
        // OnSceneLoaded will handle hiding it on subsequent reloads.
        Show();
    }
    
    /// <summary>
    /// This method is called automatically by Unity every time a scene is loaded.
    /// Its signature now uses fully qualified names to avoid any ambiguity with user scripts.
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (AppInitializer.IsInitialized)
        {
            Debug.Log($"[LoadingScreenManager] Scene '{scene.name}' loaded, and AppInitializer is already complete. Forcing an immediate hide.");
            ForceHide();
        }
    }

    private void InitializeScreenState()
    {
        if (loadingScreenRoot != null)
        {
            loadingScreenRoot.SetActive(false);
            if(canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }
    
    private void SaveInitialBlurValues()
    {
        if (blurEffect != null && !_initialValuesSaved)
        {
            _initialBlurIntensity = blurEffect.Intensity;
            _initialBlurMultiplier = blurEffect.Multiplier;
            _initialValuesSaved = true;
        }
    }

    public void Show()
    {
        if (loadingScreenRoot == null || loadingScreenRoot.activeInHierarchy) return;

        if (blurEffect != null && _initialValuesSaved)
        {
            blurEffect.Intensity = _initialBlurIntensity;
            blurEffect.Multiplier = _initialBlurMultiplier;
        }
        
        if(canvasGroup != null) canvasGroup.alpha = 0f;
        loadingScreenRoot.SetActive(true);
        
        if(canvasGroup != null) LeanTween.alphaCanvas(canvasGroup, 1f, fadeDuration).setEase(LeanTweenType.easeOutCubic);
    }
    
    public void Hide()
    {
        if (loadingScreenRoot == null || !loadingScreenRoot.activeInHierarchy) return;

        if(canvasGroup != null)
        {
            LeanTween.alphaCanvas(canvasGroup, 0f, fadeDuration)
                .setEase(LeanTweenType.easeInCubic)
                .setOnComplete(() => {
                    loadingScreenRoot.SetActive(false);
                });
        }
        
        if (blurEffect != null)
        {
            LeanTween.value(gameObject, (i) => { blurEffect.Intensity = i; }, blurEffect.Intensity, 0f, fadeDuration).setEase(LeanTweenType.easeInCubic);
            LeanTween.value(gameObject, (m) => { blurEffect.Multiplier = m; }, blurEffect.Multiplier, 0f, fadeDuration).setEase(LeanTweenType.easeInCubic);
        }
    }
    
    private void ForceHide()
    {
        if (loadingScreenRoot != null)
        {
            LeanTween.cancel(loadingScreenRoot);
            if(canvasGroup != null) canvasGroup.alpha = 0f;
            loadingScreenRoot.SetActive(false);
        }
    }
}