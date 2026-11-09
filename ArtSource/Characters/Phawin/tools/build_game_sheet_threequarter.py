# build_game_sheet_threequarter.py
# Builds the in-game ThreeQuarter sheet for Phawin from two AI-drawn sources:
#   - an idle sheet: one standing figure per facing in its first row (8 columns = facings)
#   - a walk sheet:  8 columns = facings, N rows = walk frames
# Layout is the same as Placeholder_Mannequin_ThreeQuarter.png: one row per facing in the template's
# order, column 0 = idle, the columns after it = walk frames.
# Every figure is scaled to one height, put on one ground line, lined up on the head and chest, and
# stretched vertically by 1 / cos(30) as the LAST step (CLAUDE.md section 7). Width is never stretched.
#
# usage (from the repo root):
#   python3 build_game_sheet_threequarter.py <idle.png> <idle_rows> <walk.png> <walk_rows> \
#       <source_column_names> <template_rows> <out_dir> <prefix> <stretch> <world_height> <fps> <haze_alpha>
#   source_column_names : comma list naming the 8 source columns left to right (same in both sources)
#   template_rows       : comma list of facings in the template's row order (top row first)
# requires: Python 3, numpy, Pillow, opencv-python (pip install numpy pillow opencv-python-headless)
import sys, os, json
import numpy as np
from PIL import Image
import cv2

if len(sys.argv) != 13:
    sys.exit('wrong number of arguments, see the usage comment at the top of this file')

(idle_path, idle_rows, walk_path, walk_rows, column_names, template_rows,
 out, prefix, stretch, world_height, fps, haze_alpha) = sys.argv[1:13]
idle_rows, walk_rows = int(idle_rows), int(walk_rows)
stretch, world_height, fps, haze_alpha = float(stretch), float(world_height), int(fps), int(haze_alpha)
column_names = column_names.split(',')
template_rows = template_rows.split(',')
assert len(column_names) == 8, f'expected 8 column names, got {len(column_names)}'
assert sorted(column_names) == sorted(template_rows), 'column names and template rows must hold the same facings'
os.makedirs(out, exist_ok=True)


# Cuts every figure out of a sheet. Returns a grid [row][column] of RGBA crops.
# Haze at or below haze_alpha is background. Small specks join the nearest figure; far dust is dropped.
def cut_figures(path, rows, columns):
    img = np.array(Image.open(path).convert('RGBA'))
    img[img[..., 3] <= haze_alpha] = 0
    mask = (img[..., 3] > 0).astype(np.uint8)
    count, labels, stats, centres = cv2.connectedComponentsWithStats(mask, 8)
    figures = [i for i in range(1, count) if stats[i, 4] > 2000]
    assert len(figures) == rows * columns, f'{path}: expected {rows * columns} figures, found {len(figures)}'
    row_height = img.shape[0] / rows
    figures.sort(key=lambda i: (int(centres[i][1] // row_height), centres[i][0]))

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

    grid = []
    for r in range(rows):
        grid_row = []
        for c in range(columns):
            f = figures[r * columns + c]
            selected = np.isin(labels, np.nonzero(owner == f)[0])
            ys, xs = np.nonzero(selected)
            y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
            crop = img[y0:y1, x0:x1].copy()
            crop[~selected[y0:y1, x0:x1]] = 0
            grid_row.append(crop)
        grid.append(grid_row)
    return grid


# Scales a crop uniformly, then stretches it vertically. Returns the crop and its head-chest anchor.
def scale_and_stretch(crop, scale):
    piece = Image.fromarray(crop)
    piece = piece.resize((int(round(piece.width * scale)), int(round(piece.height * scale))), Image.LANCZOS)
    piece = piece.resize((piece.width, int(round(piece.height * stretch))), Image.LANCZOS)
    result = np.array(piece)
    # Horizontal anchor: alpha-weighted centre of head + chest (top 40%), steadier than the bbox.
    top = result[: int(result.shape[0] * 0.4), :, 3].astype(float)
    anchor = (top.sum(0) * np.arange(top.shape[1])).sum() / top.sum()
    return result, anchor


idle_grid = cut_figures(idle_path, idle_rows, 8)
walk_grid = cut_figures(walk_path, walk_rows, 8)

# The walk source draws each frame row a little smaller than the one above it, so every walk row is
# scaled to the median row height. The idle figures are scaled to that same height.
walk_row_medians = [float(np.median([c.shape[0] for c in row])) for row in walk_grid]
target_height = float(np.median(walk_row_medians))
idle_median = float(np.median([c.shape[0] for c in idle_grid[0]]))
print('walk row medians', walk_row_medians, 'target', target_height, 'idle median', idle_median)

# frames[facing] = [idle, walk_0, walk_1, ...]
frames = {}
for c, name in enumerate(column_names):
    frames[name] = [scale_and_stretch(idle_grid[0][c], target_height / idle_median)]
    for r in range(walk_rows):
        frames[name].append(scale_and_stretch(walk_grid[r][c], target_height / walk_row_medians[r]))
all_frames = [frame for name in column_names for frame in frames[name]]

# One cell size for every frame, feet on one ground line.
pad = 8
left = max(a for c, a in all_frames)
right = max(c.shape[1] - a for c, a in all_frames)
tallest = max(c.shape[0] for c, a in all_frames)
cell_w = int(np.ceil(left + right)) + 2 * pad
cell_w += cell_w % 2
cell_h = tallest + 2 * pad
anchor_x = pad + int(np.ceil(left))
ground_y = cell_h - pad
columns = 1 + walk_rows


def to_cell(frame):
    crop, anchor = frame
    cell = Image.new('RGBA', (cell_w, cell_h), (0, 0, 0, 0))
    cell.alpha_composite(Image.fromarray(crop), (int(round(anchor_x - anchor)), ground_y - crop.shape[0]))
    return cell


def gif(cells, path):
    images = []
    for cell in cells:
        background = Image.new('RGBA', (cell_w, cell_h), (205, 200, 190, 255))
        background.alpha_composite(cell)
        images.append(background.convert('RGB').quantize(255))
    images[0].save(path, save_all=True, append_images=images[1:], duration=int(1000 / fps), loop=0, disposal=2)


sheet = Image.new('RGBA', (cell_w * columns, cell_h * len(template_rows)), (0, 0, 0, 0))
for r, name in enumerate(template_rows):
    cells = [to_cell(frame) for frame in frames[name]]
    for c, cell in enumerate(cells):
        sheet.alpha_composite(cell, (c * cell_w, r * cell_h))
    gif(cells[1:], f'{out}/{prefix}_Walk_{name}.gif')
sheet.save(f'{out}/{prefix}.png')

# Preview of what alpha clipping at 0.5 leaves, on a grey floor colour.
clip = np.array(sheet)
keep = clip[..., 3] >= 128
preview = np.zeros_like(clip)
preview[...] = (120, 125, 115, 255)
preview[keep] = clip[keep]
preview[keep, 3] = 255
Image.fromarray(preview).save(f'{out}/{prefix}_alphaclip_preview.png')

idle_heights = sorted(frames[name][0][0].shape[0] for name in column_names)
idle_median_stretched = idle_heights[len(idle_heights) // 2]
walk_heights = sorted(f[0].shape[0] for name in column_names for f in frames[name][1:])
meta = {
    'idle_source': os.path.basename(idle_path),
    'walk_source': os.path.basename(walk_path),
    'haze_alpha': haze_alpha,
    'stretch': stretch,
    'stretched': True,
    'cell': [cell_w, cell_h],
    'columns': columns,
    'column_0': 'idle',
    'walk_frames': walk_rows,
    'rows_in_order': template_rows,
    'fps': fps,
    'pivot_px_from_bottom_left': [anchor_x, pad],
    'pivot_normalized': [round(anchor_x / cell_w, 4), round(pad / cell_h, 4)],
    'idle_height_px_stretched': {'min': idle_heights[0], 'median': idle_median_stretched, 'max': idle_heights[-1]},
    'walk_height_px_stretched': {'min': walk_heights[0], 'max': walk_heights[-1]},
    'world_height': world_height,
    'ppu': round(idle_median_stretched / world_height, 1),
}
with open(f'{out}/{prefix}.json', 'w') as handle:
    json.dump(meta, handle, indent=2)
print(json.dumps(meta))
