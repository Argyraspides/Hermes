using Godot;
using System.Text.Json;

// Godot is unable to handle signals with the JsonElement data type directly
// so we are unable to pass in "JsonElement" in the MAVLinkJsonMessageReceivedEventHandler.
// We make a wrapper class here that inherits from GodotObject so that it can handle it
// properly.
public partial class JsonWrapper : GodotObject
{
    public JsonElement Data { get; set; }
}