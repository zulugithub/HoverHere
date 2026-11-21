#!/usr/bin/env python3
"""Draft generator for schema/parameters.yaml: reads the JSON written by `csharp_reference schema-seed`
(constructor defaults of Helicopter_Parameter.cs) and writes the physics relevant part as YAML.

The YAML file is the single source of truth afterwards and is edited by hand; this script only bootstraps it
(and can produce a draft again when parameters are added to the C# classes).

    python seed_to_yaml.py seed.json ../../schema/parameters.yaml
"""
import json
import re
import struct
import sys
from fnmatch import fnmatchcase
from pathlib import Path

import yaml

HERE = Path(__file__).resolve().parent
UNITS_FILE = HERE.parent.parent / "schema" / "units.yaml"

# top level groups of the schema: C# location, document, XML base path and which leaves are physics parameters
TOP = {
    "simulation": dict(csharp="simulation", document="simulation", xml="",
                       include=["physics/timescale", "various/V_h", "various/U_max", "various/stiction_factor"]),
    "scenery": dict(csharp="scenery", document="scenery", xml="",
                    include=["gravity", "weather/*", "ground/*"]),
    "helicopter": dict(csharp="transmitter_and_helicopter/helicopter", document="heli", xml="helicopter",
                       include=["*"],
                       exclude=["visual_effects/*"],
                       keep=["visual_effects/landing_gear_main_transition_time_gear",
                             "visual_effects/landing_gear_main_transition_time_bay",
                             "visual_effects/landing_gear_main_mechanism_tilted_forward",
                             "visual_effects/landing_gear_main_radius"]),
    "tuning": dict(csharp="transmitter_and_helicopter/tuning", document="heli", xml="tuning", include=["*"]),
}

# C# classes with an identical member list share one schema type
SHARED_TYPES = {
    "Rotor": ["stru_rotor", "stru_mainrotor", "stru_tailrotor", "stru_propeller"],
    "Wing": ["stru_wing", "stru_horizontal_fin", "stru_vertical_fin"],
    "RotorInflowTuning": ["stru_rotor_tuning_base", "stru_mainrotor_tuning", "stru_tailrotor_tuning", "stru_propeller_tuning"],
}

# parameters that hold one value per flight bank (indexed with flight_bank in the C# code)
BANKS = {
    "helicopter/governor/target_rpm",
    "helicopter/flybarless/K_a", "helicopter/flybarless/K_I", "helicopter/flybarless/K_p", "helicopter/flybarless/K_d",
    "helicopter/gyro/K_a", "helicopter/gyro/K_I", "helicopter/gyro/K_p", "helicopter/gyro/K_d",
}

# elements outside the generated groups that are no physics parameters
EXTRA_UNITY_ONLY = {"heli": ["transmitter", "helicopter/name"], "scenery": [], "simulation": []}

LIST_CAPACITY = 8


def num(value):
    """Shortest decimal text that denotes the same float32 as `value`, written so that YAML reads it as a number."""
    packed = struct.pack("f", value)
    text = repr(value)
    for digits in range(1, 10):
        candidate = f"{value:.{digits}g}"
        if struct.pack("f", float(candidate)) == packed:
            text = candidate
            break
    number = float(text)
    if number == int(number) and abs(number) < 1e15:
        return str(int(number))
    return re.sub(r"^(-?\d+)(e[+-]\d+)$", r"\1.0\2", text)


def q(text):
    return json.dumps(text, ensure_ascii=False)


class Seeder:
    def __init__(self, seed):
        self.root = seed["root"]
        units = yaml.safe_load(UNITS_FILE.read_text(encoding="utf-8"))["units"]
        self.alias = {}
        for name, info in units.items():
            self.alias[name] = name
            for a in info.get("aliases", []):
                self.alias[a] = name
        self.unknown_units = set()
        self.type_registry = {}

    # ----------------------------------------------------------------------------------------------------------
    def unit(self, raw):
        if raw in ("", "not set"):
            return None
        if raw in self.alias:
            return self.alias[raw]
        self.unknown_units.add(raw)
        return raw

    @staticmethod
    def child(node, name):
        return next(c for c in node["children"] if c["name"] == name)

    def locate(self, csharp):
        node = self.root
        for part in csharp.split("/"):
            node = self.child(node, part)
        return node

    @staticmethod
    def selected(rel, cfg):
        if any(fnmatchcase(rel, p) for p in cfg.get("keep", [])):
            return True
        if any(fnmatchcase(rel, p) for p in cfg.get("exclude", [])):
            return False
        return any(fnmatchcase(rel, p) for p in cfg.get("include", ["*"]))

    def leaf_spec(self, leaf, path):
        kind = leaf["kind"]
        calc = bool(leaf.get("calculated") or leaf.get("xml_ignore"))
        spec = {"type": {"float": "real", "int": "int", "bool": "bool", "list": "enum",
                         "vec3": "bank3" if path in BANKS else "vec3", "vec3_list": "vec3_list"}[kind]}
        unit = self.unit(leaf["unit"])
        if unit and kind not in ("bool", "list"):
            spec["unit"] = unit
        val = leaf["val"]
        if kind == "float":
            spec["default"] = num(val)
        elif kind in ("int", "list"):
            spec["default"] = val
        elif kind == "bool":
            spec["default"] = "true" if val else "false"
        elif kind == "vec3":
            spec["default"] = "[" + ", ".join(num(x) for x in val) + "]"
        elif kind == "vec3_list":
            spec["default"] = "[" + ", ".join("[" + ", ".join(num(x) for x in e) + "]" for e in val) + "]"
            spec["max_count"] = LIST_CAPACITY
        if kind in ("float", "int") and not calc:
            spec["min"] = num(leaf["min"]) if kind == "float" else leaf["min"]
            spec["max"] = num(leaf["max"]) if kind == "float" else leaf["max"]
        if kind == "list":
            spec["options"] = leaf["options"]
        if calc:
            spec["calculated"] = "true"
        hint = (leaf.get("hint") or "").strip()
        comment = (leaf.get("comment") or "").strip()
        doc = "" if hint == "not set" else hint
        if comment and comment != "not set":
            doc = f"{doc} - {comment}" if doc else comment
        spec["doc"] = doc
        return spec

    @staticmethod
    def flow(spec):
        parts = []
        for k, v in spec.items():
            if k == "doc":
                parts.append(f"doc: {q(v)}")
            elif k == "options":
                parts.append("options: [" + ", ".join(q(o) for o in v) + "]")
            elif k == "unit":
                parts.append(f"unit: {q(v)}")
            else:
                parts.append(f"{k}: {v}")
        return "{" + ", ".join(parts) + "}"

    # ----------------------------------------------------------------------------------------------------------
    def emit(self, lines, node, rel, cfg, top, indent):
        pad = "  " * indent
        for c in node["children"]:
            crel = rel + [c["name"]]
            r = "/".join(crel)
            if c["kind"] == "group":
                sub = []
                self.emit(sub, c, crel, cfg, top, indent + 1)
                if not sub:
                    continue
                lines.append(f"{pad}{c['name']}:")
                shared = next((t for t, classes in SHARED_TYPES.items() if c["csharp_type"] in classes), None)
                if shared:
                    self.emit_shared(lines, c, crel, top, indent + 1, shared)
                else:
                    lines.extend(sub)
            elif self.selected(r, cfg):
                lines.append(f"{pad}{c['name']}: {self.flow(self.leaf_spec(c, top + '/' + r))}")

    def emit_shared(self, lines, group, rel, top, indent, shared):
        pad = "  " * indent
        specs = {leaf["name"]: self.leaf_spec(leaf, top + "/" + "/".join(rel + [leaf["name"]])) for leaf in group["children"]}
        base = self.type_registry.setdefault(shared, specs)
        lines.append(f"{pad}_type: {shared}")
        for name, spec in specs.items():
            diff = {k: v for k, v in spec.items() if base[name].get(k) != v}
            if diff:
                lines.append(f"{pad}{name}: {self.flow(diff)}")

    def exclusion_patterns(self, node, rel, cfg):
        """Smallest set of path patterns (relative to the document root) that covers all excluded leaves."""
        base = [cfg["xml"]] if cfg["xml"] else []

        def walk(n, r):
            if n["kind"] != "group":
                return [], not self.selected("/".join(r), cfg)
            patterns, fully = [], []
            for c in n["children"]:
                p, f = walk(c, r + [c["name"]])
                patterns.extend(p)
                fully.append((c, f))
            if all(f for _, f in fully):
                return [], True
            for c, f in fully:
                if f:
                    patterns.append("/".join(base + r + [c["name"]]))
            return patterns, False

        patterns, _ = walk(node, rel)
        return patterns

    def write(self, target):
        body, unity_only = [], {k: list(v) for k, v in EXTRA_UNITY_ONLY.items()}
        for top, cfg in TOP.items():
            node = self.locate(cfg["csharp"])
            body += [f"{top}:", f"  _document: {cfg['document']}", f"  _csharp: {cfg['csharp']}", f"  _xml: {q(cfg['xml'])}"]
            self.emit(body, node, [], cfg, top, 1)
            body.append("")
            unity_only[cfg["document"]].extend(self.exclusion_patterns(node, [], cfg))

        out = [
            "# Single source of truth for the physics relevant parameters of the HoverHere helicopter simulator.",
            "# Bootstrapped by tools/csharp_reference/seed_to_yaml.py from Helicopter_Parameter.cs, maintained by hand since.",
            "# Consumers: schema/tools/generate_params.py (C++ structs, descriptors, SI compile, C# structs, XML loader tables).",
            "#",
            "# Leaf keys: type (real|int|bool|enum|vec3|bank3|vec3_list), unit (schema/units.yaml), default, min, max, doc,",
            "#            options (enum), max_count (vec3_list), calculated (derived value, never read from files),",
            "#            xml (element name if it differs from the key), formula (calculated leaves).",
            "# Group keys: _type (shared struct type from `types`), _document, _csharp, _xml (top level groups only).",
            "schema_version: 1",
            "",
            "# Elements of the Unity parameter files that are no physics parameters (not loaded, not reported as unknown).",
            "unity_only:",
        ]
        for doc, items in unity_only.items():
            out.append(f"  {doc}:")
            out.extend(f"    - {q(p)}" for p in items)
        out += ["", "types:"]
        for t, specs in self.type_registry.items():
            out.append(f"  {t}:")
            out.extend(f"    {name}: {self.flow(spec)}" for name, spec in specs.items())
        out.append("")
        out.extend(body)
        Path(target).write_text("\n".join(out).rstrip() + "\n", encoding="utf-8", newline="\n")
        if self.unknown_units:
            print("units without an entry in units.yaml:", sorted(self.unknown_units))


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 2
    seed = json.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
    Seeder(seed).write(sys.argv[2])
    print("wrote", sys.argv[2])
    return 0


if __name__ == "__main__":
    sys.exit(main())
