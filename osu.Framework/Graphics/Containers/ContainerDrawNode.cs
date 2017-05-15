// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Batches;
using OpenTK;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Colour;
using System;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Types of edge effects that can be applied to containers.
    /// </summary>
    public enum EdgeEffectType
    {
        None,
        Glow,
        Shadow,
    }

    /// <summary>
    /// Parametrizes the appearance of an edge effect.
    /// </summary>
    public struct EdgeEffect
    {
        /// <summary>
        /// Colour of the edge effect.
        /// </summary>
        public SRGBColour Colour;

        /// <summary>
        /// Positional offset applied to the edge effect.
        /// Useful for off-center shadows.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// The type of the edge effect.
        /// </summary>
        public EdgeEffectType Type;

        /// <summary>
        /// How round the edge effect should appear. Adds to the <see cref="Container{T}.CornerRadius"/>
        /// of the corresponding container. Not to confuse with the <see cref="Radius"/>.
        /// </summary>
        public float Roundness;

        /// <summary>
        /// How "thick" the edge effect is around the container. In other words: At what distance
        /// from the <see cref="Container"/> border the edge effect becomes fully invisible.
        /// </summary>
        public float Radius;
    }

    /// <summary>
    /// Shared data between all <see cref="ContainerDrawNode"/>s corresponding to the same
    /// <see cref="Container"/>.
    /// </summary>
    public class ContainerDrawNodeSharedData
    {
        /// <summary>
        /// The vertex batch used for rendering.
        /// </summary>
        public QuadBatch<TexturedVertex2D> VertexBatch;

        /// <summary>
        /// Whether we always want to use our own vertex batch for our corresponding
        /// <see cref="Container"/>. If false, then we may get rendered with some other
        /// shared vertex batch.
        /// </summary>
        public bool ForceOwnVertexBatch;
    }

    /// <summary>
    /// A draw node responsible for rendering a <see cref="Container"/> and the
    /// <see cref="DrawNode"/>s of its children.
    /// </summary>
    public class ContainerDrawNode : DrawNode
    {
        /// <summary>
        /// The <see cref="DrawNode"/>s of the children of our <see cref="Container"/>.
        /// </summary>
        public List<DrawNode> Children;

        /// <summary>
        /// Information about how masking of children should be carried out.
        /// </summary>
        public MaskingInfo? MaskingInfo;

        /// <summary>
        /// Information about how the edge effect should be drawn.
        /// </summary>
        public EdgeEffectInfo? EdgeEffectInfo;

        /// <summary>
        /// Shared data between all <see cref="ContainerDrawNode"/>s corresponding to the same
        /// <see cref="Container"/>.
        /// </summary>
        public ContainerDrawNodeSharedData Shared;

        /// <summary>
        /// The shader to be used for rendering the edge effect.
        /// </summary>
        public Shader Shader;

        private void drawEdgeEffect()
        {
            if (MaskingInfo == null || EdgeEffectInfo == null)
                return;

            MaskingInfo edgeEffectMaskingInfo = MaskingInfo.Value;
            edgeEffectMaskingInfo.MaskingRect = EdgeEffectInfo.Value.MaskingRect;
            edgeEffectMaskingInfo.ScreenSpaceAABB = EdgeEffectInfo.Value.ScreenSpaceDrawQuad.AABB;
            edgeEffectMaskingInfo.CornerRadius += EdgeEffectInfo.Value.Effect.Radius + EdgeEffectInfo.Value.Effect.Roundness;
            edgeEffectMaskingInfo.BorderThickness = 0;
            edgeEffectMaskingInfo.BlendRange = EdgeEffectInfo.Value.Effect.Radius;
            edgeEffectMaskingInfo.AlphaExponent = 2;

            GLWrapper.PushMaskingInfo(edgeEffectMaskingInfo);

            GLWrapper.SetBlend(new BlendingInfo(EdgeEffectInfo.Value.Effect.Type == EdgeEffectType.Glow ? BlendingMode.Additive : BlendingMode.Mixture));

            Shader.Bind();

            ColourInfo colour = ColourInfo.SingleColour(EdgeEffectInfo.Value.Effect.Colour);
            colour.TopLeft.MultiplyAlpha(DrawInfo.Colour.TopLeft.Linear.A);
            colour.BottomLeft.MultiplyAlpha(DrawInfo.Colour.BottomLeft.Linear.A);
            colour.TopRight.MultiplyAlpha(DrawInfo.Colour.TopRight.Linear.A);
            colour.BottomRight.MultiplyAlpha(DrawInfo.Colour.BottomRight.Linear.A);

            Texture.WhitePixel.DrawQuad(EdgeEffectInfo.Value.ScreenSpaceDrawQuad, colour);

            Shader.Unbind();

            GLWrapper.PopMaskingInfo();
        }

        private const int min_amount_children_to_warrant_batch = 5;

        /// <summary>
        /// A custom action to perform for every given vertex to render.
        /// If null, then by default vertices are added to a vertex batch.
        /// </summary>
        protected Action<TexturedVertex2D> CustomVertexAction => null;

        private bool mayHaveOwnVertexBatch(int amountChildren) => Shared.ForceOwnVertexBatch || amountChildren >= min_amount_children_to_warrant_batch;

        private void updateVertexBatch()
        {
            if (Children == null)
                return;

            // This logic got roughly copied from the old osu! code base. These constants seem to have worked well so far.
            int clampedAmountChildren = MathHelper.Clamp(Children.Count, 1, 1000);
            if (mayHaveOwnVertexBatch(clampedAmountChildren) && (Shared.VertexBatch == null || Shared.VertexBatch.Size < clampedAmountChildren))
                Shared.VertexBatch = new QuadBatch<TexturedVertex2D>(clampedAmountChildren * 2, 500);
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            if (CustomVertexAction == null)
            {
                updateVertexBatch();

                // Prefer to use own vertex batch instead of the parent-owned one.
                if (Shared.VertexBatch != null)
                    vertexAction = Shared.VertexBatch.Add;
            }
            else
                vertexAction = CustomVertexAction;

            base.Draw(vertexAction);

            drawEdgeEffect();
            if (MaskingInfo != null)
            {
                MaskingInfo info = MaskingInfo.Value;
                if (info.BorderThickness > 0)
                    info.BorderColour *= DrawInfo.Colour.AverageColour;

                GLWrapper.PushMaskingInfo(info);
            }

            if (Children != null)
                foreach (DrawNode child in Children)
                    child.Draw(vertexAction);

            if (MaskingInfo != null)
                GLWrapper.PopMaskingInfo();
        }
    }
}
