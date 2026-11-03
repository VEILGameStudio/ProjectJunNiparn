# Usage: python3 forest_sim.py <scene.unity> <out.png> "<label>" [camera x]
# Needs numpy + pillow. Draws simple silhouettes per tree prefab with the SideView camera maths (pitch 10, FOV 19, distance 20) and linear fog.
# Schematic perspective render of Test_Forest rows (value/colour study, not a real Unity render).
import re,sys,math,numpy as np
from PIL import Image,ImageDraw,ImageFont
scene=sys.argv[1]; out=sys.argv[2]; mode=sys.argv[3]  # 'scene' = read values from file
t=open(scene).read()
docs=re.split(r'\n(?=--- !u!)',t)
parents={'9300011':'Deep','9300271':'Far','9300501':'Mid','9300661':'Near','9300781':'Under','9301031':'Fore'}
size={'Banana':(2.46,3.5),'Bushy':(3.40,4.5),'OakBig':(3.81,5.0),'OakRound':(3.46,4.0),'Willow':(2.28,6.0)}
inst=[]
for d in docs:
    if not d.startswith('--- !u!1001'): continue
    m=re.search(r'm_TransformParent: \{fileID: (\d+)\}',d)
    if not m or m.group(1) not in parents: continue
    g=lambda p,df=None:(lambda mm: mm.group(1) if mm else df)(re.search(r'propertyPath: '+re.escape(p)+r'\n\s+value: ([^\n]*)',d))
    name=g('m_Name'); kind=re.sub(r'_\d+','',name)
    inst.append(dict(row=parents[m.group(1)],kind=kind,x=float(g('m_LocalPosition.x',0)),y=float(g('m_LocalPosition.y',0)),z=float(g('m_LocalPosition.z',0)),s=float(g('m_LocalScale.x',1)),
        c=np.array([float(g('m_Color.r',1)),float(g('m_Color.g',1)),float(g('m_Color.b',1))]),flip=g('m_FlipX','0')=='1'))
rs=lambda k:[float(v) for v in re.search(k+r': \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+)',t).groups()]
fog=np.array(rs('m_FogColor')); bg=np.array(rs('m_BackGroundColor'))
fs=float(re.search(r'm_LinearFogStart: ([\d.]+)',t).group(1)); fe=float(re.search(r'm_LinearFogEnd: ([\d.]+)',t).group(1))
W,H=1280,720; pitch=math.radians(10); fov=math.radians(19); cx=float(sys.argv[4]) if len(sys.argv)>4 else 0
cam=np.array([cx,2+20*math.sin(pitch),-20*math.cos(pitch)])
fwd=np.array([0,-math.sin(pitch),math.cos(pitch)]); up=np.array([0,math.cos(pitch),math.sin(pitch)]); right=np.array([1,0,0])
k=(H/2)/math.tan(fov/2)
def proj(p):
    v=np.array(p)-cam; z=v@fwd; return (W/2+(v@right)/z*k, H/2-(v@up)/z*k, z)
def fogmix(col,z):
    f=min(1,max(0,(fe-z)/(fe-fs))); return col*f+fog*(1-f)
def srgb(c): return tuple(int(255*min(1,max(0,v))) for v in c)
img=Image.new('RGB',(W,H),srgb(bg)); d=ImageDraw.Draw(img)
# floor: per scanline
dirt=np.array([0.22,0.17,0.12])
for sy in range(H):
    ray=fwd*k+up*(H/2-sy); ray=ray/np.linalg.norm(ray)
    if ray[1]>=-1e-4: continue
    tt=-cam[1]/ray[1]; p=cam+ray*tt
    if p[2]>130 or p[2]<-6: continue
    d.line([(0,sy),(W,sy)],fill=srgb(fogmix(dirt,tt*(ray@fwd))))
foliage={'Banana':np.array([.50,.56,.22]),'Bushy':np.array([.42,.50,.20]),'OakBig':np.array([.40,.46,.18]),'OakRound':np.array([.45,.52,.20]),'Willow':np.array([.36,.46,.24])}
trunk=np.array([.32,.25,.17])
rng=np.random.default_rng(1)
for it in sorted(inst,key=lambda i:-i['z']):
    w,h=size.get(it['kind'],(3,4)); w*=it['s']; h*=it['s']
    x0,y0,z=proj((it['x']-w/2,it['y'],it['z'])); x1,y1,_=proj((it['x']+w/2,it['y']+h,it['z']))
    if x1<-50 or x0>W+50: continue
    tc=srgb(fogmix(trunk*it['c'],z)); fc=srgb(fogmix(foliage[it['kind']]*it['c'],z)); fd=srgb(fogmix(foliage[it['kind']]*it['c']*0.6,z))
    cw=x1-x0; ch=y0-y1; mx=(x0+x1)/2
    if it['kind']=='Banana':
        d.rectangle([mx-cw*.06,y1+ch*.45,mx+cw*.06,y0],fill=tc)
        for a in range(-70,80,25):
            r=math.radians(a); d.polygon([(mx,y1+ch*.5),(mx+math.sin(r)*cw*.5,y1+ch*.5-math.cos(r)*ch*.5),(mx+math.sin(r+.25)*cw*.45,y1+ch*.55-math.cos(r+.25)*ch*.42)],fill=fc)
    elif it['kind']=='Willow':
        d.rectangle([mx-cw*.07,y1+ch*.3,mx+cw*.07,y0],fill=tc)
        d.ellipse([x0,y1,x1,y1+ch*.55],fill=fc)
        for j in range(9):
            xx=x0+cw*(.08+.84*j/8); d.rectangle([xx-cw*.05,y1+ch*.25,xx+cw*.05,y1+ch*(.7+.15*rng.random())],fill=fd if j%2 else fc)
    else:
        d.polygon([(mx-cw*.09,y0),(mx+cw*.09,y0),(mx+cw*.05,y1+ch*.45),(mx-cw*.05,y1+ch*.45)],fill=tc)
        top=.62 if it['kind']=='OakBig' else .7
        for j in range(5):
            ex=x0+cw*(.05+.18*j); ey=y1+ch*(.05+.12*(j%2))
            d.ellipse([ex-cw*.12,ey,ex+cw*.32,ey+ch*top*.75],fill=fc if j%2 else fd)
        d.ellipse([x0+cw*.08,y1,x1-cw*.08,y1+ch*top],fill=fc)
# player marker (1.55 units tall at z=0)
px0,py0,_=proj((cx-.3,0,0)); px1,py1,_=proj((cx+.3,1.55,0))
d.rounded_rectangle([px0,py1,px1,py0],radius=6,fill=(200,80,70),outline=(20,10,10),width=2)
F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',20)
d.rectangle([0,0,W,32],fill=(0,0,0)); d.text((10,5),f'{mode}  |  schematic, NOT a Unity render  |  fog {fs:.0f}-{fe:.0f}  cam x={cx:+.0f}',font=F,fill=(255,255,255))
img.save(out)
L=np.array(img.convert('L')).astype(float)[32:]
print(mode,'value bands <20/20-40/40-60/>60 (%):',[round(((L>=a*2.55)&(L<b*2.55)).mean()*100) for a,b in [(0,20),(20,40),(40,60),(60,101)]])
