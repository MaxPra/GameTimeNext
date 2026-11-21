using System.Windows;

namespace GameTimeNext.Core.Framework.UI
{
    /// <summary>
    /// Available animations for the full blackout screen.
    /// </summary>
    internal enum BlackoutAnimationType
    {
        ParticleClock,
        ParticleProfileCover
    }

    /// <summary>
    /// Base class for the animated content of a blackout window.
    /// </summary>
    internal abstract class CFBlackoutAnimationBase : FrameworkElement
    {
        public abstract void Start();

        public abstract void Stop();
    }
}
