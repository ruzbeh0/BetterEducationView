using Colossal.Mathematics;
using Game.Buildings;
using Game.Prefabs;
using Game.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Color = UnityEngine.Color;

namespace BetterEducationInfoView.Jobs
{
    [BurstCompile]
    public struct DrawSchoolCapacityOverlayJob : IJobChunk
    {
        public OverlayRenderSystem.Buffer OverlayBuffer;

        [ReadOnly] public EntityTypeHandle EntityType;
        [ReadOnly] public ComponentTypeHandle<Game.Objects.Transform> TransformType;
        [ReadOnly] public ComponentTypeHandle<PrefabRef> PrefabRefType;
        [ReadOnly] public BufferTypeHandle<Student> StudentBufferType;
        [ReadOnly] public ComponentLookup<PrefabRef> PrefabRefLookup;
        [ReadOnly] public ComponentLookup<SchoolData> SchoolDataLookup;
        [ReadOnly] public BufferLookup<InstalledUpgrade> InstalledUpgradeLookup;

        public float3 CameraRight;
        public float3 CameraUp;
        public float3 CameraPosition;
        public int YellowThreshold;
        public int RedThreshold;
        public bool HideEmptySchools;
        public float UiScale;
        public float ZoomLevel;

        public void Execute(
            in ArchetypeChunk chunk,
            int unfilteredChunkIndex,
            bool useEnabledMask,
            in v128 chunkEnabledMask)
        {
            NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);
            NativeArray<Game.Objects.Transform> transforms = chunk.GetNativeArray(ref TransformType);
            NativeArray<PrefabRef> prefabRefs = chunk.GetNativeArray(ref PrefabRefType);
            BufferAccessor<Student> students = chunk.GetBufferAccessor(ref StudentBufferType);

            float normalizedZoom = math.pow(math.clamp((ZoomLevel - 1000f) / 13000f, 0f, 1f), 0.65f);
            float thickness = math.lerp(3.2f, 14f, normalizedZoom) * math.max(0.5f, UiScale);
            float heightOffset = math.lerp(12f, 38f, normalizedZoom);

            for (int i = 0; i < chunk.Count; i++)
            {
                int studentCount = students[i].Length;
                if (HideEmptySchools && studentCount == 0)
                {
                    continue;
                }

                SchoolData schoolData;
                if (!SchoolDataLookup.TryGetComponent(prefabRefs[i].m_Prefab, out schoolData))
                {
                    continue;
                }

                Entity schoolEntity = entities[i];
                DynamicBuffer<InstalledUpgrade> upgrades;
                if (InstalledUpgradeLookup.TryGetBuffer(schoolEntity, out upgrades))
                {
                    UpgradeUtils.CombineStats(ref schoolData, upgrades, ref PrefabRefLookup, ref SchoolDataLookup);
                }

                int capacity = schoolData.m_StudentCapacity;
                if (capacity <= 0)
                {
                    continue;
                }

                float fullness = (float) studentCount * 100f / capacity;
                Color borderColor = GetCapacityColor(fullness);
                float3 labelCenter = transforms[i].m_Position + new float3(0f, heightOffset, 0f);
                int schoolLevel = math.clamp((int) schoolData.m_EducationLevel, 1, 4);

                DrawCapacityLabel(labelCenter, studentCount, capacity, schoolLevel, borderColor, thickness);
            }
        }

        private Color GetCapacityColor(float fullness)
        {
            if (fullness >= RedThreshold)
            {
                return new Color(1f, 0.18f, 0.12f, 1f);
            }

            if (fullness >= YellowThreshold)
            {
                return new Color(1f, 0.78f, 0.08f, 1f);
            }

            return new Color(0.12f, 0.84f, 0.28f, 1f);
        }

        private void DrawCapacityLabel(float3 center, int students, int capacity, int schoolLevel, Color borderColor, float thickness)
        {
            FixedList64Bytes<byte> chars = default;
            AppendNumber(ref chars, students);
            chars.Add((byte)'/');
            AppendNumber(ref chars, capacity);

            float3 right = math.normalizesafe(CameraRight);
            float3 up = math.normalizesafe(CameraUp);
            if (math.lengthsq(right) < 0.0001f)
            {
                right = new float3(1f, 0f, 0f);
            }

            if (math.lengthsq(up) < 0.0001f)
            {
                up = new float3(0f, 1f, 0f);
            }

            float3 toCamera = math.normalizesafe(CameraPosition - center);
            center += toCamera * math.max(10f, thickness * 1.8f);

            float layoutDigitWidth = thickness * 1.18f;
            float layoutDigitHeight = thickness * 2.2f;
            float layoutSpacing = thickness * 0.55f;
            float layoutTextWidth = GetTextWidth(ref chars, layoutDigitWidth, layoutSpacing);
            float digitWidth = layoutDigitWidth * 0.8f;
            float digitHeight = layoutDigitHeight * 0.8f;
            float spacing = layoutSpacing * 0.8f;
            float textWidth = GetTextWidth(ref chars, digitWidth, spacing);
            float boxWidth = layoutTextWidth + thickness * 5.4f;
            float boxHeight = layoutDigitHeight + thickness * 2.5f;
            float badgeWidth = thickness * 5.4f;
            float totalWidth = boxWidth + badgeWidth;
            float borderThickness = math.max(2.0f, thickness * 0.35f);

            float3 labelCenter = center + right * (badgeWidth * 0.5f);
            float3 badgeCenter = center - right * (boxWidth * 0.5f);
            float3 halfUp = up * (boxHeight * 0.5f);
            Color background = new Color(borderColor.r, borderColor.g, borderColor.b, 0.9f);
            Color textColor = GetReadableTextColor(borderColor);
            Color outlineColor = new Color(0f, 0f, 0f, 0.95f);
            Color iconColor = new Color(1f, 1f, 1f, 1f);

            DrawSolidPanel(center, totalWidth + borderThickness * 2f, boxHeight + borderThickness * 2f, outlineColor, right);
            DrawSolidPanel(badgeCenter, badgeWidth, boxHeight, outlineColor, right);
            DrawSolidPanel(labelCenter, boxWidth, boxHeight, background, right);
            DrawDivider(center - right * (totalWidth * 0.5f - badgeWidth), halfUp, outlineColor, borderThickness, up);
            DrawGraduationCapIcons(badgeCenter + right * (badgeWidth * 0.28f), schoolLevel, iconColor, thickness, right, up);

            float3 cursor = labelCenter - right * (textWidth * 0.5f);
            float lineThickness = math.max(0.75f, thickness * 0.304f);

            for (int i = 0; i < chars.Length; i++)
            {
                byte c = chars[i];
                float charWidth = GetCharWidth(c, digitWidth);
                float3 charCenter = cursor + right * (charWidth * 0.5f);
                DrawChar(charCenter, c, textColor, charWidth, digitHeight, lineThickness, right, up);
                cursor += right * (charWidth + spacing);
            }
        }

        private void DrawDivider(float3 center, float3 halfUp, Color color, float thickness, float3 up)
        {
            OverlayBuffer.DrawLine(color, new Line3.Segment(center - halfUp, center + halfUp), thickness, true);
        }

        private void DrawGraduationCapIcons(float3 center, int level, Color color, float thickness, float3 right, float3 up)
        {
            int iconCount = math.clamp(level, 1, 4);
            float iconSize = thickness * 1.05f;
            float iconSpacing = thickness * 0.95f;
            float startOffset = -((iconCount - 1) * iconSpacing) * 0.5f;

            for (int i = 0; i < iconCount; i++)
            {
                float3 iconCenter = center + up * (startOffset + i * iconSpacing);
                DrawGraduationCapIcon(iconCenter, iconSize, color, right, up);
            }
        }

        private void DrawGraduationCapIcon(float3 center, float size, Color color, float3 right, float3 up)
        {
            // Coordinates are normalized from Icons/graduation-cap.svg.
            DrawSvgPanel(center, size, 512f, 406.4f, 998.4f, 248f, color, right, up);
            DrawSvgPanel(center, size, 512f, 565.333f, 640f, 277.333f, color, right, up);
            DrawSvgPanel(center, size, 512f, 704f, 640f, 256f, color, right, up);

            float tasselThickness = math.max(0.7f, size * 0.055f);
            DrawSvgLine(center, size, 921.6f, 435.2f, 494.933f, 362.667f, color, tasselThickness, right, up);
            DrawSvgLine(center, size, 896f, 473.6f, 896f, 789.333f, color, tasselThickness, right, up);
            DrawSvgDot(center, size, 917.333f, 789.333f, 85.333f, color, right, up);
            DrawSvgLine(center, size, 917.333f, 800f, 917.333f, 956f, color, size * 0.07f, right, up);
        }

        private void DrawSolidPanel(float3 center, float width, float height, Color fillColor, float3 right)
        {
            float lineLength = math.max(0.01f, width - height);
            float3 halfLine = right * (lineLength * 0.5f);
            OverlayBuffer.DrawLine(fillColor, new Line3.Segment(center - halfLine, center + halfLine), height, true);
        }

        private void DrawSvgPanel(
            float3 center,
            float iconSize,
            float svgCenterX,
            float svgCenterY,
            float svgWidth,
            float svgHeight,
            Color color,
            float3 right,
            float3 up)
        {
            float scale = iconSize / 1024f;
            DrawSolidPanel(SvgPoint(center, iconSize, svgCenterX, svgCenterY, right, up), svgWidth * scale, svgHeight * scale, color, right);
        }

        private void DrawSvgLine(
            float3 center,
            float iconSize,
            float startX,
            float startY,
            float endX,
            float endY,
            Color color,
            float thickness,
            float3 right,
            float3 up)
        {
            OverlayBuffer.DrawLine(
                color,
                new Line3.Segment(
                    SvgPoint(center, iconSize, startX, startY, right, up),
                    SvgPoint(center, iconSize, endX, endY, right, up)),
                thickness,
                true);
        }

        private void DrawSvgDot(
            float3 center,
            float iconSize,
            float svgX,
            float svgY,
            float svgDiameter,
            Color color,
            float3 right,
            float3 up)
        {
            float diameter = svgDiameter * iconSize / 1024f;
            float3 dotCenter = SvgPoint(center, iconSize, svgX, svgY, right, up);
            float3 halfLine = right * math.max(0.01f, diameter * 0.02f);
            OverlayBuffer.DrawLine(color, new Line3.Segment(dotCenter - halfLine, dotCenter + halfLine), diameter, true);
        }

        private float3 SvgPoint(float3 center, float iconSize, float svgX, float svgY, float3 right, float3 up)
        {
            float scale = iconSize / 1024f;
            return center + right * ((svgX - 512f) * scale) - up * ((svgY - 512f) * scale);
        }

        private void AppendNumber(ref FixedList64Bytes<byte> chars, int value)
        {
            if (value <= 0)
            {
                chars.Add((byte)'0');
                return;
            }

            FixedList32Bytes<byte> reversed = default;
            int remaining = value;
            while (remaining > 0 && reversed.Length < 16)
            {
                reversed.Add((byte)('0' + remaining % 10));
                remaining /= 10;
            }

            for (int i = reversed.Length - 1; i >= 0; i--)
            {
                chars.Add(reversed[i]);
            }
        }

        private float GetTextWidth(ref FixedList64Bytes<byte> chars, float digitWidth, float spacing)
        {
            float width = 0f;
            for (int i = 0; i < chars.Length; i++)
            {
                width += GetCharWidth(chars[i], digitWidth);
                if (i < chars.Length - 1)
                {
                    width += spacing;
                }
            }

            return width;
        }

        private float GetCharWidth(byte c, float digitWidth)
        {
            if (c == (byte)'1')
            {
                return digitWidth * 0.72f;
            }

            if (c == (byte)'/')
            {
                return digitWidth * 0.78f;
            }

            return digitWidth;
        }

        private Color GetReadableTextColor(Color background)
        {
            float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance > 0.55f
                ? new Color(0.04f, 0.045f, 0.05f, 1f)
                : new Color(1f, 1f, 1f, 1f);
        }

        private void DrawChar(
            float3 center,
            byte c,
            Color color,
            float width,
            float height,
            float thickness,
            float3 right,
            float3 up)
        {
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            if (c == (byte)'/')
            {
                float3 slashStart = center - right * halfWidth - up * halfHeight;
                float3 slashEnd = center + right * halfWidth + up * halfHeight;
                OverlayBuffer.DrawLine(color, new Line3.Segment(slashStart, slashEnd), thickness, true);
                return;
            }

            byte mask = GetDigitMask(c);
            if (mask == 0)
            {
                return;
            }

            float3 tl = center - right * halfWidth + up * halfHeight;
            float3 tr = center + right * halfWidth + up * halfHeight;
            float3 ml = center - right * halfWidth;
            float3 mr = center + right * halfWidth;
            float3 bl = center - right * halfWidth - up * halfHeight;
            float3 br = center + right * halfWidth - up * halfHeight;

            float3 insetX = right * (thickness * 0.22f);
            float3 insetY = up * (thickness * 0.22f);

            if ((mask & 1) != 0)
            {
                OverlayBuffer.DrawLine(color, new Line3.Segment(tl + insetX, tr - insetX), thickness, true);
            }

            if ((mask & 2) != 0)
            {
                OverlayBuffer.DrawLine(color, new Line3.Segment(tr - insetY, mr + insetY), thickness, true);
            }

            if ((mask & 4) != 0)
            {
                OverlayBuffer.DrawLine(color, new Line3.Segment(mr - insetY, br + insetY), thickness, true);
            }

            if ((mask & 8) != 0)
            {
                OverlayBuffer.DrawLine(color, new Line3.Segment(br - insetX, bl + insetX), thickness, true);
            }

            if ((mask & 16) != 0)
            {
                OverlayBuffer.DrawLine(color, new Line3.Segment(bl + insetY, ml - insetY), thickness, true);
            }

            if ((mask & 32) != 0)
            {
                OverlayBuffer.DrawLine(color, new Line3.Segment(ml + insetY, tl - insetY), thickness, true);
            }

            if ((mask & 64) != 0)
            {
                OverlayBuffer.DrawLine(color, new Line3.Segment(ml + insetX, mr - insetX), thickness, true);
            }
        }

        private byte GetDigitMask(byte c)
        {
            switch (c)
            {
                case (byte)'0': return 0x3F;
                case (byte)'1': return 0x06;
                case (byte)'2': return 0x5B;
                case (byte)'3': return 0x4F;
                case (byte)'4': return 0x66;
                case (byte)'5': return 0x6D;
                case (byte)'6': return 0x7D;
                case (byte)'7': return 0x07;
                case (byte)'8': return 0x7F;
                case (byte)'9': return 0x6F;
                default: return 0;
            }
        }
    }
}
