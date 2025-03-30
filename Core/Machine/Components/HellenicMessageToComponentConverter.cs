using System.ComponentModel;

namespace Hermes.Core.Machine.Components;

/*
TODO::ARGYRASPIDES() {

    It seems like this can be auto-generated too ... now I'm wondering whether or not all this translation garbage is
    worth it. Maybe I should have Vehicles' state just be componsed of messages to avoid this translation step? Then we
    can just have a CommandInterface that can take in a vehicle object, determine if the vehicle has the appropriate message
    type that tells us whether or not its capable of something, and then sends off the MAVLink message or whatever?
}
*/
public static class HellenicMessageToComponentConverter
{
    public static ComponentType GetComponentTypeByMessage(HellenicMessage message)
    {
        switch (message.Id)
        {
            // TODO::ARGYRASPIDES() { How do you know that heading is always coming from a GPS? What if it's coming from a dedicated
            // compass component? And how do you know altitude is coming from a GPS? What if its coming from an altimeter? }
            case (uint)HellenicMessageType.LatitudeLongitude:
                return ComponentType.GPS;
            case (uint)HellenicMessageType.Altitude:
                return ComponentType.GPS;
            case (uint)HellenicMessageType.Heading:
                return ComponentType.GPS;
        }

        return ComponentType.NULL;
    }

    public static Component GetComponentByType(ComponentType componentType)
    {
        switch (componentType)
        {
            case ComponentType.GPS:
                return new GPSComponent();
            default:
                break;
        }

        return null;
    }
}
