using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework
{
    /// <summary>
    /// Provides the application properties to the engine from T1APRDH (Application Properties Definition).
    /// The application name is the definition ID, by convention the class name of the application.
    /// </summary>
    public class AppPropertiesProvider : IUIXAppPropertiesProvider
    {
        public UIXAppProperties? Read(string applicationName)
        {
            T1APRDH? t1aprdh = new TXAPRDH().Read(applicationName);

            if (t1aprdh == null)
                return null;

            return new UIXAppProperties
            {
                ApplicationName = t1aprdh.APDID,
                ApplicationDescription = t1aprdh.APDES,
                ApplicationHelpText = t1aprdh.APHPT,
                IsMenuCallPossible = t1aprdh.APMCP
            };
        }
    }
}
