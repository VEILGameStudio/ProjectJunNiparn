# build_blockout_kit.py
# Draws a grey-box (blockout) kit of simple shapes - boxes, ramps, wedges, stairs,
# cylinders, cones, spheres - exactly as the game's ThreeQuarter camera sees them
# (orthographic, pitch 30, yaw 45, which is 2:1 isometric), with a world grid printed
# on every face. Artists paint over these as a base.
#
# Run:   python3 build_blockout_kit.py            (writes into ../ next to this folder)
# Needs: Pillow, numpy, psd-tools
#
# Output (relative to the kit folder):
#   shapes/1x/grey/*.png, shapes/1x/lines/*.png   one PNG per shape at in-game size (108 px per unit)
#   shapes/2x/...                                  the same at double size, for painting
#   shapes/pivots.json                             where each shape touches the ground, per PNG
#   floor_grid_1x.png, floor_grid_2x.png           a 16 x 16 unit floor grid to paint on
#   kit_sheet.png                                  every shape with its name, for browsing
#   example_scene_1x.png / _2x.png / .psd          a small diorama built from the kit, one layer per piece

import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

KIT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))

# ThreeQuarter preset (CLAUDE.md §7). Orthographic Size 5 at 1080p = 108 px per world unit.
PITCH_DEG = 30.0
YAW_DEG = 45.0
PIXELS_PER_UNIT_1X = 108
SUPERSAMPLED_PIXELS_PER_UNIT = 432   # everything is drawn this big, then shrunk, for smooth lines

GRID_MINOR_STEP = 0.25   # faint line every quarter unit
GRID_MAJOR_STEP = 1.0    # strong line every unit

# Line widths in final pixels at 1x. They grow with the 2x export.
OUTLINE_WIDTH = 2.0
CREASE_WIDTH = 1.2
MAJOR_LINE_WIDTH = 1.0
MINOR_LINE_WIDTH = 0.6

# Light from the upper left, so the top is lightest, the left face middle, the right face darkest.
LIGHT_DIRECTION = np.array([-0.5, 1.0, -0.25])
LIGHT_DIRECTION = LIGHT_DIRECTION / np.linalg.norm(LIGHT_DIRECTION)

GREY = (205, 205, 210)
OUTLINE_COLOUR = (38, 38, 46)

pitch = math.radians(PITCH_DEG)
yaw = math.radians(YAW_DEG)
CAMERA_RIGHT = np.array([math.cos(yaw), 0.0, -math.sin(yaw)])
CAMERA_UP = np.array([math.sin(pitch) * math.sin(yaw), math.cos(pitch), math.sin(pitch) * math.cos(yaw)])
CAMERA_FORWARD = np.array([math.cos(pitch) * math.sin(yaw), -math.sin(pitch), math.cos(pitch) * math.cos(yaw)])


# ---------------------------------------------------------------- shape building

class Face:
    """One flat convex polygon of a shape, in world units (Unity axes: y is up)."""

    def __init__(self, points, group, grid_axes=(0, 1, 2), centre_hint=None):
        self.points = np.array(points, dtype=float)
        self.group = group            # faces sharing a group are drawn as one smooth surface
        self.grid_axes = grid_axes    # which world planes (x=k, y=k, z=k) print grid lines here
        normal = np.cross(self.points[1] - self.points[0], self.points[2] - self.points[0])
        if np.linalg.norm(normal) < 1e-9:
            normal = np.cross(self.points[2] - self.points[0], self.points[-1] - self.points[0])
        normal = normal / np.linalg.norm(normal)
        if centre_hint is not None and np.dot(normal, self.points.mean(axis=0) - centre_hint) < 0:
            normal = -normal
        self.normal = normal
        self.extra_lines = []         # extra 3D segments drawn as minor lines (cylinder seams)


class Shape:
    """A named solid made of faces, with a footprint used for its ground pivot."""

    def __init__(self, name, faces, footprint_centre, colour=GREY):
        self.name = name
        self.faces = faces
        self.footprint_centre = np.array(footprint_centre, dtype=float)
        self.colour = colour

    def moved(self, offset, colour=None, name=None):
        offset = np.array(offset, dtype=float)
        faces = []
        for face in self.faces:
            copy = Face(face.points + offset, face.group, face.grid_axes)
            copy.normal = face.normal
            copy.extra_lines = [(a + offset, b + offset) for a, b in face.extra_lines]
            faces.append(copy)
        return Shape(name or self.name, faces, self.footprint_centre + offset, colour or self.colour)


def polyhedron(name, vertices, face_indices, footprint_centre, axes_for_face=None):
    vertices = [np.array(v, dtype=float) for v in vertices]
    centre = np.mean(vertices, axis=0)
    faces = []
    for index, indices in enumerate(face_indices):
        axes = (0, 1, 2) if axes_for_face is None else axes_for_face(index)
        faces.append(Face([vertices[i] for i in indices], group=index + 1, grid_axes=axes, centre_hint=centre))
    return Shape(name, faces, footprint_centre)


def box(name, size_x, size_y, size_z, x=0.0, y=0.0, z=0.0):
    x1, y1, z1 = x + size_x, y + size_y, z + size_z
    vertices = [(x, y, z), (x1, y, z), (x1, y, z1), (x, y, z1),
                (x, y1, z), (x1, y1, z), (x1, y1, z1), (x, y1, z1)]
    face_indices = [(0, 1, 2, 3), (4, 5, 6, 7), (0, 1, 5, 4), (2, 3, 7, 6), (0, 3, 7, 4), (1, 2, 6, 5)]
    return polyhedron(name, vertices, face_indices, (x + size_x / 2, y, z + size_z / 2))


def ramp(name, high_side, length=1.0, width=1.0, height=1.0):
    """A wedge: a slope rising from 0 to `height`, highest at `high_side` (+x, -x, +z or -z).

    Built once rising toward +x (slope along x, `length` long, `width` across),
    then its points are mapped to the asked direction.
    """
    canonical = [(0, 0, 0), (length, 0, 0), (length, 0, width), (0, 0, width),
                 (length, height, 0), (length, height, width)]
    faces = [(0, 1, 2, 3),   # bottom
             (0, 3, 5, 4),   # slope
             (1, 2, 5, 4),   # tall wall at the high end
             (0, 1, 4),      # triangle side
             (3, 2, 5)]      # triangle side
    mapping = {
        "+x": lambda x, y, z: (x, y, z),
        "-x": lambda x, y, z: (length - x, y, z),
        "+z": lambda x, y, z: (z, y, x),
        "-z": lambda x, y, z: (z, y, length - x),
    }[high_side]
    vertices = [mapping(*p) for p in canonical]
    xs = [v[0] for v in vertices]
    zs = [v[2] for v in vertices]
    return polyhedron(name, vertices, faces, (max(xs) / 2, 0, max(zs) / 2))


def prism(name, ridge_along, length=1.0, width=1.0, height=1.0):
    """A triangular roof shape, its ridge running along x or z."""
    if ridge_along == "x":
        vertices = [(0, 0, 0), (length, 0, 0), (length, 0, width), (0, 0, width),
                    (0, height, width / 2), (length, height, width / 2)]
        faces = [(0, 1, 2, 3), (0, 1, 5, 4), (3, 2, 5, 4), (0, 3, 4), (1, 2, 5)]
        footprint = (length / 2, 0, width / 2)
    else:
        vertices = [(0, 0, 0), (width, 0, 0), (width, 0, length), (0, 0, length),
                    (width / 2, height, 0), (width / 2, height, length)]
        faces = [(0, 1, 2, 3), (0, 3, 5, 4), (1, 2, 5, 4), (0, 1, 4), (3, 2, 5)]
        footprint = (width / 2, 0, length / 2)
    return polyhedron(name, vertices, faces, footprint)


def pyramid(name, size=1.0, height=1.0):
    vertices = [(0, 0, 0), (size, 0, 0), (size, 0, size), (0, 0, size), (size / 2, height, size / 2)]
    faces = [(0, 1, 2, 3), (0, 1, 4), (1, 2, 4), (2, 3, 4), (3, 0, 4)]
    return polyhedron(name, vertices, faces, (size / 2, 0, size / 2))


def stairs(name, rising_toward, steps=4, size=1.0, height=1.0):
    """Stairs filling one cell, made of stacked boxes, highest at `rising_toward`."""
    faces = []
    depth = size / steps
    for step in range(steps):
        step_height = height * (step + 1) / steps
        start = size - depth * (step + 1) if rising_toward in ("-x", "-z") else depth * step
        if rising_toward in ("+x", "-x"):
            part = box("", depth, step_height, size, x=start)
        else:
            part = box("", size, step_height, depth, z=start)
        for face in part.faces:
            face.group += step * 10
            faces.append(face)
    return Shape(name, faces, (size / 2, 0, size / 2))


def cylinder(name, diameter=1.0, height=1.0, sides=48, seams=8):
    radius = diameter / 2
    centre = np.array([radius, 0.0, radius])
    ring = [centre + radius * np.array([math.cos(2 * math.pi * i / sides), 0, math.sin(2 * math.pi * i / sides)])
            for i in range(sides)]
    top_ring = [p + np.array([0, height, 0]) for p in ring]
    solid_centre = centre + np.array([0, height / 2, 0])
    faces = [Face(ring, group=1, grid_axes=(0, 2), centre_hint=solid_centre),
             Face(top_ring, group=2, grid_axes=(0, 2), centre_hint=solid_centre)]
    for i in range(sides):
        j = (i + 1) % sides
        side = Face([ring[i], ring[j], top_ring[j], top_ring[i]], group=3, grid_axes=(1,), centre_hint=solid_centre)
        if i % (sides // seams) == 0:
            side.extra_lines.append((ring[i], top_ring[i]))
        faces.append(side)
    return Shape(name, faces, centre)


def cone(name, diameter=1.0, height=1.0, sides=48, seams=8):
    radius = diameter / 2
    centre = np.array([radius, 0.0, radius])
    tip = centre + np.array([0, height, 0])
    ring = [centre + radius * np.array([math.cos(2 * math.pi * i / sides), 0, math.sin(2 * math.pi * i / sides)])
            for i in range(sides)]
    solid_centre = centre + np.array([0, height / 3, 0])
    faces = [Face(ring, group=1, grid_axes=(0, 2), centre_hint=solid_centre)]
    for i in range(sides):
        j = (i + 1) % sides
        side = Face([ring[i], ring[j], tip], group=2, grid_axes=(1,), centre_hint=solid_centre)
        if i % (sides // seams) == 0:
            side.extra_lines.append((ring[i], tip))
        faces.append(side)
    return Shape(name, faces, centre)


def sphere(name, diameter=1.0, slices=48, stacks=24, seams=8):
    radius = diameter / 2
    centre = np.array([radius, radius, radius])
    def point(stack, slice_index):
        theta = math.pi * stack / stacks
        phi = 2 * math.pi * slice_index / slices
        return centre + radius * np.array([math.sin(theta) * math.cos(phi), math.cos(theta), math.sin(theta) * math.sin(phi)])
    faces = []
    for stack in range(stacks):
        for slice_index in range(slices):
            corners = [point(stack, slice_index), point(stack, slice_index + 1),
                       point(stack + 1, slice_index + 1), point(stack + 1, slice_index)]
            if stack == 0:
                corners = [corners[0], corners[2], corners[3]]
            elif stack == stacks - 1:
                corners = [corners[0], corners[1], corners[2]]
            face = Face(corners, group=1, grid_axes=(1,), centre_hint=centre)
            if slice_index % (slices // seams) == 0:
                face.extra_lines.append((point(stack, slice_index), point(stack + 1, slice_index)))
            faces.append(face)
    return Shape(name, faces, (radius, 0, radius))


def half_cylinder(name, axis_along, length=1.0, diameter=1.0, sides=24):
    """A half-round lying on the ground (a log cut in half, an arch top), curved side up."""
    radius = diameter / 2
    arc = [(radius - radius * math.cos(math.pi * i / sides), radius * math.sin(math.pi * i / sides)) for i in range(sides + 1)]
    def to_world(across, up, along):
        return (along, up, across) if axis_along == "x" else (across, up, along)
    solid_centre = np.array(to_world(radius, radius / 3, length / 2))
    faces = [Face([to_world(a, u, 0) for a, u in arc], group=1, centre_hint=solid_centre),
             Face([to_world(a, u, length) for a, u in arc], group=2, centre_hint=solid_centre),
             Face([to_world(0, 0, 0), to_world(diameter, 0, 0), to_world(diameter, 0, length), to_world(0, 0, length)],
                  group=3, centre_hint=solid_centre)]
    along_axis = 0 if axis_along == "x" else 2
    for i in range(sides):
        a0, u0 = arc[i]
        a1, u1 = arc[i + 1]
        faces.append(Face([to_world(a0, u0, 0), to_world(a1, u1, 0), to_world(a1, u1, length), to_world(a0, u0, length)],
                          group=4, grid_axes=(along_axis,), centre_hint=solid_centre))
        if i % (sides // 4) == 0 and i > 0:
            faces[-1].extra_lines.append((np.array(to_world(a0, u0, 0)), np.array(to_world(a0, u0, length))))
    footprint = to_world(radius, 0, length / 2)
    return Shape(name, faces, footprint)


def kit_shapes():
    """Every piece in the kit. Names say footprint (x by z) and height in world units."""
    return [
        box("floor_1x1_h0.25", 1, 0.25, 1),
        box("floor_2x2_h0.25", 2, 0.25, 2),
        box("cube_1x1_h1", 1, 1, 1),
        box("block_1x1_h0.5", 1, 0.5, 1),
        box("block_2x1_h1", 2, 1, 1),
        box("block_1x2_h1", 1, 1, 2),
        box("block_2x2_h2", 2, 2, 2),
        box("wall_1x0.25_h2_alongX", 1, 2, 0.25),
        box("wall_0.25x1_h2_alongZ", 0.25, 2, 1),
        box("pillar_0.5x0.5_h2", 0.5, 2, 0.5),
        ramp("ramp_1x1_h1_upPlusX", "+x"),
        ramp("ramp_1x1_h1_upMinusX", "-x"),
        ramp("ramp_1x1_h1_upPlusZ", "+z"),
        ramp("ramp_1x1_h1_upMinusZ", "-z"),
        ramp("ramp_2x1_h1_upPlusX", "+x", length=2),
        ramp("ramp_1x2_h1_upPlusZ", "+z", length=2),
        prism("roof_1x1_h1_ridgeX", "x"),
        prism("roof_1x1_h1_ridgeZ", "z"),
        pyramid("pyramid_1x1_h1"),
        stairs("stairs_1x1_h1_upPlusX", "+x"),
        stairs("stairs_1x1_h1_upMinusX", "-x"),
        stairs("stairs_1x1_h1_upPlusZ", "+z"),
        stairs("stairs_1x1_h1_upMinusZ", "-z"),
        cylinder("cylinder_d1_h1"),
        cylinder("cylinder_d1_h0.25", height=0.25),
        cylinder("cylinder_d0.5_h2", diameter=0.5, height=2),
        cylinder("cylinder_d2_h0.5", diameter=2, height=0.5, sides=64),
        half_cylinder("halfround_1x1_alongX", "x"),
        half_cylinder("halfround_1x1_alongZ", "z"),
        cone("cone_d1_h1"),
        sphere("sphere_d1"),
    ]


# ---------------------------------------------------------------- rendering

def project(points, scale):
    """World points -> (screen x, screen y with y down, depth). Orthographic."""
    points = np.atleast_2d(points)
    return points @ CAMERA_RIGHT * scale, -(points @ CAMERA_UP) * scale, points @ CAMERA_FORWARD


def face_shade(normal):
    return 0.42 + 0.58 * max(0.0, float(np.dot(normal, LIGHT_DIRECTION)))


def grid_segments(face):
    """Where the world planes x=k, y=k, z=k cut this face, as (start, end, is_major)."""
    segments = []
    points = face.points
    for axis in face.grid_axes:
        if abs(face.normal[axis]) > 0.999:
            continue   # the face lies in this plane
        low, high = points[:, axis].min(), points[:, axis].max()
        k = math.floor(low / GRID_MINOR_STEP) * GRID_MINOR_STEP
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
            k += GRID_MINOR_STEP
    for start, end in face.extra_lines:
        segments.append((start, end, False))
    return segments


def disk_offsets(radius):
    r = int(math.ceil(radius))
    return [(dx, dy) for dy in range(-r, r + 1) for dx in range(-r, r + 1)
            if (dx or dy) and dx * dx + dy * dy <= radius * radius]


def shifted(array, dx, dy, fill):
    """array moved so out[y, x] = array[y + dy, x + dx]; cells that fall off the edge get `fill`."""
    out = np.full_like(array, fill)
    h, w = array.shape
    out[max(0, -dy):h - max(0, dy), max(0, -dx):w - max(0, dx)] = \
        array[max(0, dy):h - max(0, -dy), max(0, dx):w - max(0, -dx)]
    return out


def render(shapes, size_multiplier, padding_units=0.15):
    """Draw shapes together with a depth buffer, so they hide each other correctly.

    Returns grey RGBA, line-only RGBA, a per-pixel shape index (-1 = none) at final size,
    and the pixel offset that was added to screen coordinates.
    """
    final_ppu = PIXELS_PER_UNIT_1X * size_multiplier
    supersample = max(1, round(SUPERSAMPLED_PIXELS_PER_UNIT / final_ppu))
    scale = final_ppu * supersample

    all_points = np.concatenate([face.points for shape in shapes for face in shape.faces])
    sx, sy, _ = project(all_points, scale)
    pad = padding_units * scale
    # Snap the canvas origin to whole final pixels so the pivots stay exact.
    origin_x = math.floor((sx.min() - pad) / supersample) * supersample
    origin_y = math.floor((sy.min() - pad) / supersample) * supersample
    width = int(math.ceil((sx.max() + pad - origin_x) / supersample)) * supersample
    height = int(math.ceil((sy.max() + pad - origin_y) / supersample)) * supersample

    depth = np.full((height, width), np.inf, dtype=np.float32)
    face_id = np.full((height, width), -1, dtype=np.int32)
    face_list = []          # (shape index, face)
    for shape_index, shape in enumerate(shapes):
        for face in shape.faces:
            if np.dot(face.normal, CAMERA_FORWARD) >= -1e-6:
                continue    # facing away from the camera
            fx, fy, fz = project(face.points, scale)
            fx, fy = fx - origin_x, fy - origin_y
            x0, x1 = int(max(0, math.floor(fx.min()))), int(min(width, math.ceil(fx.max()) + 1))
            y0, y1 = int(max(0, math.floor(fy.min()))), int(min(height, math.ceil(fy.max()) + 1))
            if x1 <= x0 or y1 <= y0:
                continue
            mask_image = Image.new("1", (x1 - x0, y1 - y0), 0)
            ImageDraw.Draw(mask_image).polygon([(px - x0, py - y0) for px, py in zip(fx, fy)], fill=1)
            mask = np.array(mask_image, dtype=bool)
            # Depth is linear across a flat face: fit depth = a*x + b*y + c from three corners.
            matrix = np.array([[fx[0], fy[0], 1], [fx[1], fy[1], 1], [fx[2], fy[2], 1]])
            if abs(np.linalg.det(matrix)) < 1e-6:
                matrix = np.array([[fx[0], fy[0], 1], [fx[1], fy[1], 1], [fx[-1], fy[-1], 1]])
                coefficients = np.linalg.lstsq(matrix, np.array([fz[0], fz[1], fz[-1]]), rcond=None)[0]
            else:
                coefficients = np.linalg.solve(matrix, fz[:3])
            ys, xs = np.mgrid[y0:y1, x0:x1]
            face_depth = (coefficients[0] * (xs + 0.5) + coefficients[1] * (ys + 0.5) + coefficients[2]).astype(np.float32)
            region_depth = depth[y0:y1, x0:x1]
            closer = mask & (face_depth < region_depth - 1e-5)
            region_depth[closer] = face_depth[closer]
            face_id[y0:y1, x0:x1][closer] = len(face_list)
            face_list.append((shape_index, face))

    # Per-pixel lookups.
    face_shape = np.array([s for s, _ in face_list] + [-1], dtype=np.int32)
    face_group = np.array([s * 1000 + f.group for s, f in face_list] + [-1], dtype=np.int64)
    shape_index = face_shape[face_id]
    group = face_group[face_id]

    # Grid lines: draw every face's lines tagged with the face id, keep a pixel only on its own face.
    major_lines = Image.new("I", (width, height), -1)
    minor_lines = Image.new("I", (width, height), -1)
    major_draw, minor_draw = ImageDraw.Draw(major_lines), ImageDraw.Draw(minor_lines)
    major_width = max(1, round(MAJOR_LINE_WIDTH * size_multiplier * supersample))
    minor_width = max(1, round(MINOR_LINE_WIDTH * size_multiplier * supersample))
    for index, (_, face) in enumerate(face_list):
        for start, end, is_major in grid_segments(face):
            px, py, _ = project(np.array([start, end]), scale)
            line = [(px[0] - origin_x, py[0] - origin_y), (px[1] - origin_x, py[1] - origin_y)]
            if is_major:
                major_draw.line(line, fill=index, width=major_width)
            else:
                minor_draw.line(line, fill=index, width=minor_width)
    on_major = (np.array(major_lines) == face_id) & (face_id >= 0)
    on_minor = (np.array(minor_lines) == face_id) & (face_id >= 0)

    # Outline where one shape meets another or the background; crease where two flat parts of one shape meet.
    outline = np.zeros((height, width), dtype=bool)
    outline_owner = shape_index.copy()
    for dx, dy in disk_offsets(OUTLINE_WIDTH * size_multiplier * supersample / 2):
        neighbour = shifted(shape_index, dx, dy, -1)
        differs = neighbour != shape_index
        outline |= differs
        # A background pixel in the outline belongs to the shape next to it (for per-shape layers).
        takes = differs & (outline_owner < 0) & (neighbour >= 0)
        outline_owner[takes] = neighbour[takes]
    outline &= outline_owner >= 0
    crease = np.zeros((height, width), dtype=bool)
    for dx, dy in disk_offsets(CREASE_WIDTH * size_multiplier * supersample / 2):
        neighbour_group = shifted(group, dx, dy, -1)
        neighbour_shape = shifted(shape_index, dx, dy, -1)
        crease |= (neighbour_group != group) & (neighbour_shape == shape_index) & (shape_index >= 0)

    # Colours.
    face_colour = np.zeros((len(face_list) + 1, 3), dtype=np.float32)
    for index, (s, face) in enumerate(face_list):
        face_colour[index] = np.array(shapes[s].colour, dtype=np.float32) * face_shade(face.normal)
    grey = face_colour[face_id]
    grey[on_minor] *= 0.82
    grey[on_major] *= 0.58
    grey[crease] = grey[crease] * 0.35 + np.array(OUTLINE_COLOUR) * 0.65
    grey[outline] = OUTLINE_COLOUR
    filled = (face_id >= 0) | outline
    grey_alpha = filled.astype(np.float32) * 255

    lines_rgb = np.zeros((height, width, 3), dtype=np.float32)
    lines_rgb[:] = OUTLINE_COLOUR
    lines_alpha = np.zeros((height, width), dtype=np.float32)
    lines_alpha[on_minor] = 70
    lines_alpha[on_major] = 160
    lines_alpha[crease] = 230
    lines_alpha[outline] = 255

    def finish(rgb, alpha):
        image = Image.fromarray(np.dstack([np.clip(rgb, 0, 255), alpha]).astype(np.uint8), "RGBA")
        return image.resize((width // supersample, height // supersample), Image.Resampling.BOX)

    owner = np.where(outline, outline_owner, shape_index)
    return finish(grey, grey_alpha), finish(lines_rgb, lines_alpha), owner, supersample, (origin_x, origin_y, scale)


def pivot_in_pixels(shape, origin_and_scale, supersample):
    origin_x, origin_y, scale = origin_and_scale
    px, py, _ = project(shape.footprint_centre, scale)
    return (float(px[0] - origin_x) / supersample, float(py[0] - origin_y) / supersample)


# ---------------------------------------------------------------- outputs

def save_shapes():
    pivots = {}
    for multiplier in (1, 2):
        for style in ("grey", "lines"):
            os.makedirs(os.path.join(KIT_DIR, "shapes", f"{multiplier}x", style), exist_ok=True)
        for shape in kit_shapes():
            grey, lines, _, supersample, origin = render([shape], multiplier)
            grey.save(os.path.join(KIT_DIR, "shapes", f"{multiplier}x", "grey", shape.name + ".png"), optimize=True)
            lines.save(os.path.join(KIT_DIR, "shapes", f"{multiplier}x", "lines", shape.name + ".png"), optimize=True)
            px, py = pivot_in_pixels(shape, origin, supersample)
            pivots.setdefault(shape.name, {})[f"{multiplier}x"] = {
                "size_px": list(grey.size),
                "ground_pivot_px_from_top_left": [round(px, 2), round(py, 2)],
                "unity_pivot_normalized": [round(px / grey.size[0], 4), round(1 - py / grey.size[1], 4)],
            }
    with open(os.path.join(KIT_DIR, "shapes", "pivots.json"), "w") as handle:
        json.dump({"_about": "Ground pivot = the centre of the shape's footprint on the floor. "
                             "unity_pivot_normalized is measured from the bottom-left, as Unity's Sprite Editor wants it.",
                   "_pixels_per_unit": {"1x": PIXELS_PER_UNIT_1X, "2x": PIXELS_PER_UNIT_1X * 2},
                   "shapes": pivots}, handle, indent=2)


def save_floor_grid(cells=16):
    for multiplier in (1, 2):
        final_ppu = PIXELS_PER_UNIT_1X * multiplier
        supersample = 4 if multiplier == 1 else 2
        scale = final_ppu * supersample
        corners = np.array([[0, 0, 0], [cells, 0, 0], [cells, 0, cells], [0, 0, cells]], dtype=float)
        sx, sy, _ = project(corners, scale)
        pad = 0.2 * scale
        origin_x = math.floor((sx.min() - pad) / supersample) * supersample
        origin_y = math.floor((sy.min() - pad) / supersample) * supersample
        width = int(math.ceil((sx.max() + pad - origin_x) / supersample)) * supersample
        height = int(math.ceil((sy.max() + pad - origin_y) / supersample)) * supersample
        image = Image.new("RGBA", (width, height), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        def to_pixel(x, z):
            px, py, _ = project(np.array([x, 0.0, z]), scale)
            return (float(px[0] - origin_x), float(py[0] - origin_y))
        steps = int(round(cells / GRID_MINOR_STEP))
        for pass_major in (False, True):
            for i in range(steps + 1):
                k = i * GRID_MINOR_STEP
                is_major = abs(k - round(k)) < 1e-6
                if is_major != pass_major:
                    continue
                colour = OUTLINE_COLOUR + ((200,) if is_major else (60,))
                line_width = max(1, round((MAJOR_LINE_WIDTH if is_major else MINOR_LINE_WIDTH) * multiplier * supersample))
                if k in (0, cells):
                    line_width = max(1, round(OUTLINE_WIDTH * multiplier * supersample))
                draw.line([to_pixel(k, 0), to_pixel(k, cells)], fill=colour, width=line_width)
                draw.line([to_pixel(0, k), to_pixel(cells, k)], fill=colour, width=line_width)
        font = load_font(int(14 * multiplier * supersample))
        for i in range(cells):
            draw.text(to_pixel(i + 0.5, -0.25), f"x{i}", fill=OUTLINE_COLOUR + (200,), font=font, anchor="mm")
            draw.text(to_pixel(-0.25, i + 0.5), f"z{i}", fill=OUTLINE_COLOUR + (200,), font=font, anchor="mm")
        image = image.resize((width // supersample, height // supersample), Image.Resampling.BOX)
        image.save(os.path.join(KIT_DIR, f"floor_grid_{multiplier}x.png"), optimize=True)


def load_font(size):
    for path in ("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(path, size)
        except OSError:
            pass
    return ImageFont.load_default(size=size)


def save_kit_sheet():
    tiles = []
    for shape in kit_shapes():
        grey, _, _, _, _ = render([shape], 1)
        tiles.append((shape.name, grey))
    columns = 6
    cell_w = max(t.size[0] for _, t in tiles) + 40
    cell_h = max(t.size[1] for _, t in tiles) + 60
    rows = math.ceil(len(tiles) / columns)
    title_h = 90
    sheet = Image.new("RGBA", (columns * cell_w, rows * cell_h + title_h), (58, 56, 62, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((24, 20), "Blockout kit - ThreeQuarter (ortho, pitch 30, yaw 45 = 2:1 isometric)",
              fill=(235, 235, 240), font=load_font(30))
    draw.text((24, 58), "1 unit = 108 px at 1080p. Grid: strong line every 1 unit, faint every 0.25.",
              fill=(190, 190, 200), font=load_font(18))
    font = load_font(15)
    for index, (name, tile) in enumerate(tiles):
        cx = (index % columns) * cell_w
        cy = (index // columns) * cell_h + title_h
        sheet.alpha_composite(tile, (cx + (cell_w - tile.size[0]) // 2, cy + (cell_h - 40 - tile.size[1]) // 2 + 10))
        draw.text((cx + cell_w // 2, cy + cell_h - 22), name, fill=(220, 220, 228), font=font, anchor="mm")
    sheet.convert("RGB").save(os.path.join(KIT_DIR, "kit_sheet.png"), optimize=True)


def example_scene():
    """A small diorama on a 6 x 6 floor, to show how the pieces stack on the grid."""
    shapes_by_name = {s.name: s for s in kit_shapes()}
    def place(name, x, y, z, colour=None, label=None):
        return shapes_by_name[name].moved((x, y, z), colour, label or f"{name}@{x},{y},{z}")
    ground = (150, 185, 120)
    stone = (190, 190, 198)
    wood = (200, 160, 115)
    roof = (205, 110, 95)
    water = (120, 170, 210)
    pieces = []
    for x in range(6):
        for z in range(6):
            colour = water if (x, z) in ((5, 0), (5, 1), (4, 0)) else ground
            pieces.append(place("floor_1x1_h0.25", x, 0, z, colour, f"floor_{x}_{z}"))
    top = 0.25
    pieces += [
        place("block_2x2_h2", 4, top, 4, stone, "platform_2x2"),
        place("pyramid_1x1_h1", 5, top + 2, 5, roof, "platform_top"),
        place("stairs_1x1_h1_upPlusX", 2, top, 4, stone, "stairs_low"),
        place("cube_1x1_h1", 3, top, 4, stone, "stairs_base"),
        place("stairs_1x1_h1_upPlusX", 3, top + 1, 4, stone, "stairs_high"),
        place("cube_1x1_h1", 1, top, 4, wood, "house_body"),
        place("roof_1x1_h1_ridgeZ", 1, top + 1, 4, roof, "house_roof"),
        place("ramp_1x1_h1_upPlusX", 2, top, 2, stone, "ramp"),
        place("cube_1x1_h1", 3, top, 2, stone, "ramp_top_block"),
        place("cylinder_d0.5_h2", 1.25, top, 1.25, wood, "tree_trunk"),
        place("sphere_d1", 1, top + 1.9, 1, (120, 165, 95), "tree_top"),
        place("cone_d1_h1", 0, top, 0, (150, 175, 110), "bush_cone"),
        place("cylinder_d1_h1", 2, top, 0, stone, "barrel_cylinder"),
        place("halfround_1x1_alongX", 3, top, 0, wood, "log"),
        place("cylinder_d1_h0.25", 0, top, 2, stone, "round_plate"),
    ]
    return pieces


def save_example_scene():
    pieces = example_scene()
    for multiplier in (1, 2):
        grey, lines, owner, supersample, _ = render(pieces, multiplier)
        backdrop = Image.new("RGBA", grey.size, (58, 56, 62, 255))
        backdrop.alpha_composite(grey)
        backdrop.convert("RGB").save(os.path.join(KIT_DIR, f"example_scene_{multiplier}x.png"), optimize=True)
        if multiplier == 2:
            save_scene_psd(pieces, grey, lines, owner, supersample)


def save_scene_psd(pieces, grey, lines, owner, supersample):
    """One layer per piece (only the part the camera sees), plus a line-only layer on top."""
    from psd_tools import PSDImage
    from psd_tools.api.layers import Group, PixelLayer
    from psd_tools.constants import Compression
    size = grey.size
    owner_small = Image.fromarray((owner + 1).astype(np.int32), "I").resize(size, Image.Resampling.NEAREST)
    owner_small = np.array(owner_small) - 1
    grey_array = np.array(grey)
    psd = PSDImage.new("RGBA", size, color=(0, 0, 0, 0))
    background = Image.new("RGBA", size, (58, 56, 62, 255))
    psd.append(PixelLayer.frompil(background, psd, name="background", compression=Compression.RLE))
    blockout = Group.new(psd, name="blockout (one layer per piece)")
    for index, piece in enumerate(pieces):
        layer_array = grey_array.copy()
        layer_array[owner_small != index] = 0
        layer_image = Image.fromarray(layer_array, "RGBA")
        box_area = layer_image.getbbox()
        if not box_area:
            continue
        blockout.append(PixelLayer.frompil(layer_image.crop(box_area), psd, name=piece.name,
                                           top=box_area[1], left=box_area[0], compression=Compression.RLE))
    psd.append(PixelLayer.frompil(lines, psd, name="lines only (set to Multiply, paint under)",
                                  compression=Compression.RLE))
    psd.append(PixelLayer.frompil(Image.new("RGBA", size, (0, 0, 0, 0)), psd, name="paint here",
                                  compression=Compression.RLE))
    # Store the flattened picture compressed too, so apps that only read the flat image see the scene.
    from psd_tools.psd.image_data import ImageData
    flat = psd.composite().convert("RGBA")
    merged = ImageData(compression=Compression.RLE)
    merged.set_data([channel.tobytes() for channel in flat.split()[:psd._record.header.channels]], psd._record.header)
    psd._record.image_data = merged
    psd.save(os.path.join(KIT_DIR, "example_scene_2x.psd"))


def main():
    save_shapes()
    save_floor_grid()
    save_kit_sheet()
    save_example_scene()
    print("done ->", KIT_DIR)


if __name__ == "__main__":
    sys.exit(main())
