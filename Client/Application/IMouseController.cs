using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shared.Models;

namespace Client.Application
{
    public interface IMouseController
    {
        void MoveTo(double xRatio, double yRatio);

        void Click(
            double xRatio,
            double yRatio,
            MouseButtonType button);
    }
}