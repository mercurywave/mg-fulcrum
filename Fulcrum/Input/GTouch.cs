using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;
namespace Fulcrum;

public static class GTouch
{
    public static void Initialize(GestureType type)
    {
        TouchPanel.EnabledGestures = type;
    }

    public static float pinchScale;
    public static Vector2 translation = Vector2.Zero;
    public static Vector2 pinchCenter = Vector2.Zero;
    public static float pinchDelta;
    public static bool tapped;
    public static bool doubleTapped;
    public static bool UserIsTouching;

    public static Vector2 tappedPos;
    static DateTime cacheTappedAt;
    static Vector2 cacheTap;

    public static bool held = false;
    static DateTime cacheHeldAt;
    public static Vector2 heldPos = Vector2.Zero;
    public static List<FingerDrag> _draggables = new List<FingerDrag>();
    public static List<Vector2> Taps = new List<Vector2>();

    public enum eDragInputSource { Finger, Mouse };

    struct InputSample
    {
        public GestureSample Gesture;
        public eDragInputSource Source;
        public InputSample(GestureSample sample, eDragInputSource source = eDragInputSource.Finger)
        {
            Gesture = sample;
            Source = source;
        }
    }

    public static void Update()
    {
        UserIsTouching = false;
        List<InputSample> drags = new List<InputSample>();
        //translation = Vector2.Zero;
        translation = translation * .8f;
        if (translation.LengthSquared() < .1f) translation = Vector2.Zero;
        if (translation.LengthSquared() > 25)
            held = false;
        pinchScale = GMath.Decelerate(pinchScale, 1f, .25f, .000005f);
        //pinchScale = 1f;
        //pinchCenter = Vector2.Zero;
        pinchDelta = 0f;
        tapped = false;
        doubleTapped = false;
        Taps.Clear();
        //held = false;
        //tappedPos = Vector2.Zero;
        if (cacheTap != Vector2.Zero && cacheTappedAt.AddMilliseconds(300) < DateTime.Now)
        {
            tapped = true;
            tappedPos = cacheTap;
            cacheTap = Vector2.Zero;
            return;
        }
        //if (held && cacheHeldAt.AddSeconds(2) < DateTime.Now)
        //	held = false;

        _draggables.RemoveAll(d => d.State == FingerDrag.eState.Closed);
        foreach (FingerDrag drag in _draggables)
        {
            if (drag.Attached != null)
                drag.Attached.DragUpdateHandled = false;
            drag.State = FingerDrag.eState.Active;
        }

        while (TouchPanel.IsGestureAvailable)
        {
            GestureSample gesture = TouchPanel.ReadGesture();
            UserIsTouching = true;

            switch (gesture.GestureType)
            {
                case GestureType.FreeDrag:
                    translation = -gesture.Delta;
                    drags.Add(new InputSample(gesture));
                    //else //is this not actually a case?
                    //	drags[gesture.Position]
                    break;
                case GestureType.Tap:
                    cacheTap = gesture.Position;
                    cacheTappedAt = DateTime.Now;
                    held = false;
                    Taps.Add(gesture.Position);
                    break;
                case GestureType.DoubleTap:
                    //Debug.WriteLine(DateTime.Now - cacheTappedAt);
                    tappedPos = gesture.Position;
                    doubleTapped = true;
                    cacheTap = Vector2.Zero;
                    break;
                case GestureType.Pinch:
                    float scaleFactor = GetScaleFactor(gesture.Position, gesture.Position2, gesture.Delta, gesture.Delta2);
                    Vector2 translationDelta = GetTranslationDelta(gesture.Position, gesture.Position2, gesture.Delta, gesture.Delta2, translation, scaleFactor);

                    pinchScale = GMath.RollingAverage(scaleFactor, pinchScale, 5);
                    translation = -(gesture.Delta + gesture.Delta2);
                    pinchCenter = (gesture.Position + gesture.Position2) / 2;
                    break;
                case GestureType.Hold:
                    held = true;
                    heldPos = gesture.Position;
                    cacheHeldAt = DateTime.Now;
                    break;
            }
        }

        foreach (FingerDrag drag in _draggables)
        {
            drag._stale++;
            if (drag.ApplyInertia)
                drag.AverageFrameDelta = new Vector2(GMath.RollingAverage(drag.AverageFrameDelta.X, drag.FrameDelta.X, 5)
                    , GMath.RollingAverage(drag.AverageFrameDelta.Y, drag.FrameDelta.Y, 5));
            drag.FrameDelta = Vector2.Zero;
        }

        if (GMouse.IsDown()) // held has a timeout, pressed is down immediately
        {
            var dist = GScreen.Width / 16;
            // de-duplicate because touch events also count as mouse down in winforms
            if (!drags.Any(d => Vector2.DistanceSquared(new Vector2(GMouse.ScreenX, GMouse.ScreenY), d.Gesture.Position) < dist * dist))
            {
                GestureSample mouse = new GestureSample(
                    GestureType.FreeDrag,
                    new TimeSpan(0, 0, 0, 0, 8),
                    new Vector2(GMouse.ScreenX, GMouse.ScreenY),
                    Vector2.Zero,
                    new Vector2(GMouse.Dx, GMouse.Dy),
                    Vector2.Zero);
                drags.Add(new InputSample(mouse, eDragInputSource.Mouse));
            }
        }

        foreach (InputSample input in drags)
        {
            GestureSample finger = input.Gesture;
            Distributor<FingerDrag> nearby = new Distributor<FingerDrag>(null);
            foreach (FingerDrag drag in _draggables)
            {
                Vector2 vec = new Vector2(drag.Position.X, drag.Position.Y);
                int dist = (int)Vector2.DistanceSquared(finger.Position - finger.Delta, vec);
                int max = (int)Math.Pow(120 * GScreen.Scale, 2);
                if (dist < max)
                    nearby.AddNode(drag, dist);
            }
            FingerDrag closest = nearby.GetLowestNode();
            if (closest != null)
            {
                if (input.Source == closest.InputType)
                {
                    closest.FrameDelta += finger.Delta;
                    closest._stale = 0;
                    closest.Position = finger.Position;
                }
            }
            else
            {
                var fd = new FingerDrag(finger.Position, (input.Source != eDragInputSource.Mouse), input.Source);
                fd.FrameDelta = finger.Delta;
                _draggables.Add(fd);
            }
        }

        foreach (FingerDrag drag in _draggables)
        {
            if (drag.ApplyInertia && drag._stale > 0)
                drag.FrameDelta = GMath.Slide(drag.FrameDelta, drag.AverageFrameDelta, 50, 100);//not an actual animation, but proportional
            if (drag.Attached != null && !drag.Attached.DragUpdateHandled)
            {
                if (drag.ApplyInertia)
                    drag.Attached.DragMove(drag, drag.AverageFrameDelta);
                else
                    drag.Attached.DragMove(drag, drag.FrameDelta);
                drag.Attached.DragUpdateHandled = true;
            }
            if (drag._stale > 5 && drag.FrameDelta.LengthSquared() < 1)
            {
                if (drag.Attached != null) drag.BeginClose();
                else drag.Close();
            }
        }
        _draggables.RemoveAll(d => d.State == FingerDrag.eState.Closed);
    }

    public static IEnumerable<FingerDrag> Touches() // for debugging
    {
        foreach (FingerDrag drag in _draggables)
            yield return drag;
    }

    public static IEnumerable<FingerDrag> GetNewDrags()
    {
        foreach (FingerDrag drag in _draggables)
            if (drag.State == FingerDrag.eState.Waiting)
                yield return drag;
    }

    // we can assume they have an attached object, or they would have been flagged as closed already
    public static IEnumerable<FingerDrag> GetDrops()
    {
        foreach (FingerDrag drag in _draggables)
            if (drag.State == FingerDrag.eState.Closing)
                yield return drag;
    }

    public static void RegisterDraggable(IDraggable drag, FingerDrag touchPoint)
    {
        //foreach (FingerDrag test in _draggables)
        //	if (test.Attached == drag) return;
        touchPoint.Attached = drag;
    }

    public static float GetScaleFactor(Vector2 position1, Vector2 position2, Vector2 delta1, Vector2 delta2)
    {
        Vector2 oldPosition1 = position1 - delta1;
        Vector2 oldPosition2 = position2 - delta2;

        float distance = Vector2.Distance(position1, position2);
        float oldDistance = Vector2.Distance(oldPosition1, oldPosition2);

        if (oldDistance == 0 || distance == 0)
        {
            return 1.0f;
        }

        return distance / oldDistance;
    }

    public static Vector2 GetTranslationDelta(Vector2 position1, Vector2 position2, Vector2 delta1, Vector2 delta2,
        Vector2 objectPos, float scaleFactor)
    {
        Vector2 oldPosition1 = position1 - delta1;
        Vector2 oldPosition2 = position2 - delta2;

        Vector2 newPos1 = position1 + (objectPos - oldPosition1) * scaleFactor;
        Vector2 newPos2 = position2 + (objectPos - oldPosition2) * scaleFactor;
        Vector2 newPos = (newPos1 + newPos2) / 2;

        return newPos - objectPos;
    }

}

public interface IDraggable
{
    void DragMove(FingerDrag drag, Vector2 delta);
    void DragRelease(Vector2 final);
    bool DragUpdateHandled { get; set; }
}

public interface IDroppable
{
    bool TryDrop(FingerDrag drag, IDraggable obj);
}

public class FingerDrag
{
    public Vector2 OriginalPosition;
    public Vector2 Position;
    public enum eState { Waiting, Active, Closing, Closed }
    public eState State = eState.Waiting;
    public int _stale = 0;
    public IDraggable Attached = null;
    public Vector2 FrameDelta = Vector2.Zero;
    public Vector2 AverageFrameDelta = Vector2.Zero;
    public bool ApplyInertia = true;
    public int InertialDampening = 5; // number of samples if inertia is applied - higher means "floateier", assign in control. e.g. Map scrolling should use a lower number
    public GTouch.eDragInputSource InputType; // set initially and not changed, which is probably fine

    public FingerDrag(Vector2 vec, bool applyInertia = true, GTouch.eDragInputSource source = GTouch.eDragInputSource.Finger)
    {
        OriginalPosition = vec;
        Position = vec;
        ApplyInertia = applyInertia;
        InputType = source;
    }

    public void AttachToObject(IDraggable drag)
    { GTouch.RegisterDraggable(drag, this); }

    // if there's no one to tell, we can immediately close
    internal void BeginClose() => State = (Attached == null) ? eState.Closed : eState.Closing;

    internal void Close()
    {
        if (Attached != null)
            Attached.DragRelease(Position);
        State = eState.Closed;
    }
}