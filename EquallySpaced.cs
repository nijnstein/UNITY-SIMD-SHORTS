using Unity.Burst;
using Unity.Burst.Intrinsics;

using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Avx;

unsafe public partial struct simd
{

    public static bool AllEquallySpaced(void** data, int datacount, out int rowsize)
    {
        // for performance reasons: only check the first as a long substraction
        rowsize = (int)(((ulong*)data)[1] - ((ulong*)data)[0]);

        // only say indices are aligned when below some max value of spacing
        // by setting max to int32 (-1 bit) we can interpret the lsw of the 64bit 
        // pointer as a signed integer, speeding up the mainloop greatly as we can work
        // on 32bit integers instead of 64 bit uints 
        bool data_is_aligned = (rowsize % 4) == 0 && rowsize >= 0 && rowsize < int.MaxValue;

        // meeting these conditions we can check only the lower int of each long -> way faster 
        int i = 0;
        int* p = (int*)data;

        // if building for unity we might be able to use intrinsics 
        if (IsAvx2Supported)
        {
            v256 cmp = mm256_set1_epi32(rowsize);
            v256 perm = mm256_set_epi32(1, 2, 3, 4, 5, 6, 7, 0);

            unchecked
            {
                while (data_is_aligned & (i < (datacount - 16)))
                {
                    // load only 1 vector and shift it saving 1 memory gathering operation
                    v256 v1 = mm256_stride2_loadeven(p);

                    // permute vector 1 index to left
                    v256 v2 = mm256_permutevar8x32_epi32(v1, perm);

                    i += 16;
                    p += 16;

                    // fill last with value from next row 
                    v2 = mm256_insert_epi32(v2, p[0], 7);

                    data_is_aligned = 0b11111111_11111111_11111111_11111111 == ReInterpret<int, uint>(
                         mm256_movemask_epi8(
                             mm256_cmpeq_epi32(
                                 mm256_sub_epi32(v2, v1),
                                 cmp)));
                }
            }
        }

        // .net standard likes unrolled pointer walk loops a lot according to my benchmarks 
        unchecked
        {
            while (data_is_aligned & (i < (datacount - 8)))
            {
                data_is_aligned =
                    rowsize == (int)(p[2] - p[0])
                    &&
                    rowsize == (int)(p[4] - p[2])
                    &&
                    rowsize == (int)(p[6] - p[4])
                    &&
                    rowsize == (int)(p[8] - p[6]);

                i += 8;
                p += 8;
            }
            while (data_is_aligned & (i < (datacount - 2)))
            {
                data_is_aligned = rowsize == (int)(p[2] - p[0]);
                i += 2;
                p += 2;
            }
        }
        return data_is_aligned;
    }

    
}
