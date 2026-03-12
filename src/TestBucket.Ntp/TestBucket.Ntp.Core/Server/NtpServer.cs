using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Core.Server
{
    /// <summary>
    /// RFC4330 SNTP/NTP server implementation
    /// </summary>
    public class NtpServer : IDisposable
    {
        private readonly ITimeProvider _timeProvider;
        private readonly ILogger<NtpServer> _logger;
        private const int _port = 60123;
        private UdpClient? _udpServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenerTask;
        private bool _disposed;

        public event EventHandler<NtpQueryEvent>? QueryEvent;

        public NtpServer(ITimeProvider timeProvider, ILogger<NtpServer> logger)
        {
            _timeProvider = timeProvider;
            _logger = logger;
        }

        /// <summary>
        /// Starts the NTP server
        /// </summary>
        public void Start()
        {
            if (_udpServer != null)
            {
                throw new InvalidOperationException("Server is already running");
            }

            try
            {
                _udpServer = new UdpClient(_port);
                _cancellationTokenSource = new CancellationTokenSource();
                _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token));

                _logger.LogInformation("NTP server started on port {Port}", _port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start NTP server on port {Port}", _port);
                throw;
            }
        }

        /// <summary>
        /// Stops the NTP server
        /// </summary>
        public async Task StopAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();

                if (_listenerTask != null)
                {
                    try
                    {
                        await _listenerTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when stopping
                    }
                }
            }

            _udpServer?.Close();
            _udpServer = null;

            _logger.LogInformation("NTP server stopped");
        }

        /// <summary>
        /// Main listening loop for NTP requests
        /// </summary>
        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("NTP server listening for requests...");

            while (!cancellationToken.IsCancellationRequested && _udpServer != null)
            {
                try
                {
                    var result = await _udpServer.ReceiveAsync(cancellationToken);
                    _ = Task.Run(() => HandleRequestAsync(result.Buffer, result.RemoteEndPoint), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error receiving NTP request");
                }
            }
        }

        /// <summary>
        /// Handles an individual NTP request
        /// </summary>
        private async Task HandleRequestAsync(byte[] requestData, IPEndPoint clientEndPoint)
        {
            CancellationToken cancellationToken = default;
            try
            {
                _logger.LogDebug("Received NTP request from {ClientAddress}", clientEndPoint.Address);

                // Parse the incoming NTP packet
                var requestPacket = NtpPacketParser.ParsePacket(requestData);
                if(requestPacket is null)
                {
                    _logger.LogWarning("Invalid NTP request from {ClientAddress} (null)", clientEndPoint.Address);
                    return;
                }
                if (!NtpPacketParser.IsValidClientRequest(requestPacket))
                {
                    _logger.LogWarning("Invalid NTP request from {ClientAddress}", clientEndPoint.Address);
                    return;
                }

                // Create request context
                var context = new NtpRequestContext
                {
                    ClientAddress = clientEndPoint.Address
                };

                // Get time from the time provider
                var timeResponse = await _timeProvider.GetTimeAsync(context, cancellationToken);

                // Create response packet
                byte[] responseData = Array.Empty<byte>();
                if(timeResponse.ResponseBytes != null)
                {
                    // Respond with fixed response data
                    responseData = timeResponse.ResponseBytes;
                }
                else
                {
                     responseData = NtpPacketParser.CreateResponse(requestPacket, timeResponse.Timestamp, timeResponse);
                }

                // Send response
                if (_udpServer != null)
                {
                    await _udpServer.SendAsync(responseData, responseData.Length, clientEndPoint);


                    _logger.LogDebug("Sent NTP response to {ClientAddress} with time {Time}",
                        clientEndPoint.Address, timeResponse.Timestamp);

                    // Send event for UI reasons
                    var queryEvent = new NtpQueryEvent
                    {
                        ClientAddress = clientEndPoint.Address,
                        RequestData = requestData,
                        ResponseData = responseData,
                        Request = requestPacket,
                        Response = NtpPacketParser.ParsePacket(responseData) // Parse response for event
                    };
                    QueryEvent?.Invoke(this, queryEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling NTP request from {ClientAddress}", clientEndPoint.Address);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            StopAsync().GetAwaiter().GetResult();
            _cancellationTokenSource?.Dispose();
            _udpServer?.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
