using GameTimeNext.Core.Application.TableObjects;
using GameTimeNext.Shared.Profiles;

namespace GameTimeNext.Core.Application.DataManagers
{
    public class TXPROFI : TXPROFIBasic, IDtoConverter<T1PROFI, ProfileDto>
    {
        #region Dto Conversion
        public ProfileDto ToDto(T1PROFI obj)
        {
            return new ProfileDto
            {
                PFID = obj.PFID,
                GANA = obj.GANA,
                AccentColorHex = "FF00FF"
            };
        }

        public void ApplyDto(ProfileDto dto, T1PROFI obj)
        {
            obj.GANA = dto.GANA;
        }
        #endregion
    }
}
