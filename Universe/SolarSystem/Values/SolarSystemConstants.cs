/*




88        88  88888888888  88888888ba   88b           d88  88888888888  ad88888ba
88        88  88           88      "8b  888b         d888  88          d8"     "8b
88        88  88           88      ,8P  88`8b       d8'88  88          Y8,
88aaaaaaaa88  88aaaaa      88aaaaaa8P'  88 `8b     d8' 88  88aaaaa     `Y8aaaaa,
88""""""""88  88"""""      88""""88'    88  `8b   d8'  88  88"""""       `"""""8b,
88        88  88           88    `8b    88   `8b d8'   88  88                  `8b
88        88  88           88     `8b   88    `888'    88  88          Y8a     a8P
88        88  88888888888  88      `8b  88     `8'     88  88888888888  "Y88888P"


                            MESSENGER OF THE MACHINES

*/


public static class SolarSystemConstants
{
    // Semi *major* axis length of the Earth in meters and kilometers
    // using the WGS84 ellipsoid standard
    public const float EARTH_SEMI_MAJOR_AXIS_LEN_M = 6_378_137;
    public const float EARTH_SEMI_MAJOR_AXIS_LEN_KM = 6_378.137f;

    // Semi *minor* axis length of the Earth in meters and kilometers
    // using the WGS84 ellipsoid standard
    public const float EARTH_SEMI_MINOR_AXIS_LEN_M = 6_356_752.314245f;
    public const float EARTH_SEMI_MINOR_AXIS_LEN_KM = 6_356.752314245f;

    // First eccentricity squared with the WGS84 ellipsoid
    // Calculated as: (a² - b²) / a², where a is semi-major axis and b is semi-minor axis
    public const double EARTH_ECCENTRICITY_SQUARED =
        ((EARTH_SEMI_MAJOR_AXIS_LEN_KM * EARTH_SEMI_MAJOR_AXIS_LEN_KM) -
         (EARTH_SEMI_MINOR_AXIS_LEN_KM * EARTH_SEMI_MINOR_AXIS_LEN_KM)) /
        (EARTH_SEMI_MAJOR_AXIS_LEN_KM * EARTH_SEMI_MAJOR_AXIS_LEN_KM);


    // Semi *major* axis length of the a "blank planet" in meters and kilometers
    // Can be used as a default value for debugging/test purposes. This is double
    // the size of the semi *minor* axis to make it abundantly clear that this is
    // not a normal planet
    public const float BLANK_PLANET_SEMI_MAJOR_AXIS_LEN_M = 5_000_000f;
    public const float BLANK_PLANET_SEMI_MAJOR_AXIS_LEN_KM = 5_000.000f;

    // Semi *MINOR* axis length of the a "blank planet" in meters and kilometers
    // Can be used as a default value for debugging/test purposes. This is half
    // the size of the semi *major* axis to make it abundantly clear that this is
    // not a normal planet
    public const float BLANK_PLANET_SEMI_MINOR_AXIS_LEN_M = 2_500_000f;
    public const float BLANK_PLANET_SEMI_MINOR_AXIS_LEN_KM = 2_500.000f;
}
