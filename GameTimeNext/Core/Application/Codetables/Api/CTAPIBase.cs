using GameTimeNext.Core.Application.TableObjects;
using System.Windows;
using UIX.ViewController.Engine.Runnables;

namespace GameTimeNext.Core.Application.Codetables.Api
{
    /// <summary>
    /// Base of the button actions of codetable parameters. A parameter whose codetable names an API (<c>T1CTABH.PAAP1</c> / <c>PAAP2</c>,
    /// e.g. "ImageImport") gets a button next to its field; a click runs <see cref="ExecuteAsync"/> of the class
    /// <c>CTAPI_&lt;Name&gt;</c> (e.g. <c>CTAPI_ImageImport</c>), which returns the value for the parameter.
    /// Works in the module "Codetables" and in the module "Selection Lists".
    /// </summary>
    public abstract class CTAPIBase
    {
        /// <summary>Text of the button.</summary>
        public abstract string ButtonText { get; }

        /// <summary>Tooltip of the button, empty for none.</summary>
        public virtual string ButtonToolTip => string.Empty;

        /// <summary>
        /// Runs the action. Returns the new value of the parameter, a cancel (the value stays) or a failure with a message for the user.
        /// An exception is logged and shown as a failure by the caller.
        /// </summary>
        public abstract Task<CTAPIResult> ExecuteAsync(CTAPIContext context);
    }

    /// <summary>
    /// What an API knows about the place it is called from.
    /// </summary>
    public sealed class CTAPIContext
    {
        /// <summary>The application that shows the entry (log source, dialogs).</summary>
        public required UIXApplication App { get; init; }

        /// <summary>Window to attach dialogs (e.g. a file dialog) to, or null.</summary>
        public Window? Owner { get; init; }

        /// <summary>The codetable the entry belongs to.</summary>
        public required T1CTABH List { get; init; }

        /// <summary>The entry that is edited (its fields may not contain the unsaved input yet).</summary>
        public required T1CTABD Entry { get; init; }

        /// <summary>Number of the parameter (1 or 2).</summary>
        public int ParameterIndex { get; init; }

        /// <summary>The value of the parameter as it is entered right now.</summary>
        public string CurrentValue { get; init; } = string.Empty;
    }

    /// <summary>
    /// Result of an API: a value for the parameter, a cancel by the user or a failure.
    /// </summary>
    public sealed class CTAPIResult
    {
        private CTAPIResult()
        {
        }

        /// <summary>True if <see cref="Value"/> is to be written into the parameter.</summary>
        public bool HasValue { get; private init; }

        public string Value { get; private init; } = string.Empty;

        /// <summary>Message for the user if the action failed, otherwise empty.</summary>
        public string ErrorMessage { get; private init; } = string.Empty;

        /// <summary>The user cancelled: nothing changes and nothing is shown.</summary>
        public static CTAPIResult Cancelled() => new CTAPIResult();

        public static CTAPIResult WithValue(string value) => new CTAPIResult { HasValue = true, Value = value };

        public static CTAPIResult Failure(string errorMessage) => new CTAPIResult { ErrorMessage = errorMessage };
    }
}
