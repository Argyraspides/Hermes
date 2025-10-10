using Godot;
using Hermes.Core.Machine;
using Hermes.Languages.HellenicGateway;

namespace Hermes.Core;

public partial class HermesApplication : CanvasLayer
{
    private ProtocolManager _protocolManager;
    private MachineManager _machineManager;

    public override void _Ready()
    {
        _protocolManager = new ProtocolManager();
        AddChild(_protocolManager);

        _machineManager = new MachineManager();
        AddChild(_machineManager);
    }
}
