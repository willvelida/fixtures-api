#!/usr/bin/env python3
import json, re, pathlib

DATA = pathlib.Path("data")
OUT = pathlib.Path("seed"); OUT.mkdir(exist_ok=True)
MONTHS = {m: i for i, m in enumerate(
    ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"], 1)}

def norm(name):
    name = name.strip()
    if name.startswith("AFC "): name = name[4:]
    for suf in (" FC", " AFC"):
        if name.endswith(suf): name = name[:-len(suf)]
    return name.strip()

DATE_RE = re.compile(r"^(Mon|Tue|Wed|Thu|Fri|Sat|Sun)\s+([A-Z][a-z]{2})\s+(\d{1,2})(?:\s+(\d{4}))?\s*$")
MD_RE   = re.compile(r"^\s*▪.*?(\d+)\s*$")
RESULT_RE = re.compile(r"^\s*\d{1,2}:\d{2}\s+(.+?)\s+(\d+)-(\d+)\s+\(\d+-\d+\)\s+(.+?)\s*$")
FIXTURE_RE = re.compile(r"^\s*(?:\d{1,2}:\d{2}\s+)?(.+?)\s+v\s+(.+?)\s*$")

def parse(path, season, played):
    matches, year, md, cur_date = [], None, 0, None
    for line in path.read_text(encoding="utf-8").splitlines():
        if line.lstrip().startswith("#"): continue
        m = MD_RE.match(line)
        if m: md = int(m.group(1)); continue
        d = DATE_RE.match(line)
        if d:
            if d.group(4): year = int(d.group(4))
            cur_date = f"{year:04d}-{MONTHS[d.group(2)]:02d}-{int(d.group(3)):02d}"
            continue
        if played:
            r = RESULT_RE.match(line)
            if r:
                away = re.sub(r"\s*\(.*$", "", r.group(4))
                matches.append(dict(season=season, matchday=md, date=cur_date,
                    home=norm(r.group(1)), away=norm(away),
                    homeScore=int(r.group(2)), awayScore=int(r.group(3)), played=True))
        else:
            f = FIXTURE_RE.match(line)
            if f and " v " not in f.group(1):  # guard against odd lines
                matches.append(dict(season=season, matchday=md, date=cur_date,
                    home=norm(f.group(1)), away=norm(f.group(2)),
                    homeScore=None, awayScore=None, played=False))
    return matches

results  = parse(DATA / "results-2025-26.txt", "2025-26", True)
fixtures = parse(DATA / "fixtures-2026-27.txt", "2026-27", False)

# 2025-26 final table (from Wikipedia)
rows = """
1 Arsenal 38 26 7 5 71 27 44 85
2 Manchester City 38 23 9 6 77 35 42 78
3 Manchester United 38 20 11 7 69 50 19 71
4 Aston Villa 38 19 8 11 56 49 7 65
5 Liverpool 38 17 9 12 63 53 10 60
6 Bournemouth 38 13 18 7 58 54 4 57
7 Sunderland 38 14 12 12 42 48 -6 54
8 Brighton & Hove Albion 38 14 11 13 52 46 6 53
9 Brentford 38 14 11 13 55 52 3 53
10 Chelsea 38 14 10 14 58 52 6 52
11 Fulham 38 15 7 16 47 51 -4 52
12 Newcastle United 38 14 7 17 53 55 -2 49
13 Everton 38 13 10 15 47 50 -3 49
14 Leeds United 38 11 14 13 49 56 -7 47
15 Crystal Palace 38 11 12 15 41 51 -10 45
16 Nottingham Forest 38 11 11 16 48 51 -3 44
17 Tottenham Hotspur 38 10 11 17 48 57 -9 41
18 West Ham United 38 10 9 19 46 65 -19 39
19 Burnley 38 4 10 24 38 75 -37 22
20 Wolverhampton Wanderers 38 3 11 24 27 68 -41 20
""".strip().splitlines()

standings = []
for row in rows:
    p = row.rsplit(maxsplit=8)  # split off the 8 trailing numbers; rest is "pos team"
    pos_team, nums = p[0], p[1:]
    pos, team = pos_team.split(maxsplit=1)
    pld, w, d, l, gf, ga, gd, pts = map(int, nums)
    standings.append(dict(position=int(pos), team=team, played=pld, won=w, drawn=d,
        lost=l, gf=gf, ga=ga, gd=gd, points=pts))

(OUT / "fixtures.json").write_text(json.dumps(
    dict(matches=results + fixtures, standings=standings), indent=2))
print(f"results={len(results)} fixtures={len(fixtures)} standings={len(standings)}")