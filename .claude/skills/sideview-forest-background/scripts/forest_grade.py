# Usage: python3 forest_grade.py <in.unity> <out.unity> <preset.json>
# Presets live in ../presets/. Only recolours tree instances under the Forest_* rows and sets fog + camera background.
# Re-grade Test_Forest rows + fog/background in the scene YAML (keeps each tree's small colour jitter).
import re,sys,json
src,dst,cfg=sys.argv[1],sys.argv[2],json.load(open(sys.argv[3]))
t=open(src).read()
parents={'9300011':'Deep','9300271':'Far','9300501':'Mid','9300661':'Near','9300781':'Under','9301031':'Fore'}
docs=re.split(r'(\n(?=--- !u!))',t)
# pass 1: row means
import collections; acc=collections.defaultdict(list)
def cols(d): return [float(re.search(r'propertyPath: m_Color\.'+c+r'\n\s+value: ([^\n]*)',d).group(1)) for c in 'rgb']
for d in docs:
    m=re.search(r'm_TransformParent: \{fileID: (\d+)\}',d)
    if d.startswith('--- !u!1001') and m and m.group(1) in parents: acc[parents[m.group(1)]].append(cols(d))
mean={k:[sum(c[i] for c in v)/len(v) for i in range(3)] for k,v in acc.items()}
out=[];n=0
for d in docs:
    m=re.search(r'm_TransformParent: \{fileID: (\d+)\}',d)
    if d.startswith('--- !u!1001') and m and m.group(1) in parents:
        row=parents[m.group(1)]; old=cols(d); tint=cfg['rows'][row]; j=cfg.get('jitter',1.0)
        for i,c in enumerate('rgb'):
            v=max(0,min(1,tint[i]+(old[i]-mean[row][i])*j))
            d=re.sub(r'(propertyPath: m_Color\.'+c+r'\n\s+value: )[^\n]*',lambda mm:mm.group(1)+f'{v:.3f}',d,count=1)
        n+=1
    out.append(d)
t=''.join(out)
f=cfg['fog']; t=re.sub(r'm_FogColor: \{[^}]*\}',f'm_FogColor: {{r: {f[0]}, g: {f[1]}, b: {f[2]}, a: 1}}',t,count=1)
t=re.sub(r'm_LinearFogStart: [\d.]+',f"m_LinearFogStart: {cfg['fogStart']}",t,count=1)
t=re.sub(r'm_LinearFogEnd: [\d.]+',f"m_LinearFogEnd: {cfg['fogEnd']}",t,count=1)
b=cfg['background']; c=t.count('m_BackGroundColor')
t=re.sub(r'm_BackGroundColor: \{[^}]*\}',f'm_BackGroundColor: {{r: {b[0]}, g: {b[1]}, b: {b[2]}, a: 1}}',t)
open(dst,'w').write(t); print('recoloured',n,'trees; cameras with background',c)
