using Arcora.Shared.Base;

namespace Arcora.Shared.Profiles
{
    public class ProfileDto : DtoBase
    {
        public required long PFID { get; set; }
        public required string GANA { get; set; }
        public required string PPFN { get; set; }
        public required string AccentColorHex { get; set; }
    }
}
