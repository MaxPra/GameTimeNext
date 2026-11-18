using GameTimeNext.Core.Application.General;
using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Core.Framework.Config;
using GameTimeNext.Shared.Profiles;
using System.Globalization;
using System.IO;
using System.Windows.Media;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPROFI : TXPROFIBasic, IDtoConverter<T1PROFI, ProfileDto>, IDtoConverter<T1PROFI, ProfileDetailDto>
    {
        #region Dto Conversion
        public ProfileDto ToDto(T1PROFI obj)
        {
            return new ProfileDto
            {
                PFID = obj.PFID,
                GANA = obj.GANA,
                PPFN = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath, obj.PPFN),
                AccentColorHex = GetAccentColorHex(obj)
            };
        }

        public void ApplyDto(ProfileDto dto, T1PROFI obj)
        {
            obj.GANA = dto.GANA;
        }

        public ProfileDetailDto ToDetailDto(T1PROFI obj)
        {
            // Only read for the DTO, never saved, so no data manager of an application (record lock)
            T1PLTHR? currentPlaythrough = TFPLTHR.HasCurrentPlaythrough(obj.PFID) ? new TXPLTHR().Read(TFPLTHR.GetCurrentPlaythroughPtid(obj.PFID)) : null;

            return new ProfileDetailDto
            {
                PFID = obj.PFID,
                GANA = obj.GANA,
                PPFN = Path.Combine(AppConfig.Storage.ProfileCoversDirectoryPath, obj.PPFN),
                AccentColorHex = GetAccentColorHex(obj),
                TotalMinutes = TFPROFI.GetTotalGameTimeInMinutes(obj.PFID),
                TodayMinutes = TFPROFI.GetTodaysGameTimeInMinutes(obj.PFID),
                LastPlayed = obj.LAPL == DateTime.MinValue ? null : obj.LAPL,
                CurrentPlaythrough = currentPlaythrough?.PTDE,
                CurrentPlaythroughMinutes = currentPlaythrough is null ? null : TFPLTHR.GetPlaytimeInMinutes(currentPlaythrough.PTID)
            };
        }

        ProfileDetailDto IDtoConverter<T1PROFI, ProfileDetailDto>.ToDto(T1PROFI obj) => ToDetailDto(obj);

        public void ApplyDto(ProfileDetailDto dto, T1PROFI obj)
        {
            ApplyDto((ProfileDto)dto, obj);
        }

        private static string GetAccentColorHex(T1PROFI obj)
        {
            Color accent = FnTheme.GetProfileAccentColor(obj);
            return string.Create(CultureInfo.InvariantCulture, $"{accent.R:X2}{accent.G:X2}{accent.B:X2}");
        }
        #endregion
    }
}
