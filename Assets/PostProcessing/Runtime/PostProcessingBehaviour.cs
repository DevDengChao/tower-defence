using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Rendering;

namespace UnityEngine.PostProcessing
{
    using DebugMode = BuiltinDebugViewsModel.Mode;

#if UNITY_5_4_OR_NEWER
    [ImageEffectAllowedInSceneView]
#endif
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent, ExecuteInEditMode]
    [AddComponentMenu("Effects/Post-Processing Behaviour", -1)]
    public class PostProcessingBehaviour : MonoBehaviour
    {
        // Inspector fields
        public PostProcessingProfile profile;

        public Func<Vector2, Matrix4x4> jitteredMatrixFunc;

        // Internal helpers
        private Dictionary<Type, KeyValuePair<CameraEvent, CommandBuffer>> _mCommandBuffers;
        private List<PostProcessingComponentBase> _mComponents;
        private Dictionary<PostProcessingComponentBase, bool> _mComponentStates;

        private MaterialFactory _mMaterialFactory;
        private RenderTextureFactory _mRenderTextureFactory;
        private PostProcessingContext _mContext;
        private Camera _mCamera;
        private PostProcessingProfile _mPreviousProfile;

        private bool _mRenderingInSceneView;

        // Effect components
        private BuiltinDebugViewsComponent _mDebugViews;
        private AmbientOcclusionComponent _mAmbientOcclusion;
        private ScreenSpaceReflectionComponent _mScreenSpaceReflection;
        private FogComponent _mFogComponent;
        private MotionBlurComponent _mMotionBlur;
        private TaaComponent _mTaa;
        private EyeAdaptationComponent _mEyeAdaptation;
        private DepthOfFieldComponent _mDepthOfField;
        private BloomComponent _mBloom;
        private ChromaticAberrationComponent _mChromaticAberration;
        private ColorGradingComponent _mColorGrading;
        private UserLutComponent _mUserLut;
        private GrainComponent _mGrain;
        private VignetteComponent _mVignette;
        private DitheringComponent _mDithering;
        private FxaaComponent _mFxaa;

        private void OnEnable()
        {
            _mCommandBuffers = new Dictionary<Type, KeyValuePair<CameraEvent, CommandBuffer>>();
            _mMaterialFactory = new MaterialFactory();
            _mRenderTextureFactory = new RenderTextureFactory();
            _mContext = new PostProcessingContext();

            // Keep a list of all post-fx for automation purposes
            _mComponents = new List<PostProcessingComponentBase>();

            // Component list
            _mDebugViews = AddComponent(new BuiltinDebugViewsComponent());
            _mAmbientOcclusion = AddComponent(new AmbientOcclusionComponent());
            _mScreenSpaceReflection = AddComponent(new ScreenSpaceReflectionComponent());
            _mFogComponent = AddComponent(new FogComponent());
            _mMotionBlur = AddComponent(new MotionBlurComponent());
            _mTaa = AddComponent(new TaaComponent());
            _mEyeAdaptation = AddComponent(new EyeAdaptationComponent());
            _mDepthOfField = AddComponent(new DepthOfFieldComponent());
            _mBloom = AddComponent(new BloomComponent());
            _mChromaticAberration = AddComponent(new ChromaticAberrationComponent());
            _mColorGrading = AddComponent(new ColorGradingComponent());
            _mUserLut = AddComponent(new UserLutComponent());
            _mGrain = AddComponent(new GrainComponent());
            _mVignette = AddComponent(new VignetteComponent());
            _mDithering = AddComponent(new DitheringComponent());
            _mFxaa = AddComponent(new FxaaComponent());

            // Prepare state observers
            _mComponentStates = new Dictionary<PostProcessingComponentBase, bool>();

            foreach (var component in _mComponents)
                _mComponentStates.Add(component, false);

            useGUILayout = false;
        }

        private void OnPreCull()
        {
            // All the per-frame initialization logic has to be done in OnPreCull instead of Update
            // because [ImageEffectAllowedInSceneView] doesn't trigger Update events...

            _mCamera = GetComponent<Camera>();

            if (profile == null || _mCamera == null)
                return;

#if UNITY_EDITOR
            // Track the scene view camera to disable some effects we don't want to see in the
            // scene view
            // Currently disabled effects :
            //  - Temporal Antialiasing
            //  - Depth of Field
            //  - Motion blur
            _mRenderingInSceneView = SceneView.currentDrawingSceneView != null
                                     && SceneView.currentDrawingSceneView.camera == _mCamera;
#endif

            // Prepare context
            var context = _mContext.Reset();
            context.profile = profile;
            context.renderTextureFactory = _mRenderTextureFactory;
            context.materialFactory = _mMaterialFactory;
            context.camera = _mCamera;

            // Prepare components
            _mDebugViews.Init(context, profile.debugViews);
            _mAmbientOcclusion.Init(context, profile.ambientOcclusion);
            _mScreenSpaceReflection.Init(context, profile.screenSpaceReflection);
            _mFogComponent.Init(context, profile.fog);
            _mMotionBlur.Init(context, profile.motionBlur);
            _mTaa.Init(context, profile.antialiasing);
            _mEyeAdaptation.Init(context, profile.eyeAdaptation);
            _mDepthOfField.Init(context, profile.depthOfField);
            _mBloom.Init(context, profile.bloom);
            _mChromaticAberration.Init(context, profile.chromaticAberration);
            _mColorGrading.Init(context, profile.colorGrading);
            _mUserLut.Init(context, profile.userLut);
            _mGrain.Init(context, profile.grain);
            _mVignette.Init(context, profile.vignette);
            _mDithering.Init(context, profile.dithering);
            _mFxaa.Init(context, profile.antialiasing);

            // Handles profile change and 'enable' state observers
            if (_mPreviousProfile != profile)
            {
                DisableComponents();
                _mPreviousProfile = profile;
            }

            CheckObservers();

            // Find out which camera flags are needed before rendering begins
            // Note that motion vectors will only be available one frame after being enabled
            var flags = DepthTextureMode.None;
            foreach (var component in _mComponents)
            {
                if (component.active)
                    flags |= component.GetCameraFlags();
            }

            context.camera.depthTextureMode = flags;

            // Temporal antialiasing jittering, needs to happen before culling
            if (!_mRenderingInSceneView && _mTaa.active && !profile.debugViews.willInterrupt)
                _mTaa.SetProjectionMatrix(jitteredMatrixFunc);
        }

        private void OnPreRender()
        {
            if (profile == null)
                return;

            // Command buffer-based effects should be set-up here
            TryExecuteCommandBuffer(_mDebugViews);
            TryExecuteCommandBuffer(_mAmbientOcclusion);
            TryExecuteCommandBuffer(_mScreenSpaceReflection);
            TryExecuteCommandBuffer(_mFogComponent);

            if (!_mRenderingInSceneView)
                TryExecuteCommandBuffer(_mMotionBlur);
        }

        private void OnPostRender()
        {
            if (profile == null || _mCamera == null)
                return;

            if (!_mRenderingInSceneView && _mTaa.active && !profile.debugViews.willInterrupt)
                _mContext.camera.ResetProjectionMatrix();
        }

        // Classic render target pipeline for RT-based effects
        // Note that any effect that happens after this stack will work in LDR
        [ImageEffectTransformsToLDR]
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (profile == null || _mCamera == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // Uber shader setup
            var uberActive = false;
            var fxaaActive = _mFxaa.active;
            var taaActive = _mTaa.active && !_mRenderingInSceneView;
            var dofActive = _mDepthOfField.active && !_mRenderingInSceneView;

            var uberMaterial = _mMaterialFactory.Get("Hidden/Post FX/Uber Shader");
            uberMaterial.shaderKeywords = null;

            var src = source;
            var dst = destination;

            if (taaActive)
            {
                var tempRt = _mRenderTextureFactory.Get(src);
                _mTaa.Render(src, tempRt);
                src = tempRt;
            }

#if UNITY_EDITOR
            // Render to a dedicated target when monitors are enabled so they can show information
            // about the final render.
            // At runtime the output will always be the backbuffer or whatever render target is
            // currently set on the camera.
            if (profile.monitors.onFrameEndEditorOnly != null)
                dst = _mRenderTextureFactory.Get(src);
#endif

            Texture autoExposure = GraphicsUtils.whiteTexture;
            if (_mEyeAdaptation.active)
            {
                uberActive = true;
                autoExposure = _mEyeAdaptation.Prepare(src, uberMaterial);
            }

            uberMaterial.SetTexture(AutoExposure, autoExposure);

            if (dofActive)
            {
                uberActive = true;
                _mDepthOfField.Prepare(src, uberMaterial, taaActive, _mTaa.jitterVector,
                    _mTaa.model.settings.taaSettings.motionBlending);
            }

            if (_mBloom.active)
            {
                uberActive = true;
                _mBloom.Prepare(src, uberMaterial, autoExposure);
            }

            uberActive |= TryPrepareUberImageEffect(_mChromaticAberration, uberMaterial);
            uberActive |= TryPrepareUberImageEffect(_mColorGrading, uberMaterial);
            uberActive |= TryPrepareUberImageEffect(_mVignette, uberMaterial);
            uberActive |= TryPrepareUberImageEffect(_mUserLut, uberMaterial);

            var fxaaMaterial = fxaaActive
                ? _mMaterialFactory.Get("Hidden/Post FX/FXAA")
                : null;

            if (fxaaActive)
            {
                fxaaMaterial.shaderKeywords = null;
                TryPrepareUberImageEffect(_mGrain, fxaaMaterial);
                TryPrepareUberImageEffect(_mDithering, fxaaMaterial);

                if (uberActive)
                {
                    var output = _mRenderTextureFactory.Get(src);
                    Graphics.Blit(src, output, uberMaterial, 0);
                    src = output;
                }

                _mFxaa.Render(src, dst);
            }
            else
            {
                uberActive |= TryPrepareUberImageEffect(_mGrain, uberMaterial);
                uberActive |= TryPrepareUberImageEffect(_mDithering, uberMaterial);

                if (uberActive)
                {
                    if (!GraphicsUtils.isLinearColorSpace)
                        uberMaterial.EnableKeyword("UNITY_COLORSPACE_GAMMA");

                    Graphics.Blit(src, dst, uberMaterial, 0);
                }
            }

            if (!uberActive && !fxaaActive)
                Graphics.Blit(src, dst);

#if UNITY_EDITOR
            if (profile.monitors.onFrameEndEditorOnly != null)
            {
                Graphics.Blit(dst, destination);

                var oldRt = RenderTexture.active;
                profile.monitors.onFrameEndEditorOnly(dst);
                RenderTexture.active = oldRt;
            }
#endif

            _mRenderTextureFactory.ReleaseAll();
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (profile == null || _mCamera == null)
                return;

            if (_mEyeAdaptation.active && profile.debugViews.IsModeActive(DebugMode.EyeAdaptation))
                _mEyeAdaptation.OnGUI();
            else if (_mColorGrading.active && profile.debugViews.IsModeActive(DebugMode.LogLut))
                _mColorGrading.OnGUI();
            else if (_mUserLut.active && profile.debugViews.IsModeActive(DebugMode.UserLut))
                _mUserLut.OnGUI();
        }

        private void OnDisable()
        {
            // Clear command buffers
            foreach (var cb in _mCommandBuffers.Values)
            {
                _mCamera.RemoveCommandBuffer(cb.Key, cb.Value);
                cb.Value.Dispose();
            }

            _mCommandBuffers.Clear();

            // Clear components
            if (profile != null)
                DisableComponents();

            _mComponents.Clear();

            // Reset camera mode
            if (_mCamera != null)
                _mCamera.depthTextureMode = DepthTextureMode.None;

            // Factories
            _mMaterialFactory.Dispose();
            _mRenderTextureFactory.Dispose();
            GraphicsUtils.Dispose();
        }

        public void ResetTemporalEffects()
        {
            _mTaa.ResetHistory();
            _mMotionBlur.ResetHistory();
            _mEyeAdaptation.ResetHistory();
        }

        #region State management

        private List<PostProcessingComponentBase> _mComponentsToEnable = new List<PostProcessingComponentBase>();
        private List<PostProcessingComponentBase> _mComponentsToDisable = new List<PostProcessingComponentBase>();
        private static readonly int AutoExposure = Shader.PropertyToID("_AutoExposure");

        private void CheckObservers()
        {
            foreach (var cs in _mComponentStates)
            {
                var component = cs.Key;
                var state = component.GetModel().enabled;

                if (state != cs.Value)
                {
                    if (state) _mComponentsToEnable.Add(component);
                    else _mComponentsToDisable.Add(component);
                }
            }

            for (var i = 0; i < _mComponentsToDisable.Count; i++)
            {
                var c = _mComponentsToDisable[i];
                _mComponentStates[c] = false;
                c.OnDisable();
            }

            for (var i = 0; i < _mComponentsToEnable.Count; i++)
            {
                var c = _mComponentsToEnable[i];
                _mComponentStates[c] = true;
                c.OnEnable();
            }

            _mComponentsToDisable.Clear();
            _mComponentsToEnable.Clear();
        }

        private void DisableComponents()
        {
            foreach (var component in _mComponents)
            {
                var model = component.GetModel();
                if (model != null && model.enabled)
                    component.OnDisable();
            }
        }

        #endregion

        #region Command buffer handling & rendering helpers

        // Placeholders before the upcoming Scriptable Render Loop as command buffers will be
        // executed on the go so we won't need of all that stuff
        private CommandBuffer AddCommandBuffer<T>(CameraEvent evt, string bufferName)
            where T : PostProcessingModel
        {
            var cb = new CommandBuffer {name = bufferName};
            var kvp = new KeyValuePair<CameraEvent, CommandBuffer>(evt, cb);
            _mCommandBuffers.Add(typeof(T), kvp);
            _mCamera.AddCommandBuffer(evt, kvp.Value);
            return kvp.Value;
        }

        private void RemoveCommandBuffer<T>()
            where T : PostProcessingModel
        {
            KeyValuePair<CameraEvent, CommandBuffer> kvp;
            var type = typeof(T);

            if (!_mCommandBuffers.TryGetValue(type, out kvp))
                return;

            _mCamera.RemoveCommandBuffer(kvp.Key, kvp.Value);
            _mCommandBuffers.Remove(type);
            kvp.Value.Dispose();
        }

        private CommandBuffer GetCommandBuffer<T>(CameraEvent evt, string bufferName)
            where T : PostProcessingModel
        {
            CommandBuffer cb;
            KeyValuePair<CameraEvent, CommandBuffer> kvp;

            if (!_mCommandBuffers.TryGetValue(typeof(T), out kvp))
            {
                cb = AddCommandBuffer<T>(evt, bufferName);
            }
            else if (kvp.Key != evt)
            {
                RemoveCommandBuffer<T>();
                cb = AddCommandBuffer<T>(evt, bufferName);
            }
            else cb = kvp.Value;

            return cb;
        }

        private void TryExecuteCommandBuffer<T>(PostProcessingComponentCommandBuffer<T> component)
            where T : PostProcessingModel
        {
            if (component.active)
            {
                var cb = GetCommandBuffer<T>(component.GetCameraEvent(), component.GetName());
                cb.Clear();
                component.PopulateCommandBuffer(cb);
            }
            else RemoveCommandBuffer<T>();
        }

        private bool TryPrepareUberImageEffect<T>(PostProcessingComponentRenderTexture<T> component, Material material)
            where T : PostProcessingModel
        {
            if (!component.active)
                return false;

            component.Prepare(material);
            return true;
        }

        private T AddComponent<T>(T component)
            where T : PostProcessingComponentBase
        {
            _mComponents.Add(component);
            return component;
        }

        #endregion
    }
}