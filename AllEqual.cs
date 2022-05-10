using Unity.Burst;
using Unity.Burst.Intrinsics;

using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Avx;

[BurstCompile]
public struct simd
{
  
    [BurstCompile]
    public unsafe static bool AllEqual(float* p, int datacount)
    {
        int i = 0;
        float f = p[0];
        bool equal = true;

        if (IsAvx2Supported)                                
        {
            v256* p256 = (v256*)(void*)p; 

            // fill v256 with all the first value 
            v256 pop_0th = mm256_set1_ps(f);

            // compare 64 floats as 1111 or 0000 to 256 bits of 1
            v256 ones = mm256_setzero_si256();
            ones = mm256_cmpeq_epi32(ones, ones); 

            while((i < (datacount - 64)) & equal)
            {
                // shuffle 1111 into each nibble for each match
                v256 a = new v256(
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[0], pop_0th)),
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[1], pop_0th)),
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[2], pop_0th)),
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[3], pop_0th)),
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[4], pop_0th)),
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[5], pop_0th)),
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[6], pop_0th)),
                    mm256_movemask_epi8(mm256_cmpeq_epi32(p256[7], pop_0th))); 

                // equal if 256i == all ones 
                equal = ((uint)mm256_movemask_epi8(mm256_cmpeq_epi32(a, ones)) == 0xFFFFFFFF);

                p += 64;
                i += 64;                  
            }
            while ((i < (datacount & ~7)) & equal)
            {
                v256 eq = mm256_cmpeq_epi32(((v256*)(void*)p)[0], pop_0th);
                equal = (uint)mm256_movemask_epi8(eq) == 0xffffffff;
                p += 8;
                i += 8;
            }
        }
        // fallback for old stuff
        while ((i < datacount) & equal)
        {
            if (f != p[0]) return false;
            p++;
            i++;
        }

        return equal; 
    }
}
