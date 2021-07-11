// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A <see cref="Popover"/> is a transient view that appears above other on-screen content.
    /// It typically is activated by another control and includes an arrow pointing to the location from which it emerged.
    /// (loosely paraphrasing: https://developer.apple.com/design/human-interface-guidelines/ios/views/popovers/)
    /// </summary>
    public abstract class Popover : VisibilityContainer
    {
        /// <summary>
        /// The <see cref="Anchor"/> that this <see cref="Popover"/> is to be attached to the triggering UI control by.
        /// </summary>
        public Anchor PopoverAnchor
        {
            get => Anchor;
            set
            {
                BoundingBoxContainer.Origin = value;
                BoundingBoxContainer.Anchor = value.Opposite();

                Body.Anchor = Body.Origin = value;
                Arrow.Anchor = value;
                Arrow.Rotation = getRotationFor(value);
                Arrow.Alpha = value == Anchor.Centre ? 0 : 1;
                AnchorUpdated(value);
            }
        }

        /// <summary>
        /// The container holding all of this popover's elements (the <see cref="Body"/> and the <see cref="Arrow"/>).
        /// </summary>
        internal Container BoundingBoxContainer { get; }

        /// <summary>
        /// The background box of the popover.
        /// </summary>
        protected Box Background { get; private set; }

        /// <summary>
        /// The arrow of this <see cref="Popover"/>, pointing at the component which triggered it.
        /// </summary>
        protected internal Drawable Arrow { get; }

        /// <summary>
        /// The body of this <see cref="Popover"/>, containing the actual contents.
        /// </summary>
        protected internal FocusedOverlayContainer Body { get; }

        private Container content;
        protected override Container<Drawable> Content => content;

        protected Popover()
        {
            base.AddInternal(BoundingBoxContainer = new Container
            {
                AutoSizeAxes = Axes.Both,
                Children = new[]
                {
                    Arrow = CreateArrow(),
                    Body = CreateBody().With(body =>
                    {
                        body.AutoSizeAxes = Axes.Both;
                        body.State.BindTarget = State;
                        body.Children = new Drawable[]
                        {
                            Background = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                            },
                            content = new Container
                            {
                                AutoSizeAxes = Axes.Both,
                            }
                        };
                    })
                }
            });
        }

        /// <summary>
        /// Creates an arrow drawable that points away from the given <see cref="Anchor"/>.
        /// </summary>
        protected abstract Drawable CreateArrow();

        /// <summary>
        /// Creates the body of this <see cref="Popover"/>.
        /// </summary>
        protected virtual FocusedOverlayContainer CreateBody() => new PopoverFocusedOverlayContainer();

        protected override void PopIn() => this.FadeIn();
        protected override void PopOut() => this.FadeOut();

        /// <summary>
        /// Called when <see cref="Anchor"/> is set.
        /// Can be used to apply custom layout updates to the subcomponents.
        /// </summary>
        protected virtual void AnchorUpdated(Anchor anchor)
        {
        }

        private float getRotationFor(Anchor anchor)
        {
            switch (anchor)
            {
                case Anchor.TopLeft:
                    return -45;

                case Anchor.TopCentre:
                default:
                    return 0;

                case Anchor.TopRight:
                    return 45;

                case Anchor.CentreLeft:
                    return -90;

                case Anchor.CentreRight:
                    return 90;

                case Anchor.BottomLeft:
                    return -135;

                case Anchor.BottomCentre:
                    return -180;

                case Anchor.BottomRight:
                    return 135;
            }
        }

        protected internal sealed override void AddInternal(Drawable drawable) => throw new InvalidOperationException($"Use {nameof(Content)} instead.");

        #region Sizing delegation

        // Popovers rely on being 0x0 sized and placed exactly at the attachment point to their drawable for layouting logic.
        // This can cause undesirable results if somebody tries to directly set the Width/Height of a popover, expecting the body to be resized.
        // This is done via shadowing rather than overrides, because we still want framework to read the base 0x0 size.

        public new float Width
        {
            get => Body.Width;
            set
            {
                if (Body.AutoSizeAxes.HasFlagFast(Axes.X))
                    Body.AutoSizeAxes &= ~Axes.X;

                Body.Width = value;
            }
        }

        public new float Height
        {
            get => Body.Height;
            set
            {
                if (Body.AutoSizeAxes.HasFlagFast(Axes.Y))
                    Body.AutoSizeAxes &= ~Axes.Y;

                Body.Height = value;
            }
        }

        public new Vector2 Size
        {
            get => Body.Size;
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        #endregion

        protected class PopoverFocusedOverlayContainer : FocusedOverlayContainer
        {
            protected override bool BlockPositionalInput => true;

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => State.Value == Visibility.Visible;

            protected override void OnMouseUp(MouseUpEvent e)
            {
                base.OnMouseUp(e);
                handleMouseEvent(e.ScreenSpaceMouseDownPosition);
            }

            protected override bool OnClick(ClickEvent e)
            {
                return handleMouseEvent(e.ScreenSpaceMouseDownPosition);
            }

            private bool handleMouseEvent(Vector2 position)
            {
                // if the mouse event can be handled by this container, prevent it from propagating further.
                if (base.ReceivePositionalInputAt(position))
                    return true;

                // anything else means that the user is clicking away from the popover, and so that should hide the popover and trigger focus loss.
                Hide();
                return false;
            }
        }
    }
}
