# Composite the painted cards through the SideView camera (pitch 10, FOV 19, distance 20) -> preview PNG.
import cv2,numpy as np,json,math,sys
from PIL import Image
D=sys.argv[1]; out=sys.argv[2]; cx=float(sys.argv[3]); W,H=1280,720
lay={l['name']:l for l in json.load(open(D+'/layout.json'))}
p=math.radians(10); k=(H/2)/math.tan(math.radians(9.5))
cam=np.array([cx,5.47,-19.70]); fwd=np.array([0,-math.sin(p),math.cos(p)]); up=np.array([0,math.cos(p),math.sin(p)]); right=np.array([1,0,0])
def proj(P):
    v=np.array(P,float)-cam; z=v@fwd; return [W/2+(v@right)/z*k, H/2-(v@up)/z*k]
canvas=np.zeros((H,W,3),np.float32); canvas[:]=np.array([0xB4,0xCC,0xB3])/255
def comp(img,corners_world):
    src=np.float32([[0,0],[img.shape[1],0],[img.shape[1],img.shape[0]],[0,img.shape[0]]])
    dst=np.float32([proj(c) for c in corners_world])
    M=cv2.getPerspectiveTransform(src,dst)
    w=cv2.warpPerspective(img,M,(W,H),flags=cv2.INTER_LINEAR,borderValue=(0,0,0,0)).astype(np.float32)/255
    a=w[...,3:4]; canvas[:]=canvas*(1-a)+w[...,:3]*a
def load(n): return np.array(Image.open(f'{D}/{n}.png').convert('RGBA'))
def card(n):
    l=lay[n]; hw=l['width']/2; z=l['z']
    comp(load(n),[(-hw,l['top'],z),(hw,l['top'],z),(hw,l['bottom'],z),(-hw,l['bottom'],z)])
for n in ['fr_L0_haze','fr_L1_deep_pines','fr_L2_far_trunks','fr_L3_mid_trunks','fr_rays']: card(n)
# hero trees: cut from our existing tree art preview (approximation of the real prefabs)
pv=np.array(Image.open('/mnt/project-files/forest-scene/preview-rough.png').convert('RGB')).astype(int)
def cut(x0,y0,x1,y1):
    c=pv[y0:y1,x0:x1]; bg=np.abs(c-np.array([0x33,0x2C,0x23])).sum(2)<28
    a=np.where(bg,0,255).astype(np.uint8); return np.dstack([c.astype(np.uint8),a])
oak=cut(0,150,525,690); wil=cut(1490,85,1905,655)
for img,x,z,h in [(oak,-6.5,4.72,5.5),(wil,6.5,5.31,5.6),(oak,-21.3,4.37,5.0),(wil,16.5,4.59,5.2),(oak,27.5,5.33,5.2),(oak,-27.6,4.6,5.0)]:
    wdt=h*img.shape[1]/img.shape[0]; comp(img,[(x-wdt/2,h,z),(x+wdt/2,h,z),(x+wdt/2,0,z),(x-wdt/2,0,z)])
card('fr_L4_undergrowth')
g=lay['fr_L5_ground']; hw=g['width']/2
comp(load('fr_L5_ground'),[(-hw,0,3.8),(hw,0,3.8),(hw,0,-6.5),(-hw,0,-6.5)])
# character stand-in (1.55 tall, z=0)
a=proj((cx-0.32,1.55,0)); b=proj((cx+0.32,0,0))
cv2.rectangle(canvas,(int(a[0]),int(a[1])),(int(b[0]),int(b[1])),(0.78,0.32,0.28),-1)
cv2.ellipse(canvas,(int((a[0]+b[0])/2),int(a[1])+18),(22,22),0,0,360,(0.85,0.72,0.62),-1)
card('fr_L6_fg_lip')
img=(canvas.clip(0,1)*255).astype(np.uint8)
cv2.putText(img,f'preview (python composite, not Unity)  cam x={cx:+.0f}',(10,24),cv2.FONT_HERSHEY_SIMPLEX,0.6,(255,255,255),2)
Image.fromarray(img).save(out)
