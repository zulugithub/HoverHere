#!/usr/bin/env python3
"""Single source of truth tooling for the HoverHere parameters (schema/parameters.yaml, schema/mechanics.yaml).

    generate_params.py generate         lint, then write the generated C++ and C# files
    generate_params.py check            fail if a generated file differs from what `generate` would write
    generate_params.py lint [--seed seed.json] [--strict]
                                        schema consistency check; with --seed also compares the schema with the C# classes
                                        (seed: `csharp_reference schema-seed --out seed.json`)

Exit status: 0 = ok, 1 = errors found, 2 = usage error. Needs Python >= 3.9 and PyYAML.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from emit_cpp import emit_documents, emit_model, fingerprint  # noqa: E402
from emit_csharp import emit_csharp  # noqa: E402
from schema_lint import lint_against_seed, lint_model  # noqa: E402
from schema_model import Model, SchemaError, load_units  # noqa: E402

SCHEMA_DIR = Path(__file__).resolve().parent.parent
ROOT = SCHEMA_DIR.parent
SCHEMA_FILES = {"params": SCHEMA_DIR / "parameters.yaml", "mechanics": SCHEMA_DIR / "mechanics.yaml"}

PARAMS_INCLUDE = "physics/include/hoverhere/params"
PARAMS_SRC = "physics/src/params"
CSHARP_OUT = "bindings/csharp/HoverHere.Native"


def load_all():
    units = load_units(SCHEMA_DIR / "units.yaml")
    return units, {name: Model(path) for name, path in SCHEMA_FILES.items()}


def lint_all(units, models, seed=None):
    issues = []
    for name, model in models.items():
        found = lint_model(model, units)
        if name == "params" and seed:
            found += lint_against_seed(model, units, Path(seed))
        issues += [(SCHEMA_FILES[name].name, i) for i in found]
    return issues


def generated_files(units, models) -> dict[str, str]:
    all_models = list(models.values())
    fp = fingerprint(all_models, units)
    files = {}
    files[f"{PARAMS_INCLUDE}/param_documents.hpp"], files[f"{PARAMS_SRC}/param_documents.cpp"] = emit_documents(all_models, fp)
    files[f"{PARAMS_INCLUDE}/model_params.hpp"], files[f"{PARAMS_SRC}/model_params.cpp"] = emit_model(
        models["params"], units, fp, "model_params.hpp", "hoverhere::params", True,
        {"ModelParams": "model_param_descriptors"}, {"ModelParams": "make_default_params"})
    files[f"{PARAMS_INCLUDE}/mechanics_params.hpp"], files[f"{PARAMS_SRC}/mechanics_params.cpp"] = emit_model(
        models["mechanics"], units, fp, "mechanics_params.hpp", "hoverhere::params::mechanics", False,
        {"MainrotorMechanics": "mainrotor_mechanics_descriptors", "TailrotorMechanics": "tailrotor_mechanics_descriptors"},
        {"MainrotorMechanics": "make_default_mainrotor_mechanics", "TailrotorMechanics": "make_default_tailrotor_mechanics"})
    files[f"{CSHARP_OUT}/ModelParams.g.cs"] = emit_csharp(models["params"], fp)
    return files


def print_issues(issues) -> tuple[int, int]:
    errors = warnings = 0
    for file, issue in issues:
        print(f"{file}: {issue}")
        errors += issue.level == "error"
        warnings += issue.level == "warning"
    return errors, warnings


def cmd_lint(args) -> int:
    units, models = load_all()
    errors, warnings = print_issues(lint_all(units, models, args.seed))
    print(f"schema lint: {errors} error(s), {warnings} warning(s), {sum(len(m.flat) for m in models.values())} parameters "
          f"in {len(models)} schema files")
    return 1 if errors or (args.strict and warnings) else 0


def same_text(existing: bytes, text: str) -> bool:
    # a checkout with core.autocrlf turns the generated files into CRLF, which is not a difference
    return existing.replace(b"\r\n", b"\n") == text.encode("utf-8")


def cmd_generate(args) -> int:
    units, models = load_all()
    errors, _ = print_issues(lint_all(units, models))
    if errors:
        print(f"generate: {errors} lint error(s), nothing written", file=sys.stderr)
        return 1
    for rel, text in generated_files(units, models).items():
        target = ROOT / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        changed = not target.exists() or not same_text(target.read_bytes(), text)
        if changed:
            target.write_bytes(text.encode("utf-8"))
        print(f"{'updated  ' if changed else 'unchanged'} {rel}")
    return 0


def cmd_check(args) -> int:
    units, models = load_all()
    errors, _ = print_issues(lint_all(units, models))
    stale = []
    for rel, text in generated_files(units, models).items():
        target = ROOT / rel
        if not target.exists() or not same_text(target.read_bytes(), text):
            stale.append(rel)
    for rel in stale:
        print(f"stale: {rel}")
    if stale:
        print("generated files are out of date, run: python schema/tools/generate_params.py generate", file=sys.stderr)
    else:
        print("generated files are up to date")
    return 1 if stale or errors else 0


def main(argv) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    lint = sub.add_parser("lint", help="check the schema for inconsistencies")
    lint.add_argument("--seed", help="JSON written by 'csharp_reference schema-seed' (sync check against Helicopter_Parameter.cs)")
    lint.add_argument("--strict", action="store_true", help="treat warnings as errors")
    sub.add_parser("generate", help="write the generated C++ and C# files")
    sub.add_parser("check", help="fail if the generated files are out of date")
    args = parser.parse_args(argv)
    try:
        return {"lint": cmd_lint, "generate": cmd_generate, "check": cmd_check}[args.command](args)
    except SchemaError as e:
        print(f"error: {e}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
