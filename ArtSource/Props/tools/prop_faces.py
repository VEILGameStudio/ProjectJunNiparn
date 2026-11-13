# prop_faces.py
# Turns one painted picture of a box-shaped prop (wardrobe, cupboard, crate, bookshelf...)
# into textures that sit on the faces of a real 3D box, for ThreeQuarter rooms.
#
# Why: one upright sprite card cannot both stand on the floor and hide/show the player
# correctly for a wide solid prop. Painting the picture onto the box faces gives every
# pixel its real depth, and from the fixed ThreeQuarter camera it looks the same as the drawing.
#
# Usage (run in this order):
#   python3 prop_faces.py grid  <prop folder>   # source picture with a pixel grid, to read corner pixels
#   python3 prop_faces.py fit   <prop folder>   # works out pitch, yaw, width and depth from corners_px
#   python3 prop_faces.py check <prop folder>   # draws the fitted box over the source picture
#   python3 prop_faces.py bake  <prop folder>   # writes the face textures, upright parts, preview
#   python3 prop_faces.py unity <prop folder>   # writes textures, materials and the prefab into Assets/
#
# The prop folder holds the source picture and prop.json (see ../README.md for every field).
# Needs: Pillow, numpy, scipy.

import json
import math
import os
import sys
import uuid

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

GAME_PITCH, GAME_YAW = 30.0, 45.0     # the ThreeQuarter preset, CLAUDE.md §7
FACE_NAMES = ["Top", "Front", "Side"]


def camera_axes(pitch, yaw):
    p, y = math.radians(pitch), math.radians(yaw)
    right = np.array([math.cos(y), 0, -math.sin(y)])
    up = np.array([math.sin(p) * math.sin(y), math.cos(p), math.sin(p) * math.cos(y)])
    return right, up


def project(points, camera, scale, offset):
    """World points (x, y, z) -> picture pixels, orthographic, y down."""
    right, up = camera
    points = np.atleast_2d(points)
    return np.stack([points @ right * scale + offset[0], -(points @ up) * scale + offset[1]], axis=1)


class Prop:
    def __init__(self, folder):
        self.folder = folder
        self.cfg = json.load(open(os.path.join(folder, "prop.json")))
        c = self.cfg
        self.W, self.D, self.H = c["width_z"], c["depth_x"], c["height"]
        self.picture = Image.open(os.path.join(folder, c["source"])).convert("RGBA")
        self.pixels = np.array(self.picture).astype(np.float32)
        self.camera = camera_axes(c["source_pitch"], c["source_yaw"])
        self._fit()

    def _fit(self):
        """Scale and place the box: from corners_px when given, otherwise from body_rows."""
        corners = self.cfg.get("corners_px")
        if corners:
            near, top = np.array(corners["near_bottom"], float), np.array(corners["near_top"], float)
            raw_height = project([(0, self.H, 0)], self.camera, 1, (0, 0))[0]
            self.scale = (near[1] - top[1]) / -raw_height[1]
            self.offset = (near[0], near[1])
            return
        solid = self.pixels[:, :, 3] > 128
        corners = np.array([(x, h, z) for x in (0, self.D) for h in (0, self.H) for z in (0, self.W)], float)
        raw = project(corners, self.camera, 1, (0, 0))
        top_row, bottom_row = self.cfg["body_rows"]
        _, xs = np.where(solid[top_row:bottom_row])
        self.scale = (xs.max() - xs.min()) / (raw[:, 0].max() - raw[:, 0].min())
        lowest = np.where(solid.any(axis=1))[0].max()
        self.offset = (xs.min() - raw[:, 0].min() * self.scale, lowest - raw[:, 1].max() * self.scale)

    def face_quads(self):
        W, D, H = self.W, self.D, self.H
        return {"Top": [(0, H, 0), (0, H, W), (D, H, W), (D, H, 0)],
                "Front": [(0, 0, 0), (0, 0, W), (0, H, W), (0, H, 0)],   # x = 0, faces screen-left
                "Side": [(0, 0, 0), (D, 0, 0), (D, H, 0), (0, H, 0)]}    # z = 0, faces screen-right

    def to_picture(self, points):
        return project(points, self.camera, self.scale, self.offset)

    def check(self):
        out = Image.new("RGBA", self.picture.size, (255, 255, 255, 255))
        out.alpha_composite(self.picture)
        draw = ImageDraw.Draw(out)
        corners = [(x, h, z) for x in (0, self.D) for h in (0, self.H) for z in (0, self.W)]
        for a in corners:
            for b in corners:
                if a < b and sum(p != q for p, q in zip(a, b)) == 1:
                    pa, pb = self.to_picture(np.array([a, b], float))
                    draw.line([tuple(pa), tuple(pb)], fill=(0, 190, 255, 255), width=2)
        path = os.path.join(self.folder, "check_fit.png")
        out.resize((out.size[0] * 2, out.size[1] * 2)).save(path)
        print("wrote", path, "- the blue box should hug the body; adjust pitch/yaw/size in prop.json until it does")

    def bake(self):
        c = self.cfg
        W, D, H, M, RES = self.W, self.D, self.H, c["margin"], c["texture_px_per_unit"]
        name = c["output_name"]
        size = self.picture.size

        # Each picture pixel belongs to the nearest face; overhangs go to the face they hang off.
        labels = np.zeros((size[1], size[0]), np.int32)
        for index, face in enumerate(FACE_NAMES, start=1):
            mask = Image.new("L", size, 0)
            ImageDraw.Draw(mask).polygon([tuple(p) for p in self.to_picture(np.array(self.face_quads()[face], float))], fill=1)
            labels[(np.array(mask) > 0) & (labels == 0)] = index
        _, (iy, ix) = ndimage.distance_transform_edt(labels == 0, return_indices=True)
        labels = labels[iy, ix]

        upright = np.zeros(labels.shape, bool)
        for part in c.get("upright_parts", []):
            x0, y0, x1, y1 = part["box"]
            region = np.zeros(labels.shape, bool)
            region[y0:y1, x0:x1] = True
            upright |= region & (self.pixels[:, :, 3] > 0)

        def bake_face(face, world_points, shape):
            picture_points = self.to_picture(world_points.reshape(-1, 3))
            px, py = picture_points[:, 0] - 0.5, picture_points[:, 1] - 0.5
            texels = np.zeros((len(picture_points), 4), np.float32)
            for channel in range(4):
                texels[:, channel] = ndimage.map_coordinates(self.pixels[:, :, channel], [py, px], order=1, mode="constant")
            row = np.clip(np.round(py).astype(int), 0, size[1] - 1)
            col = np.clip(np.round(px).astype(int), 0, size[0] - 1)
            mine = (labels[row, col] == FACE_NAMES.index(face) + 1) & ~upright[row, col]
            texels[~mine] = 0
            texels[:, 3] = np.where(texels[:, 3] > 128, 255, 0)   # hard edges for alpha clipping
            return texels.reshape(shape[0], shape[1], 4)

        def grid(columns, rows):
            return np.meshgrid((np.arange(columns) + 0.5) / RES, (np.arange(rows) + 0.5) / RES)

        tall, wide, deep = round((H + M) * RES), round((W + 2 * M) * RES), round((D + 2 * M) * RES)
        u, v = grid(wide, tall)
        front = bake_face("Front", np.stack([np.zeros_like(u), H + M - v, W + M - u], axis=-1), (tall, wide))
        u, v = grid(deep, tall)
        side = bake_face("Side", np.stack([-M + u, H + M - v, np.zeros_like(u)], axis=-1), (tall, deep))
        u, v = grid(deep, wide)
        top = bake_face("Top", np.stack([-M + u, np.full_like(u, H), W + M - v], axis=-1), (wide, deep))

        # Whatever an upright part hid on the top is filled with the top's average colour.
        inside = np.zeros(top.shape[:2], bool)
        inside[round(M * RES):round((M + W) * RES), round(M * RES):round((M + D) * RES)] = True
        seen = inside & (top[:, :, 3] > 0)
        if seen.any():
            top[inside & (top[:, :, 3] == 0)] = top[seen].mean(axis=0)
            top[inside, 3] = 255

        out_dir = os.path.join(self.folder, "out")
        os.makedirs(out_dir, exist_ok=True)
        for face, texels in (("Front", front), ("Side", side), ("Top", top)):
            Image.fromarray(texels.astype(np.uint8), "RGBA").save(os.path.join(out_dir, f"{name}_{face}.png"))

        report = {"box_size_xyz": [D, H, W], "margin": M,
                  "quads": {"Face_Front": {"position": [-D / 2 - 0.002, (H + M) / 2, 0], "rotation_euler": [0, 90, 0], "scale": [W + 2 * M, H + M, 1]},
                            "Face_Side": {"position": [0, (H + M) / 2, -W / 2 - 0.002], "rotation_euler": [0, 0, 0], "scale": [D + 2 * M, H + M, 1]},
                            "Face_Top": {"position": [0, H + 0.002, 0], "rotation_euler": [90, 0, 0], "scale": [D + 2 * M, W + 2 * M, 1]}},
                  "upright_parts": {}}

        # Upright parts (a statue, a lamp, a plant on top) stay billboard sprites standing on the top.
        sprite_ppu = c["sprite_px_per_unit"]
        k = sprite_ppu / self.scale
        top_corners = np.array([(0, H, 0), (0, H, W), (D, H, 0)], float)
        to_world = np.linalg.solve(np.c_[self.to_picture(top_corners), np.ones(3)], top_corners)
        for part in c.get("upright_parts", []):
            x0, y0, x1, y1 = part["box"]
            crop = self.pixels[y0:y1, x0:x1].copy()
            crop[~upright[y0:y1, x0:x1]] = 0
            rows, cols = np.where(crop[:, :, 3] > 0)
            crop = crop[rows.min():rows.max() + 1, cols.min():cols.max() + 1]
            left, upper = x0 + cols.min(), y0 + rows.min()
            sprite = Image.fromarray(crop.astype(np.uint8), "RGBA")
            # Screen size in the game, then stretched by 1/cos(30) for an upright card: rows scale by k / cos(source pitch).
            width = round(sprite.size[0] * k)
            height = round(sprite.size[1] * k / math.cos(math.radians(c["source_pitch"])))
            sprite.resize((width, height), Image.Resampling.LANCZOS).save(os.path.join(out_dir, f"{name}_{part['name']}.png"))
            bx, by = part["base"]
            base = np.array([bx, by, 1.0]) @ to_world
            report["upright_parts"][part["name"]] = {
                "size_px": [width, height], "ppu": sprite_ppu,
                "pivot_normalized": [round((bx - left) / sprite.size[0], 4), round(1 - (by - upper) / sprite.size[1], 4)],
                "position": [round(base[0] - D / 2, 3), H, round(base[2] - W / 2, 3)]}

        json.dump(report, open(os.path.join(out_dir, "unity_setup.json"), "w"), indent=2)
        self.preview(out_dir, report)
        print(json.dumps(report, indent=2))

    def preview(self, out_dir, report):
        """Renders the baked faces from the game camera, to compare with the source picture."""
        W, D, H, M, RES = self.W, self.D, self.H, self.cfg["margin"], self.cfg["texture_px_per_unit"]
        name = self.cfg["output_name"]
        game = camera_axes(GAME_PITCH, GAME_YAW)
        S = self.cfg["sprite_px_per_unit"]
        canvas_size = (int((W + D) * S + 200), int((H + W) * S + 200))
        origin = np.array([canvas_size[0] / 2, canvas_size[1] - 120])
        screen = lambda point: project(np.array(point, float), game, S, origin)[0]
        canvas = Image.new("RGBA", canvas_size, (230, 228, 224, 255))

        def place(texture, corner, along_u, along_v):
            a = screen(corner)
            b = screen(np.array(corner) + along_u / RES)
            d = screen(np.array(corner) + along_v / RES)
            inverse = np.linalg.inv(np.array([[b[0] - a[0], d[0] - a[0]], [b[1] - a[1], d[1] - a[1]]]))
            coefficients = (inverse[0, 0], inverse[0, 1], -(inverse[0] @ a), inverse[1, 0], inverse[1, 1], -(inverse[1] @ a))
            picture = Image.open(texture).convert("RGBA")
            canvas.alpha_composite(picture.transform(canvas_size, Image.Transform.AFFINE, coefficients, resample=Image.Resampling.BILINEAR))

        place(os.path.join(out_dir, f"{name}_Top.png"), (-D / 2 - M, H, W / 2 + M), np.array([1, 0, 0]), np.array([0, 0, -1]))
        place(os.path.join(out_dir, f"{name}_Side.png"), (-D / 2 - M, H + M, -W / 2), np.array([1, 0, 0]), np.array([0, -1, 0]))
        place(os.path.join(out_dir, f"{name}_Front.png"), (-D / 2, H + M, W / 2 + M), np.array([0, 0, -1]), np.array([0, -1, 0]))
        for part, info in report["upright_parts"].items():
            sprite = Image.open(os.path.join(out_dir, f"{name}_{part}.png"))
            w, h = sprite.size
            shown = sprite.resize((w, round(h * math.cos(math.radians(GAME_PITCH)))))
            base = screen(info["position"])
            px, py = info["pivot_normalized"]
            canvas.alpha_composite(shown, (int(base[0] - px * w), int(base[1] - (1 - py) * shown.size[1])))
        canvas.save(os.path.join(out_dir, "preview_game_camera.png"))


def fit(folder):
    """Reads corners_px and height from prop.json and writes source_pitch, source_yaw, width_z, depth_x."""
    path = os.path.join(folder, "prop.json")
    cfg = json.load(open(path))
    corners = cfg.get("corners_px")
    if not corners:
        print("prop.json has no corners_px. Run `grid`, read the four corner pixels, and add them first.")
        return 1
    near = np.array(corners["near_bottom"], float)
    left = np.array(corners["left_bottom"], float) - near
    right = np.array(corners["right_bottom"], float) - near
    top = np.array(corners["near_top"], float) - near
    if left[0] >= 0 or right[0] <= 0 or left[1] > 0 or right[1] > 0 or top[1] >= 0:
        print("corners_px look wrong: left_bottom must be left of and not below near_bottom, right_bottom right of "
              "and not below it, and near_top straight above it. Check the picture with `grid` again.")
        return 1
    # Orthographic box: left edge rises by sin(p)/tan(y) per pixel across, right edge by sin(p)*tan(y).
    rise_left, rise_right = -left[1] / -left[0], -right[1] / right[0]
    sin_pitch = math.sqrt(rise_left * rise_right)
    if sin_pitch >= 1:
        print("corners_px give an impossible camera (looking straight down or worse). Check the corners.")
        return 1
    pitch = math.degrees(math.asin(sin_pitch))
    yaw = math.degrees(math.atan(math.sqrt(rise_right / rise_left)))
    scale = -top[1] / (cfg["height"] * math.cos(math.radians(pitch)))
    width = math.hypot(*left) / (scale * math.hypot(math.sin(math.radians(yaw)), sin_pitch * math.cos(math.radians(yaw))))
    depth = math.hypot(*right) / (scale * math.hypot(math.cos(math.radians(yaw)), sin_pitch * math.sin(math.radians(yaw))))
    cfg.update(source_pitch=round(pitch, 1), source_yaw=round(yaw, 1), width_z=round(width, 2), depth_x=round(depth, 2))
    json.dump(cfg, open(path, "w"), indent=2, ensure_ascii=False)
    print(f"pitch {pitch:.1f}  yaw {yaw:.1f}  width_z {width:.2f}  depth_x {depth:.2f}  (height {cfg['height']}) -> saved to prop.json")
    if abs(math.hypot(*top) - abs(top[1])) > 3:
        print("warning: near_top is not straight above near_bottom; the picture may not be a straight-on box drawing.")
    return 0


def grid(folder):
    """Saves the source picture with a labelled grid every 50 px, so corner pixels can be read off it."""
    cfg = json.load(open(os.path.join(folder, "prop.json")))
    picture = Image.open(os.path.join(folder, cfg["source"])).convert("RGBA")
    zoom = max(1, 1600 // max(picture.size))
    canvas = Image.new("RGBA", picture.size, (255, 255, 255, 255))
    canvas.alpha_composite(picture)
    canvas = canvas.resize((picture.size[0] * zoom, picture.size[1] * zoom), Image.Resampling.NEAREST)
    draw = ImageDraw.Draw(canvas)
    for step, colour in ((10, (255, 0, 0, 40)), (50, (255, 0, 0, 160))):
        for x in range(0, picture.size[0], step):
            draw.line([(x * zoom, 0), (x * zoom, canvas.size[1])], fill=colour)
        for y in range(0, picture.size[1], step):
            draw.line([(0, y * zoom), (canvas.size[0], y * zoom)], fill=colour)
    for x in range(0, picture.size[0], 50):
        draw.text((x * zoom + 2, 2), str(x), fill=(200, 0, 0, 255))
    for y in range(50, picture.size[1], 50):
        draw.text((2, y * zoom + 2), str(y), fill=(200, 0, 0, 255))
    path = os.path.join(folder, "grid.png")
    canvas.save(path)
    print("wrote", path, f"- labels are source pixels (picture shown x{zoom}); thin lines every 10 px, thick every 50 px")
    return 0


# ---------------------------------------------------------------------------------------------
# Unity output. Templates in tools/templates/ are the files of the first prop (Wardrobe_Test).

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
TEMPLATES = os.path.join(os.path.dirname(__file__), "templates")
TEXTURE_DIR = "Assets/_Project/Art/Sprites/ThreeQuarter/Props"
MATERIAL_DIR = "Assets/_Project/Art/Materials"
PREFAB_DIR = "Assets/_Project/Prefabs/Props"


def template(name):
    return open(os.path.join(TEMPLATES, name), encoding="utf-8").read()


def guid_of(meta_path):
    """Keeps the GUID of an asset that already exists, so re-running never breaks references."""
    if os.path.exists(meta_path):
        for line in open(meta_path, encoding="utf-8"):
            if line.startswith("guid:"):
                return line.split()[1]
    return uuid.uuid4().hex


def write(relative_path, text):
    full = os.path.join(REPO, relative_path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    open(full, "w", encoding="utf-8", newline="\n").write(text)
    print("wrote", relative_path)


def set_value(block, key, value):
    """Replaces the first `key: ...` line in a YAML block, keeping its indentation."""
    lines = block.split("\n")
    for i, line in enumerate(lines):
        if line.strip().startswith(key + ":"):
            lines[i] = line[:len(line) - len(line.lstrip())] + f"{key}: {value}"
            return "\n".join(lines)
    raise KeyError(key)


def vec(values):
    return "{x: %s, y: %s, z: %s}" % tuple(f"{v:.4f}" for v in values)


def unity(folder):
    import shutil
    cfg = json.load(open(os.path.join(folder, "prop.json")))
    name = cfg["output_name"]
    out_dir = os.path.join(folder, "out")
    setup_path = os.path.join(out_dir, "unity_setup.json")
    if not os.path.exists(setup_path):
        print("Run `bake` first.")
        return 1
    setup = json.load(open(setup_path))
    D, H, W = setup["box_size_xyz"]

    # Textures and materials, one per face.
    material_guids = {}
    for face in ("Front", "Side", "Top"):
        png = f"{TEXTURE_DIR}/{name}_{face}.png"
        texture_guid = guid_of(os.path.join(REPO, png + ".meta"))
        shutil.copyfile(os.path.join(out_dir, f"{name}_{face}.png"), os.path.join(REPO, png))
        print("wrote", png)
        write(png + ".meta", set_value(template("Face.png.meta.txt"), "guid", texture_guid))
        mat = f"{MATERIAL_DIR}/{name}_{face}.mat"
        material_guids[face] = guid_of(os.path.join(REPO, mat + ".meta"))
        text = template("Face.mat.txt").replace("5c930639f71f4c66ae49e5d96d4bd625", texture_guid)
        write(mat, set_value(text, "m_Name", f"{name}_{face}"))
        write(mat + ".meta", set_value(template("Face.mat.meta.txt"), "guid", material_guids[face]))

    # Upright parts are sprites.
    part_guids = {}
    for part, info in setup["upright_parts"].items():
        png = f"{TEXTURE_DIR}/{name}_{part}.png"
        part_guids[part] = guid_of(os.path.join(REPO, png + ".meta"))
        shutil.copyfile(os.path.join(out_dir, f"{name}_{part}.png"), os.path.join(REPO, png))
        print("wrote", png)
        meta = set_value(template("Part.png.meta.txt"), "guid", part_guids[part])
        meta = set_value(meta, "spritePivot", "{x: %s, y: %s}" % tuple(info["pivot_normalized"]))
        meta = set_value(meta, "spritePixelsToUnits", info["ppu"])
        sprite_id = uuid.uuid4().hex[:16] + "0800000000000000"
        if os.path.exists(os.path.join(REPO, png + ".meta")):   # keep the old id so the prefab link survives
            for line in open(os.path.join(REPO, png + ".meta"), encoding="utf-8"):
                if line.strip().startswith("spriteID:") and line.split(":")[1].strip():
                    sprite_id = line.split(":")[1].strip()
        meta = set_value(meta, "spriteID", sprite_id)
        write(png + ".meta", meta)

    # Prefab, built from the template's blocks.
    blocks = {}
    for chunk in template("Prop.prefab.txt").split("--- !u!")[1:]:
        file_id = int(chunk.split("&")[1].split()[0])
        blocks[file_id] = "--- !u!" + chunk.rstrip("\n")
    header = template("Prop.prefab.txt").split("--- !u!")[0].rstrip("\n")

    def renumber(block, mapping):
        for old, new in mapping.items():
            block = block.replace(f"&{old}\n", f"&{new}\n").replace(f"{{fileID: {old}}}", f"{{fileID: {new}}}")
        return block

    children, body = [], []
    for index, (part, info) in enumerate(setup["upright_parts"].items()):
        base = 1111 if index == 0 else 2001 + index * 10   # the first part keeps the template ids
        mapping = {1111: base, 1112: base + 1, 1113: base + 2, 1114: base + 3}
        game_object, transform, renderer, billboard = (renumber(blocks[i], mapping) for i in (1111, 1112, 1113, 1114))
        game_object = set_value(game_object, "m_Name", part)
        transform = set_value(transform, "m_LocalPosition", vec(info["position"]))
        renderer = set_value(renderer, "m_Sprite", "{fileID: 21300000, guid: %s, type: 3}" % part_guids[part])
        w, h = info["size_px"]
        renderer = set_value(renderer, "m_Size", "{x: %.6f, y: %.6f}" % (w / info["ppu"], h / info["ppu"]))
        body += [game_object, transform, renderer, billboard]
        children.append(base + 1)

    shadow = set_value(blocks[1122], "m_LocalPosition", vec([0, H / 2, 0]))
    body += [blocks[1121], set_value(shadow, "m_LocalScale", vec([D, H, W]))]
    body += [blocks[1123], blocks[1124], blocks[1131]]
    body += [set_value(blocks[1132], "m_LocalPosition", vec([0, H / 2, 0])), set_value(blocks[1133], "m_Size", vec([D, H, W]))]
    for face, first in (("Front", 1141), ("Side", 1151), ("Top", 1161)):
        quad = setup["quads"]["Face_" + face]
        transform = set_value(blocks[first + 1], "m_LocalPosition", vec(quad["position"]))
        transform = set_value(transform, "m_LocalScale", "{x: %.4f, y: %.4f, z: 1}" % tuple(quad["scale"][:2]))
        renderer = blocks[first + 3].replace("f2b5d7147ece4446a852a82f8b4b811e", material_guids[face]) \
            .replace("0754515ceeb84633ba7150f61e9e2bd7", material_guids[face]).replace("dcf3a90845f247a28d83510f50f09c86", material_guids[face])
        body += [blocks[first], transform, blocks[first + 2], renderer]
    children += [1122, 1132, 1142, 1152, 1162]

    root = set_value(blocks[1101], "m_Name", name)
    root_transform = blocks[1102].split("  m_Children:")[0] + "  m_Children:\n" + \
        "".join(f"  - {{fileID: {c}}}\n" for c in children) + "  m_Father: {fileID: 0}\n" + \
        "  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}"
    prefab = f"{PREFAB_DIR}/{name}.prefab"
    prefab_guid = guid_of(os.path.join(REPO, prefab + ".meta"))
    write(prefab, "\n".join([header, root, root_transform] + body) + "\n")
    write(prefab + ".meta", "fileFormatVersion: 2\nguid: %s\nPrefabImporter:\n  externalObjects: {}\n  userData: \n"
          "  assetBundleName: \n  assetBundleVariant: \n" % prefab_guid)
    print(f"done: drag {prefab} into a ThreeQuarter area, base on the floor.")
    return 0


def main():
    if len(sys.argv) != 3 or sys.argv[1] not in ("grid", "fit", "check", "bake", "unity"):
        print("usage: prop_faces.py grid|fit|check|bake|unity <prop folder>")
        return 1
    command, folder = sys.argv[1], sys.argv[2]
    if command in ("grid", "fit", "unity"):
        return {"grid": grid, "fit": fit, "unity": unity}[command](folder)
    prop = Prop(folder)
    if command == "check":
        prop.check()
    else:
        prop.bake()
    return 0


if __name__ == "__main__":
    sys.exit(main())
