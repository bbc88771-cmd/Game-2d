#!/usr/bin/env python3
"""Procedural thumbnails for the 5 islands shown in the lobby info panel.
Stylized biome scenes (SVG) — replace with real key art anytime."""
import os, math, random

IMG = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets", "img")
W, H = 480, 300


def pine(x, base, h, w, color, op=1.0):
    out = [f'<rect x="{x-w*0.05:.1f}" y="{base-h*0.06:.1f}" width="{w*0.1:.1f}" '
           f'height="{h*0.12:.1f}" fill="{color}" opacity="{op:.2f}"/>']
    for i in range(4):
        ww = w * (1 - 0.18 * i)
        yb = base - h * 0.06 - i * h * 0.22
        out.append(f'<polygon points="{x-ww/2:.1f},{yb:.1f} {x+ww/2:.1f},{yb:.1f} '
                   f'{x:.1f},{yb-h*0.42:.1f}" fill="{color}" opacity="{op:.2f}"/>')
    return "".join(out)


def frame(body, defs=""):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" '
            f'viewBox="0 0 {W} {H}">{defs}{body}'
            f'<rect width="{W}" height="{H}" fill="none" stroke="#000" stroke-opacity="0.4" '
            f'stroke-width="2"/></svg>')


def grad(id_, *stops):
    s = "".join(f'<stop offset="{o}" stop-color="{c}"/>' for o, c in stops)
    return f'<linearGradient id="{id_}" x1="0" y1="0" x2="0" y2="1">{s}</linearGradient>'


def start_island():
    random.seed(1); hz = H * 0.55
    d = f'<defs>{grad("s",(0,"#1c2a12"),(0.4,"#5a6a2a"),(0.55,"#d9a23e"))}{grad("g",(0,"#3a4a1c"),(1,"#16200a"))}</defs>'
    b = [f'<rect width="{W}" height="{H}" fill="url(#s)"/>',
         f'<circle cx="{W*0.6:.0f}" cy="{hz:.0f}" r="46" fill="#ffd87a" opacity="0.85"/>']
    for i in range(11):
        b.append(pine(20 + i * 46, hz + 8, random.uniform(60, 100), random.uniform(26, 40), "#243214", 0.9))
    b.append(f'<rect y="{hz:.0f}" width="{W}" height="{H-hz:.0f}" fill="url(#g)"/>')
    b.append(pine(W * 0.12, H * 0.98, 150, 56, "#0c1206"))
    return frame("".join(b), d)


def snow_island():
    random.seed(2); hz = H * 0.55
    d = f'<defs>{grad("sk",(0,"#15243c"),(0.5,"#3f6a8c"),(0.56,"#cfe6f2"))}{grad("sn",(0,"#dbe9f2"),(1,"#9fb8c8"))}</defs>'
    b = [f'<rect width="{W}" height="{H}" fill="url(#sk)"/>']
    # aurora
    b.append(f'<path d="M0 {H*0.2:.0f} Q {W*0.5:.0f} {H*0.05:.0f} {W} {H*0.22:.0f}" stroke="#7fe6c8" '
             f'stroke-width="10" fill="none" opacity="0.35"/>')
    for i in range(11):
        b.append(pine(20 + i * 46, hz + 8, random.uniform(55, 95), random.uniform(24, 38), "#2c3b46", 0.95))
    b.append(f'<rect y="{hz:.0f}" width="{W}" height="{H-hz:.0f}" fill="url(#sn)"/>')
    for _ in range(60):
        b.append(f'<circle cx="{random.uniform(0,W):.0f}" cy="{random.uniform(0,H):.0f}" r="{random.uniform(1,2.5):.1f}" fill="#fff" opacity="0.8"/>')
    return frame("".join(b), d)


def dead_island():
    random.seed(3); hz = H * 0.58
    d = f'<defs>{grad("sk",(0,"#3a2410"),(0.5,"#8a5a26"),(0.58,"#e0a64c"))}{grad("sa",(0,"#caa05a"),(1,"#7a5a2c"))}</defs>'
    b = [f'<rect width="{W}" height="{H}" fill="url(#sk)"/>',
         f'<circle cx="{W*0.5:.0f}" cy="{hz*0.7:.0f}" r="38" fill="#ffdca0" opacity="0.7"/>',
         f'<rect y="{hz:.0f}" width="{W}" height="{H-hz:.0f}" fill="url(#sa)"/>']
    # dunes
    for k,op in ((0.66,0.5),(0.78,0.6)):
        b.append(f'<path d="M0 {H*k:.0f} Q {W*0.5:.0f} {H*(k-0.08):.0f} {W} {H*k:.0f} L {W} {H} L 0 {H} Z" fill="#8a6634" opacity="{op}"/>')
    # dead trees
    for x in (W*0.2, W*0.7):
        b.append(f'<path d="M{x:.0f} {hz:.0f} L {x:.0f} {hz-70:.0f} M {x:.0f} {hz-45:.0f} L {x-22:.0f} {hz-66:.0f} M {x:.0f} {hz-52:.0f} L {x+20:.0f} {hz-74:.0f}" stroke="#3a2814" stroke-width="5" fill="none"/>')
    return frame("".join(b), d)


def swamp_island():
    random.seed(4); hz = H * 0.5
    d = f'<defs>{grad("sk",(0,"#16261c"),(0.5,"#3c5238"),(0.55,"#6e7a4a"))}{grad("wt",(0,"#2c3a26"),(1,"#101a12"))}</defs>'
    b = [f'<rect width="{W}" height="{H}" fill="url(#sk)"/>']
    for i in range(10):
        b.append(pine(20 + i * 50, hz + 6, random.uniform(45, 80), random.uniform(22, 34), "#1b2a1c", 0.9))
    b.append(f'<rect y="{hz:.0f}" width="{W}" height="{H-hz:.0f}" fill="url(#wt)"/>')
    # reeds
    for _ in range(26):
        x = random.uniform(0, W); yh = random.uniform(18, 40)
        b.append(f'<line x1="{x:.0f}" y1="{hz+random.uniform(8,60):.0f}" x2="{x+random.uniform(-4,4):.0f}" y2="{hz-yh:.0f}" stroke="#3c5226" stroke-width="2"/>')
    # mist
    b.append(f'<rect y="{hz-10:.0f}" width="{W}" height="40" fill="#9fb08a" opacity="0.18"/>')
    return frame("".join(b), d)


def magic_island():
    random.seed(5); hz = H * 0.6
    d = (f'<defs>{grad("sk",(0,"#1a1030"),(0.5,"#3a2a6a"),(0.6,"#6a4aa0"))}'
         f'<radialGradient id="orb" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="#bff6ff"/>'
         f'<stop offset="0.5" stop-color="#5ad6ff" stop-opacity="0.7"/><stop offset="1" stop-color="#5ad6ff" stop-opacity="0"/></radialGradient></defs>')
    b = [f'<rect width="{W}" height="{H}" fill="url(#sk)"/>']
    for i in range(10):
        b.append(pine(20 + i * 50, hz + 6, random.uniform(50, 85), random.uniform(22, 34), "#2a1c44", 0.9))
    b.append(f'<rect y="{hz:.0f}" width="{W}" height="{H-hz:.0f}" fill="#180f2a"/>')
    b.append(f'<ellipse cx="{W*0.5:.0f}" cy="{hz:.0f}" rx="120" ry="60" fill="url(#orb)"/>')
    # mushrooms
    for x,c in ((W*0.2,"#e85aa0"),(W*0.8,"#5ad6ff"),(W*0.66,"#c08aff")):
        b.append(f'<rect x="{x-3:.0f}" y="{hz+8:.0f}" width="6" height="18" fill="#d8cfe6"/>'
                 f'<ellipse cx="{x:.0f}" cy="{hz+8:.0f}" rx="14" ry="9" fill="{c}"/>')
    for _ in range(40):
        b.append(f'<circle cx="{random.uniform(0,W):.0f}" cy="{random.uniform(0,hz):.0f}" r="{random.uniform(1,2.4):.1f}" fill="#cfe9ff" opacity="{random.uniform(0.3,0.9):.2f}"/>')
    return frame("".join(b), d)


def main():
    os.makedirs(IMG, exist_ok=True)
    for name, fn in [("loc_start", start_island), ("loc_snow", snow_island),
                     ("loc_dead", dead_island), ("loc_swamp", swamp_island), ("loc_magic", magic_island)]:
        with open(os.path.join(IMG, name + ".svg"), "w", encoding="utf-8") as f:
            f.write(fn())
        print("wrote", name + ".svg")


if __name__ == "__main__":
    main()
