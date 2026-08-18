using System;
using System.Numerics;

namespace CNCMaps.Engine.Rendering {
	/// <summary>
	/// Matrix helpers that replace OpenTK.Mathematics with System.Numerics while keeping
	/// bit-identical output: System.Numerics' SIMD operators associate differently and
	/// round the last ulp differently than OpenTK's scalar math did, and its projection
	/// matrices use DirectX depth conventions (z to [0,1]) where the depth mapping in
	/// VxlRenderer relies on the GL convention (z to [-1,1]). The scalar, left-associated
	/// evaluation order below reproduces OpenTK exactly.
	/// </summary>
	internal static class MatrixMath {
		public static Matrix4x4 Mul(Matrix4x4 a, Matrix4x4 b) => new Matrix4x4(
			a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
			a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
			a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
			a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,
			a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
			a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
			a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
			a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,
			a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
			a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
			a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
			a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,
			a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
			a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
			a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
			a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44);

		/// <summary>Left-to-right product like a chained row-vector matrix stack.</summary>
		public static Matrix4x4 Mul(Matrix4x4 a, Matrix4x4 b, params Matrix4x4[] rest) {
			var m = Mul(a, b);
			foreach (var r in rest)
				m = Mul(m, r);
			return m;
		}

		/// <summary>Row-vector transform v·M (OpenTK's Vector4.TransformRow).</summary>
		public static Vector4 TransformRow(in Vector4 v, in Matrix4x4 m) => new Vector4(
			v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31 + v.W * m.M41,
			v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32 + v.W * m.M42,
			v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33 + v.W * m.M43,
			v.X * m.M14 + v.Y * m.M24 + v.Z * m.M34 + v.W * m.M44);

		/// <summary>Row-vector transform without translation (OpenTK's Vector3.TransformVector).</summary>
		public static Vector3 TransformNormal(in Vector3 v, in Matrix4x4 m) => new Vector3(
			v.X * m.M11 + v.Y * m.M21 + v.Z * m.M31,
			v.X * m.M12 + v.Y * m.M22 + v.Z * m.M32,
			v.X * m.M13 + v.Y * m.M23 + v.Z * m.M33);

		public static Matrix4x4 CreateOrthographicGL(float width, float height, float zNear, float zFar) {
			float left = -width / 2, right = width / 2, bottom = -height / 2, top = height / 2;
			var m = Matrix4x4.Identity;
			m.M11 = 2 / (right - left);
			m.M22 = 2 / (top - bottom);
			m.M33 = -2 / (zFar - zNear);
			m.M41 = -(right + left) / (right - left);
			m.M42 = -(top + bottom) / (top - bottom);
			m.M43 = -(zFar + zNear) / (zFar - zNear);
			return m;
		}

		public static Matrix4x4 CreatePerspectiveFieldOfViewGL(float fovy, float aspect, float zNear, float zFar) {
			float top = zNear * MathF.Tan(0.5f * fovy);
			float bottom = -top;
			float left = bottom * aspect, right = top * aspect;
			var m = default(Matrix4x4);
			m.M11 = 2 * zNear / (right - left);
			m.M22 = 2 * zNear / (top - bottom);
			m.M31 = (right + left) / (right - left);
			m.M32 = (top + bottom) / (top - bottom);
			m.M33 = -(zFar + zNear) / (zFar - zNear);
			m.M34 = -1;
			m.M43 = -(2 * zFar * zNear) / (zFar - zNear);
			return m;
		}
	}
}
