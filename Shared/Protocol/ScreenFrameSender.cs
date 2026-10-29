using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Models;
using Shared.Network;

namespace Shared.Protocol
{
    public sealed class ScreenFrameSender : IScreenFrameSender
    {
        private readonly IDataSender _sender;

        private readonly IMessageSerializer _serializer =
            new MessageSerializer();

        private readonly IMessageFramer _framer =
            new MessageFramer();

        private readonly IMessageValidator _validator =
            new MessageValidator();

        public ScreenFrameSender(IDataSender sender)
        {
            _sender = sender;
        }

        public async Task SendAsync(
            ScreenFrame frame,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var envelope = new ProtocolEnvelope
            {
                Kind = ProtocolMessageKinds.ScreenFrame,
                ScreenFrame = frame
            };

            _validator.ValidateEnvelope(envelope);

            byte[] payload =
                _serializer.SerializeEnvelope(envelope);

            byte[] packet = _framer.Frame(payload);

            await _sender.SendAsync(
                packet,
                cancellationToken);
        }
    }
}