using Unity.Burst;
using Unity.Burst.Intrinsics;

using static Unity.Burst.Intrinsics.X86.Avx2;
using static Unity.Burst.Intrinsics.X86.Avx;
using System.Runtime.CompilerServices;

unsafe public partial struct simd
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    unsafe static public Tout* ReCast<Tin, Tout>(Tin* value) where Tin : unmanaged where Tout : unmanaged
    {
        return (Tout*)(void*)value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static public Tout ReInterpret<Tin, Tout>(Tin value) where Tin : unmanaged where Tout : unmanaged
    {
        unsafe
        {
            return ((Tout*)(void*)&value)[0];
        }
    }

    /// <summary>
    /// load even indices from p into v256
    /// </summary>
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


}