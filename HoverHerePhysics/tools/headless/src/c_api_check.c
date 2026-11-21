#include "c_api_check.h"

#include "hoverhere/sim_api.h"

int hoverhere_c_api_check(void)
{
    hh_version_info info;
    hh_sim* sim = 0;

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
    hh_sim_destroy(sim);
    hh_sim_destroy(0);

    return 0;
}
