# Unity Burst Intrinsics 

Ive updated some functions i often use using simd instructions in Unity with burst and thought to share it here as it might be handy for reference for others. Ill be adding to this as i go.

The code targets AVX2 capable processors and has fallbacks for old cpu's.

[Test if all floats in array are equal](AllEqual.cs) 

Check if all floats in the array are equal to eachother using avx2 comparers unrolled to test 64 floats each loop

```CSharp
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
        }
```

[Check if all pointers in the array point to data that is equally spaced](EquallySpaced.cs)

Check if all pointers in the array point to memory blobs that all start with the same distance from eachother and ifso returns the stride in bytes.
```CSharp
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
``` 


## mm256_ utilities: 

Basic helpers for handling and loading data

```CSharp
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static public Tout ReInterpret<Tin, Tout>(Tin value) where Tin : unmanaged where Tout : unmanaged
    {
        unsafe
        {
            return ((Tout*)(void*)&value)[0];
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static v256 mm256_stride2_loadeven(int* p)
    {
        if (IsAvx2Supported)
        {
            v256 x_lo = mm256_load_si256(&p[0]);
            v256 x_hi = mm256_load_si256(&p[7]);
            return mm256_permutevar8x32_epi32(mm256_blend_epi32(x_lo, x_hi, 0b10101010), mm256_set_epi32(7, 5, 3, 1, 6, 4, 2, 0));
        }
        else
        {
            return new v256(p[0], p[2], p[4], p[6], p[8], p[10], p[12], p[14]);
        }
    }
``` 


