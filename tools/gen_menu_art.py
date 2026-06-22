#!/usr/bin/env python3
"""Procedural generator for the main-menu art of "Sunset of the World".

Outputs two self-contained SVG assets used by the web main menu:
  assets/img/background.svg  -- forest-at-sunset scene with glowing lava cracks
  assets/img/logo.svg        -- eclipse emblem + "SUNSET OF THE WORLD" title

These are stylized placeholders that match the reference art direction (warm
golden sunset, dark pine forest, molten cracks, ruined cabins). Replace the
SVGs with real key art at any time, or re-run this script to regenerate.

Usage:  python3 tools/gen_menu_art.py
"""
import os
import math
import random

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
IMG = os.path.join(ROOT, "assets", "img")

W, H = 1920, 1080


def pine(x, base, h, w, color, op=1.0):
    """A simple layered pine silhouette anchored at (x, base)."""
    out = []
    trunk_w = max(2.0, w * 0.06)
    out.append(
        f'<rect x="{x - trunk_w / 2:.1f}" y="{base - h * 0.08:.1f}" '
        f'width="{trunk_w:.1f}" height="{h * 0.14:.1f}" '
        f'fill="{color}" opacity="{op:.2f}"/>'
    )
    tiers = 4
    for i in range(tiers):
        ww = w * (1.0 - 0.18 * i)
        y_bottom = base - h * 0.08 - i * (h * 0.22)
        y_top = y_bottom - h * 0.42
        out.append(
            f'<polygon points="{x - ww / 2:.1f},{y_bottom:.1f} '
            f'{x + ww / 2:.1f},{y_bottom:.1f} {x:.1f},{y_top:.1f}" '
            f'fill="{color}" opacity="{op:.2f}"/>'
        )
    return "".join(out)


def tree_row(base, count, h_range, w_range, color, op, jitter=0.0):
    out = []
    for i in range(count):
        x = (i + 0.5) / count * W + random.uniform(-jitter, jitter)
        h = random.uniform(*h_range)
        w = random.uniform(*w_range)
        out.append(pine(x, base + random.uniform(-6, 10), h, w, color, op))
    return "".join(out)


def cabin(x, base, scale, op=1.0):
    bw, bh = 150 * scale, 90 * scale
    rh = 60 * scale
    body = (
        f'<rect x="{x:.1f}" y="{base - bh:.1f}" width="{bw:.1f}" height="{bh:.1f}" '
        f'fill="#0c0a08" opacity="{op:.2f}"/>'
    )
    roof = (
        f'<polygon points="{x - 12 * scale:.1f},{base - bh:.1f} '
        f'{x + bw + 12 * scale:.1f},{base - bh:.1f} '
        f'{x + bw / 2:.1f},{base - bh - rh:.1f}" fill="#100c09" opacity="{op:.2f}"/>'
    )
    win = (
        f'<rect x="{x + bw * 0.34:.1f}" y="{base - bh * 0.66:.1f}" '
        f'width="{bw * 0.22:.1f}" height="{bh * 0.3:.1f}" '
        f'fill="#ffb347" opacity="{0.5 * op:.2f}" filter="url(#glow)"/>'
    )
    return body + roof + win


def lava_crack(x, y, angle, length, depth, parts):
    """Recursive branching molten crack as a glowing polyline."""
    if depth <= 0 or length < 24:
        return
    pts = [(x, y)]
    cx, cy, ca = x, y, angle
    segs = max(2, int(length / 40))
    seg = length / segs
    for _ in range(segs):
        ca += random.uniform(-0.35, 0.35)
        cx += math.cos(ca) * seg
        cy += math.sin(ca) * seg * 0.55  # foreshorten vertically
        pts.append((cx, cy))
    d = "M " + " L ".join(f"{px:.1f} {py:.1f}" for px, py in pts)
    width = 1.0 + depth * 1.4
    parts.append(
        f'<path d="{d}" fill="none" stroke="#ff7a16" stroke-width="{width + 5:.1f}" '
        f'stroke-linecap="round" opacity="0.55" filter="url(#glow)"/>'
    )
    parts.append(
        f'<path d="{d}" fill="none" stroke="#ffd66b" stroke-width="{max(1.0, width):.1f}" '
        f'stroke-linecap="round" opacity="0.95"/>'
    )
    if random.random() < 0.8:
        lava_crack(cx, cy, angle + random.uniform(0.4, 1.1),
                   length * random.uniform(0.45, 0.7), depth - 1, parts)
    if random.random() < 0.5:
        lava_crack(pts[len(pts) // 2][0], pts[len(pts) // 2][1],
                   angle - random.uniform(0.4, 1.1),
                   length * random.uniform(0.4, 0.6), depth - 1, parts)


def build_background():
    random.seed(7)
    horizon = H * 0.5
    defs = f'''<defs>
  <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#120c08"/>
    <stop offset="0.30" stop-color="#34200f"/>
    <stop offset="0.43" stop-color="#7e441a"/>
    <stop offset="0.49" stop-color="#cf7a24"/>
    <stop offset="0.50" stop-color="#f2b14b"/>
  </linearGradient>
  <radialGradient id="sun" cx="0.62" cy="1.0" r="0.55">
    <stop offset="0" stop-color="#ffe9b0" stop-opacity="0.95"/>
    <stop offset="0.35" stop-color="#ffb84f" stop-opacity="0.55"/>
    <stop offset="1" stop-color="#ffb84f" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="ground" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#2a1a09"/>
    <stop offset="0.5" stop-color="#160e05"/>
    <stop offset="1" stop-color="#080502"/>
  </linearGradient>
  <radialGradient id="vig" cx="0.5" cy="0.46" r="0.78">
    <stop offset="0.5" stop-color="#000000" stop-opacity="0"/>
    <stop offset="1" stop-color="#000000" stop-opacity="0.62"/>
  </radialGradient>
  <filter id="glow" x="-60%" y="-60%" width="220%" height="220%">
    <feGaussianBlur stdDeviation="6" result="b"/>
    <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
  </filter>
  <filter id="soft"><feGaussianBlur stdDeviation="7"/></filter>
</defs>'''

    p = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" '
         f'viewBox="0 0 {W} {H}">', defs]
    p.append(f'<rect width="{W}" height="{H}" fill="url(#sky)"/>')
    # sun glow sitting on the horizon
    p.append(f'<ellipse cx="{W*0.62:.0f}" cy="{horizon:.0f}" rx="{W*0.55:.0f}" '
             f'ry="{H*0.5:.0f}" fill="url(#sun)"/>')
    # distant hazy ridge
    p.append(f'<g filter="url(#soft)">{tree_row(horizon+6, 60, (60,110), (26,46), "#6a4a2c", 0.40, 8)}</g>')
    # mid forest
    p.append(tree_row(horizon + 26, 46, (120, 200), (40, 70), "#241a10", 0.92, 10))
    # ruined cabins on the tree line
    p.append(cabin(W * 0.20, horizon + 40, 1.15))
    p.append(cabin(W * 0.40, horizon + 30, 0.85, 0.95))
    # ground
    p.append(f'<rect x="0" y="{horizon:.0f}" width="{W}" height="{H-horizon:.0f}" fill="url(#ground)"/>')
    # glowing lava cracks across the foreground ground
    cracks = []
    for sx in (W * 0.30, W * 0.52, W * 0.72, W * 0.88):
        lava_crack(sx, horizon + random.uniform(70, 140),
                   random.uniform(0.6, 2.4), random.uniform(360, 520), 3, cracks)
    p.append("".join(cracks))
    # near dark pines framing left & right
    p.append(pine(W * 0.06, H * 0.98, H * 0.92, 240, "#070504", 1.0))
    p.append(pine(W * 0.95, H * 0.96, H * 0.80, 210, "#070504", 1.0))
    # floating ember particles
    random.seed(21)
    for _ in range(70):
        ex, ey = random.uniform(0, W), random.uniform(H * 0.18, H * 0.62)
        r = random.uniform(0.8, 2.6)
        p.append(f'<circle cx="{ex:.0f}" cy="{ey:.0f}" r="{r:.1f}" fill="#ffcf7a" '
                 f'opacity="{random.uniform(0.15,0.7):.2f}"/>')
    # vignette
    p.append(f'<rect width="{W}" height="{H}" fill="url(#vig)"/>')
    p.append("</svg>")
    return "".join(p)


def build_logo():
    w, h = 620, 320
    defs = '''<defs>
  <radialGradient id="corona" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0.40" stop-color="#2a0d00" stop-opacity="0"/>
    <stop offset="0.55" stop-color="#ff7b16" stop-opacity="0.95"/>
    <stop offset="0.72" stop-color="#ffb347" stop-opacity="0.8"/>
    <stop offset="1" stop-color="#ffb347" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="gold" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#fff3d4"/>
    <stop offset="0.5" stop-color="#e8c87f"/>
    <stop offset="1" stop-color="#b98a3e"/>
  </linearGradient>
  <filter id="lglow" x="-60%" y="-60%" width="220%" height="220%">
    <feGaussianBlur stdDeviation="3.5" result="b"/>
    <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge>
  </filter>
</defs>'''
    cx, cy = w / 2, 92
    p = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" '
         f'viewBox="0 0 {w} {h}">', defs]
    # corona glow
    p.append(f'<circle cx="{cx}" cy="{cy}" r="78" fill="url(#corona)"/>')
    # flame tongues around the disc
    tongues = []
    for k in range(18):
        a = k / 18 * math.tau
        r1, r2 = 50, 50 + (14 if k % 2 == 0 else 26)
        x1, y1 = cx + math.cos(a) * r1, cy + math.sin(a) * r1
        x2, y2 = cx + math.cos(a) * r2, cy + math.sin(a) * r2
        tongues.append(f'<line x1="{x1:.1f}" y1="{y1:.1f}" x2="{x2:.1f}" y2="{y2:.1f}" '
                       f'stroke="#ff9a32" stroke-width="3" stroke-linecap="round" opacity="0.8"/>')
    p.append(f'<g filter="url(#lglow)">{"".join(tongues)}</g>')
    # dark eclipse disc with thin bright rim
    p.append(f'<circle cx="{cx}" cy="{cy}" r="48" fill="#0c0a09"/>')
    p.append(f'<circle cx="{cx}" cy="{cy}" r="48" fill="none" stroke="#ffd27a" '
             f'stroke-width="2.5" opacity="0.9" filter="url(#lglow)"/>')
    # title
    p.append(f'<text x="{cx}" y="218" text-anchor="middle" '
             f'font-family="DejaVu Serif, Georgia, serif" font-weight="bold" '
             f'font-size="76" letter-spacing="6" fill="url(#gold)" '
             f'filter="url(#lglow)">SUNSET</text>')
    p.append(f'<text x="{cx}" y="266" text-anchor="middle" '
             f'font-family="DejaVu Serif, Georgia, serif" '
             f'font-size="30" letter-spacing="13" fill="url(#gold)">OF THE WORLD</text>')
    p.append("</svg>")
    return "".join(p)


def main():
    os.makedirs(IMG, exist_ok=True)
    with open(os.path.join(IMG, "background.svg"), "w", encoding="utf-8") as f:
        f.write(build_background())
    with open(os.path.join(IMG, "logo.svg"), "w", encoding="utf-8") as f:
        f.write(build_logo())
    print("wrote", os.path.join(IMG, "background.svg"))
    print("wrote", os.path.join(IMG, "logo.svg"))


if __name__ == "__main__":
    main()
