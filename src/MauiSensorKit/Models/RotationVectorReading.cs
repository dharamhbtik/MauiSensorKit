namespace MauiSensorKit;

/// <summary>
/// Represents a rotation vector sensor reading as a quaternion for device orientation.
/// </summary>
public sealed record RotationVectorReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.RotationVector;

    /// <summary>
    /// Gets the X component of the quaternion.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets the Y component of the quaternion.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets the Z component of the quaternion.
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// Gets the W (scalar) component of the quaternion.
    /// </summary>
    public double W { get; init; }

    /// <summary>
    /// Gets the estimated heading accuracy in degrees, or null if unavailable.
    /// </summary>
    public double? HeadingAccuracy { get; init; }

    /// <summary>
    /// Gets the Euler pitch angle (rotation around X-axis) in radians.
    /// </summary>
    public double Pitch => Math.Asin(2.0 * (W * Y - Z * X));

    /// <summary>
    /// Gets the Euler roll angle (rotation around Y-axis) in radians.
    /// </summary>
    public double Roll => Math.Atan2(2.0 * (W * X + Y * Z), 1.0 - 2.0 * (X * X + Y * Y));

    /// <summary>
    /// Gets the Euler yaw/azimuth angle (rotation around Z-axis) in radians.
    /// </summary>
    public double Yaw => Math.Atan2(2.0 * (W * Z + X * Y), 1.0 - 2.0 * (Y * Y + Z * Z));

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        var accuracy = HeadingAccuracy.HasValue ? $", Accuracy={HeadingAccuracy.Value:F1}°" : "";
        return $"Rotation: [{X:F3}, {Y:F3}, {Z:F3}, {W:F3}]{accuracy}";
    }
}
