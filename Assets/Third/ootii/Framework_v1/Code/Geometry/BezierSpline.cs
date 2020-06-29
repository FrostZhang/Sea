using System;
using UnityEngine;
using com.ootii.Utilities.Debug;

namespace com.ootii.Geometry
{
    /// <summary>
    /// Bezier spine that allows us to render multiple curves in 3D space
    /// </summary>
    public class BezierSpline : MonoBehaviour
    {
        /// <summary>
        /// Tracks all the points on the curve
        /// </summary>
        [SerializeField]
        private Vector3[] mPoints = null;
        public Vector3[] Points
        {
            get { return mPoints; }
        }

        /// <summary>
        /// Constraints used to determine how we'll
        /// handle moving of tangent points of each point. We
        /// use curves + 1 as the number.
        /// </summary>
        [SerializeField]
        private int[] mControlConstraints = null;

        /// <summary>
        /// Number of segments to draw the curve in
        /// </summary>
        [SerializeField]
        private int mSegments = 10;
        public int Segments
        {
            get { return mSegments; }
            set { mSegments = value; }
        }

        /// <summary>
        /// Determines if the spline creates a loop
        /// </summary>
        [SerializeField]
        private bool mLoop = false;
        public bool Loop
        {
            get { return mLoop; }
            
            set 
            { 
                mLoop = value;
                if (mLoop)
                {
                    mControlConstraints[mControlConstraints.Length - 1] = mControlConstraints[0];
                    SetControlPoint(ControlPointCount - 1, GetControlPoint(0));
                }
            }
        }

        /// <summary>
        /// Total length of the line requires us to interpolate over the
        /// curves. We do this by walking the line.
        /// </summary>
        private float mLength = 0f;
        public float Length
        {
            get 
            {
                if (mLength == 0f) { CalculateCurveLengths(); }
                return mLength;  
            }
        }

        /// <summary>
        /// Returns the number of points on the curve
        /// </summary>
        public int ControlPointCount
        {
            get { return ((mPoints.Length - 1) / 3) + 1; }
        }

        /// <summary>
        /// Returns the number of curves that make up the spline
        /// </summary>
        public int CurveCount
        {
            get { return (mPoints.Length - 1) / 3; }
        }

        /// <summary>
        /// Track the individual curve lengths so we don't need to
        /// keep recalculating them.
        /// </summary>
        private float[] mCurveLengths = null;

        /// <summary>
        /// Run once the object has been initialized
        /// </summary>
        public void Awake()
        {
            CalculateCurveLengths();
        }

        /// <summary>
        /// Called when the developer uses the reset tool in the inspector
        /// </summary>
        public void Reset()
        {
            mLoop = false;
            mLength = 0f;

            // Create a default curve
            mPoints = new Vector3[] 
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
                new Vector3(0f, 0f, 2f),
                new Vector3(0f, 0f, 3f)
            };

            // Set the default curve length
            mCurveLengths = new float[] { 3f };

            // Set the default tangent constraints (one for start and one for end)
            mControlConstraints = new int[] { BezierConstraintType.MIRROR, BezierConstraintType.MIRROR };
        }

        /// <summary>
        /// Adds a new curve to the spline. Since the last element of the array
        /// is the first element in the new curve, we add 3.
        /// </summary>
        public void AddControlPoint()
        {
            if (mPoints == null) { Reset(); }

            int lIndex = mPoints.Length;
            Vector3 lLastPoint = mPoints[mPoints.Length - 1];

            Array.Resize(ref mPoints, lIndex + 3);
            Array.Resize(ref mCurveLengths, (mPoints.Length - 1) / 3);
            Array.Resize(ref mControlConstraints, mCurveLengths.Length + 1);

            lLastPoint.z += 1f;
            mPoints[lIndex++] = lLastPoint;

            lLastPoint.z += 1f;
            mPoints[lIndex++] = lLastPoint;

            lLastPoint.z += 1f;
            mPoints[lIndex++] = lLastPoint;

            // Copy the last control constraint
            mControlConstraints[mControlConstraints.Length - 1] = mControlConstraints[mControlConstraints.Length - 2];

            // If we're looping, connect the end
            if (mLoop)
            {
                mControlConstraints[mControlConstraints.Length - 1] = mControlConstraints[0];
                SetControlPoint(ControlPointCount - 1, GetControlPoint(0));
            }

            // Recalculate the curve lengths
            CalculateCurveLengths();
        }

        /// <summary>
        /// Adds a new curve to the spline. Since the last element of the array
        /// is the first element in the new curve, we add 3.
        /// </summary>
        public void InsertControlPoint(int rIndex)
        {
            if (mPoints == null) { Reset(); }

            // We may just add the control point to the end
            if (rIndex < 0 || rIndex >= ControlPointCount)
            {
                AddControlPoint();
                return;
            }

            // Allow the first control point to stay
            if (rIndex == 0) { rIndex = 1; }

            // Insert the control point at the right spot
            Array.Resize(ref mPoints, mPoints.Length + 3);
            for (int lShiftIndex = mPoints.Length - 4; lShiftIndex >= rIndex * 3; lShiftIndex--)
            {
                mPoints[lShiftIndex + 3] = mPoints[lShiftIndex];
            }

            int lNewIndex = rIndex * 3;
            int lPrevIndex = (rIndex - 1) * 3;
            int lNextIndex = (rIndex + 1) * 3;

            mPoints[lNextIndex - 1] = mPoints[lNewIndex - 1];

            mPoints[lNewIndex] = (mPoints[lNewIndex - 3] + mPoints[lNewIndex + 3]) / 2f;
            mPoints[lNewIndex + 1] = mPoints[lNewIndex] + ((mPoints[lNextIndex] - mPoints[lNewIndex]) / 2f);
            mPoints[lNewIndex - 1] = mPoints[lNewIndex] + ((mPoints[lPrevIndex] - mPoints[lNewIndex]) / 2f);

            Array.Resize(ref mCurveLengths, mCurveLengths.Length + 1);
            for (int lShiftIndex = mCurveLengths.Length - 1; lShiftIndex >= rIndex; lShiftIndex--)
            {
                mCurveLengths[lShiftIndex] = mCurveLengths[lShiftIndex - 1];
            }

            Array.Resize(ref mControlConstraints, mCurveLengths.Length + 1);
            for (int lShiftIndex = mControlConstraints.Length - 1; lShiftIndex >= rIndex; lShiftIndex--)
            {
                mControlConstraints[lShiftIndex] = mControlConstraints[lShiftIndex - 1];
            }

            // Recalculate the curve lengths
            CalculateCurveLengths();
        }

        /// <summary>
        /// Delete's the specified control point. We need to make sure we reconnect the tangents
        /// </summary>
        public void DeleteControlPoint(int rIndex)
        {
            if (rIndex < 0 || rIndex >= ControlPointCount) { return; }

            if (mPoints.Length <= 4) { return; }

            int lStartIndex = (rIndex == 0 ? 0 : (rIndex * 3) - 1);
            for (int lShiftIndex = lStartIndex; lShiftIndex < mPoints.Length - 3; lShiftIndex++)
            {
                mPoints[lShiftIndex] = mPoints[lShiftIndex + 3];
            }

            Array.Resize(ref mPoints, mPoints.Length - 3);

            for (int lShiftIndex = rIndex; lShiftIndex < mCurveLengths.Length - 1; lShiftIndex++)
            {
                mCurveLengths[lShiftIndex] = mCurveLengths[lShiftIndex + 1];
            }

            Array.Resize(ref mCurveLengths, mCurveLengths.Length - 1);

            for (int lShiftIndex = rIndex; lShiftIndex < mControlConstraints.Length - 1; lShiftIndex++)
            {
                mControlConstraints[lShiftIndex] = mControlConstraints[lShiftIndex + 1];
            }

            Array.Resize(ref mControlConstraints, mControlConstraints.Length - 1);

            // Recalculate the curve lengths
            CalculateCurveLengths();
        }

        /// <summary>
        /// Gets a control point off of the spline
        /// </summary>
        /// <param name="rIndex"></param>
        /// <returns></returns>
        public Vector3 GetControlPoint(int rIndex)
        {
            rIndex = rIndex * 3;
            if (rIndex < 0 || rIndex >= mPoints.Length) { return Vector3.zero; }

            return mPoints[rIndex];
        }

        /// <summary>
        /// Sets a control point on the line
        /// </summary>
        /// <param name="rIndex"></param>
        /// <param name="rPoint"></param>
        public void SetControlPoint(int rIndex, Vector3 rPoint)
        {
            int lControlPointCount = ControlPointCount;

            int lIndex = rIndex * 3;
            if (lIndex < 0 || lIndex >= mPoints.Length) { return; }

            // Store the delta so we can update the tangent points
            Vector3 lDelta = rPoint - mPoints[lIndex];

            // Set the control point
            mPoints[lIndex] = rPoint;

            // If we're looping, connect the ends
            if (mLoop)
            {
                if (rIndex == 0)
                {
                    mPoints[mPoints.Length - 1] = mPoints[0];
                }
                else if (rIndex == lControlPointCount - 1)
                {
                    mPoints[0] = mPoints[mPoints.Length - 1];
                }
            }

            // When the control pints move, we need to move the tangent points
            SetBackwardTangentPoint(rIndex, GetBackwardTangentPoint(rIndex) + lDelta);
            if (GetControlPointConstraint(rIndex) == BezierConstraintType.FREE) 
            { 
                SetForwardTangentPoint(rIndex, GetForwardTangentPoint(rIndex) + lDelta); 
            }

            // Recalculate the curve lengths
            CalculateCurveLengths();
        }

        /// <summary>
        /// Gets a control point constraints for the tangents
        /// </summary>
        /// <param name="rIndex"></param>
        /// <returns></returns>
        public int GetControlPointConstraint(int rIndex)
        {
            if (rIndex < 0 || rIndex >= mControlConstraints.Length) { return BezierConstraintType.MIRROR; }

            return mControlConstraints[rIndex];
        }

        /// <summary>
        /// Sets a control point constraint for the tangents
        /// </summary>
        /// <param name="rIndex"></param>
        /// <param name="rPoint"></param>
        public void SetControlPointConstraint(int rIndex, int rConstraint)
        {
            if (rIndex < 0 || rIndex >= mControlConstraints.Length) { return; }

            mControlConstraints[rIndex] = rConstraint;

            if (mLoop)
            {
                if (rIndex == 0)
                {
                    mControlConstraints[mControlConstraints.Length - 1] = rConstraint;
                }
                else if (rIndex == mControlConstraints.Length - 1)
                {
                    mControlConstraints[0] = rConstraint;
                }
            }

            ApplyControlPointConstraint(rIndex, true);
        }

        /// <summary>
        /// Gets a tangent point off of the spline
        /// </summary>
        /// <param name="rIndex"></param>
        /// <returns></returns>
        public Vector3 GetBackwardTangentPoint(int rIndex)
        {
            rIndex = rIndex * 3;
            if (rIndex < 0 || rIndex >= mPoints.Length) { return Vector3.zero; }

            rIndex = rIndex - 1;
            if (rIndex < 0)
            {
                if (mLoop)
                {
                    rIndex = mPoints.Length - 2;
                }
                else
                {
                    return Vector3.zero;
                }
            }

            return mPoints[rIndex];
        }

        /// <summary>
        /// Sets a tangent point on the spline
        /// </summary>
        /// <param name="rIndex"></param>
        /// <returns></returns>
        public void SetBackwardTangentPoint(int rIndex, Vector3 rPoint)
        {
            int lIndex = rIndex * 3;
            if (lIndex < 0 || lIndex >= mPoints.Length) { return; }

            lIndex = lIndex - 1;
            if (lIndex < 0)
            {
                if (mLoop)
                {
                    lIndex = mPoints.Length - 2;
                }
                else
                {
                    return;
                }
            }

            mPoints[lIndex] = rPoint;
            ApplyControlPointConstraint(rIndex, true);
        }

        /// <summary>
        /// Gets a tangent point off of the spline
        /// </summary>
        /// <param name="rIndex"></param>
        /// <returns></returns>
        public Vector3 GetForwardTangentPoint(int rIndex)
        {
            rIndex = rIndex * 3;
            if (rIndex < 0 || rIndex >= mPoints.Length) { return Vector3.zero; }

            rIndex = rIndex + 1;
            if (rIndex >= mPoints.Length)
            {
                if (mLoop)
                {
                    rIndex = 1;
                }
                else
                {
                    return Vector3.zero;
                }
            }

            return mPoints[rIndex];
        }

        /// <summary>
        /// Sets a tangent point on the spine
        /// </summary>
        /// <param name="rIndex"></param>
        /// <returns></returns>
        public void SetForwardTangentPoint(int rIndex, Vector3 rPoint)
        {
            int lIndex = rIndex * 3;
            if (lIndex < 0 || lIndex >= mPoints.Length) { return; }

            lIndex = lIndex + 1;
            if (lIndex >= mPoints.Length)
            {
                if (mLoop)
                {
                    lIndex = 1;
                }
                else
                {
                    return;
                }
            }

            mPoints[lIndex] = rPoint;
            ApplyControlPointConstraint(rIndex, false);
        }

        /// <summary>
        /// Given a 'percent' of 0 to 1, returns a point on the line if
        /// 0 is the start an 1 is the end.
        /// </summary>
        /// <param name="rPercent"></param>
        /// <returns></returns>
        public Vector3 GetPoint(float rPercent)
        {
            int lCurveIndex = 0;
            float lCurvePercent = 0f;
            GetCurvePercent(rPercent, ref lCurveIndex, ref lCurvePercent);

            // Finally, return the actual position
            return GetPoint(lCurveIndex, lCurvePercent);
        }

        /// <summary>
        /// Given a 'time' of 0 to 1, returns a point on the specific curve if
        /// 0 is the start an 1 is the end.
        /// </summary>
        /// <param name="rTime"></param>
        /// <returns></returns>
        public Vector3 GetPoint(int rCurveIndex, float rPercent)
        {
            // Cycle the percentage between 0 and 1
            if (rPercent < -1f || rPercent > 1f) { rPercent = rPercent % 1f; }
            if (rPercent < 0f) { rPercent = 1f + rPercent; }

            // Get the curve specific value
            rCurveIndex = rCurveIndex * 3;
            Vector3 lLocalPoint = BezierSpline.GetCubicPoint(mPoints[rCurveIndex + 0], mPoints[rCurveIndex + 1], mPoints[rCurveIndex + 2], mPoints[rCurveIndex + 3], rPercent);
            return transform.TransformPoint(lLocalPoint);
        }

        /// <summary>
        /// Given a 'percent' of 0 to 1, returns the speed at the specific point on the line if
        /// 0 is the start an 1 is the end.
        /// </summary>
        /// <param name="rPercent"></param>
        /// <returns></returns>
        public Vector3 GetVelocity(float rPercent)
        {
            int lCurveIndex = 0;
            float lCurvePercent = 0f;
            GetCurvePercent(rPercent, ref lCurveIndex, ref lCurvePercent);

            // Finally, return the actual position
            return GetVelocity(lCurveIndex, lCurvePercent);
        }

        /// <summary>
        /// Given a 'percent' of 0 to 1, returns the speed at the specific point on the line if
        /// 0 is the start an 1 is the end.
        /// </summary>
        /// <param name="rPercent"></param>
        /// <returns></returns>
        public Vector3 GetVelocity(int rCurveIndex, float rPercent)
        {
            // Cycle the percentage between 0 and 1
            if (rPercent < -1f || rPercent > 1f) { rPercent = rPercent % 1f; }
            if (rPercent < 0f) { rPercent = 1f + rPercent; }

            // Get the curve specific value
            rCurveIndex = rCurveIndex * 3;
            Vector3 lLocalVelocity = BezierSpline.GetFirstCubicDerivative(mPoints[rCurveIndex + 0], mPoints[rCurveIndex + 1], mPoints[rCurveIndex + 2], mPoints[rCurveIndex + 3], rPercent) - transform.position;
            return transform.TransformPoint(lLocalVelocity);
        }

        /// <summary>
        /// Given a 'percent' of 0 to 1, returns the direction at the specific point on the line if
        /// 0 is the start an 1 is the end.
        /// </summary>
        /// <param name="rPercent"></param>
        /// <returns></returns>
        public Vector3 GetDirection(float rPercent)
        {
            int lCurveIndex = 0;
            float lCurvePercent = 0f;
            GetCurvePercent(rPercent, ref lCurveIndex, ref lCurvePercent);

            // Finally, return the actual position
            return GetDirection(lCurveIndex, lCurvePercent);
        }

        /// <summary>
        /// Given a 'percent' of 0 to 1, returns the direction at the specific point on the line if
        /// 0 is the start an 1 is the end.
        /// </summary>
        /// <param name="rPercent"></param>
        /// <returns></returns>
        public Vector3 GetDirection(int rCurveIndex, float rPercent)
        {
            return GetVelocity(rCurveIndex, rPercent).normalized;
        }

        /// <summary>
        /// Converts the spline percentage into a curve specific percentage
        /// </summary>
        /// <param name="rPercent"></param>
        /// <param name="rCurveIndex"></param>
        /// <param name="rCurvePercent"></param>
        public void GetCurvePercent(float rPercent, ref int rCurveIndex, ref float rCurvePercent)
        {
            if (mLength == 0f) { CalculateCurveLengths(); }

            // Cycle the percentage between 0 and 1
            if (rPercent < -1f || rPercent > 1f) { rPercent = rPercent % 1f; }
            if (rPercent < 0f) { rPercent = 1f + rPercent; }

            // Get out early if we can
            if (rPercent == 0f)
            {
                rCurveIndex = 0;
                rCurvePercent = 0f;
                return;
            }
            else if (rPercent == 1f)
            {
                rCurveIndex = CurveCount - 1;
                rCurvePercent = 1f;
                return;
            }

            // First, we need to figure out which curve we're in
            float lTargetLength = mLength * rPercent;

            int lCurveIndex = 0;
            float lCurveLengths = 0f;
            for (lCurveIndex = 0; lCurveIndex < CurveCount; lCurveIndex++)
            {
                if (lTargetLength < lCurveLengths + mCurveLengths[lCurveIndex]) { break; }
                lCurveLengths += mCurveLengths[lCurveIndex];
            }

            // Now we need to refactor the time to fit in this curve
            lTargetLength = lTargetLength - lCurveLengths;

            // Finally, return the actual position
            rCurveIndex = lCurveIndex;
            rCurvePercent = lTargetLength / mCurveLengths[lCurveIndex];
        }

        /// <summary>
        /// Forces the tangents that come off the control point to respect
        /// it's type.
        /// </summary>
        /// <param name="rIndex"></param>
        public void ApplyControlPointConstraint(int rIndex, bool rLeadWithBackwardCP)
        {
            if (!mLoop && rIndex <= 0) { return; }
            if (!mLoop && rIndex >= ControlPointCount - 1) { return; }

            int lConstraint = GetControlPointConstraint(rIndex);
            if (lConstraint != BezierConstraintType.FREE)
            {
                Vector3 lControlPoint = GetControlPoint(rIndex);

                int lBackwardTangentIndex = (rIndex * 3) - 1;
                if (lBackwardTangentIndex < 0) { lBackwardTangentIndex = mPoints.Length - 2; }

                Vector3 lBackwardTangentPoint = GetBackwardTangentPoint(rIndex);

                int lForwardTangentIndex = (rIndex * 3) + 1;
                if (lForwardTangentIndex >= mPoints.Length) { lForwardTangentIndex = 1; }

                Vector3 lForwardTangentPoint = GetForwardTangentPoint(rIndex);

                if (lConstraint == BezierConstraintType.MIRROR)
                {
                    if (rLeadWithBackwardCP)
                    {
                        Vector3 lDelta = lBackwardTangentPoint - lControlPoint;
                        mPoints[lForwardTangentIndex] = lControlPoint - lDelta;
                    }
                    else
                    {
                        Vector3 lDelta = lForwardTangentPoint - lControlPoint;
                        mPoints[lBackwardTangentIndex] = lControlPoint - lDelta;
                    }
                }
                else if (lConstraint == BezierConstraintType.ALIGN)
                {
                    if (rLeadWithBackwardCP)
                    {
                        Vector3 lDelta = lControlPoint - lBackwardTangentPoint;
                        lDelta = lDelta.normalized * Vector3.Distance(lControlPoint, lForwardTangentPoint);

                        mPoints[lForwardTangentIndex] = lControlPoint - lDelta;
                    }
                    else
                    {
                        Vector3 lDelta = lControlPoint - lForwardTangentPoint;
                        lDelta = lDelta.normalized * Vector3.Distance(lControlPoint, lBackwardTangentPoint);

                        mPoints[lBackwardTangentIndex] = lControlPoint - lDelta;
                    }
                }
            }

            // Recalculate the curve lengths
            CalculateCurveLengths();
        }

        /// <summary>
        /// Iterate through the curves to determine the curve lengths
        /// </summary>
        public float CalculateCurveLengths()
        {
            mLength = 0f;
            //float lStep = 1 / mSegments;

            // Ensure we can hold the curve lengths
            if (mCurveLengths == null || mCurveLengths.Length == 0) { mCurveLengths = new float[CurveCount]; }

            // Walk each curve
            Vector3 lStart = GetPoint(0, 0f);
            for (int lCurveIndex = 0; lCurveIndex < CurveCount; lCurveIndex++)
            {
                float lCurveLength = 0f;

                // Iterate through each curve given the segmentation we defined
                for (int lSegmentIndex = 1; lSegmentIndex <= mSegments; lSegmentIndex++)
                {
                    float lTime = lSegmentIndex / (float)mSegments;
                    Vector3 lEnd = GetPoint(lCurveIndex, lTime);

                    lCurveLength += Vector3.Distance(lStart, lEnd);

                    lStart = lEnd;
                }

                mCurveLengths[lCurveIndex] = lCurveLength;
                mLength += lCurveLength;
            }

            return mLength;
        }

        // ---------------------------------------------------------------------------------------
        // STATIC FUNCTIONS
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Use the quadradic formula to define the curve between two end points. The middle point
        /// becomes a a control point.
        /// </summary>
        /// <param name="rP0"></param>
        /// <param name="rP1"></param>
        /// <param name="rP2"></param>
        /// <param name="rTime"></param>
        /// <returns></returns>
        public static Vector3 GetQuadradicPoint(Vector3 rP0, Vector3 rP1, Vector3 rP2, float rTime)
        {
            rTime = Mathf.Clamp01(rTime);
            float lOneMinusTime = 1f - rTime;

            return (lOneMinusTime * lOneMinusTime * rP0) + (2f * lOneMinusTime * rTime * rP1) + (rTime * rTime * rP2);
        }

        /// <summary>
        /// Derivatives represent a change. In this case, we can measure the change over time.
        /// </summary>
        /// <param name="rP0"></param>
        /// <param name="rP1"></param>
        /// <param name="rP2"></param>
        /// <param name="rTime"></param>
        /// <returns></returns>
        public static Vector3 GetFirstQuadradicDerivative(Vector3 rP0, Vector3 rP1, Vector3 rP2, float rTime)
        {
            return (2f * (1f - rTime) * (rP1 - rP0)) + (2f * rTime * (rP2 - rP1));
        }

        /// <summary>
        /// Use the cubic formula to define the curve between two end points. The middle point
        /// becomes a a control point.
        /// </summary>
        /// <param name="rP0"></param>
        /// <param name="rP1"></param>
        /// <param name="rP2"></param>
        /// <param name="rTime"></param>
        /// <returns></returns>
        public static Vector3 GetCubicPoint(Vector3 rP0, Vector3 rP1, Vector3 rP2, Vector3 rP3, float rTime)
        {
            rTime = Mathf.Clamp01(rTime);
            float lOneMinusTime = 1f - rTime;

            return (lOneMinusTime * lOneMinusTime * lOneMinusTime * rP0) +
                   (3f * lOneMinusTime * lOneMinusTime * rTime * rP1) +
                   (3f * lOneMinusTime * rTime * rTime * rP2) +
                   (rTime * rTime * rTime * rP3);
        }

        /// <summary>
        /// Derivatives represent a change. In this case, we can measure the change over time.
        /// </summary>
        /// <param name="rP0"></param>
        /// <param name="rP1"></param>
        /// <param name="rP2"></param>
        /// <param name="rTime"></param>
        /// <returns></returns>
        public static Vector3 GetFirstCubicDerivative(Vector3 rP0, Vector3 rP1, Vector3 rP2, Vector3 rP3, float rTime)
        {
            rTime = Mathf.Clamp01(rTime);
            float lOneMinusTime = 1f - rTime;

            return (3f * lOneMinusTime * lOneMinusTime * (rP1 - rP0)) +
                   (6f * lOneMinusTime * rTime * (rP2 - rP1)) +
                   (3f * rTime * rTime * (rP3 - rP2));
        }
    }
}
