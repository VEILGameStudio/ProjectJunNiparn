# build_perspective_guide.py
# Draws grid boxes (like BlockoutKit_ThreeQuarter) through the game's real SideView camera, plus the
# one-point perspective guide that camera makes: vanishing point, horizon, rays, floor grid.
# Artists paint over the results.
#
# The camera numbers are copied from CameraPreset.For(SideView) in
# Assets/_Project/Scripts/Core/CameraPreset.cs. If those change, change CAMERA below and rerun.
#
# Needs: Pillow, numpy, psd-tools
#   pip install pillow numpy psd-tools
#   python3 tools/build_perspective_guide.py
#
# Writes, for each picture (scene, cube_field), into the folder above this script:
#   <name>_extended_1x.png    screen + margin so the vanishing point (above the screen) is visible
#   <name>_frame_1x.png/_2x   exactly the 16:9 game screen, guides on
#   <name>_frame_2x_clean.png the same without guide lines, only the grid boxes
#   scene_layered_2x.psd      extended canvas, one layer per piece and per guide + "paint here"
# and guide_numbers.json with every number the guide is drawn from.

import json
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

GUIDE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# SideView preset (CameraPreset.cs). The camera looks at the player's feet + TargetOffset from
# CameraDistance away, pitched down by Pitch. Yaw 0 = it looks straight along world +Z.
CAMERA = {
    "pitch_degrees": 10.0,
    "yaw_degrees": 0.0,
    "field_of_view_degrees": 11.2,
    "camera_distance": 20.0,
    "target_offset_y": 1.63,
}

SCREEN_WIDTH = 1920
SCREEN_HEIGHT = 1080
WIDE_WIDTH = 2520                 # 21:9 at the same height, the widest supported aspect
MARGIN_SIDES = 400
MARGIN_TOP = 560                  # room above the screen for the vanishing point
MARGIN_BOTTOM = 120
SUPERSAMPLE = 3                   # drawn at 3x of 1x, then shrunk, for smooth lines

GRID_MINOR_STEP = 0.25            # faint line every quarter unit (same as the ThreeQuarter kit)
GRID_MAJOR_STEP = 1.0             # strong line every unit
OUTLINE_WIDTH = 2.0
MAJOR_LINE_WIDTH = 1.0
MINOR_LINE_WIDTH = 0.6

# Light from upper left and a little from the front, so in SideView the top is lightest,
# the front face light, the left face middle and the right face darkest.
LIGHT_DIRECTION = np.array([-0.5, 1.0, -0.6])
LIGHT_DIRECTION = LIGHT_DIRECTION / np.linalg.norm(LIGHT_DIRECTION)

BACKGROUND = (56, 54, 62, 255)
OUTSIDE_TINT = (0, 0, 0, 70)
GREY = (205, 205, 210)
GREEN = (140, 185, 110)
FAR_GREEN = (120, 160, 100)
RED = (200, 95, 80)
WOOD = (175, 125, 80)
OUTLINE_COLOUR = (38, 38, 46, 255)

FRAME_COLOUR = (245, 245, 245, 255)
WIDE_FRAME_COLOUR = (245, 245, 245, 150)
HORIZON_COLOUR = (255, 90, 90, 255)
RAY_COLOUR = (255, 110, 110, 70)
DEPTH_COLOUR = (90, 220, 170, 220)
VERTICAL_COLOUR = (190, 140, 255, 140)
LABEL_COLOUR = (245, 245, 245, 255)


# ---------------------------------------------------------------- camera

class SideViewCamera:
    """Pinhole camera with the SideView preset. The player's feet are at the world origin."""

    def __init__(self):
        self.pitch = math.radians(CAMERA["pitch_degrees"])
        self.forward = np.array([0.0, -math.sin(self.pitch), math.cos(self.pitch)])
        self.up = np.array([0.0, math.cos(self.pitch), math.sin(self.pitch)])
        self.right = np.array([1.0, 0.0, 0.0])
        target = np.array([0.0, CAMERA["target_offset_y"], 0.0])
        self.position = target - self.forward * CAMERA["camera_distance"]
        half_fov = math.radians(CAMERA["field_of_view_degrees"]) / 2.0
        self.focal_pixels = (SCREEN_HEIGHT / 2.0) / math.tan(half_fov)

    def depth(self, point):
        return (np.asarray(point, float) - self.position) @ self.forward

    def to_screen(self, point):
        """1x screen pixels measured from the centre of the 16:9 screen, y down."""
        offset = np.asarray(point, float) - self.position
        depth = offset @ self.forward
        return self.focal_pixels * (offset @ self.right) / depth, -self.focal_pixels * (offset @ self.up) / depth

    def vanishing_point(self, direction):
        """Where all lines with this world direction meet on screen."""
        direction = np.asarray(direction, float)
        depth = direction @ self.forward
        return self.focal_pixels * (direction @ self.right) / depth, -self.focal_pixels * (direction @ self.up) / depth

    def floor_z_at_depth(self, depth):
        """World z of the floor point straight ahead that is `depth` from the camera along its view."""
        return (depth - self.position[1] * math.sin(self.pitch)) / math.cos(self.pitch) + self.position[2]


CAM = SideViewCamera()
VANISHING_POINT = CAM.vanishing_point([0, 0, 1])
VERTICAL_VANISHING_POINT = CAM.vanishing_point([0, -1, 0])
HORIZON_Y = VANISHING_POINT[1]
_bottom_ray = CAM.pitch + math.radians(CAMERA["field_of_view_degrees"]) / 2
FLOOR_Z_AT_SCREEN_BOTTOM = CAM.position[1] / math.tan(_bottom_ray) + CAM.position[2]
_top_ray = CAM.pitch - math.radians(CAMERA["field_of_view_degrees"]) / 2
FLOOR_Z_AT_SCREEN_TOP = CAM.position[1] / math.tan(_top_ray) + CAM.position[2]
DEPTH_ROWS = [20, 30, 40, 60, 100]


# ---------------------------------------------------------------- canvas

class Canvas:
    """One transparent layer of the extended canvas, drawn SUPERSAMPLE times bigger than 1x."""

    width = SCREEN_WIDTH + 2 * MARGIN_SIDES
    height = SCREEN_HEIGHT + MARGIN_TOP + MARGIN_BOTTOM
    centre_x = MARGIN_SIDES + SCREEN_WIDTH / 2.0
    centre_y = MARGIN_TOP + SCREEN_HEIGHT / 2.0
    left, right = -centre_x, width - centre_x
    top, bottom = -centre_y, height - centre_y

    def __init__(self, fill=(0, 0, 0, 0)):
        self.image = Image.new("RGBA", (self.width * SUPERSAMPLE, self.height * SUPERSAMPLE), fill)
        self.draw = ImageDraw.Draw(self.image)

    @classmethod
    def place(cls, xy):
        return ((cls.centre_x + xy[0]) * SUPERSAMPLE, (cls.centre_y + xy[1]) * SUPERSAMPLE)

    def line(self, a, b, colour, width=1.0):
        self.draw.line([self.place(a), self.place(b)], fill=colour, width=max(1, round(width * SUPERSAMPLE)))

    def dashed(self, a, b, colour, width=1.0, dash=8.0, gap=6.0):
        length = math.dist(a, b)
        travelled = 0.0
        while travelled < length:
            start, end = travelled / length, min(travelled + dash, length) / length
            self.line((a[0] + (b[0] - a[0]) * start, a[1] + (b[1] - a[1]) * start),
                      (a[0] + (b[0] - a[0]) * end, a[1] + (b[1] - a[1]) * end), colour, width)
            travelled += dash + gap

    def polygon(self, points, fill):
        self.draw.polygon([self.place(p) for p in points], fill=fill)

    def dot(self, centre, radius, colour):
        x, y = self.place(centre)
        r = radius * SUPERSAMPLE
        self.draw.ellipse([x - r, y - r, x + r, y + r], fill=colour)

    def ring(self, centre, radius, colour, width=1.0):
        x, y = self.place(centre)
        r = radius * SUPERSAMPLE
        self.draw.ellipse([x - r, y - r, x + r, y + r], outline=colour, width=round(width * SUPERSAMPLE))

    def text(self, position, words, size=18, colour=LABEL_COLOUR, anchor="la"):
        font = load_font(size * SUPERSAMPLE)
        left, top, right, bottom = self.draw.textbbox(self.place(position), words, font=font, anchor=anchor)
        pad = 4 * SUPERSAMPLE
        self.draw.rectangle([left - pad, top - pad, right + pad, bottom + pad], fill=(20, 20, 26, 150))
        self.draw.text(self.place(position), words, font=font, fill=colour, anchor=anchor)

    def finished(self, scale):
        return self.image.resize((self.width * scale, self.height * scale), Image.LANCZOS)


def load_font(size):
    for path in ("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            pass
    return ImageFont.load_default(size=size)


# ---------------------------------------------------------------- grid-box shapes

class Face:
    """One flat convex polygon in world units (Unity axes, y up), with an outward normal."""

    def __init__(self, points, colour, centre, minor_grid=True):
        self.points = np.array(points, dtype=float)
        self.colour = colour
        self.minor_grid = minor_grid
        normal = np.cross(self.points[1] - self.points[0], self.points[2] - self.points[0])
        normal = normal / np.linalg.norm(normal)
        if np.dot(normal, self.points.mean(axis=0) - centre) < 0:
            normal = -normal
        self.normal = normal


class Piece:
    """A named group of faces that becomes one PSD layer."""

    def __init__(self, name, faces):
        self.name = name
        self.faces = faces

    def depth(self):
        return max(CAM.depth(face.points.mean(axis=0)) for face in self.faces)


def box_faces(x0, x1, y0, y1, z0, z1, colour=GREY, minor_grid=True):
    centre = np.array([(x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2])
    c = [np.array([(x0, x1)[i & 1], (y0, y1)[(i >> 1) & 1], (z0, z1)[(i >> 2) & 1]]) for i in range(8)]
    quads = [(2, 3, 7, 6), (0, 1, 5, 4), (0, 1, 3, 2), (4, 5, 7, 6), (0, 2, 6, 4), (1, 3, 7, 5)]
    return [Face([c[i] for i in quad], colour, centre, minor_grid) for quad in quads]


def box(name, x, z, width, height, depth, base=0.0, colour=GREY):
    """A box by footprint centre (x, z), size, and the height its bottom sits at."""
    return Piece(name, box_faces(x - width / 2, x + width / 2, base, base + height, z - depth / 2, z + depth / 2, colour))


def roof(name, x, z, width, depth, base, height, colour=RED):
    """A gable roof with its ridge along X, sitting on top of a block."""
    x0, x1, z0, z1 = x - width / 2, x + width / 2, z - depth / 2, z + depth / 2
    ridge_y = base + height
    centre = np.array([x, base + height / 3, z])
    faces = [
        Face([(x0, base, z0), (x1, base, z0), (x1, ridge_y, z), (x0, ridge_y, z)], colour, centre),
        Face([(x0, base, z1), (x1, base, z1), (x1, ridge_y, z), (x0, ridge_y, z)], colour, centre),
        Face([(x0, base, z0), (x0, base, z1), (x0, ridge_y, z)], colour, centre),
        Face([(x1, base, z0), (x1, base, z1), (x1, ridge_y, z)], colour, centre),
        Face([(x0, base, z0), (x1, base, z0), (x1, base, z1), (x0, base, z1)], colour, centre),
    ]
    return Piece(name, faces)


def stairs(name, x0, x1, z_front, steps, step_depth, step_height, colour=GREY):
    """Steps rising away from the camera (toward +z), one box per step."""
    faces = []
    for i in range(steps):
        faces += box_faces(x0, x1, 0, step_height * (i + 1), z_front + i * step_depth, z_front + (i + 1) * step_depth, colour)
    return Piece(name, faces)


def floor(name, x0, x1, z0, z1, colour, minor_grid, thickness=0.3):
    return Piece(name, box_faces(x0, x1, -thickness, 0.0, z0, z1, colour, minor_grid))


def face_shade(normal):
    return 0.42 + 0.58 * max(0.0, float(np.dot(normal, LIGHT_DIRECTION)))


def grid_segments(face):
    """Where the world planes x=k, y=k, z=k cross this face, as (start, end, is_major)."""
    segments = []
    points = face.points
    step = GRID_MINOR_STEP if face.minor_grid else GRID_MAJOR_STEP
    for axis in range(3):
        if abs(face.normal[axis]) > 0.999:
            continue
        low, high = points[:, axis].min(), points[:, axis].max()
        k = math.floor(low / step) * step
        while k <= high + 1e-9:
            if low + 1e-6 < k < high - 1e-6:
                hits = []
                for i in range(len(points)):
                    a, b = points[i], points[(i + 1) % len(points)]
                    da, db = a[axis] - k, b[axis] - k
                    if (da < 0) != (db < 0):
                        hits.append(a + (b - a) * (da / (da - db)))
                if len(hits) >= 2:
                    is_major = abs(k / GRID_MAJOR_STEP - round(k / GRID_MAJOR_STEP)) < 1e-6
                    segments.append((hits[0], hits[1], is_major))
            k += step
    return segments


def faces_facing_camera(faces):
    visible = [f for f in faces if np.dot(f.normal, CAM.position - f.points.mean(axis=0)) > 1e-6]
    visible.sort(key=lambda f: -CAM.depth(f.points.mean(axis=0)))
    return visible


def draw_piece(canvas, piece):
    """Shaded faces, grid lines on every face, then outlines. Painter order is only for this picture."""
    for face in faces_facing_camera(piece.faces):
        shade = face_shade(face.normal)
        fill = tuple(int(channel * shade) for channel in face.colour) + (255,)
        screen = [CAM.to_screen(p) for p in face.points]
        canvas.polygon(screen, fill)
        dark = tuple(int(channel * shade * 0.62) for channel in face.colour)
        for start, end, is_major in grid_segments(face):
            if is_major:
                canvas.line(CAM.to_screen(start), CAM.to_screen(end), dark + (230,), MAJOR_LINE_WIDTH)
            else:
                canvas.line(CAM.to_screen(start), CAM.to_screen(end), dark + (120,), MINOR_LINE_WIDTH)
        for a, b in zip(screen, screen[1:] + screen[:1]):
            canvas.line(a, b, OUTLINE_COLOUR, OUTLINE_WIDTH)


# ---------------------------------------------------------------- the two pictures

def ground_pieces():
    return [
        floor("floor far (z 4 to 400)", -400, 400, 4, 400, FAR_GREEN, minor_grid=False),
        floor("floor near / walk strip (z -3 to 4)", -40, 40, -3, 4, GREEN, minor_grid=True),
    ]


def scene_pieces():
    """A small SideView corridor sized for the real screen: about 7 units wide at the player."""
    return ground_pieces() + [
        box("far tower right", 18, 90, 6, 40, 6),
        box("far tower left", -14, 60, 4, 20, 4),
        box("far block", -6, 18, 3, 2, 3),
        box("far house", 6, 22, 3, 2.5, 3, colour=WOOD),
        roof("far house roof", 6, 22, 3.4, 3.4, 2.5, 1.5),
        box("pillar back left", -2, 9, 0.6, 3, 0.6),
        box("pillar back right", 1, 11, 0.6, 3, 0.6),
        box("terrace", -3.25, 5, 3.5, 1, 2),
        stairs("stairs up to terrace", -1.5, -0.5, 2.0, 4, 0.5, 0.25),
        box("house", 3.5, 6.25, 3, 1.8, 2.5, colour=WOOD),
        roof("house roof", 3.5, 6.25, 3.4, 2.9, 1.8, 1.0),
        box("crate left", -2.4, 0.3, 1, 1, 1),
        box("crate stack low", 2.2, 0.8, 1, 1, 1),
        box("crate stack top", 2.2, 0.8, 1, 1, 1, base=1),
        box("box small", 3.1, -0.2, 0.75, 0.5, 0.75),
        box("floating cube left", -1.2, 1.0, 0.5, 0.5, 0.5, base=2.6),
        box("floating cube right", 1.0, 2.0, 0.5, 0.5, 0.5, base=2.8),
    ]


def cube_field_pieces():
    """1x1x1 cubes all over the screen, like a perspective practice sheet."""
    pieces = ground_pieces()
    for z in (0, 3, 8, 16, 30):
        reach = int((CAM.depth([0, 0, z]) * math.tan(math.radians(CAMERA["field_of_view_degrees"]) / 2) * WIDE_WIDTH / SCREEN_HEIGHT))
        spacing = {0: 2, 3: 2, 8: 3, 16: 4, 30: 6}[z]
        for x in range(-reach - spacing, reach + spacing + 1, spacing):
            pieces.append(box(f"cube x{x} z{z}", x, z + 0.5, 1, 1, 1))
    for x in (-3, 0, 3):
        pieces.append(box(f"floating cube x{x} y2.4", x, 1.5, 0.8, 0.8, 0.8, base=2.4))
    return pieces


# ---------------------------------------------------------------- guide layers

def layer_outside_screen():
    canvas = Canvas()
    w, h = SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2
    canvas.polygon([(Canvas.left, Canvas.top), (Canvas.right, Canvas.top), (Canvas.right, -h), (Canvas.left, -h)], OUTSIDE_TINT)
    canvas.polygon([(Canvas.left, h), (Canvas.right, h), (Canvas.right, Canvas.bottom), (Canvas.left, Canvas.bottom)], OUTSIDE_TINT)
    canvas.polygon([(Canvas.left, -h), (-w, -h), (-w, h), (Canvas.left, h)], OUTSIDE_TINT)
    canvas.polygon([(w, -h), (Canvas.right, -h), (Canvas.right, h), (w, h)], OUTSIDE_TINT)
    return canvas


def layer_rays():
    canvas = Canvas()
    for step in range(0, 360, 3):
        angle = math.radians(step + 1.5)
        end = (VANISHING_POINT[0] + 6000 * math.cos(angle), VANISHING_POINT[1] + 6000 * math.sin(angle))
        canvas.line(VANISHING_POINT, end, RAY_COLOUR, 1)
    return canvas


def layer_horizon_and_vp():
    canvas = Canvas()
    vp_y = SCREEN_HEIGHT / 2 + VANISHING_POINT[1]
    canvas.line((Canvas.left, HORIZON_Y), (Canvas.right, HORIZON_Y), HORIZON_COLOUR, 2)
    canvas.dot(VANISHING_POINT, 7, HORIZON_COLOUR)
    canvas.ring(VANISHING_POINT, 16, HORIZON_COLOUR, 2)
    canvas.text((VANISHING_POINT[0] + 24, HORIZON_Y + 10),
                f"VP = (960, {vp_y:.0f}) in 1080p screen pixels: {-vp_y:.0f} px ABOVE the top edge", 20, HORIZON_COLOUR)
    canvas.text((Canvas.left + 16, HORIZON_Y - 26),
                f"HORIZON = eye level. Camera is {CAM.position[1]:.2f} units above the floor, pitched 10 down, so the horizon is above the screen",
                16, HORIZON_COLOUR)
    top = -SCREEN_HEIGHT / 2
    canvas.line((0, top + 40), (0, top + 6), HORIZON_COLOUR, 2)
    canvas.polygon([(0, top + 2), (-8, top + 16), (8, top + 16)], HORIZON_COLOUR)
    canvas.text((12, top + 22), f"VP is {-vp_y:.0f} px above this edge, straight up from the centre", 16, HORIZON_COLOUR)
    return canvas


def layer_depth_rows():
    """Floor rows at the distances CLAUDE.md uses for parallax layers."""
    canvas = Canvas()
    for depth in DEPTH_ROWS:
        z = CAM.floor_z_at_depth(depth)
        y = CAM.to_screen([0, 0, z])[1]
        canvas.dashed((Canvas.left, y), (Canvas.right, y), DEPTH_COLOUR, 1, 10, 6)
        ppu = SCREEN_HEIGHT / (2 * depth * math.tan(math.radians(CAMERA["field_of_view_degrees"]) / 2))
        canvas.text((-SCREEN_WIDTH / 2 + 10, y - 19),
                    f"D {depth}: floor z {z:.1f}, moves {20 / depth:.2f}x, PPU {ppu:.0f}", 14, DEPTH_COLOUR)
    return canvas


def layer_verticals():
    """True verticals: pitch 10 makes them lean a little toward a point far below the screen."""
    canvas = Canvas()
    v3 = VERTICAL_VANISHING_POINT
    for x in range(-1200, 1201, 200):
        start = (x, Canvas.top)
        t = (SCREEN_HEIGHT / 2 + 60 - start[1]) / (v3[1] - start[1])
        canvas.dashed(start, (start[0] + (v3[0] - start[0]) * t, start[1] + (v3[1] - start[1]) * t), VERTICAL_COLOUR, 1, 12, 8)
    lean = math.degrees(math.atan((SCREEN_WIDTH / 2) / (v3[1] + SCREEN_HEIGHT / 2)))
    canvas.text((-SCREEN_WIDTH / 2 + 10, SCREEN_HEIGHT / 2 + 30),
                f"purple = true verticals: lean {lean:.1f} deg at the screen edge, toward a point "
                f"{SCREEN_HEIGHT / 2 + v3[1]:,.0f} px down. Optional; drawing them straight is close enough.", 14, VERTICAL_COLOUR)
    return canvas


def layer_player_marker():
    canvas = Canvas()
    feet = CAM.to_screen([0, 0, 0])
    top, half = feet[1] - 430, 55
    for a, b in (((-half, feet[1]), (half, feet[1])), ((-half, top), (half, top)),
                 ((-half, feet[1]), (-half, top)), ((half, feet[1]), (half, top))):
        canvas.dashed((feet[0] + a[0], a[1]), (feet[0] + b[0], b[1]), LABEL_COLOUR, 1.5, 6, 4)
    canvas.text((feet[0], top - 8), "player ~430 px tall", 15, LABEL_COLOUR, "mb")
    canvas.text((feet[0], feet[1] + 6), "feet, z = 0", 13, LABEL_COLOUR, "ma")
    return canvas


def layer_frames():
    canvas = Canvas()
    w, h, ww = SCREEN_WIDTH / 2, SCREEN_HEIGHT / 2, WIDE_WIDTH / 2
    for a, b in (((-w, -h), (w, -h)), ((w, -h), (w, h)), ((w, h), (-w, h)), ((-w, h), (-w, -h))):
        canvas.line(a, b, FRAME_COLOUR, 2)
    for a, b in (((-ww, -h), (ww, -h)), ((ww, -h), (ww, h)), ((ww, h), (-ww, h)), ((-ww, h), (-ww, -h))):
        canvas.dashed(a, b, WIDE_FRAME_COLOUR, 1.5, 14, 8)
    canvas.text((-w + 8, h - 28), "16:9 game screen 1920x1080", 18, FRAME_COLOUR)
    canvas.text((-ww + 8, h - 26), "21:9", 16, WIDE_FRAME_COLOUR)
    canvas.line((-12, 0), (12, 0), FRAME_COLOUR, 1)
    canvas.line((0, -12), (0, 12), FRAME_COLOUR, 1)
    return canvas


def layer_title():
    canvas = Canvas()
    canvas.text((Canvas.left + 16, Canvas.top + 14),
                "SideView one-point perspective  |  perspective camera, FOV 11.2, pitch 10, yaw 0, distance 20, target +1.63", 20)
    canvas.text((Canvas.left + 16, Canvas.top + 42),
                "Depth lines (world Z) -> VP.   Across lines (world X) stay horizontal.   "
                "Grid: strong line every 1 unit, faint every 0.25.", 16)
    return canvas


GUIDE_LAYERS = [
    ("guide: outside the screen (tint)", layer_outside_screen),
    ("guide: perspective rays to VP", layer_rays),
    ("guide: depth rows / parallax distance", layer_depth_rows),
    ("guide: true verticals (optional)", layer_verticals),
    ("guide: player height", layer_player_marker),
    ("guide: horizon + vanishing point", layer_horizon_and_vp),
    ("guide: screen frames 16:9 / 21:9", layer_frames),
    ("guide: title", layer_title),
]
# Every guide is drawn on top of the boxes: the floor is opaque, so anything under it would vanish.
GUIDES_UNDER_BOXES = 0


# ---------------------------------------------------------------- output

class Layer:
    """A finished 2x layer kept as a cropped image plus where it sits, to save memory."""

    def __init__(self, name, canvas):
        full = canvas.finished(2)
        self.name = name
        self.box = full.getbbox() or (0, 0, 1, 1)
        self.image = full.crop(self.box)

    def paste_onto(self, base):
        overlay = Image.new("RGBA", base.size, (0, 0, 0, 0))
        overlay.paste(self.image, self.box[:2])
        return Image.alpha_composite(base, overlay)


def frame_crop(image, scale):
    left, top = MARGIN_SIDES * scale, MARGIN_TOP * scale
    return image.crop((left, top, left + SCREEN_WIDTH * scale, top + SCREEN_HEIGHT * scale))


def build(name, pieces, with_psd):
    print("building", name)
    size_2x = (Canvas.width * 2, Canvas.height * 2)
    background = Image.new("RGBA", size_2x, BACKGROUND)

    piece_layers = []
    for piece in sorted(pieces, key=lambda p: -p.depth()):
        canvas = Canvas()
        draw_piece(canvas, piece)
        piece_layers.append(Layer(piece.name, canvas))
    guide_layers = [Layer(layer_name, builder()) for layer_name, builder in GUIDE_LAYERS]

    clean = background
    for layer in piece_layers:
        clean = layer.paste_onto(clean)
    guided = background
    for layer in guide_layers[:GUIDES_UNDER_BOXES]:
        guided = layer.paste_onto(guided)
    for layer in piece_layers:
        guided = layer.paste_onto(guided)
    for layer in guide_layers[GUIDES_UNDER_BOXES:]:
        guided = layer.paste_onto(guided)

    guided_1x = guided.resize((Canvas.width, Canvas.height), Image.LANCZOS)
    guided_1x.convert("RGB").save(os.path.join(GUIDE_DIR, f"{name}_extended_1x.png"), optimize=True)
    frame_crop(guided_1x, 1).convert("RGB").save(os.path.join(GUIDE_DIR, f"{name}_frame_1x.png"), optimize=True)
    frame_crop(guided, 2).convert("RGB").save(os.path.join(GUIDE_DIR, f"{name}_frame_2x.png"), optimize=True)
    frame_crop(clean, 2).convert("RGB").save(os.path.join(GUIDE_DIR, f"{name}_frame_2x_clean.png"), optimize=True)

    if with_psd:
        ordered = (guide_layers[:GUIDES_UNDER_BOXES], piece_layers, guide_layers[GUIDES_UNDER_BOXES:])
        save_psd(name, background, ordered, guided)


def save_psd(name, background, ordered, flat):
    from psd_tools import PSDImage
    from psd_tools.api.layers import Group, PixelLayer
    from psd_tools.constants import Compression
    from psd_tools.psd.image_data import ImageData

    psd = PSDImage.new("RGBA", flat.size, color=(0, 0, 0, 0))
    psd.append(PixelLayer.frompil(background, psd, name="background", compression=Compression.RLE))
    under, pieces, over = ordered
    for group_name, layers in (("guides under the boxes", under), ("grid boxes (one layer per piece)", pieces),
                               ("guides on top", over)):
        if not layers:
            continue
        group = Group.new(psd, name=group_name)
        for layer in layers:
            group.append(PixelLayer.frompil(layer.image, psd, name=layer.name, top=layer.box[1], left=layer.box[0],
                                            compression=Compression.RLE))
    psd.append(PixelLayer.frompil(Image.new("RGBA", flat.size, (0, 0, 0, 0)), psd, name="paint here",
                                  compression=Compression.RLE))
    merged = ImageData(compression=Compression.RLE)
    merged.set_data([channel.tobytes() for channel in flat.split()[:psd._record.header.channels]], psd._record.header)
    psd._record.image_data = merged
    psd.save(os.path.join(GUIDE_DIR, f"{name}_layered_2x.psd"))


def numbers():
    vp_x, vp_y = VANISHING_POINT
    v3_y = VERTICAL_VANISHING_POINT[1]
    return {
        "camera": CAMERA,
        "camera_position_world": [round(v, 3) for v in CAM.position],
        "camera_height_above_floor": round(CAM.position[1], 3),
        "focal_length_pixels_at_1080": round(CAM.focal_pixels, 1),
        "vanishing_point_1080p": [round(SCREEN_WIDTH / 2 + vp_x, 1), round(SCREEN_HEIGHT / 2 + vp_y, 1)],
        "vanishing_point_2160p": [round((SCREEN_WIDTH / 2 + vp_x) * 2, 1), round((SCREEN_HEIGHT / 2 + vp_y) * 2, 1)],
        "vanishing_point_note": "pixels from the top-left of the screen; negative y = above the top edge",
        "horizon_y_1080p": round(SCREEN_HEIGHT / 2 + vp_y, 1),
        "vertical_vanishing_point_y_1080p": round(SCREEN_HEIGHT / 2 + v3_y, 1),
        "vertical_lean_at_screen_edge_degrees": round(math.degrees(math.atan((SCREEN_WIDTH / 2) / (v3_y + SCREEN_HEIGHT / 2))), 2),
        "player_feet_1080p": [round(SCREEN_WIDTH / 2 + CAM.to_screen([0, 0, 0])[0], 1),
                              round(SCREEN_HEIGHT / 2 + CAM.to_screen([0, 0, 0])[1], 1)],
        "floor_z_at_screen_bottom": round(FLOOR_Z_AT_SCREEN_BOTTOM, 2),
        "floor_z_at_screen_top": round(FLOOR_Z_AT_SCREEN_TOP, 2),
        "extended_canvas_1x": {
            "size": [Canvas.width, Canvas.height],
            "screen_16x9_top_left": [MARGIN_SIDES, MARGIN_TOP],
            "screen_21x9_top_left": [MARGIN_SIDES - (WIDE_WIDTH - SCREEN_WIDTH) // 2, MARGIN_TOP],
            "vanishing_point": [round(Canvas.centre_x + vp_x, 1), round(Canvas.centre_y + vp_y, 1)],
        },
        "depth_rows": [
            {"depth_from_camera": d, "floor_z": round(CAM.floor_z_at_depth(d), 2),
             "screen_y_1080p": round(SCREEN_HEIGHT / 2 + CAM.to_screen([0, 0, CAM.floor_z_at_depth(d)])[1], 1),
             "parallax_speed": round(20 / d, 3),
             "ppu_1080p": round(SCREEN_HEIGHT / (2 * d * math.tan(math.radians(CAMERA["field_of_view_degrees"]) / 2)), 1)}
            for d in DEPTH_ROWS
        ],
    }


def main():
    build("scene", scene_pieces(), with_psd=True)
    build("cube_field", cube_field_pieces(), with_psd=False)   # PNGs only, keeps the repo small
    with open(os.path.join(GUIDE_DIR, "guide_numbers.json"), "w", encoding="utf-8") as file:
        json.dump(numbers(), file, indent=2)
        file.write("\n")
    print(json.dumps(numbers(), indent=2))


if __name__ == "__main__":
    main()
