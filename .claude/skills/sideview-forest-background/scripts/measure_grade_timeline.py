# Usage: python3 measure_grade_timeline.py <clip.mp4> <y0> <y1> <step seconds>
# Prints mean colour, L* (mean/p10/p90) and a*/b* of the play area over time, to measure time-of-day grading.
import cv2,numpy as np,sys
cap=cv2.VideoCapture(sys.argv[1]); fps=cap.get(5); n=int(cap.get(7)); y0,y1=int(sys.argv[2]),int(sys.argv[3])
for t in np.arange(0,n/fps,float(sys.argv[4])):
    cap.set(1,int(t*fps)); ok,f=cap.read()
    if not ok: break
    p=f[y0:y1].reshape(-1,3).astype(float)  # BGR
    lab=cv2.cvtColor(f[y0:y1],cv2.COLOR_BGR2LAB).reshape(-1,3).astype(float)
    L=lab[:,0]*100/255; A=lab[:,1]-128; B=lab[:,2]-128
    b,g,r=p.mean(0)
    print(f"{t:5.1f}s meanRGB #{int(r):02x}{int(g):02x}{int(b):02x}  L mean {L.mean():4.1f} p10 {np.percentile(L,10):4.1f} p90 {np.percentile(L,90):4.1f}  a* {A.mean():5.1f} b* {B.mean():5.1f}  (b*<0 = bluer)")
