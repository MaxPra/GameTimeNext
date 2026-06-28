namespace GameTimeNext.Shared.SocketCom.ProfilesCom
{
    public class OlisProfileDTO
    {
        public required int Id { get; set; }
        public required string Title { get; set; }
        public required string AccentColorHex { get; set; }

        public OlisProfileDTO() { }
    }
}
