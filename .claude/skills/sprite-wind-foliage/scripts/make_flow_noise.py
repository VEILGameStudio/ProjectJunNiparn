#!/usr/bin/env python3
"""
make_flow_noise.py
Makes a tileable RG flow-noise texture for foliage wind: R = sideways push, G = up/down push,
0.5 = no movement (the "piss yellow" neutral from the reference clip). B is 0.

Usage:
  python3 -I make_flow_noise.py <out.png> [size=256] [big_cells=4] [bias_x=0.0] [seed=1]

  big_cells  how many large gusts across the texture (fewer = bigger, smoother gusts)
  bias_x     constant lean added to R, e.g. 0.1 = leaves lean right on average (the clip leans right)

Import in Unity: sRGB OFF, Compression None, Wrap Repeat, Filter Bilinear, no mipmaps.
"""
import sys
import numpy as np
from PIL import Image


def tileable_value_noise(size, cells, rng):
    # Random values on a grid that wraps, interpolated with smoothstep, so the edges tile.
    grid = rng.random((cells, cells))
    coords = np.arange(size) * cells / size
    i0 = coords.astype(int)
    f = coords - i0
    f = f * f * (3 - 2 * f)
    i1 = (i0 + 1) % cells
    fy, fx = np.meshgrid(f, f, indexing="ij")
    y0, x0 = np.meshgrid(i0, i0, indexing="ij")
    y1, x1 = np.meshgrid(i1, i1, indexing="ij")
    top = grid[y0, x0] * (1 - fx) + grid[y0, x1] * fx
    bottom = grid[y1, x0] * (1 - fx) + grid[y1, x1] * fx
    return top * (1 - fy) + bottom * fy


def channel(size, big_cells, rng):
    big = tileable_value_noise(size, big_cells, rng)
    small = tileable_value_noise(size, big_cells * 4, rng)
    value = big * 0.7 + small * 0.3
    # Re-centre on 0.5 and stretch to use most of the range.
    value = (value - value.mean()) / (np.abs(value - value.mean()).max() + 1e-6) * 0.45 + 0.5
    return value


def main():
    out = sys.argv[1]
    size = int(sys.argv[2]) if len(sys.argv) > 2 else 256
    big_cells = int(sys.argv[3]) if len(sys.argv) > 3 else 4
    bias_x = float(sys.argv[4]) if len(sys.argv) > 4 else 0.0
    seed = int(sys.argv[5]) if len(sys.argv) > 5 else 1
    rng = np.random.default_rng(seed)
    red = np.clip(channel(size, big_cells, rng) + bias_x, 0, 1)
    green = channel(size, big_cells, rng)
    rgb = np.stack([red, green, np.zeros_like(red)], axis=-1)
    Image.fromarray((rgb * 255 + 0.5).astype(np.uint8)).save(out)
    print(f"wrote {out}  R mean {red.mean():.3f}  G mean {green.mean():.3f}  (0.5 = still)")


if __name__ == "__main__":
    main()
