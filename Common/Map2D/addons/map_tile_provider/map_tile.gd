@tool
class_name MapTile
extends Resource

enum Format {
	 BMP,
	 JPG,
	 PNG,
	 TGA,
	 WEBP,
 }

# The bounds specify longitude of the tile (x) (from the top right corner?),
# latitude of the tile (y), and then the width and height
# of the map tile of unit longitude (w) and latitude (h)
@export var bounds: Rect2

# The coords specify the specific tile coordinate in 2D space (x, y) at a specific zoom level (z)
# When obtaining an API response from a map provider, the entire world map is split up into a grid
# of tiles. Each zoom level splits the Earth up further into more tiles, increasing by 4 everytime.
# At a zoom level of 1, the Earth is represented as a 2x2 grid of tiles (4 total). At a zoom level 2,
# this goes up to a 4x4 grid of tiles (16 total), then 8x8 (64 total) and so on.
# See: https://learn.microsoft.com/en-us/azure/azure-maps/zoom-levels-and-tile-grid?tabs=csharp
# And/or: https://www.microimages.com/documentation/TechGuides/78BingStructure.pdf
@export var coords: Vector3i

# The raw image data of the tile
@export var image: PackedByteArray
@export var format: Format
@export var path: String


func _init(b: Rect2 = Rect2(), c: Vector3i = Vector3i.ZERO, i: PackedByteArray = [], f: Format = Format.JPG):
	bounds = b
	coords = c
	image = i
	format = f
	path = ""


func pack(file: FileAccess):
	file.store_var(bounds, true)
	file.store_var(coords, true)
	file.store_32(format)
	file.store_32(len(image))
	file.store_buffer(image)


func unpack(file: FileAccess) -> Error:
	# record the path it was loaded from
	path = file.get_path()

	if file.eof_reached():
		return ERR_FILE_EOF

	bounds = file.get_var(true)
	if file.eof_reached():
		return ERR_FILE_EOF

	coords = file.get_var(true)
	if file.eof_reached():
		return ERR_FILE_EOF

	format = file.get_32()
	if file.eof_reached():
		return ERR_FILE_EOF

	var expected = file.get_32()
	if file.eof_reached():
		return ERR_FILE_EOF

	image = file.get_buffer(expected)
	if len(image) != expected:
		return ERR_FILE_EOF

	return OK


func to_image(img: Image) -> Error:
	match format:
		Format.BMP:
			return img.load_bmp_from_buffer(image)
		Format.JPG:
			return img.load_jpg_from_buffer(image)
		Format.PNG:
			return img.load_png_from_buffer(image)
		Format.TGA:
			return img.load_tga_from_buffer(image)
		Format.WEBP:
			return img.load_webp_from_buffer(image)
		_:
			return ERR_INVALID_DATA


func degrees_per_pixel(size: Vector2i = Vector2i(256, 256)) -> Vector2:
	var degrees = bounds.size
	return Vector2(degrees.x / size.x, degrees.y / size.y)


func pixels_per_degree(size: Vector2i = Vector2i(256, 256)) -> Vector2:
	var degrees = bounds.size
	return Vector2(size.x / degrees.x, size.y / degrees.y)
