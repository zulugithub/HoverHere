#include "hoverhere/io/param_loader.hpp"

#include <algorithm>
#include <cctype>
#include <charconv>
#include <cmath>
#include <cstdlib>
#include <fstream>
#include <iterator>
#include <set>
#include <sstream>
#include <unordered_map>
#include <utility>
#include <version>

#include <pugixml.hpp>

namespace hoverhere::io {
namespace {

using params::DocumentInfo;
using params::ParamDescriptor;
using params::ParamDocument;
using params::ParamType;

// ---------------------------------------------------------------------------------------------------------------
// text helpers
// ---------------------------------------------------------------------------------------------------------------
std::string_view trim(std::string_view s) noexcept
{
    while (!s.empty() && std::isspace(static_cast<unsigned char>(s.front())) != 0) {
        s.remove_prefix(1);
    }
    while (!s.empty() && std::isspace(static_cast<unsigned char>(s.back())) != 0) {
        s.remove_suffix(1);
    }
    return s;
}

bool parse_real(std::string_view s, double& out)
{
    s = trim(s);
    if (!s.empty() && s.front() == '+') {
        s.remove_prefix(1);
    }
    if (s.empty()) {
        return false;
    }
#if defined(__cpp_lib_to_chars) && __cpp_lib_to_chars >= 201611L
    const auto [end, ec] = std::from_chars(s.data(), s.data() + s.size(), out);
    return ec == std::errc{} && end == s.data() + s.size() && std::isfinite(out);
#else
    // no floating-point from_chars in this standard library (e.g. older libc++): strtod, which expects the "C" locale
    const std::string copy(s);
    char* end = nullptr;
    out = std::strtod(copy.c_str(), &end);
    return end == copy.c_str() + copy.size() && std::isfinite(out);
#endif
}

bool parse_int(std::string_view s, std::int32_t& out) noexcept
{
    s = trim(s);
    if (!s.empty() && s.front() == '+') {
        s.remove_prefix(1);
    }
    if (s.empty()) {
        return false;
    }
    const auto [end, ec] = std::from_chars(s.data(), s.data() + s.size(), out);
    return ec == std::errc{} && end == s.data() + s.size();
}

bool parse_bool(std::string_view s, bool& out) noexcept
{
    s = trim(s);
    if (s == "true" || s == "1") {
        out = true;
        return true;
    }
    if (s == "false" || s == "0") {
        out = false;
        return true;
    }
    return false;
}

std::string strip_indices(std::string_view path)
{
    std::string out;
    bool skipping = false;
    for (const char c : path) {
        if (c == '[') {
            skipping = true;
        } else if (c == ']') {
            skipping = false;
        } else if (!skipping) {
            out.push_back(c);
        }
    }
    return out;
}

// ---------------------------------------------------------------------------------------------------------------
// element lookup
// ---------------------------------------------------------------------------------------------------------------
struct Step {
    std::string name;
    int index;  // -1: the first element with that name (XmlSerializer reads each member once, later duplicates are unknown nodes)
};

std::vector<Step> split_path(std::string_view path)
{
    std::vector<Step> steps;
    while (!path.empty()) {
        const std::size_t slash = path.find('/');
        const std::string_view part = path.substr(0, slash);
        path = slash == std::string_view::npos ? std::string_view{} : path.substr(slash + 1);
        Step step{std::string(part), -1};
        const std::size_t bracket = part.find('[');
        if (bracket != std::string_view::npos) {
            step.name = std::string(part.substr(0, bracket));
            step.index = std::stoi(std::string(part.substr(bracket + 1)));
        }
        steps.push_back(std::move(step));
    }
    return steps;
}

pugi::xml_node pick_child(const pugi::xml_node& parent, const std::string& name, const int index)
{
    int i = 0;
    for (pugi::xml_node c = parent.child(name.c_str()); c; c = c.next_sibling(name.c_str()), ++i) {
        if (index < 0 || i == index) {
            return c;
        }
    }
    return {};
}

pugi::xml_node find_node(pugi::xml_node node, const std::vector<Step>& steps)
{
    for (const Step& step : steps) {
        node = pick_child(node, step.name, step.index);
        if (!node) {
            break;
        }
    }
    return node;
}

// ---------------------------------------------------------------------------------------------------------------
// loader of one document
// ---------------------------------------------------------------------------------------------------------------
class Loader {
public:
    Loader(const std::string_view text, const DocumentInfo& info, void* const root, const std::span<const ParamDescriptor> table,
           const LoadOptions& options, LoadReport& report)
        : info_(info), root_(root), table_(table), options_(options), report_(report)
    {
        line_starts_.push_back(0);
        for (std::size_t i = 0; i < text.size(); ++i) {
            if (text[i] == '\n') {
                line_starts_.push_back(i + 1);
            }
        }
    }

    int line_of_offset(const std::ptrdiff_t offset) const
    {
        if (offset < 0) {
            return 0;
        }
        const auto it = std::upper_bound(line_starts_.begin(), line_starts_.end(), static_cast<std::size_t>(offset));
        return static_cast<int>(it - line_starts_.begin());
    }

    void run(const pugi::xml_document& doc)
    {
        const pugi::xml_node root_element = doc.document_element();
        if (!root_element) {
            add(Severity::Error, "the file has no root element", "", 0);
            return;
        }
        if (info_.root_element != root_element.name()) {
            add(Severity::Error, std::string("root element is '") + root_element.name() + "' but '" + std::string(info_.root_element) +
                                     "' is expected",
                "", line_of(root_element));
            return;
        }
        index_descriptors();

        std::vector<std::pair<std::size_t, pugi::xml_node>> loaded;
        for (std::size_t i = 0; i < table_.size(); ++i) {
            const ParamDescriptor& d = table_[i];
            if (d.document != info_.id || d.calculated) {
                continue;
            }
            const pugi::xml_node element = find_node(root_element, split_path(d.xml_path));
            if (!element) {
                if (d.required) {
                    add(Severity::Error, "required element is missing", std::string(d.xml_path), 0);
                }
                continue;
            }
            if (read_leaf(d, element)) {
                ++report_.values_loaded;
                loaded.emplace_back(i, element);
            }
        }

        walk(root_element, "");

        if (options_.report_range_violations) {
            for (const auto& [index, element] : loaded) {
                check_range(table_[index], element);
            }
        }
    }

private:
    int line_of(const pugi::xml_node& node) const { return line_of_offset(node.offset_debug()); }

    void add(const Severity severity, std::string message, std::string path, const int line)
    {
        report_.diagnostics.push_back({severity, std::move(message), std::move(path), line});
    }

    // ---- known element paths ------------------------------------------------------------------------------------
    void index_descriptors()
    {
        for (const ParamDescriptor& d : table_) {
            if (d.document != info_.id) {
                continue;
            }
            const std::string path = strip_indices(d.xml_path);
            leaf_types_[path] = d.type;
            for (std::size_t slash = path.find('/'); slash != std::string::npos; slash = path.find('/', slash + 1)) {
                groups_.insert(path.substr(0, slash));
            }
            // array items ("limb_stationary/stru_limb_stationary[1]/...") may legitimately repeat
            std::string_view rest = d.xml_path;
            std::string prefix;
            while (!rest.empty()) {
                const std::size_t slash = rest.find('/');
                const std::string_view part = rest.substr(0, slash);
                rest = slash == std::string_view::npos ? std::string_view{} : rest.substr(slash + 1);
                prefix += prefix.empty() ? "" : "/";
                prefix += strip_indices(part);
                if (part.find('[') != std::string_view::npos) {
                    items_.insert(prefix);
                }
            }
        }
    }

    bool is_ignored(const std::string& path) const
    {
        return std::any_of(info_.ignored_elements.begin(), info_.ignored_elements.end(), [&path](const std::string_view pattern) {
            return path == pattern || (path.size() > pattern.size() && path.compare(0, pattern.size(), pattern) == 0 &&
                                       path[pattern.size()] == '/');
        });
    }

    // ---- reading values -----------------------------------------------------------------------------------------
    static pugi::xml_node first_child_named(const pugi::xml_node& parent, const char* name) { return pick_child(parent, name, -1); }

    bool number_at(const ParamDescriptor& d, const pugi::xml_node& node, const std::size_t component)
    {
        double v = 0;
        const char* text = node.child_value();
        if (!parse_real(text, v)) {
            add(Severity::Error, std::string("'") + std::string(trim(text)) + "' is not a number", std::string(d.xml_path), line_of(node));
            return false;
        }
        params::write_component(root_, d, component, v);
        return true;
    }

    bool read_scalar(const ParamDescriptor& d, const pugi::xml_node& node)
    {
        const std::string_view text = node.child_value();
        switch (d.type) {
        case ParamType::Real:
            return number_at(d, node, 0);
        case ParamType::Int:
        case ParamType::Enum: {
            std::int32_t v = 0;
            if (!parse_int(text, v)) {
                add(Severity::Error, std::string("'") + std::string(trim(text)) + "' is not an integer", std::string(d.xml_path), line_of(node));
                return false;
            }
            params::write_component(root_, d, 0, static_cast<double>(v));
            return true;
        }
        case ParamType::Bool: {
            bool v = false;
            if (!parse_bool(text, v)) {
                add(Severity::Error, std::string("'") + std::string(trim(text)) + "' is not a boolean (true, false, 1 or 0)",
                    std::string(d.xml_path), line_of(node));
                return false;
            }
            params::write_component(root_, d, 0, v ? 1.0 : 0.0);
            return true;
        }
        default:
            return false;
        }
    }

    // x, y, z children of `vec`; `first` is the component index of x. Missing children keep their value unless `required`.
    bool read_xyz(const ParamDescriptor& d, const pugi::xml_node& vec, const std::size_t first, const bool missing_is_error)
    {
        static constexpr const char* axes[3] = {"x", "y", "z"};
        bool any = false;
        for (std::size_t axis = 0; axis < 3; ++axis) {
            const pugi::xml_node c = first_child_named(vec, axes[axis]);
            if (!c) {
                if (missing_is_error) {
                    add(Severity::Error, std::string("element <") + axes[axis] + "> is missing", std::string(d.xml_path), line_of(vec));
                }
                continue;
            }
            if (!number_at(d, c, first + axis)) {
                return false;
            }
            any = true;
        }
        return any;
    }

    bool read_leaf(const ParamDescriptor& d, const pugi::xml_node& element)
    {
        const bool plain = info_.plain_encoding;
        switch (d.type) {
        case ParamType::Real:
        case ParamType::Int:
        case ParamType::Bool:
        case ParamType::Enum: {
            const pugi::xml_node value = plain ? element : first_child_named(element, "val");
            return value && read_scalar(d, value);
        }
        case ParamType::Vec3:
        case ParamType::Bank: {
            const pugi::xml_node vec = plain ? element : first_child_named(element, "vect3");
            return vec && read_xyz(d, vec, 0, plain && d.required);
        }
        case ParamType::Mat3: {
            std::vector<pugi::xml_node> cells;
            for (pugi::xml_node c = element.child("float"); c; c = c.next_sibling("float")) {
                cells.push_back(c);
            }
            if (cells.size() != 9) {
                add(Severity::Error, "a 3x3 matrix needs nine <float> elements, found " + std::to_string(cells.size()),
                    std::string(d.xml_path), line_of(element));
                return false;
            }
            for (std::size_t i = 0; i < 9; ++i) {
                if (!number_at(d, cells[i], i)) {
                    return false;
                }
            }
            return true;
        }
        case ParamType::PointList: {
            const pugi::xml_node list = first_child_named(element, "vect3");
            std::vector<pugi::xml_node> points;
            for (pugi::xml_node c = list.child("Vector3"); c; c = c.next_sibling("Vector3")) {
                points.push_back(c);
            }
            if (points.empty()) {
                return false;  // an empty list keeps the default list, like the XML merge does
            }
            if (points.size() > d.capacity) {
                add(Severity::Error, "the list has " + std::to_string(points.size()) + " entries but the capacity is " + std::to_string(d.capacity),
                    std::string(d.xml_path), line_of(list));
                return false;
            }
            params::set_list_count(root_, d, points.size());
            for (std::size_t i = 0; i < points.size(); ++i) {
                for (std::size_t axis = 0; axis < 3; ++axis) {
                    params::write_component(root_, d, 3 * i + axis, 0.0);
                }
                if (!read_xyz(d, points[i], 3 * i, false)) {
                    return false;
                }
            }
            return true;
        }
        }
        return false;
    }

    // ---- unknown elements ---------------------------------------------------------------------------------------
    void unknown(const pugi::xml_node& node, const std::string& path)
    {
        add(options_.unknown_elements_are_errors ? Severity::Error : Severity::Warning,
            "unknown element <" + std::string(node.name()) + ">: it is not a parameter of this document and is ignored", path, line_of(node));
    }

    void check_leaf_children(const pugi::xml_node& leaf, const std::string& path, const ParamType type)
    {
        static const std::set<std::string, std::less<>> metadata = {"val", "vect3", "str", "hint", "comment", "unit", "calculated",
                                                                     "show_in_sim", "save_under_player_prefs", "min", "max"};
        const auto check_children = [this](const pugi::xml_node& parent, const std::string& parent_path, const std::set<std::string, std::less<>>& allowed) {
            for (pugi::xml_node c = parent.first_child(); c; c = c.next_sibling()) {
                if (c.type() == pugi::node_element && allowed.find(c.name()) == allowed.end()) {
                    unknown(c, parent_path + "/" + c.name());
                }
            }
        };
        static const std::set<std::string, std::less<>> xyz = {"x", "y", "z"};
        static const std::set<std::string, std::less<>> point = {"Vector3"};
        static const std::set<std::string, std::less<>> cells = {"float"};
        static const std::set<std::string, std::less<>> none;

        if (info_.plain_encoding) {
            check_children(leaf, path, type == ParamType::Vec3 ? xyz : type == ParamType::Mat3 ? cells : none);
            return;
        }
        check_children(leaf, path, metadata);
        for (pugi::xml_node v = leaf.child("vect3"); v; v = v.next_sibling("vect3")) {
            if (type == ParamType::PointList) {
                check_children(v, path + "/vect3", point);
                for (pugi::xml_node p = v.child("Vector3"); p; p = p.next_sibling("Vector3")) {
                    check_children(p, path + "/vect3/Vector3", xyz);
                }
            } else {
                check_children(v, path + "/vect3", xyz);
            }
        }
    }

    void walk(const pugi::xml_node& node, const std::string& path)
    {
        std::unordered_map<std::string, int> seen;
        for (pugi::xml_node c = node.first_child(); c; c = c.next_sibling()) {
            if (c.type() != pugi::node_element) {
                continue;
            }
            const std::string name = c.name();
            const std::string child_path = path.empty() ? name : path + "/" + name;
            if (is_ignored(child_path)) {
                continue;
            }
            if (++seen[name] == 2 && items_.find(child_path) == items_.end()) {
                add(Severity::Warning, "element <" + name + "> appears more than once, only the first one is used", child_path, line_of(c));
            }
            if (const auto leaf = leaf_types_.find(child_path); leaf != leaf_types_.end()) {
                check_leaf_children(c, child_path, leaf->second);
            } else if (groups_.find(child_path) != groups_.end()) {
                walk(c, child_path);
            } else {
                unknown(c, child_path);
            }
        }
    }

    // ---- ranges -------------------------------------------------------------------------------------------------
    void check_range(const ParamDescriptor& d, const pugi::xml_node& element)
    {
        const std::size_t count = params::value_count(root_, d);
        for (std::size_t i = 0; i < count; ++i) {
            const double v = params::read_component(root_, d, i);
            if (!(v >= d.min && v <= d.max)) {
                add(Severity::Warning, params::describe({&d, i, v}), std::string(d.xml_path), line_of(element));
                break;
            }
        }
    }

    const DocumentInfo& info_;
    void* root_;
    std::span<const ParamDescriptor> table_;
    const LoadOptions& options_;
    LoadReport& report_;
    std::vector<std::size_t> line_starts_;
    std::unordered_map<std::string, ParamType> leaf_types_;
    std::set<std::string> groups_;
    std::set<std::string> items_;
};

// ---------------------------------------------------------------------------------------------------------------
// entry points
// ---------------------------------------------------------------------------------------------------------------
LoadReport load_text(const std::string_view xml, const std::string_view source, const ParamDocument document, void* const root,
                     const std::span<const ParamDescriptor> table, const LoadOptions& options)
{
    LoadReport report;
    report.source = std::string(source);
    pugi::xml_document doc;
    const pugi::xml_parse_result result = doc.load_buffer(xml.data(), xml.size(), pugi::parse_default, pugi::encoding_auto);
    Loader loader(xml, params::document_info(document), root, table, options, report);
    if (!result) {
        report.diagnostics.push_back({Severity::Error, std::string("XML syntax error: ") + result.description(), "", loader.line_of_offset(result.offset)});
        return report;
    }
    loader.run(doc);
    return report;
}

bool read_file(const std::filesystem::path& file, std::string& text, LoadReport& report)
{
    std::ifstream in(file, std::ios::binary);
    if (!in) {
        report.source = file.string();
        report.diagnostics.push_back({Severity::Error, "cannot open the file", "", 0});
        return false;
    }
    std::ostringstream buffer;
    buffer << in.rdbuf();
    text = buffer.str();
    return true;
}

template <class Target, class Loadfn>
LoadReport load_file(const std::filesystem::path& file, Target& target, Loadfn&& load_text_fn)
{
    LoadReport report;
    std::string text;
    if (!read_file(file, text, report)) {
        return report;
    }
    return load_text_fn(text, file.string(), target);
}

}  // namespace

std::size_t LoadReport::count(const Severity severity) const noexcept
{
    return static_cast<std::size_t>(std::count_if(diagnostics.begin(), diagnostics.end(),
                                                  [severity](const Diagnostic& d) { return d.severity == severity; }));
}

std::string LoadReport::format() const
{
    std::string out;
    for (const Diagnostic& d : diagnostics) {
        out += source;
        if (d.line > 0) {
            out += "(" + std::to_string(d.line) + ")";
        }
        out += d.severity == Severity::Error ? ": error: " : d.severity == Severity::Warning ? ": warning: " : ": info: ";
        out += d.message;
        if (!d.element_path.empty()) {
            out += " [" + d.element_path + "]";
        }
        out += "\n";
    }
    return out;
}

std::optional<ParamDocument> detect_document(const std::string_view xml)
{
    pugi::xml_document doc;
    if (!doc.load_buffer(xml.data(), xml.size(), pugi::parse_default, pugi::encoding_auto)) {
        return std::nullopt;
    }
    const std::string_view root = doc.document_element().name();
    for (const ParamDocument candidate : {ParamDocument::Heli, ParamDocument::Scenery, ParamDocument::Simulation,
                                          ParamDocument::MainrotorMechanics, ParamDocument::TailrotorMechanics}) {
        if (params::document_info(candidate).root_element == root) {
            return candidate;
        }
    }
    return std::nullopt;
}

LoadReport load_parameters_from_text(const std::string_view xml, const std::string_view source_name, const ParamDocument document,
                                     params::ModelParams& target, const LoadOptions& options)
{
    if (document != ParamDocument::Heli && document != ParamDocument::Scenery && document != ParamDocument::Simulation) {
        LoadReport report;
        report.source = std::string(source_name);
        report.diagnostics.push_back({Severity::Error, "not a parameter document", "", 0});
        return report;
    }
    params::ModelParams work = target;
    LoadReport report = load_text(xml, source_name, document, &work, params::model_param_descriptors(), options);
    if (report.ok()) {
        target = work;
    }
    return report;
}

LoadReport load_parameters_file(const std::filesystem::path& file, const ParamDocument document, params::ModelParams& target,
                                const LoadOptions& options)
{
    return load_file(file, target, [&](const std::string& text, const std::string& name, params::ModelParams& t) {
        return load_parameters_from_text(text, name, document, t, options);
    });
}

LoadReport load_mainrotor_mechanics_from_text(const std::string_view xml, const std::string_view source_name,
                                              params::mechanics::MainrotorMechanics& target, const LoadOptions& options)
{
    params::mechanics::MainrotorMechanics work = target;
    LoadReport report = load_text(xml, source_name, ParamDocument::MainrotorMechanics, &work, params::mechanics::mainrotor_mechanics_descriptors(), options);
    if (report.ok()) {
        target = work;
    }
    return report;
}

LoadReport load_mainrotor_mechanics_file(const std::filesystem::path& file, params::mechanics::MainrotorMechanics& target,
                                         const LoadOptions& options)
{
    return load_file(file, target, [&](const std::string& text, const std::string& name, params::mechanics::MainrotorMechanics& t) {
        return load_mainrotor_mechanics_from_text(text, name, t, options);
    });
}

LoadReport load_tailrotor_mechanics_from_text(const std::string_view xml, const std::string_view source_name,
                                              params::mechanics::TailrotorMechanics& target, const LoadOptions& options)
{
    params::mechanics::TailrotorMechanics work = target;
    LoadReport report = load_text(xml, source_name, ParamDocument::TailrotorMechanics, &work, params::mechanics::tailrotor_mechanics_descriptors(), options);
    if (report.ok()) {
        target = work;
    }
    return report;
}

LoadReport load_tailrotor_mechanics_file(const std::filesystem::path& file, params::mechanics::TailrotorMechanics& target,
                                         const LoadOptions& options)
{
    return load_file(file, target, [&](const std::string& text, const std::string& name, params::mechanics::TailrotorMechanics& t) {
        return load_tailrotor_mechanics_from_text(text, name, t, options);
    });
}

}  // namespace hoverhere::io
