using KeyNest.Core.Models;

namespace KeyNest.Core.Input;

public sealed class ModifierStateChangedEventArgs : EventArgs
{
    public ModifierStateChangedEventArgs(ModifierMask state)
    {
        State = state;
    }

    public ModifierMask State { get; }
}

public sealed class KeyPressedEventArgs : EventArgs
{
    public KeyPressedEventArgs(int virtualKey)
    {
        VirtualKey = virtualKey;
    }

    public int VirtualKey { get; }
}
