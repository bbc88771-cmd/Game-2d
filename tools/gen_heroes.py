#!/usr/bin/env python3
"""Procedural class portraits for the fullscreen character-select screen.
Stylized bust + class icon per class (SVG). Replace with real art anytime."""
import os

IMG = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets", "img")
W, H = 260, 320


def bust(color):
    # shoulders + head silhouette
    return (f'<path d="M40 {H} Q40 232 130 218 Q220 232 220 {H} Z" fill="{color}"/>'
            f'<circle cx="130" cy="176" r="40" fill="{color}"/>')


def icon(kind):
    cx = 130
    if kind == "staff":
        return (f'<line x1="{cx}" y1="70" x2="{cx}" y2="150" stroke="#d8c7ff" stroke-width="5"/>'
                f'<circle cx="{cx}" cy="64" r="15" fill="#bff6ff" filter="url(#g)"/>')
    if kind == "shield":
        return (f'<path d="M{cx-30} 70 L{cx+30} 70 L{cx+30} 110 Q{cx} 140 {cx-30} 110 Z" '
                f'fill="#cfe0ff" stroke="#8fb6ff" stroke-width="3" filter="url(#g)"/>')
    if kind == "sword":
        return (f'<line x1="{cx}" y1="56" x2="{cx}" y2="132" stroke="#e9e2cf" stroke-width="7" filter="url(#g)"/>'
                f'<line x1="{cx-20}" y1="120" x2="{cx+20}" y2="120" stroke="#c9a24a" stroke-width="7"/>')
    if kind == "kunai":
        return (f'<polygon points="{cx},58 {cx-12},96 {cx+12},96" fill="#cfd6dd" filter="url(#g)"/>'
                f'<rect x="{cx-5}" y="96" width="10" height="36" fill="#3a3f46"/>'
                f'<circle cx="{cx}" cy="138" r="9" fill="none" stroke="#cfd6dd" stroke-width="4"/>')
    if kind == "gear":
        spokes = "".join(
            f'<rect x="{cx-4}" y="48" width="8" height="14" transform="rotate({a} {cx} 78)" fill="#ffb347"/>'
            for a in range(0, 360, 45))
        return (f'<g filter="url(#g)">{spokes}<circle cx="{cx}" cy="78" r="18" fill="#caa05a"/>'
                f'<circle cx="{cx}" cy="78" r="7" fill="#2a1a08"/></g>')
    return ""


def portrait(c_top, c_mid, accent, ic):
    defs = (f'<defs><linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">'
            f'<stop offset="0" stop-color="{c_top}"/><stop offset="1" stop-color="{c_mid}"/></linearGradient>'
            f'<radialGradient id="vig" cx="0.5" cy="0.42" r="0.7">'
            f'<stop offset="0.55" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.6"/></radialGradient>'
            f'<filter id="g" x="-60%" y="-60%" width="220%" height="220%">'
            f'<feGaussianBlur stdDeviation="3" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter></defs>')
    body = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">', defs,
            f'<rect width="{W}" height="{H}" fill="url(#bg)"/>',
            f'<ellipse cx="130" cy="190" rx="120" ry="120" fill="{accent}" opacity="0.18"/>',
            bust("#0d0b0a"),
            icon(ic),
            f'<rect width="{W}" height="{H}" fill="url(#vig)"/>',
            f'<rect width="{W}" height="{H}" fill="none" stroke="#000" stroke-opacity="0.4" stroke-width="2"/>',
            '</svg>']
    return "".join(body)


HEROES = {
    "tessi":     ("#2a2150", "#140e2c", "#7a5cff", "staff"),
    "amira":     ("#243a52", "#10202e", "#5a9bff", "shield"),
    "swordsman": ("#4a2420", "#1e0f0d", "#ff6a4a", "sword"),
    "kaijo":     ("#1e3a2c", "#0c1a12", "#43d6a0", "kunai"),
    "walter":    ("#4a3416", "#1e1408", "#ffb347", "gear"),
}


def main():
    os.makedirs(IMG, exist_ok=True)
    for name, (a, b, ac, ic) in HEROES.items():
        with open(os.path.join(IMG, f"hero_{name}.svg"), "w", encoding="utf-8") as f:
            f.write(portrait(a, b, ac, ic))
        print("wrote", f"hero_{name}.svg")


if __name__ == "__main__":
    main()
