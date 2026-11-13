# prop_faces.py
# Turns one painted picture of a box-shaped prop (wardrobe, cupboard, crate, bookshelf...)
# into textures that sit on the faces of a real 3D box, for ThreeQuarter rooms.
#
# Why: one upright sprite card cannot both stand on the floor and hide/show the player
# correctly for a wide solid prop. Painting the picture onto the box faces gives every
# pixel its real depth, and from the fixed ThreeQuarter camera it looks the same as the drawing.
#
# Usage:
#   python3 prop_faces.py check <prop folder>   # draws the fitted box over the source picture
#   python3 prop_faces.py bake  <prop folder>   # writes the face textures, upright parts, preview
#
# The prop folder holds the source picture and prop.json (see ../README.md for every field).
# Needs: Pillow, numpy, scipy.

import json
import math
import os
import sys

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
        """Scale and place the box so its width matches the body and its lowest corner the lowest pixel."""
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


def main():
    if len(sys.argv) != 3 or sys.argv[1] not in ("check", "bake"):
        print(__doc__ if __doc__ else "usage: prop_faces.py check|bake <prop folder>")
        return 1
    prop = Prop(sys.argv[2])
    if sys.argv[1] == "check":
        prop.check()
    else:
        prop.bake()
    return 0


if __name__ == "__main__":
    sys.exit(main())
