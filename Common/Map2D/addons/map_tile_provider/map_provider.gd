@tool
class_name MapProvider
extends Node


enum MapType {
	 SATELLITE,
	 STREET,
	 HYBRID,
}


@export var language_code := "en"
@export var map_style := MapType.SATELLITE

# See "_create_tile_parameters" for a background on this function
func _create_tile_parameters_for_indices(x: int, y: int, zoom: int) -> Dictionary:
	return {
		"server": _select_server(x, y),
		"quad": _construct_quad_key(x, y, zoom),
		"x": x,
		"y": y,
		"zoom": zoom,
		"lang": language_code,
		"map_style": map_style,
		"format": MapTile.Format.BMP
	}

# Here we convert latitude, longitude, and a specific zoom level into a quadrant 
# key, a tile coordinate, and more, for a maps API request.
# E.g., for the bing maps API, a quadrant key specifies the specific quadrant
# of the Earth to retrieve (as a square map tile).
func _create_tile_parameters(lat: float, lon: float, zoom: int) -> Dictionary:
	return _create_tile_parameters_for_indices(
			longitude_to_tile(lon, zoom), latitude_to_tile(lat, zoom), zoom
	)

func get_tile_url(lat: float, lon: float, zoom: int) -> String:
	return _construct_url(_create_tile_parameters(lat, lon, zoom))


func get_tile_cache_path(lat: float, lon: float, zoom: int) -> String:
	var args = _create_tile_parameters(lat, lon, zoom)

	return _url_to_cache(_construct_url(args), args)


func get_tile_locations(lat: float, lon: float, zoom: int) -> Array:
	var args = _create_tile_parameters(lat, lon, zoom)	
	var url = _construct_url(args)

	return [ url, _url_to_cache(url, args) ]

# See: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
# Here we are converting a longitude to a specific tile 'y' coordinate based on a
# particular zoom level. We know that at each zoom level, the number of tiles representing
# the Earth's surface goes up x4, starting with 4 tiles (2x2) at a zoom level of 1
# (e.g., at zoom level 2, the Earth's surface is represented as 16 tiles
# as a 4x4 grid) From this, we can easily find which *row* the longitude belongs in
static func longitude_to_tile(lon: float, zoom: int) -> int:
	return floori((lon + 180.0) / 360.0 * (1 << zoom))

# See: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
# Here we are converting a latitude to a specific tile 'x' coordinate based on a
# particular zoom level. We know that at each zoom level, the number of tiles representing
# the Earth's surface goes up x4, starting with 4 tiles (2x2) at a zoom level of 1
# (e.g., at zoom level 2, the Earth's surface is represented as 16 tiles
# as a 4x4 grid) From this, we can easily find which *column* the latitude belongs in
static func latitude_to_tile(lat: float, zoom: int) -> int:
	return floori(
			(1.0 - log(tan(deg_to_rad(lat)) + 1.0 / cos(deg_to_rad(lat))) / PI) /
			2.0 * (1 << zoom)
	)


static func tile_to_longitude(x: int, zoom: int) -> float:
	return x * 360.0 / (1 << zoom) - 180.0


static func tile_to_latitude(y: int, zoom: int) -> float:
	return rad_to_deg(atan(sinh(PI * (1 - 2.0 * y / (1 << zoom)))))


static func tile_to_coordinates(x: int, y: int, zoom: int) -> Vector2:
	return Vector2(tile_to_latitude(y, zoom), tile_to_longitude(x, zoom))


static func tile_to_bounds(x: int, y: int, zoom: int) -> Rect2:
	var lat = tile_to_latitude(y, zoom)
	var lon = tile_to_longitude(x, zoom)

	return Rect2(
			lon, lat,
			tile_to_longitude(x + 1, zoom) - lon, tile_to_latitude(y - 1, zoom) - lat
	)

# Any classes inheriting from MapProvider should override this method to construct their 
# own API query strings
func _construct_url(args: Dictionary) -> String:
	return "debug://server{server}/q{quad}/x{x}/y{y}?zoom={zoom}&lang={lang}".format(args)

# See: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
# on how this works. What's happening here is we are converting tile coordinates
# at a particular zoom level into a quadrant key which uniquely identifies each
# tile as a single number rather than an (x,y) coordinate. The Earths's surface
# is represented by 4 tiles (2x2) at a zoom level of 1, which goes up x4 every 
# zoom level (e.g., at zoom level 2, the Earth's surface is represented as 16 tiles
# as a 4x4 grid). This means we know in advance how many tiles there are, and can 
# easily convert a latitude/longitude we wish to see into tile coordinates (x,y).
# The only thing to do next then is to convert these tile coordinates to a quadrant key.
# This quadrant key can be used to fetch the tile image of Earth's surface from an API
func _construct_quad_key(x: int, y: int, zoom: int) -> String:
	var str: PackedByteArray = []
	var i: int = zoom

	while i > 0:
		i -= 1
		var digit: int = 0x30
		var mask: int = 1 << i
		if (x & mask) != 0:
			digit += 1
		if (y & mask) != 0:
			digit += 2
		str.append(digit)

	return str.get_string_from_ascii()

# A simple load balancing strategy to ensure we don't overload any one map server.
# Usually map providers have multiple endpoints you can query. Here we are just
# taking in a tile index (see the longitude_to_tile function comment on what a tile index is) 
# and then map that to a specific server instance that we can include in our query string
# Honestly we could also just return a random number as well, and that'd work, but this is slightly better.
# We assume four available servers (hence % 4)
func _select_server(x: int, y: int) -> int:
	return (x + 2 * y) % 4


func _url_to_cache(url: String, args: Dictionary) -> String:
	return "user://tiles/debug/%d/%s.tile" % [ args["zoom"], url.md5_text() ]
