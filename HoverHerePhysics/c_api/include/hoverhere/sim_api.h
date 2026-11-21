#ifndef HOVERHERE_SIM_API_H
#define HOVERHERE_SIM_API_H

#include <stdint.h>

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
#define HH_ABI_VERSION 1u

typedef enum hh_result {
    HH_OK = 0,
    HH_ERROR_INVALID_ARGUMENT = 1,
    HH_ERROR_OUT_OF_MEMORY = 2,
    HH_ERROR_INTERNAL = 3
} hh_result;

/* Output structs carry the caller's sizeof() in struct_size so the library can stay compatible with older callers. */
typedef struct hh_version_info {
    uint32_t struct_size;
    uint32_t abi_version;
    uint32_t major;
    uint32_t minor;
    uint32_t patch;
} hh_version_info;

typedef struct hh_sim hh_sim;

HH_API uint32_t hh_get_abi_version(void);
HH_API hh_result hh_get_version(hh_version_info* out_info);

HH_API hh_result hh_sim_create(hh_sim** out_sim);
HH_API void hh_sim_destroy(hh_sim* sim);

#ifdef __cplusplus
}
#endif

#endif /* HOVERHERE_SIM_API_H */
