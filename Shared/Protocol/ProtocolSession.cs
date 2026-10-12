using System.Collections.Concurrent;
using Shared.Models;
using Shared.Network;

namespace Shared.Protocol
{
    public class ProtocolSession
    {
        private readonly IDataSender _sender;
        private readonly Stream _stream;
        private readonly IMessageFramer _framer;
        private readonly IMessageSerializer _serializer;
        private readonly IMessageValidator _validator;
        private readonly CommandDispatcher _dispatcher;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Response>> _pending = new();

        public event Action<Command>? CommandReceived;
        public event Action<Response>? ResponseReceived;
        public event Action<Response>? ResponseSent;

        public ProtocolSession(
            IDataSender sender,
            Stream stream,
            CommandDispatcher dispatcher,
            IMessageFramer? framer = null,
            IMessageSerializer? serializer = null,
            IMessageValidator? validator = null)
        {
            _sender = sender;
            _stream = stream;
            _dispatcher = dispatcher;
            _framer = framer ?? new MessageFramer();
            _serializer = serializer ?? new MessageSerializer();
            _validator = validator ?? new MessageValidator();
        }

        public async Task<Response> SendCommandAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            _validator.ValidateCommand(command);

            var tcs = new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!_pending.TryAdd(command.RequestId, tcs))
            {
                throw new InvalidOperationException($"RequestId trùng: {command.RequestId}");
            }

            using CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (_pending.TryRemove(command.RequestId, out TaskCompletionSource<Response>? pending))
                {
                    pending.TrySetCanceled(cancellationToken);
                }
            });

            try
            {
                await WriteEnvelopeAsync(new ProtocolEnvelope
                {
                    Kind = ProtocolMessageKinds.Command,
                    Command = command
                }, cancellationToken);

                return await tcs.Task.WaitAsync(cancellationToken);
            }
            catch
            {
                _pending.TryRemove(command.RequestId, out _);
                throw;
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    byte[]? payload = await _framer.ReadPayloadAsync(_stream, cancellationToken);
                    if (payload == null)
                    {
                        break;
                    }

                    ProtocolEnvelope envelope;
                    try
                    {
                        envelope = _serializer.DeserializeEnvelope(payload);
                        _validator.ValidateEnvelope(envelope);
                    }
                    catch (Exception ex)
                    {
                        await WriteEnvelopeAsync(new ProtocolEnvelope
                        {
                            Kind = ProtocolMessageKinds.Response,
                            Response = Response.Fail(
                                Guid.NewGuid().ToString(),
                                ErrorCodes.InvalidMessage,
                                ex.Message)
                        }, cancellationToken);
                        continue;
                    }

                    if (envelope.Kind == ProtocolMessageKinds.Command && envelope.Command != null)
                    {
                        Command command = envelope.Command;
                        await HandleInboundCommandAsync(command, cancellationToken);
                        if (command.Type == CommandType.DISCONNECT)
                        {
                            break;
                        }
                    }
                    else if (envelope.Kind == ProtocolMessageKinds.Response && envelope.Response != null)
                    {
                        HandleInboundResponse(envelope.Response);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                CancelPending();
            }
        }

        private async Task HandleInboundCommandAsync(Command command, CancellationToken cancellationToken)
        {
            CommandReceived?.Invoke(command);

            Response response = await _dispatcher.DispatchAsync(command, cancellationToken);
            await WriteEnvelopeAsync(new ProtocolEnvelope
            {
                Kind = ProtocolMessageKinds.Response,
                Response = response
            }, cancellationToken);

            ResponseSent?.Invoke(response);
        }

        private void HandleInboundResponse(Response response)
        {
            ResponseReceived?.Invoke(response);

            if (_pending.TryRemove(response.RequestId, out TaskCompletionSource<Response>? pending))
            {
                pending.TrySetResult(response);
            }
        }

        private async Task WriteEnvelopeAsync(ProtocolEnvelope envelope, CancellationToken cancellationToken)
        {
            byte[] payload = _serializer.SerializeEnvelope(envelope);
            byte[] packet = _framer.Frame(payload);
            await _sender.SendAsync(packet, cancellationToken);
        }

        private void CancelPending()
        {
            foreach (var pair in _pending)
            {
                if (_pending.TryRemove(pair.Key, out TaskCompletionSource<Response>? pending))
                {
                    pending.TrySetCanceled();
                }
            }
        }
    }
}
