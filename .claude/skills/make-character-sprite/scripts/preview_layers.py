"""Contact sheet: the composite plus every layer on its own.

Usage: python3 preview_layers.py layer_dir cfg.json scale out.png [per_row]
"""
import sys, json
from PIL import Image, ImageDraw
d=sys.argv[1]; cfg=json.load(open(sys.argv[2])); s=int(sys.argv[3]); out=sys.argv[4]
per=int(sys.argv[5]) if len(sys.argv)>5 else 10
L=[(n,Image.open(f'{d}/{n}.png')) for n in cfg['order']]
w,h=L[0][1].size
comp=Image.new('RGBA',(w,h),(0,0,0,0))
for n,l in L: comp.alpha_composite(l)
items=[('composite',comp)]+L
rows=(len(items)+per-1)//per
sheet=Image.new('RGBA',(w*per*s,(h+14)*rows*s),(200,205,210,255))
dr=ImageDraw.Draw(sheet)
for i,(n,img) in enumerate(items):
    cx=(i%per)*w*s; cy=(i//per)*(h+14)*s
    sheet.alpha_composite(img.resize((w*s,h*s),Image.NEAREST),(cx,cy+14*s)); dr.text((cx+4,cy+4),n,fill=(0,0,0))
    dr.rectangle([cx,cy,cx+w*s-1,cy+(h+14)*s-1],outline=(170,175,180))
sheet.save(out)
