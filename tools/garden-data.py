# Builds the embedded garden data of Tensho Assistant (DEC-027, docs/evidence/0.448-n06-n14-sources.md):
#   src/TenshoAssistant/Reminders/Data/garden-seeds.csv   from Lotlab/FFXIV-Gardening-Tracker (GPL-3.0): data/seeds.json + data/seeds_time.json
#   src/TenshoAssistant/Reminders/Data/garden-crosses.csv from nick75g/FFXIV-Crossbreed-Helper (MIT): _internal/importantfiles/crossbreeding.json
# Names are kept as the source writes them (English); Tensho links them to item ids with the game's English Item sheet at run time.
# Usage: python -I tools/garden-data.py <seeds.json> <seeds_time.json> <lotlab commit> <crossbreeding.json> <nick75g commit> <output folder>
import json, sys, os

seeds_file, times_file, lotlab_commit, cross_file, nick_commit, output = sys.argv[1:7]
seeds = json.load(open(seeds_file, encoding="utf-8"))
times = {row["Index"]: row for row in json.load(open(times_file, encoding="utf-8"))}
lines = [
    "# Garden seeds: seed item id, harvest item id, growing hours, hours before wilting without tending (0 = none), embedded by Tensho Assistant (0.449).",
    f"# Source: Lotlab/FFXIV-Gardening-Tracker, GardeningTracker/data/seeds.json and seeds_time.json, commit {lotlab_commit}",
    "#   https://github.com/Lotlab/FFXIV-Gardening-Tracker",
    "# License: GNU General Public License v3.0 (the source's LICENSE.txt), the same license as Tensho Assistant (LICENSE).",
    "# Row 107 of seeds_time.json writes GrowthTime for GrowTime; it is read as GrowTime.",
    "seed,harvest,grow,wilt",
]
for seed in sorted(seeds, key=lambda row: row["Index"]):
    time = times[seed["Index"]]
    grow = time.get("GrowTime", time.get("GrowthTime"))
    assert isinstance(grow, int) and grow > 0, seed
    lines.append(f"{seed['Seed']['Id']},{seed['Item']['Id']},{grow},{int(time['WiltTime'])}")
# 0.540: a game row the source does not have yet (docs/evidence/0.540-cornflower-seed.md).
if not any(seed["Seed"]["Id"] == 51189 for seed in seeds):
    lines.append("# Not in the source: GardeningSeed row 108 (0.540, docs/evidence/0.540-cornflower-seed.md). Ids from the game; 24 h, no wilting: what the source gives for all 26 other pot flowers.")
    lines.append("51189,51190,24,0")
open(os.path.join(output, "garden-seeds.csv"), "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")

crosses = json.load(open(cross_file, encoding="utf-8"))
mit = """MIT License

Copyright (c) 2024 Nick75

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE."""
lines = [
    "# Garden crossbreeds: resulting seed, first parent, second parent (English item names, tab-separated), embedded by Tensho Assistant (0.450).",
    f"# Source: nick75g/FFXIV-Crossbreed-Helper, _internal/importantfiles/crossbreeding.json, commit {nick_commit}",
    "#   https://github.com/nick75g/FFXIV-Crossbreed-Helper",
] + ["# " + line if line else "#" for line in mit.split("\n")] + ["target\tfirst\tsecond"]
count = 0
for target in sorted(crosses):
    for first, second in crosses[target]:
        assert "\t" not in target + first + second
        lines.append(f"{target}\t{first}\t{second}")
        count += 1
open(os.path.join(output, "garden-crosses.csv"), "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
print(len(seeds), "seeds,", len(crosses), "targets,", count, "pairs")
