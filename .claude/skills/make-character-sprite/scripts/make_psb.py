"""Pack split layers (bottom -> top as in cfg["order"]) into a Large Document (.psb) for Unity PSD Importer.

Usage: python3 make_psb.py layer_dir cfg.json PREFIX out.psb
Layers are named PREFIX_<layer> inside a group PREFIX. Also writes out_check.png (the flattened file).
"""
import json, sys
from PIL import Image
from psd_tools import PSDImage
from psd_tools.api.layers import PixelLayer, Group
d, cfgp, prefix, out = sys.argv[1:5]
cfg = json.load(open(cfgp))
w, h = Image.open(f'{d}/{cfg['order'][0]}.png').size
psd = PSDImage.new('RGBA', (w, h), color=(0, 0, 0, 0))
psd._record.header.version = 2
grp = Group.new(psd, name=prefix)
for n in cfg['order']:  # bottom -> top
    im = Image.open(f'{d}/{n}.png').convert('RGBA')
    bb = im.getbbox()
    if not bb: continue
    lay = PixelLayer.frompil(im.crop(bb), psd, name=f'{prefix}_{n}', top=bb[1], left=bb[0])
    grp.append(lay)
psd.save(out)
p = PSDImage.open(out)
print(out, p.version, p.size, [l.name for l in p.descendants()])
p.composite().save(out.replace('.psb', '_check.png'))
