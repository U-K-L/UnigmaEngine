#ifndef FLUID_HELPERS_INCLUDED
#define FLUID_HELPERS_INCLUDED

//Fluid Particle struct.
struct Particle
{
    float4 force;
    float3 position;
    float3 lastPosition;
    float3 predictedPosition;
    float3 positionDelta;
    float3 velocity;
    float3 normal;
    float3 curl;
    float density;
    float lambda;
    float spring;
};

#endif