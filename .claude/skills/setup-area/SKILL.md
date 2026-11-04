---
name: setup-area
description: Use when a teammate wants a new level, room, corridor or camera zone, wants to connect areas with a door, or says the camera looks wrong in an area.
---

# Set up an area

Spec: CLAUDE.md §1, §7 "Camera", "Camera zones", "Level settings", §11 Cinemachine.

## 1. Pick the preset — there are only two
| | `ThreeQuarter` | `SideView` |
|---|---|---|
| For | puzzle rooms, real exploration | corridors, a little puzzling, a path |
| Walkable depth | whole floor | about 3 units |
| Parallax | impossible (orthographic) | by placing layers at distance, no script |

A request for "a bit different angle" is a third preset → stop, `design-change-proposal`.

## 2. Build
1. One `LevelSettings` per area: choose the preset dropdown, **draw the play area** (not the camera box).
   Zoom/FOV/distance/damping/bounds may be overridden; pitch and yaw cannot.
2. One `CinemachineCamera` per area following the player. Never touch its values by hand.
3. Several areas in one scene: each is a `CameraZone` root (trigger box, `Ignore Raycast` layer) with a
   **Content** child holding its `LevelSettings`, camera, floor, walls, props. One `CameraZoneSwitcher` per scene.
4. Main Camera's `CinemachineBrain`: **Default Blend = Cut** in any scene with zones; tick **Lens Mode
   Override** when the scene uses a projection the camera does not start in.
5. Zones of different presets never see each other — join them with a door or a turn.
6. Doors between scenes: target scene + spawn point id through `SceneLoader`. Inside a scene: `ZoneDoor`.

## 3. Sizes that must survive 21:9
Back walls 36 wide; size far walls for the far plane plus camera travel; UI panels ≤ 935 canvas units tall.

## 4. Camera looks wrong?
Read the preset values and compare the camera's real position before changing anything — the confiner
hides a broken camera by clamping it to a corner. Never fix by moving the camera by hand.

## 5. Never
DoF in `ThreeQuarter`; writing `Camera.main.transform` or `Camera.main.orthographic`; opening someone
else's scene for editing (`123.unity`, `Test_3D.unity`).

Hand back with CLAUDE.md §10, in Thai.
