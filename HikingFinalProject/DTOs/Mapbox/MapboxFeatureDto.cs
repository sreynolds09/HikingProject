namespace HikingFinalProject.DTOs.Mapbox
{
    public class MapboxFeatureDto
    {

        // public MapboxGeometryDto geometry { get; set; } = new();

        public double[]? Center { get; set; }
        public MapboxGeometryDto? Geometry { get; set; }
    }


    public class MapboxOptions
    {
        public string AccessToken { get; set; } = string.Empty;
    }

}
