using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics;

namespace EndlessAmmoInventory.Helpers {
	class Rect {
		public Vector2 Position = new Vector2(0, 0);
		public Vector2 Dimensions = new Vector2(0, 0);
		public float X {
			get => Position.X;
			set => Position.X = value;
		}
		public float Y {
			get => Position.Y;
			set => Position.Y = value;
		}
		public float Width {
			get => Dimensions.X;
			set => Dimensions.X = value;
		}
		public float Height {
			get => Dimensions.Y;
			set => Dimensions.Y = value;
		}

		public float Right => Position.X + Dimensions.X;
		public float Bottom => Position.Y + Dimensions.Y;

		public Rect(float x, float y, float width, float height) {
			Position.X = x;
			Position.Y = y;
			Dimensions.X = width;
			Dimensions.Y = height;
		}

		public Rect(float x, float y, Vector2 dimensions) : this(x, y, dimensions.X, dimensions.Y) { }
		public Rect(Vector2 position, float width, float height) : this(position.X, position.Y, width, height) { }
		public Rect(Vector2 position, Vector2 dimensions) : this(position.X, position.Y, dimensions.X, dimensions.Y) { }
		public Rect(float width, float height) : this(0, 0, width, height) { }
		public Rect(Vector2 dimensions) : this(0, 0, dimensions.X, dimensions.Y) { }
		public Rect() : this(0, 0, 0, 0) { }

		[Obsolete]
		public void Scale(float xScale, float yScale) {
			Dimensions.X *= xScale;
			Dimensions.Y *= yScale;
		}

		[Obsolete]
		public void Scale(float scale) {
			Dimensions *= scale;
		}

		[Obsolete]
		public void Translate(float x, float y) {
			Position.X += x;
			Position.Y += y;
		}

		[Obsolete]
		public void Grow(float width, float height) {
			Dimensions.X += width;
			Dimensions.Y += height;
		}

		public Vector2 ClonePosition() {
			return new Vector2(Position.X, Position.Y);
		}

		public Vector2 CloneDimensions() {
			return new Vector2(Dimensions.X, Dimensions.Y);
		}

		public bool Contains(float x, float y) {
			return x >= X && y >= Y && x <= Right && y <= Bottom;
		}

		public bool Contains(Vector2 vector) {
			return Contains(vector.X, vector.Y);
		}

		public bool Contains(Point point) {
			return Contains(point.X, point.Y);
		}

		public Vector2 Center() {
			return Position + Dimensions / 2;
		}

		public Rect Clone() {
			return new Rect(Position.X, Position.Y, Dimensions.X, Dimensions.Y);
		}
	}

	class ScalableTexture2D {
		public readonly Texture2D Texture;
		private readonly int[] widths = new int[3];
		private readonly int[] heights = new int[3];
		private readonly int[] sourceX = new int[3];
		private readonly int[] sourceY = new int[3];

		public ScalableTexture2D(Texture2D texture, int leftWidth, int rightWidth, int topHeight, int bottomHeight) {
			Texture = texture;
			widths[0] = leftWidth;
			widths[1] = texture.Width - leftWidth - rightWidth;
			widths[2] = rightWidth;
			heights[0] = topHeight;
			heights[1] = texture.Height - topHeight - bottomHeight;
			heights[2] = bottomHeight;

			sourceX[0] = 0;
			sourceX[1] = widths[0];
			sourceX[2] = widths[0] + widths[1];
			sourceY[0] = 0;
			sourceY[1] = heights[0];
			sourceY[2] = heights[0] + heights[1];
		}

		public ScalableTexture2D(Texture2D texture, int cornerWidth, int cornerHeight) : this(texture, cornerWidth, cornerWidth, cornerHeight, cornerHeight) { }

		public ScalableTexture2D(Texture2D texture, int cornerSize) : this(texture, cornerSize, cornerSize) { }

		public void Draw(SpriteBatch spriteBatch, Rect rect, Color color, float scale = 1f) {
			float iw = rect.Width - (widths[0] + widths[2]) * scale;
			float ih = rect.Height - (heights[0] + heights[2]) * scale;

			float[] x = new float[3];
			x[0] = rect.X;
			x[1] = x[0] + widths[0] * scale;
			x[2] = x[1] + iw;

			float[] y = new float[3];
			y[0] = rect.Y;
			y[1] = y[0] + heights[0] * scale;
			y[2] = y[1] + ih;

			for (int i = 0; i < 9; i++) {
				int ix = i % 3;
				int iy = i / 3;

				Vector2 textureScale = new Vector2(ix == 1 ? iw / widths[1] : scale, iy == 1 ? ih / heights[1] : scale);
				Rectangle sourceRect = new Rectangle(sourceX[ix], sourceY[iy], widths[ix], heights[iy]);
				Vector2 position = new Vector2(x[ix], y[iy]);

				spriteBatch.Draw(Texture, position, sourceRect, color, 0f, Vector2.Zero, textureScale, SpriteEffects.None, 0);
			}
		}
	}

	class Scrollbar {
		public static ScalableTexture2D scrollbarTexture;
		public static ScalableTexture2D scrollbarInnerTexture;
		public static void OnInitialize() {
			scrollbarTexture = new ScalableTexture2D(TextureManager.Load("Images/UI/Scrollbar"), 6);
			scrollbarInnerTexture = new ScalableTexture2D(TextureManager.Load("Images/UI/ScrollbarInner"), 6);
		}

		public static void Draw(SpriteBatch spriteBatch, Rect rect, float offsetHeight, float scrollHeight, float scrollTop, Color color, float scale = 1) {
			scrollbarTexture.Draw(spriteBatch, rect, color, scale);

			Rect innerRect = rect.Clone();
			innerRect.Y += 2 * scale;
			innerRect.Height -= 4 * scale;
			innerRect.Y += scrollTop / scrollHeight * innerRect.Height;
			innerRect.Height *= offsetHeight / scrollHeight;

			scrollbarInnerTexture.Draw(spriteBatch, innerRect, color, scale);
		}
	}

	class AnimationBezier {
		private static readonly int kSplineTableSize = 11;
		private static readonly double kSampleStepSize = 1.0 / (kSplineTableSize - 1);
		private static readonly double NEWTON_ITERATIONS = 4;
		private static readonly double NEWTON_MIN_SLOPE = 1e-3;
		private static readonly double SUBDIVISION_PRECISION = 1e-7;
		private static readonly double SUBDIVISION_MAX_ITERATIONS = 10;

		private readonly double[] samples = new double[kSplineTableSize];
		private readonly double X1;
		private readonly double Y1;
		private readonly double X2;
		private readonly double Y2;
		public AnimationBezier(double x1, double y1, double x2, double y2) {
			X1 = x1;
			Y1 = y1;
			X2 = x2;
			Y2 = y2;

			for (int i = 0; i < kSplineTableSize; i++) {
				samples[i] = CalcBezier(x1, x2, kSampleStepSize * i);
			}
		}

		public double GetTForX(double x) {
			double intervalStart = 0;
			int lastSample = kSplineTableSize - 1;
			int currentSample = 1;

			for (
				;
				currentSample != lastSample && samples[currentSample] <= x;
				++currentSample
			) {
				intervalStart += kSampleStepSize;
			}
			--currentSample;

			double dist =
				(x - samples[currentSample]) /
				(samples[currentSample + 1] - samples[currentSample]);

			double guessForT = intervalStart + dist * kSampleStepSize;

			double initialSlope = GetSlope(X1, X2, guessForT);

			if (initialSlope >= NEWTON_MIN_SLOPE)
				return NewtonRaphsonIterate(x, guessForT, X1, X2);

			if (initialSlope == 0)
				return guessForT;

			return BinarySubdivide(x, intervalStart, intervalStart + kSampleStepSize, X1, X2);
		}

		public double GetYForX(double x) {
			if (x <= 0)
				return 0;
			if (x >= 1)
				return 1;

			double t = GetTForX(x);
			double bezier = CalcBezier(Y1, Y2, t);
			return bezier;
		}

		public double GetSlopeAt(double t) {
			double x = X1 + X2 + t * (t * (1 - X1) - 4 * X1);
			double y = Y1 + Y2 + t * (t * (1 - Y1) - 4 * Y1);
			if (y == 0)
				return double.MaxValue;

			return x / y;
		}

		private static double CalcBezier(double a, double b, double t) {
			return (((1 - 3 * b + 3 * a) * t + (3 * b - 6 * a)) * t + 3 * a) * t;
		}

		private static double GetSlope(double a, double b, double t) {
			return 3 * (1 - 3 * b + 3 * a) * t * t + 2 * (3 * b - 6 * a) * t + 3 * a;
		}

		private static double NewtonRaphsonIterate(double x, double t, double x1, double x2) {
			for (int i = 0; i < NEWTON_ITERATIONS; ++i) {
				double currentSlope = GetSlope(x1, x2, t);
				if (currentSlope == 0.0)
					return t;

				double currentX = CalcBezier(x1, x2, t) - x;
				t -= currentX / currentSlope;
			}

			return t;
		}

		private static double BinarySubdivide(double x, double a, double b, double x1, double x2) {
			double currentX;
			double currentT;
			int i = 0;
			do {
				currentT = a + (b - a) / 2;
				currentX = CalcBezier(x1, x2, currentT) - x;
				if (currentX > 0.0) {
					b = currentT;
				} else {
					a = currentT;
				}
			} while (
				Math.Abs(currentX) > SUBDIVISION_PRECISION &&
				++i < SUBDIVISION_MAX_ITERATIONS
			);

			return currentT;
		}
	}

	class VelocityAnimation {
		private AnimationBezier Bezier;
		private double StartValue = 0;
		public double Target { get; private set; } = 0;
		private double StartAt = 0;
		private double EndAt = 0;
		private double Magnitude = 0;
		private readonly double Duration = 0;

		public VelocityAnimation(double initialValue = 0, double duration = 200) {
			Target = initialValue;
			Duration = duration;
		}

		public double GetValue(double now = 0) {
			if (Bezier == null)
				return Target;

			if (now == 0)
				now = Main._drawInterfaceGameTime.TotalGameTime.TotalMilliseconds;

			if (now <= StartAt)
				return StartValue;

			if (now >= EndAt)
				return Target;

			return Lerp(StartValue, Target, Bezier.GetYForX((now - StartAt) / Duration));
		}

		public void SetValue(double targetValue) {
			double now = Main._drawInterfaceGameTime.TotalGameTime.TotalMilliseconds;
			double currentValue = GetValue(now);
			if (currentValue == targetValue)
				return;

			double currentMagnitude = Magnitude;
			double currentVelocity = 0;
			if (Bezier != null && currentMagnitude > 0 && now >= StartAt && now <= EndAt) {
				currentVelocity = currentMagnitude * Bezier.GetSlopeAt((now - StartAt) / Duration);
			}

			StartAt = now;
			EndAt = now + Duration;
			StartValue = currentValue;
			Target = targetValue;
			Magnitude = Math.Abs(targetValue - currentValue);

			double targetVelocity = currentVelocity * (currentMagnitude == 0 ? 1 : Magnitude / currentMagnitude);
			Bezier = new AnimationBezier(0.5, 0.5 * targetVelocity, 0.5, 1);
		}

		public void SetValueImmediate(double value) {
			double now = Main._drawInterfaceGameTime.TotalGameTime.TotalMilliseconds;
			EndAt = now;
			Target = value;
			Magnitude = 0;
		}

		private double Lerp(double a, double b, double t) {
			return a * (1 - t) + b * t;
		}
	}
}
