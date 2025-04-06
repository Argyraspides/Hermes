namespace Hermes.Languages.HellenicGateway.StateMachines;

using System.Collections.Generic;
using System.Timers;

enum MAVLinkState
{

};

public class MAVLinkStateMachine
{
    // Currently connected drones identified by their MAVLink system ID
    private HashSet<int> m_connectedDrones = new HashSet<int>();

    // Heartbeat timers for each connected drone
    private Dictionary<int, Timer> m_heartBeatTimers = new Dictionary<int, Timer>();

    // Current state of each drone in terms of their MAVLink connection to Hermes
    private Dictionary<int, MAVLinkState> m_states = new Dictionary<int, MAVLinkState>();

}
