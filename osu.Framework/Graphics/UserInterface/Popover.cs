// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions;
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
            InternalChild = BoundingBoxContainer = new Container
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
            };
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
