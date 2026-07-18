using Microsoft.Xna.Framework;

namespace Fulcrum;

public struct FRectangle
{
    Vector4 _vector = Vector4.Zero; // X, Y, Width, Height
    public FRectangle(Vector4 vect)
    {
        _vector = vect;
    }
    public FRectangle(float x, float y, float w, float h)
    {
        _vector = new Vector4(x, y, w, h);
    }
    public FRectangle(Vector2 topLeft, Vector2 scale)
    {
        _vector = new Vector4(topLeft.X, topLeft.Y, scale.X, scale.Y);
    }

    public Vector2 TopLeft => new Vector2(_vector.X, _vector.Y);
    public Vector2 BottomRight => new Vector2(_vector.X + _vector.Z, _vector.Y + _vector.W);
    public Vector2 Center => new Vector2(_vector.X + (_vector.Z / 2), _vector.Y + _vector.W / 2);
    public Vector2 Size => new Vector2(_vector.Z, _vector.W);

    public float X => _vector.X;
    public float Y => _vector.Y;
    public float Width => _vector.Z;
    public float Height => _vector.W;

    public Rectangle ToRect()
        => new Rectangle((int)_vector.X, (int)_vector.Y, (int)_vector.Z, (int)_vector.W);
    public Rectangle ToNearestRect()
        => new Rectangle(
            GMath.Round(_vector.X),
            GMath.Round(_vector.Y),
            GMath.Round(_vector.Z),
            GMath.Round(_vector.W)
        );
    public void ShiftBy(Vector2 shift) =>
        _vector += new Vector4(shift, 0, 0);
    public void ShiftTo(Vector2 newTopLeft) =>
        _vector = new Vector4(newTopLeft.X, newTopLeft.Y, Size.X, Size.Y);
    public void ScaleBy(Vector2 scale) =>
        _vector = new Vector4(TopLeft, Size.X * scale.X, Size.Y * scale.Y);
    public void SizeTo(Vector2 size) =>
        _vector = new Vector4(TopLeft, size.X, size.Y);
    public FRectangle MakeShiftBy(Vector2 shift) =>
        new FRectangle(TopLeft + shift, Size);
    public FRectangle MakeShiftTo(Vector2 newTopLeft) =>
        new FRectangle(newTopLeft, Size);
    public FRectangle MakeSizeTo(Vector2 size) =>
        new FRectangle(TopLeft, size);
    public FRectangle MakeScaleBy(Vector2 scale) =>
        new FRectangle(TopLeft, Size * scale);
    public FRectangle MakeScaleByFromCenter(Vector2 scale) =>
        new FRectangle(TopLeft - (Size * scale - Size) / 2, Size * scale);
}