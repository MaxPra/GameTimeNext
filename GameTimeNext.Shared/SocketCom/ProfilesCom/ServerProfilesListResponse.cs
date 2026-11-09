namespace GameTimeNext.Shared.SocketCom.ProfilesCom
{
    public class ServerProfilesListResponse
    {
        public List<ProfileCom> Profiles { get; set; } = new List<ProfileCom>();
        public string Message { get; set; } = string.Empty;
    }
}
