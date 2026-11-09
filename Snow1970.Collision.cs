using System;
using System.Collections.Generic;
using System.Windows;
using Drawing = System.Drawing;

namespace Snow1970
{
    internal sealed partial class World
    {
        private sealed class Wall { internal Rect Box; internal Body Owner; }
        private struct Contact
        {
            internal bool Valid;
            internal double T;
            internal int NX, NY;
            internal string Key;
            internal Pile Support;
            internal bool Boundary;
            internal double X, Y, Impact;
        }
        private List<Wall> walls = new List<Wall>();
        private double pickupAfter;
        internal double OverflowMass;

        private void RefreshCollisionGeometry()
        {
            walls.Clear();
            using (Drawing.Drawing2D.Matrix matrix = new Drawing.Drawing2D.Matrix())
                for (int i = 0; i < Bodies.Count; i++)
                {
                    Body body = Bodies[i]; if (!body.Solid) continue;
                    Rect r = body.Rect;
                    using (Drawing.Region visible = new Drawing.Region(new Drawing.RectangleF((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height)))
                    {
                        for (int j = 0; j < i; j++)
                        {
                            if (Bodies[j].Icon) continue;
                            Rect cover = Bodies[j].Rect;
                            visible.Exclude(new Drawing.RectangleF((float)cover.X, (float)cover.Y, (float)cover.Width, (float)cover.Height));
                        }
                        foreach (Drawing.RectangleF part in visible.GetRegionScans(matrix))
                            if (part.Width > .1 && part.Height > .1) walls.Add(new Wall { Box = new Rect(part.X, part.Y, part.Width, part.Height), Owner = body });
                    }
                }
        }
        private Pile SupportFor(string key)
        {
            foreach (Pile support in Piles) if (support.Key == key) return support;
            return null;
        }
        private static bool SweptBox(Rect box, double dx, double dy, Rect solid, out double time, out int nx, out int ny)
        {
            time = 0; nx = ny = 0;
            double overlapX = Math.Min(box.Right, solid.Right) - Math.Max(box.Left, solid.Left);
            double overlapY = Math.Min(box.Bottom, solid.Bottom) - Math.Max(box.Top, solid.Top);
            if (overlapX > .2 && overlapY > .2)
            {
                // Already touching snow must join now, including a sideways touch.
                if (overlapX < overlapY) nx = box.X + box.Width * .5 < solid.X + solid.Width * .5 ? -1 : 1;
                else ny = box.Y + box.Height * .5 < solid.Y + solid.Height * .5 ? -1 : 1;
                return true;
            }
            double enterX, leaveX, enterY, leaveY;
            if (Math.Abs(dx) < 1e-10)
            {
                if (box.Right <= solid.Left + .001 || box.Left >= solid.Right - .001) return false;
                enterX = double.NegativeInfinity; leaveX = double.PositiveInfinity;
            }
            else if (dx > 0) { enterX = (solid.Left - box.Right) / dx; leaveX = (solid.Right - box.Left) / dx; }
            else { enterX = (solid.Right - box.Left) / dx; leaveX = (solid.Left - box.Right) / dx; }
            if (Math.Abs(dy) < 1e-10)
            {
                if (box.Bottom <= solid.Top + .001 || box.Top >= solid.Bottom - .001) return false;
                enterY = double.NegativeInfinity; leaveY = double.PositiveInfinity;
            }
            else if (dy > 0) { enterY = (solid.Top - box.Bottom) / dy; leaveY = (solid.Bottom - box.Top) / dy; }
            else { enterY = (solid.Bottom - box.Top) / dy; leaveY = (solid.Top - box.Bottom) / dy; }
            double enter = Math.Max(enterX, enterY), leave = Math.Min(leaveX, leaveY);
            if (enter < -.000001 || enter > 1 || enter > leave || leave < 0) return false;
            time = Math.Max(0, enter);
            if (enterX > enterY) nx = dx > 0 ? -1 : 1; else ny = dy > 0 ? -1 : 1;
            return true;
        }
        private static void Consider(ref Contact best, double t, int nx, int ny, string key, Pile support, bool boundary = false)
        {
            if (t < -.000001 || t > 1) return;
            if (!best.Valid || t < best.T - 1e-8 || (Math.Abs(t - best.T) < 1e-8 && support != null && best.Support == null))
                best = new Contact { Valid = true, T = Math.Max(0, t), NX = nx, NY = ny, Key = key, Support = support, Boundary = boundary };
        }
        private Contact FirstContact(Rect box, double dx, double dy, bool snow, string departing = null)
        {
            Contact best = new Contact { T = 2 };
            if (dx < 0) Consider(ref best, (Bounds.Left - box.Left) / dx, 1, 0, "screen:left", null, true);
            if (dx > 0) Consider(ref best, (Bounds.Right - box.Right) / dx, -1, 0, "screen:right", null, true);
            if (dy < 0) Consider(ref best, (Bounds.Top - box.Top) / dy, 0, 1, "screen:top", null, true);
            if (dy > 0) Consider(ref best, (Piles[0].Y - box.Bottom) / dy, 0, -1, "ground", Piles[0], true);
            foreach (Wall wall in walls)
            {
                double t; int nx, ny;
                if (!SweptBox(box, dx, dy, wall.Box, out t, out nx, out ny)) continue;
                Pile support = ny == -1 && wall.Owner.Roof ? SupportFor(wall.Owner.Key) : null;
                Consider(ref best, t, nx, ny, wall.Owner.Key, support, true);
            }
            if (!snow) return best;
            double left = Math.Min(box.Left, box.Left + dx), right = Math.Max(box.Right, box.Right + dx);
            double top = Math.Min(box.Top, box.Top + dy), bottom = Math.Max(box.Bottom, box.Bottom + dy);
            foreach (Pile support in Piles)
            {
                if (support.Key == departing) continue;
                if (right <= support.X || left >= support.X + support.Width || top > support.Y + .5) continue;
                int first = Math.Max(0, (int)((left - support.X) / support.Cell));
                int last = Math.Min(support.Height.Length - 1, (int)((right - support.X) / support.Cell));
                for (int i = first; i <= last; i++)
                {
                    double h = support.Height[i]; if (!support.Exposed[i] || h <= .08 || bottom < support.Y - h) continue;
                    Rect column = new Rect(support.X + i * support.Cell, support.Y - h, support.Cell, h + .2);
                    double t; int nx, ny;
                    if (SweptBox(box, dx, dy, column, out t, out nx, out ny)) Consider(ref best, t, nx, ny, support.Key, support);
                }
            }
            return best;
        }
        private bool Obstructed(Rect box)
        {
            foreach (Wall wall in walls)
                if (Math.Min(box.Right, wall.Box.Right) - Math.Max(box.Left, wall.Box.Left) > .3 && Math.Min(box.Bottom, wall.Box.Bottom) - Math.Max(box.Top, wall.Box.Top) > .3) return true;
            return false;
        }
        private bool FreeBox(Rect box, double dx, double dy)
        {
            box.Offset(dx, dy);
            return box.Left >= Bounds.Left - .01 && box.Right <= Bounds.Right + .01 && box.Top >= Bounds.Top - .01 && box.Bottom <= Piles[0].Y + .01 && !Obstructed(box);
        }
        private Vector WallPenetration(Rect box)
        {
            if (!Obstructed(box)) return new Vector();
            // Resolve against the union: a narrow gap between two windows must
            // not bounce an embedded clod back and forth between internal edges.
            List<double> xs = new List<double> { 0 }, ys = new List<double> { 0 };
            foreach (Wall wall in walls)
            {
                Rect r = wall.Owner.Rect;
                xs.Add(r.Left - box.Right - .35); xs.Add(r.Right - box.Left + .35);
                ys.Add(r.Top - box.Bottom - .35); ys.Add(r.Bottom - box.Top + .35);
            }
            Vector best = new Vector(); double length = double.MaxValue;
            foreach (double x in xs) if (x * x < length && FreeBox(box, x, 0)) { best = new Vector(x, 0); length = x * x; }
            foreach (double y in ys) if (y * y < length && FreeBox(box, 0, y)) { best = new Vector(0, y); length = y * y; }
            if (length < double.MaxValue) return best;
            foreach (double x in xs) foreach (double y in ys)
                if (x * x + y * y < length && FreeBox(box, x, y)) { best = new Vector(x, y); length = x * x + y * y; }
            return best;
        }
        private void ConfineActor(Pile actor)
        {
            Rect box = actor.VisualBounds();
            if (box.Left < Bounds.Left) { actor.X += Bounds.Left - box.Left; actor.FallVX = Math.Abs(actor.FallVX) * .3; }
            if (box.Right > Bounds.Right) { actor.X -= box.Right - Bounds.Right; actor.FallVX = -Math.Abs(actor.FallVX) * .3; }
            box = actor.VisualBounds();
            if (box.Top < Bounds.Top) { actor.Y += Bounds.Top - box.Top; actor.FallVY = Math.Max(0, Math.Abs(actor.FallVY) * .15); }
            if (box.Bottom > Piles[0].Y) actor.Y -= box.Bottom - Piles[0].Y;
        }
        private Contact MoveActor(Pile actor, double dx, double dy, bool held)
        {
            ConfineActor(actor);
            for (int i = 0; i < 3; i++)
            {
                Vector correction = WallPenetration(actor.VisualBounds());
                if (correction.LengthSquared < .00001) break;
                actor.X += correction.X; actor.Y += correction.Y;
            }
            Contact result = new Contact();
            for (int pass = 0; pass < 4; pass++)
            {
                Rect box = actor.VisualBounds();
                string departing = actor.CohesiveLaunch && actor.FallVY < 0 ? actor.DepartedSupport : null;
                Contact hit = FirstContact(box, dx, dy, !held, departing);
                if (!hit.Valid) { actor.X += dx; actor.Y += dy; break; }
                actor.X += dx * hit.T + hit.NX * .12; actor.Y += dy * hit.T + hit.NY * .12;
                Rect at = actor.VisualBounds(); hit.X = at.X + at.Width * .5; hit.Y = at.Y + at.Height * .5;
                hit.Impact = Math.Abs(actor.FallVX * hit.NX + actor.FallVY * hit.NY);
                double remaining = Math.Max(0, 1 - hit.T);
                bool leavingRim = actor.DepartedSupport == hit.Key && hit.NY == -1 && actor.FallVY <= 0 && actor.FallAge < .18 && Math.Abs(actor.FallVX) > 2;
                if (!held && hit.Support != null && !leavingRim) return hit;
                result = hit;
                if (hit.NX != 0)
                {
                    actor.FallVX = held ? actor.FallVX : -actor.FallVX * .28;
                    dx = held ? 0 : -dx * remaining * .28; dy *= remaining;
                }
                else
                {
                    actor.FallVY = leavingRim || held ? 0 : -actor.FallVY * .18;
                    dy = leavingRim || held ? 0 : -dy * remaining * .18; dx *= remaining;
                }
                if (Math.Abs(dx) + Math.Abs(dy) < .0001) break;
            }
            ConfineActor(actor); return result.Support == null ? result : new Contact();
        }
        private Pile ResolveFlakeMotion(ref Flake flake, double ox, double oy)
        {
            Rect box = new Rect(ox - flake.Radius, oy - flake.Radius, flake.Radius * 2, flake.Radius * 2);
            Vector correction = WallPenetration(box);
            if (correction.LengthSquared > 0)
            {
                ox += correction.X; oy += correction.Y; flake.X += correction.X; flake.Y += correction.Y;
                box.Offset(correction);
            }
            Contact hit = FirstContact(box, flake.X - ox, flake.Y - oy, false);
            if (hit.Valid)
            {
                flake.X = ox + (flake.X - ox) * hit.T + hit.NX * .15;
                flake.Y = oy + (flake.Y - oy) * hit.T + hit.NY * .15;
                if (hit.Support != null && hit.NY == -1 && flake.VY > 0) return hit.Support;
                if (hit.NX != 0) flake.VX = -flake.VX * .3;
                if (hit.NY != 0) flake.VY = -flake.VY * .2;
            }
            flake.X = Settings.Clamp(flake.X, Bounds.Left + flake.Radius, Bounds.Right - flake.Radius);
            return null;
        }
        private double CapacityAt(Pile pile, int i)
        {
            double limit = pile.Ground ? pile.MaxHeight : Math.Min(pile.MaxHeight, Math.Max(1, pile.Y - Bounds.Top - 1));
            double cap = limit * (.955 + .025 * Math.Sin(i * .093 + pile.ShapePhase) + .018 * Math.Sin(i * .211 + 1));
            if (!pile.Ground)
            {
                double d = Math.Min((i + .5) * pile.Cell, pile.Width - (i + .5) * pile.Cell);
                cap *= Math.Sin(Math.Min(1, d / Math.Min(9, pile.Width * .4)) * Math.PI * .5);
            }
            return Math.Max(.1, cap);
        }
        private void ResolveCapacity(Pile pile, double center)
        {
            if (pile.Snowball || pile.Dynamic) return;
            double surplus = 0;
            for (int i = 0; i < pile.Height.Length; i++)
            {
                double cap = CapacityAt(pile, i);
                if (pile.Height[i] > cap) { surplus += (pile.Height[i] - cap) * pile.Cell; pile.Height[i] = cap; }
            }
            if (surplus < .001) return;
            center = Settings.Clamp(center, pile.X, pile.X + pile.Width);
            double radius = Math.Max(26, pile.MaxHeight * 1.5);
            // Broaden a rounded bank in the same physics step; no clipped flat top.
            for (int pass = 0; pass < 7 && surplus > .001; pass++)
            {
                double room = 0;
                for (int i = 0; i < pile.Height.Length; i++)
                {
                    if (!pile.Exposed[i]) { pile.NextHeight[i] = 0; continue; }
                    double distance = Math.Abs(pile.X + (i + .5) * pile.Cell - center);
                    double dome = distance < radius ? .5 + .5 * Math.Cos(distance * Math.PI / radius) : 0;
                    double available = Math.Max(0, CapacityAt(pile, i) * dome - pile.Height[i]);
                    pile.NextHeight[i] = available; room += available * pile.Cell;
                }
                if (room > .0001)
                {
                    double fraction = Math.Min(1, surplus / room);
                    for (int i = 0; i < pile.Height.Length; i++) pile.Height[i] += pile.NextHeight[i] * fraction;
                    surplus = Math.Max(0, surplus - room * fraction);
                }
                radius *= 1.8;
            }
            if (surplus > .01)
            {
                // A full screen is a finite sandbox. Excess leaves the bank as powder
                // and sublimates, instead of being silently cut or recycled forever.
                OverflowMass += surplus;
                int count = Math.Min(8, Math.Max(2, (int)(surplus / 35)));
                for (int i = 0; i < count; i++)
                {
                    double x = Settings.Clamp(center + (i - count * .5) * 8, Bounds.Left + 3, Bounds.Right - 3);
                    if (Count >= Limit && Count > 0) Count--;
                    int index = Count;
                    Add(x, pile.Y - pile.At(x) - 2, i % 2 == 0 ? -55 : 55, -55 - random.NextDouble() * 30, 1.2 + random.NextDouble(), surplus / count);
                    if (Count > index) { Flake flake = Flakes[index]; flake.Ballistic = true; flake.Sublimating = true; Flakes[index] = flake; }
                }
            }
        }
        private void DepositActor(Pile actor, Pile support)
        {
            double profile = 0; for (int i = 0; i < actor.Height.Length; i++) profile += actor.Height[i] * actor.Cell;
            double mass = actor.Mass();
            if (profile < .001) { support.Deposit(actor.X + actor.Width * .5, mass); }
            else
            {
                double center = actor.X + actor.Width * .5;
                double flatten = actor.Snowball ? 1.22 : 1.05;
                for (int i = 0; i < actor.Height.Length; i++)
                {
                    double x = center + ((i + .5) * actor.Cell - actor.Width * .5) * flatten;
                    double pos = (x - support.X) / support.Cell - .5;
                    int left = (int)Math.Floor(pos); double f = pos - left;
                    double h = actor.Height[i] * actor.Cell * mass / profile / support.Cell;
                    int a = Math.Max(0, Math.Min(support.Height.Length - 1, left)), b = Math.Max(0, Math.Min(support.Height.Length - 1, left + 1));
                    support.Height[a] += h * (1 - f); support.Height[b] += h * f;
                }
            }
            ResolveCapacity(support, actor.X + actor.Width * .5); support.BuildShape(); TerrainVersion++;
            pickupAfter = Time + 1.0 / 120;
        }
        private void Shatter(Pile actor, Contact hit)
        {
            double mass = actor.Mass(); Rect shape = actor.VisualBounds();
            double x = hit.X - hit.NX * Math.Max(0, shape.Width * .5 - 2);
            double y = hit.Y - hit.NY * Math.Max(0, shape.Height * .5 - 2);
            double spread = Settings.Clamp(hit.Impact * .38, 60, 230);
            int pieces = Math.Min(Math.Min(24, Limit), Math.Max(9, (int)(mass / 30)));
            for (int i = 0; i < pieces; i++)
            {
                double side = (i + .5) / pieces * 2 - 1;
                double outSpeed = 35 + random.NextDouble() * 60;
                double vx = hit.NX * outSpeed - hit.NY * side * spread + actor.FallVX * .08;
                double vy = hit.NY * outSpeed + hit.NX * side * spread + actor.FallVY * .05 - 20;
                double radius = i % 5 == 0 ? 2.7 + random.NextDouble() * 1.2 : 1.1 + random.NextDouble() * 1.4;
                double px = Settings.Clamp(x - hit.NY * side * Math.Min(12, shape.Width * .3) + hit.NX * radius, Bounds.Left + radius, Bounds.Right - radius);
                double py = Settings.Clamp(y + hit.NX * side * Math.Min(12, shape.Height * .3) + hit.NY * radius, Bounds.Top + radius, Piles[0].Y - radius - .1);
                PriorityAdd(px, py, vx, vy, radius, mass / pieces);
            }
            if (actor.SmoothPath != null) { actor.SmoothPath.Dispose(); actor.SmoothPath = null; }
            TerrainVersion++;
        }
        private void ProfileBall(Pile ball)
        {
            for (int i = 0; i < ball.Height.Length; i++)
            {
                double dx = (i + .5) * ball.Cell - ball.Width * .5;
                ball.Height[i] = Math.Abs(dx) >= ball.BallRadius ? 0 : ball.BallRadius + Math.Sqrt(Math.Max(0, ball.BallRadius * ball.BallRadius - dx * dx));
            }
            ball.BuildShape();
        }
        private void MergeAirborne()
        {
            for (int pass = 0; pass < 12; pass++)
            {
                bool merged = false;
                for (int i = 0; i < Falling.Count && !merged; i++)
                    for (int j = i + 1; j < Falling.Count; j++)
                    {
                        Pile a = Falling[i], b = Falling[j]; Rect ra = a.VisualBounds(), rb = b.VisualBounds();
                        double overlapX = Math.Min(ra.Right, rb.Right) - Math.Max(ra.Left, rb.Left);
                        double overlapY = Math.Min(ra.Bottom, rb.Bottom) - Math.Max(ra.Top, rb.Top);
                        if (overlapX < 1.5 || overlapY < 1.5) continue;
                        Vector delta = new Vector(rb.X + rb.Width * .5 - ra.X - ra.Width * .5, rb.Y + rb.Height * .5 - ra.Y - ra.Height * .5);
                        if (delta.X * (b.FallVX - a.FallVX) + delta.Y * (b.FallVY - a.FallVY) > 2) continue;
                        double ma = a.Mass(), mb = b.Mass(), mass = ma + mb; if (mass < 1) continue;
                        Rect footprint = Rect.Union(ra, rb);
                        double aspect = Math.Max(footprint.Width / Math.Max(1, footprint.Height), footprint.Height / Math.Max(1, footprint.Width));
                        double fill = mass / Math.Max(1, footprint.Width * footprint.Height);
                        double maxRadius = Config.Extreme ? 42 : 32;
                        if (aspect > 2.5 || fill < .56 || mass > Math.PI * maxRadius * maxRadius) continue;
                        double radius = Math.Sqrt(mass / Math.PI), width = Math.Max(72, radius * 2.7), height = Math.Max(66, radius * 2.6);
                        double x = ((ra.X + ra.Width * .5) * ma + (rb.X + rb.Width * .5) * mb) / mass;
                        double y = ((ra.Y + ra.Height * .5) * ma + (rb.Y + rb.Height * .5) * mb) / mass;
                        Pile clod = new Pile("join:" + (++detachedSerial), x - width * .5, y + radius, width, height, false)
                        {
                            Snowball = true, Dynamic = true, CohesiveLaunch = true, BallMass = mass, BallRadius = radius,
                            FallVX = (a.FallVX * ma + b.FallVX * mb) / mass, FallVY = (a.FallVY * ma + b.FallVY * mb) / mass
                        };
                        ProfileBall(clod); ConfineActor(clod);
                        Falling.RemoveAt(j); Falling.RemoveAt(i); Falling.Add(clod); TerrainVersion++; merged = true; break;
                    }
                if (!merged) break;
            }
        }
    }
}
