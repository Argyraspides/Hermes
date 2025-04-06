using Hermes.Languages.HellenicGateway.Commands;

namespace Hermes.Languages.HellenicGateway;

public interface ICommandDispatcher
{
    public void DispatchNextCommand();
    public void BufferCommand(HellenicCommand command);
}
