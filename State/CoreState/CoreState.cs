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
    private Dictionary<string, object> _stateValues = new Dictionary<string, object>();
    
    // Constants for state keys to prevent typos and allow refactoring
    public static class StateKeys
    {
        // Floats
        public const string VehicleId           = "VehicleId";
        public const string EarthHeading        = "EarthHeading";

        // Vector3s
        public const string EarthPosition       = "EarthPosition";
        public const string LocalPosition       = "LocalPosition";
        public const string Attitude            = "Attitude";
        public const string GroundVelocity      = "GroundVelocity";
        public const string GroundAcceleration  = "GroundAcceleration";

        // Enums
        public const string VehicleType         = "VehicleType";
    }

    public CoreState()
    {
        _stateValues[StateKeys.VehicleId]           = float.NaN;
        _stateValues[StateKeys.EarthHeading]        = float.NaN;

        _stateValues[StateKeys.EarthPosition]       = new Vector3(float.NaN, float.NaN, float.NaN);
        _stateValues[StateKeys.LocalPosition]       = new Vector3(float.NaN, float.NaN, float.NaN);
        _stateValues[StateKeys.Attitude]            = new Vector3(float.NaN, float.NaN, float.NaN);
        _stateValues[StateKeys.GroundVelocity]      = new Vector3(float.NaN, float.NaN, float.NaN);
        _stateValues[StateKeys.GroundAcceleration]  = new Vector3(float.NaN, float.NaN, float.NaN);

        _stateValues[StateKeys.VehicleType]         = VehicleType.Unknown;
    }

    public T GetState<T>(string key)
    {
        if (_stateValues.TryGetValue(key, out object value))
        {
            return (T)value;
        }
        throw new KeyNotFoundException($"State key '{key}' not found");
    }

    public void SetState<T>(string key, T value)
    {
        if (_stateValues.ContainsKey(key))
        {
            _stateValues[key] = value;
        }
        else
        {
            throw new KeyNotFoundException($"State key '{key}' not found");
        }
    }

    // Convenience properties that use the dictionary internally
    public VehicleType VehicleType
    {
        get => GetState<VehicleType>(StateKeys.VehicleType);
        set => SetState(StateKeys.VehicleType, value);
    }

    public float VehicleId
    {
        get => GetState<float>(StateKeys.VehicleId);
        set => SetState(StateKeys.VehicleId, value);
    }

    public Vector3 EarthPosition
    {
        get => GetState<Vector3>(StateKeys.EarthPosition);
        set => SetState(StateKeys.EarthPosition, value);
    }

    public float EarthHeading
    {
        get => GetState<float>(StateKeys.EarthHeading);
        set => SetState(StateKeys.EarthHeading, value);
    }

    public Vector3 LocalPosition
    {
        get => GetState<Vector3>(StateKeys.LocalPosition);
        set => SetState(StateKeys.LocalPosition, value);
    }

    public Vector3 Attitude
    {
        get => GetState<Vector3>(StateKeys.Attitude);
        set => SetState(StateKeys.Attitude, value);
    }

    public Vector3 GroundVel
    {
        get => GetState<Vector3>(StateKeys.GroundVelocity);
        set => SetState(StateKeys.GroundVelocity, value);
    }

    public Vector3 GroundAcc
    {
        get => GetState<Vector3>(StateKeys.GroundAcceleration);
        set => SetState(StateKeys.GroundAcceleration, value);
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

        foreach (var kvp in fromState._stateValues)
        {
            if (!IsUninitialized(kvp.Value))
            {
                _stateValues[kvp.Key] = kvp.Value;
            }
        }
    }
}