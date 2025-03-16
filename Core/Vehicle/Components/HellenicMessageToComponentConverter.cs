namespace Hermes.Core.Vehicle.Components;

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
    // TODO::ARGYRASPIDES() { The two functions below are kinda mirrors of each other ... i dont like it. Find a better way }
    public static ComponentType GetComponentTypeByMessage(HellenicMessage message)
    {
        switch (message.Id)
        {
            case (uint)HellenicMessageType.LatitudeLongitude:
                return ComponentType.GPS;
            default:
                break;
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
