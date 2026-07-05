using NUnit.Framework;
using Systems.SimpleCore.Utility;
using Unity.Collections;
using Unity.Mathematics;

namespace Systems.SimpleCore.Tests
{
    public sealed class MathAndFrustumTests
    {
        private const float TOLERANCE = 0.0001f;

        [Test]
        public void Rotate2D_RotatesCounterClockwiseByRadians()
        {
            float2 result = MathExtensions.Rotate(new float2(1f, 0f), math.PI * 0.5f);

            Assert.AreEqual(0f, result.x, TOLERANCE);
            Assert.AreEqual(1f, result.y, TOLERANCE);
        }

        [Test]
        public void Rotate3D_RotatesAroundNormalizedAxis()
        {
            float3 result = MathExtensions.Rotate(new float3(1f, 0f, 0f), new float3(0f, 2f, 0f), math.PI * 0.5f);

            Assert.AreEqual(0f, result.x, TOLERANCE);
            Assert.AreEqual(0f, result.y, TOLERANCE);
            Assert.AreEqual(-1f, result.z, TOLERANCE);
        }

        [Test]
        public void ClosestPointOnPlane_ProjectsPointOntoPlane()
        {
            float4 plane = new float4(0f, 1f, 0f, -2f);
            float3 result = FrustumUtil.ClosestPointOnPlane(plane, new float3(5f, 8f, -3f));

            Assert.AreEqual(5f, result.x, TOLERANCE);
            Assert.AreEqual(2f, result.y, TOLERANCE);
            Assert.AreEqual(-3f, result.z, TOLERANCE);
        }

        [Test]
        public void ExtractFrustumPlanes_AllowsInsidePointAndRejectsOutsidePoints()
        {
            NativeArray<float4> planes = new NativeArray<float4>(6, Allocator.Temp);
            try
            {
                FrustumUtil.ExtractFrustumPlanes(
                    new float3(0f, 0f, 0f),
                    quaternion.identity,
                    10f,
                    1f,
                    1f,
                    10f,
                    ref planes);

                Assert.IsTrue(FrustumUtil.IsPointInFrustum(new float3(0f, 0f, 5f), planes));
                Assert.IsFalse(FrustumUtil.IsPointInFrustum(new float3(0f, 0f, 0.5f), planes));
                Assert.IsFalse(FrustumUtil.IsPointInFrustum(new float3(20f, 0f, 5f), planes));
                Assert.IsFalse(FrustumUtil.IsPointInFrustum(new float3(0f, 0f, 11f), planes));
            }
            finally
            {
                planes.Dispose();
            }
        }

        [Test]
        public void ComputeFrustumGizmosLines_ReturnsTwelveEdges()
        {
            NativeArray<float3x2> lines;
            FrustumUtil.ComputeFrustumGizmosLines(
                new float3(0f, 0f, 0f),
                quaternion.identity,
                8f,
                1f,
                1f,
                4f,
                out lines,
                Allocator.Temp);

            try
            {
                Assert.AreEqual(12, lines.Length);
                Assert.AreEqual(new float3(-1f, 1f, 1f), lines[0].c0);
                Assert.AreEqual(new float3(1f, 1f, 1f), lines[0].c1);
            }
            finally
            {
                lines.Dispose();
            }
        }
    }
}
