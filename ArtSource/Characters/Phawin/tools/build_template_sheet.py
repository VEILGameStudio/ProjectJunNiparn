# build_template_sheet.py
# Turns an AI-drawn walk sheet (one figure per cell, soft haze around it) into a sprite sheet laid
# out like the Player_Mannequin template in Assets/_Project/Art/Sprites/<preset>/Placeholder/:
#   - one row per facing, rows in the template's order, a facing with no art is an empty row
#   - every frame's lowest pixel on one ground line, frames lined up on the head and chest
#   - stretched vertically by 1 / cos(pitch) as the LAST step (CLAUDE.md section 7), width unchanged
# It also writes a JSON with the cell size, the feet pivot and a suggested PPU, GIF previews per
# facing, and a preview cut at alpha 0.5 (what alpha clipping shows in the game).
#
# usage:
#   python3 build_template_sheet.py <src.png> <src_rows> <src_cols> <src_row_names> \
#       <out_dir> <prefix> <stretch> <template_rows> <template_world_height> <fps> \
#       [haze_alpha] [facings_are] [row_scale]
#   src_row_names : comma list, one name per source row; give ONE name to read the whole
#                   source sheet row by row as a single facing
#   template_rows : comma list of facings in the template's row order
#   haze_alpha    : optional, alpha at or below this is background haze (default 8)
#   facings_are   : optional, 'rows' (default) or 'cols' - 'cols' means one source COLUMN per facing,
#                   frames running down; src_row_names then names the columns left to right
#   row_scale     : optional, 'normalize' scales every source row to the same median figure height,
#                   for sheets where the generator drew each row a little smaller than the last
# requires: Python 3, numpy, Pillow, opencv-python (pip install numpy pillow opencv-python-headless)
import sys, os, json
import numpy as np
from PIL import Image
import cv2

(src, src_rows, src_cols, src_names, out, prefix, stretch,
 template_rows, world_height, fps) = sys.argv[1:11]
src_rows, src_cols, stretch = int(src_rows), int(src_cols), float(stretch)
world_height, fps = float(world_height), int(fps)
haze_alpha = int(sys.argv[11]) if len(sys.argv) > 11 else 8
facings_are = sys.argv[12] if len(sys.argv) > 12 else 'rows'
row_scale = sys.argv[13] if len(sys.argv) > 13 else 'keep'
assert facings_are in ('rows', 'cols'), f"facings_are must be 'rows' or 'cols', got '{facings_are}'"
assert row_scale in ('keep', 'normalize'), f"row_scale must be 'keep' or 'normalize', got '{row_scale}'"
src_names = src_names.split(',')
template_rows = template_rows.split(',')
if len(src_names) > 1:
    expected = src_cols if (len(sys.argv) > 12 and sys.argv[12] == 'cols') else src_rows
    assert len(src_names) == expected, f'expected {expected} facing names, got {len(src_names)}'
os.makedirs(out, exist_ok=True)

# 1. Find the figures. Haze at or below haze_alpha is background, not character.
img = np.array(Image.open(src).convert('RGBA'))
img[img[..., 3] <= haze_alpha] = 0
mask = (img[..., 3] > 0).astype(np.uint8)
count, labels, stats, centres = cv2.connectedComponentsWithStats(mask, 8)
figures = [i for i in range(1, count) if stats[i, 4] > 2000]
assert len(figures) == src_rows * src_cols, f'expected {src_rows * src_cols} figures, found {len(figures)}'
row_height = img.shape[0] / src_rows
figures.sort(key=lambda i: (int(centres[i][1] // row_height), centres[i][0]))

# Small specks belong to the nearest figure; far-away dust is dropped.
owner = np.zeros(count, int)
for i in range(1, count):
    if i in figures:
        owner[i] = i
        continue
    cx, cy = centres[i]
    best, best_distance = 0, 1e9
    for f in figures:
        x, y, w, h, _ = stats[f]
        distance = max(x - cx, 0, cx - (x + w)) + max(y - cy, 0, cy - (y + h))
        if distance < best_distance:
            best, best_distance = f, distance
    owner[i] = best if best_distance < 12 else 0

# Optional: one uniform scale per source row so every row has the same median figure height.
scales = [1.0] * len(figures)
if row_scale == 'normalize':
    row_medians = [float(np.median([stats[f, 3] for f in figures[r * src_cols:(r + 1) * src_cols]]))
                   for r in range(src_rows)]
    target = float(np.median(row_medians))
    for n in range(len(figures)):
        scales[n] = target / row_medians[n // src_cols]
    print('row medians', row_medians, 'target', target)

# 2. Cut each figure out, scale it if asked, then stretch it vertically (width unchanged).
frames = []
for n, f in enumerate(figures):
    selected = np.isin(labels, np.nonzero(owner == f)[0])
    ys, xs = np.nonzero(selected)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    crop = img[y0:y1, x0:x1].copy()
    crop[~selected[y0:y1, x0:x1]] = 0
    piece = Image.fromarray(crop)
    if scales[n] != 1.0:
        piece = piece.resize((int(round(piece.width * scales[n])), int(round(piece.height * scales[n]))),
                             Image.LANCZOS)
    piece = piece.resize((piece.width, int(round(piece.height * stretch))), Image.LANCZOS)
    crop = np.array(piece)
    # Horizontal anchor: alpha-weighted centre of head + chest (top 40%), steadier than the bbox.
    top = crop[: int(crop.shape[0] * 0.4), :, 3].astype(float)
    anchor = (top.sum(0) * np.arange(top.shape[1])).sum() / top.sum()
    frames.append((crop, anchor))

# 3. One cell size for every frame, feet on one ground line.
pad = 8
left = max(a for c, a in frames)
right = max(c.shape[1] - a for c, a in frames)
tallest = max(c.shape[0] for c, a in frames)
cell_w = int(np.ceil(left + right)) + 2 * pad
cell_w += cell_w % 2
cell_h = tallest + 2 * pad
anchor_x = pad + int(np.ceil(left))
ground_y = cell_h - pad
cells = []
for crop, anchor in frames:
    cell = Image.new('RGBA', (cell_w, cell_h), (0, 0, 0, 0))
    cell.alpha_composite(Image.fromarray(crop), (int(round(anchor_x - anchor)), ground_y - crop.shape[0]))
    cells.append(cell)

# Group frames by facing.
by_facing = {}
if len(src_names) == 1:
    by_facing[src_names[0]] = cells
elif facings_are == 'cols':
    for c, name in enumerate(src_names):
        by_facing[name] = [cells[r * src_cols + c] for r in range(src_rows)]
else:
    for r, name in enumerate(src_names):
        by_facing[name] = cells[r * src_cols:(r + 1) * src_cols]
columns = max(len(v) for v in by_facing.values())

# 4. Sheet in the template's row order.
sheet = Image.new('RGBA', (cell_w * columns, cell_h * len(template_rows)), (0, 0, 0, 0))
missing = []
for r, name in enumerate(template_rows):
    row_cells = by_facing.get(name)
    if row_cells is None:
        missing.append(name)
        continue
    for c, cell in enumerate(row_cells):
        sheet.alpha_composite(cell, (c * cell_w, r * cell_h))
    gif = []
    for cell in row_cells:
        bg = Image.new('RGBA', (cell_w, cell_h), (205, 200, 190, 255))
        bg.alpha_composite(cell)
        gif.append(bg.convert('RGB').quantize(255))
    gif[0].save(f'{out}/{prefix}_Walk_{name}.gif', save_all=True, append_images=gif[1:],
                duration=int(1000 / fps), loop=0, disposal=2)
sheet.save(f'{out}/{prefix}.png')

# Preview of what alpha clipping at 0.5 leaves, on a grey floor colour.
clip = np.array(sheet)
keep = clip[..., 3] >= 128
preview = np.zeros_like(clip)
preview[...] = (120, 125, 115, 255)
preview[keep] = clip[keep]
preview[keep, 3] = 255
Image.fromarray(preview).save(f'{out}/{prefix}_alphaclip_preview.png')

# 5. Numbers for the import step.
heights = sorted(c.shape[0] for c, a in frames)
median_height = heights[len(heights) // 2]
meta = {
    'source': os.path.basename(src),
    'haze_alpha': haze_alpha,
    'row_scale': row_scale,
    'stretch': stretch,
    'stretched': True,
    'cell': [cell_w, cell_h],
    'columns': columns,
    'rows_in_order': template_rows,
    'missing_rows': missing,
    'frames_per_facing': {k: len(v) for k, v in by_facing.items()},
    'fps': fps,
    'pivot_px_from_bottom_left': [anchor_x, pad],
    'pivot_normalized': [round(anchor_x / cell_w, 4), round(pad / cell_h, 4)],
    'figure_height_px_stretched': {'min': heights[0], 'median': median_height, 'max': heights[-1]},
    'template_world_height': world_height,
    'suggested_ppu': round(median_height / world_height, 1),
}
with open(f'{out}/{prefix}.json', 'w') as handle:
    json.dump(meta, handle, indent=2)
print(json.dumps(meta))
