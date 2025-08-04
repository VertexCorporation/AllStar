/***************************************************************************
 *  UIBlur.cs (Robust, Production-Ready Version)
 *  -----------------------------------------------------------------------
 *  This version has been perfected to handle Unity's material instancing
 *  behavior, which is a common source of bugs for runtime material property
 *  changes.
 *
 *  KEY FIX:
 *  - In the Awake() method, it now explicitly creates a UNIQUE INSTANCE of
 *    the material assigned to the Image component.
 *  - It then re-assigns this new instance back to the Image component.
 *  - All subsequent property changes (Intensity, Multiplier, etc.) are
 *    guaranteed to modify this specific instance, which the Image is
 *    actively rendering.
 *
 *  This prevents issues where animations might modify a temporary clone that
 *  gets discarded, especially when the GameObject is deactivated and
 *  re-activated. The blur effect is now guaranteed to be controllable
 *  at runtime by scripts like LoadingScreenManager.
 ***************************************************************************/

using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Krivodeling.UI.Effects
{
    [RequireComponent(typeof(Image))] // Ensures an Image component is always present.
    public class UIBlur : MonoBehaviour
    {
        #region Public Properties
        
        public Color Color { get => _color; set { _color = value; UpdateColor(); } }
#if UNITY_EDITOR
        public FlipMode EditorFlipMode { get => _editorFlipMode; set { _editorFlipMode = value; UpdateFlipMode(); } }
#endif
        public FlipMode BuildFlipMode { get => _buildFlipMode; set { _buildFlipMode = value; UpdateFlipMode(); } }
        public float Intensity { get => _intensity; set { _intensity = Mathf.Clamp01(value); UpdateIntensity(); } }
        public float Multiplier { get => _multiplier; set { _multiplier = Mathf.Clamp01(value); UpdateMultiplier(); } }
        public UnityEvent OnBeginBlur { get => _onBeginBlur; set => _onBeginBlur = value; }
        public UnityEvent OnEndBlur { get => _onEndBlur; set => _onEndBlur = value; }
        public BlurChangedEvent OnBlurChanged { get => _onBlurChanged; set => _onBlurChanged = value; }

        #endregion

        #region Serialized Fields
        
        [SerializeField]
        private Color _color = Color.white;
#if UNITY_EDITOR
        [SerializeField]
        private FlipMode _editorFlipMode;
#endif
        [SerializeField]
        private FlipMode _buildFlipMode;
        [SerializeField, Range(0f, 1f)]
        private float _intensity;
        [SerializeField, Range(0f, 1f)]
        private float _multiplier = 0.15f;
        [SerializeField]
        private UnityEvent _onBeginBlur;
        [SerializeField]
        private UnityEvent _onEndBlur;
        [SerializeField]
        private BlurChangedEvent _onBlurChanged;

        #endregion

        #region Private Fields
        
        // This will hold our unique, runtime-instance of the material.
        private Material _runtimeMaterial; 
        
        // Caching shader property IDs is a best practice for performance.
        private int _colorId;
        private int _flipXId;
        private int _flipYId;
        private int _intensityId;
        private int _multiplierId;

        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Get the required Image component.
            var imageComponent = GetComponent<Image>();
            if (imageComponent.material == null)
            {
                throw new NullReferenceException("UIBlur requires a Material to be assigned to the Image component in the Inspector.");
            }
            
            // CRITICAL FIX: Create a unique instance of the material for this specific UIBlur component.
            // This prevents conflicts with other objects using the same base material and ensures our changes stick.
            _runtimeMaterial = new Material(imageComponent.material);
            imageComponent.material = _runtimeMaterial;
            
            // Get the shader property IDs once during initialization.
            _colorId = Shader.PropertyToID("_Color");
            _flipXId = Shader.PropertyToID("_FlipX");
            _flipYId = Shader.PropertyToID("_FlipY");
            _intensityId = Shader.PropertyToID("_Intensity");
            _multiplierId = Shader.PropertyToID("_Multiplier");
        }
        
        private void Start()
        {
            // Apply the initial values set in the Inspector to our new material instance.
            SetBlur(Color, Intensity, _multiplier);
        }

        #endregion

        #region Public Methods

        public void UpdateBlur()
        {
            SetBlur(Color, Intensity, Multiplier);
            UpdateFlipMode();
        }

        public void SetBlur(Color color, float intensity, float multiplier)
        {
            Color = color;
            Intensity = intensity;
            Multiplier = multiplier;
        }

        public void BeginBlur(float speed)
        {
            StopAllCoroutines();
            StartCoroutine(BeginBlurCoroutine(speed));
        }

        public void EndBlur(float speed)
        {
            StopAllCoroutines();
            StartCoroutine(EndBlurCoroutine(speed));
        }
        
        #endregion
        
        #region Private Update Methods
        
        private void UpdateColor()
        {
            if (_runtimeMaterial == null) return;
            _runtimeMaterial.SetColor(_colorId, Color);
        }

        private void UpdateIntensity()
        {
            if (_runtimeMaterial == null) return;
            _runtimeMaterial.SetFloat(_intensityId, Intensity);
        }

        private void UpdateMultiplier()
        {
            if (_runtimeMaterial == null) return;
            _runtimeMaterial.SetFloat(_multiplierId, Multiplier);
        }

        private void UpdateFlipMode()
        {
            if (_runtimeMaterial == null) return;
            
#if UNITY_EDITOR
            if (Application.isPlaying) // Only use build flip mode when playing in editor
            {
                ApplyBuildFlipMode();
            }
            else // Use editor flip mode when not playing (for OnValidate)
            {
                ApplyEditorFlipMode();
            }
#else
            ApplyBuildFlipMode();
#endif
        }

        private void ApplyBuildFlipMode()
        {
            _runtimeMaterial.SetFloat(_flipXId, BuildFlipMode.HasFlag(FlipMode.X) ? 1f : 0f);
            _runtimeMaterial.SetFloat(_flipYId, BuildFlipMode.HasFlag(FlipMode.Y) ? 1f : 0f);
        }
        
#if UNITY_EDITOR
        private void ApplyEditorFlipMode()
        {
            _runtimeMaterial.SetFloat(_flipXId, EditorFlipMode.HasFlag(FlipMode.X) ? 1f : 0f);
            _runtimeMaterial.SetFloat(_flipYId, EditorFlipMode.HasFlag(FlipMode.Y) ? 1f : 0f);
        }
#endif
        
        #endregion

        #region Coroutines & Events
        
        private IEnumerator BeginBlurCoroutine(float speed)
        {
            OnBeginBlur?.Invoke();

            while (Intensity < 1f)
            {
                Intensity += speed * Time.deltaTime;
                OnBlurChanged.Invoke(Intensity);
                yield return null;
            }
        }

        private IEnumerator EndBlurCoroutine(float speed)
        {
            while (Intensity > 0f)
            {
                Intensity -= speed * Time.deltaTime;
                OnBlurChanged.Invoke(Intensity);
                yield return null;
            }

            OnEndBlur?.Invoke();
        }

        [Serializable]
        public class BlurChangedEvent : UnityEvent<float> { }

        #endregion
        
        #region Editor-Only Logic
#if UNITY_EDITOR
        private void OnValidate()
        {
            // OnValidate provides instant feedback in the editor when changing Inspector values.
            // It can run before Awake, so we need a safe way to get the material.
            var image = GetComponent<Image>();
            if (image != null && image.material != null)
            {
                 UpdateBlurInEditor(image.material);
            }
        }

        private void UpdateBlurInEditor(Material mat)
        {
            mat.SetColor("_Color", Color);
            mat.SetFloat("_FlipX", EditorFlipMode.HasFlag(FlipMode.X) ? 1f : 0f);
            mat.SetFloat("_FlipY", EditorFlipMode.HasFlag(FlipMode.Y) ? 1f : 0f);
            mat.SetFloat("_Intensity", Intensity);
            mat.SetFloat("_Multiplier", Multiplier);

            // This tells the editor that the material has been changed and needs to be saved.
            EditorUtility.SetDirty(mat);
        }
#endif
        #endregion
    }
}