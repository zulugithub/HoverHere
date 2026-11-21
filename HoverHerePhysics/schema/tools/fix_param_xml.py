"""Moves misplaced elements of the shipped Unity parameter files to the place the C# classes read them from.

The C# XmlSerializer silently ignores elements it does not know and every member it reads only once, so these values
were never active in Unity:

  helicopter/fuselage/S_fxyz/downwash_factor_mainrotor   ->  helicopter/fuselage/downwash_factor_mainrotor
  helicopter/fuselage/S_fxyz/downwash_factor_tailrotor   ->  helicopter/fuselage/downwash_factor_tailrotor
  tuning/rotor_tuning/propeller                          ->  tuning/rotor_tuning/propeller_tuning
  <flapping> next to <helicopter>                        ->  helicopter/flapping
  second and further <x> directly below <helicopter>     ->  removed (only the first one is read)

The edit is textual (line endings, comments, indentation and everything else stay byte for byte). It is idempotent.
hoverhere_params_check reports what is still wrong afterwards.

    py -3 fix_param_xml.py [--check] <file-or-directory>...

--check changes nothing and exits with 1 when a file would be modified.
"""
from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path

DOWNWASH = re.compile(r"[ \t]*<(downwash_factor_(?:mainrotor|tailrotor))>.*?</\1>[ \t]*(?:\r?\n)", re.DOTALL)
S_FXYZ = re.compile(r"(?P<indent>[ \t]*)<S_fxyz>(?P<body>.*?)</S_fxyz>(?P<tail>[ \t]*)(?P<eol>\r?\n)", re.DOTALL)
FUSELAGE = re.compile(r"<fuselage>(?P<body>.*?)</fuselage>", re.DOTALL)
ROTOR_TUNING = re.compile(r"<rotor_tuning>(?P<body>.*?)</rotor_tuning>", re.DOTALL)

HELI_ROOT = "stru_transmitter_and_helicopter"
LIFTED_INTO_HELICOPTER = {"flapping"}  # root level elements that belong below <helicopter>

_TAG = re.compile(r"<!--.*?-->|<\?.*?\?>|<(?P<close>/)?(?P<name>[A-Za-z_][\w.\-]*)(?P<attrs>[^<>]*?)(?P<selfclose>/)?>", re.DOTALL)


@dataclass
class _Element:
    name: str
    open_start: int
    open_end: int
    close_start: int  # == open_start for an empty element <x/>
    close_end: int


def _children(text: str, start: int, end: int) -> list[_Element]:
    """The direct child elements of the content text[start:end] (comments and the declaration are skipped)."""
    out: list[_Element] = []
    depth = 0
    current: _Element | None = None
    for m in _TAG.finditer(text, start, end):
        if m.group(0).startswith(("<!--", "<?")):
            continue
        if m.group("close"):
            depth -= 1
            if depth == 0 and current is not None:
                current.close_start, current.close_end = m.start(), m.end()
                out.append(current)
        elif m.group("selfclose"):
            if depth == 0:
                out.append(_Element(m.group("name"), m.start(), m.end(), m.start(), m.end()))
        else:
            if depth == 0:
                current = _Element(m.group("name"), m.start(), m.end(), -1, -1)
            depth += 1
    return out


def _line_span(text: str, start: int, end: int) -> tuple[int, int]:
    """[start, end) widened to whole lines when only blanks share its first and last line."""
    line_start = text.rfind("\n", 0, start) + 1
    line_end = text.find("\n", end)
    line_end = len(text) if line_end < 0 else line_end + 1
    if text[line_start:start].strip() == "" and text[end:line_end].strip() == "":
        return line_start, line_end
    return start, end


def _helicopter(text: str) -> tuple[_Element, _Element] | None:
    roots = _children(text, 0, len(text))
    if not roots or roots[0].name != HELI_ROOT:
        return None
    root = roots[0]
    for kid in _children(text, root.open_end, root.close_start):
        if kid.name == "helicopter":
            return root, kid
    return None


def _move_downwash_out_of_s_fxyz(text: str) -> str:
    def fix_fuselage(m: re.Match) -> str:
        body = m.group("body")
        s = S_FXYZ.search(body)
        if not s:
            return m.group(0)
        moved = [d.group(0) for d in DOWNWASH.finditer(s.group("body"))]
        if not moved:
            return m.group(0)
        inner = DOWNWASH.sub("", s.group("body"))
        indent = s.group("indent")
        eol = s.group("eol")
        lifted = "".join(indent + e.lstrip(" \t") for e in moved)  # same indentation as <S_fxyz>, one level up from the old place
        new_s = f"{indent}<S_fxyz>{inner}</S_fxyz>{s.group('tail')}{eol}{lifted}"
        return "<fuselage>" + body[: s.start()] + new_s + body[s.end():] + "</fuselage>"

    return FUSELAGE.sub(fix_fuselage, text, count=1)


def _rename_propeller_tuning(text: str) -> str:
    def fix(m: re.Match) -> str:
        body = m.group("body").replace("<propeller>", "<propeller_tuning>").replace("</propeller>", "</propeller_tuning>")
        return "<rotor_tuning>" + body + "</rotor_tuning>"

    return ROTOR_TUNING.sub(fix, text, count=1)


def _lift_root_level_helicopter_children(text: str) -> str:
    found = _helicopter(text)
    if found is None:
        return text
    root, heli = found
    lifted = [k for k in _children(text, root.open_end, root.close_start) if k.name in LIFTED_INTO_HELICOPTER]
    if not lifted:
        return text
    eol = "\r\n" if "\r\n" in text else "\n"

    moved_text = ""
    edits: list[tuple[int, int, str]] = []  # (position, length to delete, text to insert)
    for k in lifted:
        start, end = _line_span(text, k.open_start, k.close_end)
        piece = text[start:end]
        if end == k.close_end:  # not on lines of its own: give it one
            piece = "    " + piece + eol
        moved_text += piece
        edits.append((start, end - start, ""))

    line_start = text.rfind("\n", 0, heli.close_start) + 1
    if text[line_start:heli.close_start].strip() == "":
        edits.append((line_start, 0, moved_text))
    else:  # </helicopter> shares its line with other content: put it on a line of its own
        blank_start = len(text[: heli.close_start].rstrip(" \t"))
        heli_line = text.rfind("\n", 0, heli.open_start) + 1
        heli_indent = text[heli_line:heli.open_start] if text[heli_line:heli.open_start].strip() == "" else ""
        edits.append((blank_start, heli.close_start - blank_start, eol + moved_text + heli_indent))

    for position, length, insert in sorted(edits, key=lambda e: e[0], reverse=True):
        text = text[:position] + insert + text[position + length:]
    return text


def _drop_repeated_helicopter_children(text: str) -> str:
    found = _helicopter(text)
    if found is None:
        return text
    _, heli = found
    seen: set[str] = set()
    spans: list[tuple[int, int]] = []
    for k in _children(text, heli.open_end, heli.close_start):
        if k.name in seen:
            spans.append(_line_span(text, k.open_start, k.close_end))
        seen.add(k.name)
    for start, end in reversed(spans):
        text = text[:start] + text[end:]
    return text


def fix_text(text: str) -> str:
    text = _move_downwash_out_of_s_fxyz(text)
    text = _rename_propeller_tuning(text)
    text = _lift_root_level_helicopter_children(text)
    return _drop_repeated_helicopter_children(text)


def parameter_files(paths: list[Path]):
    for p in paths:
        if p.is_dir():
            yield from sorted(p.rglob("*.xml"))
        else:
            yield p


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    ap.add_argument("paths", nargs="+", type=Path)
    ap.add_argument("--check", action="store_true", help="only report, do not modify")
    args = ap.parse_args(argv)

    changed = 0
    for file in parameter_files(args.paths):
        original = file.read_bytes().decode("utf-8")
        fixed = fix_text(original)
        if fixed == original:
            continue
        changed += 1
        print(("would fix " if args.check else "fixed ") + str(file))
        if not args.check:
            file.write_bytes(fixed.encode("utf-8"))
    print(f"{changed} file(s) {'would be ' if args.check else ''}modified")
    return 1 if args.check and changed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
