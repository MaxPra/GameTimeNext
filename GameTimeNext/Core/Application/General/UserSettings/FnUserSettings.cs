using GameTimeNext.Core.Framework;

namespace GameTimeNext.Core.Application.General.UserSettings
{
    public class FnUserSettings
    {
        public static bool ContainsApplication(List<FavoriteApplication> favApplications, string fullName)
        {
            foreach (var application in favApplications)
            {
                if (application.FullName == fullName)
                    return true;
            }

            return false;
        }

        public static void AddFavoriteApplication(List<FavoriteApplication> favoriteApplications, FavoriteApplication favApplication)
        {
            if (ContainsApplication(favoriteApplications, favApplication.FullName))
                return;

            favApplication.Order = GetLastAddedOrder(favoriteApplications) + 1;

            favoriteApplications.Add(favApplication);

            AppEnvironment.SaveAppConfig();
        }

        public static FavoriteApplication GetFavoriteApplication(List<FavoriteApplication> favApplications, string fullName)
        {
            foreach (var application in favApplications)
            {
                if (application.FullName == fullName)
                    return application;
            }

            return null;
        }

        public static void RemoveFavoriteApplication(List<FavoriteApplication> favoriteApplications, FavoriteApplication favApplication)
        {
            if (!ContainsApplication(favoriteApplications, favApplication.FullName))
                return;

            FavoriteApplication favApplicationFromList = GetFavoriteApplication(favoriteApplications, favApplication.FullName);

            favoriteApplications.Remove(favApplicationFromList);

            ReorderFavoriteApplications(favoriteApplications);

            AppEnvironment.SaveAppConfig();
        }

        public static void SetAsPrimaryStart(FavoriteApplication favApplication)
        {
            List<FavoriteApplication> favoriteApplications = AppEnvironment.GetAppConfig().UserSettings.FavApps;

            if (!ContainsApplication(favoriteApplications, favApplication.FullName))
                return;

            FavoriteApplication favoriteApplicationFromList = GetFavoriteApplication(favoriteApplications, favApplication.FullName);

            favoriteApplicationFromList.PrimaryStart = true;

            AppEnvironment.SaveAppConfig();
        }

        public static bool IsPrimaryStartSet()
        {
            List<FavoriteApplication> favoriteApplications = AppEnvironment.GetAppConfig().UserSettings.FavApps;

            if (favoriteApplications.Count == 0)
                return false;

            foreach (FavoriteApplication favoriteApplication in favoriteApplications)
            {
                if (favoriteApplication.PrimaryStart) return true;
            }

            return false;
        }

        public static void ReorderFavoriteApplications(List<FavoriteApplication> favoriteApplications)
        {
            int orderPos = 1;

            foreach (var application in favoriteApplications)
            {
                application.Order = orderPos;
                orderPos++;
            }
        }

        private static int GetLastAddedOrder(List<FavoriteApplication> favoriteApplications)
        {

            int lastAddedOrder = 0;

            foreach (var application in favoriteApplications)
            {
                if (application.Order > lastAddedOrder)
                    lastAddedOrder = application.Order;
            }

            return lastAddedOrder;
        }
    }
}
