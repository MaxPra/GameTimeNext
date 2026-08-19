namespace GameTimeNext.Core.Application.General.UserSettings
{
    public class UserSettings
    {
        public List<FavoriteApplication> FavApps { get; set; } = new List<FavoriteApplication>();
        public short SelectedDashboardMode { get; set; } = 2;
        public short SelectedDashboardOffset { get; set; } = 0;
    }
}