using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles
{
    public class ProfileDto : DtoBase
    {
        public required long PFID { get; set; }
        public required string GANA { get; set; }
        public required string AccentColorHex { get; set; }
    }
}
