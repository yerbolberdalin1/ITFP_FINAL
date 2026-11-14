#include "deliverymath.h"

int32_t estimated_minutes(int32_t distance_metres, int32_t speed_kmh)
{
    if (distance_metres < 1 || distance_metres > 500000)
        return -1;

    if (speed_kmh < 1 || speed_kmh > 120)
        return -1;

    int32_t numerator = distance_metres * 60;
    int32_t denominator = speed_kmh * 1000;

    return (numerator + denominator - 1) / denominator;
}
