# build_game_sheet.py
# Builds one in-game sprite sheet for a character from AI-drawn sources:
#   - a walk sheet per facing: one PNG holding a grid of frames, read left to right, then the next row
#   - an idle sheet: one standing figure per facing in a row
# The result has one row per facing; column 0 = idle, the columns after it = walk frames.
# Every figure is scaled to one height, put on one ground line, lined up on the head and chest, and
# stretched vertically (x1.155 ThreeQuarter, x1.015 SideView) as the LAST step (CLAUDE.md section 7).
# Width is never stretched. A row can be a mirror of another facing's art ("mirror": true).
#
# usage (from anywhere):  python3 build_game_sheet.py <folder>/build.json
#   Paths inside build.json are relative to that folder, and the output is written into that folder.
#   See threequarter-game/build.json and sideview-game/build.json for the fields.
# requires: Python 3, numpy, Pillow, opencv-python (pip install numpy pillow opencv-python-headless)
import sys, os, json
import numpy as np
from PIL import Image
import cv2

if len(sys.argv) != 2:
    sys.exit('usage: python3 build_game_sheet.py <folder>/build.json')

config_path = os.path.abspath(sys.argv[1])
out = os.path.dirname(config_path)
with open(config_path) as handle:
    config = json.load(handle)

prefix = config['prefix']
stretch = float(config['stretch'])
world_height = float(config['world_height'])
target_height = float(config['target_height'])  # figure height in px BEFORE the stretch
fps = int(config['fps'])
haze_alpha = int(config['haze_alpha'])
walk_columns, walk_rows = config['walk']['grid']
rows = config['rows']  # [{"name": row name, "from": source facing, "mirror": bool}]
symmetric = bool(config.get('symmetric_cells', False))
PAD = 8


# Cuts the one figure out of an RGBA region. Haze at or below haze_alpha is background.
# Small specks close to the figure stay with it; far dust is dropped.
def cut_figure(region, where):
    region = region.copy()
    region[region[..., 3] <= haze_alpha] = 0
    mask = (region[..., 3] > 0).astype(np.uint8)
    count, labels, stats, centres = cv2.connectedComponentsWithStats(mask, 8)
    assert count > 1, f'{where}: nothing drawn here'
    figure = 1 + int(np.argmax(stats[1:, 4]))
    assert stats[figure, 4] > 2000, f'{where}: no figure found'
    fx, fy, fw, fh, _ = stats[figure]
    keep = [figure]
    for i in range(1, count):
        if i == figure:
            continue
        cx, cy = centres[i]
        distance = max(fx - cx, 0, cx - (fx + fw)) + max(fy - cy, 0, cy - (fy + fh))
        if distance < 12:
            keep.append(i)
    selected = np.isin(labels, keep)
    ys, xs = np.nonzero(selected)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    assert y0 > 0 and x0 > 0 and y1 < region.shape[0] and x1 < region.shape[1], f'{where}: the figure touches the edge of its cell'
    crop = region[y0:y1, x0:x1].copy()
    crop[~selected[y0:y1, x0:x1]] = 0
    return crop


# Splits a sheet into a grid and cuts the figure out of every cell, in reading order.
def cut_grid(path, columns, grid_rows, only_row=None):
    sheet = np.array(Image.open(path).convert('RGBA'))
    cell_h, cell_w = sheet.shape[0] / grid_rows, sheet.shape[1] / columns
    figures = []
    for r in range(grid_rows):
        if only_row is not None and r != only_row:
            continue
        for c in range(columns):
            region = sheet[int(round(r * cell_h)):int(round((r + 1) * cell_h)), int(round(c * cell_w)):int(round((c + 1) * cell_w))]
            figures.append(cut_figure(region, f'{os.path.basename(path)} row {r} column {c}'))
    return figures


# Scales a crop uniformly, stretches it vertically, and mirrors it if asked.
# Returns the crop and its head-chest anchor (x in pixels).
def finish(crop, scale, mirror):
    piece = Image.fromarray(crop)
    piece = piece.resize((int(round(piece.width * scale)), int(round(piece.height * scale))), Image.LANCZOS)
    piece = piece.resize((piece.width, int(round(piece.height * stretch))), Image.LANCZOS)
    result = np.array(piece)
    if mirror:
        result = result[:, ::-1].copy()
    # Horizontal anchor: alpha-weighted centre of head + chest (top 40%), steadier than the bbox.
    top = result[: int(result.shape[0] * 0.4), :, 3].astype(float)
    anchor = (top.sum(0) * np.arange(top.shape[1])).sum() / top.sum()
    return result, anchor


def source_path(relative):
    return os.path.join(out, relative)


# Cut every source facing once.
idle_config = config['idle']
idle_figures = cut_grid(source_path(idle_config['file']), len(idle_config['columns']), idle_config['row_count'], idle_config['row'])
idle_by_facing = dict(zip(idle_config['columns'], idle_figures))
idle_median = float(np.median([f.shape[0] for f in idle_figures]))

walk_by_facing = {}
for facing in sorted(set(row['from'] for row in rows)):
    walk_by_facing[facing] = cut_grid(source_path(config['walk']['files'][facing]), walk_columns, walk_rows)

# The facings are drawn separately, so each one is scaled by its own median height.
frames = {}  # row name -> [idle, walk_0, walk_1, ...] as (crop, anchor)
report = {}
for row in rows:
    facing, mirror = row['from'], bool(row.get('mirror', False))
    walk = walk_by_facing[facing]
    walk_median = float(np.median([f.shape[0] for f in walk]))
    report[row['name']] = {'from': facing, 'mirror': mirror, 'source_walk_height_px': walk_median}
    frames[row['name']] = [finish(idle_by_facing[facing], target_height / idle_median, mirror)]
    frames[row['name']] += [finish(f, target_height / walk_median, mirror) for f in walk]
all_frames = [frame for row in rows for frame in frames[row['name']]]

# One cell size for every frame, feet on one ground line.
left = max(a for c, a in all_frames)
right = max(c.shape[1] - a for c, a in all_frames)
if symmetric:  # anchor in the exact middle, so a mirrored row shares the pivot
    left = right = max(left, right)
tallest = max(c.shape[0] for c, a in all_frames)
cell_w = int(np.ceil(left)) + int(np.ceil(right)) + 2 * PAD
cell_w += cell_w % 2
cell_h = tallest + 2 * PAD
anchor_x = cell_w // 2 if symmetric else PAD + int(np.ceil(left))
ground_y = cell_h - PAD
columns = 1 + walk_columns * walk_rows


def to_cell(frame):
    crop, anchor = frame
    cell = Image.new('RGBA', (cell_w, cell_h), (0, 0, 0, 0))
    cell.alpha_composite(Image.fromarray(crop), (int(round(anchor_x - anchor)), ground_y - crop.shape[0]))
    return cell


# A looping preview GIF on a flat colour, showing only what alpha clipping at 0.5 keeps.
def gif(cells, path):
    images = []
    for cell in cells:
        pixels = np.array(cell)
        pixels[..., 3] = np.where(pixels[..., 3] >= 128, 255, 0)
        background = Image.new('RGBA', (cell_w, cell_h), (205, 200, 190, 255))
        background.alpha_composite(Image.fromarray(pixels))
        images.append(background.convert('RGB').quantize(255))
    images[0].save(path, save_all=True, append_images=images[1:], duration=int(1000 / fps), loop=0, disposal=2)


sheet = Image.new('RGBA', (cell_w * columns, cell_h * len(rows)), (0, 0, 0, 0))
for r, row in enumerate(rows):
    cells = [to_cell(frame) for frame in frames[row['name']]]
    for c, cell in enumerate(cells):
        sheet.alpha_composite(cell, (c * cell_w, r * cell_h))
    gif(cells[1:], f'{out}/{prefix}_Walk_{row["name"]}.gif')
sheet.save(f'{out}/{prefix}.png')

# Preview of what alpha clipping at 0.5 leaves, on a grey floor colour.
clip = np.array(sheet)
keep = clip[..., 3] >= 128
preview = np.zeros_like(clip)
preview[...] = (120, 125, 115, 255)
preview[keep] = clip[keep]
preview[keep, 3] = 255
Image.fromarray(preview).convert('RGB').save(f'{out}/{prefix}_alphaclip_preview.png')

idle_heights = sorted(frames[row['name']][0][0].shape[0] for row in rows)
idle_median_stretched = idle_heights[len(idle_heights) // 2]
walk_heights = sorted(f[0].shape[0] for row in rows for f in frames[row['name']][1:])
meta = {
    'stretch': stretch,
    'stretched': True,
    'cell': [cell_w, cell_h],
    'columns': columns,
    'column_0': 'idle',
    'walk_frames': walk_columns * walk_rows,
    'rows_in_order': [row['name'] for row in rows],
    'rows': report,
    'fps': fps,
    'pivot_px_from_bottom_left': [anchor_x, PAD],
    'pivot_normalized': [round(anchor_x / cell_w, 4), round(PAD / cell_h, 4)],
    'idle_height_px_stretched': {'min': idle_heights[0], 'median': idle_median_stretched, 'max': idle_heights[-1]},
    'walk_height_px_stretched': {'min': walk_heights[0], 'max': walk_heights[-1]},
    'world_height': world_height,
    'ppu': round(idle_median_stretched / world_height, 1),
}
with open(f'{out}/{prefix}.json', 'w') as handle:
    json.dump(meta, handle, indent=2)
print(json.dumps(meta))
