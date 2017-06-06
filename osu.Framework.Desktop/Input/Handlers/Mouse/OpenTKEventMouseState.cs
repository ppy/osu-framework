using OpenTK;

namespace osu.Framework.Desktop.Input.Handlers.Mouse
{
    /// <summary>
    /// An OpenTK state which came from an event callback.
    /// </summary>
    internal class OpenTKEventMouseState : OpenTKMouseState
    {
        public OpenTKEventMouseState(OpenTK.Input.MouseState tkState, bool active, Vector2? mappedPosition)
            : base(tkState, active, mappedPosition)
        {
        }
    }
}