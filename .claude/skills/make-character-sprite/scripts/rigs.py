"""Bone rigs (original-pixel coords of the GIF frame) and walk cycles for each direction."""
import math, copy


def side_rig(mx=None):
    """SL rig (faces screen-left). mx mirrors it for SR."""
    bones = [
        dict(name="root", parent=None, head=[426, 236], tail=[426, 205]),
        dict(name="spine", parent="root", head=[426, 232], tail=[428, 135]),
        dict(name="head", parent="spine", head=[424, 124], tail=[420, 50]),
        dict(name="upperarm", parent="spine", head=[438, 152], tail=[440, 186]),
        dict(name="forearm", parent="upperarm", head=[440, 186], tail=[440, 222]),
        dict(name="thigh_F", parent="root", head=[418, 238], tail=[414, 268]),
        dict(name="shin_F", parent="thigh_F", head=[414, 268], tail=[410, 302]),
        dict(name="thigh_B", parent="root", head=[434, 238], tail=[436, 268]),
        dict(name="shin_B", parent="thigh_B", head=[436, 268], tail=[440, 302]),
        dict(name="bag", parent="spine", head=[432, 138], tail=[465, 205]),
        dict(name="sash", parent="root", head=[398, 214], tail=[398, 282]),
    ]
    bind = {
        "head": [["head", 30]],
        "torso": [["spine", 14], ["root", 6]],
        "hip": [["root", 14], ["spine", 6]],
        "arm": [["spine", 4], ["upperarm", 8], ["forearm", 8]],
        "leg_SL": [["root", 5], ["thigh_F", 8], ["shin_F", 9]],
        "leg_SR": [["root", 5], ["thigh_B", 8], ["shin_B", 9]],
        "bag": [["spine", 4], ["bag", 14]],
        "sash": [["root", 6], ["sash", 14]],
    }
    rig = dict(bones=bones, bind=bind)
    if mx is not None:
        rig = copy.deepcopy(rig)
        for b in rig["bones"]:
            b["head"] = [mx - b["head"][0], b["head"][1]]; b["tail"] = [mx - b["tail"][0], b["tail"][1]]
        sw = lambda k: k.replace("_SL", "_T").replace("_SR", "_SL").replace("_T", "_SR")
        rig["bind"] = {sw(k): v for k, v in rig["bind"].items()}
        for b in rig["bones"]:
            if b["name"] == "sash":
                b["head"] = [600, 228]; b["tail"] = [600, 276]
    return rig


def side_walk(t, f):
    """f = -1 faces left, +1 faces right. Returns bone -> (rot, dx, dy)."""
    ph = 2 * math.pi * t
    s, c = math.sin(ph), math.cos(ph)
    fw = -f  # positive rotation moves a hanging limb toward -x (screen)
    bob = -1.6 * (1 - abs(s)) + 0.6
    th = 24
    knee_F = 30 * max(0.0, c) ** 1.5      # knee folds while the leg swings through
    knee_B = 30 * max(0.0, -c) ** 1.5
    return {
        "root": (0, 0, bob),
        "spine": (fw * 3 + 1.2 * math.sin(2 * ph), 0, 0),
        "head": (-fw * 2 + 1.2 * math.sin(2 * ph - 0.8), 0, 0),
        "thigh_F": (fw * th * s, 0, 0),
        "shin_F": (-fw * knee_F, 0, 0),
        "thigh_B": (-fw * th * s, 0, 0),
        "shin_B": (-fw * knee_B, 0, 0),
        "upperarm": (-fw * 24 * s, 0, 0),
        "forearm": (fw * (8 + 10 * max(0.0, -s)), 0, 0),
        "bag": (-fw * 5 * math.sin(ph - 0.9), 0, 0),
        "sash": (-fw * 7 * math.sin(ph - 1.2), 0, 0),
    }


def front_rig():
    bones = [
        dict(name="root", parent=None, head=[88, 236], tail=[88, 205]),
        dict(name="spine", parent="root", head=[88, 232], tail=[88, 130]),
        dict(name="head", parent="spine", head=[88, 121], tail=[88, 50]),
        dict(name="upperarm_R", parent="spine", head=[54, 134], tail=[44, 186]),
        dict(name="forearm_R", parent="upperarm_R", head=[44, 186], tail=[45, 222]),
        dict(name="upperarm_L", parent="spine", head=[106, 132], tail=[122, 187]),
        dict(name="forearm_L", parent="upperarm_L", head=[122, 187], tail=[121, 222]),
        dict(name="thigh_R", parent="root", head=[66, 238], tail=[64, 268]),
        dict(name="shin_R", parent="thigh_R", head=[64, 268], tail=[62, 304]),
        dict(name="thigh_L", parent="root", head=[110, 238], tail=[112, 268]),
        dict(name="shin_L", parent="thigh_L", head=[112, 268], tail=[114, 304]),
        dict(name="bag", parent="spine", head=[64, 130], tail=[60, 222]),
        dict(name="sash", parent="root", head=[60, 228], tail=[48, 278]),
    ]
    headparts = ["hair_back", "face", "eye_R", "eye_L", "mouth", "hair_front"]
    bind = {h: [["head", 30]] for h in headparts}
    bind.update({
        "neck": [["spine", 10]], "torso": [["spine", 14], ["root", 6]], "necklace": [["spine", 10]],
        "hip": [["root", 14], ["spine", 5]],
        "upperarm_R": [["spine", 3], ["upperarm_R", 9]], "arm_R": [["upperarm_R", 5], ["forearm_R", 9]],
        "upperarm_L": [["spine", 3], ["upperarm_L", 9]], "arm_L": [["upperarm_L", 5], ["forearm_L", 9]],
        "leg_R": [["root", 5], ["thigh_R", 9], ["shin_R", 9]], "foot_R": [["shin_R", 10]],
        "leg_L": [["root", 5], ["thigh_L", 9], ["shin_L", 9]], "foot_L": [["shin_L", 10]],
        "bag": [["spine", 4], ["bag", 12]], "sash": [["root", 6], ["sash", 12]],
    })
    return dict(bones=bones, bind=bind)


def front_walk(t, sideR="R", sideL="L"):
    ph = 2 * math.pi * t
    s = math.sin(ph)
    bob = -1.6 * (1 - abs(s)) + 0.6
    liftR, liftL = 3.2 * max(0.0, s), 3.2 * max(0.0, -s)
    return {
        "root": (0, 0, bob),
        "spine": (0.8 * math.sin(ph), 0, 0),
        "head": (-1.0 * math.sin(ph) + 0.8 * math.sin(2 * ph - 0.8), 0, 0),
        f"thigh_{sideR}": (0, 0, -liftR * 0.35), f"shin_{sideR}": (0, 0, -liftR * 0.65),
        f"thigh_{sideL}": (0, 0, -liftL * 0.35), f"shin_{sideL}": (0, 0, -liftL * 0.65),
        f"upperarm_{sideR}": (-5 * s, 0, 0), f"forearm_{sideR}": (-4 * max(0.0, -s), 0, 0),
        f"upperarm_{sideL}": (-5 * s, 0, 0), f"forearm_{sideL}": (4 * max(0.0, s), 0, 0),
        "bag": (3 * math.sin(ph - 0.9), 0, 0),
        "sash": (5 * math.sin(ph - 1.2), 0, 0),
    }


def back_rig(dx=0, mirror=None):
    bones = [
        dict(name="root", parent=None, head=[256, 236], tail=[256, 205]),
        dict(name="spine", parent="root", head=[256, 232], tail=[254, 130]),
        dict(name="head", parent="spine", head=[252, 121], tail=[252, 50]),
        dict(name="upperarm_SL", parent="spine", head=[226, 134], tail=[214, 178]),
        dict(name="forearm_SL", parent="upperarm_SL", head=[214, 178], tail=[214, 215]),
        dict(name="upperarm_SR", parent="spine", head=[280, 134], tail=[296, 182]),
        dict(name="forearm_SR", parent="upperarm_SR", head=[296, 182], tail=[295, 222]),
        dict(name="thigh_SL", parent="root", head=[232, 238], tail=[230, 268]),
        dict(name="shin_SL", parent="thigh_SL", head=[230, 268], tail=[228, 306]),
        dict(name="thigh_SR", parent="root", head=[280, 238], tail=[282, 268]),
        dict(name="shin_SR", parent="thigh_SR", head=[282, 268], tail=[284, 306]),
        dict(name="bag", parent="spine", head=[284, 130], tail=[230, 225]),
        dict(name="sash", parent="root", head=[296, 220], tail=[306, 272]),
    ]
    bind = {
        "head": [["head", 30]], "torso": [["spine", 14], ["root", 6]], "hip": [["root", 14], ["spine", 5]],
        "arm_SL": [["spine", 3], ["upperarm_SL", 8], ["forearm_SL", 8]],
        "arm_SR": [["spine", 3], ["upperarm_SR", 8], ["forearm_SR", 8]],
        "leg_SL": [["root", 5], ["thigh_SL", 9], ["shin_SL", 9]],
        "leg_SR": [["root", 5], ["thigh_SR", 9], ["shin_SR", 9]],
        "bag": [["spine", 4], ["bag", 12]], "sash": [["root", 6], ["sash", 12]],
    }
    rig = dict(bones=bones, bind=bind)
    rig = copy.deepcopy(rig)
    for b in rig["bones"]:
        for k in ("head", "tail"):
            x = b[k][0] + dx
            if mirror is not None:
                x = mirror - b[k][0]
            b[k] = [x, b[k][1]]
    if mirror is not None:
        sw = lambda k: k.replace("_SL", "_T").replace("_SR", "_SL").replace("_T", "_SR")
        for b in rig["bones"]:
            b["name"] = sw(b["name"]); b["parent"] = sw(b["parent"]) if b["parent"] else None
        rig["bind"] = {sw(k): [[sw(bn), sg] for bn, sg in v] for k, v in rig["bind"].items()}
        # parents must come first after renaming: order unchanged, names swapped consistently
    return rig
