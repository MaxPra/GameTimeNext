using System.Globalization;

namespace GameTimeNext.Core.Application.Playthroughs
{
    public static class CFPlaythroughsApp
    {
        /// <summary>
        /// Points per star of a playthrough rating (PTRA runs from 1 to 10, shown as 0.5 to 5 stars).
        /// </summary>
        public const int RatingPointsPerStar = 2;

        /// <summary>
        /// Converts the value of a StarRating control into the stored rating (PTRA, 0 = not rated).
        /// </summary>
        public static int ToRatingValue(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Formats a rating (PTRA scale, may be an average) as stars for the UI, e.g. 7 -> "3.5".
        /// </summary>
        public static string FormatRatingAsStars(double rating)
        {
            return (rating / RatingPointsPerStar).ToString("0.#", CultureInfo.CurrentCulture);
        }
    }
}
