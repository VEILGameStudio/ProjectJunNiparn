# Usage: python3 measure_camera_moves.py <clip.mp4>   (needs opencv-python-headless)
# Finds each camera move (zoom/pan) in a gameplay capture: start/end, duration, total zoom, pan in px, where peak speed falls (ease shape).
# Mask rows 140..540 assume a 1280x720 capture with UI outside that band; adjust for other clips.
import cv2,numpy as np,sys
cap=cv2.VideoCapture(sys.argv[1]); fps=cap.get(5); orb=cv2.ORB_create(3000); bf=cv2.BFMatcher(cv2.NORM_HAMMING,crossCheck=True)
prev=None;i=0;rec=[]
while True:
    ok,f=cap.read()
    if not ok: break
    g=cv2.cvtColor(f,cv2.COLOR_BGR2GRAY)
    if i%2==0:
        mask=np.zeros_like(g); mask[140:540,20:-20]=255
        k,d=orb.detectAndCompute(g,mask)
        if prev is not None and d is not None:
            m=bf.match(prev[1],d)
            a=np.float32([prev[0][x.queryIdx].pt for x in m]); b=np.float32([k[x.trainIdx].pt for x in m])
            M,inl=cv2.estimateAffinePartial2D(a,b,ransacReprojThreshold=1.5)
            if M is not None and inl.sum()>150:
                rec.append((i/fps,np.log(np.hypot(M[0,0],M[1,0])),M[0,2],M[1,2],int(inl.sum())))
            else: rec.append((i/fps,0,0,0,0))
        prev=(k,d)
    i+=1
rec=np.array(rec)
moving=(np.abs(rec[:,1])>0.002)|(np.abs(rec[:,2])>2)
segs=[];s=None
for j,mv in enumerate(moving):
    if mv and s is None: s=j
    if not mv and s is not None:
        if j-s>=4: segs.append((s,j))
        s=None
for s,e in segs:
    r=rec[s:e]; z=np.exp(r[:,1].sum()); dur=r[-1,0]-r[0,0]+2/fps
    sp=np.abs(r[:,1])+np.hypot(r[:,2],r[:,3])/1000; pk=(np.argmax(sp)+0.5)/len(sp)
    print(f"{r[0,0]:6.2f}-{r[-1,0]:6.2f}s dur {dur:4.2f}s zoom x{z:5.3f} pan dx {r[:,2].sum():6.0f}px dy {r[:,3].sum():5.0f}px peak-speed at {pk*100:3.0f}% of move min-inliers {int(r[:,4].min())}")
