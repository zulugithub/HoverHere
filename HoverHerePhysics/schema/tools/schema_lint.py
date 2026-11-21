"""Schema lint: structural and physical consistency checks of schema/*.yaml (plus the sync check against the C# classes)."""
from __future__ import annotations

import json
import math
from collections import Counter, defaultdict
from fnmatch import fnmatchcase
from pathlib import Path

from schema_model import (CPP_KEYWORDS, CS_KEYWORDS, IDENTIFIER, LIST_KIND, Flat, Group, Issue, Leaf, Model, Unit)

NUMERIC_TYPES = ("real", "int", "vec3", "bank3", "mat3", LIST_KIND)
UNITLESS_TYPES = ("bool", "enum")


def _is_number(x) -> bool:
    return isinstance(x, (int, float)) and not isinstance(x, bool) and math.isfinite(x)


def _components(leaf: Leaf):
    """Numeric components of the default value, or None when the default is malformed."""
    d = leaf.default
    if leaf.type in ("real", "int", "enum"):
        return [d] if _is_number(d) else None
    if leaf.type == "bool":
        return [1.0 if d else 0.0] if isinstance(d, bool) else None
    n = 3 if leaf.type in ("vec3", "bank3") else 9
    if leaf.type == "mat3" and d == "identity":
        return [1, 0, 0, 0, 1, 0, 0, 0, 1]
    if leaf.type in ("vec3", "bank3", "mat3"):
        return list(d) if isinstance(d, list) and len(d) == n and all(_is_number(x) for x in d) else None
    if isinstance(d, list) and all(isinstance(e, list) and len(e) == 3 and all(_is_number(x) for x in e) for e in d):
        return [x for e in d for x in e]
    return None


def matches(pattern: str, path: str) -> bool:
    return path == pattern or path.startswith(pattern + "/") or fnmatchcase(path, pattern)


def lint_model(model: Model, units: dict[str, Unit]) -> list[Issue]:
    out: list[Issue] = list(model.issues)

    def err(code, where, msg):
        out.append(Issue("error", code, where, msg))

    def warn(code, where, msg):
        out.append(Issue("warning", code, where, msg))

    # names ---------------------------------------------------------------------------------------------------
    def check_name(name, where):
        if not IDENTIFIER.match(name):
            err("L001", where, f"'{name}' is not a valid identifier")
        elif name in CPP_KEYWORDS or name in CS_KEYWORDS:
            err("L001", where, f"'{name}' is a C++/C# keyword")

    def walk_groups(g: Group):
        names = Counter(c.name for c in g.children)
        for n, count in names.items():
            if count > 1:
                err("L002", g.where, f"duplicate member '{n}'")
        for c in g.children:
            check_name(c.name, c.where)
            if isinstance(c, Group):
                if c.array_count is not None and (not isinstance(c.array_count, int) or c.array_count < 1 or not c.item_xml):
                    err("L003", c.where, "_array needs count >= 1 and item_xml")
                walk_groups(c)

    for root in model.roots:
        check_name(root.struct, root.where)
        walk_groups(root)
    for doc, info in model.documents.items():
        if info.get("encoding") not in ("parameter", "plain") or not info.get("root_element"):
            err("L061", doc, "document needs root_element and encoding (parameter|plain)")
    for struct in model.structs:
        if struct in CPP_KEYWORDS or struct in CS_KEYWORDS:
            err("L001", struct, "struct name is a C++/C# keyword")

    # leaves --------------------------------------------------------------------------------------------------
    for f in model.flat:
        leaf, where = f.leaf, f.path
        # unit
        if leaf.type in NUMERIC_TYPES:
            if not leaf.unit:
                err("L020", where, "numeric parameter without unit (use \"-\" for dimensionless values)")
            elif leaf.unit not in units:
                err("L021", where, f"unit '{leaf.unit}' is not in schema/units.yaml")
        elif leaf.unit:
            warn("L022", where, f"{leaf.type} parameter must not have a unit ('{leaf.unit}')")
        if not leaf.doc.strip():
            warn("L070", where, "doc text is empty")
        if "\f" in leaf.doc or any(ord(ch) < 32 for ch in leaf.doc):
            err("L071", where, "doc text contains a control character (an unescaped backslash in the C# string?)")
        if not leaf.doc.isascii():
            err("L072", where, "doc text must be ASCII (it is copied into generated C++ and C# sources)")
        if f.document not in model.documents:
            err("L060", where, f"document '{f.document}' is not listed in the documents table")

        # default
        comps = _components(leaf)
        if comps is None:
            err("L004", where, f"default {leaf.default!r} does not match type {leaf.type}")
            continue

        # bounds
        if leaf.type in ("real", "int"):
            lo, hi = leaf.min, leaf.max
            if leaf.calculated:
                pass
            elif not (_is_number(lo) and _is_number(hi)):
                err("L012", where, "min and max are required for loadable real/int parameters")
            else:
                if lo > hi:
                    err("L010", where, f"min {lo} > max {hi} (swapped bounds)")
                elif lo == hi:
                    err("L013", where, f"min == max == {lo}: the parameter would be pinned to one value (unspecified bounds?)")
                elif not (lo <= comps[0] <= hi):
                    err("L011", where, f"default {comps[0]} is outside [{lo}, {hi}]")
                if leaf.type == "int" and not (float(lo).is_integer() and float(hi).is_integer()):
                    err("L014", where, "integer parameter with non-integer bounds")
        elif leaf.type in ("vec3", "bank3", "mat3"):
            for key, value in (("min", leaf.min), ("max", leaf.max)):
                if value is not None and not _is_number(value):
                    err("L015", where, f"{key} must be a number applying to every component")
            if _is_number(leaf.min) and _is_number(leaf.max):
                if leaf.min > leaf.max:
                    err("L010", where, f"min {leaf.min} > max {leaf.max} (swapped bounds)")
                elif not leaf.calculated and any(not (leaf.min <= c <= leaf.max) for c in comps):
                    err("L011", where, f"default {comps} is outside [{leaf.min}, {leaf.max}]")
        elif leaf.type == LIST_KIND:
            if leaf.max_count < 1:
                err("L032", where, "vec3_list needs max_count >= 1")
            elif len(comps) // 3 > leaf.max_count:
                err("L032", where, f"default has {len(comps) // 3} entries, max_count is {leaf.max_count}")
        elif leaf.type == "enum":
            if not leaf.options:
                err("L030", where, "enum without options")
            elif len(set(leaf.options)) != len(leaf.options):
                err("L030", where, "duplicate enum options")
            elif not (0 <= comps[0] < len(leaf.options)) or comps[0] != int(comps[0]):
                err("L030", where, f"default {leaf.default} is not an option index (0..{len(leaf.options) - 1})")
        if leaf.type != "enum" and leaf.options:
            warn("L031", where, "options are only used by enum parameters")
        if leaf.type != LIST_KIND and leaf.max_count:
            warn("L031", where, "max_count is only used by vec3_list parameters")
        if leaf.calculated and not leaf.formula:
            warn("L040", where, "calculated parameter without formula text")

    # xml mapping ---------------------------------------------------------------------------------------------
    by_xml = defaultdict(list)
    for f in model.flat:
        by_xml[(f.document, f.xml_path)].append(f)
    for (doc, xml_path), items in by_xml.items():
        if len(items) > 1:
            err("L050", ", ".join(i.path for i in items), f"{len(items)} parameters map to the XML element '{xml_path}' of document '{doc}'")
    for f in model.flat:
        for pattern in model.unity_only.get(f.document, []) or []:
            if not f.leaf.calculated and matches(pattern, f.xml_path):
                err("L051", f.path, f"is loaded but also matches the unity_only pattern '{pattern}'")
    prefixes = defaultdict(set)
    for f in model.flat:
        parts = f.xml_path.split("/")
        for i in range(1, len(parts)):
            prefixes[f.document].add("/".join(parts[:i]))
    for f in model.flat:
        if f.xml_path in prefixes[f.document]:
            err("L052", f.path, f"XML element '{f.xml_path}' is a leaf and also the parent of other parameters")
    return out


# ------------------------------------------------------------------------------------------------------------------
# sync with Helicopter_Parameter.cs (seed written by tools/csharp_reference schema-seed)
# ------------------------------------------------------------------------------------------------------------------
SEED_KIND = {"float": "real", "int": "int", "bool": "bool", "list": "enum", "vec3": ("vec3", "bank3"), "vec3_list": "vec3_list"}
SEED_DOCUMENT = {"transmitter_and_helicopter": "heli", "scenery": "scenery", "simulation": "simulation"}


def _seed_leaves(node, path=()):
    if node["kind"] == "group":
        for c in node["children"]:
            yield from _seed_leaves(c, path + (c["name"],))
    else:
        yield "/".join(path), node


def lint_against_seed(model: Model, units: dict[str, Unit], seed_path: Path, rel_tol: float = 1e-6) -> list[Issue]:
    out: list[Issue] = []
    alias = {}
    for u in units.values():
        alias[u.name] = u.name
        for a in u.aliases:
            alias[a] = u.name
    seed = json.loads(seed_path.read_text(encoding="utf-8"))
    leaves = dict(_seed_leaves(seed["root"]))
    covered = set()

    def same(a, b):
        return math.isclose(float(a), float(b), rel_tol=rel_tol, abs_tol=1e-12)

    for f in model.flat:
        leaf = f.leaf
        if not f.csharp_path or f.document not in ("heli", "scenery", "simulation"):
            continue
        s = leaves.get(f.csharp_path)
        if s is None:
            out.append(Issue("warning", "C900", f.path, f"not found in Helicopter_Parameter.cs ({f.csharp_path})"))
            continue
        covered.add(f.csharp_path)
        expected = SEED_KIND[s["kind"]]
        expected = expected if isinstance(expected, tuple) else (expected,)
        if leaf.type not in expected:
            out.append(Issue("error", "C901", f.path, f"type {leaf.type} but C# has {s['kind']}"))
            continue
        if leaf.calculated != bool(s.get("calculated") or s.get("xml_ignore")):
            out.append(Issue("warning", "C905", f.path, f"calculated={leaf.calculated} but C# has calculated={s.get('calculated')}, XmlIgnore={s.get('xml_ignore')}"))
        c_unit = alias.get(s["unit"], s["unit"]) if s["unit"] not in ("", "not set") else None
        if leaf.type not in ("bool", "enum") and c_unit != leaf.unit:
            out.append(Issue("warning", "C902", f.path, f"unit '{leaf.unit}' differs from C# '{s['unit']}'"))
        if leaf.calculated:
            continue
        sv = s["val"]
        if leaf.type == "real" or leaf.type == "int" or leaf.type == "enum":
            if not same(leaf.default, sv):
                out.append(Issue("warning", "C903", f.path, f"default {leaf.default} differs from C# {sv}"))
        elif leaf.type in ("vec3", "bank3"):
            if not all(same(a, b) for a, b in zip(leaf.default, sv)):
                out.append(Issue("warning", "C903", f.path, f"default {leaf.default} differs from C# {sv}"))
        elif leaf.type == "bool":
            if bool(leaf.default) != bool(sv):
                out.append(Issue("warning", "C903", f.path, f"default {leaf.default} differs from C# {sv}"))
        elif leaf.type == LIST_KIND:
            if len(leaf.default) != len(sv):
                out.append(Issue("warning", "C903", f.path, f"default has {len(leaf.default)} entries, C# {len(sv)}"))
        if leaf.type in ("real", "int"):
            for key in ("min", "max"):
                if getattr(leaf, key) is not None and not same(getattr(leaf, key), s[key]):
                    out.append(Issue("warning", "C904", f.path, f"{key} {getattr(leaf, key)} differs from C# {s[key]}"))
        if leaf.type == "enum" and [str(o) for o in leaf.options] != s["options"]:
            out.append(Issue("warning", "C906", f.path, f"options {leaf.options} differ from C# {s['options']}"))

    # every C# parameter must be classified: in the schema or in unity_only
    for path, s in leaves.items():
        if path in covered:
            continue
        head, _, xml_path = path.partition("/")
        document = SEED_DOCUMENT.get(head)
        if document is None:
            out.append(Issue("error", "C910", path, "C# parameter in an unknown top level group"))
            continue
        patterns = model.unity_only.get(document, []) or []
        if not any(matches(p, xml_path) for p in patterns):
            out.append(Issue("error", "C910", path, "C# parameter is neither in the schema nor listed in unity_only"))
    return out
