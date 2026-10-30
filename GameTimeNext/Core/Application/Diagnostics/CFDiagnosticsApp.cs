using GameTimeNext.Core.Framework;
using GameTimeNext.Core.Framework.DataBase;
using GameTimeNext.Core.Framework.Utils;
using System.Globalization;
using System.Windows.Media;
using UIX.ViewController.Engine.Utils;

namespace GameTimeNext.Core.Application.Diagnostics
{
    /// <summary>
    /// Formatting helpers of the module Diagnostics.
    /// </summary>
    public static class CFDiagnosticsApp
    {
        /// <summary>
        /// Most entries the log list shows; the newest are taken.
        /// </summary>
        public const int LogTopX = 1000;

        /// <summary>
        /// Formats a point in time for the lists (current culture), empty for null.
        /// </summary>
        public static string FormatTimestamp(DateTime? timestamp)
        {
            if (timestamp == null || timestamp.Value == DateTime.MinValue)
                return string.Empty;

            return timestamp.Value.ToString("G", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Formats a cycle duration in milliseconds, e.g. "0.42 ms".
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0:0.00} ms", duration.TotalMilliseconds);
        }

        /// <summary>
        /// Formats a running time as "1d 02:03:04", without days below one day.
        /// </summary>
        public static string FormatUptime(TimeSpan uptime)
        {
            if (uptime < TimeSpan.Zero)
                uptime = TimeSpan.Zero;

            string time = uptime.ToString(@"hh\:mm\:ss", CultureInfo.CurrentCulture);

            return uptime.Days > 0
                ? string.Format(CultureInfo.CurrentCulture, "{0}d {1}", uptime.Days, time)
                : time;
        }

        /// <summary>
        /// Full text of a log entry (header line, message and exception) for the detail box and the clipboard.
        /// </summary>
        public static string FormatLogDetail(SystemLogRecord record)
        {
            string header = FnString.IsNullEmptyOrWhitespace(record.LGSR)
                ? $"{FormatTimestamp(record.LGTS)} - {record.LGTY}"
                : $"{FormatTimestamp(record.LGTS)} - {record.LGTY} - [{record.LGSR}]";

            if (!FnString.IsNullEmptyOrWhitespace(record.LGCL))
                header = $"{header} (at {record.LGCL})";

            return $"{header}{Environment.NewLine}{FormatLogText(record)}";
        }

        /// <summary>
        /// Message of a log entry, followed by the exception and the call stack if there are.
        /// </summary>
        public static string FormatLogText(SystemLogRecord record)
        {
            string text = record.LGMS;

            if (!FnString.IsNullEmptyOrWhitespace(record.LGEX))
                text = $"{text}{Environment.NewLine}{Environment.NewLine}{record.LGEX}";

            if (!FnString.IsNullEmptyOrWhitespace(record.LGST))
                text = $"{text}{Environment.NewLine}{Environment.NewLine}Call stack:{Environment.NewLine}{record.LGST}";

            return text;
        }

        /// <summary>
        /// Source and place of the call of a log entry for the detail header, e.g. "ProfilesApp · StartGame (FnLauncher.cs:144)".
        /// </summary>
        public static string FormatLogOrigin(SystemLogRecord record)
        {
            return string.Join(" · ", new[] { record.LGSR, record.LGCL }.Where(part => !FnString.IsNullEmptyOrWhitespace(part)));
        }

        /// <summary>
        /// Icon glyph of a log type (T1SYLOG.LGTY).
        /// </summary>
        public static string GetLogTypeIcon(string logType)
        {
            return logType switch
            {
                nameof(UIXLogType.Error) => UIXMdlIcons.Error,
                nameof(UIXLogType.Warning) => UIXMdlIcons.Warning,
                nameof(GTNLogType.AppStart) => UIXMdlIcons.Play,
                _ => UIXMdlIcons.Info
            };
        }

        /// <summary>
        /// Resource key of the brush for a log type (T1SYLOG.LGTY).
        /// </summary>
        public static string GetLogTypeBrushKey(string logType)
        {
            return logType switch
            {
                nameof(UIXLogType.Error) => "Brush.Danger",
                nameof(UIXLogType.Warning) => "Brush.Warning",
                _ => "Brush.TextSecondary"
            };
        }

        /// <summary>
        /// Row colors of the log list per log type: light tint below the hover / selection tint, text in the full color.
        /// Null for info rows (default colors). Frozen brushes (FnColor), usable while the rows are built in the background.
        /// </summary>
        public static (Brush? Background, Brush? Foreground) GetLogRowColors(string logType)
        {
            return logType switch
            {
                nameof(UIXLogType.Error) => (FnColor.Danger(0x1F), FnColor.Danger()),
                nameof(UIXLogType.Warning) => (FnColor.Warning(0x1F), FnColor.Warning()),
                nameof(GTNLogType.AppStart) => (FnColor.AppStart(0x1F), FnColor.AppStart()),
                _ => (null, null)
            };
        }
    }
}
