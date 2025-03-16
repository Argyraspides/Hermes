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
    public static void UpdateComponent(HellenicMessage message, Component component)
    {
        switch (message.EntityId)
        {
            // TODO::ARGYRASPIDES() { The message IDs should have corresponding enums }
            // for now 0 maps to latitudelongitude
            case 0:
                GPSComponent gpsComponent = component as GPSComponent;
                LatitudeLongitude location = message as LatitudeLongitude;
                gpsComponent.Latitude = location.Lat;
                gpsComponent.Longitude = location.Lon;
                break;
        }
    }

    // TODO::ARGYRASPIDES() { The two functions below are kinda mirrors of each other ... i dont like it. Find a better way }
    public static ComponentType GetComponentType(HellenicMessage message)
    {
        switch (message.EntityId)
        {
            // TODO::ARGYRASPIDES() { The message IDs should have corresponding enums }
            // for now 0 maps to latitudelongitude
            case 0:
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
