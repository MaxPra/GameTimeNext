using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Shared.Profiles;
using System.Globalization;
using System.IO;
using System.Windows.Media;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPROFI : TXPROFIBasic, IDtoConverter<T1PROFI, ProfileDto>
    {
        #region Dto Conversion
        public ProfileDto ToDto(T1PROFI obj)
        {
            Color accent = FnTheme.GetProfileAccentColor(obj);

            return new ProfileDto
            {
                PFID = obj.PFID,
                GANA = obj.GANA,
                PPFN = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath, obj.PPFN),
                AccentColorHex = string.Create(CultureInfo.InvariantCulture, $"{accent.R:X2}{accent.G:X2}{accent.B:X2}")
            };
        }

        public void ApplyDto(ProfileDto dto, T1PROFI obj)
        {
            obj.GANA = dto.GANA;
        }
        #endregion
    }
}
