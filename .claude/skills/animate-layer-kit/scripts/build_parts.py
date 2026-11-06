"""Layered kit -> animation-ready parts.

A kit made by splitting a finished drawing is a visible-surface split: the layers touch but never overlap, and some
cuts are straight lines (bangs cut as a polygon, hair bits inside the face layer). Moving such layers one by one
opens gaps. This script, driven by a rig file (see rigs/phawin_front.json):
  1. fixes ownership slips the rig lists (trouser pixels inside the hand, shirt inside the pendant cord ...) and
     splits layers that belong to two parts (neck below the chin goes with the body);
  2. merges layers that move together into one part, exactly (the kit's shared edge pixels add up);
  3. hands small outline crumbs to the part they outline;
  4. under every part, carries the nearest part below it on for `reach` px in its own colour (shirt under the strap,
     trousers under the hand, skin under the chin), and paints skin under the eyes so they can blink;
  5. hands dark seam lines on the lower side of a joint to the part that moves;
  6. saves parts/*.png + parts.json and checks the rest pose still matches assembled.png.
Usage: python3 -I build_parts.py <kit_dir> <rig.json> <out_dir>
       kit_dir holds assembled.png and layers/<name>.png, all on one canvas.
"""
import sys, os, json
import numpy as np
import cv2
from PIL import Image

KIT, RIG, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
R = json.load(open(RIG))
os.makedirs(f"{OUT}/parts", exist_ok=True)
W, H = R["canvas"]
KA = float(R.get("kit_alpha", 255))


def load(n):
    im = Image.open(f"{KIT}/layers/{n}.png").convert("RGBA")
    if im.size != (W, H):
        sys.exit(f"layers/{n}.png is {im.size}, the rig says the canvas is {(W, H)}. All layers must share one canvas.")
    a = np.array(im).astype(np.float32)
    a[..., 3][a[..., 3] < 4] = 0                          # stray specks
    return a


def lum(a):
    return (a[..., 0] * 3 + a[..., 1] * 6 + a[..., 2]) / 10


missing = [n for n in R["layers"] if not os.path.exists(f"{KIT}/layers/{n}.png")]
if missing:
    sys.exit(f"missing layers in {KIT}/layers: {missing}")
L = {n: load(n) for n in R["layers"]}

# --- 1. ownership fixes and splits ----------------------------------------------------------------------------------
def pick(a, rule):
    if rule == "bluish_dark":          # dark blue cloth (navy trousers) caught inside another layer
        m = ((a[..., 3] > 0) & (a[..., 2] > a[..., 0] + 15) & (lum(a) < 120)).astype(np.uint8)
        k, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
        return np.isin(lab, [i for i in range(1, k) if st[i, cv2.CC_STAT_AREA] > 40])
    if rule == "light_neutral":        # pale, not saturated (cream shirt) caught inside another layer
        return (a[..., 3] > 0) & (lum(a) > 160) & (a[..., 0] - a[..., 2] < 80)
    sys.exit(f"unknown pick rule {rule}")


for f in R.get("fixes", []):
    m = pick(L[f["from"]], f["pick"])
    L[f["to"]][m] = L[f["from"]][m]
    L[f["from"]][m] = 0
    print(f"fix: {int(m.sum())} px {f['from']} -> {f['to']} ({f.get('why', '')})")

for s in R.get("splits", []):
    src = L.pop(s["layer"])
    L[s["above"]], L[s["below"]] = src.copy(), src.copy()
    L[s["above"]][s["at_y"]:] = 0
    L[s["below"]][:s["at_y"]] = 0

# --- 2. parts ---------------------------------------------------------------------------------------------------------
PARTS, ORDER, RIGP = R["parts"], R["order"], R["rig"]
EYES = [n for n in ORDER if RIGP[n]["role"] == "eye"]
HEAD = next(n for n in ORDER if RIGP[n]["role"] == "head")


def merge(names):
    """Premultiplied sum: the kit's shared edge pixels add up to the original coverage."""
    acc = np.zeros((H, W, 4), np.float32)
    for n in names:
        a = L[n]
        acc[..., :3] += a[..., :3] * a[..., 3:4] / 255.0
        acc[..., 3] += a[..., 3]
    out = np.zeros_like(acc)
    out[..., :3] = np.clip(acc[..., :3] / np.maximum(acc[..., 3:4], 1e-3) * 255.0 * (acc[..., 3:4] > 0), 0, 255)
    out[..., 3] = np.clip(np.clip(acc[..., 3], 0, 255) * 255.0 / KA, 0, 255)   # kit drawn at alpha KA inside
    return out


P = {k: merge(PARTS[k]) for k in ORDER}
sk = L[R["skin_layer"]]
skin = (sk[..., 3] > 200) & (lum(sk) > 170) & (sk[..., 0] - sk[..., 2] > 25)


def add_into(dst, src, m):
    sa, da = src[..., 3:4] / 255.0, dst[..., 3:4] / 255.0
    tot = np.maximum(sa + da, 1e-3)
    dst[..., :3] = np.where(m[..., None], (src[..., :3] * sa + dst[..., :3] * da) / tot, dst[..., :3])
    dst[..., 3] = np.where(m, np.minimum(sa + da, 1.0)[..., 0] * 255, dst[..., 3])
    src[m] = 0


# --- 3. outline crumbs go with the part they outline ------------------------------------------------------------------
crumbs = 0
for i, lo in enumerate(ORDER):
    if lo in EYES:
        continue
    m = (P[lo][..., 3] > 0).astype(np.uint8)
    k, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
    if k <= 2:
        continue
    biggest = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    for j in range(1, k):
        if j == biggest or st[j, cv2.CC_STAT_AREA] >= 800:
            continue
        comp = lab == j
        ring = cv2.dilate(comp.astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool)
        touch = [(int((ring & (P[up][..., 3] > 0)).sum()), up) for up in ORDER[i + 1:]]
        touch = [t for t in touch if t[0] > 0]
        if touch:
            add_into(P[max(touch)[1]], P[lo], comp)
            crumbs += int(comp.sum())
print("outline crumbs handed to the part they outline:", crumbs)

full = sum(P[n][..., 3] for n in ORDER)
mask = {n: P[n][..., 3] > 0 for n in ORDER}


def core_of(n):
    """Colour sources: solid pixels a little way in from the edge, not the dark outline."""
    a = P[n]
    solid = cv2.erode((a[..., 3] >= 250).astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
    if n == HEAD:
        return solid & skin                               # paint skin, never hair
    ln = lum(a)
    if not solid.any():
        return a[..., 3] >= 200
    return solid & (ln > np.percentile(ln[solid], 12) * 0.8)


core = {n: core_of(n) for n in ORDER}
NC = {}


def nearest_colour(n):
    if n in NC:
        return NC[n]
    src = (~core[n]).astype(np.uint8)
    _, lab = cv2.distanceTransformWithLabels(src, cv2.DIST_L2, 5, labelType=cv2.DIST_LABEL_PIXEL)
    zy, zx = np.where(src == 0)
    order = np.zeros(lab.max() + 1, np.int64)
    order[lab[zy, zx]] = np.arange(len(zy))
    col = P[n][zy[order[lab]], zx[order[lab]], :3]
    w = core[n].astype(np.float32)                       # soften the mosaic so it reads as cloth/skin
    acc = cv2.GaussianBlur(col * w[..., None], (0, 0), 5)
    wn = cv2.GaussianBlur(w, (0, 0), 5)[..., None]
    NC[n] = np.where(wn > 0.02, acc / np.maximum(wn, 1e-3), col)
    return NC[n]


# --- 4. hidden areas --------------------------------------------------------------------------------------------------
# reach must be larger than the most any joint moves (the hand in the walk: about 19 px), so a moving part only ever
# uncovers painted cloth on its near side, while its far side still shows the background it hangs against.
reach = R.get("reach", {})
dist = {n: cv2.distanceTransform((~mask[n]).astype(np.uint8), cv2.DIST_L2, 5) for n in ORDER}
fill = {n: np.zeros((H, W), bool) for n in ORDER}
for i, up in enumerate(ORDER):
    lower = [n for n in ORDER[:i] if n not in EYES]
    if up in EYES or not lower:
        continue
    D = np.stack([dist[n] for n in lower])
    best, bd = D.argmin(0), D.min(0)
    ok = mask[up] & (full >= 128) & (bd <= reach.get(up, reach.get("default", 26)))
    for k, n in enumerate(lower):
        f = (ok & (best == k)).astype(np.uint8)
        f = cv2.morphologyEx(f, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (9, 9))).astype(bool)
        fill[n] |= f & mask[up] & (full >= 128) & ~mask[n]   # closed: no pinholes between fingers

# skin under the eyes: cheek colour just under each column, a little darker at the top (lid and bang shadow)
hd = P[HEAD]
eye_skin = np.zeros((H, W), bool)
for e in EYES:
    hole = cv2.dilate(mask[e].astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool) & ~(hd[..., 3] >= 250)
    ys, xs = np.where(hole)
    y0, y1 = ys.min(), ys.max()
    for x in range(xs.min(), xs.max() + 1):
        yy = np.where(hole[:, x])[0]
        if not len(yy):
            continue
        win = hd[y1 + 1:y1 + 12, max(x - 4, 0):x + 5]
        ok = skin[y1 + 1:y1 + 12, max(x - 4, 0):x + 5] & (win[..., 3] > 240)
        c = np.median(win[ok][:, :3], axis=0) if ok.sum() > 3 else np.median(hd[core[HEAD]][:, :3], axis=0)
        shade = (0.82 + 0.18 * ((yy - y0) / max(y1 - y0, 1)) ** 0.8)[:, None]
        ea = hd[yy, x, 3:4] / 255.0
        hd[yy, x, :3] = hd[yy, x, :3] * ea + c[None, :] * shade * (1 - ea)
        hd[yy, x, 3] = 255
    eye_skin |= hole
hd[..., :3] = np.where(eye_skin[..., None], cv2.GaussianBlur(hd[..., :3], (0, 0), 2), hd[..., :3])

for n in ORDER:
    f = fill[n]
    if f.any():
        a = P[n]
        ea = a[..., 3:4] / 255.0
        a[..., :3] = np.where(f[..., None], a[..., :3] * ea + nearest_colour(n) * (1 - ea), a[..., :3])
        a[..., 3] = np.where(f, 255, a[..., 3])

# --- 5. seam lines go with the part that moves ------------------------------------------------------------------------
orig = {n: P[n].copy() for n in ORDER}
moved = 0
for i, up in enumerate(ORDER):
    if up in EYES:
        continue
    near_up = cv2.dilate((orig[up][..., 3] > 128).astype(np.uint8) & ~fill[up], np.ones((7, 7), np.uint8)).astype(bool)
    for lo in ORDER[:i]:
        a = P[lo]
        dark = near_up & (orig[lo][..., 3] > 0) & ~fill[lo] & (lum(orig[lo]) < np.median(lum(a)[core[lo]]) * 0.55)
        if not dark.any():
            continue
        u = P[up]
        la, ua = orig[lo][..., 3:4] / 255.0, u[..., 3:4] / 255.0
        u[..., :3] = np.where(dark[..., None], (orig[lo][..., :3] * la + u[..., :3] * ua) / np.maximum(la + ua, 1e-3), u[..., :3])
        u[..., 3] = np.where(dark, np.minimum(la + ua, 1.0)[..., 0] * 255, u[..., 3])
        a[..., :3] = np.where(dark[..., None], nearest_colour(lo), a[..., :3])
        a[..., 3] = np.where(dark, 255, a[..., 3])
        moved += int(dark.sum())
print("seam px handed to the moving part:", moved)

# --- 6. save + rest check ---------------------------------------------------------------------------------------------
comp = np.zeros((H, W, 4), np.float32)
for n in ORDER:
    Image.fromarray(np.clip(P[n], 0, 255).astype(np.uint8)).save(f"{OUT}/parts/{n}.png")
    sa = P[n][..., 3:4] / 255.0
    comp[..., :3] = P[n][..., :3] * sa + comp[..., :3] * (1 - sa)
    comp[..., 3:4] = sa * 255 + comp[..., 3:4] * (1 - sa)
if os.path.exists(f"{KIT}/assembled.png"):
    ref = np.array(Image.open(f"{KIT}/assembled.png").convert("RGBA")).astype(np.float32)
    ra = np.clip(ref[..., 3:4] * 255 / KA, 0, 255) / 255.0
    diff = np.abs(comp[..., :3] * comp[..., 3:4] / 255 - ref[..., :3] * ra).max(-1)
    print("rest pose vs assembled.png: mean diff %.3f (should be < 0.5), px off by >24: %d (edge pixels only)"
          % (diff.mean(), (diff > 24).sum()))
json.dump({"rig": os.path.abspath(RIG), "order": ORDER, "parts": PARTS, "canvas": [W, H]},
          open(f"{OUT}/parts.json", "w"), indent=1)
