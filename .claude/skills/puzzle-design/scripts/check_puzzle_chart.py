#!/usr/bin/env python3
"""Check a level's puzzle dependency chart (see references/02-dependency-chart.md).

Usage:
    python3 check_puzzle_chart.py chart.json            # report
    python3 check_puzzle_chart.py chart.json --mermaid  # report + Mermaid flowchart

Exit code 1 if any ERROR was found, 0 otherwise. Standard library only.
"""
import argparse
import json
import sys

TYPES = {"start", "area", "clue", "item", "puzzle", "door", "goal"}
SHAPES = {
    "start": ("([", "])"),
    "goal": ("([", "])"),
    "area": ("[/", "/]"),
    "clue": ("{{", "}}"),
    "item": ("[(", ")]"),
    "puzzle": ("[", "]"),
    "door": ("[[", "]]"),
}


class Report:
    def __init__(self):
        self.lines = []
        self.errors = 0

    def error(self, msg):
        self.errors += 1
        self.lines.append("ERROR " + msg)

    def warn(self, msg):
        self.lines.append("WARN  " + msg)

    def info(self, msg):
        self.lines.append("INFO  " + msg)


def requirement_groups(node):
    """Return the node's requirements as a list of AND-groups (any one group suffices)."""
    base = list(node.get("requires", []))
    if node.get("in_area"):
        base.append(node["in_area"])
    any_groups = node.get("requires_any")
    if not any_groups:
        return [base]
    return [base + list(group) for group in any_groups]


def all_refs(node):
    refs = set(node.get("requires", []))
    for group in node.get("requires_any", []) or []:
        refs.update(group)
    if node.get("in_area"):
        refs.add(node["in_area"])
    return refs


def simulate(nodes, start_id, blocked=frozenset()):
    """Complete everything reachable in waves. Returns (done set, list of waves)."""
    done = {start_id}
    waves = []
    while True:
        wave = [
            nid
            for nid, node in nodes.items()
            if nid not in done
            and nid not in blocked
            and any(all(r in done for r in group) for group in requirement_groups(node))
        ]
        if not wave:
            return done, waves
        waves.append(wave)
        done.update(wave)


def ancestors(nodes, nid, memo):
    if nid in memo:
        return memo[nid]
    memo[nid] = set()
    result = set()
    for ref in all_refs(nodes[nid]):
        if ref in nodes:
            result.add(ref)
            result |= ancestors(nodes, ref, memo)
    memo[nid] = result
    return result


def find_cycle(nodes):
    state = {}
    stack = []

    def visit(nid):
        state[nid] = 1
        stack.append(nid)
        for ref in nodes[nid].get("requires", []) + ([nodes[nid]["in_area"]] if nodes[nid].get("in_area") else []):
            if ref not in nodes:
                continue
            if state.get(ref) == 1:
                return stack[stack.index(ref):] + [ref]
            if ref not in state:
                found = visit(ref)
                if found:
                    return found
        stack.pop()
        state[nid] = 2
        return None

    for nid in nodes:
        if nid not in state:
            found = visit(nid)
            if found:
                return found
    return None


def check(chart):
    rep = Report()
    raw = chart.get("nodes", [])
    nodes = {}
    for node in raw:
        nid = node.get("id")
        if not nid:
            rep.error("a node has no id: %r" % node)
            continue
        if nid in nodes:
            rep.error("duplicate id '%s'" % nid)
        if node.get("type") not in TYPES:
            rep.error("'%s' has unknown type '%s' (use %s)" % (nid, node.get("type"), ", ".join(sorted(TYPES))))
        nodes[nid] = node

    starts = [n for n, v in nodes.items() if v.get("type") == "start"]
    goals = [n for n, v in nodes.items() if v.get("type") == "goal"]
    if len(starts) != 1:
        rep.error("need exactly one start node, found %d" % len(starts))
    if len(goals) != 1:
        rep.error("need exactly one goal node, found %d" % len(goals))

    for nid, node in nodes.items():
        for ref in all_refs(node) | set(node.get("consumes", [])) | set(node.get("opens", [])):
            if ref not in nodes:
                rep.error("'%s' refers to missing node '%s'" % (nid, ref))
    if rep.errors:
        return rep, nodes

    cycle = find_cycle(nodes)
    if cycle:
        rep.error("cycle: " + " -> ".join(cycle))
        return rep, nodes

    start, goal = starts[0], goals[0]
    done, waves = simulate(nodes, start)

    if goal not in done:
        rep.error("goal '%s' cannot be reached - the level cannot be finished" % goal)
    for nid in nodes:
        if nid not in done and nid != goal:
            rep.error("'%s' (%s) can never be reached" % (nid, nodes[nid].get("type")))

    # A clue must be reachable without solving the puzzle it opens.
    for nid, node in nodes.items():
        for target in node.get("opens", []):
            reach, _ = simulate(nodes, start, blocked=frozenset([target]))
            if nid not in reach:
                rep.error(
                    "clue '%s' opens '%s' but can only be found after '%s' is solved - the player gets stuck"
                    % (nid, target, target)
                )

    # Consumed items must be the last use.
    memo = {}
    consumers = {}
    for nid, node in nodes.items():
        for item in node.get("consumes", []):
            consumers.setdefault(item, []).append(nid)
            if item not in all_refs(node):
                rep.warn("'%s' consumes '%s' but does not require it" % (nid, item))
    for item, users in consumers.items():
        if len(users) > 1:
            rep.error("'%s' is consumed by more than one node: %s" % (item, ", ".join(users)))
        consumer = users[0]
        before = ancestors(nodes, consumer, memo)
        for nid, node in nodes.items():
            if nid == consumer or item not in all_refs(node):
                continue
            if nid not in before:
                rep.error(
                    "'%s' is used up by '%s', but '%s' also needs it and can come after - "
                    "CLAUDE.md s8: a used-up item must be the last thing a puzzle needs" % (item, consumer, nid)
                )

    # Scale: about five puzzles, one large.
    puzzles = [n for n, v in nodes.items() if v.get("type") == "puzzle"]
    large = [n for n in puzzles if nodes[n].get("size") == "large"]
    unsized = [n for n in puzzles if nodes[n].get("size") not in ("large", "small")]
    if unsized:
        rep.warn("puzzles without size large/small: %s" % ", ".join(unsized))
    if len(large) != 1:
        rep.warn("%d large puzzles (CLAUDE.md s8: one large and about four small per level)" % len(large))
    if not 3 <= len(puzzles) <= 7:
        rep.warn("%d puzzles in the level (spec: about five)" % len(puzzles))

    # Dead ends: things nobody needs.
    needed = set()
    for node in nodes.values():
        needed |= all_refs(node)
        needed |= set(node.get("opens", []))
    for nid, node in nodes.items():
        if node.get("type") in ("item", "clue") and nid not in needed and not node.get("opens"):
            rep.warn("%s '%s' is never used - a red herring on purpose?" % (node["type"], nid))

    # Width per wave and bottlenecks.
    actionable = ("puzzle", "item", "clue")
    widths = []
    for wave in waves:
        widths.append(sum(1 for n in wave if nodes[n].get("type") in actionable))
    run = 0
    longest = 0
    for w in widths:
        run = run + 1 if w == 1 else 0
        longest = max(longest, run)
    if longest >= 3:
        rep.warn("%d waves in a row with only one thing to do - a player stuck there has nothing else to try" % longest)
    rep.info("things to do per wave: %s" % widths)
    rep.info("%d nodes, %d puzzles (%d large), %d waves to the goal" % (len(nodes), len(puzzles), len(large), len(waves)))
    return rep, nodes


def mermaid(nodes):
    out = ["flowchart TD"]
    for nid, node in nodes.items():
        left, right = SHAPES.get(node.get("type"), ("[", "]"))
        label = node.get("label", nid).replace('"', "'")
        tag = node.get("type", "")
        if tag == "puzzle" and node.get("size"):
            tag += " " + node["size"]
        out.append('    %s%s"%s<br/><small>%s</small>"%s' % (nid, left, label, tag, right))
    for nid, node in nodes.items():
        for ref in node.get("requires", []):
            arrow = "-. used up .->" if ref in node.get("consumes", []) else "-->"
            out.append("    %s %s %s" % (ref, arrow, nid))
        if node.get("in_area") and node["in_area"] not in node.get("requires", []):
            out.append("    %s --> %s" % (node["in_area"], nid))
        for i, group in enumerate(node.get("requires_any", []) or []):
            for ref in group:
                out.append("    %s -- or %d --> %s" % (ref, i + 1, nid))
        for target in node.get("opens", []):
            out.append("    %s -. clue for .-> %s" % (nid, target))
    return "\n".join(out)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("chart")
    parser.add_argument("--mermaid", action="store_true", help="also print a Mermaid flowchart")
    args = parser.parse_args()
    with open(args.chart, encoding="utf-8") as fh:
        chart = json.load(fh)
    rep, nodes = check(chart)
    print("Chart: %s" % chart.get("level", args.chart))
    for line in rep.lines:
        print(line)
    print("Result: %s" % ("FAIL (%d errors)" % rep.errors if rep.errors else "OK"))
    if args.mermaid and nodes:
        print()
        print("```mermaid")
        print(mermaid(nodes))
        print("```")
    sys.exit(1 if rep.errors else 0)


if __name__ == "__main__":
    main()
