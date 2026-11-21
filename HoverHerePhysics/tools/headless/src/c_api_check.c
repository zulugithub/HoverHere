#include "c_api_check.h"

#include <math.h>
#include <string.h>

#include "hoverhere/sim_api.h"

/* The structs have to be plain data of naturally aligned 4 and 8 byte members without padding (the C# mirror structs rely on it). */
_Static_assert(sizeof(hh_sim_options) % 8 == 0, "hh_sim_options is padded");
_Static_assert(sizeof(hh_sim_inputs) % 8 == 0, "hh_sim_inputs is padded");
_Static_assert(sizeof(hh_contact) % 8 == 0, "hh_contact is padded");
_Static_assert(sizeof(hh_sim_outputs) % 8 == 0, "hh_sim_outputs is padded");
_Static_assert(sizeof(hh_bemt_telemetry) % 8 == 0, "hh_bemt_telemetry is padded");

int hoverhere_c_api_check(void)
{
    hh_version_info info;
    hh_sim* sim = 0;
    hh_sim_options options;
    hh_reset_params reset;
    hh_sim_inputs inputs;
    hh_sim_outputs outputs;
    hh_bemt_telemetry telemetry;
    int32_t steps = -1;
    int i;
    const char* name = 0;
    uint64_t value = 0;

    if (hh_get_abi_version() != HH_ABI_VERSION) {
        return 1;
    }

    info.struct_size = (uint32_t)sizeof(info);
    info.abi_version = 0;
    info.major = 0;
    info.minor = 0;
    info.patch = 0;
    if (hh_get_version(&info) != HH_OK || info.abi_version != HH_ABI_VERSION) {
        return 2;
    }

    if (hh_get_version(0) != HH_ERROR_INVALID_ARGUMENT) {
        return 3;
    }
    if (hh_sim_create(0) != HH_ERROR_INVALID_ARGUMENT) {
        return 4;
    }

    if (hh_sim_create(&sim) != HH_OK || sim == 0) {
        return 5;
    }

    /* defaults and argument checks */
    hh_sim_options_init(&options);
    hh_reset_params_init(&reset);
    hh_sim_inputs_init(&inputs);
    memset(&outputs, 0, sizeof(outputs));
    outputs.struct_size = (uint32_t)sizeof(outputs);
    memset(&telemetry, 0, sizeof(telemetry));
    telemetry.struct_size = (uint32_t)sizeof(telemetry);
    if (options.struct_size != sizeof(options) || options.dt != 0.001 || inputs.motor_enabled != 1 || reset.rotor_spooled_up != 1) {
        return 6;
    }
    if (hh_sim_step_fixed(sim, &inputs, &outputs) != HH_ERROR_INVALID_STATE) { /* no model yet */
        return 7;
    }
    if (hh_sim_step(sim, 0.01, 0, &outputs, 0) != HH_ERROR_INVALID_ARGUMENT) {
        return 8;
    }
    inputs.struct_size = 4;
    if (hh_sim_step_fixed(sim, &inputs, &outputs) != HH_ERROR_INVALID_ARGUMENT) { /* struct too small */
        return 9;
    }
    hh_sim_inputs_init(&inputs);
    if (hh_sim_load_xml(sim, "this/file/does/not/exist.xml", HH_XML_AUTO) != HH_ERROR_IO) {
        return 10;
    }
    if (hh_sim_load_xml_text(sim, "<not_a_hoverhere_file/>", "text", HH_XML_AUTO) != HH_ERROR_PARSE) {
        return 11;
    }

    /* default helicopter: build, reset, run 20 ms */
    if (hh_sim_reset(sim, &reset) != HH_OK) {
        return 12;
    }
    if (hh_sim_step(sim, 0.0205, &inputs, &outputs, &steps) != HH_OK || steps != 20) {
        return 13;
    }
    if (!isfinite(outputs.position_O[1]) || !isfinite(outputs.mainrotor_speed) || outputs.step_count != 20 ||
        (outputs.flags & HH_FLAG_DIVERGED) != 0) {
        return 14;
    }
    if (hh_sim_get_outputs(sim, &outputs) != HH_OK) {
        return 15;
    }
    if (hh_sim_get_bemt_telemetry(sim, &telemetry) != HH_OK || telemetry.valid != 1) { /* BEMT is the default rotor model */
        return 16;
    }

    /* ground mesh: a quad below the helicopter */
    {
        const float vertices[12] = {-5.f, 0.f, -5.f, 5.f, 0.f, -5.f, 5.f, 0.f, 5.f, -5.f, 0.f, 5.f};
        const uint32_t indices[6] = {0, 3, 1, 1, 3, 2};
        const uint32_t bad_indices[3] = {0, 1, 99};
        if (hh_sim_set_ground_mesh(sim, vertices, 4, indices, 6, 0) != HH_OK) {
            return 17;
        }
        if (hh_sim_set_ground_mesh(sim, vertices, 4, bad_indices, 3, 0) != HH_ERROR_INVALID_ARGUMENT) {
            return 18;
        }
    }

    /* layout table */
    if (hh_get_layout_count() < 50 || hh_get_layout_entry(0, &name, &value) != HH_OK || name == 0 ||
        strcmp(name, "sizeof:hh_sim_options") != 0 || value != sizeof(hh_sim_options)) {
        return 19;
    }
    for (i = 0; i < hh_get_layout_count(); ++i) {
        if (hh_get_layout_entry(i, &name, &value) != HH_OK || name == 0 || strchr(name, ':') == 0) {
            return 20;
        }
    }
    if (hh_get_layout_entry(-1, &name, &value) != HH_ERROR_INVALID_ARGUMENT || hh_get_layout_entry(hh_get_layout_count(), &name, &value) != HH_ERROR_INVALID_ARGUMENT) {
        return 21;
    }

    /* physics thread */
    if (hh_sim_thread_start(sim) != HH_OK) {
        return 22;
    }
    if (hh_sim_step_fixed(sim, &inputs, &outputs) != HH_ERROR_INVALID_STATE) {
        return 23;
    }
    if (hh_sim_post_inputs(sim, &inputs) != HH_OK) {
        return 24;
    }
    if (hh_sim_thread_stop(sim) != HH_OK) {
        return 25;
    }

    hh_sim_destroy(sim);
    hh_sim_destroy(0);

    return 0;
}
