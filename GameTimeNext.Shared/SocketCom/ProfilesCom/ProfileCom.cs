namespace GameTimeNext.Shared.SocketCom.ProfilesCom
{
    public class ProfileCom
    {
        public string State { get; set; } = ProfileStates.All;
        public bool Launchable { get; set; } = false;
        public string ProfileName { get; set; } = string.Empty;
        public string PicPath { get; set; } = string.Empty;
    }
}
