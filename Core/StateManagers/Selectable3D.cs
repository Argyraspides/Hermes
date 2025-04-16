using System.Runtime.CompilerServices;
using Godot;

[assembly: InternalsVisibleTo("InputManager")]

namespace Hermes.Core.StateManagers;

public interface Selectable3D
{
    protected internal void OnMouseEntered();
    protected internal void OnMouseExited();
    protected internal void OnMouseClicked(MouseButton button);
}
