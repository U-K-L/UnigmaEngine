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
    float4x4 anisotropicTRS;
    float4 mean;
    int phase;
    int type;
    float kelvin;
};

float GetMass(int type)
{
    float masses[2] = { 0.001, 0.1575 };
    return masses[type];
}

float GetRest(int type)
{
    float rests[2] = { 1, 25 };
    return rests[type];
}

float GetSize(int type)
{
    float sizes[2] = { 0.030552188, 0.08552188 };
    return sizes[type];
}

float3 GetForces(int type)
{
    float3 forces[2];
    forces[0] = 0;
    forces[1] = float3(0, -9.8f, 0);
    
    return forces[type];
}

float GetRadius(int type)
{
    float radiuses[2] = { 0.0838125, 0.17525f };
    
    return radiuses[type];
}

int3 GetCellVectorField(float3 position, float3 _BoxSize, float3 _Resolution)
{
    float3 spacing = (_BoxSize / (_Resolution - 1));
    float3 halfContainerSize = _BoxSize / 2.0f;
    
    int3 index = ((position + halfContainerSize) / spacing);
    /*
    float3 shiftToCenter = position + halfContainerSize;
    float3 normalized = (shiftToCenter / _BoxSize);
        */

    
    return index;
}

inline uint HashCellVectorField(in int3 cellIndex, float3 _Resolution)
{   
    return cellIndex.x * _Resolution * _Resolution + cellIndex.y * _Resolution + cellIndex.z;
}

#endif