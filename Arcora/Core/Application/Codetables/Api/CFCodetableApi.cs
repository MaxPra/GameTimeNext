using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.Windows;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Codetables.Api
{
    /// <summary>
    /// Runs the button action of a codetable parameter and handles its errors. Shared by the modules "Codetables" and "Selection Lists".
    /// </summary>
    public static class CFCodetableApi
    {
        /// <summary>
        /// Creates the API of the parameter, or null if the parameter has none (or the class doesn't exist).
        /// </summary>
        public static CTAPIBase? CreateApi(T1CTABH list, int parameterIndex)
        {
            return CTAPIFactory.Create(CTAPIFactory.GetApiName(list, parameterIndex));
        }

        /// <summary>
        /// Runs the API of the parameter. Returns the new value of the parameter, or null if the user cancelled or it failed
        /// (the user is told about a failure).
        /// </summary>
        public static async Task<string?> RunAsync(UIXApplication app, Window? owner, T1CTABH list, T1CTABD entry, int parameterIndex, string currentValue)
        {
            string apiName = CTAPIFactory.GetApiName(list, parameterIndex);
            string parameterName = list.GetValue<string>($"PADE{parameterIndex}") ?? string.Empty;

            CTAPIBase? api = CTAPIFactory.Create(apiName);

            if (api == null)
            {
                ShowError(app, owner, app.GetErrorMessage("CTA0003", apiName, parameterName));
                return null;
            }

            CTAPIResult result;

            try
            {
                result = await api.ExecuteAsync(new CTAPIContext
                {
                    App = app,
                    Owner = owner,
                    List = list,
                    Entry = entry,
                    ParameterIndex = parameterIndex,
                    CurrentValue = currentValue
                });
            }
            catch (Exception ex)
            {
                FnLog.AddError(app, $"Button action of parameter {parameterIndex} of codetable {list.TXTYP} failed (API {apiName}).", ex);
                ShowError(app, owner, app.GetErrorMessage("CTA0002", parameterName, ex.Message));
                return null;
            }

            if (!FnString.IsNullEmptyOrWhitespace(result.ErrorMessage))
            {
                ShowError(app, owner, app.GetErrorMessage("CTA0002", parameterName, result.ErrorMessage));
                return null;
            }

            return result.HasValue ? result.Value : null;
        }

        private static void ShowError(UIXApplication app, Window? owner, string message)
        {
            app.GetApplication<CFMBOX>(UIXApplicationStartTarget.Window).Show(message, CFMBOXResult.Ok, CFMBOXIcon.Error, owner);
        }
    }
}
