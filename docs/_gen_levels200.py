# -*- coding: utf-8 -*-
import os, json, random
from collections import deque

BASE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(BASE)
LVL = os.path.join(ROOT, "Catdoku", "Assets", "Resources", "Levels")
os.makedirs(LVL, exist_ok=True)

def adj8(a, b):
    return abs(a[0]-b[0]) <= 1 and abs(a[1]-b[1]) <= 1 and a != b

def solve_strict(N):
    cat_used = [False]*N; mouse_used = [False]*N
    cats = [None]*N; mice = [None]*N
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
            if cat_used[cc]: continue
            mcands = list(range(N)); random.shuffle(mcands)
            for mc in mcands:
                if mouse_used[mc] or not row_ok(r, cc, mc): continue
                cat_used[cc] = mouse_used[mc] = True
                cats[r] = cc; mice[r] = mc
                if bt(r+1): return True
                cat_used[cc] = mouse_used[mc] = False
        return False
    if bt(0):
        return [(r, cats[r]) for r in range(N)], [(r, mice[r]) for r in range(N)]
    return None, None

def build_regions(N, cats, mice):
    region = [[-1]*N for _ in range(N)]
    avail = list(mice); pairs = []
    for cat in cats:
        best = min(avail, key=lambda m: abs(cat[0]-m[0]) + abs(cat[1]-m[1]))
        avail.remove(best); pairs.append((cat, best))
    for i, (cat, mou) in enumerate(pairs):
        region[cat[0]][cat[1]] = i+1
        region[mou[0]][mou[1]] = i+1
    def bfs_path(start, goal):
        prev = {start: None}; dq = deque([start])
        while dq:
            r, c = dq.popleft()
            if (r, c) == goal: break
            for dr, dc in ((1,0),(-1,0),(0,1),(0,-1)):
                nr, nc = r+dr, c+dc
                if 0 <= nr < N and 0 <= nc < N and (nr,nc) not in prev and (region[nr][nc] == -1 or (nr,nc) == goal):
                    prev[(nr,nc)] = (r,c); dq.append((nr,nc))
        if goal not in prev: return [start, goal]
        path = []; cur = goal
        while cur is not None:
            path.append(cur); cur = prev[cur]
        return path
    for i, (cat, mou) in enumerate(pairs):
        for (r, c) in bfs_path(cat, mou):
            if region[r][c] == -1: region[r][c] = i+1
    dq = deque()
    for r in range(N):
        for c in range(N):
            if region[r][c] != -1: dq.append((r, c, region[r][c]))
    while dq:
        r, c, rid = dq.popleft()
        for dr, dc in ((1,0),(-1,0),(0,1),(0,-1)):
            nr, nc = r+dr, c+dc
            if 0 <= nr < N and 0 <= nc < N and region[nr][nc] == -1:
                region[nr][nc] = rid; dq.append((nr,nc,rid))
    return region

def verify(N, cats, mice, region):
    for i in range(len(cats)):
        for j in range(i+1, len(cats)):
            assert not adj8(cats[i], cats[j])
            assert not adj8(mice[i], mice[j])
    for c in cats:
        for m in mice:
            assert not adj8(c, m)
    colors = set()
    for r in range(N):
        for c in range(N):
            assert region[r][c] > 0
            colors.add(region[r][c])
    assert len(colors) == N
    for rid in range(1, N+1):
        assert sum(1 for p in cats if region[p[0]][p[1]] == rid) == 1
        assert sum(1 for p in mice if region[p[0]][p[1]] == rid) == 1

def board_size(level_index):
    # N=8 has a small solution space (~32), so keep it to the intro levels only.
    if level_index <= 25: return 8
    if level_index <= 85: return 9
    if level_index <= 145: return 10
    return 11

def reveal_count(level_index, N):
    # more reveals early (easier), fewer later (harder)
    frac = 0.42 - (level_index / 200.0) * 0.28  # 0.42 -> 0.14
    return max(1, round(N * frac / 2))

def gen_level(N, reveal):
    cats, mice = solve_strict(N)
    region = build_regions(N, cats, mice)
    verify(N, cats, mice, region)
    solution = [["." for _ in range(N)] for _ in range(N)]
    for r, c in cats: solution[r][c] = "Q"
    for r, c in mice: solution[r][c] = "M"
    catRevealed = [[False]*N for _ in range(N)]
    for i in range(min(reveal, N)):
        r, c = cats[i]; catRevealed[r][c] = True
    for i in range(min(reveal, N)):
        r, c = mice[i]; catRevealed[r][c] = True
    key = json.dumps(solution)
    return {"solution": solution, "colorMap": region, "catRevealed": catRevealed, "hintPlan": []}, key

def main():
    seen = set()
    made = 0
    idx = 1
    attempts = 0
    while made < 200 and attempts < 60000:
        attempts += 1
        N = board_size(idx)
        reveal = reveal_count(idx, N)
        random.seed(attempts * 7919 + idx * 131)
        lv, key = gen_level(N, reveal)
        if key in seen:
            continue
        seen.add(key)
        path = os.path.join(LVL, f"level_{idx}.json")
        with open(path, "w", encoding="utf-8") as f:
            json.dump(lv, f, ensure_ascii=False, separators=(",", ":"))
        made += 1
        idx += 1
    print(f"generated {made} unique levels (attempts={attempts})")

if __name__ == "__main__":
    main()
