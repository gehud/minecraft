#ifndef UNPACK_VERTEX_INCLUDED
#define UNPACK_VERTEX_INCLUDED

void UnpackVertex_float(float2 Data, out float3 Position, out float2 UV, out float4 Light) {
    // Mask = 2 ^ Bit - 1

    uint aData = asuint(Data.x);
    uint bData = asuint(Data.y);
    
    // A
    uint yBit = 5u;
    uint zBit = 5u;
    uint iBit = 9u;

    uint yMask = 31u;
    uint zMask = 31u;
    uint iMask = 511u;

    uint ziBit = zBit + iBit;
    uint yziBit = yBit + ziBit;

    uint x = uint(aData >> yziBit);
    uint y = uint((aData >> ziBit) & yMask);
    uint z = uint((aData >> iBit) & zMask);
    uint i = uint(aData & iMask);

    Position = float3(x, y, z);

    uint u = i % 17;
    uint v = i / 17;
    float uvStep = 16.0 / 256.0;
    UV = float2(u * uvStep, v * uvStep);

    // B
    uint gBit = 6u;
    uint bBit = 6u;
    uint sBit = 6u;

    uint gMask = 63u;
    uint bMask = 63u;
    uint sMask = 63u;

    uint bsBit = bBit + sBit;
    uint gbsBit = gBit + bsBit;

    float r = int(bData >> gbsBit) / 4.0 / 16.0;
    float g = int((bData >> bsBit) & gMask) / 4.0 / 16.0;
    float b = int((bData >> sBit) & bMask) / 4.0 / 16.0;
    float s = int(bData & sMask) / 4.0 / 16.0;
    Light = float4(r, g, b, s);
}

#endif