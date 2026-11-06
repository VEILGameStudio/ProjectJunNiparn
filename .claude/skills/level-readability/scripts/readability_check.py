#!/usr/bin/env python3
"""Readability check for a game screenshot.

Makes a 2x2 sheet (original / greyscale / squint / three value bands) with the
given boxes drawn on it, and reports how much each box stands out from the
ring of pixels around it.

Usage:
    python3 readability_check.py shot.png \
        --box player:900,600,120,260 --box lock:1400,380,160,200 \
        --out shot_readability.png

Box format: name:x,y,width,height in pixels from the top-left of the image.
A box named "player" is the main character and is expected to stand out most.

Needs Pillow and numpy.
"""
import argparse
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

LOW = 1.5
GOOD = 2.0
COLORS = [(255, 64, 64), (64, 200, 255), (255, 200, 0), (120, 255, 120), (255, 120, 255), (255, 150, 60)]


def parse_box(text):
    try:
        name, rest = text.split(":", 1)
        x, y, w, h = (int(v) for v in rest.split(","))
    except ValueError:
        raise argparse.ArgumentTypeError("box must be name:x,y,w,h, got %r" % text)
    if w <= 0 or h <= 0:
        raise argparse.ArgumentTypeError("box %r has no area" % text)
    return name, (x, y, w, h)


def relative_luminance(rgb):
    """sRGB (0-255) array -> linear relative luminance (0-1), as in WCAG."""
    c = rgb / 255.0
    lin = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * lin[..., 0] + 0.7152 * lin[..., 1] + 0.0722 * lin[..., 2]


def saturation(rgb):
    mx = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    return np.where(mx > 0, (mx - mn) / np.maximum(mx, 1), 0.0)


def contrast_ratio(a, b):
    hi, lo = max(a, b), min(a, b)
    return (hi + 0.05) / (lo + 0.05)


def measure(lum, sat, box, img_w, img_h):
    x, y, w, h = box
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(img_w, x + w), min(img_h, y + h)
    if x1 <= x0 or y1 <= y0:
        return None
    pad = max(8, int(0.5 * max(w, h)))
    rx0, ry0 = max(0, x0 - pad), max(0, y0 - pad)
    rx1, ry1 = min(img_w, x1 + pad), min(img_h, y1 + pad)
    inner = np.zeros(lum.shape, dtype=bool)
    inner[y0:y1, x0:x1] = True
    ring = np.zeros(lum.shape, dtype=bool)
    ring[ry0:ry1, rx0:rx1] = True
    ring &= ~inner
    if not ring.any():
        return None
    l_in = float(np.median(lum[inner]))
    l_out = float(np.median(lum[ring]))
    return {
        "lum_in": l_in,
        "lum_out": l_out,
        "ratio": contrast_ratio(l_in, l_out),
        "sat_in": float(np.median(sat[inner])),
        "sat_out": float(np.median(sat[ring])),
    }


def bands(grey):
    """Three value bands (dark / mid / light) by image terciles."""
    lo, hi = np.percentile(grey, [33.3, 66.7])
    out = np.full(grey.shape, 128, dtype=np.uint8)
    out[grey <= lo] = 40
    out[grey > hi] = 220
    return out


def draw_boxes(img, boxes, scale):
    draw = ImageDraw.Draw(img)
    for i, (name, (x, y, w, h)) in enumerate(boxes):
        color = COLORS[i % len(COLORS)]
        rect = [x * scale, y * scale, (x + w) * scale, (y + h) * scale]
        draw.rectangle(rect, outline=color, width=max(2, int(3 * scale)))
        draw.text((rect[0] + 3, rect[1] + 2), name, fill=color)
    return img


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("image")
    parser.add_argument("--box", action="append", type=parse_box, default=[], help="name:x,y,w,h (repeatable)")
    parser.add_argument("--out", help="output sheet path (default: <image>_readability.png)")
    parser.add_argument("--panel-width", type=int, default=960, help="width of each panel in the sheet")
    args = parser.parse_args()

    src = Image.open(args.image).convert("RGB")
    rgb = np.asarray(src, dtype=np.float64)
    lum = relative_luminance(rgb)
    sat = saturation(rgb)
    grey_u8 = (np.clip(lum, 0, 1) ** (1 / 2.2) * 255).astype(np.uint8)  # perceptual-ish lightness for display
    img_w, img_h = src.size

    # Report.
    print("Image: %s (%dx%d)" % (args.image, img_w, img_h))
    print("Mean lightness: %.2f   value spread (p5-p95): %.2f-%.2f" % (
        grey_u8.mean() / 255, np.percentile(grey_u8, 5) / 255, np.percentile(grey_u8, 95) / 255))
    results = []
    for name, box in args.box:
        m = measure(lum, sat, box, img_w, img_h)
        if m is None:
            print("%-14s outside the image or too large to measure" % name)
            continue
        verdict = "LOW" if m["ratio"] < LOW else ("ok" if m["ratio"] < GOOD else "good")
        lighter = "lighter" if m["lum_in"] > m["lum_out"] else "darker"
        sat_note = " more saturated" if m["sat_in"] > m["sat_out"] + 0.08 else ""
        print("%-14s contrast %.2f  %-4s  (%s than its surroundings%s)" % (name, m["ratio"], verdict, lighter, sat_note))
        results.append((name, m))
    if results:
        player = [m for n, m in results if n == "player"]
        best_name, best = max(results, key=lambda r: r[1]["ratio"])
        if player and best_name != "player" and best["ratio"] > player[0]["ratio"] * 1.15:
            print("WARN  '%s' stands out more than the player (%.2f vs %.2f)" % (best_name, best["ratio"], player[0]["ratio"]))
        lows = [n for n, m in results if m["ratio"] < LOW]
        if lows:
            print("WARN  low contrast, likely missed by players: %s" % ", ".join(lows))

    # Sheet.
    scale = args.panel_width / img_w
    pw, ph = args.panel_width, int(round(img_h * scale))
    original = src.resize((pw, ph), Image.LANCZOS)
    grey = Image.fromarray(grey_u8).convert("RGB").resize((pw, ph), Image.LANCZOS)
    small = Image.fromarray(grey_u8).resize((max(1, img_w // 16), max(1, img_h // 16)), Image.BILINEAR)
    squint = small.resize((pw, ph), Image.BILINEAR).filter(ImageFilter.GaussianBlur(2)).convert("RGB")
    banded = Image.fromarray(bands(grey_u8)).convert("RGB").resize((pw, ph), Image.NEAREST)

    sheet = Image.new("RGB", (pw * 2, ph * 2 + 40), (20, 20, 20))
    titles = ["original", "greyscale (value only)", "squint (what survives at a glance)", "three value bands"]
    for i, panel in enumerate([original, grey, squint, banded]):
        panel = draw_boxes(panel, args.box, scale)
        ox, oy = (i % 2) * pw, (i // 2) * (ph + 20)
        sheet.paste(panel, (ox, oy + 20))
        ImageDraw.Draw(sheet).text((ox + 6, oy + 4), titles[i], fill=(230, 230, 230))
    out = args.out or args.image.rsplit(".", 1)[0] + "_readability.png"
    sheet.save(out)
    print("Sheet: %s" % out)


if __name__ == "__main__":
    sys.exit(main())
