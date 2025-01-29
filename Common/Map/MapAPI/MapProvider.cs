

using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

// TODO: A caching system should be implemented so that if a query string has already been
// used, the map tile associated with that query string is fetched locally. Dictionary mapping
// from URLs to user:// resource is a solution. And every time a map tile is fetched, it is cached
// as such. The cache should work offline and some file should be stored with mappings

// TODO: Add documentation on what this is
public partial class MapProvider : Node
{
    // Properties


}
