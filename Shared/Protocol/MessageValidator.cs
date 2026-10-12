using Shared.Models;

namespace Shared.Protocol
{
    public class MessageValidator : IMessageValidator
    {
        public const int MaxDataLength = 8 * 1024 * 1024;
        public const int MaxRequestIdLength = 128;

        public void ValidateEnvelope(ProtocolEnvelope envelope)
        {
            ArgumentNullException.ThrowIfNull(envelope);

            if (envelope.Kind == ProtocolMessageKinds.Command)
            {
                if (envelope.Command == null)
                {
                    throw new ProtocolValidationException("Envelope command thiếu nội dung.");
                }

                if (envelope.Response != null)
                {
                    throw new ProtocolValidationException("Envelope command không được kèm response.");
                }

                ValidateCommand(envelope.Command);
                return;
            }

            if (envelope.Kind == ProtocolMessageKinds.Response)
            {
                if (envelope.Response == null)
                {
                    throw new ProtocolValidationException("Envelope response thiếu nội dung.");
                }

                if (envelope.Command != null)
                {
                    throw new ProtocolValidationException("Envelope response không được kèm command.");
                }

                ValidateResponse(envelope.Response);
                return;
            }

            throw new ProtocolValidationException($"Kind không hợp lệ: {envelope.Kind}.");
        }

        public void ValidateCommand(Command command)
        {
            ArgumentNullException.ThrowIfNull(command);
            ValidateRequestId(command.RequestId);

            if (!Enum.IsDefined(command.Type))
            {
                throw new ProtocolValidationException($"CommandType không hợp lệ: {command.Type}.");
            }

            ValidateData(command.Data);
        }

        public void ValidateResponse(Response response)
        {
            ArgumentNullException.ThrowIfNull(response);
            ValidateRequestId(response.RequestId);
            ValidateData(response.Data);

            if (response.Success)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(response.ErrorCode))
            {
                throw new ProtocolValidationException("Response lỗi phải có ErrorCode.");
            }
        }

        private static void ValidateRequestId(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new ProtocolValidationException("RequestId không được rỗng.");
            }

            if (requestId.Length > MaxRequestIdLength)
            {
                throw new ProtocolValidationException("RequestId quá dài.");
            }
        }

        private static void ValidateData(string? data)
        {
            if (data != null && data.Length > MaxDataLength)
            {
                throw new ProtocolValidationException("Trường Data vượt giới hạn.");
            }
        }
    }
}
