# -*- coding: utf-8 -*-
import os, json, random, importlib.util

BASE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("g", os.path.join(BASE, "_gen_levels200.py"))
g = importlib.util.module_from_spec(spec)
spec.loader.exec_module(g)

ROOT = os.path.dirname(BASE)
LVL = os.path.join(ROOT, "Catdoku", "Assets", "Resources", "Levels")

random.seed(20261008)
N = 8
cats, mice = g.solve_strict(N)
region = g.build_regions(N, cats, mice)
g.verify(N, cats, mice, region)

# pick one cat and one mouse to leave UNREVEALED as tutorial targets
target_cat = cats[0]
target_mouse = mice[0]

solution = [["." for _ in range(N)] for _ in range(N)]
for r, c in cats: solution[r][c] = "Q"
for r, c in mice: solution[r][c] = "M"

catRevealed = [[True]*N for _ in range(N)]
# everything revealed except the two targets
for r in range(N):
    for c in range(N):
        catRevealed[r][c] = solution[r][c] in ("Q", "M")
catRevealed[target_cat[0]][target_cat[1]] = False
catRevealed[target_mouse[0]][target_mouse[1]] = False

level = {"solution": solution, "colorMap": region, "catRevealed": catRevealed, "hintPlan": []}
with open(os.path.join(LVL, "Tutorial.json"), "w", encoding="utf-8") as f:
    json.dump(level, f, ensure_ascii=False, separators=(",", ":"))

print("Tutorial.json written, N=8")
print(f"target_cat (col,row) = ({target_cat[1]},{target_cat[0]})  color={region[target_cat[0]][target_cat[1]]}")
print(f"target_mouse (col,row) = ({target_mouse[1]},{target_mouse[0]})  color={region[target_mouse[0]][target_mouse[1]]}")
print("cats:", [(c[1], c[0]) for c in cats])
print("mice:", [(c[1], c[0]) for c in mice])
