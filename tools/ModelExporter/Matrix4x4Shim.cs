// Minimal Matrix4x4/Vector4 shim so HeldModelAlignment.cs -- otherwise untestable outside the game,
// since it is the one Magenheim.Runtime file that touches Matrix4x4 -- can compile and run against
// ModelAssetTests. Implements exactly the members that file uses: column set/get, determinant of the
// 3x3 submatrix (the only part HeldModelAlignment ever writes), and a standard trace-based
// matrix-to-quaternion extraction (Shepperd's method), which is correct for any proper rotation
// matrix, not just the signed permutations this file happens to build.
using System;
using NQuat = System.Numerics.Quaternion;

namespace UnityEngine;

public struct Vector4
{
    public float x, y, z, w;
    public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    public static Vector4 zero => new(0, 0, 0, 0);
    public float this[int index]
    {
        get => index switch { 0 => x, 1 => y, 2 => z, 3 => w, _ => throw new IndexOutOfRangeException() };
        set { switch (index) { case 0: x = value; break; case 1: y = value; break; case 2: z = value; break; case 3: w = value; break; default: throw new IndexOutOfRangeException(); } }
    }
}

public struct Matrix4x4
{
    private float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;

    public void SetColumn(int column, Vector4 value)
    {
        switch (column)
        {
            case 0: m00 = value.x; m10 = value.y; m20 = value.z; m30 = value.w; break;
            case 1: m01 = value.x; m11 = value.y; m21 = value.z; m31 = value.w; break;
            case 2: m02 = value.x; m12 = value.y; m22 = value.z; m32 = value.w; break;
            case 3: m03 = value.x; m13 = value.y; m23 = value.z; m33 = value.w; break;
            default: throw new IndexOutOfRangeException();
        }
    }

    public Vector4 GetColumn(int column) => column switch
    {
        0 => new Vector4(m00, m10, m20, m30),
        1 => new Vector4(m01, m11, m21, m31),
        2 => new Vector4(m02, m12, m22, m32),
        3 => new Vector4(m03, m13, m23, m33),
        _ => throw new IndexOutOfRangeException(),
    };

    /// <summary>Determinant of the upper-left 3x3 -- the only part HeldModelAlignment ever writes.</summary>
    public float determinant =>
        m00 * (m11 * m22 - m12 * m21) - m01 * (m10 * m22 - m12 * m20) + m02 * (m10 * m21 - m11 * m20);

    /// <summary>Trace-based (Shepperd's) matrix-to-quaternion conversion, valid for any proper rotation.</summary>
    public Quaternion rotation
    {
        get
        {
            var trace = m00 + m11 + m22;
            float qw, qx, qy, qz;
            if (trace > 0f)
            {
                var s = MathF.Sqrt(trace + 1f) * 2f;
                qw = 0.25f * s; qx = (m21 - m12) / s; qy = (m02 - m20) / s; qz = (m10 - m01) / s;
            }
            else if (m00 > m11 && m00 > m22)
            {
                var s = MathF.Sqrt(1f + m00 - m11 - m22) * 2f;
                qw = (m21 - m12) / s; qx = 0.25f * s; qy = (m01 + m10) / s; qz = (m02 + m20) / s;
            }
            else if (m11 > m22)
            {
                var s = MathF.Sqrt(1f + m11 - m00 - m22) * 2f;
                qw = (m02 - m20) / s; qx = (m01 + m10) / s; qy = 0.25f * s; qz = (m12 + m21) / s;
            }
            else
            {
                var s = MathF.Sqrt(1f + m22 - m00 - m11) * 2f;
                qw = (m10 - m01) / s; qx = (m02 + m20) / s; qy = (m12 + m21) / s; qz = 0.25f * s;
            }
            return new Quaternion(new NQuat(qx, qy, qz, qw));
        }
    }
}
