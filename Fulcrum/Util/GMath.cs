using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public static class GMath
{
    public static int Clamp(int value, int min, int max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		public static float Clamp(float value, float min, float max)
		{
			if (value < min) return min;
			if (value > max) return max;
			return value;
		}

		// this is mostly for something like a camera
		// if min > max, return the average of min and max
		public static float ClampOrCenter(float value, float min, float max)
		{
			if (min > max) return (min + max) / 2f;
			return Clamp(value, min, max);
		}
		public static float ClampishOrCenter(float value, float min, float max, float factor = .9f, float threshold = .01f)
		{
			if (min > max) return Decelerate(value, (min + max) / 2f, factor, threshold);
			return Clampish(value, min, max, factor, threshold);
		}

		// lets you go past the max, but attempts to bring you back
		// handy for something you're calling every frame to bring it back in
		//factor is multiplied by the diff, diff under threshold jumps to the edge
		public static float Clampish(float value, float min, float max, float factor = .9f, float threshold = .01f)
		{
			if (value < min) return Decelerate(value, min, factor, threshold);
			if (value > max) return Decelerate(value, max, factor, threshold);
			return value;
		}
        
		//factor closer to 1 for slower decel
		public static float Decelerate(float start, float end, float factor, float threshold)
		{
			Debug.Assert(factor > 0 && factor < 1f);
			float ret = (end - start) * factor;
			if (Math.Abs(end - (ret + start)) < threshold) ret = end;
			else ret += start;
			return ret;
		}

		public static bool BetweenInclusive(int min, int value, int max)
			=> value >= min && value <= max;
		public static bool BetweenInclusive(float min, float value, float max)
			=> value >= min && value <= max;

		public static int Diff(int a, int b) => Math.Abs(a - b);
		public static float Diff(float a, float b) => Math.Abs(a - b);

		public static float fPI = 3.14159265f;

		//use mathematical definition for modulo (-1 % 100 = 99)
		public static int Mod(int value, int mod)
		{
			if (value < 0)
				return mod - ((-value - 1) % mod) - 1;
			else return value % mod;
		}

		public static float ModFloat(float value, float mod)
		{
			if (value < 0)
				return mod - ((-value) % mod);
			else return value % mod;
		}

		//because -2/10 is the same as 4/10
		public static int Div(int value, int div)
		{
			if (value < 0)
				return (value - div) / div;
			return value / div;
		}

        //yes there is one in the standard library, but it doesn't return an int for some dumb reason
		public static int Floor(float val)
		{
			if (val < 0) return (int)(val - 1);
			return (int)val;
		}
		public static int Ceiling(float val)
		{
			if (val < 0) return (int)val;
			return (int)(val + 1);
		}

        public static float RollingAverage(float start, float additional, int samples)
		{
			return (start * (samples - 1) + additional) / samples;
		}

		public static Vector2 RollingAverage(Vector2 start, Vector2 additional, int samples)
		{
			return (start * (samples - 1) + additional) / samples;
		}

		//using top as 0, circling clockwise
		public static float DegreesToRadians(float angle)
		{
			return angle * fPI / 180;
		}
		public static float RadiansToDegrees(float angle)
		{
			return angle * 180 / fPI;
		}

        
		public static int Round(float val)
		{
			return (int)Math.Round(val);
		}

		//e.g round to nearest 5
		public static int RoundToNearest(float val, int nearest)
		{
			return Round(val / nearest) * nearest;
		}
		public static float RoundToNearest(float val, float nearest)
		{
			return (float)(Math.Round(val / nearest) * nearest);
		}

		public static int Min(params int[] vals)
		{
			return vals.Min();
		}
		public static int Max(params int[] vals)
		{
			return vals.Max();
		}
}