#!/usr/bin/env python3
"""Richer procedural class portraits for the character-select screen.
Each class gets a distinct bust: headwear, face, cloak/armor, a class item,
rim light and a tinted scene backdrop. Stylized — swap for real art anytime."""
import os

IMG = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "assets", "img")
W, H = 300, 360
CX = 150


def defs(c_top, c_bot, accent):
    return (
        '<defs>'
        f'<linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{c_top}"/><stop offset="1" stop-color="{c_bot}"/></linearGradient>'
        f'<radialGradient id="acc" cx="0.5" cy="0.42" r="0.55">'
        f'<stop offset="0" stop-color="{accent}" stop-opacity="0.55"/>'
        f'<stop offset="1" stop-color="{accent}" stop-opacity="0"/></radialGradient>'
        '<radialGradient id="vig" cx="0.5" cy="0.45" r="0.72">'
        '<stop offset="0.55" stop-color="#000" stop-opacity="0"/>'
        '<stop offset="1" stop-color="#000" stop-opacity="0.62"/></radialGradient>'
        '<linearGradient id="rim" x1="0" y1="0" x2="1" y2="0">'
        '<stop offset="0" stop-color="#fff" stop-opacity="0.0"/>'
        '<stop offset="1" stop-color="#fff" stop-opacity="0.16"/></linearGradient>'
        '<filter id="soft" x="-40%" y="-40%" width="180%" height="180%"><feGaussianBlur stdDeviation="4"/></filter>'
        '<filter id="glow" x="-60%" y="-60%" width="220%" height="220%">'
        '<feGaussianBlur stdDeviation="4" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>'
        '</defs>')


def backdrop(scene):
    return (f'<rect width="{W}" height="{H}" fill="url(#bg)"/>'
            f'<ellipse cx="{CX}" cy="150" rx="150" ry="150" fill="url(#acc)"/>'
            + scene)


def cloak(color, dark):
    # shoulders + collar
    return (f'<path d="M18 {H} Q24 250 {CX} 224 Q276 250 282 {H} Z" fill="{color}"/>'
            f'<path d="M18 {H} Q24 250 {CX} 224 Q{CX} 252 {CX} {H} Z" fill="{dark}" opacity="0.5"/>'
            f'<path d="M110 250 Q{CX} 300 190 250 L190 224 Q{CX} 244 110 224 Z" fill="{dark}"/>')


def head(skin, hair=None):
    s = ''
    if hair:
        s += f'<path d="M96 150 Q92 86 {CX} 84 Q208 86 204 150 Q204 110 {CX} 108 Q96 110 96 150 Z" fill="{hair}"/>'
    s += f'<rect x="134" y="188" width="32" height="26" rx="8" fill="{skin}"/>'
    s += f'<ellipse cx="{CX}" cy="150" rx="50" ry="56" fill="{skin}"/>'
    return s


def eyes(color="#2a2018", glow=None):
    g = ' filter="url(#glow)"' if glow else ''
    col = glow or color
    return (f'<ellipse cx="131" cy="150" rx="6" ry="8" fill="{col}"{g}/>'
            f'<ellipse cx="169" cy="150" rx="6" ry="8" fill="{col}"{g}/>')


def vig():
    return (f'<path d="M150 84 Q208 86 204 150 Q204 180 188 200 L188 120 Q180 96 150 92 Z" fill="url(#rim)"/>'
            f'<rect width="{W}" height="{H}" fill="url(#vig)"/>'
            f'<rect width="{W}" height="{H}" fill="none" stroke="#000" stroke-opacity="0.45" stroke-width="3"/>')


def svg(parts):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
            + "".join(parts) + '</svg>')


def pines(color):
    out = ['<g opacity="0.5">']
    for x in (40, 80, 230, 262):
        out.append(f'<polygon points="{x-16},300 {x+16},300 {x},250" fill="{color}"/>'
                   f'<polygon points="{x-13},275 {x+13},275 {x},235" fill="{color}"/>')
    out.append('</g>')
    return "".join(out)


def tessi():
    p = [defs("#352a66", "#140e2c", "#7a5cff"),
         backdrop(pines("#1c1640")),
         # staff with orb (left)
         '<line x1="64" y1="120" x2="64" y2="330" stroke="#5a4a36" stroke-width="7"/>',
         '<circle cx="64" cy="108" r="18" fill="#bff6ff" filter="url(#glow)"/>',
         cloak("#3a2c70", "#241a4a"),
         head("#ecd3b8", hair="#2a1f12"),
         # deep hood
         '<path d="M88 150 Q84 70 150 66 Q216 70 212 150 Q200 96 150 90 Q100 96 88 150 Z" fill="#2a2050"/>',
         # blindfold band
         f'<rect x="100" y="140" width="100" height="20" rx="4" fill="#cdbff2"/>',
         '<path d="M100 150 L200 150" stroke="#8f7fd0" stroke-width="2" opacity="0.6"/>',
         # floating motes
         '<circle cx="96" cy="120" r="2.5" fill="#cfe9ff"/><circle cx="206" cy="132" r="2" fill="#cfe9ff"/>'
         '<circle cx="120" cy="96" r="1.6" fill="#cfe9ff"/>',
         vig()]
    return svg(p)


def amira():
    p = [defs("#2c4660", "#10202e", "#5a9bff"),
         backdrop(pines("#16242e")),
         cloak("#3a4a5a", "#202c36"),
         # armored shoulders
         '<path d="M70 250 Q60 214 104 210 L104 250 Z" fill="#5a6a78"/>'
         '<path d="M230 250 Q240 214 196 210 L196 250 Z" fill="#5a6a78"/>',
         head("#caa07a"),
         # helmet
         '<path d="M96 150 Q92 78 150 76 Q208 78 204 150 L204 132 Q150 96 96 132 Z" fill="#6a7a88"/>',
         '<path d="M96 132 Q150 96 204 132 L204 150 L96 150 Z" fill="#7d8f9d"/>',
         # visor slit + nasal
         '<rect x="104" y="138" width="92" height="12" rx="4" fill="#10181e"/>',
         '<rect x="146" y="138" width="8" height="40" fill="#5a6a78"/>',
         f'{eyes(glow="#9fdcff")}',
         # shield (right)
         '<path d="M250 150 L290 150 L290 196 Q270 222 250 196 Z" fill="#cfe0ff" stroke="#8fb6ff" stroke-width="3" filter="url(#glow)"/>',
         '<line x1="270" y1="156" x2="270" y2="200" stroke="#8fb6ff" stroke-width="2"/>',
         vig()]
    return svg(p)


def swordsman():
    p = [defs("#5a2c24", "#1e0f0d", "#ff6a4a"),
         backdrop(pines("#2a1512")),
         cloak("#6a2c22", "#3c160f"),
         head("#d2a884", hair="#1c130b"),
         # tousled hair
         '<path d="M98 132 Q110 92 150 90 Q192 92 202 132 Q188 104 150 102 Q112 104 98 132 Z" fill="#241810"/>',
         eyes("#3a2a1a"),
         # scar on cheek
         '<line x1="176" y1="138" x2="184" y2="166" stroke="#a85a4a" stroke-width="3"/>',
         '<path d="M132 176 Q150 186 168 176" stroke="#7a4a3a" stroke-width="2" fill="none"/>',
         # sword hilt over shoulder (right, diagonal)
         '<line x1="214" y1="300" x2="262" y2="150" stroke="#3a2c1c" stroke-width="8"/>',
         '<line x1="252" y1="150" x2="272" y2="150" stroke="#caa24a" stroke-width="8"/>',
         '<circle cx="262" cy="138" r="7" fill="#caa24a"/>',
         vig()]
    return svg(p)


def kaijo():
    p = [defs("#234034", "#0c1a12", "#43d6a0"),
         backdrop(pines("#102018")),
         cloak("#1e2a22", "#10160f"),
         # scarf
         '<path d="M110 232 Q150 250 190 232 L196 270 Q150 252 104 270 Z" fill="#3a8f6a"/>',
         head("#c89a76", hair="#0e1410"),
         # hood
         '<path d="M88 156 Q82 70 150 64 Q218 70 212 156 Q200 92 150 86 Q100 92 88 156 Z" fill="#16201a"/>',
         # face mask (lower half)
         '<path d="M104 152 Q150 150 196 152 L196 192 Q150 214 104 192 Z" fill="#1a241e"/>',
         '<line x1="104" y1="168" x2="196" y2="168" stroke="#0e1410" stroke-width="2"/>',
         eyes(glow="#5cffc0"),
         # kunai (right)
         '<polygon points="262,120 250,152 274,152" fill="#cfd6dd" filter="url(#glow)"/>',
         '<rect x="257" y="152" width="10" height="40" fill="#2a2f36"/>',
         '<circle cx="262" cy="196" r="9" fill="none" stroke="#cfd6dd" stroke-width="4"/>',
         vig()]
    return svg(p)


def walter():
    p = [defs("#5a3e1c", "#1e1408", "#ffb347"),
         backdrop(pines("#241a0c")),
         cloak("#5a4326", "#332512"),
         # gear pauldron (left)
         '<g filter="url(#glow)">'
         + "".join(f'<rect x="84" y="200" width="8" height="14" transform="rotate({a} 88 230)" fill="#caa05a"/>' for a in range(0, 360, 45))
         + '<circle cx="88" cy="230" r="18" fill="#b8893e"/><circle cx="88" cy="230" r="7" fill="#2a1a08"/></g>',
         head("#d8b48c", hair="#3a2a18"),
         # leather cap + goggles on forehead
         '<path d="M96 130 Q100 92 150 90 Q200 92 204 130 Q150 104 96 130 Z" fill="#4a3a22"/>',
         '<rect x="108" y="120" width="84" height="6" rx="3" fill="#2a1d10"/>',
         '<circle cx="126" cy="120" r="13" fill="#2a1d10"/><circle cx="126" cy="120" r="8" fill="#8fd0ff"/>',
         '<circle cx="174" cy="120" r="13" fill="#2a1d10"/><circle cx="174" cy="120" r="8" fill="#8fd0ff"/>',
         eyes("#3a2a1a"),
         # robot Chip (right)
         '<g filter="url(#glow)"><rect x="244" y="150" width="40" height="36" rx="6" fill="#7d6a52"/>'
         '<rect x="250" y="158" width="28" height="14" rx="3" fill="#10181e"/>'
         '<circle cx="258" cy="165" r="4" fill="#ffb347"/><circle cx="270" cy="165" r="4" fill="#ffb347"/>'
         '<line x1="264" y1="150" x2="264" y2="140" stroke="#7d6a52" stroke-width="3"/><circle cx="264" cy="137" r="3" fill="#ffb347"/></g>',
         vig()]
    return svg(p)


def main():
    os.makedirs(IMG, exist_ok=True)
    for name, fn in [("tessi", tessi), ("amira", amira), ("swordsman", swordsman),
                     ("kaijo", kaijo), ("walter", walter)]:
        with open(os.path.join(IMG, f"hero_{name}.svg"), "w", encoding="utf-8") as f:
            f.write(fn())
        print("wrote", f"hero_{name}.svg")


if __name__ == "__main__":
    main()
