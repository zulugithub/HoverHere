// hoverhere_params_check: loads Unity parameter files with the C++ loader and reports everything that is wrong with them
// (unknown or misplaced elements, values outside the schema bounds, malformed numbers, missing geometry elements).
//
//   hoverhere_params_check [--strict] [--quiet] [--dump] <file-or-directory>...
//
// Directories are searched recursively for *.xml files; files with another root element are skipped. The exit status is 1
// when an error was reported (with --strict also for warnings). --dump prints the loaded values of every file in the
// format of the golden files of the C# reference tool.
#include <algorithm>
#include <cstdio>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <span>
#include <sstream>
#include <string>
#include <vector>

#include "hoverhere/io/param_loader.hpp"

namespace fs = std::filesystem;
namespace params = hoverhere::params;

namespace {

std::string read_text(const fs::path& file)
{
    std::ifstream in(file, std::ios::binary);
    std::ostringstream buffer;
    buffer << in.rdbuf();
    return buffer.str();
}

void dump_values(const void* root, const std::span<const params::ParamDescriptor> table, const params::ParamDocument document)
{
    for (const bool calculated : {false, true}) {
        std::printf("%s\n", calculated ? "@calculated" : "@input");
        for (const params::ParamDescriptor& d : table) {
            if (d.document != document || d.calculated != calculated) {
                continue;
            }
            std::printf("%.*s", static_cast<int>(d.xml_path.size()), d.xml_path.data());
            const std::size_t n = params::value_count(root, d);
            for (std::size_t i = 0; i < n; ++i) {
                std::printf("\t%.9g", params::read_component(root, d, i));
            }
            std::printf("\n");
        }
    }
}

struct Totals {
    int files = 0;
    int errors = 0;
    int warnings = 0;
};

void check_file(const fs::path& file, const bool quiet, const bool dump, Totals& totals)
{
    const std::string text = read_text(file);
    const auto document = hoverhere::io::detect_document(text);
    if (!document) {
        if (!quiet) {
            std::printf("%s: skipped (not a parameter or mechanics file)\n", file.string().c_str());
        }
        return;
    }
    hoverhere::io::LoadReport report;
    if (*document == params::ParamDocument::MainrotorMechanics) {
        auto target = params::mechanics::make_default_mainrotor_mechanics();
        report = hoverhere::io::load_mainrotor_mechanics_from_text(text, file.string(), target);
        if (dump) {
            dump_values(&target, params::mechanics::mainrotor_mechanics_descriptors(), *document);
        }
    } else if (*document == params::ParamDocument::TailrotorMechanics) {
        auto target = params::mechanics::make_default_tailrotor_mechanics();
        report = hoverhere::io::load_tailrotor_mechanics_from_text(text, file.string(), target);
        if (dump) {
            dump_values(&target, params::mechanics::tailrotor_mechanics_descriptors(), *document);
        }
    } else {
        auto target = params::make_default_params();
        report = hoverhere::io::load_parameters_from_text(text, file.string(), *document, target);
        params::update_calculated(target);
        if (dump) {
            dump_values(&target, params::model_param_descriptors(), *document);
        }
    }
    ++totals.files;
    totals.errors += static_cast<int>(report.count(hoverhere::io::Severity::Error));
    totals.warnings += static_cast<int>(report.count(hoverhere::io::Severity::Warning));
    if (!report.diagnostics.empty()) {
        std::printf("%s", report.format().c_str());
    } else if (!quiet) {
        std::printf("%s: ok (%zu values)\n", file.string().c_str(), report.values_loaded);
    }
}

}  // namespace

int main(const int argc, char** const argv)
{
    bool strict = false;
    bool quiet = false;
    bool dump = false;
    const char* blob_path = nullptr;
    std::vector<fs::path> inputs;
    for (int i = 1; i < argc; ++i) {
        if (std::strcmp(argv[i], "--strict") == 0) {
            strict = true;
        } else if (std::strcmp(argv[i], "--quiet") == 0) {
            quiet = true;
        } else if (std::strcmp(argv[i], "--dump") == 0) {
            dump = true;
        } else if (std::strcmp(argv[i], "--write-default-blob") == 0 && i + 1 < argc) {
            blob_path = argv[++i];
        } else {
            inputs.emplace_back(argv[i]);
        }
    }
    if (blob_path != nullptr) {
        // the raw bytes of the default parameters, read back by the C# layout check (bindings/csharp)
        auto defaults = params::make_default_params();
        params::update_calculated(defaults);
        std::ofstream out(blob_path, std::ios::binary);
        out.write(reinterpret_cast<const char*>(&defaults), static_cast<std::streamsize>(sizeof defaults));
        if (!out) {
            std::fprintf(stderr, "cannot write %s\n", blob_path);
            return 1;
        }
        if (inputs.empty()) {
            return 0;
        }
    }
    if (inputs.empty()) {
        std::fprintf(stderr, "usage: hoverhere_params_check [--strict] [--quiet] [--dump] [--write-default-blob <file>] <file-or-directory>...\n");
        return 2;
    }

    Totals totals;
    for (const fs::path& input : inputs) {
        if (fs::is_directory(input)) {
            std::vector<fs::path> files;
            for (const auto& entry : fs::recursive_directory_iterator(input)) {
                if (entry.is_regular_file() && entry.path().extension() == ".xml") {
                    files.push_back(entry.path());
                }
            }
            std::sort(files.begin(), files.end());
            for (const fs::path& file : files) {
                check_file(file, true, dump, totals);
            }
        } else {
            check_file(input, quiet, dump, totals);
        }
    }
    std::printf("%d file(s) checked: %d error(s), %d warning(s)\n", totals.files, totals.errors, totals.warnings);
    return totals.errors > 0 || (strict && totals.warnings > 0) ? 1 : 0;
}
