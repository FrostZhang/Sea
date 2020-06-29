using System;

namespace com.ootii.Geometry
{
    /// <summary>
    /// Allows us to define how control points are constrained
    /// </summary>
    public class BezierConstraintType
    {
        public const int MIRROR = 0;

        public const int FREE = 1;

        public const int ALIGN = 2;
    }
}
