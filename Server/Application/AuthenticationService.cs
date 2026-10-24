using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.Json;
using Server.Infrastructure;
using Shared.Models;
using Shared.Security;

namespace Server.Application
{
    public sealed class AuthenticationService
        : IAuthenticationService
    {
        private readonly string _secretKey;

        public AuthenticationService(string secretKey)
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

        public async Task<bool> AuthenticateAsync(
            IClientSession session,
            CancellationToken cancellationToken)
        {
            using var deadline =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            deadline.CancelAfter(TimeSpan.FromSeconds(5));

            string challenge =
                AuthenticationProof.CreateChallenge();

            var request = new AuthenticationChallengeDto
            {
                Challenge = challenge
            };

            string json = JsonSerializer.Serialize(request);

            Command command = Command.Create(
                CommandType.AUTHENTICATE,
                json);

            Response response = await session.SendCommandAsync(
                command,
                deadline.Token);

            deadline.Token.ThrowIfCancellationRequested();

            if (!response.Success)
            {
                return false;
            }

            AuthenticationProofDto? result;

            try
            {
                result =
                    JsonSerializer.Deserialize
                        <AuthenticationProofDto>(response.Data);
            }
            catch (JsonException)
            {
                return false;
            }

            if (result == null ||
                string.IsNullOrWhiteSpace(result.Proof) ||
                result.Proof.Length != 44)
            {
                return false;
            }

            string proofInput =
                "PBL4:client-proof:v1:" + challenge;

            bool valid = AuthenticationProof.VerifyProof(
                _secretKey,
                proofInput,
                result.Proof);

            deadline.Token.ThrowIfCancellationRequested();

            return valid;
        }
    }
}