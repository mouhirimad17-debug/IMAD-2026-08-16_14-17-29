using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Shared prop-scatter grid used by every Stage 6-10 room importer
    /// (PlaceIntoFoyer/Kitchen/Office/Bedrooms/Gym). Fix-up: those importers used
    /// to build one dense row-major slot list spanning the whole room and then
    /// just take the first N slots in order - since every room's prop count is far
    /// smaller than that list's row width, every room's props ended up crammed
    /// into a single line along the z=min edge instead of spread across the
    /// room's actual footprint.
    ///
    /// This instead sizes a cols x rows grid to the prop count itself (balanced
    /// to the room's own aspect ratio) and stretches that grid to span the FULL
    /// given area on both axes, so the result is always a real 2D layout that
    /// covers the whole room - the requested `spacing` is honored as a floor
    /// (cells are never packed closer than that), growing wider automatically
    /// when a room is large relative to its prop count so coverage always wins
    /// over hitting the spacing value exactly.
    /// </summary>
    public static class FurnitureGridPlacement
    {
        public static List<Vector2> BuildSlots(float xMin, float xMax, float zMin, float zMax,
            int count, float spacing, Func<float, float, bool> exclude = null)
        {
            var result = new List<Vector2>(count);
            if (count <= 0) return result;

            float areaW = Mathf.Max(0.0001f, xMax - xMin);
            float areaD = Mathf.Max(0.0001f, zMax - zMin);

            int cols = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * areaW / areaD)));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)cols));

            List<Vector2> slots = Generate(cols, rows);
            int guard = 0;
            while (slots.Count < count)
            {
                if (++guard > 500)
                    throw new InvalidOperationException(
                        $"FurnitureGridPlacement.BuildSlots: cannot fit {count} items into a " +
                        $"{areaW:F1}x{areaD:F1}m area at >= {spacing}m spacing (only {slots.Count} valid slots found).");

                // Room too dense for this cols x rows (exclusion zone ate into it,
                // or rounding left it short) - grow the narrower axis and retry.
                if (areaW / cols >= areaD / rows) cols++;
                else rows++;
                slots = Generate(cols, rows);
            }

            return slots.GetRange(0, count);

            List<Vector2> Generate(int c, int r)
            {
                float stepX = c > 1 ? Mathf.Max(spacing, areaW / (c - 1)) : 0f;
                float stepZ = r > 1 ? Mathf.Max(spacing, areaD / (r - 1)) : 0f;
                var list = new List<Vector2>();
                for (int ri = 0; ri < r; ri++)
                for (int ci = 0; ci < c; ci++)
                {
                    float x = xMin + ci * stepX;
                    float z = zMin + ri * stepZ;
                    if (x > xMax || z > zMax) continue;
                    if (exclude != null && exclude(x, z)) continue;
                    list.Add(new Vector2(x, z));
                }
                return list;
            }
        }
    }
}
