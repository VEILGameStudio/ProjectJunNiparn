"""Export the filled parts as a layered PSB for Unity (PSD Importer + 2D Animation), plus rig.json and a parts sheet.
Eye states (half, closed) are extra hidden layers, for a sprite swap when blinking.
Usage: python3 -I export_rig.py <build_dir> <rig.json> <out_dir>
"""
import sys, os, json, importlib.util
import numpy as np
from PIL import Image, ImageDraw

D, RIG, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
os.makedirs(f"{OUT}/parts", exist_ok=True)
here = os.path.dirname(os.path.abspath(__file__))
sys.argv = [sys.argv[0], D, RIG, OUT, "none"]                # load animate.py as a module, run nothing
spec = importlib.util.spec_from_file_location("animate", os.path.join(here, "animate.py"))
A = importlib.util.module_from_spec(spec)
spec.loader.exec_module(A)
R = A.R
tag = R["name"].lower().replace(" ", "_")


def unpremul(a):
    o = a.copy()
    al = o[..., 3:4] / 255.0
    o[..., :3] = np.where(al > 0, o[..., :3] / np.maximum(al, 1e-3), 0)
    return Image.fromarray(np.clip(o, 0, 255).astype(np.uint8))


layers = [(n, unpremul(A.IMG[n]), True) for n in A.ORDER]
for e in A.LIDS:
    for state in ("half", "closed"):
        layers.append((f"{e}_{state}", unpremul(A.LIDS[e][state]), False))
for n, im, _ in layers:
    im.save(f"{OUT}/parts/{n}.png")

from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer, Group

psd = PSDImage.new("RGBA", (A.W, A.H), color=(0, 0, 0, 0))
psd._record.header.version = 2                           # PSB, what the PSD Importer expects for rigs
grp = Group.new(psd, name=tag)
for n, im, vis in layers:
    bb = im.getbbox()
    lay = PixelLayer.frompil(im.crop(bb), psd, name=n, top=bb[1], left=bb[0])
    lay.visible = vis
    grp.append(lay)
psd.save(f"{OUT}/{tag}_rig.psb")

json.dump({"canvas": [A.W, A.H], "draw_order_bottom_to_top": A.ORDER, "parts": R["parts"], "rig": R["rig"],
           "lines": R["lines"], "eye_states": ["open", "half", "closed"],
           "note": "pivots in canvas px (y down). Not stretched: CLAUDE.md stretch x1.015 SideView / x1.155 ThreeQuarter "
                   "is applied when the sprite is imported (import-sprite-art)."},
          open(f"{OUT}/{tag}_rig.json", "w"), indent=1)

tw, th, cols = 300, 360, 6
rows = (len(layers) + cols - 1) // cols
sheet = Image.new("RGB", (tw * cols, th * rows + 50), (236, 230, 220))
dr = ImageDraw.Draw(sheet)
dr.text((12, 14), f"{R['name']} - parts after painting what each one hides (bottom to top)", fill=(60, 50, 50))
for i, (n, im, vis) in enumerate(layers):
    c = im.crop(im.getbbox())
    c.thumbnail((tw - 20, th - 40))
    x, y = (i % cols) * tw, 50 + (i // cols) * th
    dr.rectangle([x + 4, y, x + tw - 4, y + th - 8], fill=(246, 242, 236), outline=(200, 192, 182))
    sheet.paste(c, (x + (tw - c.width) // 2, y + 6), c)
    dr.text((x + 10, y + th - 28), f"{i + 1}. {n}" + ("" if vis else " (hidden)"), fill=(70, 60, 60))
sheet.save(f"{OUT}/{tag}_parts_sheet.png")
print("exported:", f"{OUT}/{tag}_rig.psb", "layers:", [n for n, _, _ in layers])
