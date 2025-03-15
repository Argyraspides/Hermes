namespace Hermes.Core.Vehicle.Components;

public static class HellenicMessageToComponentConverter
{
    public static void UpdateComponent(HellenicMessage message, Component component)
    {
        switch (message.ID)
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
        switch (message.ID)
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
