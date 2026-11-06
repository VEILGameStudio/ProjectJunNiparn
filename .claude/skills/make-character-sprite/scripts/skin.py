"""Bone-weighted (skinned) deformation of split layers, like Unity 2D Animation's Sprite Skin.

Each layer is bound to one or more bones with smooth distance-based weights, so joints bend
instead of breaking. Frames are rendered by inverse linear-blend skinning with cv2.remap.
"""
import json, math
import numpy as np, cv2
from PIL import Image

S = 4
PAD = 80


def rotm(deg, c):
    a = math.radians(deg); ca, sa = math.cos(a), math.sin(a)
    cx, cy = c
    T1 = np.array([[1, 0, -cx], [0, 1, -cy], [0, 0, 1.]])
    R = np.array([[ca, -sa, 0], [sa, ca, 0], [0, 0, 1.]])
    T2 = np.array([[1, 0, cx], [0, 1, cy], [0, 0, 1.]])
    return T2 @ R @ T1


def trans(dx, dy):
    return np.array([[1, 0, dx], [0, 1, dy], [0, 0, 1.]])


class Rig:
    def __init__(self, layer_dir, cfg_path, rig):
        self.cfg = cfg = json.load(open(cfg_path))
        x0, y0 = cfg["crop"][:2]
        self.P = lambda p: ((p[0] - x0) * S + PAD, (p[1] - y0) * S + PAD)
        self.bones = {}
        for b in rig["bones"]:
            self.bones[b["name"]] = dict(b, head=self.P(b["head"]), tail=self.P(b["tail"]))
        self.order = [b["name"] for b in rig["bones"]]  # parents listed first
        self.layers = []
        for n in cfg["order"]:
            im = np.array(Image.open(f"{layer_dir}/{n}.png").convert("RGBA"))
            big = np.zeros((im.shape[0] + 2 * PAD, im.shape[1] + 2 * PAD, 4), np.uint8)
            big[PAD:PAD + im.shape[0], PAD:PAD + im.shape[1]] = im
            bind = rig["bind"].get(n, rig["bind"].get("*"))
            self.layers.append((n, big, self._weights(big, bind)))
        self.H, self.W = self.layers[0][1].shape[:2]
        yy, xx = np.mgrid[0:self.H, 0:self.W].astype(np.float32)
        self.grid = (xx, yy)

    def _weights(self, img, bind):
        """bind: list of [bone, sigma_px_orig] -> stacked normalised weight maps."""
        H, W = img.shape[:2]
        yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
        ws = []
        for bn, sig in bind:
            b = self.bones[bn]
            (ax, ay), (bx, by) = b["head"], b["tail"]
            vx, vy = bx - ax, by - ay
            L2 = vx * vx + vy * vy + 1e-6
            t = np.clip(((xx - ax) * vx + (yy - ay) * vy) / L2, 0, 1)
            dx, dy = xx - (ax + t * vx), yy - (ay + t * vy)
            d2 = dx * dx + dy * dy
            ws.append(np.exp(-d2 / (2 * (sig * S) ** 2)) + 1e-9)
        ws = np.stack(ws)
        ws /= ws.sum(0, keepdims=True)
        return [bn for bn, _ in bind], ws.astype(np.float32)

    def pose(self, local):
        """local: bone -> (rot_deg, dx, dy) relative to parent; returns world matrices."""
        Mw = {}
        for n in self.order:
            b = self.bones[n]
            r, dx, dy = local.get(n, (0, 0, 0))
            Ml = trans(dx * S, dy * S) @ rotm(r, b["head"])
            Mw[n] = (Mw[b["parent"]] @ Ml) if b.get("parent") else Ml
        return Mw

    def render(self, local, bg=None):
        Mw = self.pose(local)
        out = np.zeros((self.H, self.W, 4), np.float32) if bg is None else bg.astype(np.float32)
        xx, yy = self.grid
        for n, img, (bns, ws) in self.layers:
            Ms = np.stack([Mw[b] for b in bns])  # k,3,3
            # inverse LBS by fixed-point iteration: p = (sum w_i(p) M_i)^-1 q
            px, py = xx.copy(), yy.copy()
            for _ in range(3):
                ix = np.clip(px, 0, self.W - 1).astype(np.int32); iy = np.clip(py, 0, self.H - 1).astype(np.int32)
                w = ws[:, iy, ix]  # k,H,W
                A = np.einsum("khw,kij->hwij", w, Ms[:, :2, :])  # H,W,2,3
                a, b_, c = A[..., 0, 0], A[..., 0, 1], A[..., 0, 2]
                d, e, f = A[..., 1, 0], A[..., 1, 1], A[..., 1, 2]
                det = a * e - b_ * d + 1e-9
                qx, qy = xx - c, yy - f
                px = (e * qx - b_ * qy) / det
                py = (-d * qx + a * qy) / det
            warped = cv2.remap(img, px.astype(np.float32), py.astype(np.float32), cv2.INTER_LINEAR,
                               borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0)).astype(np.float32)
            al = warped[..., 3:4] / 255.0
            out[..., :3] = out[..., :3] * (1 - al) + warped[..., :3] * al
            out[..., 3:4] = out[..., 3:4] * (1 - al) + 255 * al
        return Image.fromarray(np.clip(out, 0, 255).astype(np.uint8))
