using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Server.Infrastructure;

namespace Server.Application
{
    public interface IAuthenticationService
    {
        Task<bool> AuthenticateAsync(
            IClientSession session,
            CancellationToken cancellationToken);
    }
}