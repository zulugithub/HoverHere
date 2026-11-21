#ifndef HOVERHERE_SIM_API_H
#define HOVERHERE_SIM_API_H

/* C API of the HoverHere physics (hoverhere.dll / libhoverhere.so). Everything crosses the boundary as plain structs of sim_types.h; all
 * vectors and quaternions are right handed (see there), the Unity wrapper HoverHereNative.cs converts at its boundary.
 *
 * Typical use (synchronous, one game thread):
 *   hh_sim_create -> hh_sim_load_xml (helicopter parameter file, optionally the scenery file and the two mechanics files) ->
 *   hh_sim_set_ground_mesh -> hh_sim_reset -> every frame: hh_sim_step(frame_dt, inputs, outputs).
 * Physics thread:
 *   ... hh_sim_reset -> hh_sim_thread_start -> every frame: hh_sim_post_inputs, hh_sim_read_outputs (both lock free).
 *
 * Thread safety: one hh_sim handle is used by one thread at a time, except that hh_sim_post_inputs and hh_sim_read_outputs may be called
 * from the game thread while the physics thread runs. Strings are UTF-8. Every function catches C++ exceptions and returns
 * HH_ERROR_INTERNAL; hh_sim_get_message returns the text of the last error or warning of the handle. */

#include <stdint.h>

#include "hoverhere/sim_types.h"

#if defined(_WIN32) || defined(__CYGWIN__)
#  if defined(HOVERHERE_C_API_BUILD)
#    define HH_API __declspec(dllexport)
#  else
#    define HH_API __declspec(dllimport)
#  endif
#elif defined(__GNUC__) || defined(__clang__)
#  define HH_API __attribute__((visibility("default")))
#else
#  define HH_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Incremented on every incompatible change of function signatures or struct layouts. */
#define HH_ABI_VERSION 2u

typedef enum hh_result {
    HH_OK = 0,
    HH_ERROR_INVALID_ARGUMENT = 1,
    HH_ERROR_OUT_OF_MEMORY = 2,
    HH_ERROR_INTERNAL = 3,
    HH_ERROR_INVALID_STATE = 4,      /* e.g. step while the physics thread runs, or no model yet */
    HH_ERROR_IO = 5,                 /* a file could not be read */
    HH_ERROR_PARSE = 6,              /* the XML is malformed or has the wrong kind */
    HH_ERROR_MODEL = 7               /* the parameters do not describe a physical model */
} hh_result;

/* Output structs carry the caller's sizeof() in struct_size so the library can stay compatible with older callers. */
typedef struct hh_version_info {
    uint32_t struct_size;
    uint32_t abi_version;
    uint32_t major;
    uint32_t minor;
    uint32_t patch;
} hh_version_info;

/* Same numbers as the document kinds of the parameter schema. */
typedef enum hh_xml_kind {
    HH_XML_AUTO = -1,                /* by the root element of the file */
    HH_XML_HELICOPTER = 0,           /* <helicopter>_parameter_file.xml */
    HH_XML_SCENERY = 1,              /* scenery parameter file (gravity, air, wind) */
    HH_XML_MAINROTOR_MECHANICS = 3,  /* <helicopter>_mainrotor_mechanics.xml */
    HH_XML_TAILROTOR_MECHANICS = 4   /* <helicopter>_tailrotor_mechanics.xml */
} hh_xml_kind;

enum { HH_MESH_FLIP_WINDING = 1 };   /* flags of hh_sim_set_ground_mesh */

typedef struct hh_sim hh_sim;

HH_API uint32_t hh_get_abi_version(void);
HH_API hh_result hh_get_version(hh_version_info* out_info);

/* Fill the structs with the default values (and struct_size). */
HH_API void hh_sim_options_init(hh_sim_options* options);
HH_API void hh_reset_params_init(hh_reset_params* params);
HH_API void hh_sim_inputs_init(hh_sim_inputs* inputs);

HH_API hh_result hh_sim_create(hh_sim** out_sim);
HH_API void hh_sim_destroy(hh_sim* sim);

/* Text of the last error or warning (diagnostics of the XML loaders, model errors) of the handle. Returns the length of the text; copies
 * at most capacity - 1 characters and a terminating zero when buffer is not null. */
HH_API int32_t hh_sim_get_message(hh_sim* sim, char* buffer, int32_t capacity);

/* Parameter files. The files are overlays on the defaults, in the order they are loaded; a new helicopter starts with
 * hh_sim_clear_setup. The model is (re)built by the next hh_sim_reset. Warnings (unknown elements, out of range values) do not fail the
 * call but are available through hh_sim_get_message. */
HH_API hh_result hh_sim_clear_setup(hh_sim* sim);
HH_API hh_result hh_sim_load_xml(hh_sim* sim, const char* path, int32_t kind);
HH_API hh_result hh_sim_load_xml_text(hh_sim* sim, const char* xml, const char* source_name, int32_t kind);
HH_API hh_result hh_sim_set_options(hh_sim* sim, const hh_sim_options* options);

/* Ground mesh in the right handed world frame (y up): vertex_count * 3 floats and index_count indices (three per triangle). The data is
 * copied. Replacing the mesh while the physics thread runs is possible but takes the physics mutex (not real time safe). */
HH_API hh_result hh_sim_set_ground_mesh(hh_sim* sim, const float* vertices_xyz, uint32_t vertex_count, const uint32_t* indices,
                                        uint32_t index_count, uint32_t flags);
HH_API hh_result hh_sim_clear_ground_mesh(hh_sim* sim);

/* Builds the model from the loaded files when needed and places the helicopter. */
HH_API hh_result hh_sim_reset(hh_sim* sim, const hh_reset_params* params);

/* Synchronous stepping. hh_sim_step advances by a variable frame time (fixed steps of options.dt with the frame cut and substep clamp)
 * and returns the number of steps in out_steps (may be null); hh_sim_step_fixed runs exactly one step. out_outputs may be null. */
HH_API hh_result hh_sim_step(hh_sim* sim, double frame_dt, const hh_sim_inputs* inputs, hh_sim_outputs* out_outputs, int32_t* out_steps);
HH_API hh_result hh_sim_step_fixed(hh_sim* sim, const hh_sim_inputs* inputs, hh_sim_outputs* out_outputs);
HH_API hh_result hh_sim_get_outputs(hh_sim* sim, hh_sim_outputs* out_outputs);

/* The complete 4 x 10 BEMT grid of the last step; HH_ERROR_INVALID_STATE (and valid = 0) when the main rotor is not a BEMT rotor. */
HH_API hh_result hh_sim_get_bemt_telemetry(hh_sim* sim, hh_bemt_telemetry* out_telemetry);

/* Physics thread. */
HH_API hh_result hh_sim_thread_start(hh_sim* sim);
HH_API hh_result hh_sim_thread_stop(hh_sim* sim);
HH_API hh_result hh_sim_post_inputs(hh_sim* sim, const hh_sim_inputs* inputs);
/* HH_OK with the newest published state; HH_ERROR_INVALID_STATE while nothing has been published yet. */
HH_API hh_result hh_sim_read_outputs(hh_sim* sim, hh_sim_outputs* out_outputs);

/* Memory layout of the structs of sim_types.h as the C compiler sees it (sizeof and offsetof of every member), for the layout test of the
 * language bindings. Names are "sizeof:<struct>" and "offsetof:<struct>.<member>". */
HH_API int32_t hh_get_layout_count(void);
HH_API hh_result hh_get_layout_entry(int32_t index, const char** out_name, uint64_t* out_value);

#ifdef __cplusplus
}
#endif

#endif /* HOVERHERE_SIM_API_H */
