
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Fulcrum;

public class SimplexNoise
{
    // * Based on example code by Stefan Gustavson (stegu@itn.liu.se).
    // * Optimisations by Peter Eastman(peastman @drizzle.stanford.edu).
    // * Better rank ordering method by Stefan Gustavson in 2012.
    // rewritten by me for XNA style
    private Vector3[] grad3 = new Vector3[] {
            new Vector3(1,1,0), new Vector3(-1,1,0), new Vector3(1,-1,0), new Vector3(-1,-1,0),
            new Vector3(1,0,1), new Vector3(-1,0,1), new Vector3(1,0,-1), new Vector3(-1,0,-1),
            new Vector3(0,1,1), new Vector3(0,-1,1), new Vector3(0,1,-1), new Vector3(0,-1,-1)
        };

    private int[] p;
    private static int[] perm = new int[512];
    private static int[] permMod12 = new int[512];

    const int SEED_COUNT = 256;
    public SimplexNoise()
    {
        List<int> arr = new List<int>(SEED_COUNT);
        for (int i = 0; i < SEED_COUNT; i++)
            arr.Add(i);
        GUtil.Shuffle(arr);
        p = arr.ToArray();

        for (int i = 0; i < 512; i++)
        {
            perm[i] = p[i & 255];
            permMod12[i] = (short)(perm[i] % 12);
        }
    }

    // Skewing and unskewing factors for 2, 3, and 4 dimensions
    private static float F3 = 1.0f / 3.0f;
    private static float G3 = 1.0f / 6.0f;

    public float GetValue(float xin, float yin, float zin)
    {
        float n0, n1, n2, n3; // Noise contributions from the four corners
                              // Skew the input space to determine which simplex cell we're in
        float s = (xin + yin + zin) * F3; // Very nice and simple skew factor for 3D
        int i = GMath.Floor(xin + s);
        int j = GMath.Floor(yin + s);
        int k = GMath.Floor(zin + s);
        float t = (i + j + k) * G3;
        float X0 = i - t; // Unskew the cell origin back to (x,y,z) space
        float Y0 = j - t;
        float Z0 = k - t;
        float x0 = xin - X0; // The x,y,z distances from the cell origin
        float y0 = yin - Y0;
        float z0 = zin - Z0;
        // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
        // Determine which simplex we are in.
        int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
        int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
        if (x0 >= y0)
        {
            if (y0 >= z0)
            { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // X Y Z order
            else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; } // X Z Y order
            else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; } // Z X Y order
        }
        else
        { // x0<y0
            if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
            else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
            else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
        }
        // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
        // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
        // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
        // c = 1/6.
        float x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
        float y1 = y0 - j1 + G3;
        float z1 = z0 - k1 + G3;
        float x2 = x0 - i2 + 2 * G3; // Offsets for third corner in (x,y,z) coords
        float y2 = y0 - j2 + 2 * G3;
        float z2 = z0 - k2 + 2 * G3;
        float x3 = x0 - 1 + 3 * G3; // Offsets for last corner in (x,y,z) coords
        float y3 = y0 - 1 + 3 * G3;
        float z3 = z0 - 1 + 3 * G3;
        // Work out the hashed gradient indices of the four simplex corners
        int ii = i & 255;
        int jj = j & 255;
        int kk = k & 255;
        int gi0 = permMod12[ii + perm[jj + perm[kk]]];
        int gi1 = permMod12[ii + i1 + perm[jj + j1 + perm[kk + k1]]];
        int gi2 = permMod12[ii + i2 + perm[jj + j2 + perm[kk + k2]]];
        int gi3 = permMod12[ii + 1 + perm[jj + 1 + perm[kk + 1]]];
        // Calculate the contribution from the four corners
        float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0; // change to 0.5 if you want
        if (t0 < 0) n0 = 0;
        else
        {
            t0 *= t0;
            n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
        }
        float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1; // change to 0.5 if you want
        if (t1 < 0) n1 = 0;
        else
        {
            t1 *= t1;
            n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
        }
        float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2; // change to 0.5 if you want
        if (t2 < 0) n2 = 0;
        else
        {
            t2 *= t2;
            n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
        }
        float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3; // change to 0.5 if you want
        if (t3 < 0) n3 = 0;
        else
        {
            t3 *= t3;
            n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
        }
        // Add contributions from each corner to get the final noise value.
        // The result is scaled to stay just inside [-1,1] (now [0, 1])
        return (32.0f * (n0 + n1 + n2 + n3) + 1) * 0.5f; // change to 76.0 if you want
    }

    private static float dot(Vector3 g, float x, float y, float z)
    {
        return g.X * x + g.Y * y + g.Z * z;
    }

    public float GetOctaveNoise(float pX, float pY, float pZ, int pOctaves)
    {
        float value = 0;
        float divisor = 0;
        float currentHalf = 0;
        float currentfloat = 0;

        for (int i = 0; i < pOctaves; i++)
        {
            currentHalf = (float)Math.Pow(0.5f, i);
            currentfloat = (float)Math.Pow(2, i);
            value += GetValue(pX * currentfloat, pY * currentfloat, pZ) * currentHalf;
            divisor += currentHalf;
        }

        return value / divisor;
    }


}

public class PerlinNoise
{
    double[,,] layers;
    double[,,] seeds;
    double[,] combined;
    Grid<double> shape;
    int _w, _h, d;
    public static Random Seed = new Random();

    // w/h are the grid used by perlin, you can Sample to pick readings proportional to another sized grid

    public PerlinNoise(int w, int h, int d, Grid<double> shape = null)
    {
        this._w = w;
        this._h = h;
        this.d = d;
        seeds = new double[d, w, h];
        layers = new double[d, w, h];
        combined = new double[w, h];
        this.shape = shape;
        Build();
    }

    public void Build()
    {
        if (shape == null)
        {
            for (int i = 0; i < d; i++)
                Reseed(i);
            for (int i = 0; i < d; i++)
                BuildLayer(i);
        }
        else
        {
            for (int i = 0; i < d - 1; i++)
                Reseed(i);
            for (int i = 0; i < d - 1; i++)
                BuildLayer(i);
            DefineShape();
        }
        CombineLayers();
    }

    // should always return between 0-1
    public double Val(int x, int y)
    {
        return combined[mod(x, _w), mod(y, _h)];
    }

    // where w/h is the size of the grid you want to populate - value will be interpolated relative to that
    public float Sample(int x, int y, int w, int h)
    {
        x = mod(x, w); y = mod(y, h); // would need to adjust this if I wanted to support tesselation
        Vector2 target = new Vector2(1f * x * (_w - 1) / w, 1f * y * (_h - 1) / h);
        var ptNW = new Point((int)target.X, (int)target.Y);
        var proportion = target - ptNW.ToVector2();
        var NW = (float)Val(x, y);
        var NE = (float)Val(x + 1, y);
        var SW = (float)Val(x, y + 1);
        var SE = (float)Val(x + 1, y + 1);
        var lerpTop = GMath.Lerp(NW, NE, proportion.X);
        var lerpBottom = GMath.Lerp(SW, SE, proportion.X);
        return GMath.Lerp(lerpTop, lerpBottom, proportion.Y);
    }

    int mod(int div, int rem)
    {
        if (div < 0) return rem + (div % rem) - 1;
        return div % rem;
    }

    void Reseed(int i)
    {
        for (int x = 0; x < _w; x++)
            for (int y = 0; y < _h; y++)
                seeds[i, x, y] = Seed.NextDouble();
    }

    void BuildLayer(int i)
    {
        for (int y = 0; y < _h; y++)
            for (int x = 0; x < _w; x++)
                layers[i, x, y] = Extrapolate(i, x, y);
    }

    void DefineShape()
    {
        for (int x = 0; x < _w; x++)
            for (int y = 0; y < _h; y++)
                layers[d - 1, x, y] = ExtrapolateFromShape(x, y);
    }

    void CombineLayers()
    {
        for (int x = 0; x < _w; x++)
            for (int y = 0; y < _h; y++)
                combined[x, y] = FinalValue(x, y);
    }

    double FinalValue(int x, int y)
    {
        double ret = 0;
        for (int i = 0; i < d; i++)
            ret += layers[i, x, y] / Math.Pow(2, d - i);
        return ret;
    }

    double Extrapolate(int i, int x, int y)
    {
        int factor = (int)Math.Pow(2, i);
        double dx, dy;
        dx = (double)x / factor;
        dy = (double)y / factor;

        double fx, fy;
        fx = dx - Math.Floor(dx);
        fy = dy - Math.Floor(dy);

        double v1, v2, v3, v4;
        v1 = seeds[i, (int)(dx) % (_w / factor), (int)(dy) % (_h)]; //divide h by factor to repeat pattern vertically
        v2 = seeds[i, (int)(dx + 1) % (_w / factor), (int)(dy) % (_h)];
        v3 = seeds[i, (int)(dx) % (_w / factor), (int)(dy + 1) % (_h)];
        v4 = seeds[i, (int)(dx + 1) % (_w / factor), (int)(dy + 1) % (_h)];

        double temp1, temp2;
        temp1 = Interpolate(v1, v2, fx);
        temp2 = Interpolate(v3, v4, fx);

        return Interpolate(temp1, temp2, fy);
    }

    double ExtrapolateFromShape(int x, int y)
    {
        double dx, dy;
        dx = (double)x * (shape.W - 1) / _w;
        dy = (double)y * (shape.H - 1) / _h;

        double fx, fy;
        fx = dx - Math.Floor(dx);
        fy = dy - Math.Floor(dy);

        double v1, v2, v3, v4;
        v1 = shape.Get((int)(dx) % (shape.W), (int)(dy) % (shape.H));
        v2 = shape.Get((int)(dx + 1) % (shape.W), (int)(dy) % (shape.H));
        v3 = shape.Get((int)(dx) % (shape.W), (int)(dy + 1) % (shape.H));
        v4 = shape.Get((int)(dx + 1) % (shape.W), (int)(dy + 1) % (shape.H));

        double temp1, temp2;
        temp1 = Interpolate(v1, v2, fx);
        temp2 = Interpolate(v3, v4, fx);

        return Interpolate(temp1, temp2, fy);
    }

    double Interpolate(double a, double b, double x)
    {
        //return a * (1 - x) + b * x;
        double ft, f;
        ft = x * Math.PI;
        f = (1 - Math.Cos(ft)) * .5;
        return a * (1 - f) + b * f;
    }
}