Forest BG layers from Test_Forest (object group "ForestRef"), exported 2026-11-11.
Source files in Unity: Assets/_Project/Art/Backgrounds/forest_ref/
The PNGs here are byte-for-byte copies of those sources (original size, transparency kept).

Order is back (01) to front (08). z = distance setting in the scene, PPU = Pixels Per Unit.

file               source in Unity          size (px)    z      PPU
01_haze            fr_L0_haze.png           2849 x 787   180    16
02_deep_pines      fr_L1_deep_pines.png     3120 x 845   100    27
03_far_trunks      fr_L2_far_trunks.png     3483 x 911    45    48
04_mid_trunks      fr_L3_mid_trunks.png     3930 x 996    22    72
05_light_rays      fr_rays.png              1160 x 289    14    24
06_undergrowth     fr_L4_undergrowth.png    3875 x 307     3.8  96
07_fg_lip          fr_L6_fg_lip.png         3683 x 330    -5   110
08_ground          fr_L5_ground.png         4032 x 865    -6.5  84  (lies flat on the floor, rotated 90 on X)

Forest_BG_Layers.psd: all 8 layers, same order, each at its original size,
centred and bottom-aligned on a 4032 x 996 canvas. The ground layer is hidden
because in the game it lies flat on the floor, not upright.
This stacking is only for painting: in the game every layer is at a different distance,
so they do not line up on screen the way they do in the PSD.

To swap a repainted layer back in: keep the same pixel size (and the same PPU in Unity),
save over the matching file in forest_ref/, and the scene picks it up with no other change.
