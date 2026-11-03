# Procedural painter for the reference-style forest layers. Output: RGBA PNGs + layout json.
import numpy as np, math, json, sys
from PIL import Image, ImageDraw, ImageFilter
OUT=sys.argv[1]; rng=np.random.default_rng(7)
CAMY,CAMZ=5.47,-19.70; EDGEZ=3.8
def yg(z): return CAMY-(CAMY/(EDGEZ-CAMZ))*(z-CAMZ)
def ytop(z): return CAMY-math.tan(math.radians(0.5))*(z-CAMZ)
def width(z): return 0.335*(z-CAMZ)*2.3333+22
def hexc(h,a=255): h=h.lstrip('#'); return (int(h[0:2],16),int(h[2:4],16),int(h[4:6],16),a)
def mix(a,b,t): return tuple(int(a[i]*(1-t)+b[i]*t) for i in range(len(a)))
SS=2
class Card:
    def __init__(s,name,z,ppu,bottom=None,top=None,w=None):
        s.name,s.z,s.ppu=name,z,ppu
        s.bottom=bottom if bottom is not None else yg(z)-1.5
        s.top=top if top is not None else ytop(z)+3
        s.w=w or width(z); s.W=int(s.w*ppu); s.H=int((s.top-s.bottom)*ppu)
        s.img=Image.new('RGBA',(s.W*SS,s.H*SS),(0,0,0,0)); s.d=ImageDraw.Draw(s.img)
    def X(s,x): return (x+s.w/2)*s.ppu*SS          # world x (relative to card centre) -> px
    def Y(s,y): return (s.top-y)*s.ppu*SS           # world y -> px
    def save(s,blur=0):
        im=s.img.resize((s.W,s.H),Image.LANCZOS)
        if blur: im=im.filter(ImageFilter.GaussianBlur(blur))
        im.save(f'{OUT}/{s.name}.png'); return dict(name=s.name,z=s.z,ppu=s.ppu,bottom=round(s.bottom,3),top=round(s.top,3),width=round(s.w,3),px=[s.W,s.H])
def vgrad(card,y0,y1,c0,c1,x0=None,x1=None):
    a=int(card.Y(y0)); b=int(card.Y(y1))
    for r in range(min(a,b),max(a,b)):
        t=(r-min(a,b))/max(1,abs(b-a)); card.d.line([(0 if x0 is None else x0,r),(card.img.width if x1 is None else x1,r)],fill=mix(c0,c1,t))
def fade_alpha_bottom(card,y_from,y_to,strength):
    """lighten towards haze colour near ground (ground fog)"""
    pass
layout=[]
# ---------- L0 haze plate ----------
c=Card('fr_L0_haze',180,16)
arr=np.zeros((c.H*SS,c.W*SS,4),np.float32)
top=np.array(hexc('#B4CCB3'));glow=np.array(hexc('#EEF3DF'));low=np.array(hexc('#A7BEA3'))
yy=np.linspace(0,1,c.H*SS)[:,None]; xx=np.linspace(-1,1,c.W*SS)[None,:]
gy=c.Y(yg(180)+14)/ (c.H*SS)
g=np.exp(-((xx*1.6)**2)-((yy-gy)/0.28)**2)
base=top*(1-yy)[...,None]+low*yy[...,None]
arr=base*(1-g[...,None])+glow*g[...,None]; arr[...,3]=255
c.img=Image.fromarray(arr.clip(0,255).astype(np.uint8)); c.d=ImageDraw.Draw(c.img)
layout.append(c.save())
# ---------- L1 deep pines ----------
def spruce(card,x,base,h,w,col,jag=0.18):
    d=card.d; tiers=int(h*1.6)+6
    d.rectangle([card.X(x-w*0.03),card.Y(base+h*0.95),card.X(x+w*0.03),card.Y(base-1)],fill=col)
    for i in range(tiers):
        t=i/tiers; yb=base+h*0.08+h*0.9*t; tw=w*(1-t)**0.9*0.5*(0.85+0.3*rng.random()); th=h/tiers*2.4
        pts=[(card.X(x),card.Y(yb+th))]
        n=6
        for k in range(n+1):
            fx=x+tw-(2*tw)*k/n; fy=yb-(jag*th*rng.random() if k%2 else 0)
            pts.append((card.X(fx),card.Y(fy)))
        d.polygon(pts,fill=col)
c=Card('fr_L1_deep_pines',100,27)
for row,(col,hr,dens) in enumerate([('#AFC6AB',(16,26),2.0),('#9AB298',(13,22),2.6)]):
    x=-c.w/2-2
    while x<c.w/2+2:
        h=rng.uniform(*hr); spruce(c,x,yg(100)-1,h,h*0.32,hexc(col)); x+=rng.uniform(dens*0.5,dens*1.6)
# ground mist bank
mist=Image.new('RGBA',c.img.size,(0,0,0,0)); md=ImageDraw.Draw(mist)
for r in range(int(c.Y(yg(100)+5)),c.img.height):
    t=min(1,(r-c.Y(yg(100)+5))/(c.Y(yg(100))-c.Y(yg(100)+5)+1)); md.line([(0,r),(c.img.width,r)],fill=hexc('#C2D6BE',int(200*t)))
c.img=Image.alpha_composite(c.img,mist); layout.append(c.save(blur=0.6))
# ---------- god rays ----------
c=Card('fr_rays',14,24)
for cx,wid,a in [(-0.5,2.4,80),(2.3,1.1,95),(4.6,1.6,60),(-7,1.3,55),(9.5,1.8,50),(-14,1.5,45),(16,1.4,45)]:
    layer=Image.new('RGBA',c.img.size,(0,0,0,0)); ld=ImageDraw.Draw(layer)
    sl=math.tan(math.radians(9)); t0=c.top; b0=yg(14)
    pts=[(c.X(cx-wid*0.35),c.Y(t0)),(c.X(cx+wid*0.35),c.Y(t0)),(c.X(cx+wid*0.6-sl*(t0-b0)),c.Y(b0)),(c.X(cx-wid*0.6-sl*(t0-b0)),c.Y(b0))]
    ld.polygon(pts,fill=hexc('#FBF6D2',min(255,int(a*1.5))))
    al=np.array(layer)[...,3].astype(float); fall=np.linspace(1,0.0,al.shape[0])[:,None]**1.3; al*=fall
    L=np.array(layer); L[...,3]=al.astype(np.uint8); layer=Image.fromarray(L).filter(ImageFilter.GaussianBlur(6*SS))
    c.img=Image.alpha_composite(c.img,layer)
layout.append(c.save())
# ---------- L2 far trunks ----------
def trunk(card,x,w,base,top,body,shade,light,line,lean=0,lw=1):
    d=card.d; dx=math.tan(math.radians(lean))*(top-base)
    flare=w*0.35
    poly=[(card.X(x-w/2-flare),card.Y(base)),(card.X(x-w/2),card.Y(base+w*1.2)),(card.X(x-w/2+dx),card.Y(top)),(card.X(x+w/2+dx),card.Y(top)),(card.X(x+w/2),card.Y(base+w*1.2)),(card.X(x+w/2+flare),card.Y(base))]
    d.polygon(poly,fill=body,outline=line,width=lw*SS)
    # shade band left, light rim right
    sp=[(card.X(x-w/2+w*0.08),card.Y(base+w*1.2)),(card.X(x-w/2+w*0.08+dx),card.Y(top)),(card.X(x-w/2+w*0.38+dx),card.Y(top)),(card.X(x-w/2+w*0.34),card.Y(base+w*1.2))]
    d.polygon(sp,fill=shade)
    lp=[(card.X(x+w/2-w*0.22),card.Y(base+w*1.2)),(card.X(x+w/2-w*0.22+dx),card.Y(top)),(card.X(x+w/2-w*0.08+dx),card.Y(top)),(card.X(x+w/2-w*0.08),card.Y(base+w*1.2))]
    d.polygon(lp,fill=light)
    return dx
c=Card('fr_L2_far_trunks',45,48)
x=-c.w/2
while x<c.w/2:
    w=rng.uniform(0.45,1.3); trunk(c,x,w,yg(45)-1,c.top+1,hexc('#7D927C'),hexc('#6D836E'),hexc('#93A891'),hexc('#627764'),lean=rng.uniform(-1.5,1.5))
    # a few bough tiers
    for k in range(rng.integers(2,6)):
        by=rng.uniform(yg(45)+4,c.top-1); bw=rng.uniform(1.2,2.4)
        side=rng.choice([-1,1]); pts=[(c.X(x),c.Y(by+0.25))]
        for q in range(7):
            f=q/6; pts.append((c.X(x+side*bw*f),c.Y(by+0.25-0.55*f-(0.18 if q%2 else 0))))
        pts.append((c.X(x),c.Y(by-0.15))); c.d.polygon(pts,fill=hexc('#768D77'))
    x+=rng.uniform(1.4,4.8)
for k in range(60):  # base shrubs
    bx=rng.uniform(-c.w/2,c.w/2); r=rng.uniform(0.6,1.8); c.d.ellipse([c.X(bx-r),c.Y(yg(45)+r*0.9),c.X(bx+r),c.Y(yg(45)-r*0.3)],fill=hexc('#869C84'))
mist=Image.new('RGBA',c.img.size,(0,0,0,0)); md=ImageDraw.Draw(mist)
for r in range(int(c.Y(yg(45)+6)),c.img.height):
    t=min(1,(r-c.Y(yg(45)+6))/(c.Y(yg(45))-c.Y(yg(45)+6)+1)); md.line([(0,r),(c.img.width,r)],fill=hexc('#B9CDB5',int(170*t)))
c.img=Image.alpha_composite(c.img,mist); layout.append(c.save(blur=0.4))
# ---------- L3 mid trunks with moss ----------
c=Card('fr_L3_mid_trunks',22,72)
xs=[];x=-c.w/2+rng.uniform(0,2)
while x<c.w/2:
    xs.append(x); x+=rng.choice([rng.uniform(1.6,2.6),rng.uniform(3,4.5),rng.uniform(5,6.5)])
for x in xs:
    if abs(x-1.0)<2.2: continue  # keep a clearing in the middle for the rays
    w=rng.uniform(0.9,2.0); lean=rng.uniform(-2.5,2.5); base=yg(22)-0.6; top=c.top+1
    dx=trunk(c,x,w,base,top,hexc('#565A42'),hexc('#3F4134'),hexc('#7A7E5C'),hexc('#23261D'),lean=lean,lw=2)
    # bark lines
    for k in range(int(w*5)):
        bx=x-w/2+w*rng.uniform(0.15,0.85); y0=rng.uniform(base+1,top-2); L=rng.uniform(0.8,2.5)
        c.d.line([(c.X(bx+dx*(y0-base)/(top-base)),c.Y(y0)),(c.X(bx+dx*(y0-base)/(top-base)+rng.uniform(-.05,.05)),c.Y(y0+L))],fill=hexc('#3E4033'),width=2*SS)
    # branch stubs
    for k in range(rng.integers(1,4)):
        sy=rng.uniform(base+3,top-1.5); side=rng.choice([-1,1]); sx=x+dx*(sy-base)/(top-base)+side*w/2; L=rng.uniform(0.5,1.4)
        c.d.line([(c.X(sx),c.Y(sy)),(c.X(sx+side*L),c.Y(sy+L*0.5))],fill=hexc('#2B2E24'),width=int(w*9*SS))
        c.d.line([(c.X(sx),c.Y(sy)),(c.X(sx+side*L),c.Y(sy+L*0.5))],fill=hexc('#5E6247'),width=int(w*6*SS))
        c.d.ellipse([c.X(sx+side*L-0.25),c.Y(sy+L*0.5+0.25),c.X(sx+side*L+0.25),c.Y(sy+L*0.5-0.1)],fill=hexc('#9DB236'))
    # moss: irregular bands hugging the lit (right) edge, plus drips
    xat=lambda yy: x+dx*(yy-base)/(top-base)
    for k in range(rng.integers(2,5)):
        y0=rng.uniform(base+0.3,top-3); L=rng.uniform(0.8,3.2); n=12
        outer=[(c.X(xat(y0+L*q/n)+w/2-0.02),c.Y(y0+L*q/n)) for q in range(n+1)]
        inner=[(c.X(xat(y0+L*q/n)+w/2-w*rng.uniform(0.12,0.45)*math.sin(math.pi*q/n)),c.Y(y0+L*q/n)) for q in range(n,-1,-1)]
        c.d.polygon(outer+inner,fill=hexc('#6F8A28'))
        hi=[(c.X(xat(y0+L*q/n)+w/2-0.02),c.Y(y0+L*q/n)) for q in range(n//2,n+1)]+[(c.X(xat(y0+L*q/n)+w/2-w*0.12*math.sin(math.pi*q/n)),c.Y(y0+L*q/n)) for q in range(n,n//2-1,-1)]
        c.d.polygon(hi,fill=hexc('#9DB338'))
        for j in range(rng.integers(1,5)):
            hx=xat(y0)+w/2-w*rng.uniform(0.05,0.35); c.d.line([(c.X(hx),c.Y(y0+0.05)),(c.X(hx+rng.uniform(-.03,.03)),c.Y(y0-rng.uniform(0.2,0.8)))],fill=hexc('#7F9A2E'),width=2*SS)
    # moss cap on the lower trunk / roots
    c.d.polygon([(c.X(x-w/2-w*0.3),c.Y(base+0.2)),(c.X(x-w*0.1),c.Y(base+rng.uniform(0.6,1.4))),(c.X(x+w/2+w*0.3),c.Y(base+0.2))],fill=hexc('#6F8A28'))
# base ferns / moss mounds
for k in range(160):
    bx=rng.uniform(-c.w/2,c.w/2); r=rng.uniform(0.25,0.9); col=hexc(rng.choice(['#3D4B2C','#4B5D32','#5F7533','#8EA432']))
    c.d.ellipse([c.X(bx-r),c.Y(yg(22)+r*0.9),c.X(bx+r),c.Y(yg(22)-r*0.5)],fill=col)
c.d.rectangle([0,c.Y(yg(22)-0.3),c.img.width,c.img.height],fill=hexc('#2F3324'))
layout.append(c.save())
# ---------- undergrowth band (hides tree bases behind the walk strip) ----------
c=Card('fr_L4_undergrowth',3.8,96,bottom=-0.6,top=2.6)
def fern(card,x,base,h,col,hi):
    d=card.d
    for a in np.linspace(-65,65,rng.integers(5,9)):
        r=math.radians(a+rng.uniform(-8,8)); L=h*rng.uniform(0.7,1.0)
        tip=(x+math.sin(r)*L*0.8,base+math.cos(r)*L)
        d.line([(card.X(x),card.Y(base)),(card.X(tip[0]),card.Y(tip[1]))],fill=col,width=int(0.09*card.ppu*SS))
        for f in np.linspace(0.2,0.9,6):
            px,py=x+(tip[0]-x)*f,base+(tip[1]-base)*f; s=L*(1-f)*0.22
            d.polygon([(card.X(px),card.Y(py)),(card.X(px-s),card.Y(py-s*0.6)),(card.X(px+s*0.2),card.Y(py+s*0.2))],fill=col)
            d.polygon([(card.X(px),card.Y(py)),(card.X(px+s),card.Y(py-s*0.6)),(card.X(px-s*0.2),card.Y(py+s*0.2))],fill=hi if rng.random()<0.3 else col)
for k in range(150):
    bx=rng.uniform(-c.w/2,c.w/2); r=rng.uniform(0.3,0.9)
    c.d.ellipse([c.X(bx-r*1.3),c.Y(r*1.1),c.X(bx+r*1.3),c.Y(-0.3)],fill=hexc(rng.choice(['#26311F','#2E3B24','#36462A'])))
for k in range(90):
    fern(c,rng.uniform(-c.w/2,c.w/2),0.05,rng.uniform(0.6,1.6),hexc(rng.choice(['#2C3A22','#3A4B2A','#455A2E'])),hexc('#7C9530'))
for k in range(40):  # little accents: white flowers, mushrooms
    fx=rng.uniform(-c.w/2,c.w/2); fy=rng.uniform(0.05,0.5)
    if rng.random()<0.6: c.d.ellipse([c.X(fx-0.06),c.Y(fy+0.06),c.X(fx+0.06),c.Y(fy-0.06)],fill=hexc('#E8EEDC'))
    else:
        c.d.rectangle([c.X(fx-0.02),c.Y(fy+0.12),c.X(fx+0.02),c.Y(fy-0.05)],fill=hexc('#D8C9A8')); c.d.pieslice([c.X(fx-0.1),c.Y(fy+0.2),c.X(fx+0.1),c.Y(fy+0.04)],180,360,fill=hexc('#C9773A'))
c.d.rectangle([0,c.Y(0.05),c.img.width,c.img.height],fill=hexc('#2A2219'))
layout.append(c.save())
# ---------- ground strip (lies flat on the floor; image top = far edge z=3.8, bottom = z=-6.5) ----------
c=Card('fr_L5_ground',None,84,bottom=0,top=10.3,w=48)
arr=np.zeros((c.H*SS,c.W*SS,4),np.float32); yy=np.linspace(0,1,c.H*SS)[:,None]
far=np.array(hexc('#3E3A22'));mid=np.array(hexc('#3A2A1E'));near=np.array(hexc('#120D09'))
col=np.where((yy<0.35)[...,None],far*(1-yy/0.35)[...,None]+mid*(yy/0.35)[...,None],mid*(1-(yy-0.35)/0.65)[...,None]+near*((yy-0.35)/0.65)[...,None])
col=np.broadcast_to(col,(c.H*SS,c.W*SS,4)).copy()
n=rng.normal(0,1,(c.H*SS//8+1,c.W*SS//8+1)); n=np.kron(n,np.ones((8,8)))[:c.H*SS,:c.W*SS]
col[...,:3]*= (1+0.06*n[...,None]); col[...,3]=255
c.img=Image.fromarray(col.clip(0,255).astype(np.uint8)); c.d=ImageDraw.Draw(c.img)
for k in range(1800):  # leaf litter / pebbles
    lx=rng.uniform(-c.w/2,c.w/2); ly=rng.uniform(4,10.3); s=rng.uniform(0.03,0.07)
    c.d.ellipse([c.X(lx-s),c.Y(ly+s*0.5),c.X(lx+s),c.Y(ly-s*0.5)],fill=hexc(rng.choice(['#4E3826','#5A4330','#2E2117','#4A4826'])))
for k in range(900):  # moss tufts near the far edge
    lx=rng.uniform(-c.w/2,c.w/2); ly=rng.uniform(9.4,10.3); s=rng.uniform(0.05,0.18)
    c.d.ellipse([c.X(lx-s),c.Y(ly+s*0.4),c.X(lx+s),c.Y(ly-s*0.4)],fill=hexc(rng.choice(['#5E6E2A','#73862E','#4A5A26'])))
layout.append(c.save())
# ---------- foreground lip ----------
c=Card('fr_L6_fg_lip',-5.0,110,bottom=-0.4,top=2.6)
for k in range(2600):
    gx=rng.uniform(-c.w/2,c.w/2); edge=rng.random()**4  # occasional taller clumps
    h=min(1.05,rng.uniform(0.35,0.7)+edge*0.45)
    lean=rng.uniform(-0.25,0.25)
    c.d.polygon([(c.X(gx-0.035),c.Y(-0.4)),(c.X(gx+0.035),c.Y(-0.4)),(c.X(gx+lean),c.Y(h))],fill=hexc('#0B100A'))
for k in range(70):
    fern(c,rng.uniform(-c.w/2,c.w/2),0.2,rng.uniform(0.4,0.8),hexc('#0B100A'),hexc('#0B100A'))
c.d.rectangle([0,c.Y(-0.05),c.img.width,c.img.height],fill=hexc('#0B100A'))
layout.append(c.save())
json.dump(layout,open(f'{OUT}/layout.json','w'),indent=1); print(json.dumps(layout,indent=0))
