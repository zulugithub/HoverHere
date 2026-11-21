#include "hoverhere/sim_api.h"

#include <new>

#include "hoverhere/version.hpp"

struct hh_sim {};

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

hh_result hh_sim_create(hh_sim** const out_sim)
{
    if (out_sim == nullptr) {
        return HH_ERROR_INVALID_ARGUMENT;
    }

    *out_sim = new (std::nothrow) hh_sim{};
    return *out_sim != nullptr ? HH_OK : HH_ERROR_OUT_OF_MEMORY;
}

void hh_sim_destroy(hh_sim* const sim)
{
    delete sim;
}

}  // extern "C"
