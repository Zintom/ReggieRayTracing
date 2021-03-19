using System;
using System.Diagnostics;
using System.Numerics;

namespace Zintom.RayTracer
{
    public interface IRayTraceable
    {
        /// <summary>
        /// Defines whether this object is solid. Rays collide with solid objects.
        /// </summary>
        public bool Solid { get; }
    }

    public static class DDARayTracer
    {

        /// <summary>
        /// Casts a ray at the given <paramref name="angle"/> for a maximum distance and returns a <see cref="Vector2"/> containing the
        /// point of intersection with a tile on the <paramref name="map"/>, if an intersection occurred.
        /// </summary>
        /// <param name="angle">The angle at which the ray will be cast from the ray start position.</param>
        /// <param name="maxDistance">The maximum distance the ray will travel.</param>
        /// <inheritdoc cref="Cast(Vector2, Vector2, IRayTraceable[,])"/>
        public static Vector2? Cast(Vector2 rayStartTrunc, float angle, float maxDistance, IRayTraceable[,] map)
        {
            Vector2 angleVector = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

            return Cast(rayStartTrunc, angleVector * maxDistance, map);
        }

        /// <summary>
        /// Casts a ray between the given points and returns a <see cref="Vector2"/> containing the
        /// point of intersection with a tile on the <paramref name="map"/>, if an intersection occurred.
        /// </summary>
        /// <remarks>
        /// The ray start/end vectors should be in tile coordinates, so for a screen position <c>x16,y16</c>, with a tile size of <c>x16,y16</c>, the tile coordinates would be <c>x1,y1</c>.
        /// <para/>
        /// The position can also be fractional, i.e, a screen position of <c>x18,y18</c>, with a tile size of <c>x16,y16</c>, the tile coordinates would be <c>x1.125,y1.125</c>.
        /// </remarks>
        /// <param name="rayStartTrunc">The ray start position, in tile coordinates.</param>
        /// <param name="rayEndTrunc">The ray end position, in tile coordinates.</param>
        /// <param name="map">The map of <see cref="IRayTraceable"/> objects which represent the tiles of the map.</param>
        /// <returns>The intersection point with a <paramref name="map"/> object, if an intersection occurred.</returns>
        public static Vector2? Cast(Vector2 rayStartTrunc, Vector2 rayEndTrunc, IRayTraceable[,] map)
        {
#if DEBUG
            Stopwatch watch = new();
            watch.Start();
#endif

            Vector2 rayDirectionVector = Vector2.Normalize(new Vector2(rayEndTrunc.X - rayStartTrunc.X, rayEndTrunc.Y - rayStartTrunc.Y));

            // How far along the hypotenuse we travel after travelling 1 unit across a given axis (X or Y)
            Vector2 rayStepSize = new((float)Math.Sqrt(1 + (rayDirectionVector.Y / rayDirectionVector.X) * (rayDirectionVector.Y / rayDirectionVector.X)),
                                      (float)Math.Sqrt(1 + (rayDirectionVector.X / rayDirectionVector.Y) * (rayDirectionVector.X / rayDirectionVector.Y)));


            Vector2 accumulatedRayLength = new(0, 0);

            // In most cases the ray won't start on a axis aligned
            // coordinate, for example it might be on position x:3.5 y:4.6.
            //
            // This means we have to calculate how far "in" to the given tile we are,
            // so for the case above, we would be 0.5 "in" on the X axis and 0.6 "in" on the Y.
            //
            // As we are calculating the ray length, we need the hypotenuse, so we multiply our "in" number
            // by the scaling factor for that axis.
            //
            // Here we also log our "step" direction, which defines which direction we will "walk"
            // when we want to walk on a given axis (X or Y).
            Vector2 step = new(0, 0);
            if (rayDirectionVector.X < 0)
            {
                step.X = -1;
                accumulatedRayLength.X = (rayStartTrunc.X - (int)rayStartTrunc.X) * rayStepSize.X;
            }
            else
            {
                step.X = 1;
                accumulatedRayLength.X = ((int)rayStartTrunc.X + 1 - rayStartTrunc.X) * rayStepSize.X;
            }

            if (rayDirectionVector.Y < 0)
            {
                step.Y = -1;
                accumulatedRayLength.Y = (rayStartTrunc.Y - (int)rayStartTrunc.Y) * rayStepSize.Y;
            }
            else
            {
                step.Y = 1;
                accumulatedRayLength.Y = ((int)rayStartTrunc.Y + 1 - rayStartTrunc.Y) * rayStepSize.Y;
            }

            // Tracks the tile location that we are currently inspecting.
            Vector2 currentTile = new((int)rayStartTrunc.X, (int)rayStartTrunc.Y);

            float distance = 0f;
            float maxDistance = Vector2.Distance(rayStartTrunc, rayEndTrunc);
            while (distance < maxDistance)
            {
                // We walk along the current shortest ray length.
                if (accumulatedRayLength.X < accumulatedRayLength.Y)
                {
                    currentTile.X += step.X;
                    distance = accumulatedRayLength.X;
                    accumulatedRayLength.X += rayStepSize.X;
                }
                else
                {
                    currentTile.Y += step.Y;
                    distance = accumulatedRayLength.Y;
                    accumulatedRayLength.Y += rayStepSize.Y;
                }

                // Guard the collision check so that
                // it is inside the bounds of the tile map.
                if (currentTile.X >= 0 && currentTile.X < map.GetLength(0)
                    && currentTile.Y >= 0 && currentTile.Y < map.GetLength(1))
                {
                    // If the given tile is solid then get the intersection point and return to the caller.
                    if (map[(int)currentTile.X, (int)currentTile.Y].Solid)
                    {
                        Vector2 collisionLocation = GetCollisionIntersection();

#if DEBUG
                        watch.Stop();
                        Debug.WriteLine($"Collision has occurred at X:{collisionLocation.X}, Y:{collisionLocation.Y}, Time taken to cast: {watch.ElapsedTicks / 10_000f}ms ({watch.ElapsedTicks}ns)");
#endif

                        return collisionLocation;
                    }
                }
            }

            Vector2 GetCollisionIntersection()
            {
                Vector2 collisionIntersection = new(rayDirectionVector.X * distance, rayDirectionVector.Y * distance);
                collisionIntersection.X += rayStartTrunc.X;
                collisionIntersection.Y += rayStartTrunc.Y;

                return new(collisionIntersection.X, collisionIntersection.Y);
            }

            return null;
        }

    }
}
