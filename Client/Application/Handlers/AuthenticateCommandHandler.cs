using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using Shared.Models;
using Shared.Protocol;
using Shared.Security;

namespace Client.Application.Handlers
{
    public sealed class AuthenticateCommandHandler : ICommandHandler
    {
        private readonly string _secretKey;

        public AuthenticateCommandHandler(string secretKey)
        {
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentException(
                    "Chưa cấu hình khóa xác thực.");
            }

            byte[] key = Convert.FromBase64String(secretKey);

            if (key.Length != 32)
            {
                throw new ArgumentException(
                    "Khóa xác thực phải có 32 byte.");
            }

            _secretKey = secretKey;
        }

        public CommandType CommandType =>
            CommandType.AUTHENTICATE;

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AuthenticationChallengeDto? request;

            try
            {
                request =
                    JsonSerializer.Deserialize
                        <AuthenticationChallengeDto>(command.Data);
            }
            catch (JsonException)
            {
                return Task.FromResult(
                    Response.Fail(
                        command.RequestId,
                        ErrorCodes.AuthenticationFailed,
                        "Dữ liệu xác thực không hợp lệ."));
            }

            if (request == null ||
                !IsValidChallenge(request.Challenge))
            {
                return Task.FromResult(
                    Response.Fail(
                        command.RequestId,
                        ErrorCodes.AuthenticationFailed,
                        "Challenge không hợp lệ."));
            }

            // Phân biệt mục đích của proof.
            string proofInput =
                "PBL4:client-proof:v1:" + request.Challenge;

            string proof = AuthenticationProof.CreateProof(
                _secretKey,
                proofInput);

            var result = new AuthenticationProofDto
            {
                Proof = proof
            };

            string json = JsonSerializer.Serialize(result);

            return Task.FromResult(
                Response.Ok(
                    command.RequestId,
                    "Client proof",
                    json));
        }

        private static bool IsValidChallenge(string? challenge)
        {
            if (string.IsNullOrWhiteSpace(challenge))
            {
                return false;
            }

            // 32 byte có biểu diễn Base64 chuẩn dài 44 ký tự.
            if (challenge.Length != 44)
            {
                return false;
            }

            try
            {
                byte[] bytes = Convert.FromBase64String(challenge);

                return bytes.Length == 32;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}