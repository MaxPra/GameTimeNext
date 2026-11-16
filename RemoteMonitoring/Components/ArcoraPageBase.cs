using Microsoft.AspNetCore.Components;
using RemoteMonitoring.Base;

namespace RemoteMonitoring.Components
{
    /// <summary>
    /// Page that shows data of arcora: loads it when shown (and when its parameters change) and again when the
    /// connection to arcora comes back, so a page opened during a lost connection fills itself afterwards.
    /// </summary>
    public abstract class ArcoraPageBase : ComponentBase, IDisposable
    {
        [Inject]
        protected SocketClient Socket { get; set; } = null!;

        protected bool IsConnected => Socket.State == ConnectionState.Connected;

        /// <summary>
        /// Loads the data of the page. Keeps what is already shown when there is no answer.
        /// </summary>
        protected abstract Task LoadAsync();

        protected override void OnInitialized()
        {
            Socket.ConnectionStateChanged += OnConnectionStateChanged;
        }

        protected override Task OnParametersSetAsync()
        {
            return LoadAsync();
        }

        private void OnConnectionStateChanged(object? sender, ConnectionState state)
        {
            // Raised on the thread of the socket client
            _ = InvokeAsync(async () =>
            {
                if (state == ConnectionState.Connected)
                    await LoadAsync();

                StateHasChanged();
            });
        }

        /// <summary>
        /// Cleanup of the page (unsubscribe its own events).
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        public void Dispose()
        {
            Socket.ConnectionStateChanged -= OnConnectionStateChanged;
            OnDispose();
            GC.SuppressFinalize(this);
        }
    }
}
