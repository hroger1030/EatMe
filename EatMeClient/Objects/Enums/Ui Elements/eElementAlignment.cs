using System;

namespace EatMeClient
{
    [Flags]
    public enum eElementAlignment
    {
        VerticalTop = 1,
        VerticalMiddle = 2,
        VerticalBottom = 4,
        HorizontalLeft = 8,
        HorizontalCenter = 16,
        HorizontalRight = 32,
    }
}
