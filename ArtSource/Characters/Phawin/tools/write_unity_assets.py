# write_unity_assets.py
# Writes Phawin's Unity files from the finished sheets, using the Player_Mannequin template as the model:
#   - the two sheets with sprite-sliced .meta files (importer settings copied from the mannequin sheets)
#   - one AnimationClip per facing and state, at the fps in each sheet's json
#   - ThreeQuarter: an Animator Override Controller on Mannequin_ThreeQuarter.controller
#   - SideView: a controller with Idle and Walk blend trees, built from Mannequin_ThreeQuarter.controller
#     with the 8 children replaced by the 6 directions PlayerAnimator.SnapToSixFacings sends. The art has
#     4 rows (Front, Back, Right, Left = Right mirrored in the sheet); each diagonal uses its side row
#   - Player_Phawin.prefab, a variant of Player_Mannequin.prefab that only swaps the art
# GUIDs and sprite ids are derived from names, so running it twice writes the same files.
# This writes YAML by hand because Unity cannot run here. Open the project afterwards and let Unity
# reimport; if Unity rewrites a file, keep Unity's version.
#
# usage (from the repo root): python3 ArtSource/Characters/Phawin/tools/write_unity_assets.py
import hashlib, json, os, re, shutil

ROOT = os.getcwd()
ART = 'ArtSource/Characters/Phawin'
SPRITES = 'Assets/_Project/Art/Sprites'
MANNEQUIN_TQ_META = f'{SPRITES}/ThreeQuarter/Placeholder/Placeholder_Mannequin_ThreeQuarter.png.meta'
MANNEQUIN_SV_META = f'{SPRITES}/SideView/Placeholder/Placeholder_Mannequin_SideView.png.meta'
MANNEQUIN_TQ_CONTROLLER_GUID = '310f72fcf6eaa0744b6b9c45d2c95eeb'
MANNEQUIN_TQ_CONTROLLER = f'{SPRITES}/ThreeQuarter/Placeholder/Mannequin_ThreeQuarter.controller'
MANNEQUIN_PREFAB_GUID = '79a3c0301066cb24db61d7dd6a03c761'
# Ids inside Player_Mannequin.prefab (see that file): its Animator, its PlayerAnimator, and the Visual's
# SpriteRenderer, whose id in the variant is (instance id XOR source id) & 0x7FFFFFFFFFFFFFFF.
MANNEQUIN_ANIMATOR_ID = 7216985310307924571
MANNEQUIN_PLAYER_ANIMATOR_ID = 3492714405758422594
MANNEQUIN_SPRITE_RENDERER_ID = 4606241631460804535
MANNEQUIN_ROOT_ID = 5552632073984095805
TQ_ORDER = ['Down', 'DownRight', 'Right', 'UpRight', 'Up', 'UpLeft', 'Left', 'DownLeft']
# Mannequin_ThreeQuarter_<state>_<facing>.anim GUIDs, read from the controller's blend trees.
MANNEQUIN_TQ_CLIPS = {
    'Idle': ['02b380989de1c7a4c82ae6b843412639', '27f2a699dc25edb44acf2b40be742347',
             '6179a498bfa4ab34a9943f4b78f90efd', '7c4a04d919a97c64c958c3c38adb8011',
             'b051f10be7522374e989fc4b72af7225', '5fa6a4db8dcc8f4489031b3193f046e0',
             '111a470e19f6c5a48ba95d3c9f5c57c1', '2963e2cdb6247c4489b0316bb861112c'],
    'Walk': ['1a7fbbe351bff9744a93e1b6047c5c5f', '256cbcf130984214d93efd006a83c786',
             '14497f44661b2e2438d1c026a6beb00d', '0573b6498505c8d4faf30333491f1d80',
             '37f2e497403dcbb48b81a2263c96aa72', 'aec6b7b003f36554ba6614ae8adfc5a1',
             'c48a43e2dff33de4eb0778485802529f', 'b61411406af927f48b2d3c7bc44e69a5'],
}
# Blend-tree positions in Mannequin_ThreeQuarter.controller, in the same order as above.
# SideView sheet rows. Left is Right mirrored when the sheet is built (Oak's call), so nothing flips at runtime.
SV_ORDER = ['Front', 'Back', 'Right', 'Left']
# The FaceX/FaceY that PlayerAnimator.SnapToSixFacings sends, and the row each one shows.
SV_CHILDREN = [('Front', (0, -1)), ('Back', (0, 1)), ('Left', (-1, -1)), ('Right', (1, -1)), ('Left', (-1, 1)), ('Right', (1, 1))]
TQ_POSITIONS = [(0, -1), (0.707, -0.707), (1, 0), (0.707, 0.707), (0, 1), (-0.707, 0.707), (-1, 0), (-0.707, -0.707)]


def guid(name):
    return hashlib.md5(('phawin:' + name).encode()).hexdigest()


def int_id(name):
    value = int(hashlib.md5(('id:' + name).encode()).hexdigest()[:8], 16)
    return value - (1 << 32) if value >= (1 << 31) else value


def big_id(name):
    return int(hashlib.md5(('big:' + name).encode()).hexdigest()[:15], 16) + 1


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w', newline='\n') as handle:
        handle.write(text)


def folder_meta(path):
    write(path + '.meta', f'fileFormatVersion: 2\nguid: {guid(path)}\nfolderAsset: yes\nDefaultImporter:\n'
          '  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')


def native_meta(path, main_id):
    write(path + '.meta', f'fileFormatVersion: 2\nguid: {guid(path)}\nNativeFormatImporter:\n'
          f'  externalObjects: {{}}\n  mainObjectFileID: {main_id}\n  userData: \n'
          '  assetBundleName: \n  assetBundleVariant: \n')


# Copies a sheet into Assets and writes its .meta: the mannequin's importer settings with our pivot,
# PPU and one sprite per cell. Returns {sprite name: internal id} and the texture guid.
def import_sheet(sheet_png, info, template_meta, folder, prefix, rows, columns):
    target = f'{folder}/{prefix}.png'
    os.makedirs(folder, exist_ok=True)
    shutil.copyfile(sheet_png, target)
    texture_guid = guid(target)
    cell_w, cell_h = info['cell']
    pivot_x, pivot_y = info['pivot_normalized']
    ppu = info['ppu']
    sheet_h = cell_h * len(rows)

    sprites, table, ids = [], [], {}
    for r, facing in enumerate(rows):
        for c in range(columns):
            name = f'{prefix}_{facing}_{c}'
            ids[name] = int_id(name)
            sprites.append(
                f'    - serializedVersion: 2\n      name: {name}\n      rect:\n        serializedVersion: 2\n'
                f'        x: {c * cell_w}\n        y: {sheet_h - (r + 1) * cell_h}\n'
                f'        width: {cell_w}\n        height: {cell_h}\n      alignment: 9\n'
                f'      pivot: {{x: {pivot_x}, y: {pivot_y}}}\n      border: {{x: 0, y: 0, z: 0, w: 0}}\n'
                '      customData: \n      outline: []\n      physicsShape: []\n      tessellationDetail: 0\n'
                f'      bones: []\n      spriteID: {guid("sprite:" + name)}\n      internalID: {ids[name]}\n'
                '      vertices: []\n      indices: \n      edges: []\n      weights: []\n')
    for name in sorted(ids):
        table.append(f'      {name}: {ids[name]}\n')

    with open(template_meta) as handle:
        meta = handle.read()
    meta = re.sub(r'^guid: \w+$', f'guid: {texture_guid}', meta, count=1, flags=re.M)
    meta = re.sub(r'^  spritePivot: .*$', f'  spritePivot: {{x: {pivot_x}, y: {pivot_y}}}', meta, count=1, flags=re.M)
    meta = re.sub(r'^  spritePixelsToUnits: .*$', f'  spritePixelsToUnits: {ppu}', meta, count=1, flags=re.M)
    # The sheet's own sprite id (4-space indent) must not be the mannequin's.
    meta = re.sub(r'^    spriteID: \w+$', f'    spriteID: {guid("sheet:" + target)}', meta, count=1, flags=re.M)
    if cell_w * columns > 2048 or sheet_h > 2048:  # the mannequin default is 2048; a bigger sheet would be shrunk
        assert cell_w * columns <= 4096 and sheet_h <= 4096, 'sheet is larger than 4096, make the cells smaller'
        meta = meta.replace('maxTextureSize: 2048', 'maxTextureSize: 4096')
    start = meta.index('    sprites:\n') + len('    sprites:\n')
    end = meta.index('\n    outline: []\n', start) + 1
    meta = meta[:start] + ''.join(sprites) + meta[end:]
    start = meta.index('    nameFileIdTable:\n') + len('    nameFileIdTable:\n')
    end = meta.index('  mipmapLimitGroupName:', start)
    meta = meta[:start] + ''.join(table) + meta[end:]
    write(target + '.meta', meta)
    return ids, texture_guid


# Writes a sprite AnimationClip that shows the given sprites at fps, looping.
def write_clip(path, name, sprite_ids, texture_guid, fps):
    keys = ''.join(f'    - time: {round(i / fps, 4)}\n      value: {{fileID: {s}, guid: {texture_guid}, type: 3}}\n'
                   for i, s in enumerate(sprite_ids))
    mapping = ''.join(f'    - {{fileID: {s}, guid: {texture_guid}, type: 3}}\n' for s in sprite_ids)
    write(path, f'''%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  serializedVersion: 8
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
{keys}    attribute: m_Sprite
    path:
    classID: 212
    script: {{fileID: 0}}
    flags: 2
  m_SampleRate: {fps}
  m_WrapMode: 0
  m_Bounds:
    m_Center: {{x: 0, y: 0, z: 0}}
    m_Extent: {{x: 0, y: 0, z: 0}}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {{fileID: 0}}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
      metaData: 0
    pptrCurveMapping:
{mapping}  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {{fileID: 0}}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: {round(len(sprite_ids) / fps, 4)}
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 1
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
''')
    native_meta(path, 7400000)


HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
OBJECT = '  m_ObjectHideFlags: 1\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'


def state_yaml(state_id, name, clip_guid, transition_ids, x, y):
    transitions = ''.join(f'  - {{fileID: {t}}}\n' for t in transition_ids)
    return (f'--- !u!1102 &{state_id}\nAnimatorState:\n  serializedVersion: 6\n{OBJECT}  m_Name: {name}\n'
            f'  m_Speed: 1\n  m_CycleOffset: 0\n  m_Transitions:\n{transitions}  m_StateMachineBehaviours: []\n'
            f'  m_Position: {{x: {x}, y: {y}, z: 0}}\n  m_IKOnFeet: 0\n  m_WriteDefaultValues: 1\n  m_Mirror: 0\n'
            '  m_SpeedParameterActive: 0\n  m_MirrorParameterActive: 0\n  m_CycleOffsetParameterActive: 0\n'
            f'  m_TimeParameterActive: 0\n  m_Motion: {{fileID: 7400000, guid: {clip_guid}, type: 2}}\n  m_Tag: \n'
            '  m_SpeedParameter: \n  m_MirrorParameter: \n  m_CycleOffsetParameter: \n  m_TimeParameter: \n')


# conditions: [(mode, parameter, threshold)], all must hold.
# mode: 1 = bool true, 2 = bool false, 3 = greater than, 4 = less than.
def transition_yaml(transition_id, conditions, destination_id):
    lines = ''.join(f'  - m_ConditionMode: {mode}\n    m_ConditionEvent: {parameter}\n    m_EventTreshold: {threshold}\n'
                    for mode, parameter, threshold in conditions)
    return (f'--- !u!1101 &{transition_id}\nAnimatorStateTransition:\n{OBJECT}  m_Name: \n  m_Conditions:\n'
            f'{lines}  m_DstStateMachine: {{fileID: 0}}\n  m_DstState: {{fileID: {destination_id}}}\n  m_Solo: 0\n'
            '  m_Mute: 0\n  m_IsExit: 0\n  serializedVersion: 3\n  m_TransitionDuration: 0\n'
            '  m_TransitionOffset: 0\n  m_ExitTime: 0.9\n  m_HasExitTime: 0\n  m_HasFixedDuration: 1\n'
            '  m_InterruptionSource: 0\n  m_OrderedInterruption: 1\n  m_CanTransitionToSelf: 0\n')


# SideView controller: the mannequin ThreeQuarter controller (Idle and Walk blend trees on FaceX/FaceY,
# IsMoving switches between them) with each blend tree's children replaced by the 6 SideView directions.
# Local file ids inside a controller only need to be unique within that file, so the template's are kept.
def write_sideview_controller(path, clip_guids):
    with open(MANNEQUIN_TQ_CONTROLLER) as handle:
        text = handle.read()
    for state in ('Idle', 'Walk'):
        children = ''
        for i, (facing, (x, y)) in enumerate(SV_CHILDREN):
            children += (f'  - serializedVersion: 2\n'
                         f'    m_Motion: {{fileID: 7400000, guid: {clip_guids[f"{state}_{facing}"]}, type: 2}}\n'
                         f'    m_Threshold: {round(i / (len(SV_CHILDREN) - 1), 4)}\n    m_Position: {{x: {x}, y: {y}}}\n'
                         '    m_TimeScale: 1\n    m_CycleOffset: 0\n    m_DirectBlendParameter: Blend\n    m_Mirror: 0\n')
        start = text.index(f'  m_Name: {state}\n  m_Childs:\n') + len(f'  m_Name: {state}\n  m_Childs:\n')
        end = text.index('  m_BlendParameter: FaceX\n', start)
        text = text[:start] + children + text[end:]
    name = os.path.splitext(os.path.basename(path))[0]
    text = text.replace('  m_Name: Mannequin_ThreeQuarter\n', f'  m_Name: {name}\n', 1)
    assert 'guid: ' in text and text.count('m_Position: {x: 0.707') == 0
    write(path, text)
    native_meta(path, 9100000)


def write_override_controller(path, base_guid, pairs):
    clips = ''.join(f'  - m_OriginalClip: {{fileID: 7400000, guid: {a}, type: 2}}\n'
                    f'    m_OverrideClip: {{fileID: 7400000, guid: {b}, type: 2}}\n' for a, b in pairs)
    name = os.path.splitext(os.path.basename(path))[0]
    write(path, HEADER + '--- !u!221 &22100000\nAnimatorOverrideController:\n  m_ObjectHideFlags: 0\n'
          '  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
          f'  m_Name: {name}\n  m_Controller: {{fileID: 9100000, guid: {base_guid}, type: 2}}\n  m_Clips:\n{clips}')
    native_meta(path, 22100000)


def write_prefab_variant(path, tq_controller_guid, sv_controller_guid, first_sprite_id, first_sprite_guid):
    instance = big_id(path + 'instance')

    def modification(target, prop, value='', reference='{fileID: 0}'):
        return (f'    - target: {{fileID: {target}, guid: {MANNEQUIN_PREFAB_GUID}, type: 3}}\n'
                f'      propertyPath: {prop}\n      value: {value}\n      objectReference: {reference}\n')

    tq_reference = f'{{fileID: 22100000, guid: {tq_controller_guid}, type: 2}}'
    sv_reference = f'{{fileID: 9100000, guid: {sv_controller_guid}, type: 2}}'
    sprite_reference = f'{{fileID: {first_sprite_id}, guid: {first_sprite_guid}, type: 3}}'
    mods = (modification(MANNEQUIN_ROOT_ID, 'm_Name', 'Player_Phawin')
            + modification(MANNEQUIN_ANIMATOR_ID, 'm_Controller', reference=tq_reference)
            + modification(MANNEQUIN_PLAYER_ANIMATOR_ID, 'threeQuarterController', reference=tq_reference)
            + modification(MANNEQUIN_PLAYER_ANIMATOR_ID, 'sideViewController', reference=sv_reference)
            + modification(MANNEQUIN_SPRITE_RENDERER_ID, 'm_Sprite', reference=sprite_reference))
    write(path, HEADER + f'--- !u!1001 &{instance}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n'
          '  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {fileID: 0}\n'
          f'    m_Modifications:\n{mods}    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n'
          '    m_AddedGameObjects: []\n    m_AddedComponents: []\n'
          f'  m_SourcePrefab: {{fileID: 100100000, guid: {MANNEQUIN_PREFAB_GUID}, type: 3}}\n')
    write(path + '.meta', f'fileFormatVersion: 2\nguid: {guid(path)}\nPrefabImporter:\n  externalObjects: {{}}\n'
          '  userData: \n  assetBundleName: \n  assetBundleVariant: \n')


def main():
    # ThreeQuarter
    tq_folder = f'{SPRITES}/ThreeQuarter/Characters/Phawin'
    tq_info = json.load(open(f'{ART}/threequarter-game/Phawin_ThreeQuarter.json'))
    tq_ids, tq_guid = import_sheet(f'{ART}/threequarter-game/Phawin_ThreeQuarter.png', tq_info, MANNEQUIN_TQ_META,
                                   tq_folder, 'Phawin_ThreeQuarter', TQ_ORDER, tq_info['columns'])
    pairs = []
    for i, facing in enumerate(TQ_ORDER):
        for state in ('Idle', 'Walk'):
            columns = [0] if state == 'Idle' else range(1, tq_info['columns'])
            clip = f'{tq_folder}/Animations/Phawin_ThreeQuarter_{state}_{facing}.anim'
            write_clip(clip, os.path.basename(clip)[:-5],
                       [tq_ids[f'Phawin_ThreeQuarter_{facing}_{c}'] for c in columns], tq_guid, tq_info['fps'])
            pairs.append((MANNEQUIN_TQ_CLIPS[state][i], guid(clip)))
    tq_controller = f'{tq_folder}/Phawin_ThreeQuarter.overrideController'
    write_override_controller(tq_controller, MANNEQUIN_TQ_CONTROLLER_GUID, pairs)

    # SideView
    sv_folder = f'{SPRITES}/SideView/Characters/Phawin'
    if os.path.isdir(f'{sv_folder}/Animations'):  # clips from an older layout (e.g. Right/Left) must not linger
        for old in os.listdir(f'{sv_folder}/Animations'):
            os.remove(f'{sv_folder}/Animations/{old}')
    sv_info = json.load(open(f'{ART}/sideview-game/Phawin_SideView.json'))
    assert sv_info['rows_in_order'] == SV_ORDER, sv_info['rows_in_order']
    sv_ids, sv_guid = import_sheet(f'{ART}/sideview-game/Phawin_SideView.png', sv_info, MANNEQUIN_SV_META,
                                   sv_folder, 'Phawin_SideView', SV_ORDER, sv_info['columns'])
    sv_clips = {}
    for facing in SV_ORDER:
        for state in ('Idle', 'Walk'):
            columns = [0] if state == 'Idle' else range(1, sv_info['columns'])
            clip = f'{sv_folder}/Animations/Phawin_SideView_{state}_{facing}.anim'
            write_clip(clip, os.path.basename(clip)[:-5], [sv_ids[f'Phawin_SideView_{facing}_{c}'] for c in columns], sv_guid, sv_info['fps'])
            sv_clips[f'{state}_{facing}'] = guid(clip)
    sv_controller = f'{sv_folder}/Phawin_SideView.controller'
    write_sideview_controller(sv_controller, sv_clips)

    for folder in [f'{SPRITES}/ThreeQuarter/Characters', tq_folder, f'{tq_folder}/Animations',
                   f'{SPRITES}/SideView/Characters', sv_folder, f'{sv_folder}/Animations']:
        folder_meta(folder)

    write_prefab_variant('Assets/_Project/Prefabs/Player_Phawin.prefab', guid(tq_controller), guid(sv_controller),
                         tq_ids['Phawin_ThreeQuarter_Down_0'], tq_guid)
    print('done')


main()
