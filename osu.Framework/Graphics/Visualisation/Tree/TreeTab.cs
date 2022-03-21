// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    internal abstract class TreeTab
    {
        public abstract object Target { get; }
        public abstract TreeContainer Tree { get; }

        public override string ToString()
        {
            return Target.ToString() ?? "";
        }

        public abstract void SetTarget();
    }

    internal sealed class DrawableTreeTab : TreeTab
    {
        public Drawable TargetDrawable;
        public readonly DrawableTreeContainer DrawableTree;

        public override object Target => TargetDrawable;
        public override TreeContainer Tree => DrawableTree;

        public DrawableTreeTab(DrawableTreeContainer tree, Drawable target)
        {
            DrawableTree = tree;
            TargetDrawable = target;
        }

        public override void SetTarget()
        {
            DrawableTree.Target = TargetDrawable;
        }
    }

    internal sealed class SearchingTab : TreeTab
    {
        public readonly DrawableTreeContainer DrawableTree;

        public override object Target => null!;
        public override TreeContainer Tree => DrawableTree;

        public SearchingTab(DrawableTreeContainer tree)
        {
            DrawableTree = tree;
        }

        public override void SetTarget()
        {
            DrawableTree.Target = null;
        }

        public override string ToString() => "Select drawable...";
    }

    internal sealed class ObjectTreeTab : TreeTab
    {
        public readonly ObjectTreeContainer ObjectTree;

        public override object Target { get; }
        public override TreeContainer Tree => ObjectTree;

        public ObjectTreeTab(ObjectTreeContainer tree, object target)
        {
            ObjectTree = tree;
            Target = target;
            tree.Target = target;
        }

        public override void SetTarget()
        {
        }
    }
}
