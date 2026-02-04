using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using InkSim;

namespace InkSim.Tests
{
    [TestFixture]
    public class PolygonDistrictTests
    {
        private DistrictDefinition _lShapedDistrict;

        [SetUp]
        public void SetUp()
        {
            // Create L-shaped district
            //   ████
            //   █
            //   █
            //   █
            _lShapedDistrict = ScriptableObject.CreateInstance<DistrictDefinition>();
            _lShapedDistrict.id = "l_district";
            _lShapedDistrict.polygonVertices = new List<Vector2Int>
            {
                new Vector2Int(0, 0),   // Bottom-left
                new Vector2Int(1, 0),   // Bottom-right of stem
                new Vector2Int(1, 3),   // Top-right of stem
                new Vector2Int(4, 3),   // Top-right of horizontal
                new Vector2Int(4, 4),   // Far top-right
                new Vector2Int(0, 4),   // Far top-left
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_lShapedDistrict);
        }

        [Test]
        public void Contains_InsideLShape_ReturnsTrue()
        {
            // Inside the vertical stem
            Assert.IsTrue(_lShapedDistrict.Contains(0, 1));

            // Inside the horizontal part
            Assert.IsTrue(_lShapedDistrict.Contains(2, 3));
        }

        [Test]
        public void Contains_OutsideLShape_ReturnsFalse()
        {
            // The "notch" in the L (should be outside)
            Assert.IsFalse(_lShapedDistrict.Contains(2, 1));
            Assert.IsFalse(_lShapedDistrict.Contains(3, 0));
        }

        [Test]
        public void Contains_OnBoundary_ReturnsTrue()
        {
            // Vertices
            Assert.IsTrue(_lShapedDistrict.Contains(0, 0));
            Assert.IsTrue(_lShapedDistrict.Contains(4, 4));
        }

        [Test]
        public void Contains_FarOutside_ReturnsFalse()
        {
            Assert.IsFalse(_lShapedDistrict.Contains(100, 100));
            Assert.IsFalse(_lShapedDistrict.Contains(-10, -10));
        }

        [Test]
        public void Contains_FallsBackToAABB_WhenNoPolygon()
        {
            var aabbDistrict = ScriptableObject.CreateInstance<DistrictDefinition>();
            aabbDistrict.minX = 0;
            aabbDistrict.maxX = 10;
            aabbDistrict.minY = 0;
            aabbDistrict.maxY = 10;
            aabbDistrict.polygonVertices = null;  // No polygon

            Assert.IsTrue(aabbDistrict.Contains(5, 5));
            Assert.IsFalse(aabbDistrict.Contains(15, 15));

            Object.DestroyImmediate(aabbDistrict);
        }

        [Test]
        public void Contains_FallsBackToAABB_WhenPolygonTooSmall()
        {
            var badPolygon = ScriptableObject.CreateInstance<DistrictDefinition>();
            badPolygon.minX = 0;
            badPolygon.maxX = 10;
            badPolygon.minY = 0;
            badPolygon.maxY = 10;
            badPolygon.polygonVertices = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 1),
                // Only 2 vertices - not a valid polygon
            };

            // Should use AABB fallback
            Assert.IsTrue(badPolygon.Contains(5, 5));

            Object.DestroyImmediate(badPolygon);
        }

        [Test]
        public void Contains_ComplexPolygon_Concave()
        {
            // Test a more complex concave shape (like a 'C')
            var cShape = ScriptableObject.CreateInstance<DistrictDefinition>();
            cShape.id = "c_district";
            cShape.polygonVertices = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(5, 0),
                new Vector2Int(5, 1),
                new Vector2Int(1, 1),
                new Vector2Int(1, 4),
                new Vector2Int(5, 4),
                new Vector2Int(5, 5),
                new Vector2Int(0, 5),
            };

            // Inside the C
            Assert.IsTrue(cShape.Contains(0, 2));
            Assert.IsTrue(cShape.Contains(0, 0));
            Assert.IsTrue(cShape.Contains(4, 0));
            Assert.IsTrue(cShape.Contains(4, 4));

            // In the hollow part of the C
            Assert.IsFalse(cShape.Contains(3, 2));

            Object.DestroyImmediate(cShape);
        }

        [Test]
        public void Contains_TrianglePolygon()
        {
            var triangle = ScriptableObject.CreateInstance<DistrictDefinition>();
            triangle.id = "triangle";
            triangle.polygonVertices = new List<Vector2Int>
            {
                new Vector2Int(0, 0),
                new Vector2Int(10, 0),
                new Vector2Int(5, 10),
            };

            // Inside triangle
            Assert.IsTrue(triangle.Contains(5, 3));

            // Outside triangle
            Assert.IsFalse(triangle.Contains(1, 8));
            Assert.IsFalse(triangle.Contains(9, 8));

            Object.DestroyImmediate(triangle);
        }
    }
}
