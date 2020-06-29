﻿// Crest Ocean System

// This file is subject to the MIT License as seen in the root of this folder structure (LICENSE)

using UnityEngine;

namespace Crest
{
    /// <summary>
    /// The main script for the ocean system. Attach this to a GameObject to create an ocean. This script initializes the various data types and systems
    /// and moves/scales the ocean based on the viewpoint. It also hosts a number of global settings that can be tweaked here.
    /// </summary>
    public class OceanRenderer : MonoBehaviour
    {
        [Tooltip("The viewpoint which drives the ocean detail. Defaults to main camera."), SerializeField]
        Transform _viewpoint;
        public Transform Viewpoint { get { return _viewpoint; } set { _viewpoint = value; } }

        [Tooltip("Optional provider for time, can be used to hard-code time for automation, or provide server time. Defaults to local Unity time."), SerializeField]
        TimeProviderBase _timeProvider;
        public float CurrentTime => _timeProvider.CurrentTime;
        public float DeltaTime => _timeProvider.DeltaTime;
        public float DeltaTimeDynamics => _timeProvider.DeltaTimeDynamics;

        [Header("Ocean Params")]

        [SerializeField, Tooltip("Material to use for the ocean surface")]
        Material _material = null;
        public Material OceanMaterial { get { return _material; } }

        [SerializeField]
        string _layerName = "Water";
        public string LayerName { get { return _layerName; } }

        [SerializeField, Delayed, Tooltip("Multiplier for physics gravity."), Range(0f, 10f)]
        float _gravityMultiplier = 1f;
        public float Gravity { get { return _gravityMultiplier * Physics.gravity.magnitude; } }


        [Header("Detail Params")]

        [Range(2, 16)]
        [Tooltip("Min number of verts / shape texels per wave."), SerializeField]
        float _minTexelsPerWave = 3f;
        public float MinTexelsPerWave => _minTexelsPerWave;

        [Delayed, Tooltip("The smallest scale the ocean can be."), SerializeField]
        float _minScale = 8f;

        [Delayed, Tooltip("The largest scale the ocean can be (-1 for unlimited)."), SerializeField]
        float _maxScale = 256f;

        [Tooltip("Drops the height for maximum ocean detail based on waves. This means if there are big waves, max detail level is reached at a lower height, which can help visual range when there are very large waves and camera is at sea level."), SerializeField, Range(0f, 1f)]
        float _dropDetailHeightBasedOnWaves = 0.2f;

        [SerializeField, Delayed, Tooltip("Resolution of ocean LOD data. Use even numbers like 256 or 384. This is 4x the old 'Base Vert Density' param, so if you used 64 for this param, set this to 256.")]
        int _lodDataResolution = 256;
        public int LodDataResolution { get { return _lodDataResolution; } }

        [SerializeField, Delayed, Tooltip("How much of the water shape gets tessellated by geometry. If set to e.g. 4, every geometry quad will span 4x4 LOD data texels. Use power of 2 values like 1, 2, 4...")]
        int _geometryDownSampleFactor = 2;

        [SerializeField, Tooltip("Number of ocean tile scales/LODs to generate."), Range(2, LodDataMgr.MAX_LOD_COUNT)]
        int _lodCount = 7;


        [Header("Simulation Params")]

        public SimSettingsAnimatedWaves _simSettingsAnimatedWaves;

        [Tooltip("Water depth information used for shallow water, shoreline foam, wave attenuation, among others."), SerializeField]
        bool _createSeaFloorDepthData = true;
        public bool CreateSeaFloorDepthData { get { return _createSeaFloorDepthData; } }

        [Tooltip("Simulation of foam created in choppy water and dissipating over time."), SerializeField]
        bool _createFoamSim = true;
        public bool CreateFoamSim { get { return _createFoamSim; } }
        public SimSettingsFoam _simSettingsFoam;

        [Tooltip("Dynamic waves generated from interactions with objects such as boats."), SerializeField]
        bool _createDynamicWaveSim = false;
        public bool CreateDynamicWaveSim { get { return _createDynamicWaveSim; } }
        public SimSettingsWave _simSettingsDynamicWaves;

        [Tooltip("Horizontal motion of water body, akin to water currents."), SerializeField]
        bool _createFlowSim = false;
        public bool CreateFlowSim { get { return _createFlowSim; } }
        public SimSettingsFlow _simSettingsFlow;

        [Tooltip("Shadow information used for lighting water."), SerializeField]
        bool _createShadowData = false;
        public bool CreateShadowData { get { return _createShadowData; } }
        [Tooltip("The primary directional light. Required if shadowing is enabled.")]
        public Light _primaryLight;
        public SimSettingsShadow _simSettingsShadow;


        [Header("Debug Params")]

        [Tooltip("Whether to generate ocean geometry tiles uniformly (with overlaps).")]
        public bool _uniformTiles = false;
        [Tooltip("Disable generating a wide strip of triangles at the outer edge to extend ocean to edge of view frustum.")]
        public bool _disableSkirt = false;
        [Tooltip("Move ocean with viewpoint.")]
        public bool _followViewpoint = true;

        /// <summary>
        /// Current ocean scale (changes with viewer altitude).
        /// </summary>
        public float Scale { get; private set; }
        public float CalcLodScale(float lodIndex) { return Scale * Mathf.Pow(2f, lodIndex); }
        public float CalcGridSize(int lodIndex) { return CalcLodScale(lodIndex) / LodDataResolution; }

        /// <summary>
        /// The ocean changes scale when viewer changes altitude, this gives the interpolation param between scales.
        /// </summary>
        public float ViewerAltitudeLevelAlpha { get; private set; }

        /// <summary>
        /// Sea level is given by y coordinate of GameObject with OceanRenderer script.
        /// </summary>
        public float SeaLevel { get { return transform.position.y; } }

        [HideInInspector] public LodTransform _lodTransform;
        [HideInInspector] public LodDataMgrAnimWaves _lodDataAnimWaves;
        [HideInInspector] public LodDataMgrSeaFloorDepth _lodDataSeaDepths;
        [HideInInspector] public LodDataMgrDynWaves _lodDataDynWaves;
        [HideInInspector] public LodDataMgrFlow _lodDataFlow;
        [HideInInspector] public LodDataMgrFoam _lodDataFoam;
        [HideInInspector] public LodDataMgrShadow _lodDataShadow;
        /// <summary>
        /// The number of LODs/scales that the ocean is currently using.
        /// </summary>
        public int CurrentLodCount { get { return _lodTransform.LodCount; } }

        /// <summary>
        /// Vertical offset of viewer vs water surface
        /// </summary>
        public float ViewerHeightAboveWater { get; private set; }

        static int sp_crestTime = Shader.PropertyToID("_CrestTime");
        static int sp_texelsPerWave = Shader.PropertyToID("_TexelsPerWave");

        SamplingData _samplingData = new SamplingData();

        void Awake()
        {
            if (!VerifyRequirements())
            {
                enabled = false;
                return;
            }

            Instance = this;
            Scale = Mathf.Clamp(Scale, _minScale, _maxScale);

            OceanBuilder.GenerateMesh(this, _lodDataResolution, _geometryDownSampleFactor, _lodCount);

            if (null == GetComponent<BuildCommandBufferBase>())
            {
                gameObject.AddComponent<BuildCommandBuffer>();
            }

            InitViewpoint();
            InitTimeProvider();
        }

        bool VerifyRequirements()
        {
            if (_material == null)
            {
                Debug.LogError("A material for the ocean must be assigned on the Material property of the OceanRenderer.", this);
                return false;
            }
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogError("Crest requires graphics devices that support compute shaders.", this);
                return false;
            }
            if (!SystemInfo.supports2DArrayTextures)
            {
                Debug.LogError("Crest requires graphics devices that support 2D array textures.", this);
                return false;
            }

            return true;
        }

        void InitViewpoint()
        {
            if (_viewpoint == null)
            {
                var camMain = Camera.main;
                if (camMain != null)
                {
                    _viewpoint = camMain.transform;
                }
                else
                {
                    Debug.LogError("Please provide the viewpoint transform, or tag the primary camera as MainCamera.", this);
                }
            }
        }

        void InitTimeProvider()
        {
            // Used assigned time provider, or use one attached to this game object
            if (_timeProvider == null && (_timeProvider = GetComponent<TimeProviderBase>()) == null)
            {
                // None found - create
                _timeProvider = gameObject.AddComponent<TimeProviderDefault>();
            }
        }

        void Update()
        {
            UpdateCollision();
            
        }

        void UpdateCollision()
        {
            if (_simSettingsAnimatedWaves.CachedHeightQueries)
            {
                (CollisionProvider as CollProviderCache).ClearCache();
            }
        }

        void LateUpdate()
        {
            // set global shader params
            Shader.SetGlobalFloat(sp_texelsPerWave, MinTexelsPerWave);
            Shader.SetGlobalFloat(sp_crestTime, CurrentTime);

            if (_viewpoint == null)
            {
                Debug.LogError("_viewpoint is null, ocean update will fail.", this);
            }

            if (_followViewpoint)
            {
                LateUpdatePosition();
                LateUpdateScale();
                LateUpdateViewerHeight();
            }

            LateUpdateLods();
        }

        void LateUpdatePosition()
        {
            Vector3 pos = _viewpoint.position;

            // maintain y coordinate - sea level
            pos.y = transform.position.y;

            transform.position = pos;

            Shader.SetGlobalVector("_OceanCenterPosWorld", transform.position);
        }

        void LateUpdateScale()
        {
            // reach maximum detail at slightly below sea level. this should combat cases where visual range can be lost
            // when water height is low and camera is suspended in air. i tried a scheme where it was based on difference
            // to water height but this does help with the problem of horizontal range getting limited at bad times.
            float maxDetailY = SeaLevel - _maxVertDispFromWaves * _dropDetailHeightBasedOnWaves;
            float camDistance = Mathf.Abs(_viewpoint.position.y - maxDetailY);

            // offset level of detail to keep max detail in a band near the surface
            camDistance = Mathf.Max(camDistance - 4f, 0f);

            // scale ocean mesh based on camera distance to sea level, to keep uniform detail.
            const float HEIGHT_LOD_MUL = 1f;
            float level = camDistance * HEIGHT_LOD_MUL;
            level = Mathf.Max(level, _minScale);
            if (_maxScale != -1f) level = Mathf.Min(level, 1.99f * _maxScale);

            float l2 = Mathf.Log(level) / Mathf.Log(2f);
            float l2f = Mathf.Floor(l2);

            ViewerAltitudeLevelAlpha = l2 - l2f;

            Scale = Mathf.Pow(2f, l2f);
            transform.localScale = new Vector3(Scale, 1f, Scale);
        }

        void LateUpdateViewerHeight()
        {
            var pos = Viewpoint.position;
            var rect = new Rect(pos.x, pos.z, 0f, 0f);

            float waterHeight;
            if (CollisionProvider.GetSamplingData(ref rect, 0f, _samplingData)
                && CollisionProvider.SampleHeight(ref pos, _samplingData, out waterHeight))
            {
                ViewerHeightAboveWater = pos.y - waterHeight;
            }

            CollisionProvider.ReturnSamplingData(_samplingData);
        }
        
        void LateUpdateLods()
        {
            // Do any per-frame update for each LOD type.

            _lodTransform.UpdateTransforms();

            if (_lodDataAnimWaves) _lodDataAnimWaves.UpdateLodData();
            if (_lodDataDynWaves) _lodDataDynWaves.UpdateLodData();
            if (_lodDataFlow) _lodDataFlow.UpdateLodData();
            if (_lodDataFoam) _lodDataFoam.UpdateLodData();
            if (_lodDataSeaDepths) _lodDataSeaDepths.UpdateLodData();
            if (_lodDataShadow) _lodDataShadow.UpdateLodData();
        }

        /// <summary>
        /// Could the ocean horizontal scale increase (for e.g. if the viewpoint gains altitude). Will be false if ocean already at maximum scale.
        /// </summary>
        public bool ScaleCouldIncrease { get { return _maxScale == -1f || transform.localScale.x < _maxScale * 0.99f; } }
        /// <summary>
        /// Could the ocean horizontal scale decrease (for e.g. if the viewpoint drops in altitude). Will be false if ocean already at minimum scale.
        /// </summary>
        public bool ScaleCouldDecrease { get { return _minScale == -1f || transform.localScale.x > _minScale * 1.01f; } }

        /// <summary>
        /// User shape inputs can report in how far they might displace the shape horizontally and vertically. The max value is
        /// saved here. Later the bounding boxes for the ocean tiles will be expanded to account for this potential displacement.
        /// </summary>
        public void ReportMaxDisplacementFromShape(float maxHorizDisp, float maxVertDisp, float maxVertDispFromWaves)
        {
            if (Time.frameCount != _maxDisplacementCachedTime)
            {
                _maxHorizDispFromShape = _maxVertDispFromShape = _maxVertDispFromWaves = 0f;
            }

            _maxHorizDispFromShape += maxHorizDisp;
            _maxVertDispFromShape += maxVertDisp;
            _maxVertDispFromWaves += maxVertDispFromWaves;

            _maxDisplacementCachedTime = Time.frameCount;
        }
        float _maxHorizDispFromShape = 0f;
        float _maxVertDispFromShape = 0f;
        float _maxVertDispFromWaves = 0f;
        int _maxDisplacementCachedTime = 0;
        /// <summary>
        /// The maximum horizontal distance that the shape scripts are displacing the shape.
        /// </summary>
        public float MaxHorizDisplacement { get { return _maxHorizDispFromShape; } }
        /// <summary>
        /// The maximum height that the shape scripts are displacing the shape.
        /// </summary>
        public float MaxVertDisplacement { get { return _maxVertDispFromShape; } }

        public static OceanRenderer Instance { get; private set; }

        /// <summary>
        /// Provides ocean shape to CPU.
        /// </summary>
        ICollProvider _collProvider;
        public ICollProvider CollisionProvider { get { return _collProvider != null ? _collProvider : (_collProvider = _simSettingsAnimatedWaves.CreateCollisionProvider()); } }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Must be at least 0.25, and must be on a power of 2
            _minScale = Mathf.Pow(2f, Mathf.Round(Mathf.Log(Mathf.Max(_minScale, 0.25f), 2f)));

            // Max can be -1 which means no maximum
            if (_maxScale != -1f)
            {
                // otherwise must be at least 0.25, and must be on a power of 2
                _maxScale = Mathf.Pow(2f, Mathf.Round(Mathf.Log(Mathf.Max(_maxScale, _minScale), 2f)));
            }

            // Gravity 0 makes waves freeze which is weird but doesn't seem to break anything so allowing this for now
            _gravityMultiplier = Mathf.Max(_gravityMultiplier, 0f);

            // LOD data resolution multiple of 2 for general GPU texture reasons (like pixel quads)
            _lodDataResolution -= _lodDataResolution % 2;

            _geometryDownSampleFactor = Mathf.ClosestPowerOfTwo(Mathf.Max(_geometryDownSampleFactor, 1));

            var remGeo = _lodDataResolution % _geometryDownSampleFactor;
            if (remGeo > 0)
            {
                var newLDR = _lodDataResolution - (_lodDataResolution % _geometryDownSampleFactor);
                Debug.LogWarning("Adjusted Lod Data Resolution from " + _lodDataResolution + " to " + newLDR + " to ensure the Geometry Down Sample Factor is a factor (" + _geometryDownSampleFactor + ").", this);
                _lodDataResolution = newLDR;
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnReLoadScripts()
        {
            Instance = FindObjectOfType<OceanRenderer>();
        }
#endif
    }
}
