"""Clean, artist-style layer split of one flat character frame (v2).

- defringes the light-grey background halo (alpha unmixing)
- cuts only along natural edges (hair/face, sleeve hem, trouser hem, bag edge)
- shared outlines belong to the layer on top, so no outline is broken in two
- works at SCALE x with smoothed cut lines, flat-colour fill under covered areas,
  and a closing outline on every cut edge that is hidden or already sits on a line

Usage: python3 split_layers.py frame.png cfg.json out_dir

The palette and background colour can be overridden per character with "palette" and "bg" in cfg.
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi

BG = np.array([245., 245., 245.])
PAL = {"skin": (243, 218, 213), "shirt": (221, 199, 175), "brown": (157, 118, 100),
       "pants": (47, 62, 80), "sash": (58, 85, 104), "sandal": (74, 55, 47),
       "eye": (70, 55, 48), "dark": (38, 40, 42), "gray": (112, 106, 100), "lgray": (170, 160, 150)}
LINE = np.array([40, 38, 40.])


def poly(sh, pts, ox, oy, s=1):
    m = Image.new("L", (sh[1], sh[0]), 0)
    ImageDraw.Draw(m).polygon([((x - ox) * s, (y - oy) * s) for x, y in pts], fill=255)
    return np.array(m) > 0


def main(src, cfgp, out):
    global BG, PAL
    cfg = json.load(open(cfgp))
    if "bg" in cfg: BG = np.array(cfg["bg"], float)
    if "palette" in cfg: PAL = {k: tuple(v) for k, v in cfg["palette"].items()}
    S = cfg.get("scale", 4)
    X0, Y0, X1, Y1 = cfg["crop"]
    img = np.array(Image.open(src).convert("RGB")).astype(float)[Y0:Y1, X0:X1]
    sh = img.shape[:2]
    rows = np.arange(sh[0])[:, None] + Y0 + np.zeros(sh)
    cols = np.arange(sh[1])[None, :] + X0 + np.zeros(sh)
    P = lambda k: poly(sh, cfg["polys"][k], X0, Y0)
    box = lambda b: (cols >= b[0]) & (cols <= b[2]) & (rows >= b[1]) & (rows <= b[3])

    # ---- 1. alpha unmixing against the flat background ----
    dbg = np.abs(img - BG).sum(2)
    core = dbg > 30
    filled = ndi.binary_fill_holes(core)
    holes = filled & ~core & (dbg < 8)
    lab, nl = ndi.label(holes)
    sizes = ndi.sum(holes, lab, range(1, nl + 1))
    big = np.isin(lab, [i + 1 for i, z in enumerate(sizes) if z > 12])
    solid = ndi.binary_erosion(filled & ~big, iterations=1) & (dbg > 30)
    _, (iy, ix) = ndi.distance_transform_edt(~solid, return_indices=True)
    F = img[iy, ix]
    num = ((img - BG) * (F - BG)).sum(2)
    den = ((F - BG) ** 2).sum(2) + 1e-6
    alpha = np.clip(num / den, 0, 1)
    alpha[solid] = 1
    alpha[dbg < 10] = 0
    col = np.where(alpha[..., None] > 0.999, img, F)  # edge pixels take the solid colour: no halo
    opaque = alpha > 0.05

    # ---- 2. material per pixel ----
    names = list(PAL); pal = np.array([PAL[n] for n in names], float)
    dist = ((col[:, :, None] - pal[None, None]) ** 2).sum(-1)
    mat = np.array(names)[dist.argmin(-1)]
    lum = col.mean(2)
    feet_zone = (np.arange(sh[0])[:, None] + Y0 + np.zeros(sh)) >= cfg["foot_y"]
    sat = col.max(2) - col.min(2)
    dark = ((lum < 120) & ~np.isin(mat, ["pants", "sash"]) & ~(feet_zone & (mat == "sandal"))) | ((sat < 26) & (lum < 185))
    dark |= np.isin(mat, ["gray", "lgray"])

    if cfg.get("generic"):
        L = np.full(sh, "", object)
        head = P("head") & opaque
        L[head] = "head"
        bodyz = opaque & ~head
        L[bodyz & ~dark] = "torso"
        L[bodyz & ~dark & (mat == "skin") & box(cfg["boxes"]["neck"])] = "neck" if "neck" in cfg["order"] else "torso"
        L[bodyz & ~dark & (mat == "pants")] = "hip"
        for lg in ["leg_SL", "leg_SR"]:
            L[bodyz & ~dark & np.isin(mat, ["pants", "sandal", "skin", "brown"]) & (rows >= cfg["hip_y"]) & P(lg)] = lg
        for pr in cfg["parts"]:
            m = bodyz & ~dark & P(pr["poly"])
            if "mats" in pr: m &= np.isin(mat, pr["mats"])
            if "not_mats" in pr: m &= ~np.isin(mat, pr["not_mats"])
            if "ymin" in pr: m &= rows >= pr["ymin"]
            L[m] = pr["name"]
    else:
        # ---- 3. label non-line pixels by natural regions ----
        L = np.full(sh, "", object)
        head = box(cfg["head_box"]) & opaque
        hull = P("face")
        hull_in = ndi.binary_erosion(hull, iterations=2)
        skin = (mat == "skin") | (lum > 200)
        # head
        L[head & skin] = "face"
        L[head & ~skin & hull & ~box(cfg["boxes"]["eye_R"]) & ~box(cfg["boxes"]["eye_L"])] = "hair_front"
        L[head & ~skin & ~hull] = "hair_back"
        for e in ["eye_R", "eye_L", "mouth"]:
            L[head & ~skin & (lum < 150) & box(cfg["boxes"][e])] = e
        near_skin = ndi.binary_dilation(head & skin, iterations=2)
        chin = head & ~skin & near_skin & hull & ~hull_in & (rows >= cfg["chin_y"])
        L[chin] = "face"
        ring = head & ~skin & ndi.binary_dilation(head & skin, iterations=2) & ~box(cfg["boxes"]["eye_R"]) & ~box(cfg["boxes"]["eye_L"])
        L[ring & (rows >= cfg["chin_y"])] = "face"
        # body
        bodyz = opaque & ~head
        armR, armL = P("arm_R"), P("arm_L")
        skin = (mat == "skin")
        L[bodyz & ~dark] = "torso"
        L[bodyz & ~dark & skin & box(cfg["boxes"]["neck"])] = "neck"
        L[bodyz & ~dark & np.isin(mat, ["pants"])] = "hip"
        L[bodyz & ~dark & np.isin(mat, ["pants", "sandal"]) & (rows >= cfg["hip_y"]) & P("leg_R")] = "leg_R"
        L[bodyz & ~dark & np.isin(mat, ["pants", "sandal"]) & (rows >= cfg["hip_y"]) & P("leg_L")] = "leg_L"
        L[bodyz & ~dark & (rows >= cfg["foot_y"]) & ~np.isin(mat, ["pants"])] = "foot"
        L[bodyz & ~dark & (mat == "sash") & P("sash")] = "sash"
        for a, m in [("R", armR), ("L", armL)]:
            hem = cfg["hem_y"][a]
            L[bodyz & ~dark & m & (rows < hem) & ~np.isin(mat, ["brown", "pants", "sash"])] = "upperarm_" + a
            L[bodyz & ~dark & m & (rows >= hem) & ~np.isin(mat, ["pants", "sash", "brown"])] = "arm_" + a
        L[bodyz & ~dark & P("bag") & np.isin(mat, ["brown", "eye", "sandal", "gray"])] = "bag"
        neck_box = box(cfg["boxes"]["necklace"])
        L[bodyz & dark & neck_box & ~ndi.binary_dilation(armR | armL, iterations=1)] = "necklace"
        foot = L == "foot"
        L[foot & (cols < cfg["mid_x"])] = "foot_R"
        L[foot & (cols >= cfg["mid_x"])] = "foot_L"

    hullT = ndi.binary_dilation(poly(sh, cfg["full"]["torso"][0], X0, Y0), iterations=1)
    L[(L == "torso") & ~hullT] = ""
    order = cfg["order"]
    rank = {n: i for i, n in enumerate(order)}

    # ---- 4. line pixels go to the highest-ranked neighbouring part ----
    todo = opaque & (L == "")
    for r in (1, 2, 3):
        best = np.full(sh, -1)
        for n in order:
            m = ndi.binary_dilation(L == n, iterations=r)
            best = np.where(m & todo & (rank[n] > best), rank[n], best)
        hit = todo & (best >= 0)
        L[hit] = np.array(order, object)[best[hit]]
        todo &= ~hit
    if todo.any():
        known = L != ""
        _, (jy, jx) = ndi.distance_transform_edt(~known, return_indices=True)
        L[todo] = L[jy[todo], jx[todo]]
    # outline pixels between hair_back and the face belong to the face (face is on top)
    # (already true via ranking)

    os.makedirs(out, exist_ok=True)
    import colorsys
    dbgimg = np.zeros(sh + (3,), np.uint8)
    for i, n in enumerate(order):
        r, g, b = colorsys.hsv_to_rgb(i / len(order), 0.9, 0.95)
        dbgimg[L == n] = (int(r * 255), int(g * 255), int(b * 255))
    Image.fromarray(dbgimg).resize((sh[1] * 4, sh[0] * 4), Image.NEAREST).save(os.path.join(out, "_labels.png"))
    # ---- 5. upscale with smoothed cut lines ----
    H, W = sh[0] * S, sh[1] * S
    up = lambda a, m=Image.LANCZOS: np.array(Image.fromarray(a).resize((W, H), m))
    colU = np.dstack([up(col[..., c].astype(np.float32), Image.BICUBIC) for c in range(3)])
    aU = up(alpha.astype(np.float32), Image.BICUBIC)
    aU = np.clip((aU - 0.5) * 2.2 + 0.5, 0, 1)  # crisp but anti-aliased silhouette
    score = []
    for n in order:
        m = up(((L == n) * 1.0).astype(np.float32), Image.BILINEAR)
        score.append(ndi.gaussian_filter(m, S * 0.45))
    score = np.stack(score)
    LU = np.array(order, object)[score.argmax(0)]
    solidU = aU > 0.5
    matU = np.array(Image.fromarray(np.vectorize(names.index)(mat).astype(np.uint8)).resize((W, H), Image.NEAREST))
    lineU = np.array(Image.fromarray(dark.astype(np.uint8) * 255).resize((W, H), Image.BILINEAR)) > 100

    sc = lambda pts: poly((H, W), pts, X0, Y0, S)
    os.makedirs(out, exist_ok=True)
    layers = {}
    above = np.zeros((H, W), bool)
    vis_all = {n: (LU == n) & (aU > 0.02) for n in order}
    for i, n in enumerate(order):
        vis = vis_all[n]
        covered = np.zeros((H, W), bool)
        for m2 in order[i + 1:]:
            covered |= vis_all[m2] & (aU > 0.5)
        full = vis.copy()
        for f in cfg.get("full", {}).get(n, []):
            full |= sc(f)
        if n == "face":
            full |= sc(cfg["polys"]["face"])
        if n == "hair_back":
            full |= ndi.binary_fill_holes(vis | sc(cfg["polys"]["face"]))
        if n in cfg.get("extend_up", {}):
            k = cfg["extend_up"][n] * S
            ext = vis.copy()
            for t in range(1, k + 1):
                ext[:-t] |= vis[t:]
            full |= ext & ndi.binary_dilation(sc(cfg["polys"][cfg["extend_zone"][n]]), iterations=S)
        full = ndi.binary_fill_holes(full) if n not in ("hair_back",) else full
        hidden = (full & ~vis & covered) | (vis & covered & ndi.binary_erosion(covered, iterations=S))  # only ever fill where something sits on top
        lay = np.zeros((H, W, 4), np.float32)
        lay[vis, :3] = colU[vis]
        lay[vis, 3] = aU[vis]
        # cut edges inside the original silhouette become fully opaque (no soft seam)
        lay[vis & solidU, 3] = 1
        fillc = cfg.get("fill", {}).get(n)
        if hidden.any() and fillc:
            lay[hidden, :3] = np.array(fillc, float)
            lay[hidden, 3] = 1
        # closing outline on cut edges that are hidden or sit on an existing line
        sil = lay[..., 3] > 0.5
        draw = sil & ~ndi.binary_erosion(sil, iterations=2) & covered & ndi.binary_erosion(solidU, iterations=1)
        if n in cfg.get("outline_cuts", []):
            lay[draw, :3] = LINE
            lay[draw, 3] = 1
        layers[n] = lay
        rgba = np.dstack([np.clip(lay[..., :3], 0, 255), lay[..., 3:] * 255]).astype(np.uint8)
        Image.fromarray(rgba).save(os.path.join(out, n + ".png"))
    print("layers:", len(order), "size", (W, H))


if __name__ == "__main__":
    main(*sys.argv[1:4])
