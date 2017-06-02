using osu.Framework.Configuration;

namespace osu.Framework.Input.Handlers
{
    /// <summary>
    /// An input handler which can have its sensitivity changed.
    /// </summary>
    public interface IHasSensitivity
    {
        BindableDouble Sensitivity { get; }
    }
}
