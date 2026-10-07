# -*- coding: utf-8 -*-
import os, random
from collections import deque
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(BASE)
ASSETS = os.path.join(ROOT, "Catdoku", "Assets", "Resources", "Images", "GameView")
OUT = os.path.join(BASE, "mockups")
os.makedirs(OUT, exist_ok=True)
MA = os.path.join(BASE, "mockup_assets")
CAT_SRC = os.path.join(MA, "Super_cute_cartoon_cat_face_ga_2026-10-06T00-17-18.png")
MOUSE_SRC = os.path.join(MA, "Cute_cartoon_mouse_face_game_i_2026-10-05T15-33-52.png")
random.seed(5)

def hsv_color(color_id, sat=0.55, val=0.92):
    h = (color_id * 0.618033988749895) % 1.0
    i = int(h * 6) % 6
    f = h * 6 - int(h * 6)
    p = val * (1 - sat); q = val * (1 - f * sat); t = val * (1 - (1 - f) * sat)
    table = [(val, t, p), (q, val, p), (p, val, t), (p, q, val), (t, p, val), (val, p, q)]
    r, g, b = table[i]
    return (int(r * 255), int(g * 255), int(b * 255), 255)

def clean_sprite(src, out_path, thr=42):
    im = Image.open(src).convert("RGBA")
    a = np.array(im).astype(np.int16)
    h, w = a.shape[:2]
    rgb = a[..., :3]
    white = (rgb[..., 0] > 255 - thr) & (rgb[..., 1] > 255 - thr) & (rgb[..., 2] > 255 - thr)
    bg = np.zeros((h, w), bool)
    dq = deque()
    for x in range(w):
        for y in (0, h - 1):
            if white[y, x] and not bg[y, x]:
                bg[y, x] = True; dq.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if white[y, x] and not bg[y, x]:
                bg[y, x] = True; dq.append((y, x))
    while dq:
        y, x = dq.popleft()
        for dy, dx in ((1,0),(-1,0),(0,1),(0,-1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < h and 0 <= nx < w and white[ny, nx] and not bg[ny, nx]:
                bg[ny, nx] = True; dq.append((ny, nx))
    a[..., 3][bg] = 0
    res = Image.fromarray(a.astype(np.uint8), "RGBA")
    alpha = res.split()[3].filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1))
    res.putalpha(alpha)
    bbox = alpha.getbbox()
    if bbox:
        res = res.crop(bbox)
    res.save(out_path)
    return res

# ---------- sprites ----------
cat = clean_sprite(CAT_SRC, os.path.join(OUT, "_cat_new.png"))
mouse = clean_sprite(MOUSE_SRC, os.path.join(OUT, "_mouse_clean.png"))
cross = Image.open(os.path.join(ASSETS, "IconCross.png")).convert("RGBA")

# ---------- new-rule solver (strict adjacency incl diagonal) ----------
def adj8(a, b):
    return abs(a[0]-b[0]) <= 1 and abs(a[1]-b[1]) <= 1 and a != b

def solve_strict(N):
    cat_used = [False]*N
    mouse_used = [False]*N
    cats = [None]*N
    mice = [None]*N
    def row_ok(r, cc, mc):
        if cc == mc or abs(cc-mc) < 2:
            return False
        if r > 0:
            for p in (cats[r-1], mice[r-1]):
                if adj8((r, cc), (r-1, p)) or adj8((r, mc), (r-1, p)):
                    return False
        return True
    def bt(r):
        if r == N:
            return True
        ccands = list(range(N)); random.shuffle(ccands)
        for cc in ccands:
            if cat_used[cc]:
                continue
            mcands = list(range(N)); random.shuffle(mcands)
            for mc in mcands:
                if mouse_used[mc] or not row_ok(r, cc, mc):
                    continue
                cat_used[cc] = mouse_used[mc] = True
                cats[r] = cc; mice[r] = mc
                if bt(r+1):
                    return True
                cat_used[cc] = mouse_used[mc] = False
        return False
    if bt(0):
        return [(r, cats[r]) for r in range(N)], [(r, mice[r]) for r in range(N)]
    return None, None

def build_regions(N, cats, mice):
    """Connected regions, each containing exactly 1 cat + 1 mouse."""
    region = [[-1]*N for _ in range(N)]
    # greedy nearest pairing: each cat -> nearest unpaired mouse
    avail = list(mice)
    pairs = []
    for cat in cats:
        best = min(avail, key=lambda m: abs(cat[0]-m[0]) + abs(cat[1]-m[1]))
        avail.remove(best)
        pairs.append((cat, best))
    # claim cat & mouse cells to their own region
    for i, (cat, mou) in enumerate(pairs):
        region[cat[0]][cat[1]] = i + 1
        region[mou[0]][mou[1]] = i + 1
    # connect each pair with a BFS path through unclaimed cells -> makes the region connected
    def bfs_path(start, goal):
        prev = {start: None}
        dq = deque([start])
        while dq:
            r, c = dq.popleft()
            if (r, c) == goal:
                break
            for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nr, nc = r + dr, c + dc
                if 0 <= nr < N and 0 <= nc < N and (nr, nc) not in prev \
                        and (region[nr][nc] == -1 or (nr, nc) == goal):
                    prev[(nr, nc)] = (r, c)
                    dq.append((nr, nc))
        if goal not in prev:
            return [start, goal]
        path = []
        cur = goal
        while cur is not None:
            path.append(cur)
            cur = prev[cur]
        return path
    for i, (cat, mou) in enumerate(pairs):
        for (r, c) in bfs_path(cat, mou):
            if region[r][c] == -1:
                region[r][c] = i + 1
    # grow all regions from their (now connected) claimed cells
    dq = deque()
    for r in range(N):
        for c in range(N):
            if region[r][c] != -1:
                dq.append((r, c, region[r][c]))
    while dq:
        r, c, rid = dq.popleft()
        for dr, dc in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nr, nc = r + dr, c + dc
            if 0 <= nr < N and 0 <= nc < N and region[nr][nc] == -1:
                region[nr][nc] = rid
                dq.append((nr, nc, rid))
    return region

def verify_regions(N, cats, mice, region):
    cat_of = {p: region[p[0]][p[1]] for p in cats}
    mouse_of = {p: region[p[0]][p[1]] for p in mice}
    problems = []
    for rid in range(1, N + 1):
        cells = [(r, c) for r in range(N) for c in range(N) if region[r][c] == rid]
        nc = sum(1 for p in cats if cat_of[p] == rid)
        nm = sum(1 for p in mice if mouse_of[p] == rid)
        # connectivity via BFS within region
        seen = set()
        if cells:
            dq = deque([cells[0]]); seen.add(cells[0])
            while dq:
                r, c = dq.popleft()
                for dr, dc in ((1,0),(-1,0),(0,1),(0,-1)):
                    nr, nc2 = r+dr, c+dc
                    if 0 <= nr < N and 0 <= nc2 < N and region[nr][nc2] == rid and (nr, nc2) not in seen:
                        seen.add((nr, nc2)); dq.append((nr, nc2))
        connected = len(seen) == len(cells)
        if nc != 1 or nm != 1 or not connected:
            problems.append(f"region{rid}: cats={nc} mice={nm} connected={connected}")
    return problems

def assign_palette(N, region, palette_size=12):
    """Assign each region a distinct palette color, neighbors get well-separated hues."""
    adjacency = {i: set() for i in range(1, N + 1)}
    for r in range(N):
        for c in range(N):
            rid = region[r][c]
            for dr, dc in ((1, 0), (0, 1)):
                nr, nc = r + dr, c + dc
                if 0 <= nr < N and 0 <= nc < N and region[nr][nc] != rid:
                    adjacency[rid].add(region[nr][nc])
                    adjacency[region[nr][nc]].add(rid)
    def hue(idx):
        return (idx * 0.618033988749895) % 1.0
    def hdist(a, b):
        d = abs(a - b) % 1.0
        return min(d, 1 - d)
    order = sorted(range(1, N + 1), key=lambda x: -len(adjacency[x]))
    assign = {}
    used = set()
    for rid in order:
        best, bestd = 1, -1.0
        for idx in range(1, palette_size + 1):
            if idx in used:
                continue
            d = min([hdist(hue(idx), hue(assign[n])) for n in adjacency[rid] if n in assign], default=1.0)
            if d > bestd:
                bestd, best = d, idx
        assign[rid] = best
        used.add(best)
    return assign

def try_font(size):
    for path in [r"C:\Windows\Fonts\msyh.ttc", r"C:\Windows\Fonts\msyh.ttf", r"C:\Windows\Fonts\arial.ttf"]:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except Exception:
                continue
    return ImageFont.load_default()

def render(N, cats, mice, title, out_name, x_ratio=0.5, reveal_cats=1):
    region = build_regions(N, cats, mice)
    probs = verify_regions(N, cats, mice, region)
    print(f"  [{out_name}] region check: {'OK' if not probs else probs}")
    cell = 104
    gap = 8
    pad = 56
    top = 128
    W = pad*2 + N*cell + (N-1)*gap
    H = top + pad + N*cell + (N-1)*gap
    bg = Image.new("RGB", (W, H), (235, 238, 244))
    dr = ImageDraw.Draw(bg)
    f_title = try_font(48)
    dr.text((W/2, 46), title, font=f_title, fill=(50, 50, 60), anchor="mm")
    animals = {}
    for i, (r, c) in enumerate(cats):
        animals[(r, c)] = "cat" if i >= reveal_cats else "cat"
    for (r, c) in mice:
        animals[(r, c)] = "mouse"
    animal_set = set(animals.keys())
    palette_assign = assign_palette(N, region)
    for r in range(N):
        for c in range(N):
            x0 = pad + c*(cell+gap); y0 = top + r*(cell+gap)
            dr.rounded_rectangle([x0, y0, x0+cell, y0+cell], radius=16, fill=hsv_color(palette_assign[region[r][c]]))
    empty = [(r, c) for r in range(N) for c in range(N) if (r, c) not in animal_set]
    elim = [(r, c) for (r, c) in empty if any(adj8((r, c), a) for a in animal_set)]
    random.shuffle(elim)
    for (r, c) in elim[:int(len(elim)*x_ratio)]:
        x0 = pad + c*(cell+gap); y0 = top + r*(cell+gap)
        cs = cross.resize((int(cell*0.46), int(cell*0.46)), Image.LANCZOS)
        bg.paste(cs, (x0 + (cell-cs.width)//2, y0 + (cell-cs.height)//2), cs)
    def paste_icon(sprite, r, c, scale=0.95):
        x0 = pad + c*(cell+gap); y0 = top + r*(cell+gap)
        ratio = sprite.width / sprite.height
        hgt = int(cell*scale); wid = int(hgt*ratio)
        if wid > int(cell*scale*1.12):
            wid = int(cell*scale*1.12); hgt = int(wid/ratio)
        s = sprite.resize((wid, hgt), Image.LANCZOS)
        bg.paste(s, (x0 + (cell-wid)//2, y0 + (cell-hgt)//2), s)
    for (r, c), kind in animals.items():
        paste_icon(cat if kind == "cat" else mouse, r, c)
    out = os.path.join(OUT, out_name)
    bg.save(out)
    print("wrote", out)

for N, seed, name in [(8, 11, "01_newrule_N8.png"), (9, 23, "02_newrule_N9.png"), (10, 37, "03_newrule_N10.png")]:
    random.seed(seed)
    cats, mice = solve_strict(N)
    if cats is None:
        print(f"N={N} infeasible")
        continue
    render(N, cats, mice, f"猫鼠同笼·对称规则（N={N}）", name, x_ratio=0.5, reveal_cats=1)

print("done ->", OUT)
