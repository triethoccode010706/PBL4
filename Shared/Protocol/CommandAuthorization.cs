using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Models;

namespace Shared.Protocol
{
    public static class CommandAuthorization
    {
        public static bool CanExecute(
            CommandType commandType,
            AuthenticationStatus authenticationStatus)
        {
            if (commandType == CommandType.DISCONNECT)
            {
                return true;
            }

            if (authenticationStatus !=
                AuthenticationStatus.Authenticated)
            {
                return false;
            }

            return true;
        }
    }
}
