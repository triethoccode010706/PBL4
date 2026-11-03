using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Application
{
    public interface IKeyboardController
    {
        void KeyDown(int virtualKeyCode, bool isExtended);

        void KeyUp(int virtualKeyCode, bool isExtended);

        void ReleaseAll();
    }
}
