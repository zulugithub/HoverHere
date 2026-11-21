"""Loading, resolving and laying out the HoverHere parameter schema (schema/*.yaml)."""
from __future__ import annotations

import copy
import hashlib
import re
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Optional

import yaml

SCHEMA_VERSION = 1

# type -> C++ / C# mapping and natural layout (identical on MSVC, GCC and Clang for x86-64 and arm64)
SCALAR_KINDS = {
    "real": dict(cpp="Real", cs="double", size=8, align=8, ptype="Real", comps=1),
    "int": dict(cpp="std::int32_t", cs="int", size=4, align=4, ptype="Int", comps=1),
    "bool": dict(cpp="bool", cs="byte", size=1, align=1, ptype="Bool", comps=1),
    "enum": dict(cpp="std::int32_t", cs="int", size=4, align=4, ptype="Enum", comps=1),
    "vec3": dict(cpp="math::Vec3", cs="Vec3d", size=24, align=8, ptype="Vec3", comps=3),
    "bank3": dict(cpp="Bank3", cs="Bank3d", size=24, align=8, ptype="Bank", comps=3),
    "mat3": dict(cpp="math::Mat3", cs="Mat3d", size=72, align=8, ptype="Mat3", comps=9),
}
LIST_KIND = "vec3_list"
LEAF_TYPES = tuple(SCALAR_KINDS) + (LIST_KIND,)
LEAF_KEYS = {"type", "unit", "default", "min", "max", "doc", "options", "max_count", "calculated", "xml", "formula", "required"}
GROUP_KEYS = {"_type", "_document", "_csharp", "_xml", "_array", "_struct", "_root"}

CPP_KEYWORDS = set("""alignas alignof and and_eq asm auto bitand bitor bool break case catch char char8_t char16_t char32_t class
compl concept const consteval constexpr constinit const_cast continue co_await co_return co_yield decltype default delete do
double dynamic_cast else enum explicit export extern false float for friend goto if inline int long mutable namespace new
noexcept not not_eq nullptr operator or or_eq private protected public register reinterpret_cast requires return short signed
sizeof static static_assert static_cast struct switch template this thread_local throw true try typedef typeid typename union
unsigned using virtual void volatile wchar_t while xor xor_eq""".split())
CS_KEYWORDS = set("""abstract as base bool break byte case catch char checked class const continue decimal default delegate do double
else enum event explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long
namespace new null object operator out override params private protected public readonly ref return sbyte sealed short sizeof
stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile
while""".split())
IDENTIFIER = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")


class SchemaError(Exception):
    pass


@dataclass
class Issue:
    level: str  # "error" | "warning"
    code: str
    where: str
    message: str

    def __str__(self) -> str:
        return f"{self.level}: [{self.code}] {self.where}: {self.message}"


@dataclass
class Unit:
    name: str
    dimension: str
    si: str
    factor: float
    offset: float
    aliases: list


@dataclass
class Leaf:
    name: str
    xml: str
    type: str
    unit: Optional[str]
    default: Any
    min: Optional[float]
    max: Optional[float]
    doc: str
    options: list
    max_count: int
    calculated: bool
    required: bool
    formula: str
    where: str
    offset: int = 0  # inside the parent struct
    size: int = 0
    align: int = 1

    @property
    def kind(self) -> dict:
        if self.type == LIST_KIND:
            return dict(cpp=f"Vec3List<{self.max_count}>", cs=f"Vec3List{self.max_count}", size=8 + 24 * self.max_count,
                        align=8, ptype="PointList", comps=3 * self.max_count)
        return SCALAR_KINDS[self.type]


@dataclass
class Group:
    name: str
    xml: str
    struct: str
    where: str
    children: list = field(default_factory=list)
    array_count: Optional[int] = None
    item_xml: Optional[str] = None
    type_ref: Optional[str] = None
    document: str = ""  # set on top level groups
    csharp: str = ""  # set on top level groups (absolute C# path of the element)
    offset: int = 0  # inside the parent struct
    size: int = 0  # of the whole member (array: count * struct size)
    align: int = 1


@dataclass
class Flat:
    """One leaf instance (arrays of structs expanded) with its absolute offset inside the root struct."""
    path: str
    xml_path: str
    csharp_path: str
    offset: int
    leaf: Leaf
    root: str
    document: str
    chain: tuple = ()  # (struct, member, array index or None, array stride) from the root struct down to the leaf


class _StrictLoader(yaml.SafeLoader):
    pass


def _construct_mapping(loader, node, deep=False):
    mapping = {}
    for key_node, value_node in node.value:
        key = loader.construct_object(key_node, deep=deep)
        if key in mapping:
            raise SchemaError(f"duplicate key '{key}' (line {key_node.start_mark.line + 1})")
        mapping[key] = loader.construct_object(value_node, deep=deep)
    return mapping


_StrictLoader.add_constructor(yaml.resolver.BaseResolver.DEFAULT_MAPPING_TAG, _construct_mapping)


def load_yaml(path: Path) -> dict:
    try:
        return yaml.load(path.read_text(encoding="utf-8"), Loader=_StrictLoader)
    except (yaml.YAMLError, SchemaError) as e:
        raise SchemaError(f"{path.name}: {e}") from e


def load_units(path: Path) -> dict:
    raw = load_yaml(path)
    units = {}
    for name, info in raw["units"].items():
        units[name] = Unit(name=name, dimension=info["dimension"], si=info["si"], factor=float(info["factor"]),
                           offset=float(info.get("offset", 0)), aliases=list(info.get("aliases", [])))
    return units


def camel(name: str) -> str:
    return "".join(part[:1].upper() + part[1:] for part in name.split("_") if part)


def join_path(*parts: str) -> str:
    return "/".join(p for p in parts if p)


def align_up(value: int, alignment: int) -> int:
    return (value + alignment - 1) // alignment * alignment


class Model:
    """A resolved schema file: root structs, struct definitions in dependency order and the flattened leaf list."""

    def __init__(self, path: Path):
        self.path = path
        self.issues: list[Issue] = []
        self.raw = load_yaml(path)
        if self.raw.get("schema_version") != SCHEMA_VERSION:
            raise SchemaError(f"{path.name}: schema_version must be {SCHEMA_VERSION}")
        self.types: dict = self.raw.get("types") or {}
        self.unity_only: dict = self.raw.get("unity_only") or {}
        self.documents: dict = self.raw.get("documents") or {}
        self.root_struct: Optional[str] = (self.raw.get("model") or {}).get("root_struct")
        self.roots: list[Group] = []
        self.structs: dict[str, Group] = {}  # definition (first instance) per struct name
        self.struct_order: list[str] = []  # dependencies first
        self.flat: list[Flat] = []
        self.sha = hashlib.sha256(path.read_bytes().replace(b"\r\n", b"\n")).hexdigest()
        self._build()
        self._layout()
        for root in self.roots:
            self._flatten(root, 0, "", "", root.csharp, root.document, root.struct, self.flat)

    def issue(self, level: str, code: str, where: str, message: str) -> None:
        self.issues.append(Issue(level, code, where, message))

    # ---------------------------------------------------------------------------------------------------------
    def _build(self) -> None:
        reserved = {"schema_version", "unity_only", "types", "model", "documents"}
        top = {k: v for k, v in self.raw.items() if k not in reserved}
        if self.root_struct:
            root = Group(name=self.root_struct, xml="", struct=self.root_struct, where=self.root_struct)
            for key, value in top.items():
                root.children.append(self._group(key, value, "", top_level=True))
            self.roots.append(root)
        else:
            for key, value in top.items():
                if not isinstance(value, dict) or not value.get("_root"):
                    raise SchemaError(f"{self.path.name}: top level group '{key}' needs _root: true or a model.root_struct")
                self.roots.append(self._group(key, value, "", top_level=True))
        for root in self.roots:
            self._register(root)

    def _group(self, name: str, raw: dict, parent_path: str, top_level: bool = False) -> Group:
        path = f"{parent_path}/{name}" if parent_path else name
        if not isinstance(raw, dict):
            raise SchemaError(f"{self.path.name}: '{path}' must be a mapping")
        for k in raw:
            if k.startswith("_") and k not in GROUP_KEYS:
                self.issue("error", "S005", path, f"unknown group key '{k}'")
        type_ref = raw.get("_type")
        members: dict = {}
        if type_ref:
            if type_ref not in self.types:
                raise SchemaError(f"{self.path.name}: {path}: unknown _type '{type_ref}'")
            members = copy.deepcopy(self.types[type_ref])
        for key, value in raw.items():
            if key.startswith("_"):
                continue
            base = members.get(key)
            if self._is_leaf(base) and isinstance(value, dict) and all(k in LEAF_KEYS for k in value):
                members[key] = {**base, **value}  # per instance override of default, bounds, doc ...
            else:
                members[key] = value
        array = raw.get("_array") or {}
        if top_level and self.root_struct:
            struct = raw.get("_struct") or camel(name) + "Params"
        else:
            struct = raw.get("_struct") or type_ref or camel(name)
        group = Group(name=name, xml=raw.get("_xml", name), struct=struct, where=path, type_ref=type_ref,
                      document=raw.get("_document", ""), csharp=raw.get("_csharp", ""),
                      array_count=array.get("count"), item_xml=array.get("item_xml"))
        for key, value in members.items():
            if self._is_leaf(value):
                group.children.append(self._leaf(key, value, path))
            elif isinstance(value, dict):
                group.children.append(self._group(key, value, path))
            else:
                raise SchemaError(f"{self.path.name}: {path}/{key}: expected a leaf or a group mapping")
        return group

    @staticmethod
    def _is_leaf(value: Any) -> bool:
        return isinstance(value, dict) and isinstance(value.get("type"), str) and value["type"] in LEAF_TYPES

    def _leaf(self, name: str, raw: dict, parent_path: str) -> Leaf:
        where = f"{parent_path}/{name}"
        for k in raw:
            if k not in LEAF_KEYS:
                self.issue("error", "S005", where, f"unknown key '{k}'")
        max_count = raw.get("max_count", 0)
        return Leaf(
            name=name, xml=str(raw.get("xml", name)), type=raw["type"], unit=raw.get("unit"), default=raw.get("default"),
            min=raw.get("min"), max=raw.get("max"), doc=str(raw.get("doc", "")), options=list(raw.get("options", [])),
            max_count=max_count if isinstance(max_count, int) and not isinstance(max_count, bool) else 0,
            calculated=bool(raw.get("calculated", False)), required=bool(raw.get("required", False)),
            formula=str(raw.get("formula", "")), where=where)

    # ---------------------------------------------------------------------------------------------------------
    @staticmethod
    def _signature(group: Group):
        sig = []
        for c in group.children:
            if isinstance(c, Leaf):
                sig.append((c.name, c.type, c.max_count if c.type == LIST_KIND else 0))
            else:
                sig.append((c.name, "struct", c.struct, c.array_count or 0))
        return tuple(sig)

    def _register(self, group: Group) -> None:
        for c in group.children:
            if isinstance(c, Group):
                self._register(c)
        existing = self.structs.get(group.struct)
        if existing is None:
            self.structs[group.struct] = group
            self.struct_order.append(group.struct)
        elif self._signature(existing) != self._signature(group):
            raise SchemaError(f"{self.path.name}: struct name '{group.struct}' is used for different member lists "
                              f"({existing.where} and {group.where}); set _struct to disambiguate")

    def _layout(self) -> None:
        # size and alignment of each struct; a member's own footprint (array: count * struct size) lives on the member
        self.struct_info: dict[str, tuple[int, int]] = {}
        for name in self.struct_order:
            group = self.structs[name]
            offset, max_align = 0, 1
            for c in group.children:
                if isinstance(c, Leaf):
                    c.size, c.align = c.kind["size"], c.kind["align"]
                else:
                    sub_size, sub_align = self.struct_info[c.struct]
                    c.size, c.align = sub_size * (c.array_count or 1), sub_align
                offset = align_up(offset, c.align)
                c.offset = offset
                offset += c.size
                max_align = max(max_align, c.align)
            self.struct_info[name] = (align_up(offset, max_align), max_align)

        def apply(g: Group) -> None:
            by_name = {c.name: c for c in self.structs[g.struct].children}
            for c in g.children:
                ref = by_name[c.name]
                c.offset, c.size, c.align = ref.offset, ref.size, ref.align
                if isinstance(c, Group):
                    apply(c)

        for root in self.roots:
            apply(root)

    def struct_size(self, name: str) -> int:
        return self.struct_info[name][0]

    def _flatten(self, group: Group, base: int, path: str, xml: str, csharp: str, document: str, root: str, out: list,
                 chain: tuple = ()) -> None:
        for c in group.children:
            offset = base + c.offset
            if isinstance(c, Leaf):
                out.append(Flat(path=path + c.name, xml_path=join_path(xml, c.xml), csharp_path=join_path(csharp, c.xml),
                                offset=offset, leaf=c, root=root, document=document,
                                chain=chain + ((group.struct, c.name, None, 0),)))
                continue
            child_document = c.document or document
            child_csharp = c.csharp or join_path(csharp, c.xml)
            if c.array_count:
                stride = self.struct_size(c.struct)
                for i in range(c.array_count):
                    self._flatten(c, offset + i * stride, f"{path}{c.name}[{i}]/",
                                  join_path(xml, c.xml, f"{c.item_xml}[{i}]"), child_csharp, child_document, root, out,
                                  chain + ((group.struct, c.name, i, stride),))
            else:
                self._flatten(c, offset, f"{path}{c.name}/", join_path(xml, c.xml), child_csharp, child_document, root, out,
                              chain + ((group.struct, c.name, None, 0),))
