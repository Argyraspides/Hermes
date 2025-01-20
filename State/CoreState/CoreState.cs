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


using Godot;
using System.Collections.Generic;


// CoreState is a generic state object that applies to all vehicles regardless of their type.
// All vehicles, being physical entities of the universe, must have properties
// such as velocity, acceleration, position, etc.
//
// State is stored in a dictionary for flexible updates and additions.
// States with an NaN value are considered uninitialized
public partial class CoreState : Node
{
    private Dictionary<string, object> m_stateValues = new Dictionary<string, object>();

    // Constants for state keys to prevent typos and allow refactoring
    private static class StateKeys
    {
        // Floats
        public const string VEHICLE_ID = "VEHICLE_ID";
        public const string EARTH_HEADING = "EARTH_HEADING";

        // Vector3s
        public const string EARTH_POSITION = "EARTH_POSITION";
        public const string LOCAL_POSITION = "LOCAL_POSITION";
        public const string ATTITUDE = "ATTITUDE";
        public const string GROUND_VELOCITY = "GROUND_VELOCITY";
        public const string GROUND_ACCELERATION = "GROUND_ACCELERATION";

        // Enums
        public const string VEHICLE_TYPE = "VEHICLE_TYPE";
    }

    public CoreState()
    {
        m_stateValues[StateKeys.VEHICLE_ID] = float.NaN;
        m_stateValues[StateKeys.EARTH_HEADING] = float.NaN;

        m_stateValues[StateKeys.EARTH_POSITION] = new Vector3(float.NaN, float.NaN, float.NaN);
        m_stateValues[StateKeys.LOCAL_POSITION] = new Vector3(float.NaN, float.NaN, float.NaN);
        m_stateValues[StateKeys.ATTITUDE] = new Vector3(float.NaN, float.NaN, float.NaN);
        m_stateValues[StateKeys.GROUND_VELOCITY] = new Vector3(float.NaN, float.NaN, float.NaN);
        m_stateValues[StateKeys.GROUND_ACCELERATION] = new Vector3(float.NaN, float.NaN, float.NaN);

        m_stateValues[StateKeys.VEHICLE_TYPE] = VehicleType.Unknown;
    }

    public T GetState<T>(string key)
    {
        if (m_stateValues.TryGetValue(key, out object value))
        {
            return (T)value;
        }
        throw new KeyNotFoundException($"State key '{key}' not found");
    }

    public void SetState<T>(string key, T value)
    {
        if (m_stateValues.ContainsKey(key))
        {
            m_stateValues[key] = value;
        }
        else
        {
            throw new KeyNotFoundException($"State key '{key}' not found");
        }
    }

    // Convenience properties that use the dictionary internally
    public VehicleType VehicleType
    {
        get => GetState<VehicleType>(StateKeys.VEHICLE_TYPE);
        set => SetState(StateKeys.VEHICLE_TYPE, value);
    }

    public float VehicleId
    {
        get => GetState<float>(StateKeys.VEHICLE_ID);
        set => SetState(StateKeys.VEHICLE_ID, value);
    }

    public Vector3 EarthPosition
    {
        get => GetState<Vector3>(StateKeys.EARTH_POSITION);
        set => SetState(StateKeys.EARTH_POSITION, value);
    }

    public float EarthHeading
    {
        get => GetState<float>(StateKeys.EARTH_HEADING);
        set => SetState(StateKeys.EARTH_HEADING, value);
    }

    public Vector3 LocalPosition
    {
        get => GetState<Vector3>(StateKeys.LOCAL_POSITION);
        set => SetState(StateKeys.LOCAL_POSITION, value);
    }

    public Vector3 Attitude
    {
        get => GetState<Vector3>(StateKeys.ATTITUDE);
        set => SetState(StateKeys.ATTITUDE, value);
    }

    public Vector3 GroundVel
    {
        get => GetState<Vector3>(StateKeys.GROUND_VELOCITY);
        set => SetState(StateKeys.GROUND_VELOCITY, value);
    }

    public Vector3 GroundAcc
    {
        get => GetState<Vector3>(StateKeys.GROUND_ACCELERATION);
        set => SetState(StateKeys.GROUND_ACCELERATION, value);
    }


    // Helper method to check if a CoreState field is in its "uninitialized" state
    private bool IsUninitialized(object value)
    {
        if (value is float f)
        {
            return float.IsNaN(f);
        }
        if (value is Vector3 v)
        {
            return float.IsNaN(v.X) && float.IsNaN(v.Y) && float.IsNaN(v.Z);
        }
        if (value is VehicleType vt)
        {
            return vt == VehicleType.Unknown;
        }

        return false;
    }


    // Copies known fields from another CoreState instance
    public void CopyKnownFields(CoreState fromState)
    {
        if (fromState == null)
            return;

        foreach (var kvp in fromState.m_stateValues)
        {
            if (!IsUninitialized(kvp.Value))
            {
                m_stateValues[kvp.Key] = kvp.Value;
            }
        }
    }
}
