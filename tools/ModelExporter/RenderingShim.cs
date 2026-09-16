namespace UnityEngine.Rendering;

public enum BlendMode
{
    Zero = 0,
    One = 1,
    SrcColor = 3,
    OneMinusSrcColor = 4,
    SrcAlpha = 5,
    OneMinusSrcAlpha = 10
}

public enum ShadowCastingMode
{
    Off = 0,
    On = 1,
    TwoSided = 2,
    ShadowsOnly = 3
}

public enum IndexFormat { UInt16, UInt32 }
