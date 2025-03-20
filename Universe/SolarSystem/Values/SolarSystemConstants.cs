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

    // MERCURY
    // Mercury is nearly spherical
    public const float MERCURY_SEMI_MAJOR_AXIS_LEN_M = 2_440_500;
    public const float MERCURY_SEMI_MAJOR_AXIS_LEN_KM = 2440.5f;
    public const float MERCURY_SEMI_MINOR_AXIS_LEN_M = 2_438_300;
    public const float MERCURY_SEMI_MINOR_AXIS_LEN_KM = 2438.3f;

    public const double MERCURY_ECCENTRICITY_SQUARED =
        ((MERCURY_SEMI_MAJOR_AXIS_LEN_KM * MERCURY_SEMI_MAJOR_AXIS_LEN_KM) -
         (MERCURY_SEMI_MINOR_AXIS_LEN_KM * MERCURY_SEMI_MINOR_AXIS_LEN_KM)) /
        (MERCURY_SEMI_MAJOR_AXIS_LEN_KM * MERCURY_SEMI_MAJOR_AXIS_LEN_KM);

    // VENUS
    // Venus is nearly spherical
    public const float VENUS_SEMI_MAJOR_AXIS_LEN_M = 6_051_800;
    public const float VENUS_SEMI_MAJOR_AXIS_LEN_KM = 6_051.8f;
    public const float VENUS_SEMI_MINOR_AXIS_LEN_M = 6_051_800;
    public const float VENUS_SEMI_MINOR_AXIS_LEN_KM = 6_051.8f;

    public const double VENUS_ECCENTRICITY_SQUARED =
        ((VENUS_SEMI_MAJOR_AXIS_LEN_KM * VENUS_SEMI_MAJOR_AXIS_LEN_KM) -
         (VENUS_SEMI_MINOR_AXIS_LEN_KM * VENUS_SEMI_MINOR_AXIS_LEN_KM)) /
        (VENUS_SEMI_MAJOR_AXIS_LEN_KM * VENUS_SEMI_MAJOR_AXIS_LEN_KM);

    // MARS
    public const float MARS_SEMI_MAJOR_AXIS_LEN_M = 3_396_200;
    public const float MARS_SEMI_MAJOR_AXIS_LEN_KM = 3_396.2f;
    public const float MARS_SEMI_MINOR_AXIS_LEN_M = 3_376_200;
    public const float MARS_SEMI_MINOR_AXIS_LEN_KM = 3_376.2f;

    public const double MARS_ECCENTRICITY_SQUARED =
        ((MARS_SEMI_MAJOR_AXIS_LEN_KM * MARS_SEMI_MAJOR_AXIS_LEN_KM) -
         (MARS_SEMI_MINOR_AXIS_LEN_KM * MARS_SEMI_MINOR_AXIS_LEN_KM)) /
        (MARS_SEMI_MAJOR_AXIS_LEN_KM * MARS_SEMI_MAJOR_AXIS_LEN_KM);

    // JUPITER
    public const float JUPITER_SEMI_MAJOR_AXIS_LEN_M = 71_492_000;
    public const float JUPITER_SEMI_MAJOR_AXIS_LEN_KM = 71_492.0f;
    public const float JUPITER_SEMI_MINOR_AXIS_LEN_M = 66_854_000;
    public const float JUPITER_SEMI_MINOR_AXIS_LEN_KM = 66_854.0f;

    public const double JUPITER_ECCENTRICITY_SQUARED =
        ((JUPITER_SEMI_MAJOR_AXIS_LEN_KM * JUPITER_SEMI_MAJOR_AXIS_LEN_KM) -
         (JUPITER_SEMI_MINOR_AXIS_LEN_KM * JUPITER_SEMI_MINOR_AXIS_LEN_KM)) /
        (JUPITER_SEMI_MAJOR_AXIS_LEN_KM * JUPITER_SEMI_MAJOR_AXIS_LEN_KM);

    // SATURN
    public const float SATURN_SEMI_MAJOR_AXIS_LEN_M = 60_268_000;
    public const float SATURN_SEMI_MAJOR_AXIS_LEN_KM = 60_268.0f;
    public const float SATURN_SEMI_MINOR_AXIS_LEN_M = 54_364_000;
    public const float SATURN_SEMI_MINOR_AXIS_LEN_KM = 54_364.0f;

    public const double SATURN_ECCENTRICITY_SQUARED =
        ((SATURN_SEMI_MAJOR_AXIS_LEN_KM * SATURN_SEMI_MAJOR_AXIS_LEN_KM) -
         (SATURN_SEMI_MINOR_AXIS_LEN_KM * SATURN_SEMI_MINOR_AXIS_LEN_KM)) /
        (SATURN_SEMI_MAJOR_AXIS_LEN_KM * SATURN_SEMI_MAJOR_AXIS_LEN_KM);

    // URANUS
    public const float URANUS_SEMI_MAJOR_AXIS_LEN_M = 25_559_000;
    public const float URANUS_SEMI_MAJOR_AXIS_LEN_KM = 25_559.0f;
    public const float URANUS_SEMI_MINOR_AXIS_LEN_M = 24_973_000;
    public const float URANUS_SEMI_MINOR_AXIS_LEN_KM = 24_973.0f;

    public const double URANUS_ECCENTRICITY_SQUARED =
        ((URANUS_SEMI_MAJOR_AXIS_LEN_KM * URANUS_SEMI_MAJOR_AXIS_LEN_KM) -
         (URANUS_SEMI_MINOR_AXIS_LEN_KM * URANUS_SEMI_MINOR_AXIS_LEN_KM)) /
        (URANUS_SEMI_MAJOR_AXIS_LEN_KM * URANUS_SEMI_MAJOR_AXIS_LEN_KM);

    // NEPTUNE
    public const float NEPTUNE_SEMI_MAJOR_AXIS_LEN_M = 25_559_000;
    public const float NEPTUNE_SEMI_MAJOR_AXIS_LEN_KM = 25_559.0f;
    public const float NEPTUNE_SEMI_MINOR_AXIS_LEN_M = 24_973_000;
    public const float NEPTUNE_SEMI_MINOR_AXIS_LEN_KM = 24_973.0f;

    public const double NEPTUNE_ECCENTRICITY_SQUARED =
        ((NEPTUNE_SEMI_MAJOR_AXIS_LEN_KM * NEPTUNE_SEMI_MAJOR_AXIS_LEN_KM) -
         (NEPTUNE_SEMI_MINOR_AXIS_LEN_KM * NEPTUNE_SEMI_MINOR_AXIS_LEN_KM)) /
        (NEPTUNE_SEMI_MAJOR_AXIS_LEN_KM * NEPTUNE_SEMI_MAJOR_AXIS_LEN_KM);

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
