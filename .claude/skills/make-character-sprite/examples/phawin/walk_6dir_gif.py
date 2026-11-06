"""Phawin example: 6-direction in-place walk GIF from split layers, using bone skinning.

Run from a folder holding the layer folders and configs:
  layers_F/ layers_B/ layers_SL/ layers_SR/ layers_BL/ layers_BR/  +  cfg_F.json cfg_B.json ...
  python3 walk_6dir_gif.py
The bone positions in scripts/rigs.py are for Phawin's source frame; a new character needs its own.
"""
import os, sys
sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "scripts"))
import numpy as np
from PIL import Image, ImageDraw
from skin import Rig, PAD
import rigs
dirs=[("Front",Rig("layers_F","cfg_F.json",rigs.front_rig()),lambda t:rigs.front_walk(t)),
      ("Back",Rig("layers_B","cfg_B.json",rigs.back_rig()),lambda t:rigs.front_walk(t,"SL","SR")),
      ("Side L",Rig("layers_SL","cfg_SL.json",rigs.side_rig()),lambda t:rigs.side_walk(t,-1)),
      ("Side R",Rig("layers_SR","cfg_SR.json",rigs.side_rig(mx=1020)),lambda t:rigs.side_walk(t,1)),
      ("Back L",Rig("layers_BL","cfg_BL.json",rigs.back_rig(dx=513)),lambda t:rigs.front_walk(t,"SL","SR")),
      ("Back R",Rig("layers_BR","cfg_BR.json",rigs.back_rig(mirror=1188)),lambda t:rigs.front_walk(t,"SL","SR"))]
N=12; sc=0.5; frames=[]
for i in range(N):
    t=i/N; tiles=[]
    for lab,R,fn in dirs:
        fr=R.render(fn(t))
        bg=Image.new("RGBA",fr.size,(236,232,224,255)); d=ImageDraw.Draw(bg)
        foot=(310-R.cfg["crop"][1])*4+PAD; cx=fr.width//2
        d.ellipse([cx-150,foot-14,cx+150,foot+22],fill=(196,190,180,255))
        bg.alpha_composite(fr)
        tiles.append((bg.resize((int(fr.width*sc),int(fr.height*sc)),Image.LANCZOS),lab))
    w,h=tiles[0][0].size
    sheet=Image.new("RGB",(w*6,h+30),(236,232,224)); dd=ImageDraw.Draw(sheet)
    for k,(im,lab) in enumerate(tiles):
        sheet.paste(im.convert("RGB"),(k*w,30)); dd.text((k*w+10,8),lab,fill=(60,60,60))
    frames.append(sheet); print("frame",i,flush=True)
frames[0].save("walk_inplace_6dir.gif",save_all=True,append_images=frames[1:],duration=85,loop=0,optimize=True)
frames[3].save("walk_f3.png")
