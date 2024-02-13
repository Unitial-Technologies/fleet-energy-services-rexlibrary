namespace Cloud
{
    internal static class Downsampling
    {
        internal static List<PointD> DouglasPeucker(List<PointD> points, double tolerance)
        {
            if (points == null || points.Count < 3)
                return points;

            // Find the point with the maximum distance
            double maxDistance = 0;
            int maxIndex = 0;

            int lastIndex = points.Count - 1;

            for (int i = 1; i < lastIndex; i++)
            {
                double distance = PointToLineDistance(points[i], points[0], points[lastIndex]);

                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }

            // If the maximum distance is greater than the tolerance, recursively simplify
            if (maxDistance > tolerance)
            {
                // Recursive call for the left and right segments
                List<PointD> leftSegment = DouglasPeucker(points.GetRange(0, maxIndex + 1), tolerance);
                List<PointD> rightSegment = DouglasPeucker(points.GetRange(maxIndex, lastIndex - maxIndex + 1), tolerance);

                // Combine the results
                List<PointD> result = new List<PointD>(leftSegment);
                result.AddRange(rightSegment.Skip(1)); // Skip the common point

                return result;
            }
            else
            {
                // If the maximum distance is within tolerance, return the segment end points
                return new List<PointD> { points[0], points[lastIndex] };
            }
        }

        static double PointToLineDistance(PointD point, PointD lineStart, PointD lineEnd)
        {
            double a = lineEnd.Value - lineStart.Value;
            double b = lineStart.Timestamp - lineEnd.Timestamp;
            double c = (lineEnd.Timestamp * lineStart.Value) - (lineStart.Timestamp * lineEnd.Value);

            return Math.Abs((a * point.Timestamp) + (b * point.Value) + c) / Math.Sqrt((a * a) + (b * b));
        }
    }
}

