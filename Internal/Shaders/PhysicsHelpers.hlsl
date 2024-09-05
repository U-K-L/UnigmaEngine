#ifndef PHYSICS_HELPERS_INCLUDED
#define PHYSICS_HELPERS_INCLUDED

struct SpaceTimePoint
{
    float3 index;
    float3 position;
    float3 force;
    float kelvin;
    float tempVal;
    float conductivity;
    uint particlesCount;
};

struct PhysicsObject
{
    uint objectId;
    float3 position;
    float strength;
    float kelvin;
    float radius;

};

#endif