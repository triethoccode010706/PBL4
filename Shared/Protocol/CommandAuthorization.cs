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
        public static bool CanExecute( CommandType commandType,AuthenticationStatus authenticationStatus)
        {
            if (commandType == CommandType.DISCONNECT)
            {
                return true;
            }

            if (commandType == CommandType.AUTHENTICATE)
            {
                return authenticationStatus ==
                    AuthenticationStatus.NotAuthenticated;
            }

            if (authenticationStatus !=
                AuthenticationStatus.Authenticated)
            {
                return false;
            }

            return Enum.IsDefined(typeof(CommandType), commandType);
        }
    }
}
