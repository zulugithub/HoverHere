"""Self test of the schema lint: every class of inconsistency has to be reported (python test_schema_lint.py)."""
import sys
import tempfile
import textwrap
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

from schema_lint import lint_model  # noqa: E402
from schema_model import Model, SchemaError, load_units  # noqa: E402

UNITS = load_units(Path(__file__).resolve().parent.parent / "units.yaml")

HEAD = """
schema_version: 1
model: {root_struct: Root}
documents: {heli: {root_element: stru_root, encoding: parameter}}
unity_only: {heli: ["transmitter"]}
g:
  _document: heli
  _csharp: g
  _xml: ""
"""


def lint(leaves: str, head: str = HEAD):
    with tempfile.TemporaryDirectory() as tmp:
        path = Path(tmp) / "schema.yaml"
        path.write_text(head + textwrap.indent(textwrap.dedent(leaves), "  "), encoding="utf-8")
        return lint_model(Model(path), UNITS)


def codes(leaves: str, head: str = HEAD):
    return sorted({i.code for i in lint(leaves, head) if i.level == "error"})


class SchemaLintTest(unittest.TestCase):
    def test_clean_schema(self):
        self.assertEqual(lint("""
            K_col: {type: real, unit: deg, default: 12, min: 0.01, max: 30, doc: "ok"}
            n: {type: int, unit: "-", default: 3, min: 1, max: 6, doc: "ok"}
            flag: {type: bool, default: true, doc: "ok"}
            kind: {type: enum, default: 1, options: [a, b], doc: "ok"}
            pos: {type: vec3, unit: m, default: [0, 0, 0], doc: "ok"}
            pts: {type: vec3_list, unit: m, default: [[0, 0, 0]], max_count: 2, doc: "ok"}
        """), [])

    def test_default_above_max_like_K_col(self):
        found = lint("K_col: {type: real, unit: deg, default: 12, min: 0.01, max: 10, doc: x}")
        self.assertEqual([(i.code, i.where) for i in found], [("L011", "g/K_col")])
        self.assertIn("default 12 is outside [0.01, 10]", found[0].message)

    def test_swapped_bounds(self):
        self.assertEqual(codes("gravity: {type: real, unit: 'm/s^2', default: 9.81, min: 20, max: 0, doc: x}"), ["L010"])

    def test_pinned_and_missing_bounds(self):
        self.assertEqual(codes("a: {type: real, unit: kg, default: 0, min: 0, max: 0, doc: x}"), ["L013"])
        self.assertEqual(codes("a: {type: real, unit: kg, default: 1, doc: x}"), ["L012"])

    def test_units(self):
        self.assertEqual(codes("a: {type: real, unit: furlong, default: 1, min: 0, max: 2, doc: x}"), ["L021"])
        self.assertEqual(codes("a: {type: real, default: 1, min: 0, max: 2, doc: x}"), ["L020"])

    def test_enum_and_list_limits(self):
        self.assertEqual(codes("k: {type: enum, default: 2, options: [a, b], doc: x}"), ["L030"])
        self.assertEqual(codes("p: {type: vec3_list, unit: m, default: [[0,0,0],[1,1,1]], max_count: 1, doc: x}"), ["L032"])

    def test_text_and_names(self):
        self.assertEqual(codes('a: {type: real, unit: kg, default: 1, min: 0, max: 2, doc: "form\\ffeed"}'), ["L071"])
        self.assertEqual(codes('a: {type: real, unit: kg, default: 1, min: 0, max: 2, doc: "caf\u00e9"}'), ["L072"])
        self.assertEqual(codes("default: {type: real, unit: kg, default: 1, min: 0, max: 2, doc: x}"), ["L001"])

    def test_xml_mapping(self):
        self.assertEqual(codes("""
            a: {type: real, unit: kg, default: 1, min: 0, max: 2, doc: x}
            b: {type: real, xml: a, unit: kg, default: 1, min: 0, max: 2, doc: x}
        """), ["L050"])
        self.assertEqual(codes("transmitter: {type: real, unit: kg, default: 1, min: 0, max: 2, doc: x}"), ["L051"])

    def test_calculated_values_need_no_bounds(self):
        self.assertEqual(lint("c: {type: real, unit: kg, default: 0, calculated: true, formula: 'a * 2', doc: x}"), [])

    def test_wrong_value_type_and_duplicate_keys(self):
        self.assertEqual(codes("a: {type: real, unit: kg, default: '1e-06', min: 0, max: 2, doc: x}"), ["L004"])
        with self.assertRaises(SchemaError):
            lint("a: {type: bool, default: true, doc: x}\na: {type: bool, default: false, doc: y}")


if __name__ == "__main__":
    unittest.main()
