using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Framework
{
    /// <summary>
    /// Provides the error messages to the engine from T1ERMSG (module "Errormessages").
    /// </summary>
    public class ErrorMessageProvider : IUIXErrorMessageProvider
    {
        public UIXErrorMessage? Read(string messageNumber)
        {
            T1ERMSG? t1ermsg = new TXERMSG().Read(messageNumber);

            if (t1ermsg == null)
                return null;

            return new UIXErrorMessage
            {
                MessageNumber = t1ermsg.ERMNR,
                Message = t1ermsg.ERMSG,
                Hint = t1ermsg.ERHIN
            };
        }
    }
}
