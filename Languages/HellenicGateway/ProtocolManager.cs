using System;
using System.Collections.Generic;
using Godot;
using Hermes.Common.Communications.WorldListener;
using Hermes.Core.Vehicle;
using Hermes.Languages.HellenicGateway.Adapters;

namespace Hermes.Languages.HellenicGateway;

public partial class ProtocolManager : Node
{
    public static ProtocolManager Instance;

    public ProtocolManager()
    {
        m_protocolAdapters = new List<IProtocolAdapter>() { new MAVLinkAdapter() };
        Instance = this;
    }

    [Signal]
    public delegate void HellenicMessageReceivedEventHandler(HellenicMessage message);

    private List<IProtocolAdapter> m_protocolAdapters;
    ICommandDispatcher m_commandDispatcher;

    public override void _Process(double delta)
    {
        foreach (IProtocolAdapter protocolAdapter in m_protocolAdapters)
        {
            if (protocolAdapter.GetNextHellenicMessage() is HellenicMessage nextMessage)
            {
                VehicleManager.Instance.OnHellenicMessageReceived(nextMessage);
                // EmitSignal(SignalName.HellenicMessageReceived, nextMessage);
            }
        }
    }

    public override void _Ready()
    {
        foreach (IProtocolAdapter protocolAdapter in m_protocolAdapters)
        {
            protocolAdapter.Start();
        }
    }

    public override void _ExitTree()
    {
        foreach (IProtocolAdapter protocolAdapter in m_protocolAdapters)
        {
            protocolAdapter.Stop();
        }
    }
}
