"""Phawin example: writes cfg_B/BL/BR/SL/SR.json. BL is the back view shifted, BR and SR are mirrors.
Coordinates are pixels of Phawin's source frame (the artist's 6-direction sheet, frame 0).
"""
import json, copy
FILL={"head":[45,47,47],"torso":[221,199,175],"hip":[47,62,80],"leg_SL":[47,62,80],"leg_SR":[47,62,80],
      "arm_SL":[221,199,175],"arm_SR":[221,199,175],"neck":[243,218,213]}
def tx(pts, f): return [list(f(x,y)) for x,y in pts]
back={"name":"B","generic":True,"scale":4,"crop":[195,12,323,322],"hip_y":236,"mid_x":256,"chin_y":0,
 "polys":{"head":[[195,12],[323,12],[323,120],[195,120]],
  "arm_SL":[[226,127],[214,138],[206,160],[202,172],[214,180],[236,180],[238,145]],
  "arm_SR":[[272,126],[286,132],[296,150],[306,170],[301,181],[304,200],[302,224],[289,226],[285,205],[282,187],[271,181],[268,145]],
  "bag":[[282,124],[294,130],[252,180],[254,196],[253,234],[221,236],[205,229],[204,190],[213,176],[240,158]],
  "sash":[[287,216],[303,212],[316,240],[314,274],[299,274],[291,250]],
  "leg_SL":[[196,236],[256,236],[256,322],[196,322]],"leg_SR":[[256,236],[323,236],[323,322],[256,322]]},
 "boxes":{"neck":[236,115,266,128]},
 "parts":[{"name":"sash","poly":"sash","mats":["sash"]},
          {"name":"arm_SL","poly":"arm_SL","not_mats":["brown","pants","sash"]},
          {"name":"arm_SR","poly":"arm_SR","not_mats":["brown","pants","sash"]},
          {"name":"bag","poly":"bag","mats":["brown","eye","sandal"]}],
 "order":["leg_SL","leg_SR","hip","sash","torso","arm_SL","arm_SR","bag","head"],
 "full":{"torso":[[[236,120],[264,120],[275,127],[286,145],[287,192],[256,197],[214,192],[222,145],[228,127]]],
         "leg_SL":[[[205,226],[256,226],[256,296],[205,296]]],"leg_SR":[[[256,226],[305,226],[305,296],[256,296]]]},
 "pivots":{"head":[252,120],"arm_SL":[228,134],"arm_SR":[278,133],"leg_SL":[232,236],"leg_SR":[280,236],"bag":[262,140],"sash":[296,220]},
 "kind":"frontback","foot_y":298}
def offset(c, dx, name):
    c=copy.deepcopy(c); c["name"]=name
    f=lambda x,y:(x+dx,y)
    c["crop"]=[c["crop"][0]+dx,c["crop"][1],c["crop"][2]+dx,c["crop"][3]]; c["mid_x"]+=dx
    c["polys"]={k:tx(v,f) for k,v in c["polys"].items()}
    c["boxes"]={k:[b[0]+dx,b[1],b[2]+dx,b[3]] for k,b in c["boxes"].items()}
    c["full"]={k:[tx(p,f) for p in v] for k,v in c["full"].items()}
    c["pivots"]={k:list(f(*v)) for k,v in c["pivots"].items()}
    return c
def mirror(c, C, name):
    c=copy.deepcopy(c); c["name"]=name
    f=lambda x,y:(C-x,y)
    c["crop"]=[C-c["crop"][2],c["crop"][1],C-c["crop"][0],c["crop"][3]]; c["mid_x"]=C-c["mid_x"]
    sw=lambda k:k.replace("_SL","_TMP").replace("_SR","_SL").replace("_TMP","_SR")
    c["polys"]={sw(k):tx(v,f) for k,v in c["polys"].items()}
    c["boxes"]={k:[C-b[2],b[1],C-b[0],b[3]] for k,b in c["boxes"].items()}
    c["full"]={sw(k):[tx(p,f) for p in v] for k,v in c["full"].items()}
    c["pivots"]={sw(k):list(f(*v)) for k,v in c["pivots"].items()}
    for p in c["parts"]: p["name"]=sw(p["name"]); p["poly"]=sw(p["poly"])
    c["order"]=[sw(n) for n in c["order"]]
    return c
sideL={"name":"SL","generic":True,"scale":4,"crop":[365,12,493,322],"hip_y":236,"mid_x":425,"chin_y":0,
 "polys":{"head":[[365,12],[493,12],[493,123],[425,123],[420,121],[400,122],[365,122]],
  "arm":[[424,150],[452,150],[459,180],[449,186],[448,214],[442,225],[431,223],[430,187],[418,185],[416,165]],
  "bag":[[408,127],[440,125],[462,148],[472,172],[480,200],[472,216],[456,224],[450,214],[450,186],[458,180],[452,150],[424,148],[410,140]],
  "sash":[[388,210],[410,210],[410,284],[388,284]],
  "leg_SL":[[380,236],[425,236],[425,322],[380,322]],"leg_SR":[[425,236],[480,236],[480,322],[425,322]]},
 "boxes":{"neck":[415,118,440,130]},
 "parts":[{"name":"sash","poly":"sash","mats":["sash"]},
          {"name":"bag","poly":"bag","mats":["brown","eye","sandal"]},
          {"name":"arm","poly":"arm","not_mats":["brown","pants","sash"]}],
 "order":["leg_SR","leg_SL","hip","sash","torso","bag","arm","head"],
 "full":{"torso":[[[412,123],[432,123],[442,135],[452,150],[455,196],[402,196],[398,170],[404,140]]],
         "leg_SL":[[[388,226],[425,226],[425,296],[388,296]]],"leg_SR":[[[425,226],[462,226],[462,296],[425,296]]],
         "hip":[[[398,183],[457,183],[459,242],[396,242]]]},
 "pivots":{"head":[422,122],"arm":[438,152],"leg_SL":[426,228],"leg_SR":[426,228],"bag":[430,135],"sash":[398,214]},
 "kind":"side","foot_y":298}
cfgs={"B":back,"BL":offset(back,513,"BL"),"BR":mirror(back,1188,"BR"),"SL":sideL}
sr=mirror(sideL,1020,"SR"); sr["polys"]["sash"]=[[590,224],[616,224],[616,276],[590,276]]
cfgs["SR"]=sr
for k,c in cfgs.items():
    c["fill"]={n:v for n,v in FILL.items()}; c["fill"]["arm"]=[221,199,175]; c["fill"]["bag"]=None
    json.dump(c,open(f"cfg_{k}.json","w"),indent=1)
print(list(cfgs))
