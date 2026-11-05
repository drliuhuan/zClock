// SPDX-License-Identifier: GPL-3.0-only
using System.Runtime.InteropServices;

namespace zClock
{
    // HWND is a hidden, vtable-only member of the PowerPoint COM ABI.
    // Slots 3..6 are IDispatch and 7..19 precede HWND; they are never invoked here.
    // No Office assembly or implementation is bundled.
    [ComImport, Guid("91493453-5A91-11CF-8700-00AA0060263B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISlideShowWindowHandle
    {
        void Reserved03();
        void Reserved04();
        void Reserved05();
        void Reserved06();
        void Reserved07();
        void Reserved08();
        void Reserved09();
        void Reserved10();
        void Reserved11();
        void Reserved12();
        void Reserved13();
        void Reserved14();
        void Reserved15();
        void Reserved16();
        void Reserved17();
        void Reserved18();
        void Reserved19();
        int HWND { get; }
    }
}
