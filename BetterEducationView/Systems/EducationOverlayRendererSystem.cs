using System;
using BetterEducationInfoView.Jobs;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace BetterEducationInfoView.Systems
{
    public partial class EducationOverlayRendererSystem : SystemBase
    {
        private OverlayRenderSystem m_OverlayRenderSystem;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private ToolSystem m_ToolSystem;
        private EntityQuery m_SchoolQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_OverlayRenderSystem = World.GetExistingSystemManaged<OverlayRenderSystem>();
            m_CameraUpdateSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();

            m_SchoolQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Game.Buildings.School>(),
                    ComponentType.ReadOnly<Game.Buildings.Student>(),
                    ComponentType.ReadOnly<PrefabRef>(),
                    ComponentType.ReadOnly<Game.Objects.Transform>()
                },
                None = new[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });
        }

        protected override void OnUpdate()
        {
            var setting = Setting.Instance;
            if (setting != null && !setting.OverlayEnabled)
            {
                return;
            }

            if (!IsEducationInfoviewActive())
            {
                return;
            }

            if (m_SchoolQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            float3 cameraRight = new float3(1f, 0f, 0f);
            float3 cameraUp = new float3(0f, 1f, 0f);
            float3 cameraPosition = float3.zero;

            var camera = UnityEngine.Camera.main;
            if (camera != null)
            {
                cameraRight = camera.transform.right;
                cameraUp = camera.transform.up;
                cameraPosition = camera.transform.position;
            }

            int yellowThreshold = math.clamp(setting?.YellowThreshold ?? 70, 1, 149);
            int redThreshold = math.clamp(setting?.RedThreshold ?? 90, yellowThreshold + 1, 150);
            bool hideEmptySchools = setting?.HideEmptySchools ?? false;
            float uiScale = math.clamp(setting?.OverlayScale ?? 140, 50, 200) / 100f;
            float zoomLevel = m_CameraUpdateSystem != null ? m_CameraUpdateSystem.zoom : 5000f;

            OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out JobHandle renderDeps);
            var drawJob = new DrawSchoolCapacityOverlayJob
            {
                OverlayBuffer = buffer,
                EntityType = SystemAPI.GetEntityTypeHandle(),
                TransformType = SystemAPI.GetComponentTypeHandle<Game.Objects.Transform>(true),
                PrefabRefType = SystemAPI.GetComponentTypeHandle<PrefabRef>(true),
                StudentBufferType = SystemAPI.GetBufferTypeHandle<Game.Buildings.Student>(true),
                PrefabRefLookup = SystemAPI.GetComponentLookup<PrefabRef>(true),
                SchoolDataLookup = SystemAPI.GetComponentLookup<SchoolData>(true),
                InstalledUpgradeLookup = SystemAPI.GetBufferLookup<InstalledUpgrade>(true),
                CameraRight = cameraRight,
                CameraUp = cameraUp,
                CameraPosition = cameraPosition,
                YellowThreshold = yellowThreshold,
                RedThreshold = redThreshold,
                HideEmptySchools = hideEmptySchools,
                UiScale = uiScale,
                ZoomLevel = zoomLevel
            };

            Dependency = drawJob.Schedule(m_SchoolQuery, JobHandle.CombineDependencies(Dependency, renderDeps));
            m_OverlayRenderSystem.AddBufferWriter(Dependency);
        }

        private bool IsEducationInfoviewActive()
        {
            var activeInfoview = m_ToolSystem.activeInfoview;
            if (activeInfoview == null)
            {
                return false;
            }

            return string.Equals(activeInfoview.name, "Education", StringComparison.OrdinalIgnoreCase)
                || activeInfoview.name.IndexOf("Education", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
