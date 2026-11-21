#include "hoverhere/sim_api.h"

#include <cstddef>
#include <cstring>
#include <fstream>
#include <iterator>
#include <new>
#include <optional>
#include <span>
#include <sstream>
#include <string>

#include "hoverhere/io/param_loader.hpp"
#include "hoverhere/simulation/helicopter_sim.hpp"
#include "hoverhere/version.hpp"

namespace sim = hoverhere::simulation;
namespace io = hoverhere::io;
namespace hp = hoverhere::params;

struct hh_sim {
    sim::HelicopterSim sim;
    sim::SimSetup setup;
    hh_sim_options options = sim::default_sim_options();
    bool dirty = true;
    std::string message;
};

namespace {

// Runs `body` and maps exceptions to error codes.
template <class Body>
hh_result guarded(Body&& body) noexcept
{
    try {
        return body();
    } catch (const std::bad_alloc&) {
        return HH_ERROR_OUT_OF_MEMORY;
    } catch (...) {
        return HH_ERROR_INTERNAL;
    }
}

[[nodiscard]] bool size_ok(const uint32_t size, const std::size_t needed) noexcept
{
    return size >= needed;
}

// Builds the model when needed. Returns HH_OK when the simulation is configured.
hh_result ensure_configured(hh_sim& s)
{
    if (s.dirty || !s.sim.configured()) {
        std::string error;
        if (!s.sim.configure(s.setup, s.options, &error)) {
            s.message = "model error: " + error;
            return HH_ERROR_MODEL;
        }
        s.dirty = false;
    }
    return HH_OK;
}

hh_result load_text(hh_sim& s, const std::string& xml, const std::string& source, const int32_t kind)
{
    std::optional<hp::ParamDocument> document;
    if (kind == HH_XML_AUTO) {
        document = io::detect_document(xml);
        if (!document) {
            s.message = source + ": not a HoverHere parameter file (unknown root element)";
            return HH_ERROR_PARSE;
        }
    } else if (kind == HH_XML_HELICOPTER || kind == HH_XML_SCENERY || kind == HH_XML_MAINROTOR_MECHANICS || kind == HH_XML_TAILROTOR_MECHANICS) {
        document = static_cast<hp::ParamDocument>(kind);
    } else {
        s.message = "unknown document kind";
        return HH_ERROR_INVALID_ARGUMENT;
    }

    io::LoadReport report;
    switch (*document) {
    case hp::ParamDocument::MainrotorMechanics: {
        auto target = hp::mechanics::make_default_mainrotor_mechanics();
        report = io::load_mainrotor_mechanics_from_text(xml, source, target);
        if (report.ok()) {
            s.setup.mainrotor_mechanics = target;
        }
        break;
    }
    case hp::ParamDocument::TailrotorMechanics: {
        auto target = hp::mechanics::make_default_tailrotor_mechanics();
        report = io::load_tailrotor_mechanics_from_text(xml, source, target);
        if (report.ok()) {
            s.setup.tailrotor_mechanics = target;
        }
        break;
    }
    default: {
        hp::ModelParams target = s.setup.params;
        report = io::load_parameters_from_text(xml, source, *document, target);
        if (report.ok()) {
            s.setup.params = target;
        }
        break;
    }
    }
    s.message = report.format();
    if (!report.ok()) {
        return HH_ERROR_PARSE;
    }
    s.dirty = true;
    return HH_OK;
}

constexpr std::uint64_t as_u64(const std::uint64_t value) noexcept
{
    return value;
}

#define HH_LAYOUT_SIZE(type) {"sizeof:" #type, as_u64(sizeof(type))},
#define HH_LAYOUT_OFFSET(type, member) {"offsetof:" #type "." #member, as_u64(offsetof(type, member))},

struct LayoutEntry {
    const char* name;
    std::uint64_t value;
};

constexpr LayoutEntry kLayout[] = {
#include "layout_table.inc"
};

}  // namespace

extern "C" {

uint32_t hh_get_abi_version(void)
{
    return HH_ABI_VERSION;
}

hh_result hh_get_version(hh_version_info* const out_info)
{
    if (out_info == nullptr || out_info->struct_size < sizeof(hh_version_info)) {
        return HH_ERROR_INVALID_ARGUMENT;
    }

    const hoverhere::Version version = hoverhere::version();
    out_info->abi_version = HH_ABI_VERSION;
    out_info->major = static_cast<uint32_t>(version.major);
    out_info->minor = static_cast<uint32_t>(version.minor);
    out_info->patch = static_cast<uint32_t>(version.patch);
    return HH_OK;
}

void hh_sim_options_init(hh_sim_options* const options)
{
    if (options != nullptr) {
        *options = sim::default_sim_options();
    }
}

void hh_reset_params_init(hh_reset_params* const params)
{
    if (params != nullptr) {
        *params = sim::default_reset_params();
    }
}

void hh_sim_inputs_init(hh_sim_inputs* const inputs)
{
    if (inputs != nullptr) {
        *inputs = sim::default_sim_inputs();
    }
}

hh_result hh_sim_create(hh_sim** const out_sim)
{
    if (out_sim == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    *out_sim = nullptr;
    return guarded([&] {
        *out_sim = new (std::nothrow) hh_sim{};
        return *out_sim != nullptr ? HH_OK : HH_ERROR_OUT_OF_MEMORY;
    });
}

void hh_sim_destroy(hh_sim* const sim)
{
    try {
        delete sim;
    } catch (...) {
    }
}

int32_t hh_sim_get_message(hh_sim* const sim, char* const buffer, const int32_t capacity)
{
    if (sim == nullptr) {
        return 0;
    }
    const auto length = static_cast<int32_t>(sim->message.size());
    if (buffer != nullptr && capacity > 0) {
        const int32_t n = length < capacity - 1 ? length : capacity - 1;
        std::memcpy(buffer, sim->message.data(), static_cast<std::size_t>(n));
        buffer[n] = '\0';
    }
    return length;
}

hh_result hh_sim_clear_setup(hh_sim* const sim)
{
    if (sim == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        sim->setup = sim::SimSetup{};
        sim->dirty = true;
        sim->message.clear();
        return HH_OK;
    });
}

hh_result hh_sim_load_xml(hh_sim* const sim, const char* const path, const int32_t kind)
{
    if (sim == nullptr || path == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        std::ifstream file(path, std::ios::binary);
        if (!file) {
            sim->message = std::string("cannot open ") + path;
            return HH_ERROR_IO;
        }
        std::ostringstream text;
        text << file.rdbuf();
        return load_text(*sim, text.str(), path, kind);
    });
}

hh_result hh_sim_load_xml_text(hh_sim* const sim, const char* const xml, const char* const source_name, const int32_t kind)
{
    if (sim == nullptr || xml == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] { return load_text(*sim, xml, source_name != nullptr ? source_name : "xml", kind); });
}

hh_result hh_sim_set_options(hh_sim* const sim, const hh_sim_options* const options)
{
    if (sim == nullptr || options == nullptr || !size_ok(options->struct_size, sizeof(hh_sim_options))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        sim->options = *options;
        sim->dirty = true;
        return HH_OK;
    });
}

hh_result hh_sim_set_ground_mesh(hh_sim* const sim, const float* const vertices_xyz, const uint32_t vertex_count, const uint32_t* const indices,
                                 const uint32_t index_count, const uint32_t flags)
{
    if (sim == nullptr || vertices_xyz == nullptr || indices == nullptr || vertex_count == 0 || index_count < 3 || index_count % 3 != 0) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        const bool ok = sim->sim.set_ground_mesh(std::span<const float>(vertices_xyz, static_cast<std::size_t>(vertex_count) * 3),
                                                 std::span<const std::uint32_t>(indices, index_count), (flags & HH_MESH_FLIP_WINDING) != 0);
        if (!ok) {
            sim->message = "the ground mesh is malformed (index out of range, non finite vertex or only degenerate triangles)";
        }
        return ok ? HH_OK : HH_ERROR_INVALID_ARGUMENT;
    });
}

hh_result hh_sim_clear_ground_mesh(hh_sim* const sim)
{
    if (sim == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        sim->sim.clear_ground_mesh();
        return HH_OK;
    });
}

hh_result hh_sim_reset(hh_sim* const sim, const hh_reset_params* const params)
{
    if (sim == nullptr || params == nullptr || !size_ok(params->struct_size, sizeof(hh_reset_params))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        const hh_result configured = ensure_configured(*sim);
        if (configured != HH_OK) {
            return configured;
        }
        sim->sim.reset(*params);
        return HH_OK;
    });
}

hh_result hh_sim_step(hh_sim* const sim, const double frame_dt, const hh_sim_inputs* const inputs, hh_sim_outputs* const out_outputs,
                      int32_t* const out_steps)
{
    if (sim == nullptr || inputs == nullptr || !size_ok(inputs->struct_size, sizeof(hh_sim_inputs)) ||
        (out_outputs != nullptr && !size_ok(out_outputs->struct_size, sizeof(hh_sim_outputs)))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        if (!sim->sim.configured() || sim->sim.thread_running()) {
            return HH_ERROR_INVALID_STATE;
        }
        const int steps = sim->sim.advance(frame_dt, *inputs);
        if (out_steps != nullptr) {
            *out_steps = steps;
        }
        if (out_outputs != nullptr) {
            sim->sim.fill_outputs(*out_outputs);
        }
        return HH_OK;
    });
}

hh_result hh_sim_step_fixed(hh_sim* const sim, const hh_sim_inputs* const inputs, hh_sim_outputs* const out_outputs)
{
    if (sim == nullptr || inputs == nullptr || !size_ok(inputs->struct_size, sizeof(hh_sim_inputs)) ||
        (out_outputs != nullptr && !size_ok(out_outputs->struct_size, sizeof(hh_sim_outputs)))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        if (!sim->sim.configured() || sim->sim.thread_running()) {
            return HH_ERROR_INVALID_STATE;
        }
        sim->sim.step_fixed(*inputs);
        if (out_outputs != nullptr) {
            sim->sim.fill_outputs(*out_outputs);
        }
        return HH_OK;
    });
}

hh_result hh_sim_get_outputs(hh_sim* const sim, hh_sim_outputs* const out_outputs)
{
    if (sim == nullptr || out_outputs == nullptr || !size_ok(out_outputs->struct_size, sizeof(hh_sim_outputs))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        if (!sim->sim.configured()) {
            return HH_ERROR_INVALID_STATE;
        }
        sim->sim.fill_outputs(*out_outputs);
        return HH_OK;
    });
}

hh_result hh_sim_get_bemt_telemetry(hh_sim* const sim, hh_bemt_telemetry* const out_telemetry)
{
    if (sim == nullptr || out_telemetry == nullptr || !size_ok(out_telemetry->struct_size, sizeof(hh_bemt_telemetry))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] { return sim->sim.fill_bemt_telemetry(*out_telemetry) ? HH_OK : HH_ERROR_INVALID_STATE; });
}

hh_result hh_sim_thread_start(hh_sim* const sim)
{
    if (sim == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] { return sim->sim.start_thread() ? HH_OK : HH_ERROR_INVALID_STATE; });
}

hh_result hh_sim_thread_stop(hh_sim* const sim)
{
    if (sim == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        sim->sim.stop_thread();
        return HH_OK;
    });
}

hh_result hh_sim_post_inputs(hh_sim* const sim, const hh_sim_inputs* const inputs)
{
    if (sim == nullptr || inputs == nullptr || !size_ok(inputs->struct_size, sizeof(hh_sim_inputs))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] {
        sim->sim.post_inputs(*inputs);
        return HH_OK;
    });
}

hh_result hh_sim_read_outputs(hh_sim* const sim, hh_sim_outputs* const out_outputs)
{
    if (sim == nullptr || out_outputs == nullptr || !size_ok(out_outputs->struct_size, sizeof(hh_sim_outputs))) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    return guarded([&] { return sim->sim.read_outputs(*out_outputs) ? HH_OK : HH_ERROR_INVALID_STATE; });
}

int32_t hh_get_layout_count(void)
{
    return static_cast<int32_t>(sizeof(kLayout) / sizeof(kLayout[0]));
}

hh_result hh_get_layout_entry(const int32_t index, const char** const out_name, uint64_t* const out_value)
{
    if (out_name == nullptr || out_value == nullptr || index < 0 || index >= hh_get_layout_count()) {
        return HH_ERROR_INVALID_ARGUMENT;
    }
    *out_name = kLayout[index].name;
    *out_value = kLayout[index].value;
    return HH_OK;
}

}  // extern "C"
