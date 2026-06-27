namespace GameTimeNext.Shared.SocketCom.ProfilesCom
{
    public class ProfileCom
    {
        public bool Playable { get; set; }
        public bool Launchable { get; set; } = false;
        public string ProfileName { get; set; } = string.Empty;
        public string PicPath { get; set; } = string.Empty;
    }
}
