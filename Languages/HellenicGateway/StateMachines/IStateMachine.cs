using System.Text.Json.Nodes;

namespace Hermes.Languages.HellenicGateway.StateMachines;

public interface IStateMachine
{
    void Start();
    void Stop();
    void HandleMessage(byte[] message);
}
