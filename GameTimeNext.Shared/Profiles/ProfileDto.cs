using GameTimeNext.Shared.Base;

namespace GameTimeNext.Shared.Profiles
{
    public class ProfileDto : DtoBase
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public required string AccentColorHex { get; set; }
    }
}
