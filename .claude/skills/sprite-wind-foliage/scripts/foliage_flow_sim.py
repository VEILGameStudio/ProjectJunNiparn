#!/usr/bin/env python3
"""
foliage_flow_sim.py
Reproduces the reference clip's tree wind in Python, so the idea can be tried and tuned
without opening Unity:

  A "slime":   one leaf layer, wind + full player disturbance       (what the clip fixed)
  B "layered": trunk static, base leaves wind + full disturbance,
               player drawn, then a holey duplicate leaf layer on top
               with the SAME wind but the disturbance turned down     (the clip's fix)

Every offset is rounded to whole art pixels and sampled nearest, like pixel art should be.

Usage:
  python3 -I foliage_flow_sim.py <out_dir> [overlay_disturb_scale=0.25] [wind_px=1.5] [disturb_px=6]
Writes:
  <out_dir>/foliage-sim-sheet.png   6 frames of A (top two rows) above the same 6 frames of B
  <out_dir>/foliage-sim.gif         A and B side by side, animated
  <out_dir>/foliage-layers.png      trunk / leaves / overlay-with-holes / full tree
"""
import sys
import os
import numpy as np
from PIL import Image

W, H = 128, 160
SCALE = 4
FPS = 15
SECONDS = 4.0
BACKGROUND = np.array([128, 128, 132], np.uint8)
LEAF_SHADES = np.array([[22, 56, 30], [34, 88, 40], [52, 122, 48], [92, 170, 64]], np.uint8)
TRUNK_SHADES = np.array([[58, 36, 24], [92, 58, 34], [124, 84, 48]], np.uint8)
rng = np.random.default_rng(7)


def value_noise(width, height, cell, seed):
    # Smooth noise: random values on a coarse grid, bilinearly interpolated.
    r = np.random.default_rng(seed)
    gw, gh = width // cell + 3, height // cell + 3
    grid = r.random((gh, gw))
    ys, xs = np.mgrid[0:height, 0:width] / cell
    x0, y0 = xs.astype(int), ys.astype(int)
    fx, fy = xs - x0, ys - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = grid[y0, x0], grid[y0, x0 + 1]
    c, d = grid[y0 + 1, x0], grid[y0 + 1, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def build_tree():
    # Returns RGBA arrays for trunk, leaves, and the holey overlay.
    ys, xs = np.mgrid[0:H, 0:W]
    trunk = np.zeros((H, W, 4), np.uint8)
    trunk_mask = (np.abs(xs - 64 - (ys - 150) * 0.04) < 7 + (ys - 90) * 0.05) & (ys > 88) & (ys < 156)
    for branch in [(-1, 0.9), (1, 0.8)]:
        side, slope = branch
        line_x = 64 + side * (104 - ys) * slope
        trunk_mask |= (np.abs(xs - line_x) < 3) & (ys > 78) & (ys < 104)
    shade = np.where(xs < 61, 0, np.where(xs < 67, 1, 2))
    trunk[trunk_mask, :3] = TRUNK_SHADES[shade[trunk_mask]]
    trunk[trunk_mask, 3] = 255

    clumps = []
    for _ in range(26):
        angle = rng.uniform(0, 2 * np.pi)
        radius = rng.uniform(0, 1) ** 0.6
        cx = 64 + np.cos(angle) * radius * 44
        cy = 54 + np.sin(angle) * radius * 34
        clumps.append((cx, cy, rng.uniform(10, 16)))
    clumps.sort(key=lambda c: c[1], reverse=True)  # back (low) clumps first, top clumps paint over
    leaves = np.zeros((H, W, 4), np.uint8)
    lit = np.zeros((H, W), np.int32)
    jitter = value_noise(W, H, 3, 11)
    for cx, cy, r in clumps:
        dx, dy = xs - cx, ys - cy
        dist = np.sqrt(dx * dx + dy * dy) / r
        mask = dist < 1.0 + (jitter - 0.5) * 0.35
        # Light from the upper left: brighter on that side of each clump, dark rim at the bottom.
        light = (-dx * 0.5 - dy * 0.7) / r * 0.45 + 0.62 - dist * 0.2 + (jitter - 0.5) * 0.5
        level = np.clip((light * 4).astype(int), 0, 3)
        level[(dist > 0.88) & (dy > 0)] = 0
        leaves[mask, :3] = LEAF_SHADES[level[mask]]
        leaves[mask, 3] = 255
        lit[mask] = level[mask]

    # Overlay = the same leaves with holes: drop the darkest shade (shadow pockets between clumps)
    # and a few random small pockets so it never reads as a solid second skin.
    pockets = value_noise(W, H, 4, 23) < 0.12
    keep = (leaves[..., 3] > 0) & (lit >= 1) & ~pockets
    overlay = np.zeros_like(leaves)
    overlay[keep] = leaves[keep]
    coverage = keep.sum() / max(1, (leaves[..., 3] > 0).sum())
    print(f"overlay keeps {coverage:.0%} of leaf pixels (the clip's overlay keeps about 68%)")
    return trunk, leaves, overlay


def sample_shifted(layer, offset_x, offset_y):
    # Nearest sampling with whole-pixel offsets: each output pixel reads layer[y - oy, x - ox].
    ys, xs = np.mgrid[0:H, 0:W]
    sx = np.clip(xs - np.rint(offset_x).astype(int), 0, W - 1)
    sy = np.clip(ys - np.rint(offset_y).astype(int), 0, H - 1)
    return layer[sy, sx]


def over(base, top):
    out = base.copy()
    m = top[..., 3] > 127  # alpha clip, never blend
    out[m] = top[m]
    return out


def player_sprite():
    p = np.zeros((20, 10, 4), np.uint8)
    p[0:8, 1:9] = [120, 40, 150, 255]   # hair
    p[8:15, 2:8] = [70, 60, 60, 255]    # body
    p[15:20, 2:4] = [40, 30, 30, 255]
    p[15:20, 6:8] = [40, 30, 30, 255]
    return p


def stamp(image, sprite, x, y):
    h, w = sprite.shape[:2]
    x, y = int(round(x)), int(round(y))
    out = image.copy()
    for j in range(h):
        for i in range(w):
            if 0 <= y + j < H and 0 <= x + i < W and sprite[j, i, 3] > 0:
                out[y + j, x + i] = sprite[j, i]
    return out


def flatten(rgba):
    rgb = np.broadcast_to(BACKGROUND, (H, W, 3)).copy()
    m = rgba[..., 3] > 127
    rgb[m] = rgba[m, :3]
    return rgb


def main():
    out_dir = sys.argv[1]
    overlay_scale = float(sys.argv[2]) if len(sys.argv) > 2 else 0.25
    wind_px = float(sys.argv[3]) if len(sys.argv) > 3 else 1.5
    disturb_px = float(sys.argv[4]) if len(sys.argv) > 4 else 6.0
    os.makedirs(out_dir, exist_ok=True)
    trunk, leaves, overlay = build_tree()

    # Flow noise much larger than the tree, scrolled sideways: R = x push, G = y push, 0.5 = still.
    big_r = value_noise(W * 3, H, 40, 1) * 0.7 + value_noise(W * 3, H, 12, 2) * 0.3
    big_g = value_noise(W * 3, H, 40, 3) * 0.7 + value_noise(W * 3, H, 12, 4) * 0.3
    hero = player_sprite()

    # Disturbance texture: neutral 0, the player stamps a soft push that fades over time (a trail).
    disturb_x = np.zeros((H, W))
    disturb_y = np.zeros((H, W))
    ys, xs = np.mgrid[0:H, 0:W]
    frames_a, frames_b = [], []
    frame_count = int(SECONDS * FPS)
    for f in range(frame_count):
        t = f / FPS
        scroll = int(t * 9) % (W * 2)
        wind_x = (big_r[:, scroll:scroll + W] - 0.5) * 2 * wind_px
        wind_y = (big_g[:, scroll:scroll + W] - 0.5) * 2 * wind_px * 0.5

        # The player crosses the canopy left to right, rising then falling (a jump through it).
        px = -12 + (W + 24) * t / SECONDS
        py = 60 + 22 * np.cos(t / SECONDS * 2 * np.pi)
        vx = (W + 24) / SECONDS
        cx, cy = px + 5, py + 10
        dx, dy = xs - cx, ys - cy
        dist = np.sqrt(dx * dx + dy * dy)
        falloff = np.clip(1 - dist / 16.0, 0, 1) ** 2
        push_x = (dx / (dist + 1e-3) * 0.6 + np.sign(vx) * 0.4) * falloff
        push_y = (dy / (dist + 1e-3) * 0.6) * falloff
        disturb_x = disturb_x * 0.86 + push_x * 0.3
        disturb_y = disturb_y * 0.86 + push_y * 0.3
        dist_x = np.clip(disturb_x, -1, 1) * disturb_px
        dist_y = np.clip(disturb_y, -1, 1) * disturb_px

        # A: one layer, everything smooth.
        a = over(trunk, sample_shifted(leaves, wind_x + dist_x, wind_y + dist_y))
        a = stamp(a, hero, px, py)
        frames_a.append(flatten(a))

        # B: trunk still, base leaves full push, player, overlay with the same wind and a dulled push.
        b = over(trunk, sample_shifted(leaves, wind_x + dist_x, wind_y + dist_y))
        b = stamp(b, hero, px, py)
        b = over(b, sample_shifted(overlay, wind_x + dist_x * overlay_scale, wind_y + dist_y * overlay_scale))
        frames_b.append(flatten(b))

    crop_top, crop_h = 10, 100  # the canopy only, so the leaf motion is big enough to see

    def big(rgb):
        part = rgb[crop_top:crop_top + crop_h]
        return Image.fromarray(part).resize((W * 3, crop_h * 3), Image.NEAREST)

    picks = [int(frame_count * k / 7) for k in range(1, 7)]
    sheet = Image.new("RGB", (3 * W * 3 + 8, 4 * crop_h * 3 + 24), (0, 0, 0))
    for i, f in enumerate(picks):
        col, row = i % 3, i // 3
        sheet.paste(big(frames_a[f]), (col * (W * 3 + 4), row * (crop_h * 3 + 4)))
        sheet.paste(big(frames_b[f]), (col * (W * 3 + 4), (row + 2) * (crop_h * 3 + 4) + 12))
    sheet.save(os.path.join(out_dir, "foliage-sim-sheet.png"))

    gif = []
    for fa, fb in zip(frames_a, frames_b):
        pair = np.concatenate([fa, np.zeros((H, 4, 3), np.uint8), fb], axis=1)
        gif.append(Image.fromarray(pair).resize((pair.shape[1] * 3, H * 3), Image.NEAREST))
    gif[0].save(os.path.join(out_dir, "foliage-sim.gif"), save_all=True, append_images=gif[1:],
                duration=int(1000 / FPS), loop=0)

    layers = [flatten(trunk), flatten(leaves), flatten(overlay), flatten(over(over(trunk, leaves), overlay))]
    strip = np.concatenate([np.pad(l, ((0, 0), (0, 4), (0, 0))) for l in layers], axis=1)
    Image.fromarray(strip).resize((strip.shape[1] * 2, H * 2), Image.NEAREST).save(
        os.path.join(out_dir, "foliage-layers.png"))
    print("wrote", out_dir)


if __name__ == "__main__":
    main()
