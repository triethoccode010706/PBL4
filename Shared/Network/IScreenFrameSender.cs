using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Models;

namespace Shared.Network
{
    public interface IScreenFrameSender
    {
        Task SendAsync(
            ScreenFrame frame,
            CancellationToken cancellationToken);
    }
}