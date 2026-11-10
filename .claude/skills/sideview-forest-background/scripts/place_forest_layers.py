# Put the painted reference-style layers into Test_Forest.unity (YAML) and set up textures + metas.
import re,json,uuid,shutil,os,sys
REPO='/home/claude/ProjectJunNiparn'; SRC=sys.argv[1]
ART='Assets/_Project/Art/Backgrounds/forest_ref'; SCN=f'{REPO}/Assets/_Project/Scenes/Test/Test_Forest.unity'
os.makedirs(f'{REPO}/{ART}',exist_ok=True)
lay=json.load(open(f'{SRC}/layout.json'))
tmpl=open(f'{REPO}/Assets/_Project/Art/Backgrounds/forest/layer_far.png.meta').read()
open(f'{REPO}/{ART}/.gitattributes','w').write('# Painted test layers kept in plain git (LFS upload is blocked from cloud sessions).\n*.png -filter -diff -merge binary\n')
if not os.path.exists(f'{REPO}/{ART}.meta'):
    open(f'{REPO}/{ART}.meta','w').write(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')
guids={}
for l in lay:
    n=l['name']; shutil.copy(f'{SRC}/{n}.png',f'{REPO}/{ART}/{n}.png')
    mp=f'{REPO}/{ART}/{n}.png.meta'
    g=re.search(r'guid: (\w+)',open(mp).read()).group(1) if os.path.exists(mp) else uuid.uuid4().hex
    m=tmpl.replace('guid: d6c9edfc03e0d7e4ab7e0d457f1bbcb0',f'guid: {g}')
    m=re.sub(r'spritePixelsToUnits: \d+',f"spritePixelsToUnits: {l['ppu']}",m)
    m=re.sub(r'(\n  alignment:) \d+',r'\1 7',m); m=m.replace('spritePivot: {x: 0.5, y: 0.5}','spritePivot: {x: 0.5, y: 0}')
    m=re.sub(r'maxTextureSize: \d+','maxTextureSize: 4096',m)
    m=re.sub(r'textureCompression: \d+','textureCompression: 0',m)
    m=m.replace('spriteMeshType: 1','spriteMeshType: 0').replace('wrapU: 1','wrapU: 1')
    m=re.sub(r'spriteID: \w+',f'spriteID: {uuid.uuid4().hex}',m)
    open(mp,'w').write(m); guids[n]=g
t=open(SCN).read()
# --- deactivate old rows except Near, hide the far floor mesh (collider stays)
for go in ['9300010','9300270','9300500','9300780','9301030']:
    t=re.sub(r'(--- !u!1 &'+go+r'\n(?:.*\n)*?  m_IsActive: )1',r'\g<1>0',t,count=1)
t=re.sub(r'(--- !u!23 &2122825427\n(?:.*\n)*?  m_Enabled: )1',r'\g<1>0',t,count=1)
# --- Near row: reposition hero trees, hide the clutter in the middle, brighten
docs=re.split(r'(\n(?=--- !u!))',t); out=[]
moves={'OakBig_4':-6.5,'Willow_10':6.5}; hide={'OakRound_5','Banana_6','OakBig_7','OakRound_8'}
for d in docs:
    if d.startswith('--- !u!1001') and 'm_TransformParent: {fileID: 9300661}' in d:
        nm=re.search(r'propertyPath: m_Name\n\s+value: (\S+)',d).group(1)
        goid=re.search(r'target: \{fileID: (\d+), guid: (\w+), type: 3\}\n\s+propertyPath: m_Name',d)
        if nm in moves: d=re.sub(r'(propertyPath: m_LocalPosition\.x\n\s+value: )[^\n]*',lambda mm:mm.group(1)+str(moves[nm]),d,count=1)
        if nm in hide and 'propertyPath: m_IsActive' not in d:
            d=d.replace('    m_RemovedComponents:',f'    - target: {{fileID: {goid.group(1)}, guid: {goid.group(2)}, type: 3}}\n      propertyPath: m_IsActive\n      value: 0\n      objectReference: {{fileID: 0}}\n    m_RemovedComponents:',1)
        for c,v in zip('rgb',(0.95,0.97,0.9)):
            d=re.sub(r'(propertyPath: m_Color\.'+c+r'\n\s+value: )[^\n]*',lambda mm:mm.group(1)+str(v),d,count=1)
    out.append(d)
t=''.join(out)
t=re.sub(r'm_BackGroundColor: \{[^}]*\}','m_BackGroundColor: {r: 0.706, g: 0.8, b: 0.702, a: 1}',t)
# --- new GameObjects
if 'm_Name: ForestRef' in t: sys.exit('ForestRef already present - run on a clean scene')
SR=re.search(r'(--- !u!212 &1527384602\n(?:.*\n)*?  m_BlendShapeWeights: \[\]\n)',t).group(1)
order={'fr_L0_haze':-60,'fr_L1_deep_pines':-50,'fr_L2_far_trunks':-40,'fr_L3_mid_trunks':-30,'fr_rays':-25,'fr_L4_undergrowth':-20,'fr_L5_ground':-15,'fr_L6_fg_lip':10}
def go(i,name,comps,active=1):
    cl=''.join(f'  - component: {{fileID: {c}}}\n' for c in comps)
    return f'--- !u!1 &{i}\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  serializedVersion: 6\n  m_Component:\n{cl}  m_Layer: 0\n  m_Name: {name}\n  m_TagString: Untagged\n  m_Icon: {{fileID: 0}}\n  m_NavMeshLayer: 0\n  m_StaticEditorFlags: 0\n  m_IsActive: {active}\n'
def tr(i,goi,pos,rot,children,father):
    ch='\n'+''.join(f'  - {{fileID: {c}}}\n' for c in children) if children else ' []\n'
    return f'--- !u!4 &{i}\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {{fileID: 0}}\n  m_PrefabInstance: {{fileID: 0}}\n  m_PrefabAsset: {{fileID: 0}}\n  m_GameObject: {{fileID: {goi}}}\n  serializedVersion: 2\n  m_LocalRotation: {{x: {rot[0]}, y: 0, z: 0, w: {rot[1]}}}\n  m_LocalPosition: {{x: {pos[0]}, y: {pos[1]}, z: {pos[2]}}}\n  m_LocalScale: {{x: 1, y: 1, z: 1}}\n  m_ConstrainProportionsScale: 0\n  m_Children:{ch}  m_Father: {{fileID: {father}}}\n  m_LocalEulerAnglesHint: {{x: {90 if rot[0] else 0}, y: 0, z: 0}}\n'
ROOT=9400000; blocks=[]; kids=[]
for j,l in enumerate(lay):
    gi=9400010+j*10; ti=gi+1; si=gi+2; kids.append(ti)
    if l['name']=='fr_L5_ground': pos,rot=(0,0.01,-6.5),(0.70710677,0.70710677)
    else: pos,rot=(0,l['bottom'],l['z']),(0,1)
    blocks.append(go(gi,l['name'],[ti,si])); blocks.append(tr(ti,gi,pos,rot,[],ROOT+1))
    s=SR.replace('--- !u!212 &1527384602',f'--- !u!212 &{si}').replace('m_GameObject: {fileID: 1527384600}',f'm_GameObject: {{fileID: {gi}}}')
    s=s.replace('guid: d6c9edfc03e0d7e4ab7e0d457f1bbcb0',f"guid: {guids[l['name']]}")
    s=re.sub(r'm_Size: \{[^}]*\}',f"m_Size: {{x: {l['px'][0]/l['ppu']:.4f}, y: {l['px'][1]/l['ppu']:.4f}}}",s)
    s=re.sub(r'm_SortingOrder: -?\d+',f"m_SortingOrder: {order[l['name']]}",s)
    blocks.append(s)
blocks.insert(0,go(ROOT,'ForestRef',[ROOT+1])); blocks.insert(1,tr(ROOT+1,ROOT,(0,0,0),(0,1),kids,470509011))
t=re.sub(r'(--- !u!4 &470509011\n(?:.*\n)*?  m_Children:\n(?:  - \{fileID: \d+\}\n)*)',lambda mm:mm.group(1)+f'  - {{fileID: {ROOT+1}}}\n',t,count=1)
# insert new docs before SceneRoots (or at end)
ins=''.join(blocks)
if '--- !u!1660057539 &9223372036854775807' in t:
    t=t.replace('--- !u!1660057539 &9223372036854775807',ins+'--- !u!1660057539 &9223372036854775807',1)
    t=re.sub(r'(SceneRoots:\n(?:.*\n)*?  m_Roots:\n)',r'\1',t)
else: t=t+ins
open(SCN,'w').write(t); print('ok', {k:v for k,v in guids.items()})
