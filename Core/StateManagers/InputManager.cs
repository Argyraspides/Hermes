#nullable enable

using System.Collections.Generic;

namespace Hermes.Core.StateManagers;

using Godot;
using Hermes.Common.HermesUtils;

public partial class InputManager : Node
{
    private Selectable3D? m_lastHoveredObject = null;

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion)
        {
            HandleMouseSelection();
        }

        if (@event is InputEventMouseButton buttonEvent)
        {
            HandleMouseClick(buttonEvent);
        }
    }

    private void HandleMouseClick(InputEventMouseButton buttonEvent)
    {
        if (buttonEvent.IsReleased()) return;

        Godot.Collections.Dictionary raycastResult = HermesUtils.MouseRaycast(GetViewport());

        if(raycastResult.Count == 0) return;

        Selectable3D hitObject = raycastResult["collider"].Obj as Selectable3D;
        hitObject?.OnMouseClicked(buttonEvent.ButtonIndex);
    }

    private void HandleMouseSelection()
    {

        Godot.Collections.Dictionary raycastResult = HermesUtils.MouseRaycast(GetViewport());

        Selectable3D hitObject = null;

        if(raycastResult.Count > 0) hitObject = raycastResult["collider"].Obj as Selectable3D;

        if (hitObject != null)
        {
            if(hitObject == m_lastHoveredObject) return;

            m_lastHoveredObject?.OnMouseExited();
            hitObject.OnMouseEntered();
            m_lastHoveredObject = hitObject;
        }
        else
        {
            m_lastHoveredObject?.OnMouseExited();
            m_lastHoveredObject = null;
        }
    }
}
