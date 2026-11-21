#ifndef HOVERHERE_HEADLESS_C_API_CHECK_H
#define HOVERHERE_HEADLESS_C_API_CHECK_H

#ifdef __cplusplus
extern "C" {
#endif

/* Exercises hoverhere/sim_api.h from a C translation unit. Returns 0 on success, otherwise the number of the failed check. */
int hoverhere_c_api_check(void);

#ifdef __cplusplus
}
#endif

#endif /* HOVERHERE_HEADLESS_C_API_CHECK_H */
