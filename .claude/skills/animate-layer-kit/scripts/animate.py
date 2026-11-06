"""Rig + animate the parts made by build_parts.py: front walk (in place) and idle with blink, as GIFs.

Every part moves by a smooth displacement field, like bone skinning, never as a rigid card: a sleeve bends from the
shoulder seam, a trouser leg bunches up between the hips and the cuff when the foot lifts, hair tips trail the
head. Inside a part nothing can tear; between parts, build_parts.py painted what is underneath.
Usage: python3 -I animate.py <build_dir> <rig.json> <out_dir> [all|walk|idle|check]
  check = full-size sheet with the arms and head pushed 1.5x further than the GIFs, plus the blink: look at it for
          gaps, ghost flaps or broken lines before handing anything over.
"""
import sys, os, math, json
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageFilter

D, RIG, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
MODE = sys.argv[4] if len(sys.argv) > 4 else "all"
R = json.load(open(RIG))
os.makedirs(OUT, exist_ok=True)
ORDER, RIGP, LN = R["order"], R["rig"], R["lines"]
W, H = R["canvas"]
HEAD = next(n for n in ORDER if RIGP[n]["role"] == "head")
NECK = tuple(RIGP[HEAD]["pivot"])


def premul(a):
    a = a.astype(np.float32)
    a[..., :3] *= a[..., 3:4] / 255.0
    return a


IMG = {n: premul(np.array(Image.open(f"{D}/parts/{n}.png").convert("RGBA"))) for n in ORDER}


def sstep(x):
    x = np.clip(x, 0.0, 1.0)
    return x * x * (3 - 2 * x)


def rot_disp(X, Y, c, ang_deg):
    a = np.radians(ang_deg)
    ca, sa = np.cos(a), np.sin(a)
    dx, dy = X - c[0], Y - c[1]
    return ca * dx - sa * dy - dx, sa * dx + ca * dy - dy


def breath_field(Y, b):
    """Chest rises, waist stays: lifted by b px at the chest line, fading to 0 at the waist line."""
    return -b * np.clip((LN["waist_y"] - Y) / (LN["waist_y"] - LN["chest_y"]), 0, 1)


def field(n, X, Y, p):
    """Forward displacement (dx, dy) of source pixel (X, Y) of part n in pose p. Positive angle = clockwise."""
    r = RIGP[n]
    role, sway, bob, br = r["role"], p["sway"], p["bob"], p["breath"]
    if role == "body":
        return np.full_like(X, sway), bob + breath_field(Y, br)
    if role == "swing":
        rx, ry = rot_disp(X, Y, r["pivot"], p["pend"] * sstep((Y - r["bend_from_y"]) / (r["bend_to_y"] - r["bend_from_y"])))
        return sway + rx, bob + breath_field(Y, br) + ry
    if role == "arm":
        # bends from the shoulder seam: no turn above bend_from_y, the full turn from bend_to_y down
        rx, ry = rot_disp(X, Y, r["pivot"], p["arms"].get(n, 0.0) * sstep((Y - r["bend_from_y"]) / (r["bend_to_y"] - r["bend_from_y"])))
        return sway + rx, bob - br * 0.93 + ry
    if role in ("head", "eye"):
        rx, ry = rot_disp(X, Y, NECK, p["head"])
        dx, dy = sway + rx, bob - br + ry
        if role == "head":
            c, rad = RIGP[HEAD]["hair_centre"], RIGP[HEAD]["hair_radius"]
            e = np.sqrt(((X - c[0]) / rad[0]) ** 2 + ((Y - c[1]) / rad[1]) ** 2)
            w = sstep((e - 0.78) / 0.5)                  # 0 on the face, 1 at the outer locks
            dx, dy = dx + w * p["hair"][0], dy + w * p["hair"][1]
        return dx, dy
    if role == "leg":
        lift = p["lift_" + r["side"]]
        ux = np.clip((Y - LN["leg_top_y"]) / (LN["leg_bottom_y"] - LN["leg_top_y"]), 0, 1)   # lean: hips move, cuff stays
        uy = sstep((Y - LN["bunch_from_y"]) / (LN["bunch_to_y"] - LN["bunch_from_y"]))      # bunch up when the foot lifts
        return sway * (1 - ux), bob * (1 - uy) - lift * uy
    if role == "foot":
        return np.zeros_like(X), np.full_like(X, -p["lift_" + r["side"]])
    raise KeyError(role)


BBOX = {}
for n in ORDER:
    ys, xs = np.where(IMG[n][..., 3] > 0)
    BBOX[n] = (max(xs.min() - 50, 0), max(ys.min() - 50, 0), min(xs.max() + 51, W), min(ys.max() + 51, H))


def warp(n, p, img=None):
    img = IMG[n] if img is None else img
    x0, y0, x1, y1 = BBOX[n]
    Xo, Yo = np.meshgrid(np.arange(x0, x1, dtype=np.float32), np.arange(y0, y1, dtype=np.float32))
    Xs, Ys = Xo.copy(), Yo.copy()
    for _ in range(6):                                   # invert the field: which source pixel lands here
        dx, dy = field(n, Xs, Ys, p)
        Xs, Ys = Xo - dx, Yo - dy
    out = np.zeros((H, W, 4), np.float32)
    out[y0:y1, x0:x1] = cv2.remap(img, Xs.astype(np.float32), Ys.astype(np.float32), cv2.INTER_LINEAR,
                                  borderMode=cv2.BORDER_CONSTANT, borderValue=0)
    return out


# ---- eyelids: half-closed and closed, drawn in the eye's own lash colour ----------------------------------------------
def lids(n, outer_right):
    e = IMG[n]
    a = e[..., 3]
    ys, xs = np.where(a > 128)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    straight = e[..., :3] / np.maximum(a[..., None] / 255.0, 1e-3)
    lash = np.median(straight[(a > 200) & ((straight @ np.array([0.3, 0.6, 0.1])) < 70)], axis=0)
    h = y1 - y0
    k = h / 47.0                                          # line sizes were tuned on a 47 px tall eye
    reg = cv2.dilate((a > 0).astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)

    def stroke(yc, bow, thick, flick):
        S = 4
        cv = np.zeros((H * S, W * S), np.uint8)
        xx = np.linspace(x0 + 2, x1 - 2, 40)
        u = (xx - (x0 + x1) / 2) / ((x1 - x0) / 2)
        mid = yc + bow * k * (1 - u ** 2)
        th = k * (thick * (0.45 + 0.55 * (1 - u ** 2)) + flick * np.clip(u if outer_right else -u, 0, 1) ** 3)
        poly = np.concatenate([np.stack([xx, mid - th / 2], 1), np.stack([xx, mid + th / 2], 1)[::-1]])
        cv2.fillPoly(cv, [np.round(poly * S).astype(np.int32)], 255)
        return cv2.resize(cv, (W, H), interpolation=cv2.INTER_AREA).astype(np.float32) * reg

    def layer(alpha, keep=None):
        o = np.zeros((H, W, 4), np.float32)
        if keep is not None:
            o[keep] = e[keep]
        al = alpha / 255.0
        o[..., :3] = lash * al[..., None] + o[..., :3] * (1 - al[..., None])
        o[..., 3] = alpha + o[..., 3] * (1 - al)
        return o

    yc = y0 + 0.5 * h                                    # half: the lid edge is an arch through the middle
    u = (np.arange(W)[None, :] - (x0 + x1) / 2) / ((x1 - x0) / 2)
    keep = (a > 0) & (np.arange(H)[:, None] >= yc - 3.0 * k * (1 - np.clip(u, -1, 1) ** 2) + 1)
    return {"half": layer(stroke(yc + 0.5, -3.0, 5.5, 3.5), keep),
            "closed": layer(stroke(y0 + 0.62 * h, 4.0, 5.0, 5.0))}   # closed: soft downward curve + outer lash


LIDS = {n: lids(n, RIGP[n].get("outer", "right") == "right") for n in ORDER if RIGP[n]["role"] == "eye"}


def render(p, scale, bg=(236, 230, 220)):
    out = np.zeros((H, W, 4), np.float32)
    out[..., :3] = bg
    out[..., 3] = 255
    # soft blob shadow on the ground (CLAUDE.md: sprites cast no real shadow)
    sh = Image.new("L", (W, H), 0)
    rx = LN["shadow_rx"] - max(p["lift_R"], p["lift_L"]) * 0.8
    cx, gy = LN["centre_x"] + p["sway"] * 0.5, LN["ground_y"]
    ImageDraw.Draw(sh).ellipse([cx - rx, gy - 26, cx + rx, gy + 26], fill=90)
    sh = np.array(sh.filter(ImageFilter.GaussianBlur(10)), np.float32)[..., None] / 255.0
    out[..., :3] = out[..., :3] * (1 - sh) + np.array([150, 138, 124], np.float32) * sh
    for n in ORDER:
        img = LIDS[n][p["eyes"]] if n in LIDS and p.get("eyes", "open") != "open" else None
        lay = warp(n, p, img)
        out = lay + out * (1 - lay[..., 3:4] / 255.0)
    im = Image.fromarray(np.clip(out[..., :3], 0, 255).astype(np.uint8))
    return im.resize((int(W * scale), int(H * scale)), Image.LANCZOS) if scale != 1 else im


def rest():
    return dict(sway=0.0, bob=0.0, breath=0.0, head=0.0, hair=(0.0, 0.0), pend=0.0, arms={},
                lift_R=0.0, lift_L=0.0, eyes="open")


def walk_pose(t, push=1.0):
    c = R["walk"]

    def body(t):
        s = math.sin(2 * math.pi * t)
        return -c["sway"] * s, c["bob"] - 2 * c["bob"] * abs(s)      # sway over the planted foot, highest when passing
    ph = 2 * math.pi * t
    s = math.sin(ph)
    sw, bo = body(t)
    lag = body(t - c["hair_lag"])
    p = rest()
    p.update(sway=sw, bob=bo, head=c["head"] * push * s, hair=(1.1 * push * (lag[0] - sw), 1.1 * push * (lag[1] - bo)),
             pend=c["pendant"] * push * math.sin(ph - 1.3), arms={k: v * push * s for k, v in c["arms"].items()},
             lift_R=c["lift"] * max(0.0, s) ** 1.3, lift_L=c["lift"] * max(0.0, -s) ** 1.3)
    return p


def idle_pose(t):
    c = R["idle"]
    ph = 2 * math.pi * t
    br = lambda t: c["breath"] * (0.5 - 0.5 * math.cos(2 * math.pi * t))
    b = br(t)
    p = rest()
    p.update(breath=b, head=c["head"] * math.sin(ph - 0.4), hair=(0.0, -1.2 * (br(t - 0.1) - b)),
             pend=c["pendant"] * math.sin(ph - 1.0), arms={k: v * b / c["breath"] for k, v in c["arms"].items()})
    return p


def save_gif(frames, path, ms):
    strip = Image.new("RGB", (frames[0].width, frames[0].height * len(frames)))
    for i, f in enumerate(frames):
        strip.paste(f, (0, i * f.height))
    # one palette for every frame (still areas do not shimmer); small vivid accents get entries of their own,
    # or a bright stone turns brown next to large cloth areas
    main = strip.quantize(colors=216, method=Image.Quantize.MEDIANCUT).getpalette()[:216 * 3]
    px = np.array(strip).reshape(-1, 3).astype(int)
    vivid = px[(px.max(1) - px.min(1)) > 90]
    extra = []
    if len(vivid) > 50:
        extra = Image.fromarray(vivid[:, None, :].astype(np.uint8)).quantize(colors=39).getpalette()[:39 * 3]
    pal = Image.new("P", (1, 1))
    pal.putpalette(main + extra + [0, 0, 0] * (256 - (len(main) + len(extra)) // 3))
    q = [f.quantize(palette=pal, dither=Image.Dither.NONE) for f in frames]
    q[0].save(path, save_all=True, append_images=q[1:], duration=ms, loop=0, optimize=False, disposal=1)


if __name__ == "__main__":
    S = R.get("gif_scale", 0.5)
    tag = R["name"].lower().replace(" ", "_")
    if MODE in ("all", "walk"):
        c = R["walk"]
        N = c["frames"]
        frames = []
        for loop in range(c.get("loops", 2)):
            for i in range(N):
                p = walk_pose(i / N)
                if loop == c.get("loops", 2) - 1:          # blink once, in the last loop
                    p["eyes"] = {N // 2 + 1: "half", N // 2 + 2: "closed", N // 2 + 3: "half"}.get(i, "open")
                frames.append(render(p, S))
        save_gif(frames, f"{OUT}/{tag}_walk.gif", c["ms"])
        print("walk:", len(frames), "frames")
    if MODE in ("all", "idle"):
        c = R["idle"]
        N = c["frames"]
        blink = {int(N * 0.62): "half", int(N * 0.62) + 1: "closed", int(N * 0.62) + 2: "half"}
        frames = []
        for i in range(N):
            p = idle_pose(i / N)
            p["eyes"] = blink.get(i, "open")
            frames.append(render(p, S))
        save_gif(frames, f"{OUT}/{tag}_idle.gif", c["ms"])
        print("idle:", len(frames), "frames")
    if MODE in ("all", "check"):
        tiles = []
        for t in (0.25, 0.75):
            tiles.append(render(walk_pose(t, push=1.5), 1.0))
        q = rest()
        for state in ("half", "closed"):
            q["eyes"] = state
            tiles.append(render(q, 1.0))
        sheet = Image.new("RGB", (W * 2, H * 2), (255, 255, 255))
        for i, t in enumerate(tiles):
            sheet.paste(t, ((i % 2) * W, (i // 2) * H))
        sheet.resize((W, H), Image.LANCZOS).save(f"{OUT}/check_sheet.png")
        print("check sheet:", f"{OUT}/check_sheet.png")
