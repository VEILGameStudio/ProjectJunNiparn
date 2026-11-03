#!/usr/bin/env python3
"""
split_tree_layers.py
Splits one painted tree sprite into the three layers the reference clip uses, plus a padded
normal map that all three share:

  <name>_Trunk.png          trunk and branches, plus a dark "backing" inside the canopy so a
                            gap that opens when the leaves move shows shadow, not sky
  <name>_Leaves.png         every leaf pixel (gets the wind and the full player push)
  <name>_LeavesOverlay.png  the same leaves with holes: darkest shade removed plus small pockets
                            (gets the same wind but a weak push)
  <name>_Layers_N.png       the tree's normal map on the same padded canvas

All four share one canvas: the original plus `pad` transparent pixels on every side, so leaves
can move without being cut off by the sprite's edge. The pivot moves up by `pad` pixels; the
printed pivot Y goes into each .meta (spritePivot y).

Usage:
  python3 -I split_tree_layers.py <tree.png> <tree_N.png> <out_dir> <name> [pad=32] [leaf_hue_min=48]
          [overlay_keep=0.65] [backing_erode=6] [mode=hue|all]

  mode=all   treats every pixel as leaves (for a plant whose stem is green, like a banana tree):
             no trunk layer content, the wind's height ramp keeps the base still instead.
"""
import os
import sys
import numpy as np
from PIL import Image, ImageFilter


def rgb_to_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    maxc = rgb.max(-1)
    minc = rgb.min(-1)
    delta = maxc - minc + 1e-6
    hue = np.where(maxc == r, ((g - b) / delta) % 6,
                   np.where(maxc == g, (b - r) / delta + 2, (r - g) / delta + 4)) * 60
    sat = np.where(maxc > 0, (maxc - minc) / (maxc + 1e-6), 0)
    return hue, sat, maxc


def mask_filter(mask, filter_name, size):
    image = Image.fromarray((mask * 255).astype(np.uint8))
    if filter_name == "min":
        image = image.filter(ImageFilter.MinFilter(size))
    elif filter_name == "max":
        image = image.filter(ImageFilter.MaxFilter(size))
    else:
        image = image.filter(ImageFilter.MedianFilter(size))
    return np.asarray(image) > 127


def smooth_noise(height, width, cell, seed):
    rng = np.random.default_rng(seed)
    grid = rng.random((height // cell + 2, width // cell + 2)).astype(np.float32)
    image = Image.fromarray((grid * 255).astype(np.uint8)).resize((width + cell, height + cell), Image.BICUBIC)
    return np.asarray(image)[:height, :width] / 255.0


def main():
    tree_path, normal_path, out_dir, name = sys.argv[1:5]
    pad = int(sys.argv[5]) if len(sys.argv) > 5 else 32
    leaf_hue_min = float(sys.argv[6]) if len(sys.argv) > 6 else 48
    overlay_keep = float(sys.argv[7]) if len(sys.argv) > 7 else 0.65
    backing_erode = int(sys.argv[8]) if len(sys.argv) > 8 else 6
    mode = sys.argv[9] if len(sys.argv) > 9 else "hue"
    os.makedirs(out_dir, exist_ok=True)

    tree = np.asarray(Image.open(tree_path).convert("RGBA")).astype(np.float32) / 255.0
    normal = np.asarray(Image.open(normal_path).convert("RGBA"))
    height, width = tree.shape[:2]
    solid = tree[..., 3] > 0.5
    hue, sat, value = rgb_to_hsv(tree[..., :3])

    # Leaves: green to yellow-green and not grey. Trunks and branches are brown (hue below ~45).
    if mode == "all":
        leaves = solid.copy()
    else:
        leaves = solid & (hue >= leaf_hue_min) & (hue <= 170) & (sat > 0.12)
        leaves = mask_filter(leaves, "median", 5) & solid
    trunk = solid & ~leaves

    # Overlay: drop the darkest leaf pixels (shadow pockets) and small random pockets.
    leaf_values = value[leaves]
    dark_cut = np.quantile(leaf_values, 0.25) if leaf_values.size else 0
    pockets = smooth_noise(height, width, 9, 5)
    keep = leaves & (value > dark_cut)
    # Raise the pocket threshold until about `overlay_keep` of the leaves are left.
    threshold = 0.0
    while threshold < 0.9 and leaves.sum() and (keep & (pockets > threshold)).sum() / leaves.sum() > overlay_keep:
        threshold += 0.02
    keep = keep & (pockets > threshold)
    keep = mask_filter(keep, "median", 3) & leaves

    # Backing: the canopy shrunk inward, filled with the darkest leaf colour, drawn on the trunk layer.
    backing = mask_filter(leaves, "min", backing_erode * 2 + 1) & ~trunk
    if mode == "all":
        # A plant with no separate trunk: a dark backing would show as a ghost silhouette.
        backing = np.zeros_like(leaves)
    dark_pixels = tree[leaves & (value <= dark_cut), :3]
    backing_colour = (dark_pixels.mean(0) * 0.8) if dark_pixels.size else np.array([0.1, 0.15, 0.1])

    def layer(mask, colour_override=None):
        out = np.zeros_like(tree)
        out[mask] = tree[mask]
        if colour_override is not None:
            colour_mask, colour = colour_override
            out[colour_mask, :3] = colour
            out[colour_mask, 3] = 1.0
        out[trunk & mask] = tree[trunk & mask]
        padded = np.zeros((height + 2 * pad, width + 2 * pad, 4), np.float32)
        padded[pad:pad + height, pad:pad + width] = out
        return Image.fromarray((padded * 255 + 0.5).astype(np.uint8), "RGBA")

    trunk_image = layer(trunk | backing, (backing, backing_colour))
    leaves_image = layer(leaves)
    overlay_image = layer(keep)
    normal_padded = np.zeros((height + 2 * pad, width + 2 * pad, 4), np.uint8)
    normal_padded[..., 0] = 128
    normal_padded[..., 1] = 128
    normal_padded[..., 2] = 255
    normal_padded[..., 3] = 255
    normal_padded[pad:pad + height, pad:pad + width] = normal

    trunk_image.save(os.path.join(out_dir, f"{name}_Trunk.png"))
    leaves_image.save(os.path.join(out_dir, f"{name}_Leaves.png"))
    overlay_image.save(os.path.join(out_dir, f"{name}_LeavesOverlay.png"))
    Image.fromarray(normal_padded, "RGBA").save(os.path.join(out_dir, f"{name}_Layers_N.png"))

    pivot_y = pad / (height + 2 * pad)
    print(f"{name}: canvas {width + 2 * pad}x{height + 2 * pad}, pivot y {pivot_y:.6f}, "
          f"leaves {leaves.sum() / max(1, solid.sum()):.0%} of tree, overlay keeps {keep.sum() / max(1, leaves.sum()):.0%} of leaves, "
          f"trunk {trunk.sum() / max(1, solid.sum()):.0%}")


if __name__ == "__main__":
    main()
