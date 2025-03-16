using System;

namespace Hermes.Core.Vehicle.Components.ComponentStates;

public abstract class ComponentState
{
    void UpdateState(ComponentState componentState)
    {
        throw new NotImplementedException("UpdateState is not implemented for this component!");
    }
}
