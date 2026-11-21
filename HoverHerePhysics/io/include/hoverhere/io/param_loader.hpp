#pragma once

#include <cstddef>
#include <filesystem>
#include <optional>
#include <string>
#include <string_view>
#include <vector>

#include "hoverhere/params/params.hpp"

// XML loaders of the parameter files used by the Unity application (stru_transmitter_and_helicopter, stru_scenery) and of
// the rotor mechanics geometry written by the Blender add-on. The element layout comes from the generated descriptor
// tables (schema/*.yaml); the semantics follow Helper.XmlMerging + XmlSerializer of the C# code:
//   * a parameter file is an overlay: values present in the file replace the values already in `params`, everything else
//     keeps its value (start from make_default_params() to get the Unity default overlay);
//   * of several elements with the same name only the first is used (the XmlSerializer reads every member once), vector
//     lists (positions_*) are replaced as a whole;
//   * elements that do not belong to a parameter are ignored (reported as warnings, except the documented Unity-only
//     elements), calculated parameters and the metadata elements of saved files (hint, min, max, ...) are ignored.
// Loading is transactional: when an error is reported the target is left unchanged.
namespace hoverhere::io {

enum class Severity { Info, Warning, Error };

struct Diagnostic {
    Severity severity;
    std::string message;
    std::string element_path;  // path of the XML element, e.g. "helicopter/fuselage/S_fxyz"
    int line;                  // 1-based line in the file, 0 if unknown
};

struct LoadReport {
    std::string source;
    std::vector<Diagnostic> diagnostics;
    std::size_t values_loaded = 0;  // parameters that were set from the file

    [[nodiscard]] std::size_t count(Severity severity) const noexcept;
    [[nodiscard]] bool ok() const noexcept { return count(Severity::Error) == 0; }
    // One line per diagnostic: "file(line): warning: message [element path]".
    [[nodiscard]] std::string format() const;
};

struct LoadOptions {
    bool report_range_violations = true;  // warn about loaded values outside the schema bounds
    bool unknown_elements_are_errors = false;
};

// Which kind of file `xml` is, judged by its root element; empty for other XML or malformed text.
[[nodiscard]] std::optional<params::ParamDocument> detect_document(std::string_view xml);

[[nodiscard]] LoadReport load_parameters_from_text(std::string_view xml, std::string_view source_name, params::ParamDocument document,
                                                   params::ModelParams& target, const LoadOptions& options = {});
[[nodiscard]] LoadReport load_parameters_file(const std::filesystem::path& file, params::ParamDocument document,
                                              params::ModelParams& target, const LoadOptions& options = {});

[[nodiscard]] LoadReport load_mainrotor_mechanics_from_text(std::string_view xml, std::string_view source_name,
                                                            params::mechanics::MainrotorMechanics& target,
                                                            const LoadOptions& options = {});
[[nodiscard]] LoadReport load_mainrotor_mechanics_file(const std::filesystem::path& file,
                                                       params::mechanics::MainrotorMechanics& target, const LoadOptions& options = {});
[[nodiscard]] LoadReport load_tailrotor_mechanics_from_text(std::string_view xml, std::string_view source_name,
                                                            params::mechanics::TailrotorMechanics& target,
                                                            const LoadOptions& options = {});
[[nodiscard]] LoadReport load_tailrotor_mechanics_file(const std::filesystem::path& file,
                                                       params::mechanics::TailrotorMechanics& target, const LoadOptions& options = {});

}  // namespace hoverhere::io
