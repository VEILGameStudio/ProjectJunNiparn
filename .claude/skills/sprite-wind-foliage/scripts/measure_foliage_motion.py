#!/usr/bin/env python3
"""
measure_foliage_motion.py
Measures how leaves move in a reference clip: how far (in art pixels), how fast (Hz),
and how "chunky" the motion is (spatial correlation length).

Usage:
  python3 -I measure_foliage_motion.py clip.mp4 <t0> <t1> <art_px> \
      <canopy x0,y0,x1,y1> <anchor x0,y0,x1,y1> [label]

  art_px  = screen pixels per art pixel (pixel-art upscale, e.g. 6)
  canopy  = box around the leaves to measure
  anchor  = box on something that does NOT sway (trunk, ground) used to cancel camera pan

Output: one line per window + a summary. All distances are in ART pixels.
"""
import sys
import cv2
import numpy as np


def box(text):
    x0, y0, x1, y1 = [int(v) for v in text.split(",")]
    return x0, y0, x1, y1


def read_frames(path, t0, t1):
    cap = cv2.VideoCapture(path)
    fps = cap.get(cv2.CAP_PROP_FPS)
    cap.set(cv2.CAP_PROP_POS_MSEC, t0 * 1000.0)
    frames = []
    while True:
        ok, frame = cap.read()
        if not ok or cap.get(cv2.CAP_PROP_POS_MSEC) > t1 * 1000.0:
            break
        frames.append(cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY).astype(np.float32))
    return frames, fps


def camera_shift(reference, frame, anchor):
    # Phase correlation on the anchor box gives the camera pan between two frames.
    x0, y0, x1, y1 = anchor
    window = cv2.createHanningWindow((x1 - x0, y1 - y0), cv2.CV_32F)
    (dx, dy), _ = cv2.phaseCorrelate(reference[y0:y1, x0:x1], frame[y0:y1, x0:x1], window)
    return dx, dy


def correlation_length(field):
    # Distance (px) at which the displacement field's autocorrelation drops to 0.5.
    f = field - field.mean()
    if f.std() < 1e-6:
        return 0.0
    spectrum = np.abs(np.fft.fft2(f)) ** 2
    auto = np.fft.ifft2(spectrum).real
    auto /= auto[0, 0]
    row = auto[0, : f.shape[1] // 2]
    below = np.where(row < 0.5)[0]
    return float(below[0]) if len(below) else float(len(row))


def main():
    path, t0, t1, art_px = sys.argv[1], float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4])
    canopy, anchor = box(sys.argv[5]), box(sys.argv[6])
    label = sys.argv[7] if len(sys.argv) > 7 else ""
    frames, fps = read_frames(path, t0, t1)
    x0, y0, x1, y1 = canopy
    reference = frames[0]
    mean_dx, rms_list, corr_list = [], [], []
    for frame in frames[1:]:
        cam_dx, cam_dy = camera_shift(reference, frame, anchor)
        # Undo the camera pan so only the leaves' own motion is left.
        matrix = np.float32([[1, 0, -cam_dx], [0, 1, -cam_dy]])
        aligned = cv2.warpAffine(frame, matrix, (frame.shape[1], frame.shape[0]))
        flow = cv2.calcOpticalFlowFarneback(reference[y0:y1, x0:x1], aligned[y0:y1, x0:x1],
                                            None, 0.5, 3, 21, 3, 5, 1.1, 0)
        fx = flow[..., 0]
        fy = flow[..., 1]
        mean_dx.append(float(fx.mean()))
        local = np.sqrt((fx - fx.mean()) ** 2 + (fy - fy.mean()) ** 2)
        rms_list.append(float(np.sqrt((local ** 2).mean())))
        corr_list.append(correlation_length(fx))
    mean_dx = np.array(mean_dx) / art_px
    rms = np.array(rms_list) / art_px
    corr = np.array(corr_list) / art_px
    # Dominant sway frequency of the whole canopy.
    signal = mean_dx - mean_dx.mean()
    spectrum = np.abs(np.fft.rfft(signal))
    freqs = np.fft.rfftfreq(len(signal), 1.0 / fps)
    peak = freqs[1:][np.argmax(spectrum[1:])] if len(signal) > 4 else 0.0
    print(f"{label:>10} t={t0:.1f}-{t1:.1f}s frames={len(frames)} "
          f"canopy sway peak-to-peak={np.ptp(mean_dx):.2f} art px, "
          f"local (non-uniform) motion rms={np.median(rms):.2f} (p90 {np.percentile(rms, 90):.2f}) art px, "
          f"correlation length={np.median(corr):.1f} art px, dominant freq={peak:.2f} Hz")


if __name__ == "__main__":
    main()
