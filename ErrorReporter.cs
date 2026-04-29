using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MapSearchApp
{
    /// <summary>
    /// Lightweight fire-and-forget error reporter.
    /// Sends a JSON POST to your back-end server on any unhandled exception.
    /// Configure the endpoint below — never blocks the UI thread.
    /// </summary>
    internal static class ErrorReporter
    {
        // ── Configure this to your server ──────────────────────────────────
        private const string ErrorEndpoint = "https://api.yourdomain.com/errors";
        // ───────────────────────────────────────────────────────────────────

        private const string AppName    = "MapSearchApp";
        private const string AppVersion = "1.0.3";

        private static readonly HttpClient _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(6)
        };

        /// <summary>
        /// Call this from App.xaml.cs to register global handlers.
        /// </summary>
        public static void Register()
        {
            // WPF dispatcher thread exceptions
            if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.DispatcherUnhandledException += (s, e) =>
                {
                    _ = ReportAsync(e.Exception, "DispatcherUnhandledException");
                    e.Handled = true; // Keep app alive; remove if you want crash-on-error
                };
            }

            // Background thread / Task exceptions
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    _ = ReportAsync(ex, "UnhandledException");
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                _ = ReportAsync(e.Exception, "UnobservedTaskException");
                e.SetObserved();
            };
        }

        /// <summary>
        /// Manually report an exception from a catch block.
        /// Fire-and-forget — never awaited by callers.
        /// </summary>
        public static void Report(Exception ex, string context = "")
            => _ = ReportAsync(ex, context);

        private static async Task ReportAsync(Exception ex, string context)
        {
            try
            {
                var payload = new
                {
                    app       = AppName,
                    version   = AppVersion,
                    context   = context,
                    message   = ex.Message,
                    type      = ex.GetType().FullName,
                    stack     = ex.StackTrace,
                    inner     = ex.InnerException?.Message,
                    machine   = Environment.MachineName,
                    os        = Environment.OSVersion.VersionString,
                    timestamp = DateTimeOffset.UtcNow.ToString("o")
                };

                string json    = JsonSerializer.Serialize(payload);
                using var body = new StringContent(json, Encoding.UTF8, "application/json");
                await _client.PostAsync(ErrorEndpoint, body).ConfigureAwait(false);
            }
            catch
            {
                // Never let the reporter itself crash the app
            }
        }
    }
}
