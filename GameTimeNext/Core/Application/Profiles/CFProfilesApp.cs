using System.Globalization;
using GameTimeNext.Core.Application.DataManagers;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Core.Framework.UI;
using GameTimeNext.Core.Framework.UI.Dialogs;
using System.IO;
using System.Windows.Media.Imaging;
using UIX.ViewController.Engine.Runnables;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Profiles
{
    public class CFProfilesApp
    {
        private static readonly Uri PlaceholderCoverUri = new Uri("pack://application:,,,/UI/Ressources/GTN_COVER_PLACEHOLDER.png", UriKind.Absolute);

        /// <summary>
        /// Opens the placeholder cover as stream, for callers that decode the image themselves.
        /// </summary>
        public static Stream OpenPlaceholderCoverStream()
        {
            return System.Windows.Application.GetResourceStream(PlaceholderCoverUri).Stream;
        }

        /// <summary>
        /// Loads the cover of a profile (T1PROFI.PPFN, file name in the profile covers folder) downscaled for display.
        /// Falls back to the placeholder cover if the file is missing or can't be read, so the profile is still shown.
        /// </summary>
        public static BitmapImage LoadProfileCover(string coverFileName, int width = 300, int height = 450)
        {
            if (FnString.IsNullEmptyOrWhitespace(coverFileName))
                return LoadPlaceholderCover(width, height);

            try
            {
                return FnImage.LoadImageWithoutLock(Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath, coverFileName), width, height)!;
            }
            catch (Exception ex)
            {
                FnLog.AddWarning(nameof(CFProfilesApp), $"Cover \"{coverFileName}\" could not be loaded, the placeholder cover is shown instead", ex);
                return LoadPlaceholderCover(width, height);
            }
        }

        /// <summary>
        /// Loads the placeholder cover (rendered from Branding/cover_placeholder.svg) for profiles without a fitting
        /// or with a missing cover, optionally downscaled (0 = original size). Frozen, so it can be used on any thread.
        /// </summary>
        public static BitmapImage LoadPlaceholderCover(int width = 0, int height = 0)
        {
            BitmapImage cover = new BitmapImage();
            cover.BeginInit();
            cover.UriSource = PlaceholderCoverUri;
            cover.CacheOption = BitmapCacheOption.OnLoad;

            if (width != 0 && height != 0)
            {
                cover.DecodePixelWidth = width;
                cover.DecodePixelHeight = height;
            }

            cover.EndInit();
            cover.Freeze();

            return cover;
        }

        public static string FormatGameTimeMinutes(double gameTime)
        {
            string returnedFormat = "";

            if (gameTime == 0)
            {
                return "n.A.";
            }

            double gameTimeMinutes = gameTime;
            double roundedMinutes = Math.Round(gameTimeMinutes, 1);

            returnedFormat = roundedMinutes.ToString("0", CultureInfo.CurrentCulture) + " min";

            return returnedFormat;
        }

        public static string FormatFirstLastDate(DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue)
            {
                return "n.A.";
            }

            return dateTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
        }

        public static bool AskForNewPlaythroughCreationIfNotActive(long pfid, UIXApplication app)
        {
            bool resultNewPlaythrough = false;

            if (TFPLTHR.GetCurrentPlaythrough(pfid) != null)
                return true;



            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ToastMessage tm = new ToastMessage("Attention!", AppConfig.Root.ApplicationName + " needs your attention!");
                tm.Show();

                CFMBOX cfmbox = app.GetApplication<CFMBOX>(UIX.ViewController.Engine.Runnables.UIXApplicationStartTarget.Window);
                CFMBOXResult result = cfmbox.Show(app.GetErrorMessage("PLT0001"), CFMBOXResult.Yes | CFMBOXResult.No, CFMBOXIcon.Question);

                if (result == CFMBOXResult.Yes)
                {
                    long nextNumber = TFPLTHR.GetCurrentPlaythroughCount(pfid, PlaythroughType.NEW_PLAYTHROUGH) + 1;
                    TFPLTHR.CreateNewPlaythrough(pfid, "Playthrough #" + nextNumber, PlaythroughType.NEW_PLAYTHROUGH);

                    resultNewPlaythrough = true;
                }
            });

            return resultNewPlaythrough;
        }
    }
}
